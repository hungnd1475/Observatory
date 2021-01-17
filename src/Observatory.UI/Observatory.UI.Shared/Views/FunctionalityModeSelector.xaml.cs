using Observatory.Core.ViewModels;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Observatory.UI.Views
{
    public sealed partial class FunctionalityModeSelector : UserControl
    {
        public static DependencyProperty IsExpandedProperty { get; } =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(FunctionalityModeSelector), new PropertyMetadata(false));

        public static DependencyProperty MainViewModelProperty { get; } =
            DependencyProperty.Register(nameof(MainViewModel), typeof(MainViewModel), typeof(FunctionalityModeSelector), new PropertyMetadata(null));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public MainViewModel MainViewModel
        {
            get { return (MainViewModel)GetValue(MainViewModelProperty); }
            set { SetValue(MainViewModelProperty, value); }
        }

        public FunctionalityModeSelector()
        {
            this.InitializeComponent();
            MainViewModel = Locator.Current.GetService<MainViewModel>();
        }
    }
}
