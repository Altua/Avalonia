# Input Handling

## Overview
The Input Handling module manages user input interactions for Avalonia applications on macOS. These components handle keyboard input, text input methods for international languages, and drag-and-drop operations.

## Architecture and Implementation Pattern

### Input Processing Flow

The input handling in Avalonia's macOS native implementation follows a clear processing pipeline:

1. **Event Capture**:
   - Native macOS events (NSEvent) are captured by custom NSView implementations
   - These events include mouse, keyboard, and text input events

2. **Event Transformation**:
   - Platform-specific events are transformed into Avalonia's platform-agnostic representation
   - Key codes are mapped from macOS scan codes to Avalonia virtual keys
   - Coordinates are transformed from macOS coordinate system to Avalonia's system

3. **Event Propagation**:
   - Transformed events are sent to Avalonia's managed code via interface callbacks
   - This ensures consistent event behavior across all platforms Avalonia supports

### Key Components

The macOS input handling implementation relies on several key architectural components:

1. **Key Mapping**:
   - Bidirectional mapping between macOS key codes and Avalonia's platform-agnostic key codes
   - Support for keyboard layouts and modifiers

2. **Text Input Method**:
   - Integration with macOS's NSTextInputClient for complex text input
   - Support for Input Method Editors (IMEs) used for languages like Chinese, Japanese, and Korean

3. **Drag and Drop**:
   - Implementation of both drag sources and drop targets
   - Data format conversion between Avalonia and macOS pasteboard

## Files

### KeyTransform.h / KeyTransform.mm
Transforms macOS key inputs to platform-agnostic key codes:
- Maps native macOS key codes to Avalonia key codes
- Handles keyboard modifier keys
- Provides consistent key handling across platforms
- Manages key translations including special keys
- Implements keyboard layout specifics

**Implementation Details:**

The key transformation system uses a mapping table to convert between macOS scan codes and Avalonia keys:

```cpp
// ScanCode - PhysicalKey - Key mapping (the virtual key is mapped as in a standard QWERTY keyboard)
const KeyInfo keyInfos[] =
{
    // Writing System Keys
    { 0x32, AvnPhysicalKeyBackquote, AvnKeyOem3, '`' },
    { 0x2A, AvnPhysicalKeyBackslash, AvnKeyOem5, '\\' },
    { 0x21, AvnPhysicalKeyBracketLeft, AvnKeyOem4, '[' },
    // Many more mappings...
};
```

The actual key transformation considers both the physical key and the keyboard layout:

```cpp
AvnKey VirtualKeyFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags)
{
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);
    if (!IsNumpadOrNumericKey(physicalKey))
    {
        const UniCharCount charCount = 4;
        UniChar chars[charCount];
        auto length = CharsFromScanCode(scanCode, modifierFlags, kUCKeyActionDown, chars, charCount);
        if (length > 0)
        {
            auto it = virtualKeyFromChar.find(chars[0]);
            if (it != virtualKeyFromChar.end())
                return it->second;
        }
    }

    auto it = qwertyVirtualKeyFromPhysicalKey.find(physicalKey);
    return it == qwertyVirtualKeyFromPhysicalKey.end() ? AvnKeyNone : it->second;
}
```

The implementation uses macOS's Carbon framework to properly handle keyboard layouts:

```cpp
static UniCharCount CharsFromScanCode(UInt16 scanCode, NSEventModifierFlags modifierFlags, UInt16 keyAction, UniChar* buffer, UniCharCount bufferSize)
{
    auto currentKeyboard = TISCopyCurrentKeyboardInputSource();
    if (!currentKeyboard)
        return 0;

    auto layoutData = static_cast<CFDataRef>(TISGetInputSourceProperty(currentKeyboard, kTISPropertyUnicodeKeyLayoutData));
    if (!layoutData)
        return 0;

    auto* keyboardLayout = reinterpret_cast<const UCKeyboardLayout*>(CFDataGetBytePtr(layoutData));

    // Calculate modifiers for key translation
    int glyphModifiers = 0;
    if (modifierFlags & NSEventModifierFlagShift)
        glyphModifiers |= shiftKey;
    if (modifierFlags & NSEventModifierFlagCapsLock)
        glyphModifiers |= alphaLock;
    if (modifierFlags & NSEventModifierFlagOption)
        glyphModifiers |= optionKey;

    // Use UCKeyTranslate to get the character for the key
    UCKeyTranslate(
        keyboardLayout,
        scanCode,
        keyAction,
        (glyphModifiers >> 8) & 0xFF,
        LMGetKbdType(),
        kUCKeyTranslateNoDeadKeysBit,
        &deadKeyState,
        bufferSize,
        &length,
        buffer);
        
    // More processing for dead keys and special characters...
}
```

### AvnTextInputMethod.h / AvnTextInputMethod.mm
Implements text input methods for IME (Input Method Editor) support:
- Enables input of complex languages (Chinese, Japanese, Korean, etc.)
- Handles composition-based text input
- Provides integration with macOS input methods
- Manages text input state
- Supports text selection during composition

**Implementation Details:**

The `AvnTextInputMethod` class provides the bridge between Avalonia's text input requirements and macOS's text input system:

```cpp
class AvnTextInputMethod: public virtual ComObject, public virtual IAvnTextInputMethod{
private:
    id<AvnTextInputMethodDelegate> _inputMethodDelegate;
public:
    FORWARD_IUNKNOWN()
    
