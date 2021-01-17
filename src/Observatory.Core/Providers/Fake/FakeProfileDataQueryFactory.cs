using Observatory.Core.Models;
using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Providers.Fake
{
    public class FakeProfileDataQueryFactory : IProfileDataQueryFactory
    {
        private readonly FakeProfileDataQuery _query;

        public FakeProfileDataQueryFactory()
        {
            _query = new FakeProfileDataQuery();
        }

        public FakeProfileDataQueryFactory(IReadOnlyList<MailFolder> folders,
            IReadOnlyList<MessageSummary> messageSummaries,
            IReadOnlyList<MessageDetail> messageDetails)
        {
            _query = new FakeProfileDataQuery(folders, messageSummaries, messageDetails);
        }

        public FakeProfileDataQuery Connect()
        {
            return _query;
        }

        IProfileDataQuery IProfileDataQueryFactory.Connect() => Connect();
    }
}
