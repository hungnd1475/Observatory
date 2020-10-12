using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMailBoxViewModel
    {
        public List<DesignTimeMailFolderViewModel> AllFolders { get; set; }
        public List<DesignTimeMailFolderViewModel> FavoriteFolders => AllFolders.Where(f => f.IsFavorite).ToList();
    }
}
