using Autofac;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Views;
using ReactiveUI;

namespace Observatory.UI.Shared
{
    public class UIModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MailManagerPage>().As<IViewFor<MailManagerViewModel>>();
            builder.RegisterType<CalendarManagerPage>().As<IViewFor<CalendarViewModel>>();
        }
    }
}
