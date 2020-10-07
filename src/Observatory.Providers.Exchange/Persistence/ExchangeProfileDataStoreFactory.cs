using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Providers.Exchange.Persistence
{
    public class ExchangeProfileDataStoreFactory : IProfileDataQueryFactory
    {
        private readonly string _path;

        public ExchangeProfileDataStoreFactory(string path)
        {
            _path = path;
        }

        public ExchangeProfileDataStore Connect()
        {
            return new ExchangeProfileDataStore(_path);
        }

        IProfileDataQuery IProfileDataQueryFactory.Connect()
        {
            return Connect();
        }
    }
}
