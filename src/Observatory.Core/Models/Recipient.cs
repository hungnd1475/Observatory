using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Observatory.Core.Models
{
    public class Recipient
    {
        public string EmailAddress { get; set; }
        public string DisplayName { get; set; }
    }
}
