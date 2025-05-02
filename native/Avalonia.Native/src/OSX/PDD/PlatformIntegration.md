# Platform Integration

## Overview
The Platform Integration module provides connections between Avalonia and macOS platform-specific features. These files handle integration with system services, platform features, and provide platform-specific behaviors needed for proper functioning on macOS.

## Architecture and Implementation Pattern

### Integration Strategy

The Platform Integration module follows several key architectural principles:

1. **Adaptation Layer**:
   - Implements an adaptation layer between Avalonia's platform-agnostic APIs and macOS native functionality
   - Translates between Avalonia concepts and macOS equivalents
   - Maintains consistent behavior across different macOS versions

2. **System Service Bridging**:
   - Connects to macOS system services (clipboard, file dialogs, screens)
   - Maps service-specific APIs to platform-neutral interfaces
   - Handles macOS-specific edge cases and limitations

3. **Notification Architecture**:
   - Sets up listeners for macOS system events and notifications
   - Propagates these events back to the Avalonia framework
   - Ensures thread-safety when crossing between managed and native code

### Implementation Pattern

Most platform integration components follow a similar implementation pattern:

1. **COM-style Interface Implementation**:
   - Implements Avalonia-defined interfaces using COM-style patterns
   - Uses RAII patterns for resource management
   - Follows consistent error handling approaches with START_COM_CALL macros

2. **Objective-C Integration**:
   - Uses @autoreleasepool for managing Objective-C object lifetimes
   - Creates Objective-C wrappers for callback mechanisms
   - Leverages Objective-C delegates to receive platform notifications

## Files

### app.mm
Provides platform-specific application features and lifecycle management, including:
- Application lifecycle events (launch, terminate, etc.)
- Application delegate integration
- System event handling
- Application activation/deactivation handling
- Integration with macOS application model

**Implementation Details:**

The app module integrates Avalonia with the NSApplication lifecycle:

```objc
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
@property (nonatomic, strong) NSWindow* __strong window;
@property (nonatomic, assign) IAvnApplicationEvents* events;
@property IAvnPlatformThreadingInterface* __strong threading;
@end

@implementation AvnAppDelegate
-(void)applicationWillFinishLaunching:(NSNotification *)notification
{
    if(_events)
        _events->OnFrameworkInitialized();
}

-(void)applicationDidFinishLaunching:(NSNotification *)notification
{
    if(_events)
        _events->OnApplicationStartup();
}

-(void)applicationWillTerminate:(NSNotification *)notification
{
    if(_events)
        _events->OnApplicationExit();
}
```

### clipboard.mm
Implements clipboard functionality for copy/paste operations:
- Clipboard data reading and writing
- Format conversion between Avalonia and macOS formats
- Support for various data types (text, images, etc.)
- Integration with NSPasteboard
- Clipboard change notifications

**Implementation Details:**

The clipboard implementation wraps NSPasteboard and provides a COM-style interface:

```cpp
class Clipboard : public ComSingleObject<IAvnClipboard, &IID_IAvnClipboard>
{
private:
    NSPasteboard* _pb;
    NSPasteboardItem* _item;
public:
    FORWARD_IUNKNOWN()
    
    Clipboard(NSPasteboard* pasteboard, NSPasteboardItem* item)
    {
        if(pasteboard == nil && item == nil)
            pasteboard = [NSPasteboard generalPasteboard];

        _pb = pasteboard;
        _item = item;
    }
    
    // Set text on the clipboard
    virtual HRESULT SetText(char* type, char* utf8String) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto string = [NSString stringWithUTF8String:(const char*)utf8String];
            auto typeString = [NSString stringWithUTF8String:(const char*)type];
            if(_item == nil)
                [_pb setString: string forType: typeString];
            else
                [_item setString: string forType:typeString];
        
            return S_OK;
        }
    }
    
    // Get text from the clipboard
    virtual HRESULT GetText(char* type, IAvnString**ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(ppv == nullptr)
                return E_POINTER;
            
            NSString* typeString = [NSString stringWithUTF8String:(const char*)type];
            NSString* string = _item == nil ? 
                [_pb stringForType:typeString] : 
                [_item stringForType:typeString];
            
            *ppv = CreateAvnString(string);
            
            return S_OK;
        }
    }
```

### Screens.mm
Handles multiple monitor support, screen metrics, and DPI:
- Detects and monitors available screens
- Provides screen size and position information
- Handles DPI and scaling factors for each screen
- Monitors screen configuration changes
- Provides screen-related events to Avalonia

