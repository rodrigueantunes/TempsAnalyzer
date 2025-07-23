using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static TempsAnalyzer.ViewModels.MainViewModel;

namespace TempsAnalyzer.Views
{
    public partial class FenetreAssociationInitiales : Window
    {
        public ObservableCollection<MappingInitialeItem> MappingList { get; set; }

        public FenetreAssociationInitiales(List<MappingInitialeItem> mapping)
        {
            InitializeComponent();
            MappingList = new ObservableCollection<MappingInitialeItem>(
                mapping?.Select(m => new MappingInitialeItem
                {
                    Initiales = m.Initiales,
                    Libelle = m.Libelle,
                    Service = m.Service
                }) ?? new List<MappingInitialeItem>()
            );
            DataContext = this;
        }

        public List<MappingInitialeItem> GetResult()
        {
            // Filtre uniquement les entrées valides, sans doublon d'initiales
            return MappingList
                .Where(m => !string.IsNullOrWhiteSpace(m.Initiales) && !string.IsNullOrWhiteSpace(m.Libelle))
                .GroupBy(m => m.Initiales)
                .Select(g => g.First())
                .ToList();
        }

        private void Valider_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // Ferme la fenêtre avec succès
        }


        private void Annuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

    }
}
