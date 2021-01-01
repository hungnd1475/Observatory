using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Core.Services.ChangeTracking;
using Observatory.Core.Services.Models;
using Observatory.Providers.Exchange.Models;
using Observatory.Providers.Exchange.Persistence;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Graph.HeaderHelper;
using MG = Microsoft.Graph;
using AM = AutoMapper;
using System.Net.Http;
using static Microsoft.Graph.SerializerExtentions;

namespace Observatory.Providers.Exchange.Services
{
    public class ExchangeMailService : IMailService, IEnableLogger
    {
        static class SpecialFolders
        {
            public const string ROOT = "msgfolderroot";
            public const string ARCHIVE = "archive";
            public const string CONVERSATION_HISTORY = "conversationhistory";
            public const string JUNK = "junkemail";
            public const string OUTBOX = "outbox";
        }

        public const string MESSAGES_SELECT_QUERY = "Subject,Sender,ReceivedDateTime,IsRead,Importance," +
            "HasAttachments,Flag,ToRecipients,CcRecipients,Body," +
            "ConversationId,ConversationIndex,IsDraft,ParentFolderId," +
            "From,BodyPreview";

        public const string PREFER_HEADER = "Prefer";
        public const string MAX_PAGE_SIZE = "odata.maxpagesize";

        public const string MAIL_FOLDER_ROOT = "msgfolderroot";

        private readonly ProfileRegister _register;
        private readonly ExchangeProfileDataStore.Factory _storeFactory;
        private readonly MG.GraphServiceClient _client;
        private readonly AM.IMapper _mapper;
        private readonly Subject<DeltaSet<MailFolder>> _folderChanges = new Subject<DeltaSet<MailFolder>>();
        private readonly Subject<DeltaSet<Message>> _messageChanges = new Subject<DeltaSet<Message>>();

        public IObservable<DeltaSet<MailFolder>> FolderChanges => _folderChanges.AsObservable();

        public IObservable<DeltaSet<Message>> MessageChanges => _messageChanges.AsObservable();

        public ExchangeMailService(ProfileRegister register,
            ExchangeProfileDataStore.Factory storeFactory,
            MG.GraphServiceClient client,
            AM.IMapper mapper)
        {
            _register = register;
            _storeFactory = storeFactory;
            _client = client;
            _mapper = mapper;
        }

        public async Task InitializeAsync()
        {
            using var store = _storeFactory.Invoke(_register.DataFilePath, true);
            if (await store.Database.EnsureCreatedAsync())
            {
                store.Profiles.Add(new Profile()
                {
                    EmailAddress = _register.EmailAddress,
                    DisplayName = _register.EmailAddress,
                    ProviderId = _register.ProviderId,
                });
                store.FolderSynchronizationStates.Add(new FolderSynchronizationState()
                {
                    Id = _register.EmailAddress,
                });
                await store.SaveChangesAsync();
            }
        }

