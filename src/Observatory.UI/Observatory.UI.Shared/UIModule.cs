using Autofac;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Views.Calendar;
using Observatory.UI.Views.Mail;
using ReactiveUI;

namespace Observatory.UI
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
