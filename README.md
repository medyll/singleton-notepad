# SingletonNotepad

**One place for all notes.**

A minimalist Windows 11 desktop app for rapid note-taking, anchored on a single Markdown file. Combats note fragmentation with LLM-driven normalization of content using user-defined rules.

## Features (Planned)

- **Single-file discipline:** Reads/writes only `MY_SINGLETON_NOTEPAD.md`
- **Auto-save:** 2s debounce, no manual saving needed
- **LLM normalization:** Restructure notes using custom rules (Ollama, OpenAI, Anthropic)
- **Inline diff preview:** See changes before applying normalization
- **Change tracking:** Automatic backup and memory log
- **Native Win11 feel:** Fluent Design, MSIX-packaged

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI | WinUI 3 / Windows App SDK **2.0.1** (MSIX packaged, single-project) |
| Runtime | **.NET 10** — `net10.0-windows10.0.26100.0` |
| Min Windows | `10.0.17763.0` |
| MVVM | CommunityToolkit.Mvvm **8.3.2** (source generators) |
| DI | Microsoft.Extensions.DependencyInjection **8.0.1** |
| Diff | DiffPlex **1.9.0** |
| Markdown | Markdig **0.40.0** |
| Tests | MSTest |

## Current Status

**Phase:** Sprint 2 — LLM Normalization — **in progress** (74%)

**Completed:** Sprint 1 (MVP: file I/O, settings, window persistence, single-instance, UI shells) + S2-01 (ILlmProvider + OllamaProvider). 16 tests passing.

**Next:** S2-02 — NormalizationService (rules loader, rate-limit, diff computation).

## ⚠️ Critical Build Constraints

> Read before touching the build — these caused hours of debugging.

### 1. Platform flag is mandatory

```bash
dotnet build -p:Platform=x64
```

Never `dotnet build` alone. Without `-p:Platform=x64`:
error MSB: ProcessorArchitecture neutral — WinUI3 requires explicit platform (x86 / x64 / ARM64).

### 2. Rename `FreshRef` → `SingletonNotepad` immediately after scaffold

`dotnet new winui` creates all files with namespace `FreshRef` and `x:Class="FreshRef.*"`.
**Before writing any code**, rename every occurrence in `.xaml` and `.cs` files:
- `namespace FreshRef` → `namespace SingletonNotepad`
- `x:Class="FreshRef.Foo"` → `x:Class="SingletonNotepad.Foo"`
- `xmlns:local="using:FreshRef"` → `xmlns:local="using:SingletonNotepad"`

Mismatch between XAML `x:Class` and `.cs` namespace = **XamlCompiler silent crash** (exit code 1, zero output, no output.json).

### 3. `x:Bind` requires a typed property on the code-behind class

XamlCompiler (net472 binary, Pass 1) resolves `{x:Bind ViewModel.Foo}` at compile time.
`ViewModel` must be a **typed public property** declared in the `.xaml.cs` file.

```csharp
// SettingsPage.xaml.cs
public SettingsViewModel ViewModel { get; }  // ← required for x:Bind to compile
```

A runtime-only property resolved via DI in the constructor is not enough — XamlCompiler reads
the `.cs` source file to find the type. Missing typed property = silent crash.

### 4. `.slnx` solution must declare platforms explicitly

VS 2026 uses `.slnx` format. Without a `<Configurations>` block, VS defaults to `Any CPU`
which does not exist in WinUI3 projects → error on solution load.

Required content:
```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
    <Platform Name="x64" />
    <Platform Name="x86" />
    <Platform Name="ARM64" />
  </Configurations>
  <Project Path="SingletonNotepad\SingletonNotepad.csproj" />
</Solution>
```

### 5. Never add XAML files before their code-behind types exist

XamlCompiler processes all `.xaml` files in the project during Pass 1 — even files not yet
wired into navigation. If a XAML file references a ViewModel type that doesn't compile yet,
XamlCompiler crashes silently. Add XAML + code-behind together, never XAML alone.

## Project Structure

```
D:\development\singleton-notepad\
├── SingletonNotepad\
│   ├── SingletonNotepad.slnx         ← solution (slnx, explicit x64/x86/ARM64 platforms)
│   └── SingletonNotepad\             ← WinUI3 project
│       ├── SingletonNotepad.csproj
│       ├── App.xaml(.cs)             ← namespace SingletonNotepad
│       ├── MainWindow.xaml(.cs)      ← namespace SingletonNotepad
│       ├── MainPage.xaml(.cs)        ← namespace SingletonNotepad
│       ├── Package.appxmanifest
│       ├── app.manifest
│       ├── Assets/
│       ├── Core/
│       │   ├── Models/               ← AppSettings, NormalizationRule, ChangeRecord
│       │   ├── Services/             ← IFileService, ISettingsService, etc.
│       │   ├── Providers/            ← ILlmProvider
│       │   └── Helpers/              ← MonitorHelper, PathHelper
│       ├── ViewModels/               ← MainViewModel, SettingsViewModel
│       ├── Views/
│       │   ├── SettingsPage.xaml(.cs)
│       │   └── Controls/
│       │       ├── MarkdownEditor.xaml(.cs)
│       │       └── InlineDiffEditor.xaml(.cs)
│       └── Resources/
│           └── DefaultRules.md
└── bmad/
```

## Build

```bash
# Always specify platform
dotnet build -p:Platform=x64

# Tests (when test project exists)
dotnet test -p:Platform=x64
```

## Roadmap

| Sprint | Goal |
|--------|------|
| 1 | MVP Foundation (file I/O, settings, window, single-instance, UI shell) |
| 2 | LLM Normalization (Ollama provider, diff preview, MEMORY tracking) |
| 3 | Polish (Markdown highlighting, OpenAI/Anthropic, DPAPI keys, theme) |
| 4 | Advanced (FSWatcher, versioned backups, plugin system) |

## License

MIT
