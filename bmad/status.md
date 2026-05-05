# Singleton Notepad — Project Status

**Last updated:** 2026-05-05
**Phase:** Sprint 1 — MVP Foundation
**Overall:** NOT STARTED — clean reset (all code deleted 2026-05-05)

---

## Environment

| Item | Value |
|------|-------|
| OS | Windows 11 Pro 10.0.26220 |
| IDE | Visual Studio 2026 |
| Runtime | .NET 10 |
| SDK | Windows App SDK 2.0.1 |
| Solution format | `.slnx` (VS 2026 native) |
| Build command | `dotnet build -p:Platform=x64` |

---

## Sprint 1 Stories

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold WinUI3 project + DI + MVVM wiring | pending | - |
| S1-02 | FileService (load, save, auto-save debounce, watch) | pending | - |
| S1-03 | SettingsService (JSON persistence in %LocalAppData%) | pending | - |
| S1-04 | MonitorHelper + window positioning (DisplayArea API) | pending | - |
| S1-05 | Single-instance guard (named Mutex + P/Invoke) | pending | - |
| S1-06 | MainView shell (MenuBar + CommandBar + Editor + StatusBar) | pending | - |
| S1-07 | SettingsView shell (NavigationView left panes) | pending | - |
| S1-08 | Unit tests for FileService + SettingsService | pending | - |

---

## Next Action

Start S1-01: scaffold WinUI3 project from `dotnet new winui`, rename FreshRef → SingletonNotepad, wire DI.

See `README.md` for mandatory build constraints before starting.