    // Interface map for COM-style implementation
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(IAvnTextInputMethod, IID_IAvnTextInputMethod)
    END_INTERFACE_MAP()
    
    // Set the client that will receive text input events
    HRESULT SetClient(IAvnTextInputMethodClient* client) override;
    
    // Reset the input method state
    virtual void Reset() override;
    
    // Set the cursor rectangle where text is being edited
    virtual void SetCursorRect(AvnRect rect) override;
    
    // Set the current text and selection
    virtual void SetSurroundingText(char* text, int anchorOffset, int cursorOffset) override;
    
public:
    ComPtr<IAvnTextInputMethodClient> Client;
};
```

The implementation for handling text input is relatively simple:

```cpp
void AvnTextInputMethod::SetSurroundingText(char* text, int anchorOffset, int cursorOffset) {
    // Update the delegate with the current text and selection
    [_inputMethodDelegate setText:[NSString stringWithUTF8String:text]];
    [_inputMethodDelegate setSelection: anchorOffset : cursorOffset];
}

void AvnTextInputMethod::SetCursorRect(AvnRect rect) {
    // Update the delegate with the current cursor position
    [_inputMethodDelegate setCursorRect: rect];
}
```

The actual text input handling happens in the `AvnView` class, which implements NSTextInputClient to interact with the macOS input system.

### AvnTextInputMethodDelegate.h
Defines delegate for text input method events:
- Declares event handlers for text input changes
- Establishes communication protocol for IME
- Provides callback methods for text composition
- Defines text selection interfaces
- Standardizes text input state notifications

**Implementation Details:**

The `AvnTextInputMethodDelegate` protocol defines the methods needed for text input coordination:

```objc
@protocol AvnTextInputMethodDelegate
@required
// Update the text being edited
-(void) setText:(NSString* _Nonnull) text;

// Update the cursor's bounding rectangle
-(void) setCursorRect:(AvnRect) cursorRect;

// Update the selection range within the text
-(void) setSelection: (int) start : (int) end;
@end
```

This protocol is implemented by `AvnView` to handle text input for Avalonia windows.

### dnd.mm
Implements drag and drop functionality:
- Provides drag source implementation
- Implements drop target support
- Handles data format conversions for dragged content
- Manages drag visual feedback
- Coordinates drag and drop operations between applications

**Implementation Details:**

The drag and drop system uses two main components:

1. **Custom Drag Source**:

```objc
@implementation AvnDndSource
{
    NSDragOperation _operation;
    ComPtr<IAvnDndResultCallback> _cb;
    void* _sourceHandle;
};

// Define which operations are supported during dragging
- (NSDragOperation)draggingSession:(nonnull NSDraggingSession *)session 
    sourceOperationMaskForDraggingContext:(NSDraggingContext)context
{
    return _operation;
}

// Handle the end of a drag operation
- (void)draggingSession:(NSDraggingSession *)session
           endedAtPoint:(NSPoint)screenPoint
              operation:(NSDragOperation)operation
{
    if(_cb != nil)
    {
        auto cb = _cb;
        _cb = nil;
        cb->OnDragAndDropComplete(ConvertDragDropEffects(operation));
    }
    if(_sourceHandle != nil)
    {
        FreeAvnGCHandle(_sourceHandle);
        _sourceHandle = nil;
    }
}
@end
```

2. **Conversion Functions**:

```cpp
// Convert between macOS drag operations and Avalonia effects
extern AvnDragDropEffects ConvertDragDropEffects(NSDragOperation nsop)
{
    int effects = 0;
    if((nsop & NSDragOperationCopy) != 0)
        effects |= (int)AvnDragDropEffects::Copy;
    if((nsop & NSDragOperationMove) != 0)
        effects |= (int)AvnDragDropEffects::Move;
    if((nsop & NSDragOperationLink) != 0)
        effects |= (int)AvnDragDropEffects::Link;
    return (AvnDragDropEffects)effects;
};

// Create a custom data type for Avalonia-specific transfers
extern NSString* GetAvnCustomDataType()
{
    char buffer[256];
    sprintf(buffer, "net.avaloniaui.inproc.uti.n%in", getpid());
    return [NSString stringWithUTF8String:buffer];
}
```

The drop target functionality is implemented in `AvnView` through the NSDraggingDestination protocol.

## Data Flow Between Avalonia and macOS Input Systems

### Keyboard Input Flow

1. **Key Down/Up in macOS**:
   - NSEvent is received in AvnView's keyDown/keyUp methods
   - Physical scan code and modifiers are extracted
   - KeyTransform maps scan code to Avalonia virtual key
   - Event is packaged as RawKeyEvent and sent to Avalonia

2. **Text Input Flow**:
   - NSTextInputClient methods in AvnView receive text input
   - Text is processed and sent to Avalonia via IAvnTextInputMethodClient
   - Composition events are tracked and synchronized

3. **Drag and Drop Flow**:
   - Drag starts in Avalonia, creating an AvnDndSource
   - NSDraggingSession manages the dragging process in macOS
   - Drop effects are translated between systems
   - Completion callback notifies Avalonia of the result 
