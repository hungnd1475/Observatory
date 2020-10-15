using Observatory.Core.Persistence;
using Observatory.Core.Providers.Fake;
using Observatory.Core.Services;
using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMailBoxViewModel : MailBoxViewModel
    {
        public DesignTimeMailBoxViewModel() 
            : base(new FakeProfileDataQueryFactory(), new FakeMailService())
        {
        }
    }
}
