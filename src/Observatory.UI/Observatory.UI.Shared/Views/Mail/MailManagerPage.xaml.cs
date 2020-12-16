using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Extensions;
using Uno.Logging;
using Windows.ApplicationModel.Core;
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

        public static DependencyProperty TitleTextProperty { get; } =
            DependencyProperty.Register(nameof(TitleText), typeof(string), typeof(MailManagerPage), new PropertyMetadata("Mail"));

        public MailManagerViewModel ViewModel
        {
            get => (MailManagerViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public string TitleText
        {
            get { return (string)GetValue(TitleTextProperty); }
            set { SetValue(TitleTextProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MailManagerViewModel)value;
        }

        public UISettings Settings { get; }

        public MailManagerPage()
        {
            this.InitializeComponent();
            Settings = Locator.Current.GetService<UISettings>();

            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            App.ThemeListener.ThemeChanged += ThemeListener_ThemeChanged;

            ContentGridShadow.Receivers.Add(NavigationPaneRoot);
            TopBarGridShadow.Receivers.Add(NavigationView);
            SearchFolderTextBox.LostFocus += SearchFolderTextBox_LostFocus;

            this.WhenActivated(disposables => 
            {
                this.WhenAnyValue(x => x.ViewModel.SelectedProfile)
                    .DistinctUntilChanged()
                    .Select(x => x == null ? "Mail" : $"Mail - {x.DisplayName}")
                    .BindTo(this, x => x.TitleTextBlock.Text)
                    .DisposeWith(disposables);
            });
        }

        private void ThemeListener_ThemeChanged(Microsoft.Toolkit.Uwp.UI.Helpers.ThemeListener sender)
        {
            var key = sender.CurrentTheme == ApplicationTheme.Light
                ? "LightCommandBarOverflowPresenterStyle"
                : "DarkCommandBarOverflowPresenterStyle";
            MessageDetailCommandBar.CommandBarOverflowPresenterStyle = (Style)Resources[key];
        }

        private void SearchFolderTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchFolderTextBox.Text))
            {
                HideSearchFolderCommandBar();
            }
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            WindowTitleRegion.Height = new GridLength(sender.Height);
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

        public void ShowSearchFolderCommandBar()
        {
            DefaultFolderCommandBar.Visibility = Visibility.Collapsed;
            SearchFolderCommandBar.Visibility = Visibility.Visible;
            SearchFolderTextBox.Focus(FocusState.Keyboard);
        }

        public void HideSearchFolderCommandBar()
        {
            DefaultFolderCommandBar.Visibility = Visibility.Visible;
            SearchFolderCommandBar.Visibility = Visibility.Collapsed;
            SearchFolderTextBox.Text = string.Empty;
        }
    }
}
