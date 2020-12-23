using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Core.Services.ChangeTracking;
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
        public IObservable<DeltaSet<MailFolder>> FolderChanges { get; } =
            Observable.Empty<DeltaSet<MailFolder>>();

        public IObservable<DeltaSet<Message>> MessageChanges { get; } =
            Observable.Empty<DeltaSet<Message>>();

        public Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SynchronizeMessagesAsync(string folderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IEntityUpdater<Message> UpdateMessage(string messageId)
        {
            throw new NotImplementedException();
        }
    }
}
