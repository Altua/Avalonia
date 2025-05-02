# UI Components

## Overview
The UI Components module contains implementations for user interface elements that integrate with macOS native UI features. These components provide the visual and interactive elements required for a fully functional application on macOS.

## Files

### AutoFitContentView.h / AutoFitContentView.mm
A specialized view component that automatically resizes to fit its content. This component:
- Adjusts its size based on content dimensions
- Provides proper layout capabilities for dynamic content
- Enables automatic sizing behavior for popups and other dynamic elements

### menu.h / menu.mm
Implements macOS native menu system integration, providing:
- Application menu bar support
- Context menu support
- Menu item management and event handling
- Menu structure and hierarchies
- Keyboard shortcut integration
- Menu state management (checked state, enabled/disabled)

### trayicon.h / trayicon.mm
Implements system tray icon support, allowing Avalonia applications to:
- Display an icon in the macOS menu bar (system tray)
- Provide context menus for the tray icon
- Handle click events on the tray icon
- Update the tray icon appearance

### cursor.h / cursor.mm
Handles custom mouse cursor implementation and management, providing:
- Custom cursor creation and management
- System cursor integration
- Cursor state changes based on UI interaction
- Cursor position management 
