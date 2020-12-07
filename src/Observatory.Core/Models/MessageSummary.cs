using Observatory.Core.Virtualization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Models
{
    public class MessageSummary : IVirtualizableSource<string>
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public Recipient Sender { get; set; }
        public DateTimeOffset? ReceivedDateTime { get; set; }
        public bool? IsRead { get; set; }
        public Importance? Importance { get; set; }
        public bool? HasAttachments { get; set; }
        public List<Recipient> CcRecipients { get; set; }
        public List<Recipient> ToRecipients { get; set; }
        public string ThreadId { get; set; }
        public int? ThreadPosition { get; set; }
        public bool? IsDraft { get; set; }
        public string FolderId { get; set; }
        public string BodyPreview { get; set; }
        public bool? IsFlagged { get; set; }
    }
}
