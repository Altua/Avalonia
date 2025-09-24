#include <unordered_set>
#import "FirstResponderObserver.h"
#include "WindowOverlayImpl.h"
#include "WindowInterfaces.h"

WindowOverlayImpl::~WindowOverlayImpl()
{
    [View removeFromSuperview];
    [[NSNotificationCenter defaultCenter] removeObserver: View];

    for (id monitor in eventMonitors)
    {
        [NSEvent removeMonitor: monitor];
    }
}

WindowOverlayImpl::WindowOverlayImpl(void* parentWindow, char* parentView, IAvnWindowEvents *events) : WindowImpl(events), WindowBaseImpl(events, false, true), TopLevelImpl(events) {
    this->parentWindow = (__bridge NSWindow*) parentWindow;
    this->parentView = FindNSView(this->parentWindow, [NSString stringWithUTF8String:parentView]);
    this->canvasView = FindNSView(this->parentWindow, @"PPTClipView");

    // We should ideally choose our parentview to be positioned exactly on top of the main window
    // This is needed to replicate default avalonia behaviour
    // If parentview is positioned differently, we shall adjust the origin and size accordingly (bottom left coordinates)
    [this->parentView addSubview:View];
    this->parentWindow.initialFirstResponder = View;
    
    // Next responder of AvnView should be the PPTView, on top of which the AvnView is placed.
    // If AvnView was not added then PPTView would have been default firstResponder.
    // So, all unhandled events of AvnView should technically go to the PPTView, not to the parentView.
    View.nextResponder = FindNSView(this->parentWindow, @"PPTView");
     
    firstResponderObserver = [[FirstResponderObserver alloc] initWithView: View];
    
    NSRect frame = this->parentView.frame;
    frame.size.height += frame.origin.y;
    frame.origin.y = -frame.origin.y;

    [View setFrame:frame];
    lastSize = frame.size;
    
    // View must resize along with the parent.
    View.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;

    InitializeColorPicker();

    [[NSNotificationCenter defaultCenter] addObserver:View selector:@selector(overlayWindowDidBecomeKey:) name:NSWindowDidBecomeKeyNotification object:this->parentWindow];
    [[NSNotificationCenter defaultCenter] addObserver:View selector:@selector(overlayWindowDidResignKey:) name:NSWindowDidResignKeyNotification object:this->parentWindow];

    id mouseMovedMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskMouseMoved handler:^NSEvent * (NSEvent * event) {
        //NSLog(@"MONITOR mouseMoved START");

        if ([event window] != this->parentWindow)
        {
            if (isTrackingMouse)
            {
                isTrackingMouse = false;
                [View mouseExited: event];
            }
            
            //NSLog(@"MONITOR overlay=FALSE -> normal chain");
            return event;
        }

        // We add our own event monitor in order to be able to catch and override all mouse events before PowerPoint
        // This fixes cursor overrides done by PowerPoint in the NSResponder chain
        // We only need it here in WindowOverlayImpl and not any other Avalonia window

        auto localPoint = [View convertPoint:[event locationInWindow] toView:View];
        auto avnPoint = ToAvnPoint(localPoint);
        auto point = [View translateLocalPoint:avnPoint];

        auto hitTest = this->BaseEvents->HitTest(point);
        static bool shouldUpdateCursor = false;
        isTrackingMouse = hitTest;
        
        if (hitTest == false)
        {
            //NSLog(@"MONITOR overlay=TRUE hitTest=FALSE -> normal chain");
            shouldUpdateCursor = true;
            return event;
        }
        else
        {
            //NSLog(@"MONITOR overlay=TRUE hitTest=TRUE -> force event");
            if (shouldUpdateCursor)
            {
                // There are times when PowerPoint's NSTrackingArea fires after Avalonia's NSTrackingArea
                // We must ensure that we have the final word for the cursor set by forcing a second update of the cursor

                UpdateCursor();
                shouldUpdateCursor = false;
            }

            [View mouseMoved:event];
            return nil;
        }
    }];

    id leftMouseDownMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown handler:^NSEvent * (NSEvent * event) {
        NSLog(@"MONITOR mouseDown START");

        if ([event window] != this->parentWindow)
        {
            NSLog(@"MONITOR window=FALSE overlay=FALSE -> normal chain");
            return event;
        }

        auto localPoint = [View convertPoint:[event locationInWindow] toView:View];
        auto avnPoint = ToAvnPoint(localPoint);
        auto point = [View translateLocalPoint:avnPoint];

        if (point.Y < 0)
        {
            // Ribbon/title bar above our view
            NSLog(@"MONITOR window=TRUE overlay=FALSE -> ribbon/title bar");
            return event;
        }

        auto hitTest = this->BaseEvents->HitTest(point);
        if (hitTest == false)
        {
            this->BaseEvents->OnSlideMouseActivate(point);
        }

        return event;
    }];
    
    // Special key codes that need explicit sending to AvnView
    // IMPORTANT:
    // Careful while adding keys to this list.
    // The keys are directly sent to AvnView, so PowerPoint will not get a chance to handle them.
    // As a result keys in this list will not be handled by PowerPoint if Grunt object is selected.
    static const std::unordered_set<unsigned short> specialKeyCodes = {
        11,  // Cmd+b (Bold)
        34,  // Cmd+I (Italic)
        32   // Cmd+U (Underline)
    };

    id keydownMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskKeyDown | NSEventMaskKeyUp | NSEventMaskFlagsChanged handler:^NSEvent * (NSEvent * event) {
        NSUInteger flags = [event modifierFlags] & NSEventModifierFlagDeviceIndependentFlagsMask;

        AvnInputModifiers modifiers = GetCommandModifier([event modifierFlags]);
        NSLog(@"WOI: Dispatching Key Flags =%ld, Event=%ld", flags, [event type]);

        // We are only interested in special combinations and not regular keys:
        // - Modifier alone is pressed or released (NSEventTypeFlagsChanged)
        // - Modifier+Key events (modifiers != AvnInputModifiersNone)
        if ((modifiers != AvnInputModifiersNone) || ([event type] == NSEventTypeFlagsChanged))
        {
            NSLog(@"WOI: Captured Key Event Flags =%ld, Event=%ld", flags, [event type]);
            if ((specialKeyCodes.find([event keyCode]) != specialKeyCodes.end()) &&
                ([[[event window] firstResponder] isKindOfClass:[AvnView class]]))
            {
                // The normal processing chain will call `performKeyEquivalent` for most combinations,
                // with the exception of a few special ones. We force `sendEvent` these to our AvnView,
                // where they will reach the regular `keyDown` / `keyUp` handlers. In the future we can
                // consider having a singular handling point for both scenarios.

                // Possible AvnView first responder scenarios are:
                // 1) Powerpoint window: firstResponder is our overlay after a Grunt object was selected
                // 2) Standalone Avalonia window: firstResponder is always an AvnView
                
                NSLog(@"WOI: MONITOR Forcing keyboard event to AvnWindow");
                [[event window] sendEvent:event];
                return nil;
            }
        }

        NSLog(@"WOI: Monitor not handled key=%hu", [event keyCode]);
        return event;
    }];
    
    eventMonitors = [NSArray arrayWithObjects: mouseMovedMonitor, leftMouseDownMonitor, keydownMonitor, nil];
}


