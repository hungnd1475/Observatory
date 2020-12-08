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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Observatory.UI.Views
{
    public sealed partial class ProviderItem : UserControl
    {
        public static DependencyProperty ProviderProperty { get; } =
            DependencyProperty.Register(nameof(Provider), typeof(IProfileProvider), typeof(ProviderItem), new PropertyMetadata(null));

        public IProfileProvider Provider
        {
            get { return (IProfileProvider)GetValue(ProviderProperty); }
            set { SetValue(ProviderProperty, value); }
        }

        public ProviderItem()
        {
            this.InitializeComponent();
        }

        public async void LogoLoaded(object sender, RoutedEventArgs e)
        {
            using var iconStream = Provider.ReadIcon();
            var image = sender as Image;
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(iconStream.AsRandomAccessStream());
            image.Source = bitmap;
        }
    }
}
