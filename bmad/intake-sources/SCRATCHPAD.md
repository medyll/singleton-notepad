# Singleton Notepad — Spécifications Techniques

> Application WinUI 3 minimaliste pour la prise de notes rapide, centrée sur un fichier unique avec normalisation automatique par LLM externe.

**Philosophie :** "One place for all notes"

---

## Table des Matières

1. [Vision](#1-vision)
2. [Fonctionnalités](#2-fonctionnalités)
3. [Architecture Technique](#3-architecture-technique)
4. [Expérience Utilisateur](#4-expérience-utilisateur)
5. [Intégration LLM](#5-intégration-llm)
6. [Structure de Projet](#6-structure-de-projet)
7. [Considérations Techniques](#7-considérations-techniques)
8. [Roadmap](#8-roadmap)
9. [Notes de Développement](#9-notes-de-développement)
10. [Décisions d'Architecture](#10-décisions-darchitecture)
11. [Questions Ouvertes](#11-questions-ouvertes)

---

## 1. Vision

Application Windows WinUI 3 pour la prise de notes rapide, centrée sur **un seul fichier Markdown** (`MY_SINGLETON_NOTEPAD.md`) avec réorganisation automatique via LLM.

**Objectif :** Fini la dispersion des notes dans multiples fichiers et applications.

---

## 2. Fonctionnalités

### 2.1 Édition de Fichier Unique
- **Fichier cible** : `MY_SINGLETON_NOTEPAD.md` (nom configurable)
- **Lecture/écriture exclusive** : L'application ne manipule QUE ce fichier
- **Chemin configurable** : Stocké dans les paramètres locaux
- **Support Markdown** : Coloration syntaxique, preview optionnelle

### 2.2 Normalisation Automatique par LLM
- **Fichier de règles** : `NOTEPAD_SINGLETON_AGENTS.md`
  - Instructions de réarrangement pour le LLM
  - Exemple : organiser par date, priorités, tags
- **Déclenchement** :
  - Manuel : bouton "Normaliser" dans l'UI
  - Automatique : à la fermeture, ou après inactivité (configurable)
- **Portée** :
  - **Document entier** : Normalise tout le contenu
  - **Sélection uniquement** : L'utilisateur sélectionne un extrait → normalisation ciblée
- **Providers** : Ollama (local), OpenAI, Anthropic

### 2.3 Tracking des Changements
- **Fichier mémoire** : `NOTEPAD_SINGLETON_MEMORY.md`
  - Historique des modifications (timestamps, aperçu)
  - Backups avant normalisation
  - Fréquence de snapshot : configurable

### 2.4 Positionnement Fenêtre
- **Moniteur principal uniquement**
- **Position persistante** : Coordonnées X, Y et dimensions sauvegardées
- **Single-instance** : Mutex pour empêcher 2 instances

---

## 3. Architecture Technique

### 3.1 Structure du Projet

```
SingletonNotepad/
├── App.xaml.cs                 # Point d'entrée, configuration DI
├── MainWindow.xaml.cs          # Fenêtre principale, positionnement
├── Core/
│   ├── Services/
│   │   ├── IFileService.cs
│   │   ├── FileService.cs
│   │   ├── INormalizationService.cs
│   │   ├── NormalizationService.cs
│   │   ├── IMemoryTrackerService.cs
│   │   ├── MemoryTrackerService.cs
│   │   └── ISettingsService.cs
│   ├── Models/
│   │   ├── AppSettings.cs
│   │   ├── NormalizationRule.cs
│   │   └── ChangeRecord.cs
│   └── Helpers/
│       ├── MonitorHelper.cs
│       └── PathHelper.cs
├── Views/
│   ├── MainView.xaml
│   ├── SettingsView.xaml
│   └── Controls/
│       ├── MarkdownEditor.xaml
│       └── NormalizationPreview.xaml
└── Resources/
    └── DefaultRules.md
```

### 3.2 Stack Technique

| Composant | Technologie |
|-----------|-------------|
| Framework | WinUI 3 (Windows App SDK) |
| Language | C# |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Parsing Markdown | Markdig |
| Diff Preview | DiffPlex |
| Tests | MSTest + Playwright |

### 3.3 Références UI — Notepad Windows 11

**Composants WinUI 3 à utiliser :**

```csharp
// Menu principal (en haut)
<MenuBar>
    <MenuFlyoutItem Text="Édition" />
    <MenuFlyoutItem Text="Affichage" />
    <MenuFlyoutItem Text="Paramètres" />
</MenuBar>

// Toolbar (boutons alignés à gauche)
<CommandBar DefaultLabelPosition="Right">
    <AppBarButton Icon="Open" Label="Ouvrir" />
    <AppBarButton Icon="Save" Label="Enregistrer" />
    <AppBarButton Icon="Refresh" Label="Normaliser" />
    <AppBarButton Icon="Copy" Label="Copie" />
</CommandBar>

// Content area (éditeur)
<Grid>
    <TextBox 
        AcceptsReturn="True"
        TextWrapping="NoWrap"
        FontFamily="Cascadia Code"
        IsSpellCheckEnabled="False"
    />
</Grid>

// Status bar (en bas)
<StatusBar>
    <StatusBarItem>UTF-8</StatusBarItem>
    <StatusBarItem>Ligne 12, Col 34</StatusBarItem>
    <StatusBarItem>Sync</StatusBarItem>
    <StatusBarItem>Normalisé: 14:32</StatusBarItem>
</StatusBar>
```

**Contrôles Settings :**

```csharp
// Navigation verticale (NavigationView)
<NavigationView PaneDisplayMode="Left">
    <NavigationViewItem Content="Apparence" />
    <NavigationViewItem Content="Fichiers" />
    <NavigationViewItem Content="Normalisation" />
    <NavigationViewItem Content="Historique" />
</NavigationView>

// ToggleSwitch pour options
<ToggleSwitch Header="Word wrap" IsOn="{x:Bind ViewModel.WordWrap}" />

// ComboBox pour sélections
<ComboBox Header="Thème" SelectedIndex="{x:Bind ViewModel.ThemeIndex}">
    <ComboBoxItem Content="Clair" />
    <ComboBoxItem Content="Sombre" />
    <ComboBoxItem Content="Système" />
</ComboBox>
```

**Palette de couleurs (Fluent Design) :**

```csharp
// Couleurs de référence (Notepad Windows 11)
Application.Current.Resources["CardBackgroundFillColorDefault"] = "#20202020";
Application.Current.Resources["CardStrokeFillColorDefault"] = "#F0F0F0F0";
Application.Current.Resources["AccentFillColorDefault"] = "#60CDFF"; // Bleu Windows 11
```

### 3.3 Stockage des Paramètres

```csharp
// LocalSettings (ApplicationData.Current.LocalSettings)
{
  "SingletonFilePath": "C:\\Users\\...\\MY_SINGLETON_NOTEPAD.md",
  "AgentsFilePath": "C:\\Users\\...\\NOTEPAD_SINGLETON_AGENTS.md",
  "MemoryFilePath": "C:\\Users\\...\\NOTEPAD_SINGLETON_MEMORY.md",
  "WindowPositionX": 100,
  "WindowPositionY": 100,
  "WindowWidth": 800,
  "WindowHeight": 600,
  "LlmProvider": "Ollama",
  "LlmEndpoint": "http://localhost:11434/api/generate",
  "LlmModel": "llama3.1",
  "LlmApiKey": "",
  "AutoNormalizeOnClose": true,
  "AutoNormalizeIdleMinutes": 5,
  "SnapshotFrequency": "OnNormalize"
}
```

### 3.4 Format des Fichiers

**MY_SINGLETON_NOTEPAD.md** :
```markdown
# Notes

## 2025-01-15
- Idée projet X
- Réunion à 14h

## Tasks
- [ ] Faire Y
- [x] Terminé Z
```

**NOTEPAD_SINGLETON_AGENTS.md** :
```markdown
# Normalization Rules

## Objective
Réorganiser le contenu par date décroissante, puis par catégorie.

## Instructions
1. Extraire les sections avec dates (YYYY-MM-DD)
2. Trier par date décroissante
3. Regrouper tâches non-datées dans "## Inbox"
4. Conserver les checkboxes intactes
5. Ajouter timestamp de normalisation
```

**NOTEPAD_SINGLETON_MEMORY.md** :
```markdown
# Change History

## 2025-01-15 14:32:00
- Type: ManualEdit
- LinesChanged: 3
- Preview: "Ajout tâche dans section Tasks"

## 2025-01-15 18:00:00
- Type: AutoNormalize
- LinesChanged: 15
- Preview: "Réorganisation par date"
- BackupRef: backup_20250115_180000.md
```

---

## 4. Expérience Utilisateur

### 4.1 Démarrage
- **Cold start** : < 2 secondes
- **Premier lancement** : Wizard de configuration (choix des chemins)
- **Lancements suivants** : Ouverture directe avec dernier fichier

### 4.2 Interface Principale — Inspiration Notepad Windows 11

**Design System :** WinUI 3 Fluent Design (mêmes codes visuels que Notepad Windows 11)

**Structure de la fenêtre (sans les onglets) :**

```
┌──────────────────────────────────────────────────────────────┐
│ [←]  Singleton Notepad       │  Édition  │  Affichage  │ ⚙️ │
├──────────────────────────────────────────────────────────────┤
│  [📁 Ouvrir]  [💾 Enregistrer]  [🔄 Normaliser]  [📋 Copie] │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│   # Titre                                                   │
│                                                              │
│   Contenu éditable                                          │
│   - Coloration syntaxique Markdown                          │
│   - Numérotation des lignes (optionnelle)                  │
│   - Indentation guidée                                      │
│                                                              │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│  UTF-8  │  Ligne 12, Col 34  │  Sync  │  Normalisé: 14:32  │
└──────────────────────────────────────────────────────────────┘
```

**Menu principal (Navigation interne) :**

| Menu | Sous-menu | Action |
|------|-----------|--------|
| **Édition** | Rechercher, Remplacer, Annuler/Rétablir | Actions d'édition |
| **Affichage** | Zoom, Word wrap, Numérotation | Options d'affichage |
| **⚙️ Paramètres** | → Navigation vers SettingsView | Ouvre les paramètres |

**Barre d'outils (Toolbar) — Alignée à gauche :**

| Icône | Action | Raccourci |
|-------|--------|-----------|
| `📁 Ouvrir` | Ouvrir un fichier | `Ctrl+O` |
| `💾 Enregistrer` | Sauvegarder | `Ctrl+S` |
| `🔄 Normaliser` | Normaliser (LLM) | `Ctrl+N` |
| `📋 Copie` | Copie le contenu | `Ctrl+C` |

**Barre de statut (Status Bar) — En bas :**

| Section | Information |
|---------|-------------|
| Encodage | `UTF-8` |
| Position | `Ligne 12, Col 34` |
| État | `Sync` / `Non enregistré` |
| Normalisation | `Normalisé: 14:32` |

### 4.3 Navigation Interne — Paramètres

**Structure SettingsView (inspirée Notepad Windows 11) :**

```
┌──────────────────────────────────────────────┐
│ ← Paramètres                               │
├──────────────────────────────────────────────┤
│ Apparence                                  │
│  ├─ Thème (Clair / Sombre / Système)      │
│  ├─ Police (nom, taille)                   │
│  └─ Word wrap (On/Off)                     │
├──────────────────────────────────────────────┤
│ Fichiers                                   │
│  ├─ Chemin MY_SINGLETON_NOTEPAD.md         │
│  ├─ Chemin AGENTS.md                       │
│  └─ Chemin MEMORY.md                       │
├──────────────────────────────────────────────┤
│ Normalisation                              │
│  ├─ Provider LLM (Ollama/OpenAI/Anthropic) │
│  ├─ Endpoint & Modèle                      │
│  ├─ Auto-normaliser à la fermeture         │
│  └─ Délai inactivité (minutes)             │
├──────────────────────────────────────────────┤
│ Historique                                 │
│  ├─ Fréquence snapshots                    │
│  └─ Ouvrir l'historique (MEMORY.md)        │
└──────────────────────────────────────────────┘
```

### 4.4 Feedback Utilisateur

| État | Indicateur |
|------|------------|
| **Auto-save** | Status bar: `Sync` (vert) / `Non enregistré` (orange) |
| **Normalisation** | Dialog : Progress + Preview diff (avant/après) |
| **Erreurs** | Toast notification (fichier verrouillé, LLM indisponible) |

### 4.5 Raccourcis Clavier

| Raccourci | Action |
|-----------|--------|
| `Ctrl+O` | Ouvrir fichier |
| `Ctrl+S` | Sauvegarder |
| `Ctrl+N` | Normaliser |
| `Ctrl+Q` | Quitter |
| `F5` | Rafraîchir (reload depuis disque) |
| `Ctrl+,` | Ouvrir paramètres |

---

## 5. Intégration LLM

### 5.1 Flow de Normalisation

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│ Utilisateur │────▶│ App WinUI    │────▶│ LLM Service │
│  clique 🔄  │     │ (prépare     │     │ (construit  │
└─────────────┘     │  le prompt)  │     │  la requête)│
                    └──────────────┘     └─────────────┘
                                              │
                    ┌──────────────┐          │
                    │ Affiche      │◀─────────┘
                    │ preview diff │
                    └──────────────┘
                          │
                    ┌─────▼──────┐     ┌─────────────┐
                    │ Utilisateur│────▶│ Écrit dans  │
                    │  valide    │     │  fichier    │
                    └────────────┘     └─────────────┘
```

### 5.2 Construction du Prompt

**Mode Document Entier :**

```csharp
var prompt = $@"
You are a Markdown Normalization Assistant.

## Rules (from NOTEPAD_SINGLETON_AGENTS.md):
{normalizationRulesContent}

## Current Content (from MY_SINGLETON_NOTEPAD.md):
{currentFileContent}

## Task
Apply the normalization rules to reorganize the content.
Return ONLY the normalized Markdown content, no explanation.
";
```

**Mode Sélection :**

```csharp
var prompt = $@"
You are a Markdown Normalization Assistant.

## Rules (from NOTEPAD_SINGLETON_AGENTS.md):
{normalizationRulesContent}

## Selected Text to Normalize:
{selectedText}

## Context (surrounding content):
{surroundingContext}

## Task
Apply the normalization rules ONLY to the selected text.
Return ONLY the normalized selection, preserve surrounding context.
Mark the normalized section with <!-- NORMALIZED --> comment.
";
```

### 5.3 UI — Bouton Normaliser (2 modes)

```
┌─────────────────────────────────────────┐
│  [🔄] Normaliser                        │
├─────────────────────────────────────────┤
│  → Normaliser tout le document          │
│  → Normaliser la sélection              │
└─────────────────────────────────────────┘
```

**Comportement :**
- **Aucune sélection** : Le bouton agit sur tout le document
- **Texte sélectionné** : Le bouton propose les 2 options (menu flyout)
- **Raccourci `Ctrl+N`** : Ouvre le menu flyout si sélection, sinon normalise tout

### 5.4 Preview Diff — Intégré dans l'Éditeur

**Principe :** Le diff s'affiche directement dans l'éditeur (pas de nouvelle fenêtre), avec coloration inline.

**Mode Lecture Diff (après normalisation) :**

```
┌────────────────────────────────────────────────────────────┐
│  [✓] Appliquer   [✗] Annuler   [⚙️] Options               │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  # Notes                                                   │
│                                                            │
│  ## 2025-01-15                                             │
│  - Idée projet X                                           │
│  - Réunion à 14h                                           │
│                                                            │
│ ┌────────────────────────────────────────────────────────┐ │
│ │ ## Tasks  ← inchangé                                   │ │
│ │ - [x] Terminé Z  ← inchangé                            │ │
│ │ - [ ] Tâche A  ← ajouté (vert)                         │ │
│ │ - [ ] Tâche B  ← ajouté (vert)                         │ │
│ │ - Ancienne tâche  ← supprimé (rouge barré)             │ │
│ └────────────────────────────────────────────────────────┘ │
│                                                            │
│  ## Inbox                                                  │
│  - Note rapide                                             │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Coloration Inline :**

| Type | Affichage |
|------|-----------|
| **Ligne ajoutée** | Fond vert clair (`#E8F5E9`), texte vert foncé |
| **Ligne supprimée** | Fond rouge clair (`#FFEBEE`), texte barré rouge |
| **Ligne modifiée** | Fond orange clair, avec diff mot-à-mot |
| **Inchangé** | Fond normal, texte normal |

**Implémentation WinUI 3 :**

```csharp
// RichTextBlock avec Inlines colorées
foreach (var line in diff.Lines)
{
    var paragraph = new Paragraph();
    
    switch (line.Type)
    {
        case ChangeType.Inserted:
            paragraph.Background = new SolidColorBrush(Colors.LightGreen);
            break;
        case ChangeType.Deleted:
            paragraph.Background = new SolidColorBrush(Colors.LightCoral);
            paragraph.TextDecorations = TextDecorations.Strikethrough;
            break;
    }
    
    paragraph.Inlines.Add(new Run { Text = line.Text });
    DiffView.Blocks.Add(paragraph);
}
```

**Flow Utilisateur :**

1. Utilisateur clique sur `🔄 Normaliser`
2. LLM génère le contenu normalisé en background
3. **L'éditeur bascule en mode "Preview Diff"** (même fenêtre, même emplacement)
4. L'utilisateur voit les changements colorés inline
5. Boutons `[✓] Appliquer` / `[✗] Annuler` en haut de l'éditeur
6. Si Appliquer → écriture fichier + retour mode édition normal
7. Si Annuler → retour mode édition normal (contenu original)

**Avantages :**
- ✅ Pas de rupture de contexte (reste dans le même éditeur)
- ✅ Vision immédiate avant/après (inline)
- ✅ Plus rapide (pas de nouvelle fenêtre à ouvrir)
- ✅ Moins de confusion (une seule vue)
┌──────────────────────────────────────────────────────┐
│  Normalisation — Sélection (lignes 12-24)           │
├──────────────────────────────────────────────────────┤
│  ┌─────────────────┬───────────────────────────────┐ │
│  │   AVANT         │        APRÈS                  │ │
│  ├─────────────────┼───────────────────────────────┤ │
│  │ - Tâche A       │ ## Tasks                      │ │
│  │ - Tâche B       │ - [ ] Tâche A                 │ │
│  │ Inbox           │ - [ ] Tâche B                 │ │
│  │                 │                               │ │
│  └─────────────────┴───────────────────────────────┘ │
├──────────────────────────────────────────────────────┤
│  [Annuler]              [Appliquer à la sélection]  │
└──────────────────────────────────────────────────────┘
```

### 5.5 Providers Supportés

**Ollama (Local - Recommandé)** :
```csharp
var request = new {
  model = "llama3.1",
  prompt = prompt,
  stream = false
};
// POST http://localhost:11434/api/generate
```

**OpenAI** :
```csharp
var request = new {
  model = "gpt-4o-mini",
  messages = [{ role = "user", content = prompt }],
  temperature = 0.3
};
// POST https://api.openai.com/v1/chat/completions
```

**Anthropic** :
```csharp
var request = new {
  model = "claude-sonnet-4-20250514",
  max_tokens = 4096,
  messages = [{ role = "user", content = prompt }]
};
// POST https://api.anthropic.com/v1/messages
```

---

## 6. Structure de Projet

### 6.1 Dependencies (Package.appxmanifest)

```xml
<Package>
  <Capabilities>
    <Capability Name="internetClient" />
    <uap:Capability Name="broadFileSystemAccess" />
  </Capabilities>
</Package>
```

### 6.2 NuGet Packages

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.x" />
<PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.x" />
<PackageReference Include="Markdig" Version="3.x" />
<PackageReference Include="DiffPlex" Version="1.9.0" />
```

### 6.3 Gestion des Erreurs

| Exception | Traitement |
|-----------|------------|
| FileIOException | Retry avec backoff exponentiel (3 tentatives) |
| HttpRequestException | Timeout 30s, notification utilisateur |
| UnauthorizedAccessException | Demande permission via toast |

---

## 7. Considérations Techniques

### 7.1 Performance
- **Lazy loading** : Ne charger le fichier qu'à l'ouverture
- **Background normalization** : Task async pour ne pas bloquer l'UI
- **Debounce** : Auto-save après 2s d'inactivité

### 7.2 Sécurité
- **API Keys** : Stockées dans LocalSettings (chiffrées si sensible)
- **File permissions** : Vérifier accès en lecture/écriture au startup
- **Validation LLM** : Sanitiser la réponse avant écriture

### 7.3 Edge Cases
- **Fichier inexistant** : Créer automatiquement avec template vide
- **Fichier modifié externally** : Detect via FileSystemWatcher, proposer reload
- **LLM retourne contenu invalide** : Validation Markdown basique avant apply
- **Multi-instance** : Mutex pour empêcher 2 instances

### 7.4 Accessibilité
- Support clavier complet (tab order, shortcuts)
- Contraste couleurs conforme WCAG
- Screen reader friendly (AutomationProperties)

---

## 8. Roadmap

### Phase 1 — MVP
- [ ] Lecture/écriture fichier unique
- [ ] Positionnement fenêtre persistant
- [ ] Settings basiques (chemins)
- [ ] Auto-save

### Phase 2 — LLM
- [ ] Intégration Ollama
- [ ] Fichier de règles normalisation
- [ ] Preview diff avant apply
- [ ] Tracking mémoire (MEMORY.md)

### Phase 3 — Polish
- [ ] Coloration syntaxique Markdown
- [ ] Raccourcis clavier
- [ ] Notifications toast
- [ ] Support multi-providers (OpenAI, Anthropic)

### Phase 4 — Advanced
- [ ] FileSystemWatcher pour changements externes
- [ ] Backup automatique versionné
- [ ] Plugin system pour règles custom
- [ ] Sync cloud optionnelle (OneDrive, Dropbox)

---

## 9. Notes de Développement

### 9.1 Positionnement Fenêtre

```csharp
public void RestoreWindowPosition(Window window)
{
  var settings = ApplicationData.Current.LocalSettings;
  
  if (settings.Values.TryGetValue("WindowPositionX", out var x) &&
      settings.Values.TryGetValue("WindowPositionY", out var y))
  {
    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
    var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTOPRIMARY);
    
    if (IsPositionOnMonitor((int)x, (int)y, monitor))
    {
      SetWindowPos(hwnd, IntPtr.Zero, (int)x, (int)y, 0, 0, 
                   SWP_NOSIZE | SWP_NOZORDER);
      return;
    }
  }
  
  CenterOnPrimaryMonitor(window);
}
```

### 9.2 Mutex Single-Instance

```csharp
private static Mutex _mutex;

public App()
{
  _mutex = new Mutex(true, "SingletonNotepadInstance", out bool createdNew);
  
  if (!createdNew)
  {
    BringExistingInstanceToFront();
    Exit();
  }
  
  InitializeComponent();
}
```

---

## 10. Décisions d'Architecture

| Décision | Rationale |
|----------|-----------|
| WinUI 3 | Natif Windows, moderne, support long terme |
| LocalSettings | Simple, pas de dépendance externe |
| Ollama en premier | Gratuit, local, pas de API key requise |
| Fichier unique | Philosophie "one place for all notes" |
| Normalisation manuelle + auto | Contrôle utilisateur + commodité |
| Preview avant apply | Confiance, éviter surprises LLM |

---

## 11. Questions Ouvertes

1. **Format du fichier MEMORY** : Markdown lisible ou JSON pour parsing ?
2. **Fréquence max de normalisation auto** : Limiter pour éviter API costs ?
3. **Taille max fichier** : Avertir au-delà de X lignes ?
4. **Support images dans Markdown** : Base64 inline ou liens externes ?
5. **Export/Import** : Permettre migration vers d'autres apps ?
