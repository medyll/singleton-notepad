# Singleton Notepad — Spécifications Techniques

> Application desktop minimaliste pour la prise de notes rapide, centrée sur un fichier unique avec normalisation automatique par LLM externe.

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

Application desktop cross-platform pour la prise de notes rapide, centrée sur **un seul fichier Markdown** (`MY_SINGLETON_NOTEPAD.md`) avec réorganisation automatique via LLM.

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
├── Program.cs                    # Point d'entrée
├── App.axaml / App.axaml.cs      # Application, configuration DI
├── MainWindow.axaml / .axaml.cs  # Fenêtre principale, positionnement
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
│   │   ├── NormalizationResult.cs
│   │   └── ChangeRecord.cs
│   └── Helpers/
│       └── MonitorHelper.cs
└── Views/
    ├── MainView.axaml
    ├── SettingsView.axaml
    ├── ApparenceSettingsPage.axaml
    └── FichiersSettingsPage.axaml
```

### 3.2 Stack Technique

| Composant | Technologie |
|-----------|-------------|
| Framework | Avalonia 11 (cross-platform) |
| Language | C# .NET 8 |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Parsing Markdown | Markdig |
| Diff Preview | DiffPlex |
| DI | Microsoft.Extensions.DependencyInjection |
| Tests | MSTest |

### 3.3 Références UI — Composants Standards

**Composants Avalonia à utiliser :**

```csharp
// Menu principal (en haut)
<Menu>
    <MenuItem Header="Édition">
        <MenuItem Header="Rechercher" InputGesture="Ctrl+F"/>
        <MenuItem Header="Remplacer" InputGesture="Ctrl+H"/>
    </MenuItem>
    <MenuItem Header="Affichage">
        <MenuItem Header="Zoom"/>
        <MenuItem Header="Plein écran"/>
    </MenuItem>
    <MenuItem Header="Paramètres" InputGesture="Ctrl+,"/>
</Menu>

// Toolbar (boutons alignés à gauche)
<StackPanel Orientation="Horizontal">
    <Button Command="{Binding LoadFileCommand}" ToolTip.Tip="Ouvrir"/>
    <Button Command="{Binding SaveFileCommand}" ToolTip.Tip="Enregistrer"/>
    <Button Command="{Binding NormalizeCommand}" ToolTip.Tip="Normaliser"/>
</StackPanel>

// Content area (éditeur)
<Grid>
    <TextBox 
        AcceptsReturn="True"
        TextWrapping="NoWrap"
        FontFamily="Consolas"
    />
</Grid>

// Status bar (en bas)
<Border BorderThickness="0,1,0,0" Padding="8,4">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="UTF-8"/>
        <TextBlock Text="Ligne 12, Col 34"/>
        <TextBlock Text="Sync"/>
    </StackPanel>
</Border>
```

**Contrôles Settings :**

```csharp
// Navigation par onglets
<TabControl>
    <TabItem Header="Apparence">
        <!-- Settings contenu -->
    </TabItem>
    <TabItem Header="Fichiers">
        <!-- Settings contenu -->
    </TabItem>
</TabControl>

// CheckBox pour options
<CheckBox Content="Word wrap" IsChecked="{Binding WordWrap, Mode=TwoWay}"/>

// ComboBox pour sélections
<ComboBox SelectedItem="{Binding FontFamily, Mode=TwoWay}">
    <ComboBoxItem Content="Consolas"/>
    <ComboBoxItem Content="Cascadia Code"/>
