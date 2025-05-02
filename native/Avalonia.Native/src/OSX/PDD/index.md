# Avalonia Native OSX Implementation

This document serves as an index to the Avalonia Native macOS implementation. Each section below links to a detailed document describing a specific component group of the implementation.

## [Core Components](CoreComponents.md)
The foundation of the Avalonia macOS implementation, providing window, view, and application lifecycle management. These components form the structural backbone of the platform, handling core window operations, view rendering, and the main entry point for the native backend.

## [UI Components](UIComponents.md)
Specialized UI elements that integrate with macOS native interfaces, including menus, cursors, and system tray icons. These components enable Avalonia applications to provide a native-feeling user experience that conforms to macOS UI standards.

## [Rendering](Rendering.md)
Graphics rendering implementation that provides hardware-accelerated graphics through Metal and OpenGL backends. This module abstracts rendering targets and handles frame timing to deliver smooth, high-performance visual output.

## [Platform Integration](PlatformIntegration.md)
Integration with macOS platform-specific features and services such as clipboard, file dialogs, screen management, and platform settings. These components allow Avalonia applications to leverage native system capabilities and adapt to the macOS environment.

## [Protocols and Interfaces](ProtocolsAndInterfaces.md)
Communication protocols and interfaces establishing contracts between components in the codebase. This module defines standardized methods of interaction between different parts of the Avalonia native implementation.

## [Accessibility and Automation](AccessibilityAndAutomation.md)
Support for accessibility features and UI automation, enabling Avalonia applications to be accessible to users with disabilities and supporting automated testing frameworks through standard macOS accessibility protocols.

## [Input Handling](InputHandling.md)
User input management for keyboard, international text input methods, and drag-and-drop operations. These components translate platform-specific input events to Avalonia's cross-platform input system and provide support for complex text input scenarios.

## [Utilities](Utilities.md)
Common functionality, helper classes, and support code used throughout the implementation. This module provides shared infrastructure like string handling, COM interface helpers, control hosting, and specialized utilities for threading and memory management.