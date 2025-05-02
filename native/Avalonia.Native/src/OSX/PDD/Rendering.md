# Rendering

## Overview
The Rendering module implements graphics rendering capabilities for Avalonia on macOS. It provides hardware-accelerated rendering through Metal and OpenGL, along with abstractions for render targets and frame timing.

## Files

### metal.mm
Implements Metal rendering interface for hardware-accelerated graphics. This file:
- Provides Metal device management
- Implements Metal render target support
- Handles Metal command buffer and queue management
- Creates and manages Metal rendering sessions
- Provides hardware-accelerated rendering capabilities

### cgl.mm
Implements OpenGL rendering interface for hardware-accelerated graphics. This file:
- Provides OpenGL context creation and management
- Sets up OpenGL rendering surfaces
- Manages OpenGL state for rendering
- Facilitates OpenGL texture creation and usage
- Implements OpenGL fallback when Metal is not available

### rendertarget.mm
Provides abstraction for rendering targets, enabling:
- Platform-agnostic rendering surface management
- Pixel format handling for different backends
- Texture management for rendering
- Scaling and DPI-aware rendering
- Rendering synchronization

### PlatformRenderTimer.mm
Handles timing for rendering frames, providing:
- Frame-rate management
- Timer-based rendering
- VSync integration
- Render loop management
- Frame scheduling based on display refresh rate 
