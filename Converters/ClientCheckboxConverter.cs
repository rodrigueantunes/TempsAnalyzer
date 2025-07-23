using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace TempsAnalyzer.Converters
{
    public class ClientCheckboxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var clientsFiltres = value as ObservableCollection<string>;
            var client = parameter as string;

            return clientsFiltres != null && client != null && clientsFiltres.Contains(client);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isChecked = (bool)value;
            var client = parameter as string;
            var clientsFiltres = targetType == typeof(ObservableCollection<string>)
                ? new ObservableCollection<string>()
                : null;

            return Binding.DoNothing; // Gestion dynamique dans XAML derrière
        }
    }
}
