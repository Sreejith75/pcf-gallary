# Architecture Diagrams - Orchestrator & Pipeline

This document contains detailed Mermaid diagrams for the AI Orchestrator and Code Generation Pipeline.

---

## 1. Orchestrator State Machine

```mermaid
stateDiagram-v2
    [*] --> Idle
    
    Idle --> Initializing: buildComponent()
    Initializing --> Stage1_Intent: Init Success
    Initializing --> Failed: Init Failed
    
    Stage1_Intent --> Stage2_Capability: Intent Valid
    Stage1_Intent --> Retrying_Stage1: LLM Error
    Stage1_Intent --> Failed: Permanent Error
    Retrying_Stage1 --> Stage1_Intent: Retry (max 3)
    Retrying_Stage1 --> Failed: Max Retries
    
    Stage2_Capability --> Stage3_Spec: Capability Matched
    Stage2_Capability --> Failed: No Match
    
    Stage3_Spec --> Stage4_Rules: Spec Valid
    Stage3_Spec --> Retrying_Stage3: LLM Error
    Stage3_Spec --> Failed: Permanent Error
    Retrying_Stage3 --> Stage3_Spec: Retry (max 2)
    Retrying_Stage3 --> Failed: Max Retries
    
    Stage4_Rules --> Stage5_Final: Rules Pass
    Stage4_Rules --> Failed: Rule Violation
    
    Stage5_Final --> Stage6_CodeGen: Approved
    Stage5_Final --> Failed: Cross-Ref Failed
    
    Stage6_CodeGen --> Stage7_Package: Code Generated
    Stage6_CodeGen --> Failed: Generation Failed
    
    Stage7_Package --> Completed: ZIP Created
    Stage7_Package --> Failed: Packaging Failed
    
    Completed --> Idle: Reset
    Failed --> Idle: Reset
    
    note right of Retrying_Stage1
        Exponential backoff
        1s, 2s, 4s
    end note
    
    note right of Stage4_Rules
        34 validation rules
        Auto-fix warnings
    end note
```

---

## 2. Brain Router Task Flow

```mermaid
flowchart TD
    START[Routing Request] --> TASK{BrainTask Type}
    
    TASK -->|INTERPRET_INTENT| INTENT[Load Intent Files]
    TASK -->|MATCH_CAPABILITY| CAP[Load Capability Files]
    TASK -->|GENERATE_SPEC| SPEC[Load Spec Files]
    TASK -->|VALIDATE_RULES| RULES[Load Rule Files]
    TASK -->|VALIDATE_FINAL| FINAL[Load Validation Files]
    
    INTENT --> INTENT_FILES["• global-intent.schema.json<br/>• intent-mapping.rules.json<br/>• ambiguity-resolution.rules.json<br/>• intent-interpreter.prompt.md"]
    CAP --> CAP_FILES["• registry.index.json<br/>• {capabilityId}.capability.json"]
    SPEC --> SPEC_FILES["• component-spec.schema.json<br/>• {capabilityId}.capability.json<br/>• component-spec-generator.prompt.md"]
    RULES --> RULE_FILES["• pcf-core.rules.md<br/>• pcf-performance.rules.md<br/>• pcf-accessibility.rules.md"]
    FINAL --> FINAL_FILES["• component-spec.schema.json<br/>• {capabilityId}.capability.json"]
    
    INTENT_FILES --> BUDGET[Calculate Budget]
    CAP_FILES --> BUDGET
    SPEC_FILES --> BUDGET
    RULE_FILES --> BUDGET
    FINAL_FILES --> BUDGET
    
    BUDGET --> CHECK{Within<br/>5000 tokens?}
    CHECK -->|Yes| CACHE[Check Cache]
    CHECK -->|No| ERROR[Budget Exceeded Error]
    
    CACHE --> LOAD[Load Files]
    LOAD --> LOG[Log Routing Decision]
    LOG --> RETURN[Return Brain Artifacts]
    
    style START fill:#e1f5ff
    style RETURN fill:#c8e6c9
    style ERROR fill:#ffcdd2
```

---

## 3. LLM Call Flow with Retry

