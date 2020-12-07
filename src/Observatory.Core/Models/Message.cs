using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Models
{
    public class Message
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
        public string Body { get; set; }
        public ContentType? BodyType { get; set; }

        public MessageSummary Summary() => new MessageSummary()
        {
            Id = Id,
            BodyPreview = BodyPreview,
            CcRecipients = CcRecipients,
            FolderId = FolderId,
            HasAttachments = HasAttachments,
            Importance = Importance,
            IsDraft = IsDraft,
            IsFlagged = IsFlagged,
            IsRead = IsRead,
            ReceivedDateTime = ReceivedDateTime,
            Sender = Sender,
            Subject = Subject,
            ThreadId = ThreadId,
            ThreadPosition = ThreadPosition,
            ToRecipients = ToRecipients,
        };

        public MessageDetail Detail() => new MessageDetail()
        {
            Id = Id,
            FolderId = FolderId,
            Body = Body,
            BodyType = BodyType,
        };
    }
}
