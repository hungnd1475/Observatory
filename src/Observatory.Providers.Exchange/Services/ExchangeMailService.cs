using DynamicData;
using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using Observatory.Core.Services;
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

namespace Observatory.Providers.Exchange.Services
{
    public class ExchangeMailService : IMailService
    {
        public const string REMOVED_FLAG = "@removed";
        public const string DELTA_LINK = "@odata.deltaLink";
        public const string NEXT_LINK = "@odata.nextLink";

        public const string MESSAGES_SELECT_QUERY = "Subject,Sender,ReceivedDateTime,IsRead,Importance," +
            "HasAttachments,Flag,ToRecipients,CcRecipients,Body," +
            "ConversationId,ConversationIndex,IsDraft,ParentFolderId," +
            "From,BodyPreview";

        public const string PREFER_HEADER = "Prefer";
        public const string MAX_PAGE_SIZE = "odata.maxpagesize";

        private readonly ProfileRegister _register;
        private readonly ExchangeProfileDataStore.Factory _storeFactory;
        private readonly MG.GraphServiceClient _client;
        private readonly Subject<IEnumerable<DeltaEntity<MailFolder>>> _folderChanges =
            new Subject<IEnumerable<DeltaEntity<MailFolder>>>();
        private readonly Subject<IEnumerable<DeltaEntity<MessageSummary>>> _messageChanges =
            new Subject<IEnumerable<DeltaEntity<MessageSummary>>>();

        public IObservable<IEnumerable<DeltaEntity<MailFolder>>> FolderChanges => _folderChanges.AsObservable();

        public IObservable<IEnumerable<DeltaEntity<MessageSummary>>> MessageChanges => _messageChanges.AsObservable();

        public ExchangeMailService(ProfileRegister register,
            ExchangeProfileDataStore.Factory storeFactory,
            MG.GraphServiceClient client)
        {
            _register = register;
            _storeFactory = storeFactory;
            _client = client;
        }

        public async Task InitializeAsync()
        {
            using var store = _storeFactory.Invoke(_register.DataFilePath);
            if (await store.Database.EnsureCreatedAsync())
            {
                store.Profiles.Add(new Profile()
                {
                    EmailAddress = _register.EmailAddress,
                    DisplayName = _register.EmailAddress,
                    ProviderId = _register.ProviderId,
                });
                store.FolderSynchronizationStates.Add(new FolderSynchronizationState());
                store.MessageSynchronizationStates.Add(new MessageSynchronizationState());
                await store.SaveChangesAsync();
            }
        }

        public Task<MessageDetail> FetchMessageDetailAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default)
        {
            using var store = _storeFactory.Invoke(_register.DataFilePath);
            var state = await store.FolderSynchronizationStates.FirstAsync();
            if (state.DeltaLink == null)
            {
                static async Task<MailFolder> RequestSpecialFolder(MG.IMailFolderRequestBuilder requestBuilder, 
                    FolderType type, bool isFavorite)
                {
                    var folder = await requestBuilder.Request()
                        .GetAsync()
                        .ConfigureAwait(false);
                    return folder.Create(type, isFavorite);
                }
                var specialFolders = await Task.WhenAll(
                    RequestSpecialFolder(_client.Me.MailFolders.Inbox, FolderType.Inbox, true),
                    RequestSpecialFolder(_client.Me.MailFolders.SentItems, FolderType.SentItems, true),
                    RequestSpecialFolder(_client.Me.MailFolders.Drafts, FolderType.Drafts, true),
                    RequestSpecialFolder(_client.Me.MailFolders.DeletedItems, FolderType.DeletedItems, true));
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
                        .Select(f => f.Create()));

                    if (page.NextPageRequest != null)
                    {
                        request = page.NextPageRequest;
                    }
                    else
                    {
                        state.DeltaLink = page.GetDeltaLink();
                        store.Update(state);
                        store.AddRange(folders);
                        await store.SaveChangesAsync();

                        _folderChanges.OnNext(folders
                            .Select(f => DeltaEntity.Added(f.Id, f))
                            .ToList().AsEnumerable());
                        break;
                    }
                }
            }
            else
            {
                MG.IMailFolderDeltaCollectionPage page = new MG.MailFolderDeltaCollectionPage();
                page.InitializeNextPageRequest(_client, state.DeltaLink);

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

                var updatedFolders = await store.Folders
                    .Where(f => deltaFolders.Keys.Contains(f.Id))
                    .Select(f => store.Entry(f))
                    .ToDictionaryAsync(f => f.Entity.Id);
                var newFolders = deltaFolders.Values
                    .Where(f => !updatedFolders.ContainsKey(f.Id))
                    .Select(f => f.Create())
                    .ToList();
                foreach (var f in updatedFolders)
                {
                    f.Value.Update(deltaFolders[f.Key]);
                }

                state.DeltaLink = page.GetDeltaLink();
                store.Update(state);
                store.AddRange(newFolders);
                store.RemoveRange(removedFolders);
                await store.SaveChangesAsync();

                var changes = new List<DeltaEntity<MailFolder>>();
                changes.AddRange(newFolders.Select(f => DeltaEntity.Added(f.Id, f)));
                changes.AddRange(updatedFolders.Values.Select(f => DeltaEntity.Updated(f.Entity.Id, f.Entity)));
                changes.AddRange(removedFolders.Select(f => DeltaEntity.Removed<MailFolder>(f.Id)));
                if (changes.Count > 0 && _folderChanges.HasObservers)
                {
                    _folderChanges.OnNext(changes);
                }
            }
        }

        public Task SynchronizeMessagesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
