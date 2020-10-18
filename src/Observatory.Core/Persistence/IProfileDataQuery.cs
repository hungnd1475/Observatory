using Observatory.Core.Models;
using Observatory.Core.Persistence.Specifications;
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
        /// Gets an instance of <see cref="ISpecificationQueryable{T}"/> that queries <see cref="MailFolder"/>.
        /// </summary>
        ISpecificationQueryable<MailFolder> Folders { get; }

        /// <summary>
        /// Gets an instance of <see cref="ISpecificationQueryable{T}"/> that queries <see cref="MessageSummary"/>.
        /// </summary>
        ISpecificationQueryable<MessageSummary> MessageSummaries { get; }

        /// <summary>
        /// Gets an instance of <see cref="ISpecificationQueryable{T}"/> that queries <see cref="MessageDetail"/>.
        /// </summary>
        ISpecificationQueryable<MessageDetail> MessageDetails { get; }
    }
}
