# Core Components

## Overview
The Core Components module contains the fundamental building blocks for Avalonia's macOS window system integration. These files implement the primary window infrastructure, view system, and the entry point for the native macOS backend.

## Avalonia Native Architecture and Implementation Pattern

### What Comes From Avalonia
Avalonia provides a set of interfaces and abstract classes that define the expected behavior for platform-specific implementations. The key interfaces are defined in the `avalonia-native.h` header file and include:

1. **Interface Definitions**: The macOS implementation must satisfy interfaces like:
   - `IAvaloniaNativeFactory` - Factory interface for creating platform objects
   - `IAvnWindow` - Interface for window implementations
   - `IAvnTopLevel` - Interface for top-level containers
   - `IAvnWindowEvents` - Interface for window event callbacks

2. **Data Structures**: Avalonia defines common data structures used across platforms:
   - `AvnSize`, `AvnPoint`, `AvnRect` - Geometry structures
   - `AvnWindowState` - Enum for window states (Normal, Minimized, etc.)
   - `AvnKey` - Key codes for input handling
   - Enumerations for cursors, events, and more

3. **Event Model**: Avalonia defines an event callback pattern where native code calls back into managed code:
   - `RawMouseEvent`, `RawKeyEvent`, `Resized`, `ScalingChanged`, etc.

### Custom macOS Implementation

The macOS implementation overrides and implements Avalonia's interfaces with platform-specific code:

1. **Native Objective-C Classes**:
   - `AvnView` - A custom `NSView` subclass that handles rendering and input
   - `AvnWindow` - A custom `NSWindow` implementation for Avalonia windows

2. **C++ Wrapper Classes**:
   - `WindowBaseImpl` - Implements `IAvnWindowBase` with macOS-specific window functionality
   - `WindowImpl` - Extends `WindowBaseImpl` to implement `IAvnWindow` 
   - `TopLevelImpl` - Implements `IAvnTopLevel` for top-level containers

3. **Factory Implementation**:
   - `AvaloniaNative` class in `main.mm` implements `IAvaloniaNativeFactory` to create platform objects

4. **COM-style Implementation Pattern**:
   The code uses a COM-like interface implementation pattern:
   ```cpp
   class WindowImpl : public virtual WindowBaseImpl, public virtual IAvnWindow, public IWindowStateChanged
   {
   public:
       FORWARD_IUNKNOWN()
       BEGIN_INTERFACE_MAP()
           INHERIT_INTERFACE_MAP(WindowBaseImpl)
           INTERFACE_MAP_ENTRY(IAvnWindow, IID_IAvnWindow)
       END_INTERFACE_MAP()
       // Implementation...
   }
   ```

### Data Flow Between Managed and Native Code

1. **Managed to Native**: 
   - Avalonia calls the native implementation through interfaces like `IAvnWindow`
   - Method calls like `SetWindowState`, `Resize`, or `SetTitle` cross from managed to native

2. **Native to Managed**: 
   - Native macOS events are captured by the custom NSView/NSWindow implementations
   - Events are translated to Avalonia events and sent back to managed code via callbacks
   - For example, an NSEvent for mouse movement is translated to `RawMouseEvent` in `AvnView.mm`

3. **Rendering Pipeline**:
   - Avalonia sends rendering commands to the native implementation
   - The native code manages CALayer/Metal/OpenGL rendering depending on configuration

## Files

### AvnView.h / AvnView.mm
NSView subclass that handles rendering, input, and accessibility for Avalonia windows. This is the primary view component responsible for:
- Handling user input events (mouse, keyboard, etc.)
- Supporting text input methods
- Supporting drag and drop operations
- Rendering Avalonia visual elements via platform backends

**Implementation Details:**

The `AvnView` class inherits from NSView and implements multiple protocols:
```objc
@interface AvnView : NSView<NSTextInputClient, NSDraggingDestination, AvnTextInputMethodDelegate, CALayerDelegate>
```

Key methods include:
1. **Input handling** - Mouse events are processed and translated to Avalonia events:
```objc
- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
    // Verify if input should be ignored based on window state
    if([self ignoreUserInput: triggerInputWhenDisabled])
    {
        return;
    }

    // Convert coordinates
    NSPoint eventLocation = [event locationInWindow];
    auto viewLocation = [self convertPoint:NSMakePoint(0, 0) toView:nil];
    auto localPoint = NSMakePoint(eventLocation.x - viewLocation.x, viewLocation.y - eventLocation.y);
    auto point = ToAvnPoint(localPoint);
    
    // Pass event to Avalonia
    auto parent = _parent.tryGet();
    if(parent != nullptr)
    {
        parent->TopLevelEvents->RawMouseEvent(type, timestamp, modifiers, point, delta);
    }
}
```

