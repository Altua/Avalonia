# Utilities

## Overview
The Utilities module provides common functionality, helper classes, and utility functions that support the Avalonia Native implementation on macOS. These utilities facilitate operations such as COM interoperability, memory management, reference counting, and various platform-specific helpers.

## Architecture and Implementation Pattern

### Common Utilities Architecture

The Utilities module follows several architectural principles:

1. **Cross-Cutting Concerns**:
   - Implements common functionality needed across multiple modules
   - Centralizes implementation of repeated patterns
   - Provides standard interfaces for common operations

2. **Low-Level Integration**:
   - Bridges between C++, Objective-C, and platform APIs
   - Handles platform-specific details
   - Provides abstractions over macOS-specific functionality

3. **Memory Management**:
   - Implements RAII (Resource Acquisition Is Initialization) pattern
   - Provides reference counting mechanisms
   - Manages object lifecycles and resource cleanup

### Implementation Approach

The Utilities module employs several implementation strategies:

1. **COM-Style Implementation**:
   - Uses COM-like interfaces and reference counting
   - Implements QueryInterface pattern for object identity
   - Provides standardized memory management

2. **Macro-Based Utilities**:
   - Uses preprocessor macros for common patterns
   - Reduces boilerplate code
   - Centralizes implementation details

3. **Platform Integration**:
   - Wraps macOS-specific APIs
   - Handles platform error codes and exceptions
   - Provides abstractions over platform implementation details

## Files

### comimpl.h
Provides COM-like implementation utilities, including:
- Reference counting implementation
- QueryInterface pattern implementation
- COM interface inheritance
- Smart pointers for COM objects

**Implementation Details:**

The COM implementation utilities provide a framework for COM-like interfaces:

```cpp
// Base COM interface implementation
class IUnknown
{
public:
    virtual HRESULT QueryInterface(IID riid, void** ppvObject) = 0;
    virtual ULONG AddRef() = 0;
    virtual ULONG Release() = 0;
};

// COM pointer implementation
template<typename T>
class ComPtr
{
private:
    T* _ptr;
    
public:
    ComPtr() : _ptr(nullptr) { }
    
    ComPtr(T* ptr) : _ptr(ptr)
    {
        if(_ptr != nullptr)
            _ptr->AddRef();
    }
    
    ComPtr(const ComPtr<T>& other) : _ptr(other._ptr)
    {
        if(_ptr != nullptr)
            _ptr->AddRef();
    }
    
    ~ComPtr()
    {
        if(_ptr != nullptr)
            _ptr->Release();
    }
    
    // Operator overloads for smart pointer functionality
    T* operator->() const { return _ptr; }
    operator T*() const { return _ptr; }
    
    // Assignment operators
    ComPtr<T>& operator=(T* ptr)
    {
        if(_ptr != nullptr)
            _ptr->Release();
        
        _ptr = ptr;
        
        if(_ptr != nullptr)
            _ptr->AddRef();
        
        return *this;
    }
    
    ComPtr<T>& operator=(const ComPtr<T>& other)
    {
        if(_ptr != nullptr)
            _ptr->Release();
        
        _ptr = other._ptr;
        
        if(_ptr != nullptr)
            _ptr->AddRef();
        
        return *this;
    }
};

// Base implementation class for COM objects
template<typename T, const IID* piid>
class ComSingleObject : public T
{
private:
    std::atomic<ULONG> _refCount;
    
public:
    ComSingleObject() : _refCount(1) { }
    
    virtual ~ComSingleObject() { }
    
    // IUnknown implementation
    virtual HRESULT QueryInterface(IID riid, void** ppvObject) override
    {
        if(riid == *piid || riid == IID_IUnknown)
        {
            AddRef();
            *ppvObject = this;
            return S_OK;
        }
        
        *ppvObject = nullptr;
        return E_NOINTERFACE;
    }
    
    virtual ULONG AddRef() override
    {
        return ++_refCount;
    }
    
    virtual ULONG Release() override
    {
        auto newCount = --_refCount;
        
        if(newCount == 0)
            delete this;
        
        return newCount;
    }
};
```

### common.h
Provides common definitions and utilities, including:
- Error handling macros
- Common data structures
- Platform compatibility wrappers
- Standard interface patterns

**Implementation Details:**

The common utilities provide a set of helper macros and structures:

```cpp
// Error handling macros for COM methods
#define START_COM_CALL HRESULT hresult = S_OK; try {
#define END_COM_CALL                         \
    return hresult;                         \
    }                                        \
    catch(AvnException& ex)                 \
    {                                        \
        return ex.GetResult();              \
    }                                        \
    catch(...)                              \
    {                                        \
        return E_FAIL;                      \
    }

// Forward all IUnknown methods to the base class implementation
#define FORWARD_IUNKNOWN() \
    virtual HRESULT QueryInterface(IID riid, void** ppvObject) override { return ComSingleObject::QueryInterface(riid, ppvObject); } \
    virtual ULONG AddRef() override { return ComSingleObject::AddRef(); } \
    virtual ULONG Release() override { return ComSingleObject::Release(); }

// Exception class for COM error handling
class AvnException
{
private:
    HRESULT _result;
    
public:
    AvnException(HRESULT result) : _result(result) { }
    
    HRESULT GetResult() const { return _result; }
};

// Throw a COM exception with a specific HRESULT
inline void AvnThrowIfFailed(HRESULT result)
{
    if(FAILED(result))
        throw AvnException(result);
}

// Common pixel structure
struct AvnPixelSize
{
    int Width;
    int Height;
    
    AvnPixelSize() : Width(0), Height(0) { }
    AvnPixelSize(int width, int height) : Width(width), Height(height) { }
};

// Common point structure
struct AvnPoint
{
    double X;
    double Y;
    
    AvnPoint() : X(0), Y(0) { }
    AvnPoint(double x, double y) : X(x), Y(y) { }
};

// Common size structure
struct AvnSize
{
    double Width;
    double Height;
    
    AvnSize() : Width(0), Height(0) { }
    AvnSize(double width, double height) : Width(width), Height(height) { }
};
```

### AvnString.h / AvnString.mm
Provides string handling utilities for cross-platform string compatibility, including:
- String conversion between platform formats
- UTF-8/UTF-16 conversions
- String allocation and management
- String comparison utilities

**Implementation Details:**

The AvnString class provides string handling capabilities:

```cpp
class AvnString : public ComSingleObject<IAvnString, &IID_IAvnString>
{
private:
    char* _ptr;
    size_t _length;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnString(const char* ptr, size_t length = -1)
    {
        if(length == -1)
            length = strlen(ptr);
        
        _length = length;
        _ptr = new char[length + 1];
        memcpy(_ptr, ptr, length);
        _ptr[length] = 0;
    }
    
    AvnString(NSString* nsString)
    {
        if(nsString == nil)
        {
            _ptr = new char[1];
            _ptr[0] = 0;
            _length = 0;
            return;
        }
        
        const char* utf8 = [nsString UTF8String];
        _length = strlen(utf8);
        _ptr = new char[_length + 1];
        memcpy(_ptr, utf8, _length + 1);
    }
    
    virtual ~AvnString()
    {
        delete[] _ptr;
    }
    
    // IAvnString implementation
    virtual HRESULT GetUTF8String(void* ptr, int len) override
    {
        START_COM_CALL;
        
        if(len < 0)
            return E_INVALIDARG;
        
        auto dest = (char*)ptr;
        
        if(len == 0)
        {
            // Just return the required length
            return (HRESULT)_length;
        }
        
        size_t toCopy = std::min((size_t)len - 1, _length);
        memcpy(dest, _ptr, toCopy);
        dest[toCopy] = 0;
        
        return (HRESULT)_length;
    }
    
    const char* GetString() const { return _ptr; }
    size_t GetLength() const { return _length; }
};

// Create a managed string from an NSString
ComPtr<IAvnString> CreateAvnString(NSString* nsString)
{
    return nsString == nullptr ? nullptr : new AvnString(nsString);
}

// Convert a C string to NSString
NSString* ToNSString(const char* str)
{
    return str == nullptr ? nil : [NSString stringWithUTF8String:str];
}
```

### KeyTransform.h / KeyTransform.mm
Provides keyboard input transformation utilities, including:
- Key code mapping between platforms
- Keyboard modifiers handling
- Key event transformations
- Character encoding utilities

**Implementation Details:**

The KeyTransform utilities provide mapping between platform-specific key codes:

```cpp
// Map from macOS virtual key to Avalonia key
AvnKey VirtualKeyFromNSKey(unsigned short keyCode)
{
    switch(keyCode)
    {
        case 0x00: return AvnKeyA;
        case 0x01: return AvnKeyS;
        case 0x02: return AvnKeyD;
        case 0x03: return AvnKeyF;
        case a0x04: return AvnKeyH;
        case 0x05: return AvnKeyG;
        case 0x06: return AvnKeyZ;
        case 0x07: return AvnKeyX;
        // Many more mappings...
        
        default: return AvnKeyNone;
    }
}

// Map from Avalonia key to macOS menu character
unichar MenuCharFromVirtualKey(AvnKey key)
{
    switch(key)
    {
        case AvnKeyA: return 'a';
        case AvnKeyB: return 'b';
        case AvnKeyC: return 'c';
        case AvnKeyD: return 'd';
        // Many more mappings...
        
        case AvnKeyDelete: return NSDeleteCharacter;
        case AvnKeyBack: return NSBackspaceCharacter;
        case AvnKeyReturn: return NSCarriageReturnCharacter;
        case AvnKeyEscape: return 27;
        
        default: return 0;
    }
}

// Convert NSEvent modifiers to Avalonia modifiers
AvnInputModifiers ModifiersFromNSEvent(NSEvent* event)
{
    NSEventModifierFlags modifiers = [event modifierFlags];
    AvnInputModifiers result = NoModifiers;
    
    if(modifiers & NSEventModifierFlagControl)
        result = (AvnInputModifiers)(result | Control);
    
    if(modifiers & NSEventModifierFlagShift)
        result = (AvnInputModifiers)(result | Shift);
    
    if(modifiers & NSEventModifierFlagOption)
        result = (AvnInputModifiers)(result | Alt);
    
    if(modifiers & NSEventModifierFlagCommand)
        result = (AvnInputModifiers)(result | Windows);
    
    return result;
}
```

