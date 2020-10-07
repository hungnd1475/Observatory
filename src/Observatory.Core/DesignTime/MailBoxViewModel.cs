using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class MailBoxViewModel
    {
        public List<MailFolderViewModel> AllFolders { get; set; }
        public List<MailFolderViewModel> FavoriteFolders => AllFolders.Where(f => f.IsFavorite).ToList();
    }
}
