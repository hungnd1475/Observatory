using Observatory.Core.Services;
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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Observatory.UI.Views.Mail
{
    public sealed partial class ProviderSelector : ContentDialog
    {
        public static DependencyProperty ProvidersProperty { get; } =
            DependencyProperty.Register(nameof(Providers), typeof(IEnumerable<IProfileProvider>), typeof(ProviderSelector), new PropertyMetadata(null));

        public IEnumerable<IProfileProvider> Providers
        {
            get { return (IEnumerable<IProfileProvider>)GetValue(ProvidersProperty); }
            set { SetValue(ProvidersProperty, value); }
        }

        public IProfileProvider SelectedProvider { get; private set; }

        public ProviderSelector()
        {
            this.InitializeComponent();
        }

        public void SelectProvider(object sender, ItemClickEventArgs e)
        {
            SelectedProvider = (IProfileProvider)e.ClickedItem;
            Hide();
        }
    }
}
