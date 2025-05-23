//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//
#pragma once
#import <Foundation/Foundation.h>

#import <AppKit/AppKit.h>
#include "common.h"
#include "WindowImpl.h"
#include "WindowOverlayImpl.h"
#include "TopLevelImpl.h"
#include "KeyTransform.h"

@class AvnAccessibilityElement;
@protocol IRenderTarget;

@interface AvnView : NSView<NSTextInputClient, NSDraggingDestination, AvnTextInputMethodDelegate, CALayerDelegate>
-(AvnView* _Nonnull) initWithParent: (TopLevelImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
-(void) onClosed;
-(void) setModifiers:(NSEventModifierFlags)modifierFlags;
-(void) resetPressedMouseButtons;

-(AvnPlatformResizeReason) getResizeReason;
-(void) setResizeReason:(AvnPlatformResizeReason)reason;
-(void) setRenderTarget:(NSObject<IRenderTarget>* _Nonnull)target;
-(void) raiseAccessibilityChildrenChanged;

@property (readonly, assign) WindowImpl* parent;

@end
