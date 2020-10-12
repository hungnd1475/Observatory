using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMailManagerViewModel
    {
        public List<DesignTimeProfileViewModel> Profiles { get; }
        public DesignTimeProfileViewModel SelectedProfile => Profiles[0];
        public DesignTimeMailFolderViewModel SelectedFolder => Profiles[0].MailBox.FavoriteFolders[0];
        public DesignTimeMainViewModel HostScreen { get; set; }

        public DesignTimeMailManagerViewModel() 
        {
            HostScreen = new DesignTimeMainViewModel()
            {
                SelectedMode = ViewModels.FunctionalityMode.Mail,
            };
            Profiles = new List<DesignTimeProfileViewModel>()
            {
                new DesignTimeProfileViewModel()
                {
                    DisplayName = "Outlook",
                    EmailAddress = "hungnd1475@outlook.com",
                    IsSelected = true,
                    MailBox = new DesignTimeMailBoxViewModel()
                    {
                        AllFolders = new List<DesignTimeMailFolderViewModel>()
                        {
                            new DesignTimeMailFolderViewModel() { Name = "Inbox", IsFavorite = true, IsSelected = true },
                            new DesignTimeMailFolderViewModel() { Name = "Drafts", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Sent", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Archive", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Deleted", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Junk", IsFavorite = false },
                            new DesignTimeMailFolderViewModel() { Name = "Newsletters", IsFavorite = false },
                        },
                    },
                },
                new DesignTimeProfileViewModel()
                {
                    DisplayName = "Gmail Work",
                    EmailAddress = "hungnd1475@gmail.com",
                    MailBox = new DesignTimeMailBoxViewModel()
                    {
                        AllFolders = new List<DesignTimeMailFolderViewModel>()
                        {
                            new DesignTimeMailFolderViewModel() { Name = "Inbox", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Drafts", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Sent Mail", IsFavorite = true },
                            new DesignTimeMailFolderViewModel() { Name = "Archive", IsFavorite = false },
                            new DesignTimeMailFolderViewModel() { Name = "Deleted", IsFavorite = false },
                            new DesignTimeMailFolderViewModel() { Name = "Trash", IsFavorite = false },
                        }
                    }
                }
            };
        }
    }
}
