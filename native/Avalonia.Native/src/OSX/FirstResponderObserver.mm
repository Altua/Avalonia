#import <AppKit/AppKit.h>
#import "WindowImpl.h"
#import "FirstResponderObserver.h"

@implementation FirstResponderObserver
{
    AvnView* _view;
}


- (instancetype)initWithView: (AvnView*) view
{
    _view = view;
    [[_view window] addObserver:self
                     forKeyPath:@"firstResponder"
                        options:NSKeyValueObservingOptionNew
                        context:nil];
    return self;
}

- (void)dealloc
{
    [[_view window] removeObserver:self forKeyPath:@"firstResponder"];
}

-(void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSKeyValueChangeKey,id> *)change context:(void *)context
{
    if ([keyPath isEqualToString:@"firstResponder"] && [object isKindOfClass:[NSWindow class]])
    {
        WindowImpl* parent = _view.parent;
        if(parent != nullptr)
        {
            id firstResponder = [change valueForKey:NSKeyValueChangeNewKey];
            dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
                NSString* firstResponderName = NSStringFromClass([firstResponder class]);
                parent->WindowEvents->LogFirstResponder([firstResponderName UTF8String]);
            });
        }
    }
}

@end
