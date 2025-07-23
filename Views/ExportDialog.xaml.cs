using System.Windows;

namespace TempsAnalyzer.Views
{
    public partial class ExportDialog : Window
    {
        public TypeRegroupement Regroupement { get; private set; }
        public bool InclureGraphiques { get; private set; }
        public bool InclureTableaux { get; private set; }

        public ExportDialog()
        {
            InitializeComponent();

            // Liaison avec les checkboxes
            ChkGraphiques.IsChecked = true;
            ChkTableaux.IsChecked = true;
        }

        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            // Déterminer le type de regroupement sélectionné
            if (RdoSemaine.IsChecked == true)
                Regroupement = TypeRegroupement.Semaine;
            else if (RdoMois.IsChecked == true)
                Regroupement = TypeRegroupement.Mois;
            else
                Regroupement = TypeRegroupement.Annee;

            // Récupérer les options supplémentaires
            InclureGraphiques = ChkGraphiques.IsChecked == true;
            InclureTableaux = ChkTableaux.IsChecked == true;

            // Fermer la boîte de dialogue avec succès
            DialogResult = true;
            Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            // Fermer la boîte de dialogue avec annulation
            DialogResult = false;
            Close();
        }
    }
}