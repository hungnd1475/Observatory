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

        public IObservable<IEnumerable<DeltaEntity<MessageSummary>>> MessageSummaryChanges { get; } =
            Observable.Empty<IEnumerable<DeltaEntity<MessageSummary>>>();

        public IObservable<IEnumerable<DeltaEntity<MessageDetail>>> MessageDetailChanges { get; } =
            Observable.Empty<IEnumerable<DeltaEntity<MessageDetail>>>();

        public Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SynchronizeMessagesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
