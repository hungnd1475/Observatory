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
        /// Gets an <see cref="IEntityUpdater{TEntity}"/> to update one or many messages.
        /// </summary>
        /// <param name="messageIds">The message id.</param>
        /// <returns>An instance of <see cref="IEntityUpdater{TEntity}"/>.</returns>
        IEntityUpdater<UpdatableMessage> UpdateMessage(IReadOnlyList<string> messageIds);

        /// <summary>
        /// Moves one or many messages to another folder.
        /// </summary>
        /// <param name="messageIds">The ids of messages to move.</param>
        /// <param name="destinationFolderId">The id of the destination folder.</param>
        /// <returns></returns>
        Task MoveMessage(IReadOnlyList<string> messageIds, string destinationFolderId);

        /// <summary>
        /// Moves one or many messages to the folder of a given <see cref="FolderType"/>.
        /// Only <see cref="FolderType.Inbox"/>, <see cref="FolderType.DeletedItems"/>,
        /// <see cref="FolderType.Archive"/> are supported.
        /// </summary>
        /// <param name="messageIds">The ids of messages to move.</param>
        /// <param name="destinationFolderType">The type of the destination folder.</param>
        /// <returns></returns>
        Task MoveMessage(IReadOnlyList<string> messageIds, FolderType destinationFolderType);
    }
}
