# PCF Accessibility Rules

## Overview
WCAG compliance and accessibility standards for PCF components. Default target is WCAG 2.1 Level AA.

---

## Keyboard Navigation

### RULE: PCF_A11Y_001
**Category**: accessibility  
**Severity**: error

**Condition**: All interactive elements must be keyboard accessible.

**Rationale**: WCAG 2.1.1 (Level A) - Keyboard accessibility is fundamental.

**Validation**:
- Interactive controls have `tabindex` attribute
- Focus management implemented
- No keyboard traps

**Action**: Reject if keyboard navigation not possible. Code generation ensures proper tabindex.

---

### RULE: PCF_A11Y_002
**Category**: accessibility  
**Severity**: error

**Condition**: Focus indicators must be visible.

**Rationale**: WCAG 2.4.7 (Level AA) - Users must see what has focus.

**Validation**:
- CSS includes `:focus` styles
- Focus outline not removed without replacement

**Action**: Code generation includes visible focus styles.

---

## Screen Reader Support

### RULE: PCF_A11Y_003
**Category**: accessibility  
**Severity**: error

**Condition**: All controls must have accessible labels.

**Rationale**: WCAG 4.1.2 (Level A) - Screen readers need text alternatives.

**Validation**:
- `aria-label` or `aria-labelledby` present
- Form controls have associated labels

**Action**: Reject if no accessible name. Code generation adds ARIA labels.

---

### RULE: PCF_A11Y_004
**Category**: accessibility  
**Severity**: error

**Condition**: Dynamic content changes must be announced.

**Rationale**: WCAG 4.1.3 (Level AA) - Screen readers need status updates.

**Validation**:
- Use `aria-live` regions for dynamic updates
- Status messages have appropriate roles

**Action**: Code generation includes ARIA live regions where needed.

---

## Color and Contrast

### RULE: PCF_A11Y_005
**Category**: accessibility  
**Severity**: warning

**Condition**: Text contrast ratio must meet WCAG AA standards.

**Rationale**: WCAG 1.4.3 (Level AA) - Minimum contrast 4.5:1 for normal text.

**Validation**:
- Default colors meet contrast requirements
- User-customizable colors validated

**Action**: Warn if custom colors may not meet contrast ratio.

---

### RULE: PCF_A11Y_006
**Category**: accessibility  
**Severity**: error

**Condition**: Information must not rely on color alone.

**Rationale**: WCAG 1.4.1 (Level A) - Color-blind users need alternatives.

**Validation**:
- Use icons, text, or patterns in addition to color
- State changes indicated by multiple visual cues

**Action**: Code generation uses multiple indicators for state.

---

## ARIA Attributes

### RULE: PCF_A11Y_007
**Category**: accessibility  
**Severity**: error

**Condition**: ARIA roles must be semantically correct.

**Rationale**: WCAG 4.1.2 (Level A) - Incorrect roles confuse assistive technology.

**Validation**:
- Roles match component purpose
- No conflicting ARIA attributes

**Action**: Code generation uses appropriate semantic roles.

---

### RULE: PCF_A11Y_008
**Category**: accessibility  
**Severity**: warning

**Condition**: ARIA states must be updated dynamically.

**Rationale**: Screen readers rely on current state information.

**Validation**:
- `aria-checked`, `aria-selected`, `aria-expanded` updated on change
- State reflects actual component state

**Action**: Code generation includes state synchronization.

---

## High Contrast Mode

### RULE: PCF_A11Y_009
**Category**: accessibility  
**Severity**: warning

**Condition**: Component must be usable in high contrast mode.

**Rationale**: Windows High Contrast Mode users need visible controls.

**Validation**:
- CSS includes high contrast media queries
- Borders and outlines visible in high contrast

**Action**: Code generation includes high contrast CSS.

---

## Summary

**Total Rules**: 9  
**Error Severity**: 6  
**Warning Severity**: 3

Accessibility is non-negotiable. Error-level violations must be fixed before component approval.