AvnInputModifiers WindowOverlayImpl::GetCommandModifier(NSEventModifierFlags modFlag)
{
    unsigned int rv = 0;

    if (modFlag & NSEventModifierFlagControl)
        rv |= Control;
    if (modFlag & NSEventModifierFlagShift)
        rv |= Shift;
    if (modFlag & NSEventModifierFlagOption)
        rv |= Alt;
    if (modFlag & NSEventModifierFlagCommand)
        rv |= Windows;

    if (rv == 0)
        return AvnInputModifiersNone;
    else
        return (AvnInputModifiers)rv;
}


bool WindowOverlayImpl::IsOverlay()
{
    return true;
}

HRESULT WindowOverlayImpl::Activate() {
    START_COM_CALL;

    @autoreleasepool {
        NSWindow *window = this->parentWindow;

        if (window == nullptr) {
            NSLog(@"ACT: Overlay window not found");
        }
        else {
            if ([window makeFirstResponder:View]) {
                NSString* firstResponderName = NSStringFromClass([View class]);
                NSLog(@"ACT: Successfully made the view the first responder: %@", firstResponderName);
            } else {
                NSLog(@"ACT: Failed to make the view the first responder.");
            }
        }        
    }

    return S_OK;
}

HRESULT WindowOverlayImpl::Close()
{
    START_COM_CALL;
    if ( !closed ) {
        closed = true;
        HRESULT result = WindowImpl::Close();
        [View onClosed];
        BaseEvents->Closed();
        return result;
    }

    return S_OK;
}

