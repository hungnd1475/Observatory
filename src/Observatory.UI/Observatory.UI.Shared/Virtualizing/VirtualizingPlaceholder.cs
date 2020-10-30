using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.UI.Virtualizing
{
    public class VirtualizingPlaceholder
    {
        public int Index { get; }

        public VirtualizingPlaceholder(int index) 
        {
            Index = index;
        }
    }
}