</ComboBox>
```

### 3.4 Stockage des Paramètres

```csharp
// JSON file dans AppData
{
  "SingletonFilePath": "C:\\Users\\...\\MY_SINGLETON_NOTEPAD.md",
  "AgentsFilePath": "C:\\Users\\...\\NOTEPAD_SINGLETON_AGENTS.md",
  "WindowPositionX": 100,
  "WindowPositionY": 100,
  "WindowWidth": 800,
  "WindowHeight": 600,
  "LlmProvider": "Ollama",
  "LlmEndpoint": "http://localhost:11434/api/generate",
  "LlmModel": "llama3.1",
  "AutoNormalizeOnClose": true,
  "AutoNormalizeIdleMinutes": 5,
  "Theme": "System",
  "FontFamily": "Consolas",
  "FontSize": 14,
  "WordWrap": true
}
```

### 3.5 Format des Fichiers

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

### 4.2 Interface Principale

**Structure de la fenêtre :**

```
┌──────────────────────────────────────────────────────────────┐
│  Singleton Notepad                                           │
├──────────────────────────────────────────────────────────────┤
│  Édition  │  Affichage  │  Paramètres                        │
├──────────────────────────────────────────────────────────────┤
│  [📁 Ouvrir]  [💾 Enregistrer]  [🔄 Normaliser]  [📋 Copie] │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│   # Titre                                                    │
│                                                              │
│   Contenu éditable                                           │
│   - Coloration syntaxique Markdown                           │
│   - Numérotation des lignes (optionnelle)                    │
│   - Indentation guidée                                       │
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
| **Paramètres** | → Navigation vers SettingsView | Ouvre les paramètres |

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

**Structure SettingsView :**

```
┌──────────────────────────────────────────────┐
│ ← Paramètres                               │
├──────────────────────────────────────────────┤
│ [Apparence]  [Fichiers]                     │
├──────────────────────────────────────────────┤
│                                              │
│ Thème                                        │
│  ○ Clair                                     │
│  ○ Sombre                                    │
│  ● Système                                   │
│                                              │
│ Police                                       │
│  [Consolas ▼]                                │
│  Taille: [14]                                │
│                                              │
│ Éditeur                                      │
│  ☑ Retour automatique à la ligne            │
│  ☐ Afficher les numéros de ligne            │
│  ☑ Surbrillance des parenthèses             │
│                                              │
└──────────────────────────────────────────────┘
```

### 4.4 Feedback Utilisateur

| État | Indicateur |
|------|------------|
| **Auto-save** | Status bar: `Sync` (vert) / `Non enregistré` (orange) |
| **Normalisation** | Toast notification + progression |
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
│ Utilisateur │────▶│  Application │────▶│ LLM Service │
│  clique 🔄  │     │ (prépare     │     │ (construit  │
└─────────────┘     │  le prompt)  │     │  la requête)│
                    └──────────────┘     └─────────────┘
                                              │
                    ┌──────────────┐          │
                    │ Notification │◀─────────┘
                    │ progression  │
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
";
```

### 5.3 Providers Supportés

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

### 6.1 Dependencies

```xml
<PackageReference Include="Avalonia" Version="11.2.1" />
<PackageReference Include="Avalonia.Desktop" Version="11.2.1" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.1" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Markdig" Version="0.34.0" />
<PackageReference Include="DiffPlex" Version="1.9.0" />
```

### 6.2 Gestion des Erreurs

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
- **API Keys** : Stockées dans JSON (chiffrées si sensible)
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
- Screen reader friendly

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
  var settings = _settingsService.GetSettings();
  
  var x = settings.WindowPositionX;
  var y = settings.WindowPositionY;
  
  if (IsValidPosition(x, y))
  {
    window.Position = new PixelPoint((int)x, (int)y);
    return;
  }
  
  // Center on primary monitor if invalid
  window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
}
```

### 9.2 Mutex Single-Instance

```csharp
private static Mutex _mutex;

public static void Main(string[] args)
{
  _mutex = new Mutex(true, "SingletonNotepadInstance", out bool createdNew);
  
  if (!createdNew)
  {
    BringExistingInstanceToFront();
    return;
  }
  
  BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
```

---

## 10. Décisions d'Architecture

| Décision | Rationale |
|----------|-----------|
| Avalonia 11 | Cross-platform (Windows, Linux, macOS), stable |
| JSON settings | Simple, lisible, portable |
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
