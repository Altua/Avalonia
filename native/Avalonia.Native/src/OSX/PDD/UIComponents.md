# UI Components

## Overview
The UI Components module contains implementations for user interface elements that integrate with macOS native UI features. These components provide the visual and interactive elements required for a fully functional application on macOS.

## Architecture and Implementation Pattern

### Native Integration Architecture

The UI Components module follows a layered architectural approach:

1. **Native Wrapper Pattern**:
   - Wraps macOS native UI components in C++ classes
   - Implements Avalonia interfaces for consistent cross-platform behavior
   - Maintains native look and feel while providing Avalonia functionality

2. **Event Bridging**:
   - Translates between macOS UI events and Avalonia events
   - Maintains event flow consistency across platforms
   - Handles platform-specific event nuances

3. **Visual Integration**:
   - Ensures UI components match macOS visual standards
   - Adapts to macOS theme and appearance changes
   - Provides proper scaling and DPI awareness

### Implementation Approach

The UI Components use several implementation strategies:

1. **Objective-C++ Integration**:
   - Combines Objective-C for native UI components with C++ for Avalonia interfaces
   - Uses the @implementation pattern for native classes
   - Bridges between managed .NET and native Objective-C code

2. **Delegation Pattern**:
   - Uses macOS delegation patterns for event handling
   - Implements NSDelegate protocols for component callbacks
   - Maintains clear separation of concerns

3. **Factory Pattern**:
   - Provides factory methods for component creation
   - Centralizes component instantiation logic
   - Ensures proper initialization and setup of components

## Files

### AutoFitContentView.h / AutoFitContentView.mm
A specialized view component that automatically resizes to fit its content. This component:
- Adjusts its size based on content dimensions
- Provides proper layout capabilities for dynamic content
- Enables automatic sizing behavior for popups and other dynamic elements

**Implementation Details:**

The AutoFitContentView class provides automatic layout capabilities:

```objc
@implementation AutoFitContentView
{
    NSView* _content;
    bool _showsBlur;
}

// Initialize with content view
- (instancetype)initWithContent:(NSView *)content
{
    if (self = [super init])
    {
        _content = content;
        _showsBlur = false;
        [self addSubview:content];
    }
    
    return self;
}

// Automatically adjust size based on content
- (void)layout
{
    [super layout];
    
    NSRect frame = self.frame;
    NSRect contentFrame = _content.frame;
    
    // Set content to fill the view
    contentFrame.origin = NSMakePoint(0, 0);
    contentFrame.size = frame.size;
    _content.frame = contentFrame;
    
    // Update blur effect if enabled
    if(_showsBlur)
    {
        [self SetBlurVisualEffect];
    }
}

// Enable or disable blur effect
- (void)ShowBlur:(bool)showBlur
{
    _showsBlur = showBlur;
    
    if(_showsBlur)
    {
        [self SetBlurVisualEffect];
    }
}

// Apply the visual blur effect
- (void)SetBlurVisualEffect
{
    // Create a visual effects view with a blur effect
    NSVisualEffectView* blurView = [[NSVisualEffectView alloc] initWithFrame:[self bounds]];
    [blurView setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];
    [blurView setBlendingMode:NSVisualEffectBlendingModeBehindWindow];
    [blurView setState:NSVisualEffectStateActive];
    [blurView setMaterial:NSVisualEffectMaterialLight];
    
    // Insert the blur view behind content
    [self addSubview:blurView positioned:NSWindowBelow relativeTo:_content];
}
```

### menu.h / menu.mm
Implements macOS native menu system integration, providing:
- Application menu bar support
- Context menu support
- Menu item management and event handling
- Menu structure and hierarchies
- Keyboard shortcut integration
- Menu state management (checked state, enabled/disabled)

**Implementation Details:**

The menu implementation consists of several key components:

1. **Menu Item Implementation**:

```cpp
class AvnAppMenuItem : public ComSingleObject<IAvnMenuItem, &IID_IAvnMenuItem>
{
private:
    NSMenuItem* _native; // here we hold a pointer to an AvnMenuItem
    IAvnActionCallback* _callback;
    IAvnPredicateCallback* _predicate;
    bool _isCheckable;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenuItem(bool isSeparator)
    {
        _isCheckable = false;

        if(isSeparator)
        {
            _native = [NSMenuItem separatorItem];
        }
        else
        {
            _native = [[AvnMenuItem alloc] initWithAvnAppMenuItem: this];
        }
        
        _callback = nullptr;
    }
    
    // Set the item's checked state
    virtual HRESULT SetIsChecked(bool isChecked) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [_native setState:(isChecked && _isCheckable ? NSOnState : NSOffState)];
            return S_OK;
        }
    }
    
    // Set the item's keyboard shortcut
    virtual HRESULT SetGesture(AvnKey key, AvnInputModifiers modifiers) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(key != AvnKeyNone)
            {
                NSEventModifierFlags flags = 0;
                
                if (modifiers & Control)
                    flags |= NSEventModifierFlagControl;
                if (modifiers & Shift)
                    flags |= NSEventModifierFlagShift;
                if (modifiers & Alt)
                    flags |= NSEventModifierFlagOption;
                if (modifiers & Windows)
                    flags |= NSEventModifierFlagCommand;
                
                auto menuChar = MenuCharFromVirtualKey(key);
                
                if (menuChar != 0)
                {
                    auto keyString = [NSString stringWithCharacters:&menuChar length:1];
                    
                    [_native setKeyEquivalent: keyString];
                    [_native setKeyEquivalentModifierMask:flags];
                    
                    return S_OK;
                }
            }
            
            // Nothing matched... clear.
            [_native setKeyEquivalent: @""];
            [_native setKeyEquivalentModifierMask: 0];
            
            return S_OK;
        }
    }
};
```

2. **Menu Implementation**:

```cpp
class AvnAppMenu : public ComSingleObject<IAvnMenu, &IID_IAvnMenu>
{
private:
    AvnMenu* _native;
    ComPtr<IAvnMenuEvents> _baseEvents;
    AvnMenuDelegate* _delegate;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenu(IAvnMenuEvents* events)
    {
        _baseEvents = events;
        _delegate = [[AvnMenuDelegate alloc] initWithParent: this];
        _native = [[AvnMenu alloc] initWithDelegate: _delegate];
    }

    // Insert a menu item at the specified index
    virtual HRESULT InsertItem(int index, IAvnMenuItem *item) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if([_native hasGlobalMenuItem])
            {
                index++;
            }
            
            auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
            
            if(avnMenuItem != nullptr)
            {
                [_native insertItem: avnMenuItem->GetNative() atIndex:index];
            }
            
            return S_OK;
        }
    }
    
    // Handle menu update events
    void RaiseNeedsUpdate()
    {
        if(_baseEvents != nullptr)
        {
            _baseEvents->NeedsUpdate();
        }
    }
    
    void RaiseOpening()
    {
        if(_baseEvents != nullptr)
        {
            _baseEvents->Opening();
        }
    }
};
```

3. **Menu Delegate for Event Handling**:

```objc
@implementation AvnMenuDelegate
{
    AvnAppMenu* _parent;
}

// Initialize with parent menu
- (id) initWithParent:(AvnAppMenu *)parent
{
    self = [super init];
    _parent = parent;
    return self;
}

// Callback when menu needs update
- (void)menuNeedsUpdate:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseNeedsUpdate();
}

// Callback when menu is about to open
- (void)menuWillOpen:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseOpening();
}

// Callback when menu has closed
- (void)menuDidClose:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseClosed();
}
```

### trayicon.h / trayicon.mm
Implements system tray icon support, allowing Avalonia applications to:
- Display an icon in the macOS menu bar (system tray)
- Provide context menus for the tray icon
- Handle click events on the tray icon
- Update the tray icon appearance

**Implementation Details:**

The tray icon implementation integrates with the macOS status item system:

```objc
@implementation AvnStatusItem
{
    NSStatusItem* _native;
    ComPtr<IAvnMenu> _menu;
    ComPtr<IAvnTrayIconEvents> _events;
}

// Initialize with standard size
- (id) init
{
    self = [super init];
    _native = [[NSStatusBar systemStatusBar] statusItemWithLength:NSSquareStatusItemLength];
    _menu = nullptr;
    _events = nullptr;
    
    // Set up click handling
    [_native.button setTarget:self];
    [_native.button setAction:@selector(onClick:)];
    
    return self;
}

// Handle click event
- (void)onClick:(id)sender
{
    if(_menu != nullptr)
    {
        auto avnMenu = dynamic_cast<AvnAppMenu*>(_menu.operator->());
        
        if(avnMenu != nullptr)
        {
            auto nsmenu = avnMenu->GetNative();
            
            [_native popUpStatusItemMenu:nsmenu];
        }
    }
    
    if(_events != nullptr)
    {
        _events->OnClicked();
    }
}

// Set the tray icon image
- (void)setIcon:(NSImage *)image
{
    // Scale image appropriately for the menu bar
    NSSize size = NSMakeSize(18, 18);
    [image setSize:size];
    
    _native.button.image = image;
}

// Set the tooltip text
- (void)setToolTip:(NSString *)toolTip
{
    _native.button.toolTip = toolTip;
}

// Set the attached menu
- (void)setMenu:(IAvnMenu*)menu
{
    _menu = menu;
}

// Set the event handler
- (void)setEvents:(IAvnTrayIconEvents*)events
{
    _events = events;
}
```

### cursor.h / cursor.mm
Handles custom mouse cursor implementation and management, providing:
- Custom cursor creation and management
- System cursor integration
- Cursor state changes based on UI interaction
- Cursor position management

**Implementation Details:**

The cursor implementation provides both standard system cursors and custom cursor support:

```cpp
class CursorFactory : public ComSingleObject<IAvnCursorFactory, &IID_IAvnCursorFactory>
{
public:
    FORWARD_IUNKNOWN()
    
    // Create a standard system cursor
    virtual HRESULT GetCursor(AvnStandardCursorType cursorType, IAvnCursor** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            NSCursor* cursor = nullptr;
            
            switch(cursorType)
            {
                case CursorArrow:
                    cursor = [NSCursor arrowCursor];
                    break;
                    
                case CursorIbeam:
                    cursor = [NSCursor IBeamCursor];
                    break;
                    
                case CursorWait:
                    cursor = [NSCursor arrowCursor];
                    break;
                    
                case CursorCross:
                    cursor = [NSCursor crosshairCursor];
                    break;
                // Many more cursor mappings...
            }
            
            *ppv = new Cursor(cursor);
            
            return S_OK;
        }
    }
    
    // Create a custom cursor from image data
    virtual HRESULT CreateCustomCursor(void* bitmapData, size_t length, AvnPixelSize hotPixel, IAvnCursor** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            NSImage* image = [[NSImage alloc] initWithData:[NSData dataWithBytes:bitmapData length:length]];
            
            if(image != nullptr)
            {
                NSCursor* cursor = [[NSCursor alloc] initWithImage:image hotSpot:NSMakePoint(hotPixel.Width, hotPixel.Height)];
                
                *ppv = new Cursor(cursor);
                
                return S_OK;
            }
            
            return E_FAIL;
        }
    }
};
```

## UI Component Events and Interaction Flow

### Menu Interaction Flow

1. **Menu Creation**:
   - Application creates menu structure through Avalonia's Menu API
   - Native menu wrapper is created with menu delegate
   - Menu items are added to the menu structure

2. **Menu Event Flow**:
   - User clicks on menu item
   - NSMenuItem triggers didSelectItem: method
   - AvnMenuItem forwards action to AvnAppMenuItem
   - AvnAppMenuItem invokes callback in managed code

3. **Menu Update Flow**:
   - When menu is about to be displayed, menuWillOpen: is called
   - Menu delegate forwards to AvnAppMenu::RaiseOpening
   - Managed code updates menu item states (enabled/checked)
   - Menu is displayed with current state

### Tray Icon Interaction Flow

1. **Tray Icon Creation**:
   - Application creates tray icon through Avalonia's TrayIcon API
   - NSStatusItem is created in the system status bar
   - Icon and tooltip are set

2. **Tray Icon Click Flow**:
   - User clicks on tray icon
   - onClick: method is triggered
   - If menu is attached, menu is displayed
   - Click event is forwarded to managed code

3. **Tray Icon Update Flow**:
   - Application changes tray icon properties
   - Changes are applied to the NSStatusItem
   - Visual updates appear in the system status bar 
