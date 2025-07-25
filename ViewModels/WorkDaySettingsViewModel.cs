using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TempsAnalyzer.Helpers;
using TempsAnalyzer.Services;

namespace TempsAnalyzer.ViewModels
{
    public class WorkDaySettingsViewModel
    {
        public double GlobalHoursPerDay
        {
            get => WorkDaySettingsService.Instance.GlobalHoursPerDay;
            set => WorkDaySettingsService.Instance.GlobalHoursPerDay = value;
        }

        public ObservableCollection<ResourceHoursVM> ResourceHours { get; }

        public ICommand SaveCmd { get; }

        public WorkDaySettingsViewModel()
        {
            ResourceHours = new ObservableCollection<ResourceHoursVM>(
                WorkDaySettingsService.Instance.GetAll()
                    .Select(kvp => new ResourceHoursVM { ResourceId = kvp.Key, HoursPerDay = kvp.Value })
            );

            SaveCmd = new RelayCommand(Save);
        }

        private void Save()
        {
            foreach (var rh in ResourceHours)
                WorkDaySettingsService.Instance.SetHoursPerDay(rh.ResourceId, rh.HoursPerDay);
            WorkDaySettingsService.Instance.Persist();
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is TempsAnalyzer.Views.WorkDaySettingsView)?.Close();
        }
    }

    public class ResourceHoursVM : INotifyPropertyChanged
    {
        public string ResourceId { get; set; }
        private double _hoursPerDay;
        public double HoursPerDay
        {
            get => _hoursPerDay;
            set
            {
                if (_hoursPerDay != value)
                {
                    _hoursPerDay = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HoursPerDay)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
