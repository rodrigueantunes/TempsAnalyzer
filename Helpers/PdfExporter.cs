using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using TempsAnalyzer.Models;
using TempsAnalyzer.Views;

// Alias pour éviter le conflit ScottPlot.Colors / QuestPDF.Helpers.Colors
using QColors = QuestPDF.Helpers.Colors;

namespace TempsAnalyzer.Helpers
{
    public static class PdfExporter
    {
        public static string LogoClientPath { get; set; }
        public static string LogoEntreprisePath { get; set; }

        // Taille des polices du tableau
        private const float HeaderFontSize = 7f;
        private const float CellFontSize = 6f;

        #region Logos

        public static void ChargerLogoClient()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.ico;*.webp"
            };
            if (dialog.ShowDialog() == true)
                LogoClientPath = dialog.FileName;
        }

        public static void ChargerLogoEntreprise()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.ico;*.webp"
            };
            if (dialog.ShowDialog() == true)
                LogoEntreprisePath = dialog.FileName;
        }

        private static byte[] LoadLogoFromResources()
        {
            try
            {
                return Properties.Resources.volume_software;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur de chargement du logo : {ex.Message}");
                return null;
            }
        }

        private static readonly byte[] _logoEntrepriseBytes = LoadLogoFromResources();

        #endregion

        #region Public API

        // Nouvelle signature (multiRessource)
        public static void GenererPdf(
            List<SaisieEntry> saisies,
            bool inclureGraphiques,
            bool inclureTableaux,
            TypeRegroupement typeRegroupement = TypeRegroupement.Semaine,
            bool multiRessource = false)
        {
            var clientsUniques = saisies.Select(s => s.NomClient).Distinct().ToList();
            bool afficherColonneClient = clientsUniques.Count > 1;
            string clientTitre = afficherColonneClient ? "" : $" {clientsUniques.First()}";

            string typePeriode = ObtentirNomPeriodeMinuscule(typeRegroupement);
            var nomFichierDefaut = $"Rapport{char.ToUpper(typePeriode[0])}{typePeriode.Substring(1)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            var saveDialog = new SaveFileDialog
            {
                Filter = "Fichiers PDF|*.pdf",
                Title = "Enregistrer le rapport PDF",
                FileName = nomFichierDefaut
            };

            if (saveDialog.ShowDialog() != true)
                return;

            string cheminFichier = saveDialog.FileName;

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                // Page de garde
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Row(row =>
                    {
                        if (!string.IsNullOrEmpty(LogoClientPath))
                            row.ConstantItem(80).Image(LogoClientPath).FitWidth();
                        else
                            row.ConstantItem(120);

                        row.RelativeItem().AlignCenter().Text($"Rapport {ObtentirNomPeriode(typeRegroupement)}{clientTitre}")
                           .FontSize(32).Bold().FontColor(QColors.Blue.Medium);

                        if (_logoEntrepriseBytes != null)
                            row.ConstantItem(170).Image(_logoEntrepriseBytes).FitWidth();
                        else
                            row.ConstantItem(120);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingVertical(100).AlignCenter().Column(c =>
                        {
                            c.Item().Text($"Rapport d'Analyse des Temps - {ObtentirNomPeriode(typeRegroupement)}")
                                   .FontSize(48).Bold().FontColor(QColors.Blue.Darken2);
                            c.Item().Text($"Généré le {DateTime.Now:dd MMMM yyyy}")
                                   .FontSize(24).FontColor(QColors.Grey.Darken1);
                            c.Item().Text($"Ce rapport présente l'analyse détaillée du temps travaillé par client et par type d'activité pour chaque {ObtentirNomPeriodeMinuscule(typeRegroupement)}.")
                                   .FontSize(16).FontColor(QColors.Grey.Darken2);
                        });

                        col.Item().PaddingTop(50).Text("Consulter le détail du rapport.");
                    });
                });

                // Pages de contenu
                switch (typeRegroupement)
                {
                    case TypeRegroupement.Semaine:
                        GenererPagesSemaine(container, saisies, inclureGraphiques, inclureTableaux, afficherColonneClient, clientTitre, multiRessource);
                        break;
                    case TypeRegroupement.Mois:
                        GenererPagesMois(container, saisies, inclureGraphiques, inclureTableaux, afficherColonneClient, clientTitre, multiRessource);
                        break;
                    default:
                        GenererPagesAnnee(container, saisies, inclureGraphiques, inclureTableaux, afficherColonneClient, clientTitre, multiRessource);
                        break;
                }
            });

            document.GeneratePdf(cheminFichier);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = cheminFichier,
                UseShellExecute = true
            });
        }

        // Ancienne signature (compatibilité)
        public static void GenererPdf(
            List<SaisieEntry> saisies,
            bool inclureGraphiques,
            bool inclureTableaux,
            TypeRegroupement typeRegroupement)
            => GenererPdf(saisies, inclureGraphiques, inclureTableaux, typeRegroupement, false);

        #endregion

        #region Pages par période

        private static void GenererPagesSemaine(IDocumentContainer container, List<SaisieEntry> saisies,
            bool inclureGraphiques, bool inclureTableaux, bool afficherColonneClient, string clientTitre, bool multiRessource)
        {
            var groupesSemaine = saisies
                .GroupBy(s => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .OrderBy(g => g.Key);

            foreach (var groupe in groupesSemaine)
            {
                AjouterPageContenuGeneric(container, groupe, $"Semaine {groupe.Key}", afficherColonneClient,
                    clientTitre, TypeRegroupement.Semaine, inclureTableaux, inclureGraphiques, multiRessource);
            }
        }

        private static void GenererPagesMois(IDocumentContainer container, List<SaisieEntry> saisies,
            bool inclureGraphiques, bool inclureTableaux, bool afficherColonneClient, string clientTitre, bool multiRessource)
        {
            var groupesMois = saisies
                .GroupBy(s => new { s.Date.Year, s.Date.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);

            foreach (var groupe in groupesMois)
            {
                var dateExemple = new DateTime(groupe.Key.Year, groupe.Key.Month, 1);
                string titrePeriode = $"{dateExemple:MMMM yyyy}";
                AjouterPageContenuGeneric(container, groupe, titrePeriode, afficherColonneClient,
                    clientTitre, TypeRegroupement.Mois, inclureTableaux, inclureGraphiques, multiRessource);
            }
        }

        private static void GenererPagesAnnee(IDocumentContainer container, List<SaisieEntry> saisies,
            bool inclureGraphiques, bool inclureTableaux, bool afficherColonneClient, string clientTitre, bool multiRessource)
        {
            var groupesAnnee = saisies
                .GroupBy(s => s.Date.Year)
                .OrderBy(g => g.Key);

            foreach (var groupe in groupesAnnee)
            {
                string titrePeriode = $"Année {groupe.Key}";
                AjouterPageContenuGeneric(container, groupe, titrePeriode, afficherColonneClient,
                    clientTitre, TypeRegroupement.Annee, inclureTableaux, inclureGraphiques, multiRessource);
            }
        }

        #endregion

        #region Page générique

        private static void AjouterPageContenuGeneric<T, TKey>(
            IDocumentContainer container,
            IGrouping<TKey, T> groupe,
            string titrePeriode,
            bool afficherColonneClient,
            string clientTitre,
            TypeRegroupement typeRegroupement,
            bool inclureTableaux,
            bool inclureGraphiques,
            bool multiRessource)
            where T : SaisieEntry
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Row(row =>
                {
                    if (!string.IsNullOrEmpty(LogoClientPath))
                        row.ConstantItem(80).Image(LogoClientPath).FitArea();
                    else
                        row.ConstantItem(120);

                    row.RelativeItem().AlignCenter().Text($"Rapport {ObtentirNomPeriode(typeRegroupement)}{clientTitre} - {titrePeriode}")
                        .FontSize(22).Bold().FontColor(QColors.Blue.Medium);

                    if (_logoEntrepriseBytes != null)
                        row.ConstantItem(170).Image(_logoEntrepriseBytes).FitWidth();
                    else
                        row.ConstantItem(120);
                });

                page.Content().Column(col =>
                {
                    if (inclureTableaux)
                    {
                        if (multiRessource)
                            AjouterTableauMulti(col, groupe, afficherColonneClient);
                        else
                            AjouterTableauMono(col, groupe, afficherColonneClient);
                    }

                    if (inclureGraphiques)
                        AjouterGraphiques(col, groupe);
                });

                page.Footer().AlignCenter()
                    .Text("Rapport Temps by Antunes")
                    .FontSize(10).FontColor(QColors.Grey.Darken1);
            });
        }

        #endregion

        #region Tableaux

        private static void AjouterTableauMono<T, TKey>(ColumnDescriptor col, IGrouping<TKey, T> groupe, bool afficherColonneClient)
    where T : SaisieEntry
        {
            // Cumul par (Date + VF + Désignation + Type + Client)
            var lignes = groupe
                .Select(s => new
                {
                    Date = s.Date.Date,
                    Client = s.NomClient,
                    VF = s.VF,
                    DesignationVF = s.DesignationVF,
                    Type = s.Libelle,
                    Jours = s.Temps
                })
                .GroupBy(x => new { x.Date, x.Client, x.VF, x.DesignationVF, x.Type })
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.Client,
                    g.Key.VF,
                    g.Key.DesignationVF,
                    g.Key.Type,
                    Jours = Math.Round(g.Sum(x => x.Jours), 2),
                    Heures = Math.Round(g.Sum(x => x.Jours) * 8, 2)
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.VF)
                .ThenBy(x => x.Type)
                .ToList();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);   // Date
                    if (afficherColonneClient) columns.RelativeColumn(2); // Client
                    columns.RelativeColumn(1);   // VF
                    columns.RelativeColumn(3);   // Désignation VF
                    columns.RelativeColumn(2);   // Type
                    columns.RelativeColumn(1);   // Jours
                    columns.RelativeColumn(1);   // hh:mm
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date");
                    if (afficherColonneClient)
                        header.Cell().Element(CellStyle).Text("Client");
                    header.Cell().Element(CellStyle).Text("VF");
                    header.Cell().Element(CellStyle).Text("Désignation VF");
                    header.Cell().Element(CellStyle).Text("Type");
                    header.Cell().Element(CellStyle).Text("Jours");
                    header.Cell().Element(CellStyle).Text("hh:mm");
                });

                foreach (var l in lignes)
                {
                    var h = (int)Math.Floor(l.Heures);
                    var m = (int)Math.Round((l.Heures - h) * 60);
                    var hhmm = $"{h}h{m:D2}";

                    table.Cell().Element(CellContent).Text(l.Date.ToString("dd/MM/yyyy"));
                    if (afficherColonneClient)
                        table.Cell().Element(CellContent).Text(l.Client);
                    table.Cell().Element(CellContent).Text(l.VF);
                    table.Cell().Element(CellContent).Text(l.DesignationVF);
                    table.Cell().Element(CellContent).Text(l.Type);
                    table.Cell().Element(CellContent).Text($"{l.Jours:0.##}");
                    table.Cell().Element(CellContent).Text(hhmm);
                }
            });
        }


        private static void AjouterTableauMulti<T, TKey>(ColumnDescriptor col, IGrouping<TKey, T> groupe, bool afficherColonneClient)
            where T : SaisieEntry
        {
            // CUMUL : par (Date, Ressource, Service, VF, DesignationVF, Type)
            var lignes = groupe
                .Select(s => new
                {
                    Date = s.Date.Date,
                    Ressource = string.IsNullOrWhiteSpace(s.Ressource)
                                    ? (string.IsNullOrWhiteSpace(s.Initiales) ? "?" : s.Initiales)
                                    : s.Ressource,
                    Service = string.IsNullOrWhiteSpace(s.Service) ? "Inconnu" : s.Service,
                    Client = s.NomClient,
                    VF = s.VF,
                    DesignationVF = s.DesignationVF,
                    Type = s.Libelle,
                    Jours = s.Temps
                })
                .GroupBy(x => new { x.Date, x.Ressource, x.Service, x.Client, x.VF, x.DesignationVF, x.Type })
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.Ressource,
                    g.Key.Service,
                    g.Key.Client,
                    g.Key.VF,
                    g.Key.DesignationVF,
                    g.Key.Type,
                    Jours = Math.Round(g.Sum(x => x.Jours), 2),
                    Heures = Math.Round(g.Sum(x => x.Jours) * 8, 2)
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Ressource)
                .ThenBy(x => x.VF)
                .ThenBy(x => x.Type)
                .ToList();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1); // Date
                    columns.RelativeColumn(2); // Ressource
                    columns.RelativeColumn(1); // Service
                    if (afficherColonneClient)
                        columns.RelativeColumn(2); // Client
                    columns.RelativeColumn(1); // VF
                    columns.RelativeColumn(3); // Désignation VF
                    columns.RelativeColumn(2); // Type
                    columns.RelativeColumn(1); // Jours
                    columns.RelativeColumn(1); // hh:mm
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date");
                    header.Cell().Element(CellStyle).Text("Ressource");
                    header.Cell().Element(CellStyle).Text("Service");
                    if (afficherColonneClient)
                        header.Cell().Element(CellStyle).Text("Client");
                    header.Cell().Element(CellStyle).Text("VF");
                    header.Cell().Element(CellStyle).Text("Désignation VF");
                    header.Cell().Element(CellStyle).Text("Type");
                    header.Cell().Element(CellStyle).Text("Jours");
                    header.Cell().Element(CellStyle).Text("hh:mm");
                });

                foreach (var l in lignes)
                {
                    var h = (int)Math.Floor(l.Heures);
                    var m = (int)Math.Round((l.Heures - h) * 60);
                    var hhmm = $"{h}h{m:D2}";

                    table.Cell().Element(CellContent).Text(l.Date.ToString("dd/MM/yyyy"));
                    table.Cell().Element(CellContent).Text(l.Ressource);
                    table.Cell().Element(CellContent).Text(l.Service);
                    if (afficherColonneClient)
                        table.Cell().Element(CellContent).Text(l.Client);
                    table.Cell().Element(CellContent).Text(l.VF);
                    table.Cell().Element(CellContent).Text(l.DesignationVF);
                    table.Cell().Element(CellContent).Text(l.Type);
                    table.Cell().Element(CellContent).Text($"{l.Jours:0.##}");
                    table.Cell().Element(CellContent).Text(hhmm);
                }
            });
        }

        // Styles réutilisables
        private static IContainer CellStyle(IContainer c) =>
            c.DefaultTextStyle(t => t.Bold().FontSize(HeaderFontSize))
             .Background(QColors.Grey.Lighten2)
             .Padding(4);

        private static IContainer CellContent(IContainer c) =>
            c.DefaultTextStyle(t => t.FontSize(CellFontSize))
             .Padding(3);

        #endregion

        #region Graphiques

        private static void AjouterGraphiques<T, TKey>(ColumnDescriptor col, IGrouping<TKey, T> groupe)
            where T : SaisieEntry
        {
            // cumul par VF uniquement
            var cumulPourGraph = groupe
                .GroupBy(s => s.VF)
                .Select(g => new
                {
                    VF = g.Key,
                    Jours = Math.Round(g.Sum(x => x.Temps), 2),
                    Heures = Math.Round(g.Sum(x => x.Temps) * 8, 2)
                })
                .OrderBy(x => x.VF)
                .ToList();

            if (cumulPourGraph.Count == 0) return;

            // Graphique Jours
            col.Item().PaddingTop(20).Element(container =>
            {
                var plt = new Plot();
                double[] valeurs = cumulPourGraph.Select(x => x.Jours).ToArray();
                string[] labels = cumulPourGraph.Select(x => x.VF).ToArray();

                var bar = plt.Add.Bars(valeurs);
                bar.Label = "Temps (jours)";
                plt.Title("Répartition du temps par VF (jours)");
                plt.Axes.Left.Label.Text = "Temps (jours)";
                plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);
                if (valeurs.Length > 0)
                    plt.Axes.SetLimitsY(0, valeurs.Max() * 1.2);

                var img = plt.GetImage(800, 400);
                container.Image(img.GetImageBytes());
            });

            // Graphique Heures
            col.Item().PaddingTop(20).Element(container =>
            {
                var pltH = new Plot();
                double[] valeursH = cumulPourGraph.Select(x => x.Heures).ToArray();
                string[] labelsH = cumulPourGraph.Select(x => x.VF).ToArray();

                var barH = pltH.Add.Bars(valeursH);
                barH.Label = "Temps (heures)";
                pltH.Title("Répartition du temps par VF (heures)");
                pltH.Axes.Left.Label.Text = "Temps (heures)";
                pltH.Axes.Bottom.TickLabelStyle.Rotation = 45;
                pltH.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    Enumerable.Range(0, labelsH.Length).Select(i => (double)i).ToArray(), labelsH);
                if (valeursH.Length > 0)
                    pltH.Axes.SetLimitsY(0, valeursH.Max() * 1.2);

                var imgH = pltH.GetImage(800, 400);
                container.Image(imgH.GetImageBytes());
            });
        }

        #endregion

        #region Utils

        private static string ObtentirNomPeriode(TypeRegroupement typeRegroupement) =>
            typeRegroupement switch
            {
                TypeRegroupement.Mois => "Mensuel",
                TypeRegroupement.Annee => "Annuel",
                _ => "Hebdomadaire"
            };

        private static string ObtentirNomPeriodeMinuscule(TypeRegroupement typeRegroupement) =>
            typeRegroupement switch
            {
                TypeRegroupement.Mois => "mois",
                TypeRegroupement.Annee => "année",
                _ => "semaine"
            };

        private class VFResume
        {
            public string Client { get; set; }
            public string VF { get; set; }
            public string DesignationVF { get; set; }
            public string Libelle { get; set; }
            public double TempsTotalJours { get; set; }
            public double TempsTotalHeures { get; set; }
        }

        #endregion
    }
}
