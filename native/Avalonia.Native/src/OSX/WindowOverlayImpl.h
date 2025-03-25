#ifndef AVALONIA_NATIVE_OSX_WINDOWOVERLAYIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWOVERLAYIMPL_H

#include "common.h"
#include "WindowImpl.h"
#include "AvnView.h"

class WindowOverlayImpl : public virtual WindowImpl
{
private:
    NSWindow* parentWindow;
    NSView* parentView;
    NSView* canvasView;
    NSColorPanel* colorPanel;
    bool isTrackingMouse;
    NSArray* eventMonitors;
    bool closed;
    FORWARD_IUNKNOWN()
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    END_INTERFACE_MAP()
    void InitializeColorPicker();
    static AvnInputModifiers GetCommandModifier(NSEventModifierFlags modFlag);
public:
    WindowOverlayImpl(void* parentWindow, char* parentView, IAvnWindowEvents* events);
    virtual ~WindowOverlayImpl();
    virtual bool IsOverlay() override;
    virtual HRESULT GetScaling(double *ret) override;
    virtual HRESULT PointToClient(AvnPoint point, AvnPoint *ret) override;
    virtual HRESULT PointToScreen(AvnPoint point, AvnPoint *ret) override;
    virtual HRESULT GetPPTClipViewOrigin(AvnPoint *ret) override;
    virtual HRESULT TakeScreenshot(void** ret, int* retLength) override;
    virtual HRESULT PickColor(AvnColor color, bool* cancel, AvnColor* ret) override;
    virtual HRESULT Activate() override;
    virtual HRESULT Close() override;
};

#endif
