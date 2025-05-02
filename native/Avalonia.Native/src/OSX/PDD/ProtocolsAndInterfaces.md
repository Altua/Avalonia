# Protocols and Interfaces

## Overview
The Protocols and Interfaces module defines communication protocols and interfaces used throughout the Avalonia native macOS implementation. These files establish contracts between components and provide structured communication channels.

## Architecture and Implementation Pattern

### Protocol-Based Architecture

The Protocols and Interfaces module follows a layered architectural approach:

1. **Separation of Concerns**:
   - Clearly defines interfaces between different components
   - Separates implementation details from interface definitions
   - Enables modular and testable code structure

2. **Protocol Hierarchies**:
   - Organizes protocols in a hierarchical manner
   - Provides specialization through protocol inheritance
   - Creates reusable interface patterns

3. **Multiple Communication Channels**:
   - Uses different interface types for different communication needs
   - Combines Objective-C protocols with C++ abstract classes
   - Provides bridge between object-oriented and COM-style interfaces

### Implementation Approaches

The module uses several distinct implementation approaches:

1. **Objective-C Protocols**:
   - Used for native macOS class integration
   - Provides clear contracts for macOS components
   - Allows for dynamic dispatch and runtime flexibility

2. **C++ Abstract Interfaces**:
   - Used for COM-style interfaces
   - Provides strong type checking and compilation guarantees
   - Supports multiple inheritance patterns

3. **Delegate Patterns**:
   - Used for event callbacks and notifications
   - Establishes clear ownership and lifecycle management
   - Provides decoupled communication channels

## Files

### WindowProtocol.h
Defines protocols for window-related functionality:
- Declares window event handling protocols
- Establishes window state notification protocols
- Defines window input handling protocols
- Provides window lifecycle management protocols

**Implementation Details:**

The `AvnWindowProtocol` defines the contract for all window implementations:

```objc
@protocol AvnWindowProtocol
// Poll a modal session to keep it responsive
-(void) pollModalSession: (NSModalSession _Nonnull) session;

// Determine if the window should handle events
-(bool) shouldTryToHandleEvents;

// Enable or disable window input
-(void) setEnabled: (bool) enable;

// Menu management
-(void) showAppMenuOnly;
-(void) showWindowMenuWithAppMenu;
-(void) applyMenu:(AvnMenu* _Nullable)menu;

// Automation support
-(IAvnAutomationPeer* _Nonnull) automationPeer;

// Extended window features
-(double) getExtendedTitleBarHeight;
-(void) setIsExtended:(bool)value;

// Parent-child window management
-(void) disconnectParent;
-(bool) isDialog;

// Window activation behavior
-(void) setCanBecomeKeyWindow:(bool)value;
@end
```

This protocol is implemented by both `AvnWindow` and `AvnPanel` classes to provide consistent window behavior.

### WindowInterfaces.h
Defines interfaces for window operations:
- Declares window management interfaces
- Establishes window property access interfaces
- Defines window state change interfaces
- Provides window interaction interfaces

**Implementation Details:**

The WindowInterfaces.h file defines concrete window classes that implement the window protocol:

```objc
@interface AvnWindow : NSWindow <AvnWindowProtocol, NSWindowDelegate, AvnAccessibility>
// Initialize a window with parent and style
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent
                          contentRect: (NSRect)contentRect
                            styleMask: (NSWindowStyleMask)styleMask;

// Access the content view
-(AvnView* _Nullable) view;
@end

@interface AvnPanel : NSPanel <AvnWindowProtocol, NSWindowDelegate, AvnAccessibility>
// Initialize a panel with parent and style
-(AvnPanel* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent
                         contentRect: (NSRect)contentRect
                           styleMask: (NSWindowStyleMask)styleMask;
@end
```

These classes provide concrete implementations of the window protocol, enabling both standard windows and utility panels.

### INSWindowHolder.h
Interface for classes that hold NSWindow references:
- Defines contract for NSWindow container objects
- Establishes NSWindow access methods
- Provides NSWindow lifecycle management
- Standardizes NSWindow reference handling

**Implementation Details:**

The INSWindowHolder defines a simple contract for accessing NSWindow objects:

```cpp
struct INSWindowHolder
{
    // Get the underlying NSWindow reference
    virtual NSWindow* _Nonnull GetNSWindow() = 0;
};

// Similar interface for NSView access
struct INSViewHolder
{
    // Get the underlying NSView reference
    virtual AvnView* _Nonnull GetNSView() = 0;
};
```

These interfaces enable other components to access platform window and view objects without exposing implementation details.

### IWindowStateChanged.h
Interface for handling window state changes:
- Defines contract for window state change notifications
- Establishes state transition interfaces
- Provides window state monitoring
- Standardizes window state event handling

**Implementation Details:**

The IWindowStateChanged interface defines the contract for window state transitions:

```cpp
struct IWindowStateChanged: public IUnknown
{
    // Called when the window state (normal, minimized, maximized) has changed
    virtual void WindowStateChanged() = 0;
    
    // Signal the start of a state transition
    virtual void StartStateTransition() = 0;
    
    // Signal the end of a state transition
    virtual void EndStateTransition() = 0;
    
    // Get the current window decorations state
    virtual SystemDecorations Decorations() = 0;
    
    // Get the current window state
    virtual AvnWindowState WindowState() = 0;
};
```

This interface is implemented by WindowImpl to handle window state changes and transitions between states.

### ResizeScope.h / ResizeScope.mm
Manages window resize operations scope and lifecycle:
- Provides a scope-based approach to resize operations
- Ensures proper cleanup after resize operations
- Handles resize event propagation
- Manages resize state transitions
- Coordinates resize-related operations

**Implementation Details:**

ResizeScope uses the RAII (Resource Acquisition Is Initialization) pattern to manage resize operations:

```cpp
class ResizeScope
{
public:
    // Initialize resize scope with a view and reason
    ResizeScope(AvnView* _Nonnull view, AvnPlatformResizeReason reason)
    {
        _view = view;
        // Save the current resize reason
        _restore = [view getResizeReason];
        // Set the new resize reason for this scope
        [view setResizeReason:reason];
    }

    // Automatically restore the original resize reason
    ~ResizeScope()
    {
        [_view setResizeReason:_restore];
    }
    
private:
    AvnView* _Nonnull _view;
    AvnPlatformResizeReason _restore;
};
```

This class is used to safely manage the resize reason during window resizing operations, ensuring that the resize reason is properly restored when the operation completes.

## Interface Interactions and Communication Flow

### Window State Management Flow

1. **Window Creation**:
   - WindowBaseImpl creates an AvnWindow or AvnPanel
   - The window implements the AvnWindowProtocol
   - The window holds a reference back to its parent WindowBaseImpl

2. **State Change Propagation**:
   - NSWindow receives a state change notification
   - AvnWindow forwards the notification to WindowImpl
   - WindowImpl implements IWindowStateChanged to handle the notification
   - WindowImpl notifies Avalonia of the state change

3. **Resize Operation Flow**:
   - Resize request comes from Avalonia
   - WindowBaseImpl creates a ResizeScope to track the resize reason
   - Window size is adjusted using NSWindow methods
   - When the resize completes, ResizeScope is destroyed and cleans up
   - Window notifies Avalonia of the completed resize

4. **Property Access Flow**:
   - Components access window or view using INSWindowHolder/INSViewHolder
   - The interface provides a stable contract for window/view access
   - Implementation details are abstracted away behind the interface
   - Changes to the implementation don't affect interface consumers 
