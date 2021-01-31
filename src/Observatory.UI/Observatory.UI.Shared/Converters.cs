using Observatory.Core.ViewModels;
using Observatory.UI.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Observatory.UI
{
    public static class Converters
    {
        public static Visibility EqualityToVisibility(object x, object y)
        {
            return Equals(x, y) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static bool Equality(object x, object y)
        {
            return Equals(x, y);
        }

        public static Visibility InequalityToVisibility(object x, object y)
        {
            return !Equals(x, y) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility ObjectNullToVisibility(object x)
        {
            return x == null ? Visibility.Collapsed : Visibility.Visible;
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

        public static FluentSystemIconSymbol FunctionalityModeToIcon(FunctionalityMode mode)
        {
            return mode switch
            {
                FunctionalityMode.Mail => FluentSystemIconSymbol.Mail,
                FunctionalityMode.Calendar => FluentSystemIconSymbol.Calendar,
                FunctionalityMode.Setup => FluentSystemIconSymbol.Setting,
                _ => throw new NotSupportedException(),
            };
        }

        public static Visibility IntegerVisibility(int x)
        {
            return x == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public static Visibility CollectionEmptyToVisibility(object value)
        {
            if (value is IEnumerable collection)
            {
                foreach (var _ in collection)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            throw new ArgumentException("Argument is not a collection.");
        }

        public static GridLength PixelsToGridLength(double pixels)
        {
            return new GridLength(pixels);
        }

        public static double GridLengthToPixels(GridLength gridLength)
        {
            return gridLength.Value;
        }

        public static FontFamily StringToFontFamily(string familyName)
        {
            return new FontFamily(familyName);
        }
    }
}
