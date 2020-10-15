using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Observatory.UI.Shared.Converters
{
    public class Converters
    {
        public static Visibility EqualityToVisibility(object x, object y)
        {
            return Equals(x, y) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static bool BoolNegation(bool x)
        {
            return !x;
        }

        public static Visibility BoolToVisibility(bool x)
        {
            return x ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility BoolNegationToVisibility(bool x)
        {
            return x ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
