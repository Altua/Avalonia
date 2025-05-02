# Accessibility and Automation

## Overview
The Accessibility and Automation module provides support for accessibility features and UI automation on macOS. These components enable Avalonia applications to be accessible to users with disabilities and support automated testing frameworks.

## Architecture and Implementation Pattern

### Integration with macOS Accessibility Framework

The Avalonia Native macOS implementation bridges between Avalonia's platform-agnostic accessibility API and macOS's NSAccessibility protocol. This integration follows these key architectural principles:

1. **Peer-Based Architecture**:
   - Each Avalonia UI element has a corresponding `IAvnAutomationPeer` in the managed code
   - The native implementation maps these peers to macOS accessibility elements

2. **Hierarchy Mirroring**:
   - The visual hierarchy of UI elements is mirrored in the accessibility tree
   - Parent-child relationships are maintained between accessibility elements

3. **Event Propagation**:
   - Accessibility events from Avalonia are mapped to NSAccessibility notifications
   - macOS accessibility events are captured and propagated back to Avalonia

### Interfaces and Protocols

The implementation uses several key components to bridge between Avalonia and macOS:

1. **AvnAccessibility Protocol**: 
   A custom Objective-C protocol that defines methods for raising accessibility events

2. **AvnAutomationNode**: 
   A COM object that implements `IAvnAutomationNode` and serves as the bridge between managed peers and native accessibility elements

3. **AvnAccessibilityElement**:
   A subclass of NSAccessibilityElement that adapts Avalonia's accessibility information to macOS's accessibility API

## Files

### automation.h / automation.mm
Implements macOS accessibility and UI automation support:
- Creates accessibility element hierarchy
- Maps Avalonia UI elements to NSAccessibility protocol
- Implements accessibility properties and actions
- Provides automation API for automated testing
- Handles accessibility notifications

**Implementation Details:**

The core of the accessibility implementation is the `AvnAccessibilityElement` class:

```objc
@implementation AvnAccessibilityElement
{
    IAvnAutomationPeer* _peer;
    AvnAutomationNode* _node;
    NSMutableArray* _children;
}

+ (NSAccessibilityElement *)acquire:(IAvnAutomationPeer *)peer
{
    if (peer == nullptr)
        return nil;
    
    auto instance = peer->GetNode();
    
    if (instance != nullptr)
        return dynamic_cast<AvnAutomationNode*>(instance)->GetOwner();
    
    // Handle different types of peers with specialized approaches
    if (peer->IsInteropPeer())
    {
        auto view = (__bridge NSAccessibilityElement*)peer->InteropPeer_GetNativeControlHandle();
        return view;
    }
    else if (peer->IsRootProvider())
    {
        auto window = peer->RootProvider_GetWindow();
        // For root elements, use the window as the accessibility element
        auto holder = dynamic_cast<INSViewHolder*>(window);
        auto view = holder->GetNSView();
        return (NSAccessibilityElement*)[view window];
    }
    else
    {
        return [[AvnAccessibilityElement alloc] initWithPeer:peer];
    }
}
```

The implementation maps Avalonia's automation control types to macOS accessibility roles:

```objc
- (NSAccessibilityRole)accessibilityRole
{
    auto controlType = _peer->GetAutomationControlType();
    
    switch (controlType) {
        case AutomationButton: return NSAccessibilityButtonRole;
        case AutomationCalendar: return NSAccessibilityGridRole;
        case AutomationCheckBox: return NSAccessibilityCheckBoxRole;
        case AutomationComboBox: return NSAccessibilityPopUpButtonRole;
        // Many more mappings...
        default: return NSAccessibilityGroupRole;
    }
}
```

UI automation actions are implemented by responding to the appropriate accessibility actions:

```objc
- (BOOL)accessibilityPerformPress
{
    if (_peer->IsInvokeProvider())
    {
        _peer->InvokeProvider_Invoke();
    }
    else if (_peer->IsExpandCollapseProvider())
    {
        _peer->ExpandCollapseProvider_Expand();
    }
    else if (_peer->IsToggleProvider())
    {
        _peer->ToggleProvider_Toggle();
    }
    return YES;
}
```

### AvnAccessibility.h
Defines accessibility interfaces and structures:
- Declares accessibility property identifiers
- Establishes accessibility interface contracts
- Defines accessibility event types
- Provides accessibility property structures
- Standardizes accessibility communication protocols

**Implementation Details:**

The `AvnAccessibility` protocol defines methods for raising accessibility events:

```objc
@protocol AvnAccessibility <NSAccessibility>
@required
- (void) raiseChildrenChanged;
@optional
- (void) raiseFocusChanged;
- (void) raisePropertyChanged:(AvnAutomationProperty)property;
@end
```

This protocol is implemented by both `AvnAccessibilityElement` and Avalonia window classes to provide a consistent mechanism for accessibility notifications.

### AvnAutomationNode.h
Implements automation node hierarchy for accessibility:
- Represents UI elements in the automation tree
- Provides navigation between automation nodes
- Implements automation properties and patterns
- Enables querying of UI element properties
- Supports automated interaction with UI elements

**Implementation Details:**

The `AvnAutomationNode` class bridges between the managed `IAvnAutomationNode` interface and the native accessibility implementation:

```cpp
class AvnAutomationNode : public ComSingleObject<IAvnAutomationNode, &IID_IAvnAutomationNode>
{
public:
    FORWARD_IUNKNOWN()
    AvnAutomationNode(id <AvnAccessibility> owner) { _owner = owner; }
    AvnAccessibilityElement* GetOwner() { return _owner; }
    
    // Called from managed code to notify of accessibility changes
    virtual void Dispose() override { _owner = nil; }
    virtual void ChildrenChanged() override { [_owner raiseChildrenChanged]; }
    virtual void PropertyChanged(AvnAutomationProperty property) override { 
        [_owner raisePropertyChanged:property]; 
    }
    virtual void FocusChanged() override { [_owner raiseFocusChanged]; }
    
private:
    __strong id <AvnAccessibility> _owner;
};
```

When an accessibility event occurs in Avalonia, it calls the corresponding method on the `IAvnAutomationNode` interface, which triggers the appropriate accessibility notification in macOS.

## Data Flow Between Avalonia and macOS Accessibility

1. **Initialization**:
   - Avalonia creates automation peers for UI elements
   - Each peer is associated with an `AvnAutomationNode` through the `SetNode` method
   - The `AvnAutomationNode` is linked to an `AvnAccessibilityElement`

2. **Property Access**:
   - VoiceOver or other assistive technologies query properties through NSAccessibility
   - `AvnAccessibilityElement` delegates these queries to the corresponding `IAvnAutomationPeer` methods
   - Information flows from Avalonia to macOS accessibility consumers

3. **Actions**:
   - User actions (via accessibility tools) are received by `AvnAccessibilityElement`
   - These actions are translated to calls on the appropriate provider interfaces
   - For example, `accessibilityPerformPress` maps to `InvokeProvider_Invoke()`

4. **Event Notifications**:
   - Changes in Avalonia trigger calls to `IAvnAutomationNode` methods
   - These are forwarded to the `AvnAccessibilityElement` via the `AvnAutomationNode`
   - The accessibility element posts appropriate NSAccessibility notifications 