using System.ComponentModel;

namespace TempsAnalyzer.Models
{
    public class RessourceFiltreItem : INotifyPropertyChanged
    {
        public string Initiales { get; set; }
        public string Libelle { get; set; }
        public string Service { get; set; }

        private bool _isChecked = true;
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
