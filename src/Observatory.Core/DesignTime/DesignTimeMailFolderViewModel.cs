using DynamicData;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Providers.Fake;
using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMailFolderViewModel : MailFolderViewModel
    {
        public DesignTimeMailFolderViewModel()
            : base(new Node<MailFolder, string>(
                new MailFolder()
                {
                    Id = "folder-01",
                    Name = "Inbox",
                    IsFavorite = true,
                    Type = FolderType.Inbox,
                }, "folder-01"), new FakeProfileDataQueryFactory())
        {
            this.RestoreAsync()
                .ContinueWith(_ => { });
        }
    }
}
