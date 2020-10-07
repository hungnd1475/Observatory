using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Models
{
    public class MailFolder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public FolderType Type { get; set; }
        public string ParentId { get; set; }
        public bool IsFavorite { get; set; }
    }
}
