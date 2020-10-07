using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class MailManagerViewModel
    {
        public List<ProfileViewModel> Profiles { get; }
        public ProfileViewModel SelectedProfile => Profiles[0];
        public MailFolderViewModel SelectedFolder => Profiles[0].MailBox.FavoriteFolders[0];
        public MainViewModel Main { get; set; }

        public MailManagerViewModel() 
        {
            Main = new MainViewModel()
            {
                SelectedMode = ViewModels.FunctionalityMode.Mail,
            };
            Profiles = new List<ProfileViewModel>()
            {
                new ProfileViewModel()
                {
                    DisplayName = "Outlook",
                    EmailAddress = "hungnd1475@outlook.com",
                    IsSelected = true,
                    MailBox = new MailBoxViewModel()
                    {
                        AllFolders = new List<MailFolderViewModel>()
                        {
                            new MailFolderViewModel() { Name = "Inbox", IsFavorite = true, IsSelected = true },
                            new MailFolderViewModel() { Name = "Drafts", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Sent", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Archive", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Deleted", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Junk", IsFavorite = false },
                            new MailFolderViewModel() { Name = "Newsletters", IsFavorite = false },
                        },
                    },
                },
                new ProfileViewModel()
                {
                    DisplayName = "Gmail Work",
                    EmailAddress = "hungnd1475@gmail.com",
                    MailBox = new MailBoxViewModel()
                    {
                        AllFolders = new List<MailFolderViewModel>()
                        {
                            new MailFolderViewModel() { Name = "Inbox", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Drafts", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Sent Mail", IsFavorite = true },
                            new MailFolderViewModel() { Name = "Archive", IsFavorite = false },
                            new MailFolderViewModel() { Name = "Deleted", IsFavorite = false },
                            new MailFolderViewModel() { Name = "Trash", IsFavorite = false },
                        }
                    }
                }
            };
        }
    }
}
