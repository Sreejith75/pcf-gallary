# Component Interaction Diagram

## Service Communication Patterns

```mermaid
graph TB
    subgraph "Orchestrator Core"
        ORCH[Orchestrator]
    end
    
    subgraph "Support Services"
        BR[Brain Router]
        LLM[LLM Adapter]
        VAL[Validator]
        CODEGEN[Code Generator]
        PKG[Packager]
    end
    
    subgraph "Data Sources"
        BRAIN[(AI Brain<br/>File System)]
        CACHE[(Redis Cache)]
    end
    
    subgraph "External APIs"
        OPENAI[OpenAI]
        ANTHROPIC[Anthropic]
        AZURE[Azure OpenAI]
    end
    
    ORCH -->|loadForStage| BR
    ORCH -->|executePrompt| LLM
    ORCH -->|validateSchema| VAL
    ORCH -->|executeRules| VAL
    ORCH -->|generate| CODEGEN
    ORCH -->|createZip| PKG
    
    BR -->|read| BRAIN
    BR -->|get/set| CACHE
    
    LLM -->|API call| OPENAI
    LLM -->|API call| ANTHROPIC
    LLM -->|API call| AZURE
    
    VAL -->|loadRules| BR
    
    style ORCH fill:#fff4e6
    style BRAIN fill:#e1f5ff
    style CACHE fill:#fff3e0
```

## Orchestrator State Machine

```mermaid
stateDiagram-v2
    [*] --> Initialized: buildComponent()
    
    Initialized --> IntentInterpretation: Start Stage 1
    IntentInterpretation --> CapabilityMatching: Intent Valid
    IntentInterpretation --> Error: Intent Invalid
    
    CapabilityMatching --> SpecGeneration: Capability Found
    CapabilityMatching --> Error: No Capability
    
    SpecGeneration --> RulesValidation: Spec Valid
    SpecGeneration --> Error: Spec Invalid
    
    RulesValidation --> FinalValidation: No Errors
    RulesValidation --> Error: Rule Violation
    
    FinalValidation --> CodeGeneration: Approved
    FinalValidation --> Error: Cross-Reference Failed
    
    CodeGeneration --> Packaging: Files Generated
    
    Packaging --> Complete: ZIP Created
    
    Complete --> [*]
    Error --> [*]
```

## Brain Router Caching Strategy

```mermaid
sequenceDiagram
    participant Orchestrator
    participant BrainRouter
    participant Cache
    participant FileSystem
    
    Orchestrator->>BrainRouter: loadSchema("global-intent")
    BrainRouter->>Cache: get("schema:global-intent")
    
    alt Cache Hit
        Cache-->>BrainRouter: Schema content
        BrainRouter-->>Orchestrator: Schema
    else Cache Miss
        Cache-->>BrainRouter: null
        BrainRouter->>FileSystem: read("global-intent.schema.json")
        FileSystem-->>BrainRouter: File content
        BrainRouter->>Cache: set("schema:global-intent", content, TTL=3600)
        BrainRouter-->>Orchestrator: Schema
    end
```

## LLM Adapter Retry Logic

```mermaid
flowchart TD
    START[Execute LLM Call]
    ATTEMPT[Attempt API Call]
    SUCCESS{Success?}
    PARSE[Parse Response]
    VALIDATE{Valid JSON?}
    RETRY_COUNT{Retry Count<br/>< Max?}
    BACKOFF[Exponential Backoff]
    ERROR[Return Error]
    RETURN[Return Result]
    
    START --> ATTEMPT
    ATTEMPT --> SUCCESS
    SUCCESS -->|Yes| PARSE
    SUCCESS -->|No| RETRY_COUNT
    PARSE --> VALIDATE
    VALIDATE -->|Yes| RETURN
    VALIDATE -->|No| RETRY_COUNT
    RETRY_COUNT -->|Yes| BACKOFF
    RETRY_COUNT -->|No| ERROR
    BACKOFF --> ATTEMPT
    
    style ERROR fill:#ffcdd2
    style RETURN fill:#c8e6c9
```

## Validator Rule Execution Flow

```mermaid
flowchart LR
    INPUT[Component Spec]
    
    subgraph "Rule Loading"
        LOADCORE[Load Core Rules]
        LOADPERF[Load Perf Rules]
        LOADA11Y[Load A11Y Rules]
    end
    
    subgraph "Rule Execution"
        PARSECORE[Parse Core Rules]
        PARSEPERF[Parse Perf Rules]
        PARSEA11Y[Parse A11Y Rules]
        
        EXECCORE[Execute Core]
        EXECPERF[Execute Perf]
        EXECA11Y[Execute A11Y]
    end
    
    subgraph "Action Application"
        COLLECT[Collect Results]
        APPLYERR[Apply Error Actions]
        APPLYWARN[Apply Warning Actions]
        APPLYINFO[Apply Info Actions]
    end
    
    OUTPUT[Validation Result]
    
    INPUT --> LOADCORE
    INPUT --> LOADPERF
    INPUT --> LOADA11Y
    
    LOADCORE --> PARSECORE
    LOADPERF --> PARSEPERF
    LOADA11Y --> PARSEA11Y
    
    PARSECORE --> EXECCORE
    PARSEPERF --> EXECPERF
    PARSEA11Y --> EXECA11Y
    
    EXECCORE --> COLLECT
    EXECPERF --> COLLECT
    EXECA11Y --> COLLECT
    
    COLLECT --> APPLYERR
    COLLECT --> APPLYWARN
    COLLECT --> APPLYINFO
    
    APPLYERR --> OUTPUT
    APPLYWARN --> OUTPUT
    APPLYINFO --> OUTPUT
```

