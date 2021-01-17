using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Services.Models
{
    public class UpdatableMessage
    {
        public bool? IsRead { get; set; }
        public bool? IsFlagged { get; set; }
    }
}
