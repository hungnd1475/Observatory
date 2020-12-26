using DynamicData;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Services.ChangeTracking;
using Observatory.Core.Services.Models;
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
        IObservable<DeltaSet<MailFolder>> FolderChanges { get; }

        /// <summary>
        /// Gets an observable of changes to the message summaries.
        /// </summary>
        IObservable<DeltaSet<Message>> MessageChanges { get; }

        /// <summary>
        /// Synchronizes the mail folders. All changes will be published to <see cref="FolderChanges"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SynchronizeFoldersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes the messages for a given folder. All changes with be published to <see cref="MessageChanges"/>.
        /// </summary>
        /// <param name="folderId">The folder id for which messages are to be synchronized.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SynchronizeMessagesAsync(string folderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an <see cref="IEntityUpdater{TEntity}"/> to update a message given its id.
        /// </summary>
        /// <param name="messageIds">The message id.</param>
        /// <returns>An instance of <see cref="IEntityUpdater{TEntity}"/>.</returns>
        IEntityUpdater<UpdatableMessage> UpdateMessage(params string[] messageIds);
    }
}
