using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public class MessageVirtualizingSource : IVirtualizingSource<MessageSummary>
    {
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly string _folderId;

        public MessageVirtualizingSource(
            IProfileDataQueryFactory queryFactory,
            string folderId)
        {
            _queryFactory = queryFactory;
            _folderId = folderId;
        }

        public MessageSummary[] GetItems(int startIndex, int maxNumberOfItems)
        {
            using var query = _queryFactory.Connect();
            var specification = Specification.Relay<MessageSummary>(
                q => q.Where(m => m.FolderId == _folderId)
                    .OrderByDescending(m => m.ReceivedDateTime)
                    .Skip(startIndex)
                    .Take(maxNumberOfItems));
            return query.MessageSummaries.ToArray(specification);
        }

        public int GetTotalCount()
        {
            using var query = _queryFactory.Connect();
            return query.MessageSummaries.Count(m => m.FolderId == _folderId);
        }

        public int IndexOf(MessageSummary entity)
        {
            using var query = _queryFactory.Connect();
            var specification = Specification.Relay<MessageSummary>(
                q => q.Where(m => m.FolderId == _folderId && m.ReceivedDateTime > entity.ReceivedDateTime));
            return query.MessageSummaries.Count(specification);
        }
    }
}
