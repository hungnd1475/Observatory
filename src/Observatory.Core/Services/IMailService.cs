using DynamicData;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Observatory.Core.Services
{
    /// <summary>
    /// Defines a service to interact with a mail server.
    /// </summary>
    public interface IMailService 
    {
        /// <summary>
        /// Gets an observable of changes to the mail folders.
        /// </summary>
        IObservable<IEnumerable<DeltaEntity<MailFolder>>> FolderChanges { get; }

        /// <summary>
        /// Gets an observable of changes to the message summaries.
        /// </summary>
        IObservable<IEnumerable<DeltaEntity<MessageSummary>>> MessageSummaryChanges { get; }

        /// <summary>
        /// Gets an observable of changes to the message details.
        /// </summary>
        IObservable<IEnumerable<DeltaEntity<MessageDetail>>> MessageDetailChanges { get; }

        /// <summary>
        /// Synchronizes the mail folders. All changes will be published to <see cref="FolderChanges"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes the messages. All changes with be published to <see cref="MessageSummaryChanges"/> and <see cref="MessageDetailChanges"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SynchronizeMessagesAsync(CancellationToken cancellationToken = default);
    }
}
