using Autofac.Features.Indexed;
using Observatory.Core.Providers.Fake;
using Observatory.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeIndexedProfileProviders : IIndex<string, IProfileProvider>
    {
        private readonly Dictionary<string, FakeProfileProvider> _providers;

        public DesignTimeIndexedProfileProviders(Dictionary<string, FakeProfileProvider> providers)
        {
            _providers = providers;
        }

        public IProfileProvider this[string key] => _providers[key];

        public bool TryGetValue(string key, out IProfileProvider value)
        {
            var result =  _providers.TryGetValue(key, out var provider);
            value = provider;
            return result;
        }
    }
}
