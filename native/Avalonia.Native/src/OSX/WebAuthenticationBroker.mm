#include <AppKit/AppKit.h>
#include <WebKit/WebKit.h>
#include <dispatch/dispatch.h>
#include <objc/runtime.h>
#include "WebAuthenticationBroker.h"
#include "AvnString.h"
#include "INSWindowHolder.h"

@implementation WebAuthenticationController
{
    NSWindow* _window;
    WKWebView* _webView;
}

-(instancetype)initWithUrl: (NSString*)url redirectUrl:(NSString*)redirectUrl
{
    self = [super init];
    if (self != nil)
    {
        self.requestUrl = url;
        self.redirectUrl = redirectUrl;
        
        _window = [[NSWindow alloc] initWithContentRect:NSMakeRect(0, 0, 900, 700)
                                              styleMask:(NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskResizable)
                                                backing:NSBackingStoreBuffered
                                                  defer:NO];
        _window.title = @"Sign in";

        _webView = [[WKWebView alloc] initWithFrame: _window.contentView.bounds];
        _webView.autoresizingMask  = (NSViewWidthSizable | NSViewHeightSizable);
        [_window.contentView addSubview:_webView];
        _window.releasedWhenClosed = NO;
        
        _window.delegate = self;
        _webView.navigationDelegate = self;
    }
    
    return self;
}

-(void)dealloc
{
}

-(void)run
{
    NSURL* requestUrl = [NSURL URLWithString:self.requestUrl];
    NSURLRequest* request = [NSURLRequest requestWithURL:requestUrl];
    [_webView loadRequest:request];

    [self runModalSession];
}

- (void)runModalSession
{
    NSModalSession session = [NSApp beginModalSessionForWindow: _window];
    NSTimer* timer = [NSTimer scheduledTimerWithTimeInterval:1/60.0f repeats:YES block:^(NSTimer * _Nonnull timer) {
        NSModalResponse resp = [NSApp runModalSession:session];
        if (resp != NSModalResponseContinue) {
            [timer invalidate];
            [NSApp endModalSession:session];
        }
    }];
}

- (void)onComplete: (NSString*) url
{
    if (self.completion)
    {
        self.completion(url);
        self.completion = nil;
        
        [NSApp stopModal];
        _window.delegate = nil;
    }
}

- (void)webView:(WKWebView *)webView decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    NSString* url = navigationAction.request.URL.absoluteString;

    if (url != nil && [url hasPrefix:self.redirectUrl])
    {
        decisionHandler(WKNavigationActionPolicyCancel);
        [_window orderOut: nil];
        [self onComplete: url];
    }
    else
    {
        decisionHandler(WKNavigationActionPolicyAllow);
    }
}

- (void)windowWillClose:(NSNotification *)notification
{
    [self onComplete:nil];
}

@end

void WebAuthenticationBroker::Authenticate(
                              const char* authUrl,
                              const char* redirectUrl,
                              IAvnSystemDialogEvents* events)
{
    START_COM_CALL;
    
    if (authUrl == nullptr || redirectUrl == nullptr || events == nullptr)
    {
        return;
    }
    
    _events = events;
    Run(authUrl, redirectUrl);
}

void WebAuthenticationBroker::Run(const char* authUrl, const char* redirectUrl)
{
    @autoreleasepool {
        NSMutableCharacterSet* charSet = [NSMutableCharacterSet whitespaceAndNewlineCharacterSet];
        [charSet invert];
        
        NSString* startUrl = [[NSString stringWithUTF8String:authUrl] stringByAddingPercentEncodingWithAllowedCharacters: charSet];
        
        NSString* callbackUrl = [NSString stringWithUTF8String:redirectUrl];
        if (startUrl == nil || callbackUrl == nil)
        {
            OnComplete(nil);
            return;
        }
        
        _controller = [[WebAuthenticationController alloc] initWithUrl: startUrl redirectUrl: callbackUrl];
        _controller.completion = ^(NSString *url) {
            this->OnComplete(url);
        };
        
        [_controller run];
    }
}

void WebAuthenticationBroker::OnComplete(NSString *url)
{
    if (url != nil)
    {
        auto uriStrings = CreateAvnStringArray(url);
        _events->OnCompleted(uriStrings);
    }
    else
    {
        _events->OnCompleted(nullptr);
    }    
}

IAvnWebAuthenticationBroker* CreateWebAuthenticationBroker()
{
    return new WebAuthenticationBroker();
}
