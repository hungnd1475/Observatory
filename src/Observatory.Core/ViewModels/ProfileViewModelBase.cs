using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Observatory.Core.ViewModels
{
    public abstract class ProfileViewModelBase : ReactiveObject
    {
        public string EmailAddress { get; }
        [Reactive] public string DisplayName { get; set; }
        
        public abstract MailBoxViewModel MailBox { get; }
        public abstract CalendarViewModel Calendar { get; }

        [Reactive] public bool IsAuthenticated { get; set; }

        public abstract ReactiveCommand<Unit, Unit> AuthenticateCommand { get; }
        public abstract ReactiveCommand<string, Unit> UpdateNameCommand { get; }
        public abstract ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public ProfileViewModelBase(string emailAddress)
        {
            EmailAddress = emailAddress;
        }
    }
}
