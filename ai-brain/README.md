# AI Brain v1 - PCF Component Builder

## Overview

The AI Brain is the deterministic reasoning engine that powers safe, production-ready PCF component generation. It is **indexed, not injected** — the LLM never receives the entire brain in its prompt context.

## Architecture Principles

1. **Modular**: Each file has a single, well-defined responsibility
2. **Queryable**: The Brain Router loads files selectively based on context
3. **Deterministic**: Same input always produces same reasoning path
4. **Model-agnostic**: Works with any LLM that can follow structured prompts

## Core Components

### Schemas (`/schemas`)
Canonical data structures that define the "language of thought" for the AI system.

- `global-intent.schema.json` - Component-agnostic user intent classification
- `component-spec.schema.json` - Normalized component specification format
- `capability-registry.schema.json` - Structure for capability definitions
- `validation-rules.schema.json` - Rule definition format

### Capabilities (`/capabilities`)
Reality anchors that prevent feature hallucination.

- `registry.index.json` - Master index of all supported components
- `*.capability.json` - Per-component capability definitions with explicit limits

### Intent (`/intent`)
Rules for interpreting and disambiguating user requests.

- `intent-mapping.rules.json` - Maps user language to canonical intents
- `ambiguity-resolution.rules.json` - Handles unclear or conflicting requests

### Procedures (`/procedures`)
Step-by-step reasoning flows that guide the AI's decision-making process.

- `create-component.flow.md` - Main component generation workflow

### Rules (`/rules`)
Non-negotiable constraints that enforce PCF compliance and best practices.

- `pcf-core.rules.md` - Fundamental PCF requirements
- `pcf-performance.rules.md` - Performance and optimization rules
- `pcf-accessibility.rules.md` - WCAG and accessibility standards

### Prompts (`/prompts`)
Thin execution adapters that reference schemas and rules.

- `intent-interpreter.prompt.md` - Converts user input to structured intent
- `component-spec-generator.prompt.md` - Generates component specifications

### Knowledge (`/knowledge`)
Factual PCF information without reasoning logic.

- `pcf-lifecycle.md` - PCF component lifecycle documentation

### Test Cases (`/test-cases`)
Validation scenarios for brain behavior.

- `happy-paths.json` - Expected successful scenarios
- `edge-cases.json` - Boundary conditions and unusual inputs
- `rejection-cases.json` - Inputs that should be rejected

## Usage Pattern

```
User Input → Brain Router → Load Relevant Files → LLM Execution → Validated Output
```

The Brain Router determines which files to load based on:
- Current workflow stage
- Component type
- Validation requirements
- User intent classification

## Version Control

See `version.json` for current version and changelog.

## Evolution Strategy

- **Backward Compatible**: New capabilities are additive
- **Schema Versioning**: Breaking changes increment major version
- **Capability Registry**: New components added without breaking existing ones
- **Rule Layering**: New rules supplement, never replace, core rules
