using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Models
{
    public class MessageDetail
    {
        public string Id { get; set; }
        public string Body { get; set; }
        public ContentType? BodyType { get; set; }
    }
}
