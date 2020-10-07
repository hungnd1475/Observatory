using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class ProfileViewModel
    {
        public string EmailAddress { get; set; }
        public string DisplayName { get; set; }
        public MailBoxViewModel MailBox { get; set; }
        public bool IsSelected { get; set; }
    }
}