```mermaid
sequenceDiagram
    participant O as Orchestrator
    participant BR as Brain Router
    participant LLM as LLM Adapter
    participant V as Validator
    
    O->>BR: route(INTERPRET_INTENT)
    BR->>BR: Load 4 files (3900 tokens)
    BR-->>O: Brain Artifacts
    
    O->>LLM: execute(INTERPRET_INTENT, artifacts)
    LLM->>LLM: Assemble prompt
    LLM->>LLM: Call OpenAI API
    
    alt Success
        LLM-->>O: GlobalIntent JSON
        O->>V: validateSchema(GlobalIntent)
        V-->>O: Valid ✓
    else LLM Timeout
        LLM-->>O: Error: Timeout
        O->>O: Wait 1s (backoff)
        O->>LLM: Retry (attempt 2)
        LLM->>LLM: Call OpenAI API
        LLM-->>O: GlobalIntent JSON
        O->>V: validateSchema(GlobalIntent)
        V-->>O: Valid ✓
    else Schema Violation
        LLM-->>O: Invalid JSON
        O->>V: validateSchema(response)
        V-->>O: Invalid ✗
        O->>O: Wait 1s
        O->>LLM: Retry (attempt 2)
        LLM-->>O: GlobalIntent JSON
        O->>V: validateSchema(GlobalIntent)
        V-->>O: Valid ✓
    else Max Retries
        LLM-->>O: Error: Timeout
        O->>O: Wait 2s (backoff)
        O->>LLM: Retry (attempt 3)
        LLM-->>O: Error: Timeout
        O->>O: Max retries exceeded
        O-->>O: Reject Build
    end
```

---

## 4. Code Generation Pipeline Flow

```mermaid
flowchart TD
    START[Approved ComponentSpec] --> GEN1[Generate ControlManifest.Input.xml]
    
    GEN1 --> VAL1{Validate<br/>XML Schema}
    VAL1 -->|Pass| GEN2[Generate package.json]
    VAL1 -->|Fail| FIX1{Auto-fixable?}
    FIX1 -->|Yes| AUTO1[Template Retry]
    FIX1 -->|No| LLM1[LLM Fix]
    AUTO1 --> VAL1
    LLM1 --> VAL1
    
    GEN2 --> VAL2{Validate<br/>JSON Schema}
    VAL2 -->|Pass| GEN3[Generate tsconfig.json]
    VAL2 -->|Fail| FIX2[Fix & Retry]
    FIX2 --> VAL2
    
    GEN3 --> VAL3{Validate<br/>JSON Schema}
    VAL3 -->|Pass| GEN4[Generate index.ts]
    VAL3 -->|Fail| FIX3[Fix & Retry]
    FIX3 --> VAL3
    
    GEN4 --> VAL4{TypeScript<br/>Compile + Lint}
    VAL4 -->|Pass| GEN5[Generate styles.css]
    VAL4 -->|Fail| FIX4{Linting<br/>Error?}
    FIX4 -->|Yes| AUTO4[ESLint --fix]
    FIX4 -->|No| LLM4[LLM Fix Code]
    AUTO4 --> VAL4
    LLM4 --> VAL4
    
    GEN5 --> VAL5{CSS<br/>Lint}
    VAL5 -->|Pass| GEN6[Generate strings.resx]
    VAL5 -->|Fail| AUTO5[stylelint --fix]
    AUTO5 --> VAL5
    
    GEN6 --> VAL6{RESX<br/>Schema}
    VAL6 -->|Pass| GEN7[Generate README.md]
    VAL6 -->|Fail| FIX6[Fix & Retry]
    FIX6 --> VAL6
    
    GEN7 --> GEN8[Generate .gitignore]
    GEN8 --> BUILD[Build Verification]
    
    BUILD --> NPM[npm install]
    NPM --> TSC[npx tsc]
    TSC --> PCF_BUILD[pac pcf build]
    PCF_BUILD --> PCF_PUSH[pac pcf push --dry-run]
    
    PCF_PUSH --> PKG{Build<br/>Success?}
    PKG -->|Yes| ZIP[Create ZIP Package]
    PKG -->|No| REJECT[Reject Build]
    
    ZIP --> VALIDATE_PKG{Package<br/>Valid?}
    VALIDATE_PKG -->|Yes| COMPLETE[Deployable ZIP]
    VALIDATE_PKG -->|No| REJECT
    
    style START fill:#e1f5ff
    style COMPLETE fill:#c8e6c9
    style REJECT fill:#ffcdd2
```

