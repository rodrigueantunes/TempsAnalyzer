using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TempsAnalyzer.Converters
{
    public class BoolToGreenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is bool b && b;
            return isActive ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Color.FromRgb(25, 118, 210)); // bleu
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
