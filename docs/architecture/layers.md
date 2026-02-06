# Layer Responsibilities

## Overview

The PCF Component Builder is organized into 5 logical layers, each with distinct responsibilities and clear boundaries.

---

## Layer 1: Presentation Layer

**Purpose**: User interaction and result delivery

### Responsibilities
- Accept user input (natural language component description)
- Display component preview (optional)
- Provide ZIP download
- Show validation errors and warnings
- Display build progress

### Components
- Web UI (React/Next.js)
- REST API endpoints
- WebSocket for real-time updates (optional)

### Inputs
- User prompt (string)
- Builder configuration

### Outputs
- ZIP file (component package)
- Build result (success/error)
- Validation report

### Constraints
- **No business logic** - Pure UI/API layer
- **No validation** - Delegates to orchestrator
- **No AI calls** - Delegates to orchestrator

---

## Layer 2: Orchestration Layer

**Purpose**: Workflow coordination and brain routing

### Responsibilities
- Execute 7-stage pipeline
- Coordinate service calls
- Manage build state
- Persist intermediate artifacts
- Handle errors and retries
- Maintain audit trail

### Components
- **Component Builder Orchestrator**: Main workflow coordinator
- **Brain Router**: Selective AI Brain file loader

### Inputs
- User prompt + configuration
- Resume request (optional)

### Outputs
- Build result with spec or error
- Persisted artifacts
- Audit log

### Constraints
- **Stateless service** - State persisted to storage
- **No business logic** - Defined in AI Brain
- **No code generation** - Delegates to Code Generator

**See**: [AI Orchestrator Specification](orchestrator.md) for detailed design

---

## Layer 3: Reasoning Layer

**Purpose**: Intent interpretation and validation

### Responsibilities
- Load AI Brain artifacts selectively
- Execute LLM prompts
- Validate JSON against schemas
- Execute validation rules
- Apply auto-fixes and downgrades

### Components
- **AI Brain** (Read-only file system)
- **LLM Service Adapter**
- **Validator Engine**

### AI Brain Structure
```
ai-brain/
├── schemas/          # JSON schemas
├── capabilities/     # Component definitions
├── intent/          # Intent mapping rules
├── procedures/      # Workflow definitions
├── rules/           # Validation rules
├── prompts/         # LLM prompt templates
└── knowledge/       # PCF reference docs
```

### Inputs
- User prompt (for LLM)
- JSON data (for validation)
- Workflow stage (for brain loading)

### Outputs
- GlobalIntent JSON
- ComponentSpec JSON
- Validation results
- Rule execution results

### Constraints
- **AI Brain is read-only** - Never modified at runtime
- **Indexed loading** - Never load entire brain
- **No code generation** - Only specifications

---

## Layer 4: Generation Layer

**Purpose**: Code synthesis from specifications

### Responsibilities
- Load code templates
- Render templates with spec data
- Generate TypeScript implementation
- Generate PCF manifest XML
- Generate CSS styles
- Generate localization resources
- Lint generated code

### Components
- **Code Generator**: Template-based code synthesis

### Templates
```
templates/
├── index.ts.template              # TypeScript implementation
├── ControlManifest.Input.xml.template  # PCF manifest
├── styles.css.template            # Component styles
└── strings.resx.template          # Localization
```

### Inputs
- Approved ComponentSpec JSON
- Capability definition

### Outputs
- Generated source files:
  - `index.ts` (TypeScript)
  - `ControlManifest.Input.xml` (Manifest)
  - `styles.css` (Styles)
  - `strings.resx` (Localization)

### Constraints
- **Template-based only** - No AI involvement
- **Deterministic** - Same spec → Same code
- **Validated output** - Linting enforced

---

## Layer 5: Packaging Layer

**Purpose**: Bundle assembly and delivery

### Responsibilities
- Create PCF folder structure
- Generate package.json
- Generate pcfconfig.json
- Generate README.md
- Bundle all resources
- Create ZIP file
- Validate package structure

### Components
- **PCF Packager**: Bundle assembler

### Package Structure
```
component-name/
├── package.json
├── pcfconfig.json
├── ControlManifest.Input.xml
├── index.ts
├── styles.css
├── strings.resx
└── README.md
```

### Inputs
- Generated source files
- Component specification

### Outputs
- ZIP buffer (deployable package)
- Package manifest (metadata)

### Constraints
- **Standard PCF structure only** - No custom layouts
- **Deterministic** - Same files → Same ZIP
- **Validated** - Structure validation enforced

---

## Layer Interaction Rules

### Communication Flow

```
Presentation → Orchestration → Reasoning → Generation → Packaging
     ↓              ↓              ↓            ↓           ↓
  User Input    Workflow      AI Brain      Templates    ZIP File
```

### Dependency Rules

1. **Presentation** depends on **Orchestration** only
2. **Orchestration** depends on **Reasoning**, **Generation**, **Packaging**
3. **Reasoning** has no dependencies (reads AI Brain)
4. **Generation** depends on **Reasoning** (for specs)
5. **Packaging** depends on **Generation** (for files)

### Data Flow Rules

1. **Downward only** - Upper layers call lower layers, never reverse
2. **No layer skipping** - Must go through orchestration
3. **Stateless services** - State persisted externally
4. **Immutable artifacts** - Once persisted, never modified

---

## Separation of Concerns

| Layer | Business Logic | AI Calls | Validation | Code Gen | Packaging |
|-------|---------------|----------|------------|----------|-----------|
| Presentation | ❌ | ❌ | ❌ | ❌ | ❌ |
| Orchestration | ❌ (in Brain) | ❌ (delegates) | ❌ (delegates) | ❌ | ❌ |
| Reasoning | ✅ (in Brain) | ✅ | ✅ | ❌ | ❌ |
| Generation | ❌ | ❌ | ✅ (linting) | ✅ | ❌ |
| Packaging | ❌ | ❌ | ✅ (structure) | ❌ | ✅ |

---

## Scalability Considerations

### Horizontal Scaling

- **Presentation**: Multiple UI/API instances behind load balancer
- **Orchestration**: Stateless, scales horizontally
- **Reasoning**: Brain Router caches artifacts in Redis
- **Generation**: Stateless, scales horizontally
- **Packaging**: Stateless, scales horizontally

### Vertical Scaling

- **AI Brain**: Read-only, can be CDN-distributed
- **LLM Calls**: Rate-limited, pooled connections
- **Validation**: CPU-intensive, benefits from more cores

---

## Summary

Each layer has a **single, well-defined responsibility**:

1. **Presentation**: User interaction
2. **Orchestration**: Workflow coordination
3. **Reasoning**: AI-powered interpretation and validation
4. **Generation**: Deterministic code synthesis
5. **Packaging**: Bundle assembly

This separation ensures:
- ✅ Clear boundaries
- ✅ Independent testing
- ✅ Easy maintenance
- ✅ Horizontal scalability
- ✅ Extensibility without breaking changes