---

## 5. Validation Checkpoints

```mermaid
graph LR
    subgraph Stage 1: Intent
        I1[User Prompt] --> I2[LLM Call]
        I2 --> I3{Schema<br/>Valid?}
        I3 -->|Yes| I4[GlobalIntent ✓]
        I3 -->|No| I5[Retry/Reject]
    end
    
    subgraph Stage 2: Capability
        C1[GlobalIntent] --> C2[Registry Query]
        C2 --> C3{Capability<br/>Exists?}
        C3 -->|Yes| C4[Capability ✓]
        C3 -->|No| C5[Reject]
    end
    
    subgraph Stage 3: Spec
        S1[Intent + Capability] --> S2[LLM Call]
        S2 --> S3{Schema<br/>Valid?}
        S3 -->|Yes| S4[ComponentSpec ✓]
        S3 -->|No| S5[Retry/Reject]
    end
    
    subgraph Stage 4: Rules
        R1[ComponentSpec] --> R2[Execute 34 Rules]
        R2 --> R3{All Rules<br/>Pass?}
        R3 -->|Yes| R4[Validated Spec ✓]
        R3 -->|Warnings| R6[Auto-fix]
        R3 -->|Errors| R5[Reject]
        R6 --> R4
    end
    
    subgraph Stage 5: Final
        F1[Validated Spec] --> F2[Cross-Reference]
        F2 --> F3{Capability<br/>Bounds?}
        F3 -->|Yes| F4[Approved Spec ✓]
        F3 -->|No| F5[Reject]
    end
    
    I4 --> C1
    C4 --> S1
    S4 --> R1
    R4 --> F1
    
    style I4 fill:#c8e6c9
    style C4 fill:#c8e6c9
    style S4 fill:#c8e6c9
    style R4 fill:#c8e6c9
    style F4 fill:#c8e6c9
    style I5 fill:#ffcdd2
    style C5 fill:#ffcdd2
    style S5 fill:#ffcdd2
    style R5 fill:#ffcdd2
    style F5 fill:#ffcdd2
```

---

## 6. Error-Fix Loop Detail

```mermaid
stateDiagram-v2
    [*] --> Generate: Start File Generation
    
    Generate --> Validate: File Generated
    
    Validate --> Success: Valid ✓
    Validate --> CheckFixable: Invalid ✗
    
    CheckFixable --> AutoFix: Linting Error
    CheckFixable --> TemplateRetry: Schema Error
    CheckFixable --> LLMFix: Compilation Error
    CheckFixable --> Fatal: Unfixable
    
    AutoFix --> Validate: Apply ESLint/stylelint --fix
    TemplateRetry --> Generate: Regenerate from template
    LLMFix --> Validate: Apply LLM fix
    
    Success --> [*]: Proceed to Next File
    Fatal --> [*]: Reject Build
    
    note right of CheckFixable
        Max 2 attempts per file
        Track attempt count
    end note
    
    note right of AutoFix
        ESLint --fix
        stylelint --fix
    end note
    
    note right of LLMFix
        Use FIX_CODE call type
        Max 1 LLM fix attempt
    end note
```

---

## 7. Build Verification Steps

```mermaid
sequenceDiagram
    participant P as Pipeline
    participant NPM as npm
    participant TSC as TypeScript
    participant PCF as pac pcf
    
    P->>NPM: npm install
    NPM-->>P: Dependencies installed ✓
    
    P->>TSC: npx tsc
    TSC->>TSC: Compile TypeScript
    alt Compilation Success
        TSC-->>P: Compiled ✓
    else Compilation Error
        TSC-->>P: Error: Type mismatch
        P->>P: Reject Build ✗
    end
    
    P->>PCF: pac pcf build
    PCF->>PCF: Build component
    alt Build Success
        PCF-->>P: Built ✓
    else Build Error
        PCF-->>P: Error: Invalid manifest
        P->>P: Reject Build ✗
    end
    
    P->>PCF: pac pcf push --dry-run
    PCF->>PCF: Validate deployment
    alt Validation Success
        PCF-->>P: Valid ✓
        P->>P: Proceed to Packaging
    else Validation Error
        PCF-->>P: Error: Missing resources
        P->>P: Reject Build ✗
    end
```

