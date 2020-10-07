using Observatory.Core.Models;
using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Application.ViewModels
{
    public class ProfileRegistrationViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public IEnumerable<IProfileProvider> Providers { get; }

        public ReactiveCommand<IProfileProvider, ProfileRegister> Register { get; }

        public ProfileRegistrationViewModel(IEnumerable<IProfileProvider> providers)
        {
            Providers = providers;
            Register = ReactiveCommand.CreateFromTask<IProfileProvider, ProfileRegister>(RegisterAsync);
        }

        private static Task<ProfileRegister> RegisterAsync(IProfileProvider provider)
        {
            return provider.AuthenticateAsync(null);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
