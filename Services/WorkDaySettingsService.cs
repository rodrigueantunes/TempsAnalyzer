using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TempsAnalyzer.Services
{
    public sealed class WorkDaySettingsService
    {
        private const double DefaultHours = 8.0;
        private static readonly Lazy<WorkDaySettingsService> _instance = new(() => new WorkDaySettingsService());
        public static WorkDaySettingsService Instance => _instance.Value;

        private readonly Dictionary<string, double> _perResource;
        private readonly string _path;

        private WorkDaySettingsService()
        {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TempsAnalyzer", "workdaysettings.json");
            _perResource = Load();
        }

        public double GlobalHoursPerDay { get; set; } = DefaultHours;

        public double GetHoursPerDay(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return GlobalHoursPerDay;
            return _perResource.TryGetValue(resourceId, out var h) ? h : GlobalHoursPerDay;
        }

        public void SetHoursPerDay(string resourceId, double hours)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return;
            if (Math.Abs(hours - GlobalHoursPerDay) < 0.01) _perResource.Remove(resourceId);
            else _perResource[resourceId] = hours;
        }

        public IReadOnlyDictionary<string, double> GetAll() => _perResource;

        public void Persist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var data = new PersistModel { Global = GlobalHoursPerDay, PerResource = _perResource };
            File.WriteAllText(_path, JsonConvert.SerializeObject(data));
        }

        private Dictionary<string, double> Load()
        {
            if (!File.Exists(_path)) return new Dictionary<string, double>();
            var json = File.ReadAllText(_path);
            var data = JsonConvert.DeserializeObject<PersistModel>(json);
            GlobalHoursPerDay = data?.Global ?? DefaultHours;
            return data?.PerResource ?? new Dictionary<string, double>();
        }

        private class PersistModel
        {
            public double Global { get; set; }
            public Dictionary<string, double> PerResource { get; set; }
        }
    }
}