**Implementation Details:**

The screens implementation monitors NSScreen changes:

```cpp
class Screens : public ComSingleObject<IAvnScreens, &IID_IAvnScreens>
{
private:
    ComPtr<IAvnScreensEvents> _events;
    NSObject* _observer;
public:
    FORWARD_IUNKNOWN()
    
    virtual void GetScreens(AvnScreen* ret, int* count) override
    {
        @autoreleasepool
        {
            auto screens = [NSScreen screens];
            auto screenCount = [screens count];
            
            if(ret != nullptr)
            {
                int index = 0;
                
                for(NSScreen* screen in screens)
                {
                    if(index > *count)
                        break;
                    
                    ret[index].Bounds = ConvertNSRect([screen frame]);
                    ret[index].WorkingArea = ConvertNSRect([screen visibleFrame]);
                    ret[index].Scaling = [screen backingScaleFactor];
                    ret[index].IsPrimary = index == 0;
                    
                    index++;
                }
            }
            
            *count = (int)screenCount;
        }
    }
    
    // Register for screen configuration change notifications
    virtual void RegisterScreensChange(IAvnScreensEvents* cb) override
    {
        _events = cb;
        
        // Set up NSNotificationCenter observer for display changes
        _observer = [[NSNotificationCenter defaultCenter] 
            addObserverForName:NSApplicationDidChangeScreenParametersNotification
                        object:nil
                         queue:nil
                    usingBlock:^(NSNotification *notification) {
                        if(_events != nullptr)
                            _events->OnScreensChanged();
                    }];
    }
```

### StorageProvider.mm
Implements file dialogs and storage access:
- Open file dialogs
- Save file dialogs
- Folder selection dialogs
- File system access permissions
- Integration with macOS file system and security model

**Implementation Details:**

The storage provider implementation wraps NSOpenPanel and NSSavePanel:

```cpp
virtual void OpenFileDialog(IAvnWindow* parentWindowHandle,
                         IAvnSystemDialogEvents* events,
                         bool allowMultiple,
                         const char* title,
                         const char* initialDirectory,
                         const char* initialFile,
                         IAvnFilePickerFileTypes* filters) override
{
    @autoreleasepool
    {
        auto panel = [NSOpenPanel openPanel];
        
        panel.allowsMultipleSelection = allowMultiple;
        
        if(title != nullptr)
        {
            panel.message = [NSString stringWithUTF8String:title];
            panel.title = [NSString stringWithUTF8String:title];
        }
        
        // Set initial directory and file if specified
        if(initialDirectory != nullptr)
        {
            auto directoryString = [NSString stringWithUTF8String:initialDirectory];
            panel.directoryURL = [NSURL URLWithString:directoryString];
        }
        
        if(initialFile != nullptr)
        {
            panel.nameFieldStringValue = [NSString stringWithUTF8String:initialFile];
        }
        
        // Configure file filters using an accessory view
        SetAccessoryView(panel, filters, false);
        
        // Show the dialog and handle the result
        auto handler = ^(NSModalResponse result) {
            if(result == NSFileHandlingPanelOKButton)
            {
                auto urls = [panel URLs];
                
                if(urls.count > 0)
                {
                    auto uriStrings = CreateAvnStringArray(urls);
                    events->OnCompleted(uriStrings);
                    return;
                }
            }
            
            events->OnCompleted(nullptr);
        };
        
        // Show as sheet if parent window is provided, otherwise show as modal dialog
        if(parentWindowHandle != nullptr)
        {
            auto windowHolder = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
            [panel beginSheetModalForWindow:windowHolder->GetNSWindow() 
                         completionHandler:handler];
        }
        else
        {
            [panel beginWithCompletionHandler: handler];
        }
    }
}
```

### platformthreading.mm
Provides threading abstractions for platform-specific threading:
- Thread creation and management
- Thread synchronization primitives
- Main thread access
- Integration with NSThread and GCD
- Event dispatch to appropriate threads

**Implementation Details:**

The platform threading implementation manages thread dispatching and synchronization:

