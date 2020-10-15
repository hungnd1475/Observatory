using Autofac.Features.Indexed;
using Observatory.Core.Services;
using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMailManagerViewModel : MailManagerViewModel
    {
        public DesignTimeMailManagerViewModel() 
            : base(DesignTimeData.ProfileRegistrationService, 
                  DesignTimeData.IndexedProfileProviders)
        {
        }
    }
}
