using Observatory.Core.Interactivity;
using Observatory.Core.ViewModels;
using Observatory.UI.Views.Mail;
using ReactiveUI;
using Splat;
using System;
using System.Reactive.Disposables;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Observatory.UI.Views
{
    /// <summary>
    /// The main page that is responsible for routing each functionality view.
    /// </summary>
    public sealed partial class MainPage : Page, IViewFor<MainViewModel>
    {
        public static ProviderSelector ProviderSelector { get; } = new ProviderSelector();

        public static MailFolderSelector MailFolderSelector { get; } = new MailFolderSelector();

        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(MainPage), null);

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainViewModel)value;
        }

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = Locator.Current.GetService<MainViewModel>();

#if NETFX_CORE
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var appTitlebar = ApplicationView.GetForCurrentView().TitleBar;
            appTitlebar.ButtonBackgroundColor = Colors.Transparent;
            appTitlebar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appTitlebar.ButtonForegroundColor = Colors.White;
            appTitlebar.ButtonHoverForegroundColor = Colors.White;
            appTitlebar.ButtonPressedForegroundColor = Colors.White;
#endif

            this.WhenActivated(disposables =>
            {
                Interactions.ProviderSelection
                    .RegisterHandler(async context =>
                    {
                        ProviderSelector.Providers = context.Input;
                        await ProviderSelector.ShowAsync();
                        context.SetOutput(ProviderSelector.SelectedProvider);
                    })
                    .DisposeWith(disposables);

                Interactions.MailFolderSelection
                    .RegisterHandler(async context =>
                    {
                        MailFolderSelector.ViewModel = context.Input;
                        await MailFolderSelector.ShowAsync();
                        context.SetOutput(MailFolderSelector.Result);
                    })
                    .DisposeWith(disposables);
            });
        }
    }
}
