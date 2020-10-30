using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Observatory.UI.Views.Mail
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MailManagerPage : Page, IViewFor<MailManagerViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MailManagerViewModel), typeof(MailManagerPage), null);

        public static DependencyProperty ProvidersFlyoutProperty { get; } =
            DependencyProperty.Register(nameof(ProvidersFlyout), typeof(MenuFlyout), typeof(MailManagerPage), null);

        public MailManagerViewModel ViewModel
        {
            get => (MailManagerViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MailManagerViewModel)value;
        }

        public MenuFlyout ProvidersFlyout
        {
            get => (MenuFlyout)GetValue(ProvidersFlyoutProperty);
            set => SetValue(ProvidersFlyoutProperty, value);
        }

        public MailManagerPage()
        {
            this.InitializeComponent();
            this.WhenActivated(disposables => 
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .Select(vm =>
                    {
                        var providers = vm.HostScreen.Providers;
                        var flyout = new MenuFlyout()
                        {
                            Placement = FlyoutPlacementMode.Full,
                            MenuFlyoutPresenterStyle = new Style(typeof(MenuFlyoutPresenter)),
                        };
                        flyout.MenuFlyoutPresenterStyle.Setters.AddRange(new Setter[]
                        {
                            new Setter(PaddingProperty, 0),
                            new Setter(MaxHeightProperty, 400),
                            new Setter(MinHeightProperty, 240),
                            new Setter(HeightProperty, providers.Count() * 40),
                        });
                        flyout.Items.AddRange(new MenuFlyoutItemBase[]
                        {
                            new MenuFlyoutItem()
                            {
                                Text = "Choose a provider",
                                FontSize = 14,
                                FontWeight = FontWeights.SemiBold,
                                IsHitTestVisible = false,
                            },
                            new MenuFlyoutSeparator(),
                        });
                        foreach (var p in providers)
                        {
                            flyout.Items.Add(new MenuFlyoutItem()
                            {
                                Height = 45,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                Text = p.DisplayName,
                                Command = vm.HostScreen.AddProfile,
                                CommandParameter = p,
                                Icon = new PathIcon()
                                {
                                    Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), p.IconGeometry),
                                },
                            });
                        }
                        return flyout;
                    })
                    .BindTo(this, x => x.ProvidersFlyout)
                    .DisposeWith(disposables);
            });
        }

        public void ToggleNavigationPane()
        {
            NavigationView.IsPaneOpen = !NavigationView.IsPaneOpen;
        }

        public void ToggleProfileListPane()
        {
            ProfileListSplitView.IsPaneOpen = !ProfileListSplitView.IsPaneOpen;
        }

        public void ToggleFolderListPane()
        {
            FolderListSplitView.IsPaneOpen = !FolderListSplitView.IsPaneOpen;
        }

        public void SelectFolder(MUXC.TreeView sender, MUXC.TreeViewItemInvokedEventArgs e)
        {
            ViewModel.SelectedFolder = e.InvokedItem as MailFolderViewModel;
            ToggleFolderListPane();
        }
    }
}
