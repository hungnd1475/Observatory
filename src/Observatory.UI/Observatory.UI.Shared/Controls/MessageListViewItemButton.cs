using Splat;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Observatory.UI.Controls
{
    public class MessageListViewItemButton : Button, IEnableLogger
    {
        public static readonly DependencyProperty ActiveForegroundProperty =
            DependencyProperty.Register(nameof(ActiveForeground), typeof(Brush), typeof(MessageListViewItemButton), new PropertyMetadata(null));

        public Brush ActiveForeground
        {
            get { return (Brush)GetValue(ActiveForegroundProperty); }
            set { SetValue(ActiveForegroundProperty, value); }
        }

        public MessageListViewItemButton()
        {
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            this.Log().Debug("Pointer exited.");
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled(e);
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            //e.Handled = true;
        }

        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            //e.Handled = true;
        }
    }
}
