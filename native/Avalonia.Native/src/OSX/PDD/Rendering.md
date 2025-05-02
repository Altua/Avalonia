# Rendering

## Overview
The Rendering module implements graphics rendering capabilities for Avalonia on macOS. It provides hardware-accelerated rendering through Metal and OpenGL, along with abstractions for render targets and frame timing.

## Architecture and Implementation Pattern

### Multi-Backend Rendering Architecture

The Rendering module follows a flexible, multi-backend architecture:

1. **Backend Abstraction**:
   - Supports multiple rendering backends (Metal, OpenGL)
   - Defines common interfaces across rendering technologies
   - Allows applications to choose the most appropriate backend

2. **Layer-Based Rendering**:
   - Leverages Core Animation layers for composition
   - Utilizes hardware-accelerated compositing
   - Integrates with macOS rendering system

3. **Command-Based Pipeline**:
   - Implements a command-based rendering approach
   - Separates command preparation from execution
   - Enables efficient resource management and synchronization

### Implementation Strategy

The rendering implementation follows several key strategies:

1. **Bridge Pattern**:
   - Creates Objective-C wrapper classes around rendering targets
   - Bridges between C++ interfaces and Objective-C rendering APIs
   - Maintains consistent semantics across language boundaries

2. **RAII Resource Management**:
   - Uses C++ RAII pattern for resource cleanup
   - Ensures proper release of rendering resources
   - Prevents resource leaks in rendering pipelines

3. **Lazy Initialization**:
   - Defers creation of rendering resources until needed
   - Handles device loss and recreation gracefully
   - Minimizes resource usage when not actively rendering

## Files

### metal.mm
Implements Metal rendering interface for hardware-accelerated graphics. This file:
- Provides Metal device management
- Implements Metal render target support
- Handles Metal command buffer and queue management
- Creates and manages Metal rendering sessions
- Provides hardware-accelerated rendering capabilities

**Implementation Details:**

The Metal implementation consists of several key components:

1. **Metal Device Management**:

```cpp
class AvnMetalDevice : public ComSingleObject<IAvnMetalDevice, &IID_IAvnMetalDevice>
{
public:
    id<MTLDevice> device;
    id<MTLCommandQueue> queue;
    FORWARD_IUNKNOWN()

    void *GetDevice() override {
        return (__bridge void*) device;
    }

    void *GetQueue() override {
        return (__bridge void*) queue;
    }

    AvnMetalDevice(id <MTLDevice> device, id <MTLCommandQueue> queue) : device(device), queue(queue) {
    }
};

// Factory method to create Metal devices
class AvnMetalDisplay : public ComSingleObject<IAvnMetalDisplay, &IID_IAvnMetalDisplay>
{
public:
    FORWARD_IUNKNOWN()
    HRESULT CreateDevice(IAvnMetalDevice **ret) override {
        auto device = MTLCreateSystemDefaultDevice();
        if(device == nil) {
            ret = nil;
            return E_FAIL;
        }
        auto queue = [device newCommandQueue];
        *ret = new AvnMetalDevice(device, queue);
        return S_OK;
    }
};
```

2. **Metal Rendering Sessions**:

```cpp
class AvnMetalRenderSession : public ComSingleObject<IAvnMetalRenderingSession, &IID_IAvnMetalRenderingSession>
{
    id<CAMetalDrawable> _drawable;
    id<MTLCommandQueue> _queue;
    id<MTLTexture> _texture;
    CAMetalLayer* _layer;
    AvnPixelSize _size;
    double _scaling;
public:
    FORWARD_IUNKNOWN()

    // Constructor initializes the session with drawable and size info
    AvnMetalRenderSession(AvnMetalDevice* device, CAMetalLayer* layer, 
                         id<CAMetalDrawable> drawable, const AvnPixelSize &size, 
                         double scaling)
        : _drawable(drawable), _size(size), _scaling(scaling), _queue(device->queue),
         _texture([drawable texture]) {
        _layer = layer;
    }

    // Destructor automatically presents the drawable to the screen
    ~AvnMetalRenderSession()
    {
        auto buffer = [_queue commandBuffer];
        [buffer presentDrawable: _drawable];
        [buffer commit];
    }
    
    // Surface properties
    HRESULT GetPixelSize(AvnPixelSize *ret) override {
        *ret = _size;
        return 0;
    }

    double GetScaling() override {
        return _scaling;
    }

    void *GetTexture() override {
        return (__bridge void*) _texture;
    }
};
```

3. **Metal Render Target**:

