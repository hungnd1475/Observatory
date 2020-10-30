using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Providers.Exchange.Models
{
    public class MessageSynchronizationState
    {
        public string FolderId { get; set; }
        public string NextLink { get; set; }
        public string DeltaLink { get; set; }
        public DateTimeOffset? TimeLastSync { get; set; }
    }
}
