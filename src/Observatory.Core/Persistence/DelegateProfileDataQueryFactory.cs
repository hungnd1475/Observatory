using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence
{
    public class DelegateProfileDataQueryFactory : IProfileDataQueryFactory
    {
        private readonly string _path;
        private readonly Func<string, IProfileDataQuery> _factory;

        public DelegateProfileDataQueryFactory(string path, Func<string, IProfileDataQuery> factory)
        {
            _path = path;
            _factory = factory;
        }

        public virtual IProfileDataQuery Connect()
        {
            return _factory.Invoke(_path);
        }
    }
}
