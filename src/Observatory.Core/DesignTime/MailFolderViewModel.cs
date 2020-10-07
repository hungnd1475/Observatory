using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class MailFolderViewModel
    {
        public string Name { get; set; }
        public bool IsFavorite { get; set; }
        public List<MailFolderViewModel> ChildFolders { get; set; }
        public bool IsSelected { get; set; }
    }
}
