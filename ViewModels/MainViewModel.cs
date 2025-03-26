using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TempsAnalyzer.Helpers;
using TempsAnalyzer.Models;
using TempsAnalyzer.Services;

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

        public MainViewModel()
        {
            Saisies = new ObservableCollection<SaisieEntry>();
            ExporterPdfCommand = new RelayCommand(ExporterPdf);
            ChargerLogoClientCommand = new RelayCommand(ChargerLogoClient); // Déclarer la commande ici
            ChargerLogoEntrepriseCommand = new RelayCommand(ChargerLogoEntreprise); // Déclarer la commande ici
            ActualiserVFsCommand = new RelayCommand(ActualiserVFs);
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
            if (dialog.ShowDialog() == true)
            {
                var result = ExcelService.LoadSaisieAvecLibelle(dialog.FileName);

                _saisiesInitiales = new ObservableCollection<SaisieEntry>(result);

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

        private void Filtrer()
        {
            if (_saisiesInitiales == null) return;

            var filtresClients = ClientsDisponibles
                .Where(c => c.IsChecked)
                .Select(c => c.NomClient)
                .ToHashSet();

            var filtresTypes = TypesDisponibles
                .Where(t => t.IsChecked)
                .Select(t => t.Code)
                .ToHashSet();

            var result = _saisiesInitiales
                .Where(s => filtresClients.Contains(s.NomClient) && filtresTypes.Contains(s.Code));

            if (DateDebut.HasValue && DateFin.HasValue)
            {
                result = result.Where(s => s.Date.Date >= DateDebut.Value.Date && s.Date.Date <= DateFin.Value.Date);
            }

            var vfsFiltrées = result.Select(s => s.VF).Distinct().OrderBy(vf => vf).ToList();

            if (VFDisponibles.Count == 0 || !VFDisponibles.Select(v => v.VF).OrderBy(v => v).SequenceEqual(vfsFiltrées))
            {
                VFDisponibles = new ObservableCollection<VFFiltreItem>(
                    vfsFiltrées.Select(vf => new VFFiltreItem { VF = vf, IsChecked = true })
                );
            }

            var vfCochées = VFDisponibles.Where(v => v.IsChecked).Select(v => v.VF).ToHashSet();
            result = result.Where(s => vfCochées.Contains(s.VF));

            if (DateDebut.HasValue && DateFin.HasValue)
            {
                var regroupéParSemaineEtVF = result
                    .GroupBy(s => new { Semaine = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday), s.VF })
                    .Select(g => new { Semaine = g.Key.Semaine, VF = g.Key.VF, Saisies = g })
                    .OrderBy(g => g.Semaine)
                    .ThenBy(g => g.VF);

                var regroupé = regroupéParSemaineEtVF.SelectMany(g => g.Saisies
                    .GroupBy(s => new { s.VF, s.NomClient, s.Code, s.Libelle })
                    .Select(s => new SaisieEntry
                    {
                        VF = s.Key.VF,
                        NomClient = s.Key.NomClient,
                        Code = s.Key.Code,
                        Libelle = s.Key.Libelle,
                        Temps = Math.Round(s.Sum(x => x.Temps), 2),
                        Date = s.First().Date
                    })
                );

                Saisies = new ObservableCollection<SaisieEntry>(regroupé);
            }
            else
            {
                Saisies = new ObservableCollection<SaisieEntry>(
                    result.Select(s => { s.Temps = Math.Round(s.Temps, 2); return s; })
                );
            }

            OnPropertyChanged(nameof(Saisies));
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
            PdfExporter.LogoClientPath = LogoClientPath;
            PdfExporter.LogoEntreprisePath = LogoEntreprisePath;

            PdfExporter.GenererPdf(Saisies.ToList(), InclureGraphiques, InclureTableaux);
        }

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
