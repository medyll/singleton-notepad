# SingletonNotepad

**One place for all notes.**

A minimalist Windows 11 desktop app for rapid note-taking, anchored on a single Markdown file. Combats note fragmentation with LLM-driven normalization of content using user-defined rules.

## Features

- **Single-file discipline:** Reads/writes only `MY_SINGLETON_NOTEPAD.md`
- **Auto-save:** 2s debounce, no manual saving needed
- **LLM normalization:** Restructure notes using custom rules (Ollama, OpenAI, Anthropic, OpenAI-compatible)
- **Inline diff preview:** ProseMirror decorations inside the TipTap editor — see changes before applying normalization
- **Change tracking:** Automatic backup (versioned) and memory log (`NOTEPAD_SINGLETON_MEMORY.md`)
- **LLM chat:** Floating (FAB) or split chat panel, context-aware (selection or full note)
- **Skills:** Auto-detected skill files silently injected into chat/normalization prompts; manual `/skill` invocation
- **Skills & settings:** Dynamic model selector, spell check with language auto-detect, DPAPI-encrypted API keys
- **Native Win11 feel:** Fluent Design, MSIX-packaged

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI | WinUI 3 / Windows App SDK **2.0.1** (MSIX packaged, single-project) |
| Runtime | **.NET 10** — `net10.0-windows10.0.26100.0` |
| Min Windows | `10.0.17763.0` |
| Editor | **TipTap/ProseMirror** in WebView2 (esbuild bundle in `Assets/Editor/`, build tooling in `_tiptap-build/`) |
| MVVM | CommunityToolkit.Mvvm **8.4.2** (source generators) |
| DI | Microsoft.Extensions.DependencyInjection **8.0.1** |
| Diff | DiffPlex **1.9.0** |
| Markdown | Markdig **0.38.0** |
| Key storage | System.Security.Cryptography.ProtectedData **8.0.0** (DPAPI) |
| Tests | MSTest **4.0.2** |

## Current Status

**Phase:** Sprint 6 — Editor Quality & UX Refactor — **in progress** (75% overall)

**Completed:** Sprints 1–5 (MVP, LLM normalization, polish, backups, chat & model UX) + S6-01..S6-04, S6-07, S6-08 (inline ProseMirror diff, dead-code sweep, TipTap bundle audit, chat float/split modes, skills integration). Build green, **83/83 tests passing**.

**Next:** S6-05 — ChatBubble UX polish (loading indicator, user/AI message styling, clear confirmation, error InfoBar). Then S6-06 — NormalizeRateLimitSeconds slider in Settings.

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
├── SingletonNotepad\                 ← WinUI3 project + solution folder
│   ├── SingletonNotepad.slnx         ← solution (slnx, explicit x64/x86/ARM64 platforms)
│   ├── SingletonNotepad.csproj
│   ├── App.xaml(.cs)                 ← DI bootstrap, named Mutex single-instance
│   ├── MainWindow.xaml(.cs)          ← AppWindow positioning, monitor logic
│   ├── MainPage.xaml(.cs)            ← CommandBar + WebView2 editor + StatusBar + split chat
│   ├── Package.appxmanifest
│   ├── app.manifest
│   ├── Assets/
│   │   └── Editor/                   ← editor.html + tiptap.bundle.js (WebView2 TipTap editor)
│   ├── Core/
│   │   ├── Models/                   ← AppSettings, NormalizationRule/Result, ChangeRecord, ChatMessage, SkillDefinition, DiffPayload
│   │   ├── Services/                 ← FileService, SettingsService, NormalizationService, MemoryTrackerService, ChatService, ModelService, SkillService, LanguageDetector, ProviderDetectionService
│   │   ├── Providers/                ← ILlmProvider + Ollama/OpenAi/Anthropic/OpenAiCompatible + LlmProviderSelector
│   │   └── Helpers/                  ← MonitorHelper
│   ├── ViewModels/                   ← MainViewModel, SettingsViewModel, ChatViewModel
│   ├── Views/
│   │   ├── SettingsPage.xaml(.cs)
│   │   ├── Converters/
│   │   └── Controls/
│   │       └── ChatBubble.xaml(.cs)  ← floating chat panel (FAB + expanded)
│   └── Resources/
│       └── DefaultRules.md
├── SingletonNotepad.Tests\           ← MSTest (compiles Core sources via <Compile> links)
├── _tiptap-build\                    ← esbuild tooling for tiptap.bundle.js (see BUILDING.md)
└── bmad\                             ← BMAD artifacts (PRD, architecture, sprints, stories)
```

## Build

> ⚠️ **Cibler le `.csproj` directement** — la `.slnx` ne propage pas `-p:Platform` et génère "ProcessorArchitecture neutral".

```bash
# Depuis SingletonNotepad/
dotnet build SingletonNotepad.csproj -p:Platform=x64

# Tests
dotnet test ..\SingletonNotepad.Tests\SingletonNotepad.Tests.csproj -p:Platform=x64
```

Même info dans : `SingletonNotepad.csproj` (commentaire haut) · `SingletonNotepad.slnx` (commentaire) · `bmad/status.md` · `bmad/artifacts/docs/ARCHITECTURE.md`

## Roadmap

| Sprint | Goal | Status |
|--------|------|--------|
| 1 | MVP Foundation (file I/O, settings, window, single-instance, UI shell) | ✅ |
| 2 | LLM Normalization (Ollama provider, diff preview, MEMORY tracking) | ✅ |
| 3 | Polish (Markdown highlighting, OpenAI/Anthropic, DPAPI keys, theme) | ✅ |
| 4 | Advanced (versioned backups, rules editor, release candidate) | ✅ |
| 5 | Chat & Model UX (floating chat, model selector, spell check) | ✅ |
| 6 | Editor Quality & UX Refactor (ProseMirror inline diff, chat float/split, skills) | 🔄 in progress |

## License

MIT
