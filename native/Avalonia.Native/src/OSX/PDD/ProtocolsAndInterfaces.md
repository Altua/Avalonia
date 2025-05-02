# Protocols and Interfaces

## Overview
The Protocols and Interfaces module defines communication protocols and interfaces used throughout the Avalonia native macOS implementation. These files establish contracts between components and provide structured communication channels.

## Files

### WindowProtocol.h
Defines protocols for window-related functionality:
- Declares window event handling protocols
- Establishes window state notification protocols
- Defines window input handling protocols
- Provides window lifecycle management protocols

### WindowInterfaces.h
Defines interfaces for window operations:
- Declares window management interfaces
- Establishes window property access interfaces
- Defines window state change interfaces
- Provides window interaction interfaces

### INSWindowHolder.h
Interface for classes that hold NSWindow references:
- Defines contract for NSWindow container objects
- Establishes NSWindow access methods
- Provides NSWindow lifecycle management
- Standardizes NSWindow reference handling

### IWindowStateChanged.h
Interface for handling window state changes:
- Defines contract for window state change notifications
- Establishes state transition interfaces
- Provides window state monitoring
- Standardizes window state event handling

### ResizeScope.h / ResizeScope.mm
Manages window resize operations scope and lifecycle:
- Provides a scope-based approach to resize operations
- Ensures proper cleanup after resize operations
- Handles resize event propagation
- Manages resize state transitions
- Coordinates resize-related operations 
