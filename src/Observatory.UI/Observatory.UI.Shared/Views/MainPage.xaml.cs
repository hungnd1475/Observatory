using Observatory.Core.ViewModels;
using Observatory.UI.Views.Mail;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Observatory.UI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IViewFor<MainViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(MainPage), null);

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = Locator.Current.GetService<MainViewModel>();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            var appTitlebar = ApplicationView.GetForCurrentView().TitleBar;
            appTitlebar.ButtonBackgroundColor = Colors.Transparent;
            appTitlebar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appTitlebar.ButtonForegroundColor = Colors.White;

            this.WhenActivated(disposables => 
            {
                ViewModel.ProviderSelection
                    .RegisterHandler(async interaction =>
                    {
                        var selector = new ProviderSelector() { Providers = interaction.Input };
                        await selector.ShowAsync();
                        interaction.SetOutput(selector.SelectedProvider);
                    })
                    .DisposeWith(disposables);
            });
        }

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
    }
}
