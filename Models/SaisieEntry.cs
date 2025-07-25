using System;
using System.Globalization;
using TempsAnalyzer.Services;

namespace TempsAnalyzer.Models
{
    public class SaisieEntry
    {
        public DateTime Date { get; set; }
        public string Code { get; set; }
        public string Libelle { get; set; }
        public string NomClient { get; set; }
        public string VF { get; set; }
        public string Produit { get; set; }
        public double Temps { get; set; }
        public string DesignationVF { get; set; }

        public string Initiales { get; set; }
        public string Ressource { get; set; }
        public string Service { get; set; }

        public string RessourceOuInitiales => !string.IsNullOrWhiteSpace(Ressource) ? Ressource : Initiales;

        private double HoursPerDay => WorkDaySettingsService.Instance.GetHoursPerDay(RessourceOuInitiales);

        public double TempsHeuresDouble => Temps * HoursPerDay;

        public string TempsHeures => FormatHeures(TempsHeuresDouble);

        public string TempsAffichage => $"{Math.Round(Temps, 2)} jour(s) / {FormatHeures(TempsHeuresDouble)}";

        public string SemaineAffichage
        {
            get
            {
                var culture = CultureInfo.CurrentCulture;
                int semaine = culture.Calendar.GetWeekOfYear(Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                return $"Semaine {semaine}";
            }
        }

        private static string FormatHeures(double heures)
        {
            int h = (int)heures;
            int min = (int)Math.Round((heures - h) * 60);
            return $"{h}h {min}min";
        }
    }
}
