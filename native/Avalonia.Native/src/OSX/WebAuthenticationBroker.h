//
//  WebAuthenticationBroker.h
//  Avalonia.Native.OSX
//
//  Created by Adarsh Bhat on 15/12/25.
//  Copyright © 2025 Avalonia. All rights reserved.
//
#pragma once

#import <AppKit/AppKit.h>
#import <WebKit/WebKit.h>
#include "common.h"

@interface WebAuthenticationController : NSObject<WKNavigationDelegate, NSWindowDelegate>
@property (nonatomic, copy) NSString* redirectUrl;
@property (nonatomic, copy) NSString* requestUrl;
@property (nonatomic, copy) void (^completion)(NSString* url);
@end

class WebAuthenticationBroker : public ComSingleObject<IAvnWebAuthenticationBroker, &IID_IAvnWebAuthenticationBroker>
{
public:
    FORWARD_IUNKNOWN()
    
    ~WebAuthenticationBroker()
    {
        _controller = nil;
    }

    virtual void Authenticate(const char* authUrl,
                              const char* redirectUrl,
                              IAvnSystemDialogEvents* events) override;
    
private:
    void Run(const char* url, const char* callbackUrl);
    void OnComplete(NSString* url);
    
    WebAuthenticationController* _controller = nil;
    ComPtr<IAvnSystemDialogEvents> _events;
};

extern IAvnWebAuthenticationBroker* CreateWebAuthenticationBroker();
