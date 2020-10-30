using Observatory.Core.Models;
using Observatory.Core.Providers.Fake;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Providers.Fake
{
    public static class FakeData
    {
        public static readonly ProfileRegister[] ProfileRegisters;
        public static readonly FakeProfileProvider[] ProfileProviders;
        public static readonly FakeProfileRegistrationService ProfileRegistrationService;

        static FakeData()
        {
            ProfileRegisters = new ProfileRegister[]
            {
                new ProfileRegister()
                {
                    Id = "fake-profile-01",
                    EmailAddress = "hungnd1475@outlook.com",
                    DataFilePath = null,
                    ProviderId = "fake-provider-01",
                },
                new ProfileRegister()
                {
                    Id = "fake-profile-02",
                    EmailAddress = "hungnd1475@gmail.com",
                    DataFilePath = null,
                    ProviderId = "fake-provider-02",
                },
            };
            ProfileRegistrationService = new FakeProfileRegistrationService(ProfileRegisters);
            ProfileProviders = new FakeProfileProvider[]
            {
                new FakeProfileProvider(ProfileRegisters[0].ProviderId, ProfileRegisters[0].EmailAddress, "Outlook", 
                    new FakeProfileDataQueryFactory(), new FakeMailService()),
                new FakeProfileProvider(ProfileRegisters[1].ProviderId, ProfileRegisters[1].EmailAddress, "Gmail Work", 
                    new FakeProfileDataQueryFactory(
                        folders: new List<MailFolder>()
                        {
                            new MailFolder() { Id = "folder-01", Name = "Inbox", IsFavorite = true, Type = FolderType.Inbox },
                            new MailFolder() { Id = "folder-02", Name = "Drafts", IsFavorite = true, Type = FolderType.Drafts },
                            new MailFolder() { Id = "folder-03", Name = "Sent Mail", IsFavorite = true, Type = FolderType.SentItems },
                            new MailFolder() { Id = "folder-04", Name = "Archive", IsFavorite = false, Type = FolderType.Archive },
                            new MailFolder() { Id = "folder-05", Name = "Deleted", IsFavorite = false, Type = FolderType.DeletedItems },
                            new MailFolder() { Id = "folder-06", Name = "Trash", IsFavorite = false, Type = FolderType.None }
                        },
                        messageSummaries: new List<MessageSummary>(),
                        messageDetails: new List<MessageDetail>()), 
                    new FakeMailService()),
            };
        }
    }
}
