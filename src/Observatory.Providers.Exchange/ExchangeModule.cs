using Autofac;
using Observatory.Core.Services;
using Observatory.Providers.Exchange.Persistence;
using Observatory.Providers.Exchange.Services;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Observatory.Core.Tests")]

namespace Observatory.Providers.Exchange
{
    public class ExchangeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ExchangeAuthenticationService>()
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<ExchangeProfileDataStore>()
                .InstancePerDependency();
            builder.RegisterType<ExchangeProfileProvider>()
                .As<IProfileProvider>()
                .Keyed<IProfileProvider>(ExchangeProfileProvider.PROVIDER_ID)
                .SingleInstance();
        }
    }
}
