using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Providers.Fake.Persistence;
using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Providers.Fake
{
    public class FakeProfileViewModel : ProfileViewModelBase
    {
        public override MailBoxViewModel MailBox { get; }

        public override CalendarViewModel Calendar => throw new NotImplementedException();

        public override ReactiveCommand<Unit, Unit> AuthenticateCommand => throw new NotImplementedException();

        public override ReactiveCommand<string, Unit> UpdateNameCommand => throw new NotImplementedException();

        public override ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public FakeProfileViewModel(ProfileRegister register, Profile state,
            FakeProfileDataQueryFactory queryFactory,
            FakeMailService mailService) 
            : base(register)
        {
            MailBox = new MailBoxViewModel(queryFactory, mailService);
            DisplayName = state.DisplayName;
        }

        public void Restore()
        {
            MailBox.Restore();
        }
    }
}
