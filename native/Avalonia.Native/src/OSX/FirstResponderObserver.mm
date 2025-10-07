#import <AppKit/AppKit.h>
#import "WindowImpl.h"
#import "FirstResponderObserver.h"

@implementation FirstResponderObserver
{
    AvnView* _view;
    NSWindow* _window;
}


- (instancetype)initWithView: (AvnView*) view
{
    _view = view;
    _window = view.window;
    [_window addObserver:self
                     forKeyPath:@"firstResponder"
                        options:NSKeyValueObservingOptionNew
                        context:nil];
    return self;
}

- (void)dealloc
{
    [_window removeObserver:self forKeyPath:@"firstResponder"];
}

-(void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSKeyValueChangeKey,id> *)change context:(void *)context
{
    WindowImpl* parent = _view.parent;
    if(parent != nullptr)
    {
        id firstResponder = [change valueForKey:NSKeyValueChangeNewKey];
        NSString* firstResponderName = NSStringFromClass([firstResponder class]);
        parent->WindowEvents->LogFirstResponder([firstResponderName UTF8String]);
    }
}

@end