2. **Rendering setup** - The view uses CALayer for rendering:
```objc
-(void) setRenderTarget:(NSObject<IRenderTarget>*)target
{
    if([self layer])
    {
        [self layer].delegate = nil;
    }
    _currentRenderTarget = target;
    auto layer = [target layer];
    [self setLayer: layer];
    [layer setDelegate: self];
    layer.needsDisplayOnBoundsChange = YES;
    [self updateRenderTarget];
}
```

### AvnWindow.mm
Implements window-specific functionality for macOS windows. This provides core window operations and event handling including:
- Window event propagation to Avalonia
- Window positioning and sizing
- Window activation management

### WindowBaseImpl.h / WindowBaseImpl.mm
Base implementation for all window types with common window functionality, including:
- Common window creation code
- Window positioning and sizing
- Window event handling
- Input event propagation

**Implementation Details:**

`WindowBaseImpl` is a C++ class that serves as the foundation for all window implementations:

1. **Window creation**:
```cpp
void WindowBaseImpl::CreateNSWindow(bool usePanel) {
    if (usePanel) {
        Window = [[AvnPanel alloc] initWithParent:this contentRect:NSRect{0, 0, lastSize} styleMask:NSWindowStyleMaskBorderless];
        [Window setHidesOnDeactivate:false];
    } else {
        Window = [[AvnWindow alloc] initWithParent:this contentRect:NSRect{0, 0, lastSize} styleMask:NSWindowStyleMaskBorderless];
    }
}
```

2. **Resizing**:
```cpp
HRESULT WindowBaseImpl::Resize(double x, double y, AvnPlatformResizeReason reason) {
    if (_inResize) {
        return S_OK;
    }

    _inResize = true;
    auto resizeBlock = ResizeScope(View, reason);

    @autoreleasepool {
        // Apply size constraints
        auto maxSize = lastMaxSize;
        auto minSize = lastMinSize;

        if (x < minSize.width) {
            x = minSize.width;
        }
        // More constraint checks...

        @try {
            if(x != lastSize.width || y != lastSize.height)
            {
                lastSize = NSSize{x, y};
                SetClientSize(lastSize);
                [Window invalidateShadow];
            }
        }
        @finally {
            _inResize = false;
        }

        return S_OK;
    }
}
```

### WindowImpl.h / WindowImpl.mm
Implements the IAvnWindow interface for standard windows with state management for:
- Window decorations
- Window state (minimized, maximized, normal)
- Window resizing capabilities
- Title bar customization
- Window focus management

**Implementation Details:**

`WindowImpl` extends `WindowBaseImpl` and adds functionality specific to standard windows:

1. **Window state management**:
```cpp
HRESULT WindowImpl::SetWindowState(AvnWindowState state) {
    START_COM_CALL;
    
    @autoreleasepool {
        _inSetWindowState = true;
        
        // Store the state we're trying to set
        _lastWindowState = state;
        
        if (state == Normal) {
            if(IsZoomed()) {
                DoZoom();
            }
            
            [Window setStyleMask:CalculateStyleMask()];
            
            if(_fullScreenActive) {
                [Window toggleFullScreen:Window];
                _fullScreenActive = false;
            }
        } else if (state == Maximized) {
            if(!_fullScreenActive) {
                if (!IsZoomed()) {
                    if(Decorations() == SystemDecorationsFull) {
                        DoZoom();
                    } else {
                        // Custom maximization for borderless windows
                        // Store current window frame for restoration
                        _preZoomSize = [Window frame];
                        
                        // Get the target screen size
                        NSScreen* screen = [Window screen];
                        NSRect screenRect = [screen visibleFrame];
                        
                        // Set to maximum size while preserving position
                        [Window setFrame:screenRect display:YES];
                    }
                }
            }
        } else if (state == Minimized) {
            [Window miniaturize:Window];
        } else if (state == FullScreen) {
            if(!_fullScreenActive) {
                _fullScreenActive = true;
                [Window toggleFullScreen:Window];
            }
        }
        
        _inSetWindowState = false;
        return S_OK;
    }
}
```

2. **Setting window title**:
```cpp
HRESULT WindowImpl::SetTitle(char* utf8title) {
    START_COM_CALL;
    
    @autoreleasepool {
        _lastTitle = [NSString stringWithUTF8String:(const char*)utf8title];
        [Window setTitle:_lastTitle];
        return S_OK;
    }
}
```

