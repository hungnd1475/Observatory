using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Models
{
    public class ProfileRegister
    {
        public string Id { get; set; }
        public string EmailAddress { get; set; }
        public string DataFilePath { get; set; }
        public string ProviderId { get; set; }
    }
}
