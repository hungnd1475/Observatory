using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Providers.Fake;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMainViewModel : MainViewModel
    {
        public DesignTimeMainViewModel()
            : base(DesignTimeData.ProfileRegistrationService,
                  new ProfilePersistenceConfiguration(null, null), 
                  DesignTimeData.ProfileProviders, 
                  new DesignTimeMailManagerViewModel(), 
                  new CalendarViewModel())
        {
        }
    }
}
