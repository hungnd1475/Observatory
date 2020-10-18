using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence
{
    /// <summary>
    /// Represents a <see cref="IProfileDataQuery"/> that delegates the creation work to an actual factory method.
    /// </summary>
    public class DelegateProfileDataQueryFactory : IProfileDataQueryFactory
    {
        private readonly string _path;
        private readonly Func<string, IProfileDataQuery> _factory;

        /// <summary>
        /// Constructs an instance of <see cref="DelegateProfileDataQueryFactory"/>.
        /// </summary>
        /// <param name="path">The path to the data store.</param>
        /// <param name="factory">The actual factory method.</param>
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
