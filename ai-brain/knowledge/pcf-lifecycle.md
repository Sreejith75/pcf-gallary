# PCF Component Lifecycle

## Overview
This document provides factual information about the PowerApps Component Framework (PCF) component lifecycle. It contains no reasoning logic—only reference material.

## Component Structure

A PCF component consists of:

1. **Manifest File** (`ControlManifest.Input.xml`)
   - Defines component metadata
   - Declares properties and data bindings
   - Specifies resources (code, CSS, images)
   - Sets framework version requirements

2. **Implementation File** (TypeScript)
   - Implements `IStandardControl` interface
   - Contains lifecycle methods
   - Handles rendering and updates

3. **Resource Files**
   - CSS for styling
   - RESX for localization
   - Images and other assets

## Lifecycle Methods

### init()
**Purpose**: Initialize the component

**Called**: Once when component is first loaded

**Parameters**:
- `context`: Component context with utilities and data
- `notifyOutputChanged`: Callback to notify framework of output changes
- `state`: Persisted state from previous session (if any)
- `container`: HTML element to render into

**Responsibilities**:
- Create DOM structure
- Attach event listeners
- Initialize component state
- Store references to context and notifyOutputChanged

### updateView()
**Purpose**: Update component when data changes

**Called**: 
- After init()
- When bound data changes
- When container size changes
- When component is shown after being hidden

**Parameters**:
- `context`: Updated component context

**Responsibilities**:
- Read updated property values from context
- Update DOM to reflect new data
- Handle visibility changes
- Respond to size changes

**Performance Note**: This method is called frequently. Keep it fast.

### getOutputs()
**Purpose**: Return current output values

**Called**: When framework needs output values (after notifyOutputChanged callback)

**Returns**: Object with output property values

**Responsibilities**:
- Return current state of output properties
- Ensure values match declared types

### destroy()
**Purpose**: Clean up resources

**Called**: When component is removed from DOM

**Responsibilities**:
- Remove event listeners
- Clear timers and intervals
- Release large objects
- Prevent memory leaks

## Context Object

The `context` parameter provides:

### context.parameters
Access to input/output properties defined in manifest

```typescript
// For property defined as:
// <property name="value" type="Whole.None" usage-hint="bound" />

const currentValue = context.parameters.value.raw;
const formattedValue = context.parameters.value.formatted;
```

### context.mode
Information about component state:
- `isControlDisabled`: Whether control should be disabled
- `isVisible`: Whether control is visible
- `label`: Display label for the control

### context.utils
Utility functions:
- `getImageResource()`: Load image resources
- `hasEntityPrivilege()`: Check user permissions
- `lookupObjects()`: Perform lookups

### context.webAPI
Dataverse Web API access (limited operations)

### context.factory
Create sub-components (popup, device features)

## Property Types

### Supported Data Types
- `SingleLine.Text`: Single line of text
- `Multiple`: Multi-line text
- `Whole.None`: Integer number
- `Decimal`: Decimal number
- `TwoOptions`: Boolean
- `DateAndTime.DateOnly`: Date without time
- `DateAndTime.DateAndTime`: Date with time
- `Currency`: Currency value
- `Lookup.Simple`: Reference to another record
- `OptionSet`: Single-select dropdown
- `MultiSelectOptionSet`: Multi-select dropdown

### Property Usage
- `bound`: Two-way data binding with field
- `input`: Configuration parameter (one-way)
- `output`: Output value only

## Build and Deployment

### Build Process
1. `npm install` - Install dependencies
2. `npm run build` - Compile TypeScript and bundle
3. `pac pcf push` - Deploy to test environment

### Output
- Compiled JavaScript bundle
- Minified CSS
- Solution ZIP file for import

## Constraints

### Security
- No direct external API calls
- No eval() or Function() constructor
- Content Security Policy compliant

### Performance
- Keep updateView() fast (< 100ms recommended)
- Minimize DOM manipulations
- Use efficient event handling

### Offline
- Must work without internet connection
- No CDN dependencies in production
- Bundle all required resources

## Best Practices

1. **Initialization**: Do heavy work in init(), not updateView()
2. **Event Handling**: Use event delegation when possible
3. **Memory**: Always clean up in destroy()
4. **Accessibility**: Include ARIA attributes
5. **Responsiveness**: Handle container resize in updateView()
6. **Localization**: Use RESX files for all user-facing text

## References

- [PCF Official Documentation](https://docs.microsoft.com/powerapps/developer/component-framework/)
- [PCF Gallery](https://pcf.gallery/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
