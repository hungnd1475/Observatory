using Observatory.Core.Models;
using Observatory.Core.Services;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Observatory.Core.Providers.Fake
{
    public class FakeMailService : IMailService
    {
        public IObservable<IEnumerable<DeltaEntity<MailFolder>>> FolderChanges { get; } =
            Observable.Empty<IEnumerable<DeltaEntity<MailFolder>>>();

        public IObservable<(string FolderId, IEnumerable<DeltaEntity<Message>> Changes)> MessageChanges { get; } =
            Observable.Empty<(string FolderId, IEnumerable<DeltaEntity<Message>> Changes)>();

        public Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SynchronizeMessagesAsync(string folderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IEntityUpdater<Message> UpdateMessage(string folderId, string messageId)
        {
            throw new NotImplementedException();
        }
    }
}
