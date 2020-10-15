using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Observatory.UI.Shared.Extentions
{
    public class ButtonEx : DependencyObject
    {
        public static readonly DependencyProperty HoverForegroundProperty =
            DependencyProperty.RegisterAttached("HoverForeground", typeof(Brush), typeof(ButtonEx), new PropertyMetadata(null));

        public static Brush GetHoverForeground(DependencyObject obj)
        {
            return (Brush)obj.GetValue(HoverForegroundProperty);
        }

        public static void SetHoverForeground(DependencyObject obj, Brush value)
        {
            obj.SetValue(HoverForegroundProperty, value);
        }
    }
}
