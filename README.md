# ⏱ TempsAnalyzer

**TempsAnalyzer** est une application WPF (Windows Presentation Foundation) permettant d’analyser et de visualiser les temps de travail à partir de fichiers Excel. Elle propose des fonctionnalités avancées de filtrage, d’exportation PDF esthétique avec graphiques, et de gestion dynamique des données.

---

## 🧩 Fonctionnalités

- 📥 **Chargement d’un fichier Excel (.xlsx ou .xlsm)**
- 📊 **Affichage des données dans un tableau structuré**
- 🔍 **Filtres dynamiques** :
  - Par **client**
  - Par **type d’activité**
  - Par **période (date de début / fin)**
  - Par **VF (et désignation associée)**
- 🧾 **Exportation au format PDF avec** :
  - Logos entreprise/client personnalisables
  - Graphiques hebdomadaires (jours / heures)
  - Tableaux récapitulatifs
- 🖼 **Support de l’ajout de logos**
- ✅ **Compatibilité avec les formats d’heures fractionnaires (ex : 1.25 = 1 jour = 8h)**
- 📅 **Groupement automatique par semaine**
- 🔄 **Bouton de réinitialisation des VFs disponibles**
- 📈 **Affichage total du temps travaillé (jours + heures) en haut à droite de l’application**

---

## 📁 Structure du Projet

TempsAnalyzer/
│
├── Models/
│   ├── SaisieEntry.cs           # Modèle principal des données
│   ├── ClientFiltreItem.cs      # Classe pour filtrage client
│   ├── ActiviteFiltreItem.cs    # Classe pour filtrage type
│   └── VFFiltreItem.cs          # Classe pour filtrage VF
│
├── Services/
│   └── ExcelService.cs          # Lecture du fichier Excel
│
├── Helpers/
│   └── PdfExporter.cs           # Génération de PDF avec QuestPDF
│
├── ViewModels/
│   └── MainViewModel.cs         # Logique principale + filtrage
│
├── Views/
│   └── MainWindow.xaml          # Interface graphique principale
│
├── Resources/
│   └── logo-defaults.png        # Logos par défaut (optionnels)
│
├── App.xaml / App.xaml.cs       # Fichier d’entrée
└── TempsAnalyzer.csproj         # Fichier de projet

---

## 🔧 Installation et Lancement

### Prérequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

1. Télécharger la Release
2. Ouvrir l'executable

---

## ✅ Utilisation

1. Cliquez sur **📄 Charger Fichier Excel**
2. Sélectionnez un fichier contenant :
   - Feuille **Saisie** (avec colonnes : date, code, produit, client, VF, temps…)
   - Feuille **Activités** (avec colonnes : code, libellé)
3. Utilisez les filtres (client, type, date, VF)
4. Cliquez sur **📑 Exporter PDF** pour générer un rapport

---

## 📌 Format attendu des fichiers Excel

### Feuille `Saisie`

| Date       | Code | Temps | Produit | Client | VF   |
|------------|------|-------|---------|--------|------|
| 12/03/2024 | AAA  | 125   | P001    | ClientX| VF01 |

> **Remarque :** Le champ `Temps` est en centièmes de jour (ex: `125` = `1,25` jours)

### Feuille `Activités`

| Code | Libellé activité            |
|------|-----------------------------|
| AAA  | Rédaction de documentation  |
| BBB  | Développement technique     |

---

## 📦 Export PDF

L’export comprend :

- En-tête avec logos
- Page d’intro avec date
- Pages hebdomadaires :
  - Tableau récapitulatif par VF
  - Graphiques : jours & heures
- Totaux arrondis
- Format automatique pour chaque section

---

## 🎨 Personnalisation

- Logos client et entreprise modifiables à l'aide des boutons dédiés
- Possibilité d’activer/désactiver graphiques et/ou tableaux avant export

---

## 🛠 Commandes Personnalisées

- `ExporterPdfCommand` → Génération du PDF
- `ChargerLogoClientCommand` / `ChargerLogoEntrepriseCommand`
- `ActualiserVFsCommand` → Recharge la liste des VFs visibles

---

## ❗ Problèmes connus

- Les champs Excel doivent respecter un format bien précis.
- Les codes d’activités sans libellé apparaissent comme "Inconnu".
- Si aucun filtre n’est actif, tous les éléments sont affichés par défaut.

---

## ✍️ Auteurs

Développé par **[Antunes Rodrigue]**  
© 2024-2025 - Tous droits réservés

---

## 🖼️ Aperçu

![image_2025-03-26_164735490](https://github.com/user-attachments/assets/99cce7ce-bd8a-46fb-86d6-ee5f56ee838e)

---

## 📃 Licence

Ce projet est sous licence MIT 

```
