using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TempsAnalyzer.Converters
{
    public class DateColumnVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var debut = values[0] as DateTime?;
            var fin = values[1] as DateTime?;
            return (debut.HasValue && fin.HasValue) ? Visibility.Collapsed : Visibility.Visible;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