```cpp
class PlatformThreading : public ComSingleObject<IAvnPlatformThreadingInterface, &IID_IAvnPlatformThreadingInterface>
{
private:
    ComPtr<IAvnPlatformThreadingInterfaceEvents> _events;
public:
    FORWARD_IUNKNOWN()
    
    // Run an action on the main thread
    virtual HRESULT RunLoopOnMainThread() override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (_events != NULL)
                _events->RunLoop(stop);
            [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.001]];
            
            return S_OK;
        }
    }
    
    // Dispatch a callback to the main thread
    virtual void DispatchCallback(IAvnActionCallback* callback) override
    {
        @autoreleasepool
        {
            if ([NSThread isMainThread])
            {
                callback->Run();
            }
            else
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    callback->Run();
                });
            }
        }
    }
```

### PlatformSettings.mm
Manages and provides platform-specific settings:
- System color scheme detection (light/dark mode)
- Accent color detection
- System font settings
- Accessibility settings
- User preference monitoring

**Implementation Details:**

The platform settings implementation provides system theme information:

```cpp
class PlatformSettings : public ComSingleObject<IAvnPlatformSettings, &IID_IAvnPlatformSettings>
{
    CocoaThemeObserver* observer;

public:
    FORWARD_IUNKNOWN()
    
    // Get the current platform theme (light/dark)
    virtual AvnPlatformThemeVariant GetPlatformTheme() override
    {
        @autoreleasepool
        {
            if (@available(macOS 10.14, *))
            {
                if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameVibrantLight) {
                    return AvnPlatformThemeVariant::Light;
                } else if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameDarkAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameVibrantDark) {
                    return AvnPlatformThemeVariant::Dark;
                } else if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastVibrantLight) {
                    return AvnPlatformThemeVariant::HighContrastLight;
                } else if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastDarkAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastVibrantDark) {
                    return AvnPlatformThemeVariant::HighContrastDark;
                }
            }
            return AvnPlatformThemeVariant::Light;
        }
    }
    
    // Get the system accent color
    virtual unsigned int GetAccentColor() override
    {
        @autoreleasepool
        {
            if (@available(macOS 10.14, *))
            {
                auto color = [NSColor controlAccentColor];
                return to_argb(color);
            }
            else
            {
                return 0;
            }
        }
    }
```

### PlatformBehaviorInhibition.mm
Controls platform-specific behaviors that need to be disabled or modified:
- Prevents or modifies default system behaviors
- Handles platform-specific workarounds
- Disables conflicting system features
- Ensures consistent behavior across platforms

**Implementation Details:**

The platform behavior inhibition module manages macOS-specific behavior adjustments:

```cpp
class PlatformBehaviorInhibition : public ComSingleObject<IAvnPlatformBehaviorInhibition, &IID_IAvnPlatformBehaviorInhibition>
{
private:
    bool _appSleepInhibited;
    
public:
    FORWARD_IUNKNOWN()
    
    PlatformBehaviorInhibition()
    {
        _appSleepInhibited = false;
    }
    
    // Control application sleep behavior
    virtual HRESULT SetInhibitAppSleep(bool inhibitAppSleep, char* reason) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (inhibitAppSleep == _appSleepInhibited)
                return S_OK;
                
            if (inhibitAppSleep)
            {
                NSString* reasonStr = [NSString stringWithUTF8String:reason];
                
                // Prevent the system from sleeping while the app is running
                IOPMAssertionID assertionID;
                IOReturn success = IOPMAssertionCreateWithName(
                    kIOPMAssertionTypeNoDisplaySleep,
                    kIOPMAssertionLevelOn,
                    (__bridge CFStringRef)reasonStr,
                    &assertionID);
                
                if (success == kIOReturnSuccess)
                    _appSleepInhibited = true;
            }
            else
            {
                // Allow the system to sleep normally
                // Release the assertion
                _appSleepInhibited = false;
            }
            
            return S_OK;
        }
    }
}
```

## Data Flow Between Avalonia and macOS Platform Services

### System Settings Flow

1. **Theme Changes**:
   - macOS appearance changes trigger NSNotifications
   - CocoaThemeObserver captures these changes
   - Changes are propagated to Avalonia via callbacks
   - Avalonia applies appropriate styling based on the theme

2. **File Dialog Flow**:
   - Avalonia requests a file dialog via IAvnStorageProvider
   - StorageProvider configures and displays NSOpenPanel or NSSavePanel
   - User selections are captured and returned as URI strings
   - Avalonia uses these URIs to access the selected files

3. **Screen Information Flow**:
   - Screens implementation monitors display configuration
   - Changes in screen setup trigger NSNotifications
   - Screen information is collected and translated to Avalonia's format
   - Avalonia adjusts layout and scaling based on screen information 
