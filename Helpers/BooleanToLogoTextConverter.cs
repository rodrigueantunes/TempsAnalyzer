using System;
using System.Globalization;
using System.Windows.Data;

namespace TempsAnalyzer.Helpers
{
    public class BooleanToLogoTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = (bool)value;
            string libelle = parameter as string;  // Récupérer le texte du libellé passé en paramètre

            if (libelle != null)
            {
                // Ajouter un "V" si sélectionné
                return isSelected ? $"✔️ {libelle}" : libelle;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // Pas besoin de convertisseur inverse
        }
    }
}
