using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Providers.Fake
{
    public class FakeProfileDataQuery : IProfileDataQuery
    {
        private readonly IReadOnlyList<MailFolder> _folders;
        private readonly IReadOnlyList<MessageSummary> _messageSummaries;
        private readonly IReadOnlyList<MessageDetail> _messageDetails;

        public ISpecificationQueryable<MailFolder> Folders => new InMemorySpecificationQueryable<MailFolder>(_folders);

        public ISpecificationQueryable<MessageSummary> MessageSummaries => new InMemorySpecificationQueryable<MessageSummary>(_messageSummaries);

        public ISpecificationQueryable<MessageDetail> MessageDetails => new InMemorySpecificationQueryable<MessageDetail>(_messageDetails);

        public FakeProfileDataQuery()
        {
            _folders = new List<MailFolder>()
            {
                new MailFolder() { Id = "folder-01", Name = "Inbox", IsFavorite = true, Type = FolderType.Inbox },
                new MailFolder() { Id = "folder-02", Name = "Drafts", IsFavorite = true, Type = FolderType.Drafts },
                new MailFolder() { Id = "folder-03", Name = "Sent", IsFavorite = true, Type = FolderType.SentItems },
                new MailFolder() { Id = "folder-04", Name = "Archive", IsFavorite = true, Type = FolderType.Archive },
                new MailFolder() { Id = "folder-05", Name = "Deleted", IsFavorite = true, Type = FolderType.DeletedItems },
                new MailFolder() { Id = "folder-06", Name = "Junk", IsFavorite = false, Type = FolderType.None },
                new MailFolder() { Id = "folder-07", Name = "Newsletter", IsFavorite = false, Type = FolderType.None },
            }.AsReadOnly();
            _messageSummaries = new List<MessageSummary>()
            {
                new MessageSummary()
                {
                    Id = "message-01",
                    Subject = "Look out for falling prices this week...",
                    BodyPreview = "Doorbuster deals will be revealed, starting tomorrow and Tuesday at 9AM and 12PM ET.",
                    Sender = new Recipient() { DisplayName = "Lenovo US | Semi-Annual Sale", EmailAddress = "Lenovo@ecomms.lenovo.com" },
                    ReceivedDateTime = new DateTimeOffset(2020, 10, 12, 3, 2, 0, TimeSpan.FromHours(10)),
                    IsRead = true,
                    Importance = Importance.Normal,
                    HasAttachments = false,
                    IsFlagged = false,
                    IsDraft = false,
                    ThreadId = "thread-01",
                    ThreadPosition = 0,
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient() { DisplayName = "Hung D Nguyen", EmailAddress = "hungnd1475@outlook.com" },
                    },
                    CcRecipients = new List<Recipient>(),
                    FolderId = "folder-01",
                },
                new MessageSummary()
                {
                    Id = "message-02",
                    Subject = "The One and Only Factor That Will Make You a Senior Developer | Travis Rodgers in The Startup",
                    BodyPreview = "Stories for Hung D. Nguyen. Why I think GCP is better",
                    Sender = new Recipient() { DisplayName = "Medium Daily Digest", EmailAddress = "noreply@medium.com" },
                    ReceivedDateTime = new DateTimeOffset(2020, 10, 12, 9, 10, 0, TimeSpan.FromHours(10)),
                    IsRead = false,
                    Importance = Importance.Normal,
                    HasAttachments = true,
                    IsFlagged = true,
                    IsDraft = false,
                    ThreadId = "thread-02",
                    ThreadPosition = 0,
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient() { DisplayName = "Hung D Nguyen", EmailAddress = "hungnd1475@outlook.com" },
                    },
                    CcRecipients = new List<Recipient>(),
                    FolderId = "folder-01",
                },
                new MessageSummary()
                {
                    Id = "message-03",
                    Subject = "Continue your journey by setting up your PC for Insider builds",
                    BodyPreview = "Experience new Windows 10 preview features!",
                    Sender = new Recipient() { DisplayName = "Windows Insider Program", EmailAddress = "windowsinsiderprogram@e-mail.microsoft.com" },
                    ReceivedDateTime = new DateTimeOffset(2020, 10, 1, 3, 2, 0, TimeSpan.FromHours(10)),
                    IsRead = false,
                    Importance = Importance.High,
                    HasAttachments = true,
                    IsFlagged = false,
                    IsDraft = false,
                    ThreadId = "thread-03",
                    ThreadPosition = 0,
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient() { DisplayName = "Hung D Nguyen", EmailAddress = "hungnd1475@outlook.com" },
                    },
                    CcRecipients = new List<Recipient>(),
                    FolderId = "folder-01",
                },
                new MessageSummary()
                {
                    Id = "message-04",
                    Subject = "Practice Coding with Tree Coordinates",
                    BodyPreview = "Hi Hung, Improve your skills with this challenge recommended for you:",
                    Sender = new Recipient()
                    {
                        DisplayName = "no-reply=hackerrankmail.com@postmaster.hackerrankmail.com",
                        EmailAddress = "no-reply=hackerrankmail.com@postmaster.hackerrankmail.com"
                    },
                    ReceivedDateTime = new DateTimeOffset(2020, 9, 27, 11, 0, 0, TimeSpan.FromHours(10)),
                    IsRead = true,
                    Importance = Importance.High,
                    HasAttachments = true,
                    IsFlagged = true,
                    IsDraft = false,
                    ThreadId = "thread-04",
                    ThreadPosition = 0,
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient() { DisplayName = "Hung D Nguyen", EmailAddress = "hungnd1475@outlook.com" },
                    },
                    CcRecipients = new List<Recipient>(),
                    FolderId = "folder-01",
                },
            }.AsReadOnly();
            _messageDetails = new List<MessageDetail>();
        }

        public FakeProfileDataQuery(IReadOnlyList<MailFolder> folders,
            IReadOnlyList<MessageSummary> messageSummaries,
            IReadOnlyList<MessageDetail> messageDetails)
        {
            _folders = folders;
            _messageSummaries = messageSummaries;
            _messageDetails = messageDetails;
        }

        public void Dispose()
        {
        }
    }
}
