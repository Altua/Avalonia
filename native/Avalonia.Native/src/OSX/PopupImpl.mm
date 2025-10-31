//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#include "WindowInterfaces.h"
#include "AvnView.h"
#include "WindowImpl.h"
#include "automation.h"
#include "menu.h"
#include "common.h"
#import "WindowBaseImpl.h"
#import "WindowProtocol.h"
#import <AppKit/AppKit.h>

class PopupImpl : public virtual WindowBaseImpl, public IAvnPopup
{
private:
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnPopup, IID_IAvnPopup)
    END_INTERFACE_MAP()
    virtual ~PopupImpl(){}
    ComPtr<IAvnWindowEvents> WindowEvents;
    PopupImpl(IAvnWindowEvents* events) : TopLevelImpl(events), WindowBaseImpl(events)
    {
        WindowEvents = events;
        [Window setLevel:NSPopUpMenuWindowLevel];
    }
protected:
    virtual NSWindowStyleMask CalculateStyleMask() override
    {
        return NSWindowStyleMaskBorderless;
    }

public:
    
    HRESULT SetParent(IAvnWindowBase *parent) override
    {
        auto result = WindowBaseImpl::SetParent(parent);
        
        // if PP textbox is firstResponder then it will not relinquish the keyboard focus, if the popup can't become a key window.
        if (Parent != nullptr && Parent->IsOverlay() && ![NSApp.mainWindow.firstResponder isKindOfClass:[AvnView class]])
        {
            [GetWindowProtocol() setCanBecomeKeyWindow: true];
        }
        
        return result;
    }
    
    virtual HRESULT Show(bool activate, bool isDialog) override
    {
        auto windowProtocol = GetWindowProtocol();
        
        [windowProtocol setEnabled:true];
        
        return WindowBaseImpl::Show(activate, true);
    }
    
    virtual bool ShouldTakeFocusOnShow() override
    {
        // Don't steal the focus from another windows if our parent is inactive
        if (Parent != nullptr && Parent->Window != nullptr && ![Parent->Window isKeyWindow])
            return false;
        
        // Don't steal focus when user hovers mouse over powerpoint while another application is focused
        if (Parent->IsOverlay())
            return false;

        return WindowBaseImpl::ShouldTakeFocusOnShow();
    }
    
    HRESULT SetIgnoresMouseEvents(bool value) override {
        START_COM_CALL;

        @autoreleasepool {
            if (Window != nullptr)
            {
                [Window setIgnoresMouseEvents:value];
            }

            return S_OK;
        }
    }
};


extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events)
{
    @autoreleasepool
    {
        IAvnPopup* ptr = dynamic_cast<IAvnPopup*>(new PopupImpl(events));
        return ptr;
    }
}
