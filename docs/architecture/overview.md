# System Architecture Overview

## Executive Summary

The PCF Component Builder is a deterministic, validation-first system that transforms natural language descriptions into production-ready PowerApps Component Framework (PCF) components using an AI Brain knowledge system.

## Core Principle

**The AI Brain is indexed, not injected.**

The LLM never receives the entire brain. Instead, a Brain Router selectively loads only the necessary files for each workflow stage, ensuring:
- Reduced token usage
- Faster execution
- Deterministic behavior
- Clear audit trails

## Architecture at a Glance

The system consists of **5 logical layers** and **6 key services**:

### Layers
1. **Presentation Layer** - User interaction (UI/API)
2. **Orchestration Layer** - Workflow coordination
3. **Reasoning Layer** - AI Brain + LLM + Validation
4. **Generation Layer** - Code synthesis
5. **Packaging Layer** - Bundle assembly

### Services
1. **Orchestrator** - Workflow coordinator
2. **Brain Router** - Selective file loader
3. **LLM Adapter** - Language model interface
4. **Validator** - Compliance engine
5. **Code Generator** - Template engine
6. **Packager** - Bundle assembler

## Design Principles

### 1. Deterministic Pipelines
Same input → Same output, always. No randomness in workflow execution.

### 2. Typed Schemas
All data structures pre-defined in JSON Schema. No free-form text in critical fields.

### 3. Validation-First Design
34 validation rules enforced before code generation. Errors reject, warnings downgrade.

### 4. Zero Hallucination Tolerance
Capability constraints prevent LLM from inventing features. Explicit limits and forbidden behaviors.

### 5. Clear Separation of Concerns
Each service has a single responsibility. No cross-cutting concerns.

### 6. Extensibility Without Code Changes
New capabilities, rules, and LLM models added via configuration, not code.

## Workflow Stages

The system executes a **5-stage workflow** defined in the AI Brain:

1. **Intent Interpretation** - Natural language → Structured intent
2. **Capability Matching** - Intent → Component capability
3. **Specification Generation** - Capability → Component spec
4. **Rules Validation** - Spec → Validated spec (34 rules)
5. **Final Validation** - Cross-reference and approval

After validation, deterministic code generation and packaging occur.

## Data Flow

```
User Prompt
    ↓
Orchestrator (Stage 1: Intent Interpretation)
    ↓
Brain Router loads: global-intent.schema.json, intent-mapping.rules.json
    ↓
LLM Adapter executes prompt → GlobalIntent JSON
    ↓
Validator validates against schema
    ↓
Orchestrator (Stage 2: Capability Matching)
    ↓
Brain Router loads: registry.index.json, capability file
    ↓
Orchestrator matches capability
    ↓
Orchestrator (Stage 3: Spec Generation)
    ↓
Brain Router loads: component-spec.schema.json, capability
    ↓
LLM Adapter executes prompt → ComponentSpec JSON
    ↓
Validator validates against schema
    ↓
Orchestrator (Stage 4: Rules Validation)
    ↓
Brain Router loads: all rules/*.md files
    ↓
Validator executes 34 rules → Validated spec
    ↓
Orchestrator (Stage 5: Final Validation)
    ↓
Validator cross-references spec with capability
    ↓
Code Generator applies templates → Source files
    ↓
Packager creates ZIP → Output
```

## Technology Stack

- **Runtime**: Node.js 20+ / TypeScript 5+
- **Validation**: Ajv (JSON Schema)
- **Templates**: Handlebars
- **LLM**: OpenAI / Anthropic / Azure
- **Packaging**: JSZip

## Safety Mechanisms

1. **Capability Constraints** - Explicit feature lists
2. **Forbidden Behaviors** - Explicit prohibitions with alternatives
3. **Schema Validation** - All JSON validated
4. **Rule Enforcement** - 34 rules at Stage 4
5. **Audit Logging** - Complete decision trail

## Performance Targets

- Intent interpretation: < 2s
- Capability matching: < 100ms
- Spec generation: < 3s
- Rules validation: < 500ms
- Code generation: < 1s
- Packaging: < 500ms
- **Total**: < 7s end-to-end

## Next Steps

- Review [Layer Responsibilities](layers.md)
- Explore [Service Specifications](services.md)
- Study [System Architecture Diagram](diagrams/system-architecture.md)
- Understand [Data Flow Diagram](diagrams/data-flow.md)