## Code Generator Template Flow

```mermaid
flowchart TD
    INPUT[Approved Spec]
    
    LOADCAP[Load Capability]
    GETTEMPLATES[Get Template Paths]
    
    subgraph "Template Rendering"
        LOADTS[Load TS Template]
        LOADXML[Load XML Template]
        LOADCSS[Load CSS Template]
        LOADRESX[Load RESX Template]
        
        RENDERTS[Render TypeScript]
        RENDERXML[Render Manifest]
        RENDERCSS[Render CSS]
        RENDERRESX[Render RESX]
    end
    
    subgraph "Post-Processing"
        LINTTS[Lint TypeScript]
        FORMATXML[Format XML]
        PREFIXCSS[Prefix CSS]
    end
    
    OUTPUT[Generated Files]
    
    INPUT --> LOADCAP
    LOADCAP --> GETTEMPLATES
    
    GETTEMPLATES --> LOADTS
    GETTEMPLATES --> LOADXML
    GETTEMPLATES --> LOADCSS
    GETTEMPLATES --> LOADRESX
    
    LOADTS --> RENDERTS
    LOADXML --> RENDERXML
    LOADCSS --> RENDERCSS
    LOADRESX --> RENDERRESX
    
    RENDERTS --> LINTTS
    RENDERXML --> FORMATXML
    RENDERCSS --> PREFIXCSS
    
    LINTTS --> OUTPUT
    FORMATXML --> OUTPUT
    PREFIXCSS --> OUTPUT
    RENDERRESX --> OUTPUT
```

## Packager Assembly Flow

```mermaid
flowchart LR
    INPUT[Generated Files]
    
    subgraph "Structure Creation"
        CREATEROOT[Create Root Folder]
        CREATESRC[Create src/]
        CREATERES[Create resources/]
    end
    
    subgraph "File Placement"
        PLACETS[Place index.ts]
        PLACEXML[Place Manifest]
        PLACECSS[Place CSS]
        PLACERESX[Place RESX]
    end
    
    subgraph "Metadata Generation"
        GENPKG[Generate package.json]
        GENPCF[Generate pcfconfig.json]
        GENREADME[Generate README.md]
    end
    
    subgraph "Packaging"
        BUNDLE[Bundle All Files]
        CREATEZIP[Create ZIP]
        VALIDATE[Validate Structure]
    end
    
    OUTPUT[ZIP Buffer]
    
    INPUT --> CREATEROOT
    CREATEROOT --> CREATESRC
    CREATEROOT --> CREATERES
    
    CREATESRC --> PLACETS
    CREATESRC --> PLACEXML
    CREATERES --> PLACECSS
    CREATERES --> PLACERESX
    
    PLACETS --> GENPKG
    PLACEXML --> GENPKG
    GENPKG --> GENPCF
    GENPCF --> GENREADME
    
    GENREADME --> BUNDLE
    BUNDLE --> CREATEZIP
    CREATEZIP --> VALIDATE
    VALIDATE --> OUTPUT
```

## Inter-Service Dependencies

```mermaid
graph TD
    ORCH[Orchestrator]
    BR[Brain Router]
    LLM[LLM Adapter]
    VAL[Validator]
    CODEGEN[Code Generator]
    PKG[Packager]
    
    ORCH -.->|depends on| BR
    ORCH -.->|depends on| LLM
    ORCH -.->|depends on| VAL
    ORCH -.->|depends on| CODEGEN
    CODEGEN -.->|depends on| PKG
    VAL -.->|depends on| BR
    
    style ORCH fill:#fff4e6
    style BR fill:#e1f5ff
    style LLM fill:#f3e5f5
    style VAL fill:#e8f5e9
    style CODEGEN fill:#fff3e0
    style PKG fill:#fce4ec
```

## Concurrent Operations

```mermaid
gantt
    title Component Build Timeline
    dateFormat  X
    axisFormat %Ls
    
    section Stage 1
    Load Brain Files    :a1, 0, 100ms
    Execute LLM        :a2, after a1, 1500ms
    Validate Schema    :a3, after a2, 50ms
    
    section Stage 2
    Load Capability    :b1, after a3, 50ms
    Match Capability   :b2, after b1, 50ms
    
    section Stage 3
    Load Schema        :c1, after b2, 50ms
    Execute LLM        :c2, after c1, 2000ms
    Validate Schema    :c3, after c2, 50ms
    
    section Stage 4
    Load Rules         :d1, after c3, 100ms
    Execute Rules      :d2, after d1, 400ms
    
    section Stage 5
    Final Validation   :e1, after d2, 100ms
    
    section Generation
    Generate Code      :f1, after e1, 800ms
    Create Package     :f2, after f1, 400ms
```

## Error Propagation

```mermaid
flowchart TD
    SERVICE[Service Layer]
    ERROR{Error Occurs}
    
    ERROR -->|Schema Validation| SCHEMA_ERR[ValidationError]
    ERROR -->|Rule Violation| RULE_ERR[RuleViolationError]
    ERROR -->|LLM Failure| LLM_ERR[LLMError]
    ERROR -->|File Not Found| FILE_ERR[FileNotFoundError]
    
    SCHEMA_ERR --> WRAP[Wrap in BuildError]
    RULE_ERR --> WRAP
    LLM_ERR --> WRAP
    FILE_ERR --> WRAP
    
    WRAP --> LOG[Log to Audit Trail]
    LOG --> FORMAT[Format User Message]
    FORMAT --> RESPONSE[Return Error Response]
    
    style ERROR fill:#ffcdd2
    style RESPONSE fill:#ffcdd2
```
