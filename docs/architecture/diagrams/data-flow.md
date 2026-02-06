# Data Flow Diagrams

## End-to-End Data Flow

```mermaid
flowchart TD
    START([User Prompt:<br/>"5-star rating control"]) --> INIT[Initialize Build Context]
    
    INIT --> STAGE1[Stage 1: Intent Interpretation]
    
    STAGE1 --> LOAD1[Load: global-intent.schema.json<br/>intent-mapping.rules.json<br/>ambiguity-resolution.rules.json]
    LOAD1 --> LLM1[LLM: Execute Intent Prompt]
    LLM1 --> VAL1{Validate<br/>Schema}
    VAL1 -->|Invalid| ERR1[Return Error]
    VAL1 -->|Valid| INTENT[GlobalIntent JSON]
    
    INTENT --> STAGE2[Stage 2: Capability Matching]
    STAGE2 --> LOAD2[Load: registry.index.json]
    LOAD2 --> QUERY[Query by classification<br/>+ primaryPurpose]
    QUERY --> MATCH{Capability<br/>Found?}
    MATCH -->|No| ERR2[Return Error:<br/>No matching capability]
    MATCH -->|Yes| LOADCAP[Load: star-rating.capability.json]
    LOADCAP --> CHECKCAP{Features<br/>Supported?}
    CHECKCAP -->|No| ERR3[Return Error:<br/>Unsupported feature]
    CHECKCAP -->|Yes| CAP[Capability Matched]
    
    CAP --> STAGE3[Stage 3: Spec Generation]
    STAGE3 --> LOAD3[Load: component-spec.schema.json<br/>capability definition]
    LOAD3 --> LLM2[LLM: Execute Spec Prompt]
    LLM2 --> VAL2{Validate<br/>Schema}
    VAL2 -->|Invalid| ERR4[Return Error]
    VAL2 -->|Valid| SPEC[ComponentSpec JSON]
    
    SPEC --> STAGE4[Stage 4: Rules Validation]
    STAGE4 --> LOAD4[Load: pcf-core.rules.md<br/>pcf-performance.rules.md<br/>pcf-accessibility.rules.md]
    LOAD4 --> EXECRULES[Execute 34 Rules]
    EXECRULES --> CHECKERR{Error-Level<br/>Violations?}
    CHECKERR -->|Yes| ERR5[Return Error:<br/>Rule violation]
    CHECKERR -->|No| CHECKWARN{Warnings?}
    CHECKWARN -->|Yes| DOWNGRADE[Apply Downgrades<br/>Document Warnings]
    CHECKWARN -->|No| VALSPEC[Validated Spec]
    DOWNGRADE --> VALSPEC
    
    VALSPEC --> STAGE5[Stage 5: Final Validation]
    STAGE5 --> CROSS[Cross-Reference<br/>Spec vs Capability]
    CROSS --> CHECKFINAL{Valid?}
    CHECKFINAL -->|No| ERR6[Return Error]
    CHECKFINAL -->|Yes| APPROVED[Approved Spec]
    
    APPROVED --> CODEGEN[Code Generation]
    CODEGEN --> GENFILES[Generate:<br/>index.ts<br/>ControlManifest.xml<br/>styles.css<br/>strings.resx]
    
    GENFILES --> PACKAGE[Packaging]
    PACKAGE --> CREATEZIP[Create ZIP:<br/>Folder structure<br/>package.json<br/>Bundle resources]
    
    CREATEZIP --> OUTPUT([ZIP File Output])
    
    ERR1 --> END([Error Response])
    ERR2 --> END
    ERR3 --> END
    ERR4 --> END
    ERR5 --> END
    ERR6 --> END
    
    style START fill:#e3f2fd
    style OUTPUT fill:#c8e6c9
    style END fill:#ffcdd2
    style INTENT fill:#fff9c4
    style CAP fill:#fff9c4
    style SPEC fill:#fff9c4
    style VALSPEC fill:#fff9c4
    style APPROVED fill:#c8e6c9
```

## Stage 1: Intent Interpretation Data Flow

