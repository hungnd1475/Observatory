using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMailFolderViewModel
    {
        public string Name { get; set; }
        public bool IsFavorite { get; set; }
        public List<DesignTimeMailFolderViewModel> ChildFolders { get; set; }
        public bool IsSelected { get; set; }
    }
}