### WindowOverlayImpl.h / WindowOverlayImpl.mm
Implements overlay windows that can be positioned on top of other applications, enabling features like:
- Transparent windows that can be positioned over other applications
- Windows that can receive input while being displayed over other applications
- Special window behaviors for overlay scenarios

### TopLevelImpl.h / TopLevelImpl.mm
Base implementation for top-level windows (not child windows), providing:
- Top-level window management
- Screen position tracking
- Parent-child window relationships

**Implementation Details:**

`TopLevelImpl` serves as a base class for all top-level containers:

```cpp
// Creating render targets for different rendering backends
HRESULT TopLevelImpl::CreateMetalRenderTarget(IAvnMetalDevice* device, IAvnMetalRenderTarget** ret) {
    START_COM_CALL;
    
    @autoreleasepool {
        if(device == nullptr)
            return E_POINTER;
        
        MetalRenderTarget* target = [[MetalRenderTarget alloc] initWithDevice:device];
        
        *ret = nullptr;
        [target getRenderTarget:ret];
        
        if(*ret) {
            // Update the view's render target
            [View setRenderTarget:target];
            currentRenderTarget = target;
        }
        
        return S_OK;
    }
}

// Point coordinate transformations between client and screen space
HRESULT TopLevelImpl::PointToClient(AvnPoint point, AvnPoint* ret) {
    START_COM_CALL;
    
    @autoreleasepool {
        if(ret == nullptr)
            return E_POINTER;
        
        NSRect windowPoint = [View.window convertRectFromScreen:NSMakeRect(point.X, point.Y, 0, 0)];
        NSPoint viewPoint = [View convertPoint:NSMakePoint(windowPoint.origin.x, windowPoint.origin.y) fromView:nil];
        
        *ret = ToAvnPoint(viewPoint);
        
        return S_OK;
    }
}
```

### PopupImpl.mm
Implements popup windows like context menus and dropdown menus with specialized behavior:
- Temporary, non-modal windows
- Automatic dismissal when clicked outside
- Specialized positioning and sizing

### main.mm
Core entry point and factory implementation for the native macOS backend. It initializes the application and provides:
- COM object factory implementation
- Avalonia Native API implementation
- Application initialization
- Factory methods for creating platform objects

**Implementation Details:**

`main.mm` contains the factory implementation (`AvaloniaNative`) that creates all required platform components:

```cpp
class AvaloniaNative : public ComSingleObject<IAvaloniaNativeFactory, &IID_IAvaloniaNativeFactory>
{
public:
    FORWARD_IUNKNOWN()
    
    // Initialize the platform
    virtual HRESULT Initialize(IAvnGCHandleDeallocatorCallback* deallocator,
            IAvnApplicationEvents* events,
            IAvnDispatcher* dispatcher) override
    {
        START_COM_CALL;
        
        _deallocator = deallocator;
        _dispatcher = dispatcher;
        @autoreleasepool{
            [[ThreadingInitializer new] do];
        }
        InitializeAvnApp(events, disableAppDelegate);
        return S_OK;
    };
    
    // Create a window 
    virtual HRESULT CreateWindow(IAvnWindowEvents* cb, IAvnWindow** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(cb == nullptr || ppv == nullptr)
                return E_POINTER;
            *ppv = CreateAvnWindow(cb);
            return S_OK;
        }
    };
    
    // Create platform-specific features like clipboard, screens, menus, etc.
    virtual HRESULT CreateScreens(IAvnScreenEvents* cb, IAvnScreens** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateScreens(cb);
            return S_OK;
        }
    }
    
    // Platform-specific options for macOS
    virtual IAvnMacOptions* GetMacOptions() override
    {
        return (IAvnMacOptions*)new MacOptions();
    }
}
```

The `MacOptions` class provides macOS-specific settings:

```cpp
class MacOptions : public ComSingleObject<IAvnMacOptions, &IID_IAvnMacOptions>
{
public:
    // Set the application title displayed in the dock/menu
    virtual HRESULT SetApplicationTitle(char* utf8String) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto appTitle = [NSString stringWithUTF8String: utf8String];
            if (disableSetProcessName == 0)
            {
                [[NSProcessInfo processInfo] setProcessName:appTitle];
                SetProcessName(appTitle);
            }
            // More implementation...
            return S_OK;
        }
    }
    
    // Control dock visibility
    virtual HRESULT SetShowInDock(int show) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            AvnDesiredActivationPolicy = show
                ? NSApplicationActivationPolicyRegular 
                : NSApplicationActivationPolicyAccessory;
            return S_OK;
        }
    }
}
``` 