HRESULT WindowOverlayImpl::PointToClient(AvnPoint point, AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        point = ConvertPointY(point);
        NSRect convertRect = [parentWindow convertRectFromScreen:NSMakeRect(point.X, point.Y, 0.0, 0.0)];

        auto viewPoint = NSMakePoint(convertRect.origin.x, convertRect.origin.y);

        // NSLog(@"PointToClient %@", NSStringFromPoint(viewPoint));
        *ret = [View translateLocalPoint:ToAvnPoint(viewPoint)];

        return S_OK;
    }
}

HRESULT WindowOverlayImpl::GetScaling(double *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        if (parentWindow == nullptr) {
            *ret = 1;
            return S_OK;
        }

        *ret = [parentWindow backingScaleFactor];
        return S_OK;
    }
}

HRESULT WindowOverlayImpl::PointToScreen(AvnPoint point, AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        auto cocoaViewPoint = ToNSPoint([View translateLocalPoint:point]);
        NSRect convertRect = [parentWindow convertRectToScreen:NSMakeRect(cocoaViewPoint.x, cocoaViewPoint.y, 0.0, 0.0)];

        auto cocoaScreenPoint = NSPointFromCGPoint(NSMakePoint(convertRect.origin.x, convertRect.origin.y));
        *ret = ConvertPointY(ToAvnPoint(cocoaScreenPoint));

        return S_OK;
    }
}

HRESULT WindowOverlayImpl::GetPPTClipViewOrigin(AvnPoint *ret) {
    START_COM_CALL;

    // We need this whenever scrollbars are present inside PPTClipView.
    // This is a fix for PowerPoint's builtin PointsToScreenPixelsX returning
    // the same value regardless of scroll position on Macos.

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        if (this->canvasView == nullptr) {
            NSLog(@"PPTClipView not found!");
            return E_FAIL;
        }

        auto canvasOriginPoint = [canvasView bounds].origin;
        ret->X = canvasOriginPoint.x;
        ret->Y = canvasOriginPoint.y;

        return S_OK;
    }
}

HRESULT WindowOverlayImpl::TakeScreenshot(void** ret, int* retLength) {
    START_COM_CALL;

    NSView* view = [[this->parentWindow contentView] superview];
    
    if (view == nullptr) {
        NSLog(@"TakeScreenshot: contentView or superview not found!");
        return E_FAIL;
    }

    NSSize viewSize = [view bounds].size;
    NSBitmapImageRep *imageRep = [[NSBitmapImageRep alloc]
                                  initWithBitmapDataPlanes:NULL
                                  pixelsWide:viewSize.width
                                  pixelsHigh:viewSize.height
                                  bitsPerSample:8
                                  samplesPerPixel:4
                                  hasAlpha:YES
                                  isPlanar:NO
                                  colorSpaceName:NSCalibratedRGBColorSpace
                                  bytesPerRow:0
                                  bitsPerPixel:0];

    [view cacheDisplayInRect:[view bounds] toBitmapImageRep:imageRep];

    NSDictionary *imageProps = @{};
    NSData *bitmapData = [imageRep representationUsingType:NSBitmapImageFileTypePNG properties:imageProps];
    
    *retLength = [bitmapData length];
    *ret = (void *)[bitmapData bytes];
    
    //NSLog(@"Writing bitmap to file %@", filePath);
    //[bitmapData writeToFile:filePath atomically:YES];

    return S_OK;
}

