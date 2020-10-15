using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Observatory.UI.Shared.Controls
{
    public class MessageSummaryCommandButton : Button
    {
        public static readonly DependencyProperty ActiveForegroundProperty =
            DependencyProperty.Register(nameof(ActiveForeground), typeof(Brush), typeof(MessageSummaryCommandButton), new PropertyMetadata(null));

        public Brush ActiveForeground
        {
            get { return (Brush)GetValue(ActiveForegroundProperty); }
            set { SetValue(ActiveForegroundProperty, value); }
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled(e);
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            e.Handled = true;
        }

        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            e.Handled = true;
        }
    }
}
