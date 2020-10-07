using Autofac;
using Observatory.Core.Services;
using Observatory.Providers.Exchange.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Providers.Exchange
{
    public class ExchangeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ExchangeAuthenticationService>()
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<ExchangeProfileProvider>()
                .As<IProfileProvider>()
                .Keyed<IProfileProvider>(ExchangeProfileProvider.PROVIDER_ID)
                .SingleInstance();
        }
    }
}
