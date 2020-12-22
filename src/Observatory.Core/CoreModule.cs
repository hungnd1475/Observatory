﻿using Autofac;
using Observatory.Core.Models.Settings;
using Observatory.Core.Persistence;
using Observatory.Core.Providers.Fake;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using Observatory.Core.ViewModels.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core
{
    public class CoreModule : Module
    {
        private readonly string _profileRegistryPath;
        private readonly string _profileDataDirectory;

        public CoreModule(string profileRegistryPath,
            string profileDataDirectory)
        {
            _profileRegistryPath = profileRegistryPath;
            _profileDataDirectory = profileDataDirectory;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new ProfilePersistenceConfiguration(_profileRegistryPath, _profileDataDirectory))
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new PersistentProfileRegistrationService(_profileRegistryPath))
                .As<IProfileRegistrationService>()
                .SingleInstance();

            //builder.Register(c => DesignTimeData.ProfileRegistrationService)
            //    .As<IProfileRegistrationService>().SingleInstance();
            //builder.Register(c => DesignTimeData.ProfileProviders[0])
            //    .As<IProfileProvider>()
            //    .Keyed<IProfileProvider>(DesignTimeData.ProfileRegisters[0].ProviderId)
            //    .SingleInstance();
            //builder.Register(c => DesignTimeData.ProfileProviders[1])
            //    .As<IProfileProvider>()
            //    .Keyed<IProfileProvider>(DesignTimeData.ProfileRegisters[1].ProviderId)
            //    .SingleInstance();

            builder.RegisterType<MainViewModel>()
                .AsSelf().SingleInstance();
            builder.RegisterType<MailManagerViewModel>()
                .Keyed<IFunctionalityViewModel>(FunctionalityMode.Mail)
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();
            builder.RegisterType<CalendarViewModel>()
                .Keyed<IFunctionalityViewModel>(FunctionalityMode.Calendar)
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();
            builder.RegisterType<SettingsViewModel>()
                .Keyed<IFunctionalityViewModel>(FunctionalityMode.Setup)
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<MailSettings>()
                .AsSelf().SingleInstance();
        }
    }
}
