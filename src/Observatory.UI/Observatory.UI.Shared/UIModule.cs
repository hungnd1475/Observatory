using Autofac;
using Observatory.Core.Persistence;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using Observatory.Core.ViewModels.Settings;
using Observatory.UI.Settings;
using Observatory.UI.Views.Calendar;
using Observatory.UI.Views.Mail;
using Observatory.UI.Views.Settings;
using ReactiveUI;

namespace Observatory.UI
{
    public class UIModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MailManagerPage>().As<IViewFor<MailManagerViewModel>>();
            builder.RegisterType<CalendarManagerPage>().As<IViewFor<CalendarViewModel>>();
            builder.RegisterType<SettingPage>().As<IViewFor<SettingsViewModel>>();

            builder.RegisterType<UWPSettingsStore>()
                .As<ISettingsStore>()
                .SingleInstance();
            builder.RegisterType<UISettings>()
                .AsSelf().SingleInstance();
        }
    }
}
