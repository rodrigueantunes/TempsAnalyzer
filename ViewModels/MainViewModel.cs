using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using TempsAnalyzer.Helpers;
using TempsAnalyzer.Models;
using TempsAnalyzer.Services;
using TempsAnalyzer.Views;

namespace TempsAnalyzer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SaisieEntry> _saisies;
        public ObservableCollection<SaisieEntry> Saisies
        {
            get => _saisies;
            set
            {
                _saisies = value;
                OnPropertyChanged();
                UpdateTotaux();
            }
        }

        private bool _multiRessourceActif;
        public bool MultiRessourceActif
        {
            get => _multiRessourceActif;
            set { _multiRessourceActif = value; OnPropertyChanged(); }
        }

        public ObservableCollection<int> ListeAnnees { get; } =
            new ObservableCollection<int>(Enumerable.Range(2020, 21));

        private int _anneeSelectionnee = DateTime.Now.Year; 
        public int AnneeSelectionnee
        {
            get => _anneeSelectionnee;
            set
            {
                if (_anneeSelectionnee != value)
                {
                    _anneeSelectionnee = value;
                    OnPropertyChanged();
                    if (MultiRessourceActif)
                        LoadMultiRessource();
                }
            }
        }

        public class MappingInitialeItem
        {
            public string Initiales { get; set; }
            public string Libelle { get; set; }
            public string Service { get; set; }
        }


        private ObservableCollection<RessourceFiltreItem> _ressourcesDisponibles = new ObservableCollection<RessourceFiltreItem>();
        public ObservableCollection<RessourceFiltreItem> RessourcesDisponibles
        {
            get => _ressourcesDisponibles;
            set { _ressourcesDisponibles = value; OnPropertyChanged(); }
        }
        private bool _allRessourcesChecked = true;
        public bool AllRessourcesChecked
        {
            get => _allRessourcesChecked;
            set
            {
                if (_allRessourcesChecked != value)
                {
                    _allRessourcesChecked = value;
                    OnPropertyChanged();
                    foreach (var r in RessourcesDisponibles)
                        r.IsChecked = value;
                    Filtrer(); 
                }
            }
        }





        // Ajouter les propriétés pour les totaux
        private double _totalJours;
        public double TotalJours
        {
            get => _totalJours;
            set { _totalJours = value; OnPropertyChanged(); }
        }

        private double _totalHeures;
        public double TotalHeures
        {
            get => _totalHeures;
            set { _totalHeures = value; OnPropertyChanged(); }
        }

        // Méthode pour mettre à jour les totaux
        private void UpdateTotaux()
        {
            // Calculer les totaux
            TotalJours = Saisies.Sum(s => s.Temps);
            TotalHeures = TotalJours * 8; // 1 jour = 8 heures
        }

        private ObservableCollection<SaisieEntry> _saisiesInitiales;

        private ObservableCollection<ClientFiltreItem> _clientsDisponibles;
        public ObservableCollection<ClientFiltreItem> ClientsDisponibles
        {
            get => _clientsDisponibles;
            set { _clientsDisponibles = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ActiviteFiltreItem> _typesDisponibles;
        public ObservableCollection<ActiviteFiltreItem> TypesDisponibles
        {
            get => _typesDisponibles;
            set { _typesDisponibles = value; OnPropertyChanged(); }
        }
        public ICommand ActualiserVFsCommand { get; }

        private bool _allClientsChecked = true;
        public bool AllClientsChecked
        {
            get => _allClientsChecked;
            set
            {
                if (_allClientsChecked != value)
                {
                    _allClientsChecked = value;
                    OnPropertyChanged();

                    foreach (var client in ClientsDisponibles)
                        client.IsChecked = value;

                    Filtrer();
                }
            }
        }



        private bool _allTypesChecked = true;
        public bool AllTypesChecked
        {
            get => _allTypesChecked;
            set
            {
                if (_allTypesChecked != value)
                {
                    _allTypesChecked = value;
                    OnPropertyChanged();

                    foreach (var type in TypesDisponibles)
                        type.IsChecked = value;

                    Filtrer();
                }
            }
        }

        private bool _logoClientEstSelectionné;
        public bool LogoClientEstSelectionné
        {
            get => _logoClientEstSelectionné;
            set { _logoClientEstSelectionné = value; OnPropertyChanged(); }
        }

        private bool _logoEntrepriseEstSelectionné;
        public bool LogoEntrepriseEstSelectionné
        {
            get => _logoEntrepriseEstSelectionné;
            set { _logoEntrepriseEstSelectionné = value; OnPropertyChanged(); }
        }

        public ICommand ChargerLogoClientCommand { get; }
        public ICommand ChargerLogoEntrepriseCommand { get; }

        private DateTime? _dateDebut;
        public DateTime? DateDebut
        {
            get => _dateDebut;
            set { _dateDebut = value; OnPropertyChanged(); OnPropertyChanged(nameof(AfficherSemaine)); Filtrer(); }
        }

        private DateTime? _dateFin;
        public DateTime? DateFin
        {
            get => _dateFin;
            set { _dateFin = value; OnPropertyChanged(); OnPropertyChanged(nameof(AfficherSemaine)); Filtrer(); }
        }

        public bool AfficherSemaine => DateDebut.HasValue && DateFin.HasValue;

        public ICommand ToggleMultiRessourceCommand { get; }
        public ICommand AssocierInitialesCommand { get; }



        public MainViewModel()
        {
            Saisies = new ObservableCollection<SaisieEntry>();
            ExporterPdfCommand = new RelayCommand(ExporterPdf);
            ChargerLogoClientCommand = new RelayCommand(ChargerLogoClient); // Déclarer la commande ici
            ChargerLogoEntrepriseCommand = new RelayCommand(ChargerLogoEntreprise); // Déclarer la commande ici
            ActualiserVFsCommand = new RelayCommand(ActualiserVFs);
            ToggleMultiRessourceCommand = new RelayCommand(ToggleMultiRessource);
            AssocierInitialesCommand = new RelayCommand(OuvrirFenetreAssociationInitiales);
        }

        private Dictionary<string, string> _initialesToRessource = new();

        private List<MappingInitialeItem> _mappingInitiales = new List<MappingInitialeItem>();

        private void ChargerMappingInitiales()
        {
            var chemin = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InitialesToRessource.json");


            if (System.IO.File.Exists(chemin))
            {
                var json = System.IO.File.ReadAllText(chemin);
                if (json.Trim().StartsWith("{")) // Ancien format (dictionnaire)
                {
                    var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, MappingInitialeItem>>(json);
                    _mappingInitiales = dict.Values.ToList();
                }
                else // Nouveau format (liste)
                {
                    _mappingInitiales = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MappingInitialeItem>>(json) ?? new List<MappingInitialeItem>();
                }
            }
            else
            {
                _mappingInitiales = new List<MappingInitialeItem>();
            }
        }


        public async Task LoadMultiRessourceWithDialogAsync()
        {
            ProgressDialog dialog = null;
            // Ouvre la ProgressDialog sur le thread UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                dialog = new TempsAnalyzer.Views.ProgressDialog
                {
                    Owner = Application.Current.MainWindow
                };
                dialog.Show();
            });

            try
            {
                // Lance le vrai chargement sur un thread séparé (pour ne pas bloquer l’UI)
                await Task.Run(() => LoadMultiRessource());
            }
            finally
            {
                // Ferme la ProgressDialog sur le thread UI
                Application.Current.Dispatcher.Invoke(() => dialog.Close());
            }
        }



        private void OuvrirFenetreAssociationInitiales()
        {
            ChargerMappingInitiales(); // <-- Ajoute cette ligne, très important !
            var win = new FenetreAssociationInitiales(_mappingInitiales);
            if (win.ShowDialog() == true)
            {
                _mappingInitiales = win.GetResult();
                SauvegarderMappingInitiales();
                // Mets à jour toutes les saisies déjà chargées (utile pour MAJ live)
                foreach (var s in Saisies)
                {
                    var mapping = _mappingInitiales.FirstOrDefault(m => m.Initiales == s.Initiales);
                    s.Ressource = mapping?.Libelle ?? s.Initiales;
                    s.Service = mapping?.Service ?? "";
                }
                OnPropertyChanged(nameof(Saisies));
            }
        }




        private void SauvegarderMappingInitiales()
        {
            var chemin = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InitialesToRessource.json");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_mappingInitiales, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(chemin, json);
        }



        public void AssocierInitialesRessources()
        {
            var win = new TempsAnalyzer.Views.FenetreAssociationInitiales(_mappingInitiales);
            if (win.ShowDialog() == true)
            {
                _mappingInitiales = win.GetResult();
                SauvegarderMappingInitiales();
                // Recharge si besoin (optionnel) :
                if (MultiRessourceActif)
                    LoadMultiRessource();
            }
        }



        private void ActualiserVFs()
        {
            // Mettre à jour l'état des VFs en fonction des cases cochées
            var vfsCochees = VFDisponibles.Where(vf => vf.IsChecked).Select(vf => vf.VF).ToList();

            // Mettre à jour la liste des saisies affichées en fonction des VFs cochées
            var result = _saisiesInitiales
                .Where(saisie => vfsCochees.Contains(saisie.VF)) // Filtrer par les VFs cochées
                .ToList();

            // Réafficher les saisies mises à jour dans le tableau
            Saisies = new ObservableCollection<SaisieEntry>(result);

            // Appliquer des actions supplémentaires comme le regroupement ou l'affichage des résultats si nécessaire
            Filtrer();
        }

        public void LoadFromFile()
        {
            var dialog = new OpenFileDialog { Filter = "Fichier Excel|*.xlsx;*.xlsm" };

            if (MultiRessourceActif)
            {
                MultiRessourceActif = false;

                if (RessourcesDisponibles != null)
                {
                    foreach (var r in RessourcesDisponibles)
                        r.PropertyChanged -= RessourceItem_PropertyChanged;
                }

                RessourcesDisponibles = new ObservableCollection<RessourceFiltreItem>();
                OnPropertyChanged(nameof(MultiRessourceActif));
            }

            if (dialog.ShowDialog() == true)
            {
                var result = ExcelService.LoadSaisieAvecLibelle(dialog.FileName);

                foreach (var s in result)
                    s.NomClient = NormalizeClient(s.NomClient);

                _saisiesInitiales = new ObservableCollection<SaisieEntry>(result);

                foreach (var s in result)
                    s.NomClient = (s.NomClient ?? "INCONNU").Trim().ToUpperInvariant();

                // Récupérer les clients
                ClientsDisponibles = new ObservableCollection<ClientFiltreItem>(
                    result.Select(s => s.NomClient)
                          .Distinct()
                          .OrderBy(c => c)
                          .Select(n => new ClientFiltreItem { NomClient = n, IsChecked = true }));

                foreach (var client in ClientsDisponibles)
                {
                    client.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ClientFiltreItem.IsChecked))
                        {
                            AllClientsChecked = ClientsDisponibles.All(c => c.IsChecked);
                            Filtrer();
                        }
                    };
                }

                // Récupérer les types
                TypesDisponibles = new ObservableCollection<ActiviteFiltreItem>(
                    result.Select(s => new { s.Code, s.Libelle })
                          .Distinct()
                          .OrderBy(t => t.Libelle)
                          .Select(t => new ActiviteFiltreItem { Code = t.Code, Libelle = t.Libelle, IsChecked = true }));

                foreach (var type in TypesDisponibles)
                {
                    type.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ActiviteFiltreItem.IsChecked))
                        {
                            AllTypesChecked = TypesDisponibles.All(t => t.IsChecked);
                            Filtrer();
                        }
                    };
                }

                // Récupérer les VFs avec leur désignation
                VFsDisponibles = new ObservableCollection<VFFiltreItem>(
                    result.Select(s => new { s.VF, s.DesignationVF })
                    .Where(v => !string.IsNullOrWhiteSpace(v.VF))
                    .Distinct()
                    .OrderBy(v => v.VF)
                    .Select(vf => new VFFiltreItem { VF = vf.VF, DesignationVF = vf.DesignationVF, IsChecked = true }));
                foreach (var vf in VFsDisponibles)
                {
                    vf.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(VFFiltreItem.IsChecked))
                        {
                            AllVFsChecked = VFsDisponibles.All(v => v.IsChecked);
                            Filtrer();
                        }
                    };
                }

                Filtrer();
            }
        }

        private ObservableCollection<VFFiltreItem> _vfsDisponibles;
        public ObservableCollection<VFFiltreItem> VFsDisponibles
        {
            get => _vfsDisponibles;
            set { _vfsDisponibles = value; OnPropertyChanged(); }
        }

        private bool _allVFsChecked = true;
        public bool AllVFsChecked
        {
            get => _allVFsChecked;
            set
            {
                if (_allVFsChecked != value)
                {
                    _allVFsChecked = value;
                    OnPropertyChanged();

                    foreach (var vf in VFsDisponibles)
                    {
                        vf.IsChecked = value;
                    }

                    Filtrer();
                }
            }
        }

        public void LoadMultiRessource()
        {
            // Charge le mapping initiales/ressource/service (depuis le JSON)
            ChargerMappingInitiales();

            if (!MultiRessourceActif) return;

            var dossier = $@"\\172.16.0.49\Partage\Volume\Suivi des temps\{AnneeSelectionnee}";
            if (!Directory.Exists(dossier))
            {
                Saisies = new ObservableCollection<SaisieEntry>();
                RessourcesDisponibles = new ObservableCollection<RessourceFiltreItem>();
                return;
            }

            // Recherche des fichiers valides
            var fichiers = Directory.EnumerateFiles(dossier, $"saisie_temps_{AnneeSelectionnee}_*.xlsm")
                .Where(f =>
                {
                    var nom = Path.GetFileName(f);
                    if (nom.Equals($"saisie_temps_{AnneeSelectionnee}_MODELE.xlsm", StringComparison.OrdinalIgnoreCase))
                        return false;
                    var sansExt = Path.GetFileNameWithoutExtension(nom);
                    var parts = sansExt.Split('_');
                    // Forme attendue : saisie_temps_YYYY_XX (exactement 4 parties)
                    return parts.Length == 4 && parts[2] == AnneeSelectionnee.ToString();
                })
                .ToList();

            var toutesSaisies = new List<SaisieEntry>();
            foreach (var fichier in fichiers)
            {
                try
                {
                    var saisies = ExcelService.LoadSaisieAvecLibelle(fichier);
                    var initiales = Path.GetFileNameWithoutExtension(fichier).Split('_')[3];

                    // Recherche dans le mapping
                    MappingInitialeItem mapping = null;
                    if (_mappingInitiales != null)
                        mapping = _mappingInitiales.FirstOrDefault(m => m.Initiales == initiales);

                    foreach (var s in saisies)
                    {
                        s.Initiales = initiales;
                        s.Ressource = mapping?.Libelle ?? initiales;
                        s.Service = mapping?.Service ?? "";
                    }
                    toutesSaisies.AddRange(saisies);

                    foreach (var s in toutesSaisies)
                        s.NomClient = NormalizeClient(s.NomClient);
                }
                catch
                {
                    // Logging possible : fichier illisible ou Excel corrompu
                }
            }

            // Génère la liste unique des ressources (ressource = initiales + libelle + service)
            var ressources = toutesSaisies
                .Select(s => new { s.Initiales, s.Ressource, s.Service })
                .Distinct()
                .OrderBy(r => r.Ressource)
                .ThenBy(r => r.Service)
                .ToList();

            // On essaie de conserver les cases cochées si possible
            var anciennesCochees = RessourcesDisponibles?.Where(r => r.IsChecked).Select(r => r.Libelle + "|" + r.Service).ToHashSet()
                                    ?? new HashSet<string>();

            RessourcesDisponibles = new ObservableCollection<RessourceFiltreItem>(
                ressources.Select(r => new RessourceFiltreItem
                {
                    Initiales = r.Initiales,
                    Libelle = r.Ressource,
                    Service = r.Service,
                    IsChecked = anciennesCochees.Count == 0 || anciennesCochees.Contains(r.Ressource + "|" + r.Service)
                })
            );

            foreach (var res in RessourcesDisponibles)
            {
                res.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(RessourceFiltreItem.IsChecked))
                    {
                        AllRessourcesChecked = RessourcesDisponibles.All(r => r.IsChecked);
                        Filtrer();
                    }
                };
            }

            // Mets à jour la liste principale
            Saisies = new ObservableCollection<SaisieEntry>(toutesSaisies);
            _saisiesInitiales = new ObservableCollection<SaisieEntry>(toutesSaisies);

            foreach (var s in toutesSaisies)
                s.NomClient = (s.NomClient ?? "INCONNU").Trim().ToUpperInvariant();

            ClientsDisponibles = new ObservableCollection<ClientFiltreItem>(
            toutesSaisies.Select(s => s.NomClient)
            .Distinct()
            .OrderBy(c => c)
            .Select(n => new ClientFiltreItem { NomClient = n, IsChecked = true })
);
            foreach (var client in ClientsDisponibles)
            {
                client.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ClientFiltreItem.IsChecked))
                    {
                        AllClientsChecked = ClientsDisponibles.All(c => c.IsChecked);
                        Filtrer();
                    }
                };
            }

            TypesDisponibles = new ObservableCollection<ActiviteFiltreItem>(
                toutesSaisies.Select(s => new { s.Code, s.Libelle })
                    .Distinct()
                    .OrderBy(t => t.Libelle)
                    .Select(t => new ActiviteFiltreItem { Code = t.Code, Libelle = t.Libelle, IsChecked = true })
            );
            foreach (var type in TypesDisponibles)
            {
                type.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ActiviteFiltreItem.IsChecked))
                    {
                        AllTypesChecked = TypesDisponibles.All(t => t.IsChecked);
                        Filtrer();
                    }
                };
            }

            foreach (var r in RessourcesDisponibles)
            {
                r.PropertyChanged -= RessourceItem_PropertyChanged;
                r.PropertyChanged += RessourceItem_PropertyChanged;
            }


            VFsDisponibles = new ObservableCollection<VFFiltreItem>(
                toutesSaisies.Select(s => new { s.VF, s.DesignationVF })
                    .Where(v => !string.IsNullOrWhiteSpace(v.VF))
                    .Distinct()
                    .OrderBy(v => v.VF)
                    .Select(vf => new VFFiltreItem { VF = vf.VF, DesignationVF = vf.DesignationVF, IsChecked = true })
            );
            foreach (var vf in VFsDisponibles)
            {
                vf.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(VFFiltreItem.IsChecked))
                    {
                        AllVFsChecked = VFsDisponibles.All(v => v.IsChecked);
                        Filtrer();
                    }
                };
            }

            Filtrer();
    
            UpdateTotaux();
        }


        private void Filtrer()
        {
            if (_saisiesInitiales == null) return;

            // ---- 1) Point de départ : toutes les saisies brutes
            IEnumerable<SaisieEntry> result = _saisiesInitiales;

            // ---- 2) Filtres Client & Type
            var filtresClients = ClientsDisponibles?
                .Where(c => c.IsChecked)
                .Select(c => c.NomClient)
                .ToHashSet() ?? new HashSet<string>();

            result = result.Where(s => filtresClients.Contains(NormalizeClient(s.NomClient)));

            var filtresTypes = TypesDisponibles?
                .Where(t => t.IsChecked)
                .Select(t => t.Code)
                .ToHashSet() ?? new HashSet<string>();

            result = result.Where(s => filtresClients.Contains(s.NomClient) &&
                                       filtresTypes.Contains(s.Code));



            // ---- 3) Filtre Date
            if (DateDebut.HasValue && DateFin.HasValue)
                result = result.Where(s => s.Date.Date >= DateDebut.Value.Date &&
                                           s.Date.Date <= DateFin.Value.Date);

            // ---- 4) Filtre Ressource (SI mode multi)
            if (MultiRessourceActif && RessourcesDisponibles != null && RessourcesDisponibles.Count > 0)
            {
                var ressourcesCochees = RessourcesDisponibles
                    .Where(r => r.IsChecked)
                    .Select(r => r.Libelle)
                    .ToHashSet();

                result = result.Where(s => ressourcesCochees.Contains(s.Ressource));
            }

            // ---- 5) Met à jour la liste des VF (si besoin)
            var vfsFiltrees = result.Select(s => s.VF)
                                    .Distinct()
                                    .OrderBy(vf => vf)
                                    .ToList();

            if (VFDisponibles == null ||
                VFDisponibles.Count == 0 ||
                !VFDisponibles.Select(v => v.VF).OrderBy(x => x).SequenceEqual(vfsFiltrees))
            {
                VFDisponibles = new ObservableCollection<VFFiltreItem>(
                    vfsFiltrees.Select(vf => new VFFiltreItem { VF = vf, IsChecked = true })
                );
            }

            var vfCochees = VFDisponibles
                .Where(v => v.IsChecked)
                .Select(v => v.VF)
                .ToHashSet();

            result = result.Where(s => vfCochees.Contains(s.VF));

            // ---- 6) Sortie finale : tri par Date (puis VF pour stabilité)
            Saisies = new ObservableCollection<SaisieEntry>(
                result.OrderBy(s => s.Date)
                      .ThenBy(s => s.VF)
                      .Select(s => { s.Temps = Math.Round(s.Temps, 2); return s; })
            );

            OnPropertyChanged(nameof(Saisies));
            UpdateTotaux();
        }

        private void RessourceItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RessourceFiltreItem.IsChecked))
            {
                // met à jour le master checkbox
                AllRessourcesChecked = RessourcesDisponibles.All(r => r.IsChecked);
                Filtrer();
            }
        }



        private bool _isLoadingMulti;
        public async void ToggleMultiRessource()
        {
            if (_isLoadingMulti) return;

            if (!MultiRessourceActif)
            {
                MultiRessourceActif = true;
                _isLoadingMulti = true;
                await LoadMultiRessourceWithDialogAsync();
                _isLoadingMulti = false;
            }
            else
            {
                MultiRessourceActif = false;
                if (RessourcesDisponibles != null)
                {
                    foreach (var r in RessourcesDisponibles)
                        r.PropertyChanged -= RessourceItem_PropertyChanged;
                }
                RessourcesDisponibles = new ObservableCollection<RessourceFiltreItem>();
                Filtrer();
                OnPropertyChanged(nameof(MultiRessourceActif));
            }
        }



        private bool _inclureGraphiques = true;
        public bool InclureGraphiques
        {
            get => _inclureGraphiques;
            set { _inclureGraphiques = value; OnPropertyChanged(); }
        }

        private bool _inclureTableaux = true;
        public bool InclureTableaux
        {
            get => _inclureTableaux;
            set { _inclureTableaux = value; OnPropertyChanged(); }
        }

        public ICommand ExporterPdfCommand { get; }

        private void ExporterPdf()
        {
            // Afficher la boîte de dialogue d'options
            var dialog = new ExportDialog
            {
                Owner = Application.Current.MainWindow
            };

            bool? resultat = dialog.ShowDialog();

            if (resultat == true)
            {
                // Utiliser les paramètres sélectionnés dans la fenêtre
                PdfExporter.LogoClientPath = LogoClientPath;
                PdfExporter.LogoEntreprisePath = LogoEntreprisePath;

                PdfExporter.GenererPdf(
                    Saisies.ToList(),
                    dialog.InclureGraphiques,
                    dialog.InclureTableaux,
                    dialog.Regroupement,
                    MultiRessourceActif);

            }
        }

        private static string NormalizeClient(string c)
            => string.IsNullOrWhiteSpace(c) ? "INCONNU" : c.Trim().ToUpperInvariant();

        private string _logoClientPath;
        public string LogoClientPath
        {
            get => _logoClientPath;
            set { _logoClientPath = value; OnPropertyChanged(); }
        }

        private string _logoEntreprisePath;
        public string LogoEntreprisePath
        {
            get => _logoEntreprisePath;
            set { _logoEntreprisePath = value; OnPropertyChanged(); }
        }

        private void ChargerLogoClient()
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.ico;*.svg" };
            if (dlg.ShowDialog() == true)
            {
                LogoClientPath = dlg.FileName;
                LogoClientEstSelectionné = true; // Marque comme sélectionné
            }
        }

        private void ChargerLogoEntreprise()
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.ico;*.svg" };
            if (dlg.ShowDialog() == true)
            {
                LogoEntreprisePath = dlg.FileName;
                LogoEntrepriseEstSelectionné = true; // Marque comme sélectionné
            }
        }

        private ObservableCollection<VFFiltreItem> _vfDisponibles = new ObservableCollection<VFFiltreItem>();
        public ObservableCollection<VFFiltreItem> VFDisponibles
        {
            get => _vfDisponibles;
            set
            {
                if (_vfDisponibles != value)
                {
                    _vfDisponibles = value;
                    OnPropertyChanged(nameof(VFDisponibles));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
