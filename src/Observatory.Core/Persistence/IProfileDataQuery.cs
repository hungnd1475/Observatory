using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence
{
    /// <summary>
    /// Defines a contract for querying a profile data store.
    /// </summary>
    public interface IProfileDataQuery : IDisposable
    {
        /// <summary>
        /// Asynchronously retrieve all folders with an optional specification.
        /// </summary>
        /// <param name="specificator">The specification to retrieve the folders, leave null if all folders are to be retrieved.</param>
        /// <returns></returns>
        Task<IReadOnlyList<MailFolder>> GetFoldersAsync(Func<IQueryable<MailFolder>, IQueryable<MailFolder>> specificator = null);

        /// <summary>
        /// Asynchronously retrieve all messages with an optional specification.
        /// </summary>
        /// <param name="specificator">The specification to retrieve the messages, leave null if all messages are to be retrieved.</param>
        /// <returns></returns>
        Task<IReadOnlyList<MessageSummary>> GetMessageSummariesAsync(Func<IQueryable<MessageSummary>, IQueryable<MessageSummary>> specificator = null);

        /// <summary>
        /// Asynchronously retrieve a single message detail by its id. 
        /// </summary>
        /// <param name="id">The id of the message whose detail is to be retrieved.</param>
        /// <returns></returns>
        Task<MessageDetail> GetMessageDetailAsync(string id);
    }
}
