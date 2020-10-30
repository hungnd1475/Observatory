using Observatory.Core.Models;
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
    public abstract class ProfileViewModelBase : ReactiveObject, IDisposable
    {
        protected readonly ProfileRegister _register;

        public string EmailAddress => _register.EmailAddress;
        [Reactive] public string DisplayName { get; set; }
        
        public abstract MailBoxViewModel MailBox { get; }
        public abstract CalendarViewModel Calendar { get; }

        [Reactive] public bool IsAuthenticated { get; set; }

        public abstract ReactiveCommand<Unit, Unit> AuthenticateCommand { get; }
        public abstract ReactiveCommand<string, Unit> UpdateNameCommand { get; }
        public abstract ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public ProfileViewModelBase(ProfileRegister register)
        {
            _register = register;
        }

        public virtual void Dispose()
        {
            MailBox.Dispose();
        }
    }
}
