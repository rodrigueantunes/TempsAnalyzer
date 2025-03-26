using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using OfficeOpenXml;
using TempsAnalyzer.Models;

namespace TempsAnalyzer.Services
{
    public static class ExcelService
    {
        public static List<SaisieEntry> LoadSaisieAvecLibelle(string filePath)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var package = new ExcelPackage(new FileInfo(filePath));

            var feuilleSaisie = package.Workbook.Worksheets["Saisie"];
            var feuilleActivites = package.Workbook.Worksheets["Activités"];

            var activites = new Dictionary<string, string>();
            var entries = new List<SaisieEntry>();

            // Lire les activités
            int rowAct = 2;
            while (feuilleActivites.Cells[rowAct, 1].Value != null)
            {
                string code = feuilleActivites.Cells[rowAct, 1].Text.Trim();
                string libelle = feuilleActivites.Cells[rowAct, 2].Text.Trim();
                if (!activites.ContainsKey(code))
                    activites[code] = libelle;
                rowAct++;
            }

            // Lire les saisies
            int row = 5;
            while (feuilleSaisie.Cells[row, 3].Value != null)
            {
                string rawDate = feuilleSaisie.Cells[row, 3].Text;
                if (!DateTime.TryParse(rawDate, out DateTime date))
                {
                    row++; // ignorer les lignes invalides (par exemple : "Solde")
                    continue;
                }

                var entry = new SaisieEntry
                {
                    Date = date,
                    Code = feuilleSaisie.Cells[row, 4].Text.Trim(),
                    Produit = feuilleSaisie.Cells[row, 8].Text.Trim(),
                    NomClient = string.IsNullOrWhiteSpace(feuilleSaisie.Cells[row, 9].Text)
                        ? "Inconnu"
                        : feuilleSaisie.Cells[row, 9].Text.Trim(),
                    VF = string.IsNullOrWhiteSpace(feuilleSaisie.Cells[row, 10].Text)
                        ? (!string.IsNullOrWhiteSpace(feuilleSaisie.Cells[row, 8].Text)
                            ? feuilleSaisie.Cells[row, 8].Text.Trim()
                            : "VSW")
                        : feuilleSaisie.Cells[row, 10].Text.Trim(),
                    Temps = double.TryParse(feuilleSaisie.Cells[row, 7].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double t) ? t / 100.0 : 0
                };

                // Assurer que la désignation de la VF est lue
                entry.DesignationVF = feuilleSaisie.Cells[row, 11].Text.Trim();

                entry.Libelle = activites.ContainsKey(entry.Code) ? activites[entry.Code] : "Inconnu";

                entries.Add(entry);
                row++;
            }

            return entries;
        }
    }
}