void WindowOverlayImpl::InitializeColorPicker() {
    this->colorPanel = [NSColorPanel sharedColorPanel];
    this->colorPanel.showsAlpha = true;

    // Create a view to serve as the accessory view
    NSView *accessoryView = [[NSView alloc] initWithFrame:NSMakeRect(0, 0, 200, 40)];

    // Create OK button
    NSButton *okButton = [[NSButton alloc] initWithFrame:NSMakeRect(10, 5, 80, 30)];
    [okButton setTitle:@"OK"];
    [okButton setTarget:View];
    [okButton setAction:@selector(colorPanelOkButtonPressed:)];

    // Create Cancel button
    NSButton *cancelButton = [[NSButton alloc] initWithFrame:NSMakeRect(110, 5, 80, 30)];
    [cancelButton setTitle:@"Cancel"];
    [cancelButton setTarget:View];
    [cancelButton setAction:@selector(colorPanelCancelButtonPressed:)];

    // Add buttons to the accessory view
    [accessoryView addSubview:okButton];
    [accessoryView addSubview:cancelButton];

    // Set the accessory view to the color panel
    [this->colorPanel setAccessoryView:accessoryView];

    [[NSNotificationCenter defaultCenter] addObserver:View selector:@selector(colorPanelWillClose:) name:NSWindowWillCloseNotification object:this->colorPanel];
}

HRESULT WindowOverlayImpl::PickColor(AvnColor color, bool* cancel, AvnColor* ret) {
    START_COM_CALL;

    NSColor* initialColor = this->colorPanel.color;
    
    this->colorPanel.color = [NSColor colorWithRed:color.Red / 255.0
                                           green:color.Green / 255.0
                                             blue:color.Blue / 255.0
                                           alpha:color.Alpha / 255.0];

    NSInteger modalResponse = [NSApp runModalForWindow:colorPanel];

    // Handle different modal responses
    if (modalResponse == NSModalResponseOK) {
        NSColor *selectedColor = [this->colorPanel color];
        NSLog(@"OK pressed, got back color: %@", selectedColor);

        ret->Alpha = round([selectedColor alphaComponent] * 255.0);
        ret->Red = round([selectedColor redComponent] * 255.0);
        ret->Green = round([selectedColor greenComponent] * 255.0);
        ret->Blue = round([selectedColor blueComponent] * 255.0);
        *cancel = 0;
    } else {
        NSLog(@"Modal session was aborted (cancel or window closed manually).");
        *cancel = 1;
    }

    return S_OK;
}


#pragma mark-
// Additional AvnView methods

@interface AvnView ()

- (bool) keyboardEvent: (NSEvent *) event withType: (AvnRawKeyEventType)type;

@end

@implementation AvnView (OverlayWindowExtension)


// This executes before keydown events but after the monitor.
// So this is a nice (only?) way to let PowerPoint handle the keyboard shortcuts if Avalonia or Grunt doesn't handle them at runtime.
// Event monitor is still required for handling certain key combinations, which PowerPoint blocks from reaching here.
- (BOOL)performKeyEquivalent:(NSEvent *)event
{
    WindowImpl* parent = self.parent;
    if (parent == nullptr){
        return [super performKeyEquivalent: event];
    }
    
    auto modifiers = WindowOverlayImpl::GetCommandModifier(event.modifierFlags);
    auto key = VirtualKeyFromScanCode(event.keyCode, event.modifierFlags);
    auto timestamp = static_cast<uint64_t>(event.timestamp * 1000);
    AvnRawKeyEventType type = event.type == NSEventTypeKeyDown ? KeyDown : KeyUp;

    bool handled = event.window.firstResponder == self && [self keyboardEvent: event withType: type];
    if (!handled && parent->IsOverlay())
    {
        handled = parent->BaseEvents->MonitorKeyEvent(type, timestamp, modifiers, key);
    }
    
    if (!handled)
    {
        handled = [super performKeyEquivalent: event];
    }
    
    return handled;
}

@end