        public async Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default)
        {
            using var store = _storeFactory.Invoke(_register.DataFilePath, false);
            var syncState = await store.FolderSynchronizationStates.FirstAsync();

            if (syncState.DeltaLink == null)
            {
                static async Task<MailFolder> RequestSpecialFolder(MG.IMailFolderRequestBuilder requestBuilder,
                    FolderType type, bool isFavorite,
                    AM.IMapper mapper)
                {
                    var folder = await requestBuilder.Request()
                        .GetAsync()
                        .ConfigureAwait(false);
                    return mapper.Map<MG.MailFolder, MailFolder>(folder, opt => opt.AfterMap((src, dst) =>
                    {
                        dst.IsFavorite = isFavorite;
                        dst.Type = type;
                    }));
                }

                var specialFolders = await Task.WhenAll(
                    RequestSpecialFolder(_client.Me.MailFolders[SpecialFolders.ROOT], FolderType.Root, false, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders.Inbox, FolderType.Inbox, true, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders.SentItems, FolderType.SentItems, true, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders.Drafts, FolderType.Drafts, true, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders.DeletedItems, FolderType.DeletedItems, true, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders[SpecialFolders.ARCHIVE], FolderType.Archive, true, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders[SpecialFolders.JUNK], FolderType.OtherSpecial, false, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders[SpecialFolders.OUTBOX], FolderType.OtherSpecial, false, _mapper),
                    RequestSpecialFolder(_client.Me.MailFolders[SpecialFolders.CONVERSATION_HISTORY], FolderType.OtherSpecial, false, _mapper));
                var specialIds = new HashSet<string>(specialFolders.Select(f => f.Id));
                var folders = new List<MailFolder>(specialFolders);

                var pageSize = 20;
                var request = _client.Me.MailFolders
                    .Delta()
                    .Request()
                    .Header(PREFER_HEADER, $"{MAX_PAGE_SIZE}={pageSize}");

                while (true)
                {
                    var page = await request.GetAsync(cancellationToken)
                        .ConfigureAwait(false);
                    folders.AddRange(page
                        .Where(f => !specialIds.Contains(f.Id))
                        .Select(f => _mapper.Map<MG.MailFolder, MailFolder>(f)));

                    if (page.NextPageRequest != null)
                    {
                        request = page.NextPageRequest;
                    }
                    else
                    {
                        syncState.DeltaLink = page.GetDeltaLink();
                        store.Update(syncState);
                        store.AddRange(folders);
                        store.AddRange(folders.Select(f => new MessageSynchronizationState()
                        {
                            FolderId = f.Id,
                        }));
                        await store.SaveChangesAsync();

                        if (folders.Count > 0 && _folderChanges.HasObservers)
                        {
                            var changes = new DeltaSet<MailFolder>(folders
                                .Select(f => DeltaEntity.Added(f))
                                .ToList().AsEnumerable());
                            _folderChanges.OnNext(changes);
                        }
                        break;
                    }
                }
            }
            else
            {
                MG.IMailFolderDeltaCollectionPage page = new MG.MailFolderDeltaCollectionPage();
                page.InitializeNextPageRequest(_client, syncState.DeltaLink);

                var deltaFolders = new Dictionary<string, MG.MailFolder>();
                var removedFolders = new List<MailFolder>();

                while (page.NextPageRequest != null)
                {
                    var request = page.NextPageRequest;
                    page = await request.GetAsync(cancellationToken)
                        .ConfigureAwait(false);
                    foreach (var f in page)
                    {
                        if (f.IsRemoved())
                        {
                            removedFolders.Add(new MailFolder() { Id = f.Id });
                        }
                        else
                        {
                            deltaFolders.Add(f.Id, f);
                        }
                    }
                }

                var changes = new DeltaSet<MailFolder>();

                if (deltaFolders.Count > 0)
                {
                    var updatedFolders = await store.Folders
                        .Where(f => deltaFolders.Keys.Contains(f.Id))
                        .ToDictionaryAsync(f => f.Id);
                    var newFolders = deltaFolders.Values
                        .Where(f => !updatedFolders.ContainsKey(f.Id))
                        .Select(f => _mapper.Map<MG.MailFolder, MailFolder>(f))
                        .ToList();
                    foreach (var f in updatedFolders)
                    {
                        _mapper.Map(deltaFolders[f.Key], f.Value);
                    }

                    store.AddRange(newFolders);
                    store.AddRange(newFolders.Select(f => new MessageSynchronizationState() { FolderId = f.Id }));
                    changes.AddRange(newFolders.Select(f => DeltaEntity.Added(f)));
                    changes.AddRange(updatedFolders.Values.Select(f => DeltaEntity.Updated(f)));
                }

                if (removedFolders.Count > 0)
                {
                    store.RemoveRange(removedFolders);
                    store.RemoveRange(removedFolders.Select(f => new MessageSynchronizationState() { FolderId = f.Id }));
                    changes.AddRange(removedFolders.Select(f => DeltaEntity.Removed(f)));
                }

                syncState.DeltaLink = page.GetDeltaLink();
                store.Update(syncState);
                await store.SaveChangesAsync(false)
                    .ConfigureAwait(false);

                if (_folderChanges.HasObservers)
                {
                    _folderChanges.OnNext(changes);
                }
            }
        }

