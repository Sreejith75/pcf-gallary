# System Architecture Diagram

## Overall System Architecture

```mermaid
graph TB
    subgraph "PRESENTATION LAYER"
        UI[Web UI / API Client]
    end
    
    subgraph "ORCHESTRATION LAYER"
        ORCH[Component Builder<br/>Orchestrator]
        BR[Brain Router]
    end
    
    subgraph "REASONING LAYER"
        BRAIN[(AI Brain<br/>Read-Only)]
        LLM[LLM Service<br/>Adapter]
        VAL[Validator<br/>Engine]
    end
    
    subgraph "GENERATION LAYER"
        CODEGEN[Code<br/>Generator]
    end
    
    subgraph "PACKAGING LAYER"
        PKG[PCF<br/>Packager]
    end
    
    subgraph "EXTERNAL SERVICES"
        OPENAI[OpenAI API]
        ANTHROPIC[Anthropic API]
        AZURE[Azure OpenAI]
    end
    
    UI -->|User Prompt| ORCH
    ORCH -->|Load Files| BR
    BR -->|Read| BRAIN
    ORCH -->|Execute Prompt| LLM
    LLM -->|API Call| OPENAI
    LLM -->|API Call| ANTHROPIC
    LLM -->|API Call| AZURE
    ORCH -->|Validate| VAL
    VAL -->|Load Rules| BR
    ORCH -->|Generate Code| CODEGEN
    CODEGEN -->|Package| PKG
    PKG -->|ZIP File| UI
    
    style BRAIN fill:#e1f5ff
    style ORCH fill:#fff4e6
    style LLM fill:#f3e5f5
    style VAL fill:#e8f5e9
    style CODEGEN fill:#fff3e0
    style PKG fill:#fce4ec
```

## Layer Interaction Flow

```mermaid
sequenceDiagram
    participant User
    participant Presentation
    participant Orchestrator
    participant BrainRouter
    participant AIBrain
    participant LLMAdapter
    participant Validator
    participant CodeGen
    participant Packager
    
    User->>Presentation: Submit component request
    Presentation->>Orchestrator: buildComponent(prompt)
    
    Note over Orchestrator: Stage 1: Intent Interpretation
    Orchestrator->>BrainRouter: loadForStage(INTENT_INTERPRETATION)
    BrainRouter->>AIBrain: Read schemas & rules
    AIBrain-->>BrainRouter: Files content
    BrainRouter-->>Orchestrator: Schemas & Rules
    Orchestrator->>LLMAdapter: executePrompt(intentPrompt)
    LLMAdapter-->>Orchestrator: GlobalIntent JSON
    Orchestrator->>Validator: validateSchema(intent)
    Validator-->>Orchestrator: Valid ✓
    
    Note over Orchestrator: Stage 2: Capability Matching
    Orchestrator->>BrainRouter: loadCapability(intent)
    BrainRouter->>AIBrain: Read capability
    AIBrain-->>BrainRouter: Capability definition
    BrainRouter-->>Orchestrator: Capability
    Orchestrator->>Orchestrator: Match capability
    
    Note over Orchestrator: Stage 3: Spec Generation
    Orchestrator->>BrainRouter: loadForStage(SPEC_GENERATION)
    BrainRouter->>AIBrain: Read spec schema
    AIBrain-->>BrainRouter: Schema
    BrainRouter-->>Orchestrator: Schema
    Orchestrator->>LLMAdapter: executePrompt(specPrompt)
    LLMAdapter-->>Orchestrator: ComponentSpec JSON
    Orchestrator->>Validator: validateSchema(spec)
    Validator-->>Orchestrator: Valid ✓
    
    Note over Orchestrator: Stage 4: Rules Validation
    Orchestrator->>BrainRouter: loadForStage(RULES_VALIDATION)
    BrainRouter->>AIBrain: Read all rules
    AIBrain-->>BrainRouter: 34 rules
    BrainRouter-->>Orchestrator: Rules
    Orchestrator->>Validator: executeRules(spec, rules)
    Validator-->>Orchestrator: Validated spec + warnings
    
    Note over Orchestrator: Stage 5: Final Validation
    Orchestrator->>Validator: crossReference(spec, capability)
    Validator-->>Orchestrator: Approved ✓
    
    Note over Orchestrator: Code Generation
    Orchestrator->>CodeGen: generate(spec)
    CodeGen-->>Orchestrator: Source files
    
    Note over Orchestrator: Packaging
    Orchestrator->>Packager: createZip(files)
    Packager-->>Orchestrator: ZIP buffer
    
    Orchestrator-->>Presentation: BuildResult
    Presentation-->>User: Download ZIP
```

