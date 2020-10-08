using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Providers.Exchange.Persistence
{
    public class ExchangeProfileDataQueryFactory : IProfileDataQueryFactory
    {
        private readonly string _path;
        private readonly ExchangeProfileDataStore.Factory _storeFactory;

        public ExchangeProfileDataQueryFactory(string path,
            ExchangeProfileDataStore.Factory storeFactory)
        {
            _path = path;
            _storeFactory = storeFactory;
        }

        public IProfileDataQuery Connect()
        {
            return _storeFactory.Invoke(_path);
        }
    }
}
