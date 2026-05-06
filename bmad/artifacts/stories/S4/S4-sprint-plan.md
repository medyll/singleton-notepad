# Sprint 4 — Advanced & Release Candidate

**PM:** Clément
**Date:** 2026-05-06
**Goal:** Ship production-ready v1.0 with versioned backups, backup management UI, custom rules editor, and release polish.

---

## Stories

### S4-01 — Versioned Backup System
- Enhance `NormalizationService.BackupContentAsync` to maintain numbered versions
- Keep last N backups (configurable, default: 10), delete older ones
- Backup filename format: `backup_{timestamp}_v{n}.md`
- Cleanup task runs on each new backup

### S4-02 — Backup Management UI
- New "Historique" pane in SettingsPage (NavigationView item)
- List available backups with timestamps and sizes
- One-click restore from backup
- Delete individual backups

### S4-03 — Custom Rules Editor
- New "Règles" pane in SettingsPage
- Edit `NOTEPAD_SINGLETON_AGENTS.md` content inline (TextBox)
- Live preview of markdown formatting
- Save button + auto-save on change (debounced)

### S4-04 — Release Candidate
- Final smoke test: open → edit → save → normalize → preview → apply → MEMORY.md updated
- Verify cold start < 2s
- MSIX packaging finalization
- App icon + metadata check
- No P0/P1 bugs open

---

## Exit Criteria
- All 4 stories done with passing tests
- Versioned backups work: old backups cleaned up, numbered versions maintained
- Backup UI: list + restore + delete all functional
- Rules editor: save and reload AGENTS.md works
- Release build produces valid MSIX or standalone .exe
- No regressions from Sprint 3