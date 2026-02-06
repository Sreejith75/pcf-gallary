# Glossary

## A

**AI Brain**  
File-based knowledge system containing schemas, capabilities, rules, procedures, and prompts. The brain is indexed (selectively loaded) rather than injected (fully loaded) into LLM context.

**Ambiguity Resolution**  
Process of resolving conflicts or unclear requirements in user input using predefined resolution strategies.

**Audit Trail**  
Complete log of all decisions and actions taken during component build process.

**Auto-Fix**  
Automatic correction applied by validator when a warning-level rule is violated.

## B

**Brain Router**  
Service responsible for selectively loading AI Brain files based on workflow stage and context.

**Build Context**  
State object containing all information needed for current workflow stage.

**Build Result**  
Final output of component build process, including spec, ZIP file, errors, warnings, and metadata.

## C

**Capability**  
Definition of a specific PCF component type with explicit features, limits, and forbidden behaviors.

**Capability Matching**  
Stage 2 of workflow where user intent is matched to a specific component capability.

**Component Specification (ComponentSpec)**  
Normalized JSON structure defining all aspects of a PCF component.

**Cross-Reference Validation**  
Final validation stage ensuring spec matches capability constraints.

## D

**Deterministic Pipeline**  
Workflow that produces the same output for the same input every time.

**Downgrade**  
Automatic adjustment of component feature when requested value exceeds capability limits.

## E

**Enum-Based Schema**  
JSON schema using enumerated values instead of free text to prevent hallucination.

**Extensibility Point**  
Mechanism for adding new capabilities, rules, or features without code changes.

## F

**Forbidden Behavior**  
Explicitly prohibited action or feature with documented alternative.

**Final Validation**  
Stage 5 of workflow performing cross-reference checks before code generation.

## G

**Global Intent**  
Component-agnostic representation of user's desired component using standardized enums.

## H

**Hallucination**  
LLM generating features or capabilities not defined in AI Brain. System prevents this through capability constraints.

**Horizontal Scaling**  
Adding more orchestrator instances to handle increased load.

## I

**Indexed Loading**  
Strategy of loading only required brain files instead of entire brain.

**Intent Interpretation**  
Stage 1 of workflow converting natural language to structured GlobalIntent.

**Intent Mapping**  
Rules translating natural language patterns to canonical intent classifications.

## L

**LLM Adapter**  
Service interfacing between orchestrator and language model APIs.

**Limits**  
Explicit boundaries defined in capability (e.g., maxStars: 10).

## O

**Orchestrator**  
Central service coordinating the 5-stage workflow and managing state transitions.

## P

**PCF (PowerApps Component Framework)**  
Microsoft framework for building custom controls for PowerApps.

**Prompt Template**  
Thin execution adapter for LLM, referencing schemas and rules without containing business logic.

**Property**  
Component input/output field with specific data type and usage.

## R

**Rejection**  
Build failure due to error-level rule violation or invalid input.

**Rule**  
Validation constraint with condition, severity, and action.

**Rule Execution**  
Process of evaluating all rules against component spec.

**Rules Validation**  
Stage 4 of workflow executing 34 validation rules.

## S

**Schema Validation**  
Verification that JSON conforms to JSON Schema Draft-07 specification.

**Specification Generation**  
Stage 3 of workflow creating ComponentSpec from intent and capability.

**Stage**  
One of 7 steps in component build workflow.

**Stateless Service**  
Service with no persistent state, enabling horizontal scaling.

## T

**Template Engine**  
Service rendering code templates with component specification data.

**Thin Prompt**  
Prompt containing only execution logic, referencing brain files for business rules.

**Token Usage**  
Number of tokens consumed by LLM API call.

## V

**Validation Report**  
Document summarizing all validation results, errors, warnings, and downgrades.

**Validator Engine**  
Service executing schema and rule validation.

## W

**Workflow**  
5-stage process: Intent Interpretation → Capability Matching → Spec Generation → Rules Validation → Final Validation

## Z

**Zero Hallucination Tolerance**  
Design principle ensuring LLM cannot invent features not defined in AI Brain.

---

## Acronyms

| Acronym | Full Form |
|---------|-----------|
| A11Y | Accessibility |
| API | Application Programming Interface |
| CDN | Content Delivery Network |
| CI/CD | Continuous Integration / Continuous Deployment |
| CSS | Cascading Style Sheets |
| HA | High Availability |
| JSON | JavaScript Object Notation |
| LLM | Large Language Model |
| NLP | Natural Language Processing |
| PCF | PowerApps Component Framework |
| RESX | Resource File (XML) |
| TTL | Time To Live |
| WCAG | Web Content Accessibility Guidelines |
| XML | Extensible Markup Language |
| ZIP | Compressed Archive Format |

---

## Rule Severity Levels

| Severity | Meaning | Action |
|----------|---------|--------|
| **error** | Non-negotiable violation | Reject build |
| **warning** | Best practice violation | Apply auto-fix or downgrade |
| **info** | Informational note | Document only |

---

## Workflow Stages

| Stage | Number | Purpose |
|-------|--------|---------|
| Intent Interpretation | 1 | Convert user input to GlobalIntent |
| Capability Matching | 2 | Match intent to component capability |
| Specification Generation | 3 | Create ComponentSpec |
| Rules Validation | 4 | Execute 34 validation rules |
| Final Validation | 5 | Cross-reference spec with capability |
| Code Generation | 6 | Generate PCF source files |
| Packaging | 7 | Create ZIP bundle |

---

## PCF Data Types

| Type | Description |
|------|-------------|
| SingleLine.Text | Single line text input |
| Multiple | Multi-line text |
| Whole.None | Integer number |
| Decimal | Decimal number |
| TwoOptions | Boolean (Yes/No) |
| DateAndTime.DateOnly | Date without time |
| DateAndTime.DateAndTime | Date with time |
| Currency | Currency value |
| Lookup.Simple | Reference to another record |
| OptionSet | Single-select dropdown |
| MultiSelectOptionSet | Multi-select dropdown |
