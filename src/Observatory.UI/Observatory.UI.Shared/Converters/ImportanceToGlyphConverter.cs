using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Shared.Converters
{
    public class ImportanceToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Importance importance)
            {
                return importance switch
                {
                    Importance.Low => "\uE1FD",
                    Importance.High => "\uE171",
                    _ => null,
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
