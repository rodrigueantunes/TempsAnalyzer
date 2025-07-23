using System.Globalization;

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


        public string TempsHeures
        {
            get
            {
                var heures = Temps * 8;
                int h = (int)heures;
                int min = (int)((heures - h) * 60);
                return $"{h}h {min}min";
            }
        }
        public string TempsAffichage
        {
            get
            {
                var heures = Temps * 8;
                int h = (int)heures;
                int min = (int)((heures - h) * 60);
                return $"{Math.Round(Temps, 2)} jour(s) / {h}h {min}min";
            }
        }
        public string SemaineAffichage
        {
            get
            {
                var culture = CultureInfo.CurrentCulture;
                int semaine = culture.Calendar.GetWeekOfYear(Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                return $"Semaine {semaine}";
            }
        }

        public string Initiales { get; set; } 
        public string Ressource { get; set; }
        public string Service { get; set; }

        public string RessourceOuInitiales => !string.IsNullOrWhiteSpace(Ressource) ? Ressource : Initiales;

    }


}