        public async Task SynchronizeMessagesAsync(string folderId, CancellationToken cancellationToken = default)
        {
            using var store = _storeFactory.Invoke(_register.DataFilePath, true);
            var syncState = await store.MessageSynchronizationStates.FindAsync(folderId);

            MG.IMessageDeltaRequest request = null;
            MG.IMessageDeltaCollectionPage page = new MG.MessageDeltaCollectionPage();

            if (syncState.NextLink != null)
            {
                page.InitializeNextPageRequest(_client, syncState.NextLink);
                request = page.NextPageRequest;
            }
            else if (syncState.DeltaLink != null)
            {
                page.InitializeNextPageRequest(_client, syncState.DeltaLink);
                request = page.NextPageRequest;
            }
            else
            {
                var pageSize = 30;
                request = _client.Me.MailFolders[folderId]
                    .Messages
                    .Delta()
                    .Request()
                    .Select(MESSAGES_SELECT_QUERY)
                    .Header(PREFER_HEADER, $"{MAX_PAGE_SIZE}={pageSize}")
                    .OrderBy("ReceivedDateTime desc")
                    .Filter($"ReceivedDateTime ge {DateTimeOffset.UtcNow.AddMonths(-1):yyyy-MM-dd}");
            }

            while (!cancellationToken.IsCancellationRequested && request != null)
            {
                page = await request.GetAsync(cancellationToken)
                    .ConfigureAwait(false);
                var deltaMessages = page.Where(m => !m.IsRemoved())
                    .ToDictionary(m => m.Id);
                var removedIds = page.Where(m => m.IsRemoved())
                    .Select(m => m.Id)
                    .ToArray();

                if (deltaMessages.Count > 0)
                {
                    var updatedMessages = await store.Messages
                        .Where(m => deltaMessages.Keys.Contains(m.Id))
                        .ToDictionaryAsync(m => m.Id);
                    var newMessages = deltaMessages
                        .Where(m => !updatedMessages.Keys.Contains(m.Key))
                        .Select(m => _mapper.Map<MG.Message, Message>(m.Value))
                        .ToArray();

                    foreach (var m in updatedMessages)
                    {
                        _mapper.Map(deltaMessages[m.Key], m.Value);
                    }

                    store.AddRange(newMessages);
                }

                if (removedIds.Length > 0)
                {
                    var removedMessages = await store.Messages
                        .Where(m => m.FolderId == folderId && removedIds.Contains(m.Id))
                        .ToListAsync();
                    store.RemoveRange(removedMessages);
                }

                if (page.NextPageRequest != null)
                {
                    request = page.NextPageRequest;
                    syncState.NextLink = page.GetNextLink();
                }
                else
                {
                    request = null;
                    syncState.NextLink = null;
                    syncState.DeltaLink = page.GetDeltaLink();
                }

                // save without accepting changes so that we can publish the changes after
                await store.SaveChangesAsync(acceptAllChangesOnSuccess: false)
                    .ConfigureAwait(false);

                if (_messageChanges.HasObservers)
                {
                    var changes = store.GetChanges<Message>();
                    if (changes.Count > 0)
                    {
                        _messageChanges.OnNext(changes);
                    }
                }

                store.ChangeTracker.AcceptAllChanges();
            }
        }

        public IEntityUpdater<UpdatableMessage> UpdateMessage(IReadOnlyList<string> messageIds)
        {
            return new RelayEntityUpdater<UpdatableMessage>(async sourceMessage =>
            {
                var serializer = new MG.Serializer();
                var targetMessage = _mapper.Map<UpdatableMessage, MG.Message>(sourceMessage);
                await Task.WhenAll(messageIds.Paginate(20).Select(async page =>
                {
                    var batch = new MG.BatchRequestContent();
                    foreach (var id in page)
                    {
                        var request = _client.Me.Messages[id]
                            .Request()
                            .GetHttpRequestMessage();
                        request.Method = new HttpMethod("PATCH");
                        request.Content = serializer.SerializeAsJsonContent(targetMessage);
                        batch.AddBatchRequestStep(request);
                    }
                    await _client.Batch.Request()
                        .PostAsync(batch)
                        .ConfigureAwait(false);
                }));

                using var store = _storeFactory.Invoke(_register.DataFilePath, true);
                var originalMessages = await store.Messages
                    .Where(m => messageIds.Contains(m.Id))
                    .ToArrayAsync();
                foreach (var m in originalMessages)
                {
                    _mapper.Map(sourceMessage, m);
                }
                await store.SaveChangesAsync()
                    .ConfigureAwait(false);

                if (_messageChanges.HasObservers)
                {
                    var changes = new DeltaSet<Message>(originalMessages
                        .Select(m => DeltaEntity.Updated(m)));
                    _messageChanges.OnNext(changes);
                }
            });
        }
    }
}
