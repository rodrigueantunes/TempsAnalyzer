using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TempsAnalyzer.Models;
using Microsoft.Win32;
using System.Windows.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TempsAnalyzer.Helpers
{
    public static class PdfExporter
    {
        public static string LogoClientPath { get; set; }
        public static string LogoEntreprisePath { get; set; }

        public static void ChargerLogoClient()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.ico;*.webp"
            };

            if (dialog.ShowDialog() == true)
                LogoClientPath = dialog.FileName;
        }

        private static byte[] LoadLogoFromResources()
        {
            try
            {
                // Accès à la ressource directement sous forme de tableau d'octets
                var logo = Properties.Resources.volume_software;

                // Retourne le tableau d'octets (si c'est déjà un tableau de bytes)
                return logo;
            }
            catch (Exception ex)
            {
                // Journalisation de l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur de chargement du logo : {ex.Message}");
                return null;
            }
        }


        // Chargement de l'image de ressource
        private static byte[] _logoEntrepriseBytes = LoadLogoFromResources();


        public static void ChargerLogoEntreprise()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.ico;*.webp"
            };

            if (dialog.ShowDialog() == true)
                LogoEntreprisePath = dialog.FileName;
        }

        public static void GenererPdf(List<SaisieEntry> saisies, bool inclureGraphiques, bool inclureTableaux)
        {
            var clientsUniques = saisies.Select(s => s.NomClient).Distinct().ToList();
            bool afficherColonneClient = clientsUniques.Count > 1;
            string clientTitre = afficherColonneClient ? "" : $" {clientsUniques.First()}";

            var groupesParSemaine = saisies
                .GroupBy(s => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .OrderBy(g => g.Key);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                // Page principale stylisée
                container.Page(page =>
                {
                    page.Margin(30);

                    // En-tête avec logos
                    page.Header().Row(row =>
                    {
                        // Logo Client - Utilisation de FitWidth pour préserver les proportions
                        if (!string.IsNullOrEmpty(LogoClientPath))
                            row.ConstantItem(80).Image(LogoClientPath).FitWidth();
                        else
                            row.ConstantItem(120); // Espace vide si pas de logo

                        row.RelativeItem().AlignCenter().Text($"Rapport Hebdomadaire{clientTitre}")
                            .FontSize(32).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                        // Logo Entreprise - Utilisation de FitWidth pour préserver les proportions
                        if (_logoEntrepriseBytes != null)
                            row.ConstantItem(170).Image(_logoEntrepriseBytes).FitWidth();
                        else
                            row.ConstantItem(120); // Espace vide si pas de logo
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingVertical(100).AlignCenter().Column(c =>
                        {
                            c.Item().Text("Rapport d'Analyse des Temps").FontSize(48).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                            c.Item().Text($"Généré le {DateTime.Now:dd MMMM yyyy}").FontSize(24).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            c.Item().Text("Ce rapport présente l'analyse détaillée du temps travaillé par client et par type d'activité pour chaque semaine.")
                                .FontSize(16).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        });

                        // Sauter quelques lignes avant de commencer le rapport détaillé
                        col.Item().PaddingTop(50).Text("Consulter le détail du rapport.");
                    });
                });

                // Pages de contenu de chaque semaine
                foreach (var semaineGroupe in groupesParSemaine)
                {
                    container.Page(page =>
                    {
                        page.Margin(30);

                        // En-tête avec logos
                        page.Header().Row(row =>
                        {
                            // Vérification du logo client
                            if (!string.IsNullOrEmpty(LogoClientPath))
                                row.ConstantItem(80).Image(LogoClientPath).FitArea();
                            else
                                row.ConstantItem(120); // Laisser un espace vide si le logo client n'est pas disponible

                            row.RelativeItem().AlignCenter().Text($"Rapport Hebdomadaire{clientTitre} - Semaine {semaineGroupe.Key}")
                                .FontSize(22).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                            // Vérification du logo entreprise
                            if (_logoEntrepriseBytes != null)
                                row.ConstantItem(170).Image(_logoEntrepriseBytes).FitWidth();
                            else
                                row.ConstantItem(120); // Espace vide si pas de logo
                        });


                        page.Content().Column(col =>
                        {
                            var cumulParVF = semaineGroupe
                                .GroupBy(s => new { s.VF, s.Libelle, Client = s.NomClient, Designation = s.DesignationVF })
                                .Select(g => new VFResume
                                {
                                    Client = g.Key.Client,
                                    VF = g.Key.VF,
                                    Libelle = g.Key.Libelle,
                                    DesignationVF = g.Key.Designation,
                                    TempsTotalJours = Math.Round(g.Sum(s => s.Temps), 2),
                                    TempsTotalHeures = Math.Round(g.Sum(s => s.Temps) * 8, 2)
                                })
                                .OrderBy(x => x.VF).ThenBy(x => x.Client).ToList();

                            if (inclureTableaux)
                            {
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        if (afficherColonneClient)
                                            columns.RelativeColumn(2);  
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(3);  
                                        columns.RelativeColumn(2);   
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        if (afficherColonneClient)
                                            header.Cell().Element(CellStyle).Text("Client");
                                        header.Cell().Element(CellStyle).Text("VF");
                                        header.Cell().Element(CellStyle).Text("Désignation VF");
                                        header.Cell().Element(CellStyle).Text("Type");
                                        header.Cell().Element(CellStyle).Text("Jours");
                                        header.Cell().Element(CellStyle).Text("hh:mm");

                                        static IContainer CellStyle(IContainer c)
                                            => c.DefaultTextStyle(x => x.Bold())
                                                .Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5);
                                    });

                                    foreach (var vf in cumulParVF)
                                    {
                                        var heures = (int)Math.Floor(vf.TempsTotalHeures);
                                        var minutes = (int)Math.Round((vf.TempsTotalHeures - heures) * 60);
                                        string tempsFormatte = $"{heures}h{minutes:D2}";

                                        if (afficherColonneClient)
                                            table.Cell().Element(CellContent).Text(vf.Client);
                                        table.Cell().Element(CellContent).Text(vf.VF);
                                        table.Cell().Element(CellContent).Text(vf.DesignationVF);
                                        table.Cell().Element(CellContent).Text(vf.Libelle);
                                        table.Cell().Element(CellContent).Text($"{vf.TempsTotalJours:0.##}");
                                        table.Cell().Element(CellContent).Text(tempsFormatte);

                                        static IContainer CellContent(IContainer c) => c.Padding(4);
                                    }
                                });
                            }

                            if (inclureGraphiques)
                            {
                                // Graphique en Jours
                                col.Item().PaddingTop(20).Element(container =>
                                {
                                    var plt = new Plot();
                                    double[] valeurs = cumulParVF.Select(x => x.TempsTotalJours).ToArray();
                                    string[] labels = cumulParVF.Select(x => x.VF).ToArray();

                                    var bar = plt.Add.Bars(valeurs);
                                    bar.Label = "Temps (jours)";
                                    plt.Title("Répartition du temps par VF (jours)");
                                    plt.Axes.Left.Label.Text = "Temps (jours)";
                                    plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
                                    plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);
                                    plt.Axes.SetLimitsY(0, valeurs.Max() * 1.2);

                                    var img = plt.GetImage(800, 400);
                                    container.Image(img.GetImageBytes());
                                });

                                // Graphique en Heures
                                col.Item().PaddingTop(20).Element(container =>
                                {
                                    var pltHeures = new Plot();
                                    double[] valeursHeures = cumulParVF.Select(x => x.TempsTotalHeures).ToArray();
                                    string[] labelsHeures = cumulParVF.Select(x => x.VF).ToArray();

                                    var barHeures = pltHeures.Add.Bars(valeursHeures);
                                    barHeures.Label = "Temps (heures)";
                                    pltHeures.Title("Répartition du temps par VF (heures)");
                                    pltHeures.Axes.Left.Label.Text = "Temps (heures)";
                                    pltHeures.Axes.Bottom.TickLabelStyle.Rotation = 45;
                                    pltHeures.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(Enumerable.Range(0, labelsHeures.Length).Select(i => (double)i).ToArray(), labelsHeures);
                                    pltHeures.Axes.SetLimitsY(0, valeursHeures.Max() * 1.2);

                                    var imgHeures = pltHeures.GetImage(800, 400);
                                    container.Image(imgHeures.GetImageBytes());
                                });
                            }
                        });

                        page.Footer().AlignCenter()
                            .Text("Rapport Temps by Antunes")
                            .FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                    });
                }
            });

            var nomFichier = $"RapportHebdo_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            document.GeneratePdf(nomFichier);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = nomFichier,
                UseShellExecute = true
            });
        }

        private class VFResume
        {
            public string Client { get; set; }
            public string VF { get; set; }
            public string DesignationVF { get; set; }
            public string Libelle { get; set; }
            public double TempsTotalJours { get; set; }
            public double TempsTotalHeures { get; set; }
        }
    }
}
