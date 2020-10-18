using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence
{
    /// <summary>
    /// Defines a factory that knows how to create an instance of <see cref="IProfileDataQuery"/>.
    /// </summary>
    public interface IProfileDataQueryFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="IProfileDataQuery"/>.
        /// </summary>
        /// <returns></returns>
        IProfileDataQuery Connect();
    }
}