## Service Dependencies

```mermaid
graph LR
    ORCH[Orchestrator]
    BR[Brain Router]
    LLM[LLM Adapter]
    VAL[Validator]
    CODEGEN[Code Generator]
    PKG[Packager]
    BRAIN[(AI Brain)]
    
    ORCH --> BR
    ORCH --> LLM
    ORCH --> VAL
    ORCH --> CODEGEN
    CODEGEN --> PKG
    BR --> BRAIN
    VAL --> BR
    
    style BRAIN fill:#e1f5ff
    style ORCH fill:#fff4e6
```

## Component Responsibilities

```mermaid
mindmap
  root((PCF Builder))
    Orchestrator
      Workflow Coordination
      Stage Management
      Error Handling
      Audit Logging
    Brain Router
      Selective File Loading
      Caching
      Schema Parsing
      File System Access
    LLM Adapter
      Prompt Assembly
      Model Execution
      Response Parsing
      Retry Logic
    Validator
      Schema Validation
      Rule Execution
      Downgrade Application
      Report Generation
    Code Generator
      Template Rendering
      TypeScript Generation
      Manifest Generation
      Resource Bundling
    Packager
      Folder Structure
      ZIP Creation
      Metadata Injection
      Package Validation
```

## Technology Stack

```mermaid
graph TB
    subgraph "Frontend"
        REACT[React / Next.js]
    end
    
    subgraph "Backend Services"
        NODE[Node.js 20+]
        TS[TypeScript 5+]
        EXPRESS[Express.js]
    end
    
    subgraph "Validation"
        AJV[Ajv JSON Schema]
        CUSTOM[Custom Rule Engine]
    end
    
    subgraph "Templates"
        HBS[Handlebars]
    end
    
    subgraph "LLM Integration"
        OPENAI_SDK[OpenAI SDK]
        ANTHROPIC_SDK[Anthropic SDK]
        AZURE_SDK[Azure OpenAI SDK]
    end
    
    subgraph "Packaging"
        JSZIP[JSZip]
    end
    
    subgraph "Storage"
        FS[File System<br/>AI Brain]
        REDIS[Redis<br/>Cache]
        PG[PostgreSQL<br/>Audit Logs]
    end
    
    REACT --> EXPRESS
    EXPRESS --> NODE
    NODE --> TS
    NODE --> AJV
    NODE --> CUSTOM
    NODE --> HBS
    NODE --> OPENAI_SDK
    NODE --> ANTHROPIC_SDK
    NODE --> AZURE_SDK
    NODE --> JSZIP
    NODE --> FS
    NODE --> REDIS
    NODE --> PG
```

## Deployment Architecture

```mermaid
graph TB
    subgraph "Client"
        BROWSER[Web Browser]
    end
    
    subgraph "Load Balancer"
        LB[NGINX / AWS ALB]
    end
    
    subgraph "Application Tier"
        APP1[App Instance 1]
        APP2[App Instance 2]
        APP3[App Instance N]
    end
    
    subgraph "Cache Layer"
        REDIS_CACHE[Redis Cluster]
    end
    
    subgraph "Storage Layer"
        FS_BRAIN[AI Brain<br/>File System<br/>Read-Only]
        DB[PostgreSQL<br/>Audit Logs]
    end
    
    subgraph "External Services"
        LLM_APIS[LLM APIs<br/>OpenAI/Anthropic/Azure]
    end
    
    BROWSER --> LB
    LB --> APP1
    LB --> APP2
    LB --> APP3
    APP1 --> REDIS_CACHE
    APP2 --> REDIS_CACHE
    APP3 --> REDIS_CACHE
    APP1 --> FS_BRAIN
    APP2 --> FS_BRAIN
    APP3 --> FS_BRAIN
    APP1 --> DB
    APP2 --> DB
    APP3 --> DB
    APP1 --> LLM_APIS
    APP2 --> LLM_APIS
    APP3 --> LLM_APIS
    
    style FS_BRAIN fill:#e1f5ff
    style REDIS_CACHE fill:#fff3e0
    style DB fill:#e8f5e9
```

## Key Characteristics

### Stateless Design
- Orchestrator instances are stateless
- Horizontal scaling supported
- No session affinity required

### Read-Only AI Brain
- Mounted as read-only file system
- Shared across all instances
- Versioned deployments

### Caching Strategy
- Brain artifacts cached in Redis
- TTL-based invalidation
- Reduces file I/O

### High Availability
- Multiple app instances
- Load balancer health checks
- Graceful degradation on LLM failures
