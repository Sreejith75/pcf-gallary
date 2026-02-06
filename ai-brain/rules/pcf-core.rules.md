# PCF Core Rules

## Overview
Non-negotiable constraints that ensure PCF compliance and component functionality. All rules in this file have **error** severity unless otherwise specified.

---

## Naming Conventions

### RULE: PCF_NAMING_001
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Component name must be PascalCase and contain only alphanumeric characters.

**Rationale**: PCF framework requirement for TypeScript class naming.

**Validation**:
- Component name matches regex: `^[A-Z][A-Za-z0-9]*$`
- No special characters, spaces, or hyphens

**Action**: Reject if invalid, suggest corrected name.

---

### RULE: PCF_NAMING_002
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Namespace must be PascalCase and contain only alphanumeric characters.

**Rationale**: PowerApps solution publisher namespace requirement.

**Validation**:
- Namespace matches regex: `^[A-Z][A-Za-z0-9]*$`

**Action**: Reject if invalid.

---

### RULE: PCF_NAMING_003
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Property names must be camelCase.

**Rationale**: PCF manifest schema requirement.

**Validation**:
- Property name matches regex: `^[a-z][a-zA-Z0-9]*$`

**Action**: Auto-fix by converting to camelCase.

---

## Data Binding

### RULE: PCF_BINDING_001
**Category**: pcf-compliance  
**Severity**: error

**Condition**: At least one property must have `usage: "bound"` for field controls.

**Rationale**: Field controls must bind to a Dataverse field.

**Validation**:
- If `classification` is `input-control` or `display-control`
- Then at least one property has `usage: "bound"`

**Action**: Reject if no bound property exists.

---

### RULE: PCF_BINDING_002
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Bound property data type must be valid PCF type.

**Rationale**: Only specific Dataverse data types are supported.

**Validation**:
- Data type is one of: `SingleLine.Text`, `Multiple`, `Whole.None`, `Decimal`, `TwoOptions`, `DateAndTime.DateOnly`, `DateAndTime.DateAndTime`, `Currency`, `Lookup.Simple`, `OptionSet`, `MultiSelectOptionSet`

**Action**: Reject if invalid type, suggest closest valid type.

---

### RULE: PCF_BINDING_003
**Category**: pcf-compliance  
**Severity**: warning

**Condition**: Output properties should have meaningful names.

**Rationale**: Improves component usability in PowerApps.

**Validation**:
- Output property names are not generic (e.g., not "output1", "result")

**Action**: Warn if generic name detected.

---

## Manifest Requirements

### RULE: PCF_MANIFEST_001
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Component must have a display name.

**Rationale**: Required for PowerApps component gallery.

**Validation**:
- `displayName` field is present and non-empty
- Length between 1 and 100 characters

**Action**: Reject if missing or invalid.

---

### RULE: PCF_MANIFEST_002
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Component must have a description.

**Rationale**: Required for PowerApps component gallery.

**Validation**:
- `description` field is present and non-empty
- Length between 10 and 500 characters

**Action**: Reject if missing or too short.

---

## Resource Constraints

### RULE: PCF_RESOURCE_001
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Code resource must be specified.

**Rationale**: Every PCF component requires a TypeScript implementation.

**Validation**:
- `resources.code` is present and points to valid `.ts` file

**Action**: Reject if missing.

---

### RULE: PCF_RESOURCE_002
**Category**: pcf-compliance  
**Severity**: warning

**Condition**: CSS resources should be bundled, not external.

**Rationale**: Offline functionality and security requirements.

**Validation**:
- CSS paths do not reference external URLs
- No CDN links in CSS resources

**Action**: Warn if external CSS detected, suggest bundling.

---

### RULE: PCF_RESOURCE_003
**Category**: best-practice  
**Severity**: info

**Condition**: Localization resources recommended for production components.

**Rationale**: Supports multi-language PowerApps environments.

**Validation**:
- `resources.resx` contains at least one resource file

**Action**: Info note if missing.

---

## Lifecycle Methods

### RULE: PCF_LIFECYCLE_001
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Component must implement required lifecycle methods.

**Rationale**: PCF framework contract.

**Validation**:
- Implementation includes: `init()`, `updateView()`, `getOutputs()`, `destroy()`

**Action**: This is enforced at code generation stage, not spec validation.

---

## Security

### RULE: PCF_SECURITY_001
**Category**: security  
**Severity**: error

**Condition**: No external API calls in component code.

**Rationale**: PCF components run in sandboxed environment and must work offline.

**Validation**:
- No `fetch()`, `XMLHttpRequest`, or similar in generated code
- No external dependencies that make network calls

**Action**: Reject if external API dependency detected.

---

### RULE: PCF_SECURITY_002
**Category**: security  
**Severity**: error

**Condition**: No inline scripts or eval() usage.

**Rationale**: Content Security Policy compliance.

**Validation**:
- No `eval()`, `Function()` constructor, or inline event handlers

**Action**: Reject if detected.

---

## Compatibility

### RULE: PCF_COMPAT_001
**Category**: pcf-compliance  
**Severity**: error

**Condition**: Component must target supported PCF version.

**Rationale**: Ensures compatibility with PowerApps runtime.

**Validation**:
- `dependencies.pcfVersion` is `1.0.0` or higher

**Action**: Reject if unsupported version.

---

### RULE: PCF_COMPAT_002
**Category**: best-practice  
**Severity**: warning

**Condition**: External libraries should be minimized.

**Rationale**: Reduces bundle size and potential conflicts.

**Validation**:
- `dependencies.externalLibraries` array length ≤ 3

**Action**: Warn if more than 3 external libraries.

---

## Summary

**Total Rules**: 15  
**Error Severity**: 12  
**Warning Severity**: 2  
**Info Severity**: 1

These rules form the foundation of PCF compliance. Violation of any error-level rule must result in specification rejection.
