# AppWeaver.AIBrain

**AI Brain Runtime** - Orchestration and validation engine for AI-driven PCF component generation.

## Overview

`AppWeaver.AIBrain` is a C# class library that serves as the **orchestration and validation layer** for the PCF Component Builder. It loads AI Brain artifacts (schemas, rules, capabilities, procedures), routes requests to minimal contexts, validates component specifications, and produces execution plans for the Node.js code generation layer.

**Core Principle**: The AI Brain decides what is possible. This library enforces that decision.

## Architecture

This library implements a **compiler-style architecture**:
- **Brain Loader**: Loads and deserializes brain artifacts from disk
- **Brain Router**: Routes tasks to minimal required context (indexed, not injected)
- **Brain Context**: Minimal context passed to LLM execution layers
- **Validators**: Enforce capability bounds and PCF rules
- **Procedure Executor**: Orchestrates validation and produces execution plans

## Installation

```bash
dotnet add package AppWeaver.AIBrain
```

## Quick Start

### 1. Register Services

```csharp
using AppWeaver.AIBrain;

var builder = WebApplication.CreateBuilder(args);

// Register AI Brain services
builder.Services.AddAppWeaverAIBrain(
    brainRootPath: "/path/to/ai-brain",
    options =>
    {
        options.MaxTokensPerTask = 5000;
        options.EnableCaching = true;
        options.DefaultNamespace = "Contoso";
    });

var app = builder.Build();
```

### 2. Route a Brain Task

```csharp
using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Models;

public class ComponentService
{
    private readonly IBrainRouter _router;

    public ComponentService(IBrainRouter router)
    {
        _router = router;
    }

    public async Task<IBrainContext> GetIntentContextAsync()
    {
        // Load minimal context for intent interpretation
        var context = await _router.RouteAsync(BrainTask.InterpretIntent);

        // Context contains only: schema, intent rules, ambiguity rules
        var schema = context.GetArtifact<object>("schema");
        var intentRules = context.GetArtifact<object>("intentMappingRules");

        return context;
    }
}
```

### 3. Validate a Component Spec

```csharp
using AppWeaver.AIBrain.Abstractions;
using AppWeaver.AIBrain.Models.Specs;

public class ValidationService
{
    private readonly IProcedureExecutor _executor;

    public ValidationService(IProcedureExecutor executor)
    {
        _executor = executor;
    }

    public async Task<SpecValidationResult> ValidateAsync(ComponentSpec spec)
    {
        // Validate against capability bounds and PCF rules
        var result = await _executor.ValidateComponentSpecAsync(spec);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"[{error.RuleId}] {error.Message}");
            }
        }

        return result;
    }
}
```

## Brain Tasks

The library supports 8 brain tasks:

| Task | Loads | Purpose |
|------|-------|---------|
| `InterpretIntent` | global-intent schema, intent rules | Parse natural language to GlobalIntent |
| `MatchCapability` | registry, capability | Match intent to capability |
| `GenerateComponentSpec` | component-spec schema, capability | Generate ComponentSpec |
| `ValidateRules` | PCF rules, accessibility rules | Validate against 34 rules |
| `ValidateFinal` | schema, capability | Final cross-reference validation |
| `LoadSchema` | specified schema | Load schema for ad-hoc use |
| `LoadCapability` | specified capability | Load capability for ad-hoc use |
| `LoadPrompt` | specified prompt | Load prompt template |

## Validation

The library enforces **34 validation rules** across 5 categories:

- **PCF Lifecycle** (4 rules): Lifecycle methods, context storage
- **Performance** (10 rules): Bundle size, execution time, dependencies
- **Accessibility** (9 rules): WCAG AA compliance, keyboard nav, ARIA
- **Security** (6 rules): No external calls, XSS prevention
- **Core PCF** (15 rules): Naming, binding, manifest, resources

### Validation Result

```csharp
public record SpecValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; }
    public IReadOnlyList<ValidationWarning> Warnings { get; init; }
    public IReadOnlyList<ValidationDowngrade> Downgrades { get; init; }
    public int TotalRules { get; init; }
    public int PassedRules { get; init; }
}
```

## Integration with Node.js

This library produces **execution plans** that the Node.js layer executes:

```csharp
public record ComponentExecutionPlan
{
    public string BuildId { get; init; }
    public GlobalIntent Intent { get; init; }
    public string CapabilityId { get; init; }
    public ComponentSpec ComponentSpec { get; init; }
    public IReadOnlyList<FileGenerationStep> FilesToGenerate { get; init; }
    public SpecValidationResult ValidationReport { get; init; }
}
```

The Node.js layer:
1. Receives the execution plan
2. Calls LLMs for intent/spec generation
3. Generates TypeScript files using templates
4. Runs PCF CLI for build/packaging

## Configuration

```csharp
public class BrainOptions
{
    public string BrainRootPath { get; set; }           // Path to ai-brain/
    public int MaxTokensPerTask { get; set; } = 5000;   // Token budget
    public int MaxFilesPerTask { get; set; } = 10;      // File limit
    public bool EnableCaching { get; set; } = true;     // In-memory cache
    public int CacheExpirationMinutes { get; set; } = 60;
    public bool ValidateOnLoad { get; set; } = true;
    public string DefaultNamespace { get; set; } = "Contoso";
}
```

## Design Principles

1. **AI Brain is indexed, not injected** - Load only what's needed
2. **No LLM calls inside the Brain** - This is orchestration only
3. **One responsibility per class** - Single-purpose services
4. **Deterministic, testable behavior** - No randomness
5. **Clean Architecture** - No framework leakage

## License

MIT

## Version

1.0.0