```mermaid
flowchart LR
    INPUT["User Prompt:<br/>'5-star rating control'"]
    
    subgraph "Brain Router"
        LOAD1[Load Schema]
        LOAD2[Load Mapping Rules]
        LOAD3[Load Resolution Rules]
    end
    
    subgraph "LLM Adapter"
        ASSEMBLE[Assemble Prompt]
        EXECUTE[Execute LLM]
        PARSE[Parse Response]
    end
    
    subgraph "Validator"
        VALIDATE[Validate Schema]
    end
    
    OUTPUT["GlobalIntent JSON:<br/>{<br/>  classification: 'input-control',<br/>  uiIntent: {<br/>    primaryPurpose: 'collect-rating'<br/>  }<br/>}"]
    
    INPUT --> LOAD1
    INPUT --> LOAD2
    INPUT --> LOAD3
    LOAD1 --> ASSEMBLE
    LOAD2 --> ASSEMBLE
    LOAD3 --> ASSEMBLE
    ASSEMBLE --> EXECUTE
    EXECUTE --> PARSE
    PARSE --> VALIDATE
    VALIDATE --> OUTPUT
```

## Stage 2: Capability Matching Data Flow

```mermaid
flowchart LR
    INPUT["GlobalIntent JSON"]
    
    subgraph "Brain Router"
        LOADREG[Load Registry Index]
        LOADCAP[Load Capability File]
    end
    
    subgraph "Orchestrator"
        QUERY[Query by:<br/>classification +<br/>primaryPurpose]
        VERIFY[Verify Features]
        CHECKFORB[Check Forbidden<br/>Behaviors]
    end
    
    OUTPUT["Matched Capability:<br/>'star-rating'"]
    
    INPUT --> LOADREG
    LOADREG --> QUERY
    QUERY --> LOADCAP
    LOADCAP --> VERIFY
    VERIFY --> CHECKFORB
    CHECKFORB --> OUTPUT
```

## Stage 3: Specification Generation Data Flow

```mermaid
flowchart LR
    INPUT1["GlobalIntent JSON"]
    INPUT2["Capability Definition"]
    
    subgraph "Brain Router"
        LOADSCHEMA[Load Component<br/>Spec Schema]
    end
    
    subgraph "LLM Adapter"
        ASSEMBLE[Assemble Prompt<br/>with Intent +<br/>Capability]
        EXECUTE[Execute LLM]
        PARSE[Parse Response]
    end
    
    subgraph "Validator"
        VALSCHEMA[Validate Schema]
        VALPROPS[Validate Properties]
        VALRES[Validate Resources]
    end
    
    OUTPUT["ComponentSpec JSON:<br/>{<br/>  componentId: 'star-rating',<br/>  properties: [...],<br/>  resources: {...}<br/>}"]
    
    INPUT1 --> ASSEMBLE
    INPUT2 --> ASSEMBLE
    LOADSCHEMA --> ASSEMBLE
    ASSEMBLE --> EXECUTE
    EXECUTE --> PARSE
    PARSE --> VALSCHEMA
    VALSCHEMA --> VALPROPS
    VALPROPS --> VALRES
    VALRES --> OUTPUT
```

## Stage 4: Rules Validation Data Flow

```mermaid
flowchart TD
    INPUT["ComponentSpec JSON"]
    
    subgraph "Brain Router"
        LOADCORE[Load PCF Core Rules<br/>15 rules]
        LOADPERF[Load Performance Rules<br/>10 rules]
        LOADA11Y[Load Accessibility Rules<br/>9 rules]
    end
    
    subgraph "Validator"
        EXECCORE[Execute Core Rules]
        EXECPERF[Execute Perf Rules]
        EXECA11Y[Execute A11Y Rules]
        
        CHECKERR{Error-Level<br/>Violations?}
        CHECKWARN{Warnings?}
        
        APPLYFIX[Apply Auto-Fixes]
        DOCDOWN[Document Downgrades]
    end
    
    OUTPUT["Validated Spec +<br/>Warnings +<br/>Downgrades"]
    
    INPUT --> LOADCORE
    INPUT --> LOADPERF
    INPUT --> LOADA11Y
    
    LOADCORE --> EXECCORE
    LOADPERF --> EXECPERF
    LOADA11Y --> EXECA11Y
    
    EXECCORE --> CHECKERR
    EXECPERF --> CHECKERR
    EXECA11Y --> CHECKERR
    
    CHECKERR -->|Yes| REJECT[Reject Spec]
    CHECKERR -->|No| CHECKWARN
    
    CHECKWARN -->|Yes| APPLYFIX
    APPLYFIX --> DOCDOWN
    DOCDOWN --> OUTPUT
    
    CHECKWARN -->|No| OUTPUT
    
    style REJECT fill:#ffcdd2
```