```cpp
class AvnMetalRenderTarget : public ComSingleObject<IAvnMetalRenderTarget, &IID_IAvnMetalRenderTarget>
{
    CAMetalLayer* _layer;
    double _scaling = 1;
    AvnPixelSize _size = {1,1};
    ComPtr<AvnMetalDevice> _device;
public:
    double PendingScaling = 1;
    AvnPixelSize PendingSize = {1,1};
    FORWARD_IUNKNOWN()
    
    // Create a render target with layer and device
    AvnMetalRenderTarget(CAMetalLayer* layer, ComPtr<AvnMetalDevice> device)
    {
        _layer = layer;
        _device = device;
    }

    // Begin a drawing session
    HRESULT BeginDrawing(IAvnMetalRenderingSession **ret) override {
        if([NSThread isMainThread])
        {
            // Flush all existing rendering
            auto buffer = [_device->queue commandBuffer];
            [buffer commit];
            [buffer waitUntilCompleted];
            _size = PendingSize;
            _scaling= PendingScaling;
            CGSize layerSize = {(CGFloat)_size.Width, (CGFloat)_size.Height};

            [_layer setDrawableSize: layerSize];
        }
        auto drawable = [_layer nextDrawable];
        if(drawable == nil)
        {
            ret = nil;
            return E_FAIL;
        }
        *ret = new AvnMetalRenderSession(_device, _layer, drawable, _size, _scaling);
        return 0;
    }
};
```

### cgl.mm
Implements OpenGL rendering interface for hardware-accelerated graphics. This file:
- Provides OpenGL context creation and management
- Sets up OpenGL rendering surfaces
- Manages OpenGL state for rendering
- Facilitates OpenGL texture creation and usage
- Implements OpenGL fallback when Metal is not available

**Implementation Details:**

The OpenGL implementation includes these key components:

1. **OpenGL Context Management**:

```cpp
class AvnGlContext : public virtual ComSingleObject<IAvnGlContext, &IID_IAvnGlContext>
{
public:
    CGLContextObj Context;
    int SampleCount = 0, StencilBits = 0;
    FORWARD_IUNKNOWN()
    
    // Saved context for proper context restoration
    class SavedGlContext : public virtual ComUnknownObject
    {
        CGLContextObj _savedContext;
        ComPtr<AvnGlContext> _parent;
    public:
        SavedGlContext(CGLContextObj saved, AvnGlContext* parent)
        {
            _savedContext = saved;
            _parent = parent;
            _parent->_usageCount++;
        }
        
        // RAII pattern to restore the previous context
        ~SavedGlContext()
        {
            if(_parent->Context == CGLGetCurrentContext())
                CGLSetCurrentContext(_savedContext);
            _parent->_usageCount--;
            CGLUnlockContext(_parent->Context);
        }
    };
    
    // Make the context current for rendering
    virtual HRESULT MakeCurrent(IUnknown** ppv) override
    {
        START_COM_CALL;
        
        CGLContextObj saved = CGLGetCurrentContext();
        CGLLockContext(Context);
        if(CGLSetCurrentContext(Context) != 0)
        {
            CGLUnlockContext(Context);
            return E_FAIL;
        }
        *ppv = new SavedGlContext(saved, this);
        
        return S_OK;
    }
    
    // Resource cleanup
    ~AvnGlContext()
    {
        CGLReleaseContext(Context);
    }
};
```

2. **OpenGL Display Implementation**:

```cpp
class AvnGlDisplay : public virtual ComSingleObject<IAvnGlDisplay, &IID_IAvnGlDisplay>
{
    void* _libgl;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnGlDisplay()
    {
        // Load the OpenGL library
        _libgl = dlopen("/System/Library/Frameworks/OpenGL.framework/Versions/A/Libraries/libGL.dylib", RTLD_LAZY);
    }
    
    // Get function pointers from OpenGL
    virtual void* GetProcAddress(char* proc) override
    {
        return dlsym(_libgl, proc);
    }
    
    // Create new OpenGL contexts
    virtual HRESULT CreateContext(IAvnGlContext* share, IAvnGlContext**ppv) override
    {
        START_COM_CALL;
        
        CGLContextObj shareContext = nil;
        if(share != nil)
        {
            AvnGlContext* shareCtx = dynamic_cast<AvnGlContext*>(share);
            if(shareCtx != nil)
                shareContext = shareCtx->Context;
        }
        CGLContextObj ctx = ::CreateCglContext(shareContext);
        if(ctx == nil)
            return E_FAIL;
        *ppv = new AvnGlContext(ctx);
        return S_OK;
    }
    
    // Wrap an existing native context
    virtual HRESULT WrapContext(void* native, IAvnGlContext**ppv) override
    {
        START_COM_CALL;
        
        if(native == nil)
            return E_INVALIDARG;
        *ppv = new AvnGlContext((CGLContextObj) native);
        return S_OK;
    }
};
```

### rendertarget.mm
Provides abstraction for rendering targets, enabling:
- Platform-agnostic rendering surface management
- Pixel format handling for different backends
- Texture management for rendering
- Scaling and DPI-aware rendering
- Rendering synchronization

**Implementation Details:**

