using Observatory.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Shared.Converters
{
    public class FunctionalityModeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is FunctionalityMode mode)
            {
                return mode switch
                {
                    FunctionalityMode.Mail => Symbol.Mail,
                    FunctionalityMode.Calendar => Symbol.Calendar,
                    _ => throw new NotSupportedException(),
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Symbol symbol)
            {
                return symbol switch
                {
                    Symbol.Mail => FunctionalityMode.Mail,
                    Symbol.Calendar => FunctionalityMode.Calendar,
                    _ => throw new NotSupportedException(),
                };
            }
            return value;
        }
    }
}
