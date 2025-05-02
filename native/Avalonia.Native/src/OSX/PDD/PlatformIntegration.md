# Platform Integration

## Overview
The Platform Integration module provides connections between Avalonia and macOS platform-specific features. These files handle integration with system services, platform features, and provide platform-specific behaviors needed for proper functioning on macOS.

## Files

### app.mm
Provides platform-specific application features and lifecycle management, including:
- Application lifecycle events (launch, terminate, etc.)
- Application delegate integration
- System event handling
- Application activation/deactivation handling
- Integration with macOS application model

### clipboard.mm
Implements clipboard functionality for copy/paste operations:
- Clipboard data reading and writing
- Format conversion between Avalonia and macOS formats
- Support for various data types (text, images, etc.)
- Integration with NSPasteboard
- Clipboard change notifications

### Screens.mm
Handles multiple monitor support, screen metrics, and DPI:
- Detects and monitors available screens
- Provides screen size and position information
- Handles DPI and scaling factors for each screen
- Monitors screen configuration changes
- Provides screen-related events to Avalonia

### StorageProvider.mm
Implements file dialogs and storage access:
- Open file dialogs
- Save file dialogs
- Folder selection dialogs
- File system access permissions
- Integration with macOS file system and security model

### platformthreading.mm
Provides threading abstractions for platform-specific threading:
- Thread creation and management
- Thread synchronization primitives
- Main thread access
- Integration with NSThread and GCD
- Event dispatch to appropriate threads

### PlatformSettings.mm
Manages and provides platform-specific settings:
- System color scheme detection (light/dark mode)
- Accent color detection
- System font settings
- Accessibility settings
- User preference monitoring

### PlatformBehaviorInhibition.mm
Controls platform-specific behaviors that need to be disabled or modified:
- Prevents or modifies default system behaviors
- Handles platform-specific workarounds
- Disables conflicting system features
- Ensures consistent behavior across platforms 
