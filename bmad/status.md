# BMAD Status — singleton-notepad

**Phase:** development · **Progress:** 45% · **Last Updated:** 2026-05-05

## Next Action
**Implement S1-05:** MainPage shell: MenuBar + CommandBar + Editor + StatusBar

## Sprint 1 — MVP Foundation (in_progress)

| Story | Status |
|-------|--------|
| S1-01: Scaffold WinUI 3 + DI + MVVM + named Mutex single-instance | ✅ done |
| S1-02: FileService + auto-create + auto-save (2s debounce) | ✅ done |
| S1-03: SettingsService (JSON in %LocalAppData%\SingletonNotepad\settings.json) | ✅ done |
| S1-04: MainWindow AppWindow positioning (DisplayArea.Primary.WorkArea) | ✅ done |
| S1-05: MainPage shell: MenuBar + CommandBar + Editor + StatusBar | 🔄 next |
| S1-06: SettingsPage shell: NavigationView + Apparence/Fichiers panes | ⬜ pending |
| S1-07: Unit tests: FileService + SettingsService | ⬜ pending |

## Stack

- **UI:** WinUI 3 / Windows App SDK **2.0.1** (MSIX packaged)
- **Runtime:** .NET 10 — `net10.0-windows10.0.26100.0`
- **MVVM:** CommunityToolkit.Mvvm 8.4.2 — `[ObservableProperty]` on **partial properties** (not fields)
- **DI:** Microsoft.Extensions.DependencyInjection 8.0.1
- **Single-instance:** named Mutex `"SingletonNotepad-7B3F9A2C-Instance"` + P/Invoke FindWindow/SetForegroundWindow
- **Settings storage:** JSON file — NOT LocalSettings, NOT PasswordVault
- **Monitor API:** `DisplayArea.Primary.WorkArea` — NOT System.Windows.Forms.Screen
- **Build:** `dotnet build -p:Platform=x64` (platform flag mandatory)

## 3 Dimensions

### Marketing
A minimalist Windows 11 notepad — one file, zero friction, LLM-powered normalization.

### Product
WinUI 3 single-file Markdown editor with auto-save, settings, and LLM reorganization.

### Far Vision
The last note-taking app you'll ever need — one file, self-organizing, always in sync.
