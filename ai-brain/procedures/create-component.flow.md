# Component Creation Procedure

## Overview
This procedure defines the step-by-step reasoning flow for generating a PCF component from user input. Each step includes validation checkpoints and decision rules.

## Workflow Stages

### Stage 1: Intent Interpretation

**Objective**: Convert natural language user input into structured global intent.

**Steps**:
1. Parse user input for key terms and patterns
2. Load `intent-mapping.rules.json`
3. Match patterns against mapping rules
4. Apply modifiers to refine intent
5. Resolve ambiguities using `ambiguity-resolution.rules.json`
6. Validate against `global-intent.schema.json`

**Validation Checkpoint**:
- ✅ Intent conforms to global-intent schema
- ✅ All required fields populated
- ✅ No conflicting properties

**Downgrade Rules**:
- If multiple capabilities match → Use `prefer-simpler` strategy
- If vague description → Apply `defaultAssumptions`
- If conflicting requirements → Reject with clarification request

**Reject Rules**:
- No matching classification found
- Contradictory requirements (e.g., both read-only and editable)
- Explicitly forbidden behavior requested

---

### Stage 2: Capability Matching

**Objective**: Find the appropriate capability definition that matches the interpreted intent.

**Steps**:
1. Load `registry.index.json`
2. Query by `classification` and `primaryPurpose`
3. Retrieve matching capability file(s)
4. Score each capability against intent
5. Select highest-scoring capability
6. Verify all requested features are supported

**Validation Checkpoint**:
- ✅ At least one capability matches
- ✅ Capability supports required features
- ✅ No forbidden behaviors requested

**Downgrade Rules**:
- If requested feature not supported → Remove feature and document in downgrades
- If parameter exceeds limits → Clamp to maximum allowed value
- If external dependency forbidden → Use bundled alternative

**Reject Rules**:
- No capability matches the intent
- Core required feature is forbidden
- Capability marked as deprecated

---

### Stage 3: Specification Generation

**Objective**: Create a complete component specification conforming to component-spec schema.

**Steps**:
1. Generate unique `componentId` (kebab-case)
2. Generate `componentName` (PascalCase)
3. Set `namespace` from configuration
4. Map capability features to component properties
5. Define required PCF properties based on data binding
6. Configure resources (code, css, resx)
7. Apply customizations within capability bounds
8. Validate against `component-spec.schema.json`

**Validation Checkpoint**:
- ✅ Spec conforms to component-spec schema
- ✅ All properties have valid PCF data types
- ✅ Resource paths are valid
- ✅ Customizations within capability limits

**Downgrade Rules**:
- If property name invalid → Auto-fix to camelCase
- If data type unsupported → Use closest supported type
- If resource missing → Use default template

**Reject Rules**:
- Cannot generate valid component name
- Required property has no valid PCF data type mapping
- Namespace is invalid

---

### Stage 4: Rules Validation

**Objective**: Ensure the specification complies with all PCF rules and best practices.

**Steps**:
1. Load applicable rule files from `/rules`:
   - `pcf-core.rules.md` (always)
   - `pcf-performance.rules.md` (if performanceTarget ≠ lightweight)
   - `pcf-accessibility.rules.md` (always)
2. For each rule, evaluate condition against spec
3. Execute action based on severity:
   - **error** → Reject specification
   - **warning** → Downgrade feature or add to warnings
   - **info** → Add to validation notes
4. Document all applied rules in `validation.rulesApplied`
5. Document all downgrades in `validation.downgrades`

**Validation Checkpoint**:
- ✅ No error-level rule violations
- ✅ All warnings documented
- ✅ Downgrades have alternatives specified

**Downgrade Rules**:
- If performance budget exceeded → Simplify feature or reduce limits
- If accessibility requirement not met → Add required ARIA attributes
- If best practice violated → Apply auto-fix if available

**Reject Rules**:
- Core PCF compliance rule violated (error severity)
- Security vulnerability detected
- Accessibility error that cannot be auto-fixed

---

### Stage 5: Final Validation

**Objective**: Perform final checks before approving specification for code generation.

**Steps**:
1. Validate complete spec against `component-spec.schema.json`
2. Cross-reference with capability definition for consistency
3. Verify all downgrades have been applied
4. Ensure no forbidden behaviors present
5. Check that spec is deterministic (same input → same output)

**Validation Checkpoint**:
- ✅ Spec is valid and complete
- ✅ Spec matches capability constraints
- ✅ All validation metadata populated
- ✅ Ready for code generation

**Output**:
- Valid `component-spec.json` file
- Validation report with warnings and downgrades
- List of applied rules

---

## Decision Matrix

| Scenario | Action | Stage |
|----------|--------|-------|
| User input too vague | Apply defaults, proceed | Stage 1 |
| Multiple capabilities match | Use prefer-simpler strategy | Stage 2 |
| Feature not supported | Remove feature, document downgrade | Stage 2 |
| Parameter exceeds limit | Clamp to max, document downgrade | Stage 3 |
| Core rule violation | Reject with explanation | Stage 4 |
| Accessibility auto-fixable | Apply fix, document warning | Stage 4 |
| No matching capability | Reject with alternatives | Stage 2 |
| Forbidden behavior requested | Reject with alternative | Stage 2 |

## Error Handling

**Principle**: Always provide actionable feedback.

**Error Response Format**:
```json
{
  "status": "rejected",
  "stage": "capability-matching",
  "reason": "No capability supports the requested feature combination",
  "userMessage": "Cannot create a rating control with video playback. Rating controls support star-based feedback only.",
  "suggestions": [
    "Use a star-rating control for feedback collection",
    "Use a media-control for video playback"
  ],
  "availableCapabilities": ["star-rating", "slider-rating"]
}
```

## Success Criteria

A component specification is approved when:
1. ✅ Conforms to all schemas
2. ✅ Matches a valid capability
3. ✅ Passes all error-level rules
4. ✅ Has no forbidden behaviors
5. ✅ Is deterministic and reproducible
