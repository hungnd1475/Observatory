﻿using Autofac;
using Observatory.Core.Persistence;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
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
                .AsSelf().SingleInstance();
            builder.Register(c => new ProfileRegistrationService(_profileRegistryPath))
                .AsSelf().SingleInstance();
            builder.RegisterType<MailManagerViewModel>()
                .AsSelf().SingleInstance();
            builder.RegisterType<CalendarViewModel>()
                .AsSelf().SingleInstance();
            builder.RegisterType<MainViewModel>()
                .AsSelf().SingleInstance();
        }
    }
}
