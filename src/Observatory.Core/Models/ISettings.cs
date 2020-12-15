using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Models
{
    public interface ISettings
    {
        Task Restore();
    }
}
