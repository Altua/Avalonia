# Input Handling

## Overview
The Input Handling module manages user input interactions for Avalonia applications on macOS. These components handle keyboard input, text input methods for international languages, and drag-and-drop operations.

## Files

### KeyTransform.h / KeyTransform.mm
Transforms macOS key inputs to platform-agnostic key codes:
- Maps native macOS key codes to Avalonia key codes
- Handles keyboard modifier keys
- Provides consistent key handling across platforms
- Manages key translations including special keys
- Implements keyboard layout specifics

### AvnTextInputMethod.h / AvnTextInputMethod.mm
Implements text input methods for IME (Input Method Editor) support:
- Enables input of complex languages (Chinese, Japanese, Korean, etc.)
- Handles composition-based text input
- Provides integration with macOS input methods
- Manages text input state
- Supports text selection during composition

### AvnTextInputMethodDelegate.h
Defines delegate for text input method events:
- Declares event handlers for text input changes
- Establishes communication protocol for IME
- Provides callback methods for text composition
- Defines text selection interfaces
- Standardizes text input state notifications

### dnd.mm
Implements drag and drop functionality:
- Provides drag source implementation
- Implements drop target support
- Handles data format conversions for dragged content
- Manages drag visual feedback
- Coordinates drag and drop operations between applications 
