# iDesk Desktop Widget Platform — Architecture Design

## Overview

A single-process, multi-window desktop widget platform for Windows. All widgets share a common `DeskWidget` base class (providing bookmark/dock mode, cross-virtual desktop support) and a unified theming system. A central console manages widget instance creation and deletion.

## Architecture

### Solution Structure — Single Solution, Multi Project

```
IDESK.sln
├── src/
│   ├── IDESK.Core/              # Shared library: DeskWidget base, theme, logging, helpers
│   ├── IDESK.Console/           # Central console window
│   ├── IDESK.Widgets.Todo/      # Todo widget
│   ├── IDESK.Widgets.Notes/     # Notes widget (scaffold only)
│   ├── IDESK.Widgets.Schedule/  # Schedule widget (scaffold only)
│   └── IDESK.Host/              # Single EXE entry point, DI container setup
```

### Project Dependencies

```
IDESK.Host → IDESK.Console, IDESK.Widgets.Todo
IDESK.Console → IDESK.Core
IDESK.Widgets.Todo → IDESK.Core
IDESK.Widgets.Notes → IDESK.Core
IDESK.Widgets.Schedule → IDESK.Core
```

All widget projects are peers — they depend only on Core, never on each other.

### Project Types

- **IDESK.Core**: Class library with `<UseWPF>true</UseWPF>`. Contains DeskWidget, theme XAMLs, logging abstractions, RelayCommand helper.
- **IDESK.Console**: Class library with `<UseWPF>true</UseWPF>`. Console window that lists available widget types and creates instances.
- **IDESK.Widgets.\***: Class libraries with `<UseWPF>true</UseWPF>`. Each contains its ViewModel, models, services, and WPF controls.
- **IDESK.Host**: WPF Application (exe). References all projects. Sets up DI container, loads shared themes, launches Console.

### Runtime Flow

1. `IDESK.Host/App.xaml.cs` builds the DI container, registering all services and ViewModels
2. Host creates the Console window and shows it
3. User clicks "Todo" button in Console
4. Console resolves `TodoListViewModel` from DI, creates a `DeskWidget`, sets its `NormalContent` to the Todo UserControl
5. User interacts with the Todo widget; closing it disposes the instance

### Theme System

All theme XAMLs (Colors.xaml, Icons.xaml, DefaultLightTheme.xaml, BookmarkStyles.xaml) live in `IDESK.Core/Style/`. The Host loads them as application-level resources at startup.

### Current Implementation Scope

- Full migration of existing Todo code into `IDESK.Widgets.Todo`
- Console with a single "Open Todo" button
- Notes and Schedule projects as empty scaffolds

## Guardrails

- Widget projects are peers — Core is the only shared dependency
- No circular dependencies between widget projects
- All projects within the same solution share a single version
