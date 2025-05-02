# Accessibility and Automation

## Overview
The Accessibility and Automation module provides support for accessibility features and UI automation on macOS. These components enable Avalonia applications to be accessible to users with disabilities and support automated testing frameworks.

## Files

### automation.h / automation.mm
Implements macOS accessibility and UI automation support:
- Creates accessibility element hierarchy
- Maps Avalonia UI elements to NSAccessibility protocol
- Implements accessibility properties and actions
- Provides automation API for automated testing
- Handles accessibility notifications

### AvnAccessibility.h
Defines accessibility interfaces and structures:
- Declares accessibility property identifiers
- Establishes accessibility interface contracts
- Defines accessibility event types
- Provides accessibility property structures
- Standardizes accessibility communication protocols

### AvnAutomationNode.h
Implements automation node hierarchy for accessibility:
- Represents UI elements in the automation tree
- Provides navigation between automation nodes
- Implements automation properties and patterns
- Enables querying of UI element properties
- Supports automated interaction with UI elements 
