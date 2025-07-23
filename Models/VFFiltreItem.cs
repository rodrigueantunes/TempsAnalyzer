using System.ComponentModel;

public class VFFiltreItem : INotifyPropertyChanged
{
    private string _vf;
    public string VF
    {
        get => _vf;
        set
        {
            if (_vf != value)
            {
                _vf = value;
                OnPropertyChanged(nameof(VF));
            }
        }
    }

    private bool _isChecked = true;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));

                // Lorsque l'état de cette case change, notifier la vue du changement
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }

    public string DesignationVF { get; set; }
    public string Designation => $"{DesignationVF}";

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