## Code Generation Data Flow

```mermaid
flowchart LR
    INPUT["Approved ComponentSpec"]
    
    subgraph "Code Generator"
        LOADTEMP[Load Templates]
        GENTS[Generate TypeScript]
        GENMAN[Generate Manifest]
        GENCSS[Generate CSS]
        GENRESX[Generate RESX]
    end
    
    OUTPUT["Generated Files:<br/>- index.ts<br/>- ControlManifest.xml<br/>- styles.css<br/>- strings.resx"]
    
    INPUT --> LOADTEMP
    LOADTEMP --> GENTS
    LOADTEMP --> GENMAN
    LOADTEMP --> GENCSS
    LOADTEMP --> GENRESX
    
    GENTS --> OUTPUT
    GENMAN --> OUTPUT
    GENCSS --> OUTPUT
    GENRESX --> OUTPUT
```

## Packaging Data Flow

```mermaid
flowchart LR
    INPUT["Generated Files"]
    
    subgraph "Packager"
        CREATESTRUCT[Create Folder<br/>Structure]
        GENPKG[Generate<br/>package.json]
        BUNDLE[Bundle Resources]
        CREATEZIP[Create ZIP]
        VALIDATE[Validate Package]
    end
    
    OUTPUT["ZIP Buffer"]
    
    INPUT --> CREATESTRUCT
    CREATESTRUCT --> GENPKG
    GENPKG --> BUNDLE
    BUNDLE --> CREATEZIP
    CREATEZIP --> VALIDATE
    VALIDATE --> OUTPUT
```

## Error Handling Flow

```mermaid
flowchart TD
    ERROR[Error Detected]
    
    CLASSIFY{Error Type}
    
    CLASSIFY -->|Schema Validation| SCHEMA_ERR[Schema Validation Error]
    CLASSIFY -->|Rule Violation| RULE_ERR[Rule Violation Error]
    CLASSIFY -->|Capability Not Found| CAP_ERR[Capability Error]
    CLASSIFY -->|LLM Failure| LLM_ERR[LLM Error]
    
    SCHEMA_ERR --> FORMAT1[Format Error Message]
    RULE_ERR --> FORMAT2[Format Error Message<br/>+ Suggestion]
    CAP_ERR --> FORMAT3[Format Error Message<br/>+ Alternatives]
    LLM_ERR --> RETRY{Retry<br/>Possible?}
    
    RETRY -->|Yes| BACKOFF[Exponential Backoff]
    RETRY -->|No| FORMAT4[Format Error Message]
    
    BACKOFF --> REATTEMPT[Reattempt LLM Call]
    REATTEMPT --> SUCCESS{Success?}
    SUCCESS -->|Yes| CONTINUE[Continue Workflow]
    SUCCESS -->|No| FORMAT4
    
    FORMAT1 --> LOG[Log to Audit Trail]
    FORMAT2 --> LOG
    FORMAT3 --> LOG
    FORMAT4 --> LOG
    
    LOG --> RESPONSE[Return Error Response<br/>to User]
    
    style ERROR fill:#ffcdd2
    style RESPONSE fill:#ffcdd2
    style CONTINUE fill:#c8e6c9
```

## Data Transformations Summary

| Stage | Input | Process | Output |
|-------|-------|---------|--------|
| 1 | `string` (user prompt) | Intent interpretation via LLM | `GlobalIntent` JSON |
| 2 | `GlobalIntent` | Capability matching via registry | `capabilityId: string` |
| 3 | `GlobalIntent + Capability` | Spec generation via LLM | `ComponentSpec` JSON |
| 4 | `ComponentSpec` | Rules validation (34 rules) | `ValidatedSpec` JSON |
| 5 | `ValidatedSpec` | Cross-reference validation | `ApprovedSpec` JSON |
| 6 | `ApprovedSpec` | Template-based code generation | `GeneratedFile[]` |
| 7 | `GeneratedFile[]` | ZIP packaging | `Buffer` (ZIP file) |
