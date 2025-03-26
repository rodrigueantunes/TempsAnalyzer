// Models/ActiviteFiltreItem.cs
using System.ComponentModel;

namespace TempsAnalyzer.Models
{
    public class ActiviteFiltreItem : INotifyPropertyChanged
    {
        public string Code { get; set; }
        public string Libelle { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
