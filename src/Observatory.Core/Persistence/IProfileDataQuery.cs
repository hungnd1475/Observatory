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

    public static class ProfileDataQueryExtensions
    {
        private static readonly Dictionary<Type, Func<IProfileDataQuery, object>> QUERY_TYPE_MAPPER =
            new Dictionary<Type, Func<IProfileDataQuery, object>>()
            {
                { typeof(MailFolder), q => q.Folders },
                { typeof(MessageSummary), q => q.MessageSummaries },
                { typeof(MessageDetail), q => q.MessageDetails }
            };

        public static ISpecificationQueryable<T> ForType<T>(this IProfileDataQuery query)
        {
            return (ISpecificationQueryable<T>)QUERY_TYPE_MAPPER[typeof(T)](query);
        }
    }
}
