using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence
{
    public interface IProfileDataQueryFactory
    {
        IProfileDataQuery Connect();
    }
}