---

## 8. Package Structure

```mermaid
graph TD
    ROOT[Component Package ZIP] --> SRC[Source Files]
    ROOT --> BUILD[Build Artifacts]
    ROOT --> CONFIG[Configuration]
    ROOT --> DOCS[Documentation]
    
    SRC --> MANIFEST[ControlManifest.Input.xml]
    SRC --> INDEX[index.ts]
    SRC --> CSS[css/styles.css]
    SRC --> RESX[strings/strings.resx]
    
    BUILD --> BUNDLE[out/bundle.js]
    BUILD --> SOURCEMAP[out/bundle.js.map]
    BUILD --> PROCESSED[out/ControlManifest.xml]
    
    CONFIG --> PKG[package.json]
    CONFIG --> TSCONFIG[tsconfig.json]
    CONFIG --> GITIGNORE[.gitignore]
    
    DOCS --> README[README.md]
    
    style ROOT fill:#e1f5ff
    style SRC fill:#fff3e0
    style BUILD fill:#f3e5f5
    style CONFIG fill:#e8f5e9
    style DOCS fill:#fce4ec
```

---

## 9. Complete System Data Flow

```mermaid
flowchart TD
    USER[User Prompt] --> ORCH[Orchestrator]
    
    ORCH --> S1[Stage 1: Intent]
    S1 --> BR1[Brain Router: INTERPRET_INTENT]
    BR1 --> LLM1[LLM: Convert to JSON]
    LLM1 --> VAL1[Validator: Schema Check]
    VAL1 --> INTENT[GlobalIntent ✓]
    
    INTENT --> S2[Stage 2: Capability]
    S2 --> BR2[Brain Router: MATCH_CAPABILITY]
    BR2 --> MATCH[Registry Query]
    MATCH --> CAP[Capability ✓]
    
    CAP --> S3[Stage 3: Spec]
    S3 --> BR3[Brain Router: GENERATE_SPEC]
    BR3 --> LLM2[LLM: Generate Spec]
    LLM2 --> VAL2[Validator: Schema Check]
    VAL2 --> SPEC[ComponentSpec ✓]
    
    SPEC --> S4[Stage 4: Rules]
    S4 --> BR4[Brain Router: VALIDATE_RULES]
    BR4 --> VAL3[Validator: 34 Rules]
    VAL3 --> VSPEC[Validated Spec ✓]
    
    VSPEC --> S5[Stage 5: Final]
    S5 --> BR5[Brain Router: VALIDATE_FINAL]
    BR5 --> VAL4[Validator: Cross-Reference]
    VAL4 --> ASPEC[Approved Spec ✓]
    
    ASPEC --> S6[Stage 6: Code Gen]
    S6 --> GEN[Code Generator]
    GEN --> FILES[8 Generated Files]
    FILES --> LINT[Linting & Validation]
    LINT --> CODE[Source Code ✓]
    
    CODE --> S7[Stage 7: Package]
    S7 --> BUILD[Build Verification]
    BUILD --> PKG[Packager]
    PKG --> ZIP[Deployable ZIP ✓]
    
    ZIP --> USER_RESULT[User Download]
    
    style USER fill:#e1f5ff
    style USER_RESULT fill:#c8e6c9
    style INTENT fill:#fff3e0
    style CAP fill:#fff3e0
    style SPEC fill:#fff3e0
    style VSPEC fill:#fff3e0
    style ASPEC fill:#fff3e0
    style CODE fill:#f3e5f5
    style ZIP fill:#c8e6c9
```

---

## Summary

These diagrams illustrate:

1. **Orchestrator State Machine** - Complete state transitions with retry logic
2. **Brain Router Task Flow** - File loading and budget calculation
3. **LLM Call Flow** - Retry strategy with exponential backoff
4. **Code Generation Pipeline** - 8-step file generation with validation
5. **Validation Checkpoints** - 5 validation stages across the pipeline
6. **Error-Fix Loop** - Auto-fix, template retry, and LLM fix strategies
7. **Build Verification** - 4-step verification process
8. **Package Structure** - ZIP contents and organization
9. **Complete System Data Flow** - End-to-end from user prompt to ZIP

All diagrams use Mermaid for easy rendering and modification.