### AvnAutoreleasePool.h
Provides AutoreleasePool wrapper to ensure proper Objective-C object cleanup, including:
- RAII pattern for autorelease pools
- Exception-safe resource management
- Nested pool support

**Implementation Details:**

The AutoreleasePool wrapper provides RAII management for Objective-C autorelease pools:

```cpp
class AvnAutoreleasePool
{
private:
    NSAutoreleasePool* _pool;
    
public:
    AvnAutoreleasePool() 
    {
        _pool = [[NSAutoreleasePool alloc] init];
    }
    
    ~AvnAutoreleasePool()
    {
        [_pool drain];
    }
    
    // Prevent copying
    AvnAutoreleasePool(const AvnAutoreleasePool&) = delete;
    AvnAutoreleasePool& operator=(const AvnAutoreleasePool&) = delete;
};

#define BEGIN_AUTORELEASEPOOL { AvnAutoreleasePool pool;
#define END_AUTORELEASEPOOL }
```

## Utility Patterns and Practices

### Error Handling Pattern

1. **COM Error Handling**:
   - Use START_COM_CALL and END_COM_CALL macros to encapsulate method bodies
   - Convert exceptions to HRESULT values
   - Provide structured error information to managed code

```cpp
HRESULT MyComMethod(int param)
{
    START_COM_CALL;
    
    // Method implementation
    if(param < 0)
        throw AvnException(E_INVALIDARG);
    
    // Success path
    DoSomething(param);
    
    END_COM_CALL;
}
```

### Memory Management Pattern

1. **RAII Resources**:
   - Use constructor/destructor pairs for resource acquisition/release
   - Ensure resources are properly released even with exceptions
   - Avoid manual resource management where possible

```cpp
class ScopedResource
{
private:
    Resource* _resource;
    
public:
    ScopedResource() : _resource(AcquireResource()) { }
    ~ScopedResource() { if(_resource) ReleaseResource(_resource); }
    
    // Access the resource
    Resource* Get() { return _resource; }
    
    // Prevent copying
    ScopedResource(const ScopedResource&) = delete;
    ScopedResource& operator=(const ScopedResource&) = delete;
};
```

### String Handling Pattern

1. **Safe String Conversion**:
   - Always handle UTF-8/UTF-16 conversions properly
   - Use AvnString for cross-boundary string passing
   - Check for NULL strings and handle empty strings gracefully

```cpp
// Convert from managed string to NSString safely
NSString* ConvertToNSString(IAvnString* str)
{
    if(str == nullptr)
        return @"";
    
    char buffer[1024];
    int requiredLength = str->GetUTF8String(buffer, sizeof(buffer));
    
    if(requiredLength < sizeof(buffer))
    {
        return [NSString stringWithUTF8String:buffer];
    }
    else
    {
        // Handle long strings
        char* longBuffer = new char[requiredLength + 1];
        str->GetUTF8String(longBuffer, requiredLength + 1);
        NSString* result = [NSString stringWithUTF8String:longBuffer];
        delete[] longBuffer;
        return result;
    }
}
```

### controlhost.mm
Implements hosting of native controls in Avalonia:
- Provides embedding of NSViews in Avalonia applications
- Handles input forwarding to embedded controls
- Manages focus and keyboard navigation
- Coordinates layout between Avalonia and native controls
- Implements event routing between frameworks

### deadlock.mm
Utilities for detecting and handling deadlocks:
- Implements deadlock detection mechanisms
- Provides timeout-based deadlock recovery
- Logs deadlock diagnostic information
- Implements prevention strategies
- Helps with debugging thread synchronization issues

### noarc.mm
Contains code that must run without Automatic Reference Counting:
- Implements manual memory management code
- Handles scenarios where ARC is inappropriate
- Provides compatibility with non-ARC code
- Manages object references manually
- Interfaces with legacy Objective-C patterns 
