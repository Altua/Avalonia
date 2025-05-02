# Utilities

## Overview
The Utilities module provides common functionality, helper classes, and support code used throughout the Avalonia native macOS implementation. These components offer shared infrastructure and reusable elements for the rest of the codebase.

## Files

### common.h
Contains common definitions, types, and utilities shared across the codebase:
- Defines common data structures
- Provides macros for common operations
- Declares shared constants and enumerations
- Implements COM interface helpers
- Establishes error handling patterns

### AvnString.h / AvnString.mm
Provides string utilities and conversions:
- Implements UTF-8 string handling
- Provides conversions between NSString and C strings
- Manages string memory and lifecycle
- Implements string comparison utilities
- Supports string manipulation operations

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
