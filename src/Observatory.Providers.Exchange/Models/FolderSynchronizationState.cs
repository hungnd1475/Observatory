using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Providers.Exchange.Models
{
    public class FolderSynchronizationState
    {
        public int Id { get; set; }
        public string DeltaLink { get; set; }
        public DateTimeOffset? TimeLastSync { get; set; }
    }
}