The render target implementation provides a common interface for different rendering backends:

```cpp
class SoftwareRenderTarget : public ComSingleObject<IAvnSoftwareRenderTarget, &IID_IAvnSoftwareRenderTarget>
{
private:
    void* _buf;
    int _width, _height, _stride;
    double _scaling;
    AvnPixelFormat _format;
public:
    FORWARD_IUNKNOWN()
    
    // Constructor initializes the software rendering buffer
    SoftwareRenderTarget(void* buf, int width, int height, int stride, double scaling, AvnPixelFormat format)
    {
        _buf = buf;
        _width = width;
        _height = height;
        _stride = stride;
        _scaling = scaling;
        _format = format;
    }
    
    // Get rendering framebuffer
    virtual HRESULT GetFramebuffer(AvnFramebuffer* fb) override
    {
        START_COM_CALL;
        
        if(fb == nullptr)
            return E_POINTER;
        
        fb->Data = _buf;
        fb->Width = _width;
        fb->Height = _height;
        fb->Stride = _stride;
        fb->PixelFormat = _format;
        fb->Dpi.X = _scaling * 96.0f;
        fb->Dpi.Y = _scaling * 96.0f;
        
        return S_OK;
    }
};

// Factory function to create the appropriate render target
extern IAvnSoftwareRenderTarget* CreateSoftwareRenderTarget(void* buf, int width, int height, int stride, double scaling, AvnPixelFormat format)
{
    return new SoftwareRenderTarget(buf, width, height, stride, scaling, format);
}
```

### PlatformRenderTimer.mm
Handles timing for rendering frames, providing:
- Frame-rate management
- Timer-based rendering
- VSync integration
- Render loop management
- Frame scheduling based on display refresh rate

**Implementation Details:**

The render timer implementation synchronizes rendering with the display:

```cpp
@implementation AvnRenderTimer
{
    CVDisplayLinkRef _displayLink;
    ComPtr<IAvnPlatformRenderCallback> _cb;
    bool _valid;
}

// Initialize with display and callback
- (instancetype) initWithDisplay: (CGDirectDisplayID)displayId callback: (IAvnPlatformRenderCallback*) cb
{
    if(self = [super init])
    {
        _valid = false;
        _cb = cb;
        
        // Create a display link synchronized with the display's refresh rate
        CVReturn ret = CVDisplayLinkCreateWithCGDisplay(displayId, &_displayLink);
        if(ret != kCVReturnSuccess)
            return self;
        ret = CVDisplayLinkSetOutputCallback(_displayLink, &OnDisplayLinkFired, (void*)CFBridgingRetain(self));
        if(ret != kCVReturnSuccess)
            return self;
        _valid = true;
    }
    return self;
}

// Start the render timer
- (void) start
{
    if(_valid && !CVDisplayLinkIsRunning(_displayLink))
        CVDisplayLinkStart(_displayLink);
}

// Stop the render timer
- (void) stop
{
    if(_valid && CVDisplayLinkIsRunning(_displayLink))
        CVDisplayLinkStop(_displayLink);
}

// Display link callback - invokes the render callback
static CVReturn OnDisplayLinkFired(CVDisplayLinkRef displayLink,
                                   const CVTimeStamp *now,
                                   const CVTimeStamp *outputTime,
                                   CVOptionFlags flagsIn,
                                   CVOptionFlags *flagsOut,
                                   void *context)
{
    AvnRenderTimer* timer = (__bridge AvnRenderTimer*)context;
    @autoreleasepool
    {
        [timer onFire];
    }
    return kCVReturnSuccess;
}

// Fire the render callback
- (void) onFire
{
    if(_cb != nullptr)
        _cb->Render();
}
```

## Rendering Flow and Backend Selection

### Rendering Pipeline Flow

1. **Initialization**:
   - Avalonia initializes the appropriate rendering backend (Metal or OpenGL)
   - A render target is created with the selected backend
   - The render target is attached to the application's views

2. **Render Frame Sequence**:
   - PlatformRenderTimer triggers rendering at the display refresh rate
   - Avalonia prepares rendering commands
   - BeginDrawing creates a rendering session for the current frame
   - Avalonia renders to the session's target surface
   - Session completion presents the rendered content to the screen

3. **Backend-Specific Paths**:
   - Metal rendering uses CAMetalLayer and MTLTexture
   - OpenGL rendering uses CGLContext and GL textures
   - Software rendering uses CPU memory buffers
   - All backends provide a consistent interface to Avalonia

### Backend Selection Logic

The rendering system selects the appropriate backend based on:

1. **Hardware Capabilities**:
   - Checks if Metal is supported on the device
   - Falls back to OpenGL if Metal is unavailable
   - Uses software rendering as a last resort

2. **Application Requirements**:
   - Considers application-specified preferences
   - Balances performance vs. compatibility
   - Adapts to specific rendering feature needs 
