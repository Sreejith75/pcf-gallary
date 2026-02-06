# AI Brain Structure Reference

## Quick Reference

The AI Brain is a file-based knowledge system located in `ai-brain/` directory.

## Directory Structure

```
ai-brain/
├── schemas/                    # Data structure definitions
├── capabilities/               # Component capability definitions
├── intent/                     # Intent interpretation rules
├── procedures/                 # Workflow procedures
├── rules/                      # Validation rules
├── prompts/                    # LLM prompt templates
├── knowledge/                  # Factual PCF information
└── test-cases/                 # Validation test scenarios
```

## Schemas (`/schemas`)

| File | Purpose | Used By |
|------|---------|---------|
| `global-intent.schema.json` | Component-agnostic user intent | Stage 1: Intent Interpretation |
| `component-spec.schema.json` | Normalized component specification | Stage 3: Spec Generation |
| `capability-registry.schema.json` | Capability definition structure | Capability authoring |
| `validation-rules.schema.json` | Rule definition format | Rule authoring |

## Capabilities (`/capabilities`)

| File | Purpose |
|------|---------|
| `registry.index.json` | Master index of all capabilities |
| `star-rating.capability.json` | Star rating component definition |

### Capability Structure

```json
{
  "capabilityId": "unique-id",
  "componentType": "Human readable name",
  "supportedFeatures": [...],
  "limits": {...},
  "forbidden": [...]
}
```

## Intent Rules (`/intent`)

| File | Purpose |
|------|---------|
| `intent-mapping.rules.json` | Maps natural language to intent |
| `ambiguity-resolution.rules.json` | Resolves conflicts and ambiguity |

## Procedures (`/procedures`)

| File | Purpose |
|------|---------|
| `create-component.flow.md` | 5-stage component creation workflow |

### Workflow Stages

1. Intent Interpretation
2. Capability Matching
3. Specification Generation
4. Rules Validation
5. Final Validation

## Rules (`/rules`)

| File | Rules | Severity Breakdown |
|------|-------|-------------------|
| `pcf-core.rules.md` | 15 | 12 errors, 2 warnings, 1 info |
| `pcf-performance.rules.md` | 10 | 0 errors, 7 warnings, 3 info |
| `pcf-accessibility.rules.md` | 9 | 6 errors, 3 warnings, 0 info |
| **Total** | **34** | **18 errors, 12 warnings, 4 info** |

### Rule Format

```markdown
### RULE: RULE_ID
**Category**: category
**Severity**: error|warning|info
**Condition**: What to check
**Action**: What to do if violated
```

## Prompts (`/prompts`)

| File | Purpose | Output |
|------|---------|--------|
| `intent-interpreter.prompt.md` | Convert user input to intent | GlobalIntent JSON |
| `component-spec-generator.prompt.md` | Generate component spec | ComponentSpec JSON |

## Knowledge (`/knowledge`)

| File | Purpose |
|------|---------|
| `pcf-lifecycle.md` | PCF framework reference documentation |

## Test Cases (`/test-cases`)

| File | Count | Purpose |
|------|-------|---------|
| `happy-paths.json` | 5 | Successful scenarios |
| `edge-cases.json` | 10 | Boundary conditions |
| `rejection-cases.json` | 10 | Invalid inputs |

## Loading Strategy

### Stage 1: Intent Interpretation
```
Load:
- global-intent.schema.json
- intent-mapping.rules.json
- ambiguity-resolution.rules.json
- intent-interpreter.prompt.md
```

### Stage 2: Capability Matching
```
Load:
- registry.index.json
- {matched-capability}.capability.json
```

### Stage 3: Spec Generation
```
Load:
- component-spec.schema.json
- {matched-capability}.capability.json
- component-spec-generator.prompt.md
```

### Stage 4: Rules Validation
```
Load:
- pcf-core.rules.md
- pcf-performance.rules.md
- pcf-accessibility.rules.md
```

### Stage 5: Final Validation
```
Load:
- component-spec.schema.json
- {matched-capability}.capability.json
```

## Key Principles

1. **Indexed, Not Injected** - Never load entire brain
2. **Read-Only** - Brain files are never modified at runtime
3. **Versioned** - Brain updates are versioned in `version.json`
4. **Cacheable** - Files can be cached with TTL
5. **Deterministic** - Same files always produce same results

## File Access Patterns

### Direct Access
```typescript
const schema = await brainRouter.getSchema('global-intent');
const capability = await brainRouter.getCapability('star-rating');
const rules = await brainRouter.getRules('pcf-core');
```

### Stage-Based Access
```typescript
const artifacts = await brainRouter.loadForStage(WorkflowStage.INTENT_INTERPRETATION);
// Returns only files needed for that stage
```

## Extensibility

### Adding a New Capability
1. Create `new-component.capability.json`
2. Update `registry.index.json`
3. Add patterns to `intent-mapping.rules.json`
4. Add test cases

### Adding a New Rule
1. Add rule to appropriate `rules/*.md` file
2. Follow standard rule format
3. Add test case

### Updating a Schema
1. Increment version in `version.json`
2. Update schema file
3. Add migration guide if breaking change

## Version Control

Current version tracked in `version.json`:
```json
{
  "version": "1.0.0",
  "schemaVersion": "1.0",
  "released": "2026-02-06"
}
```
