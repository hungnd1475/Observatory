using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMessageSummaryViewModel : MessageSummaryViewModel
    {
        public DesignTimeMessageSummaryViewModel(MessageSummary state)
            : base(state)
        {
        }
    }
}
