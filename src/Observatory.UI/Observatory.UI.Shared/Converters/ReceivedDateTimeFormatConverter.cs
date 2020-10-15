using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Shared.Converters
{
    public class ReceivedDateTimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset time)
            {
                var now = DateTimeOffset.Now;
                if (now.Date == time.Date)
                {
                    return time.ToString("hh:mm tt");
                }

                var delta = now.DateTime - time.DateTime;
                if (delta < TimeSpan.FromDays(7))
                {
                    return time.ToString("ddd hh:mm tt");
                }

                if (now.Year == time.Year)
                {
                    return time.ToString("dd/MM hh:mm tt");
                }

                return time.ToString("dd/MM/yyyy hh:mm tt");
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
