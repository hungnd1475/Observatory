using Observatory.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Observatory.UI
{
    public static class Converters
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

        public static Symbol FunctionalityModeToIcon(FunctionalityMode mode)
        {
            return mode switch
            {
                FunctionalityMode.Mail => Symbol.Mail,
                FunctionalityMode.Calendar => Symbol.Calendar,
                _ => throw new NotSupportedException(),
            };
        }
    }
}
