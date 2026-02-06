# LLM Integration Specification

## Executive Summary

The LLM is used sparingly and strategically within the PCF Component Builder. Each LLM call has a single, well-defined task with strict input/output contracts. Prompts are thin and disposable, referencing AI Brain artifacts rather than embedding logic. Hallucination is prevented by design through schema validation, enum constraints, and capability bounds.

**Core Principle**: The LLM executes, never improvises. The AI Brain decides, the LLM translates.

---

## 1. Types of LLM Calls

```typescript
/**
 * LLMCallType defines the purpose of an LLM invocation
 */
export enum LLMCallType {
  /**
   * Convert natural language to GlobalIntent JSON
   * Used in: Stage 1 (Intent Interpretation)
   * Output: JSON only
   */
  INTERPRET_INTENT = 'interpret-intent',
  
  /**
   * Generate ComponentSpec from intent and capability
   * Used in: Stage 3 (Specification Generation)
   * Output: JSON only
   */
  GENERATE_SPEC = 'generate-spec',
  
  /**
   * Generate a single code file from spec
   * Used in: Stage 6 (Code Generation) - if LLM-assisted
   * Output: Code only (TypeScript, CSS, XML, RESX)
   */
  GENERATE_FILE = 'generate-file',
  
  /**
   * Fix linting or validation errors in generated code
   * Used in: Stage 6 (Code Generation) - retry on error
   * Output: Code only
   */
  FIX_CODE = 'fix-code'
}
```

### Call Frequency

| Call Type | Frequency per Build | Stage | Required |
|-----------|-------------------|-------|----------|
| INTERPRET_INTENT | 1 | Stage 1 | ✅ Yes |
| GENERATE_SPEC | 1 | Stage 3 | ✅ Yes |
| GENERATE_FILE | 0-4 | Stage 6 | ❌ No (template-based preferred) |
| FIX_CODE | 0-2 | Stage 6 | ❌ No (only on error) |

**Typical build**: 2 LLM calls (INTERPRET_INTENT + GENERATE_SPEC)  
**Maximum build**: 8 LLM calls (2 required + 4 files + 2 fixes)

---

## 2. Input Structure for Each Call Type

### 2.1 INTERPRET_INTENT

```typescript
interface InterpretIntentInput {
  callType: LLMCallType.INTERPRET_INTENT;
  
  /**
   * User's natural language component description
   */
  userPrompt: string;
  
  /**
   * Brain artifacts loaded by Brain Router
   */
  brainArtifacts: {
    schema: JSONSchema;              // global-intent.schema.json
    intentMappingRules: any;         // intent-mapping.rules.json
    ambiguityResolutionRules: any;   // ambiguity-resolution.rules.json
  };
  
  /**
   * Prompt template
   */
  promptTemplate: string;            // intent-interpreter.prompt.md
  
  /**
   * Configuration
   */
  config: {
    temperature: 0.1;                // Low temperature for determinism
    maxTokens: 2000;                 // Max output tokens
    responseFormat: 'json_object';   // Force JSON output
  };
}
```

**Example**:
```typescript
{
  callType: 'interpret-intent',
  userPrompt: 'I need a 5-star rating control',
  brainArtifacts: {
    schema: { /* global-intent.schema.json */ },
    intentMappingRules: { /* rules */ },
    ambiguityResolutionRules: { /* rules */ }
  },
  promptTemplate: '# Intent Interpreter Prompt\n...',
  config: {
    temperature: 0.1,
    maxTokens: 2000,
    responseFormat: 'json_object'
  }
}
```

---

### 2.2 GENERATE_SPEC

```typescript
interface GenerateSpecInput {
  callType: LLMCallType.GENERATE_SPEC;
  
  /**
   * Validated GlobalIntent from Stage 1
   */
  globalIntent: GlobalIntent;
  
  /**
   * Matched capability from Stage 2
   */
  capability: Capability;
  
  /**
   * Brain artifacts loaded by Brain Router
   */
  brainArtifacts: {
    schema: JSONSchema;              // component-spec.schema.json
    capability: Capability;          // matched capability file
  };
  
  /**
   * Prompt template
   */
  promptTemplate: string;            // component-spec-generator.prompt.md
  
  /**
   * Configuration
   */
  config: {
    namespace: string;               // e.g., 'Contoso'
    temperature: 0.1;
    maxTokens: 3000;
    responseFormat: 'json_object';
  };
}
```

**Example**:
```typescript
{
  callType: 'generate-spec',
  globalIntent: {
    classification: 'input-control',
    uiIntent: { primaryPurpose: 'collect-rating', ... }
  },
  capability: {
    capabilityId: 'star-rating',
    supportedFeatures: ['basic-rating', 'hover-preview'],
    limits: { maxStars: 10 }
  },
  brainArtifacts: {
    schema: { /* component-spec.schema.json */ },
    capability: { /* star-rating.capability.json */ }
  },
  promptTemplate: '# Component Spec Generator Prompt\n...',
  config: {
    namespace: 'Contoso',
    temperature: 0.1,
    maxTokens: 3000,
    responseFormat: 'json_object'
  }
}
```

---

### 2.3 GENERATE_FILE

```typescript
interface GenerateFileInput {
  callType: LLMCallType.GENERATE_FILE;
  
  /**
   * Approved ComponentSpec from Stage 5
   */
  componentSpec: ComponentSpec;
  
  /**
   * File to generate
   */
  fileType: 'typescript' | 'css' | 'xml' | 'resx';
  
  /**
   * Template (if template-based generation fails)
   */
  template?: string;
  
  /**
   * Prompt template
   */
  promptTemplate: string;            // file-generator.prompt.md
  
  /**
   * Configuration
   */
  config: {
    temperature: 0.2;                // Slightly higher for code variety
    maxTokens: 4000;
    responseFormat: 'text';          // Code output
  };
}
```

**Example**:
```typescript
{
  callType: 'generate-file',
  componentSpec: { /* approved spec */ },
  fileType: 'typescript',
  promptTemplate: '# File Generator Prompt\n...',
  config: {
    temperature: 0.2,
    maxTokens: 4000,
    responseFormat: 'text'
  }
}
```

---

### 2.4 FIX_CODE

```typescript
interface FixCodeInput {
  callType: LLMCallType.FIX_CODE;
  
  /**
   * Original code with errors
   */
  originalCode: string;
  
  /**
   * Linting/validation errors
   */
  errors: Array<{
    line: number;
    column: number;
    message: string;
    rule: string;
  }>;
  
  /**
   * ComponentSpec for context
   */
  componentSpec: ComponentSpec;
  
  /**
   * Prompt template
   */
  promptTemplate: string;            // code-fixer.prompt.md
  
  /**
   * Configuration
   */
  config: {
    temperature: 0.1;                // Low for minimal changes
    maxTokens: 4000;
    responseFormat: 'text';
  };
}
```

**Example**:
```typescript
{
  callType: 'fix-code',
  originalCode: 'export class StarRating { ... }',
  errors: [
    {
      line: 15,
      column: 10,
      message: 'Missing semicolon',
      rule: 'semi'
    }
  ],
  componentSpec: { /* spec */ },
  promptTemplate: '# Code Fixer Prompt\n...',
  config: {
    temperature: 0.1,
    maxTokens: 4000,
    responseFormat: 'text'
  }
}
```

---

## 3. Output Contracts

### 3.1 INTERPRET_INTENT Output

```typescript
interface InterpretIntentOutput {
  /**
   * Success case: Valid GlobalIntent JSON
   */
  success: true;
  data: GlobalIntent;
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

interface InterpretIntentError {
  /**
   * Error case: Ambiguous or invalid input
   */
  success: false;
  error: {
    code: 'ambiguous-input' | 'invalid-schema' | 'llm-error';
    message: string;
    clarificationNeeded?: string;
    options?: string[];
  };
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

type InterpretIntentResult = InterpretIntentOutput | InterpretIntentError;
```

**Success Example**:
```json
{
  "success": true,
  "data": {
    "classification": "input-control",
    "uiIntent": {
      "primaryPurpose": "collect-rating",
      "visualStyle": "standard",
      "dataBinding": "single-value"
    },
    "behavior": { ... },
    "interaction": { ... },
    "accessibility": { ... }
  },
  "metadata": {
    "model": "gpt-4",
    "tokensUsed": 450,
    "duration": 1200
  }
}
```

**Error Example**:
```json
{
  "success": false,
  "error": {
    "code": "ambiguous-input",
    "message": "Cannot determine if control is for input or display",
    "clarificationNeeded": "Should users be able to change the rating?",
    "options": ["yes-editable", "no-readonly"]
  },
  "metadata": {
    "model": "gpt-4",
    "tokensUsed": 320,
    "duration": 980
  }
}
```

---

### 3.2 GENERATE_SPEC Output

```typescript
interface GenerateSpecOutput {
  success: true;
  data: ComponentSpec;
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

interface GenerateSpecError {
  success: false;
  error: {
    code: 'invalid-schema' | 'capability-violation' | 'llm-error';
    message: string;
    stage?: string;
    suggestion?: string;
  };
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

type GenerateSpecResult = GenerateSpecOutput | GenerateSpecError;
```

**Success Example**:
```json
{
  "success": true,
  "data": {
    "componentId": "star-rating",
    "componentName": "StarRating",
    "namespace": "Contoso",
    "displayName": "Star Rating",
    "capabilities": { ... },
    "properties": [ ... ],
    "resources": { ... }
  },
  "metadata": {
    "model": "gpt-4",
    "tokensUsed": 680,
    "duration": 2100
  }
}
```

---

### 3.3 GENERATE_FILE Output

```typescript
interface GenerateFileOutput {
  success: true;
  data: {
    fileType: 'typescript' | 'css' | 'xml' | 'resx';
    content: string;
    lintingPassed: boolean;
    warnings: string[];
  };
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

interface GenerateFileError {
  success: false;
  error: {
    code: 'linting-failed' | 'invalid-syntax' | 'llm-error';
    message: string;
    lintErrors?: Array<{
      line: number;
      message: string;
    }>;
  };
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

type GenerateFileResult = GenerateFileOutput | GenerateFileError;
```

---

### 3.4 FIX_CODE Output

```typescript
interface FixCodeOutput {
  success: true;
  data: {
    fixedCode: string;
    fixesApplied: Array<{
      line: number;
      originalError: string;
      fix: string;
    }>;
    lintingPassed: boolean;
  };
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

interface FixCodeError {
  success: false;
  error: {
    code: 'unfixable-error' | 'llm-error';
    message: string;
    remainingErrors: Array<{
      line: number;
      message: string;
    }>;
  };
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
  };
}

type FixCodeResult = FixCodeOutput | FixCodeError;
```

---

## 4. Error Handling and Retry Strategy

### 4.1 Error Classification

```typescript
enum LLMErrorType {
  /**
   * Transient errors - retry with backoff
   */
  TIMEOUT = 'timeout',
  RATE_LIMIT = 'rate-limit',
  NETWORK_ERROR = 'network-error',
  SERVICE_UNAVAILABLE = 'service-unavailable',
  
  /**
   * Validation errors - retry with modified prompt
   */
  INVALID_JSON = 'invalid-json',
  SCHEMA_VIOLATION = 'schema-violation',
  
  /**
   * Permanent errors - do not retry
   */
  INVALID_API_KEY = 'invalid-api-key',
  QUOTA_EXCEEDED = 'quota-exceeded',
  CONTENT_FILTER = 'content-filter',
  
  /**
   * Business logic errors - do not retry
   */
  AMBIGUOUS_INPUT = 'ambiguous-input',
  CAPABILITY_VIOLATION = 'capability-violation'
}
```

### 4.2 Retry Strategy

```typescript
interface RetryConfig {
  maxRetries: number;
  backoffStrategy: 'exponential' | 'linear';
  initialDelay: number;
  maxDelay: number;
}

const RETRY_CONFIGS: Record<LLMCallType, Record<LLMErrorType, RetryConfig>> = {
  [LLMCallType.INTERPRET_INTENT]: {
    [LLMErrorType.TIMEOUT]: {
      maxRetries: 3,
      backoffStrategy: 'exponential',
      initialDelay: 1000,
      maxDelay: 8000
    },
    [LLMErrorType.RATE_LIMIT]: {
      maxRetries: 5,
      backoffStrategy: 'exponential',
      initialDelay: 2000,
      maxDelay: 32000
    },
    [LLMErrorType.INVALID_JSON]: {
      maxRetries: 2,
      backoffStrategy: 'linear',
      initialDelay: 1000,
      maxDelay: 1000
    },
    [LLMErrorType.SCHEMA_VIOLATION]: {
      maxRetries: 2,
      backoffStrategy: 'linear',
      initialDelay: 1000,
      maxDelay: 1000
    },
    // Permanent errors: maxRetries = 0
    [LLMErrorType.INVALID_API_KEY]: {
      maxRetries: 0,
      backoffStrategy: 'linear',
      initialDelay: 0,
      maxDelay: 0
    }
  },
  
  [LLMCallType.GENERATE_SPEC]: {
    // Similar config to INTERPRET_INTENT
  },
  
  [LLMCallType.GENERATE_FILE]: {
    // Lower retry counts (file generation is optional)
  },
  
  [LLMCallType.FIX_CODE]: {
    // Max 1 retry (avoid infinite fix loops)
  }
};
```

### 4.3 Retry Implementation

```typescript
async function executeWithRetry<T>(
  callType: LLMCallType,
  input: any,
  execute: () => Promise<T>
): Promise<T> {
  let lastError: Error;
  let attempt = 0;
  
  while (attempt <= getMaxRetries(callType)) {
    try {
      return await execute();
    } catch (error) {
      lastError = error;
      const errorType = classifyError(error);
      const config = RETRY_CONFIGS[callType][errorType];
      
      // Don't retry permanent errors
      if (config.maxRetries === 0) {
        throw error;
      }
      
      // Check if we've exhausted retries
      if (attempt >= config.maxRetries) {
        throw new MaxRetriesExceededError(callType, attempt, lastError);
      }
      
      // Calculate backoff delay
      const delay = calculateDelay(config, attempt);
      
      // Log retry attempt
      logger.warn(`LLM call failed, retrying in ${delay}ms`, {
        callType,
        attempt: attempt + 1,
        maxRetries: config.maxRetries,
        errorType,
        error: error.message
      });
      
      // Wait before retry
      await sleep(delay);
      
      attempt++;
    }
  }
  
  throw lastError;
}

function calculateDelay(config: RetryConfig, attempt: number): number {
  if (config.backoffStrategy === 'exponential') {
    return Math.min(
      config.initialDelay * Math.pow(2, attempt),
      config.maxDelay
    );
  } else {
    return config.initialDelay;
  }
}
```

### 4.4 Error Response Format

```typescript
interface LLMError {
  code: string;
  message: string;
  callType: LLMCallType;
  attempt: number;
  maxRetries: number;
  recoverable: boolean;
  suggestion?: string;
  originalError?: any;
}
```

**Example**:
```json
{
  "code": "LLM_TIMEOUT",
  "message": "LLM call timed out after 30 seconds",
  "callType": "interpret-intent",
  "attempt": 3,
  "maxRetries": 3,
  "recoverable": false,
  "suggestion": "Try again later or check LLM service status"
}
```

---

## 5. Hallucination Prevention by Design

### 5.1 Schema-Based Constraints

**Mechanism**: All JSON outputs must conform to strict JSON schemas

```typescript
/**
 * Validate LLM output against schema
 */
async function validateOutput(
  output: any,
  schema: JSONSchema,
  callType: LLMCallType
): Promise<ValidationResult> {
  const ajv = new Ajv({ strict: true, allErrors: true });
  const validate = ajv.compile(schema);
  
  const valid = validate(output);
  
  if (!valid) {
    throw new SchemaViolationError(
      `LLM output for ${callType} violates schema`,
      validate.errors
    );
  }
  
  return { valid: true, errors: [] };
}
```

**Prevention**:
- ❌ LLM cannot invent new enum values
- ❌ LLM cannot add extra fields
- ❌ LLM cannot skip required fields
- ❌ LLM cannot use wrong data types

---

### 5.2 Enum-Only Values

**Mechanism**: All user-facing values are enums, not free text

```json
{
  "classification": {
    "type": "string",
    "enum": ["input-control", "display-control", "navigation-control"]
  },
  "primaryPurpose": {
    "type": "string",
    "enum": ["collect-rating", "collect-text", "display-value"]
  }
}
```

**Prevention**:
- ❌ LLM cannot invent new component types
- ❌ LLM cannot create custom purposes
- ✅ LLM must choose from predefined options

---

### 5.3 Capability Bounds Enforcement

**Mechanism**: All features and limits come from capability definition

```typescript
/**
 * Validate spec against capability bounds
 */
function validateCapabilityBounds(
  spec: ComponentSpec,
  capability: Capability
): ValidationResult {
  const errors: string[] = [];
  
  // Check features
  for (const feature of spec.capabilities.features) {
    if (!capability.supportedFeatures.includes(feature)) {
      errors.push(`Feature '${feature}' not supported by capability '${capability.capabilityId}'`);
    }
  }
  
  // Check limits
  for (const [key, value] of Object.entries(spec.capabilities.customizations)) {
    const limit = capability.limits[key];
    if (limit && value > limit) {
      errors.push(`Customization '${key}' value ${value} exceeds limit ${limit}`);
    }
  }
  
  // Check forbidden behaviors
  for (const forbidden of capability.forbidden) {
    if (specContainsBehavior(spec, forbidden.behavior)) {
      errors.push(`Forbidden behavior '${forbidden.behavior}': ${forbidden.reason}`);
    }
  }
  
  return {
    valid: errors.length === 0,
    errors
  };
}
```

**Prevention**:
- ❌ LLM cannot add unsupported features
- ❌ LLM cannot exceed capability limits
- ❌ LLM cannot include forbidden behaviors
- ✅ LLM must stay within capability bounds

---

### 5.4 Thin Prompts (Reference, Don't Embed)

**Mechanism**: Prompts reference brain artifacts, never embed logic

**Bad (Embedded Logic)**:
```markdown
Generate a component with these rules:
1. Max 10 properties
2. At least one bound property
3. Property names must be camelCase
4. Data types must be: Whole.None, SingleLine.Text, ...
```

**Good (Referenced Logic)**:
```markdown
Generate a component conforming to:
- Schema: component-spec.schema.json (loaded)
- Rules: pcf-core.rules.md (loaded)
- Capability: {capabilityId}.capability.json (loaded)

Apply all rules mechanically. Do not invent features.
```

**Prevention**:
- ❌ LLM cannot ignore outdated embedded rules
- ✅ LLM always uses latest brain artifacts
- ✅ Rules updated in brain, not in prompts

---

### 5.5 Post-Generation Validation

**Mechanism**: All LLM outputs are validated before acceptance

```typescript
async function executeLLMCall(
  callType: LLMCallType,
  input: any
): Promise<any> {
  // 1. Execute LLM call
  const rawOutput = await llmProvider.execute(input);
  
  // 2. Parse output
  const parsed = parseOutput(rawOutput, callType);
  
  // 3. Validate against schema
  await validateOutput(parsed, input.brainArtifacts.schema, callType);
  
  // 4. Validate against capability (if applicable)
  if (callType === LLMCallType.GENERATE_SPEC) {
    await validateCapabilityBounds(parsed, input.capability);
  }
  
  // 5. Validate against rules (if applicable)
  if (callType === LLMCallType.GENERATE_SPEC) {
    await validateRules(parsed, input.brainArtifacts.rules);
  }
  
  // 6. Return validated output
  return parsed;
}
```

**Prevention**:
- ❌ Invalid outputs are rejected immediately
- ❌ Schema violations trigger retry
- ❌ Capability violations trigger rejection
- ✅ Only validated outputs proceed to next stage

---

### 5.6 Deterministic Configuration

**Mechanism**: Low temperature for consistent outputs

```typescript
const LLM_CONFIG = {
  [LLMCallType.INTERPRET_INTENT]: {
    temperature: 0.1,        // Very low for determinism
    topP: 0.95,
    frequencyPenalty: 0,
    presencePenalty: 0
  },
  
  [LLMCallType.GENERATE_SPEC]: {
    temperature: 0.1,        // Very low for determinism
    topP: 0.95,
    frequencyPenalty: 0,
    presencePenalty: 0
  },
  
  [LLMCallType.GENERATE_FILE]: {
    temperature: 0.2,        // Slightly higher for code variety
    topP: 0.95,
    frequencyPenalty: 0,
    presencePenalty: 0
  },
  
  [LLMCallType.FIX_CODE]: {
    temperature: 0.1,        // Very low for minimal changes
    topP: 0.95,
    frequencyPenalty: 0,
    presencePenalty: 0
  }
};
```

**Prevention**:
- ✅ Same input → Same output (high probability)
- ✅ Minimal creativity, maximum consistency
- ✅ Predictable behavior for testing

---

## 6. LLM Adapter Interface

```typescript
export interface ILLMAdapter {
  /**
   * Execute an LLM call with retry logic
   */
  execute<T>(
    callType: LLMCallType,
    input: any
  ): Promise<LLMResult<T>>;
  
  /**
   * Validate LLM output against schema
   */
  validate(
    output: any,
    schema: JSONSchema
  ): Promise<ValidationResult>;
  
  /**
   * Set LLM provider configuration
   */
  setProvider(
    provider: 'openai' | 'anthropic' | 'azure',
    config: ProviderConfig
  ): void;
  
  /**
   * Get usage statistics
   */
  getStats(): LLMStats;
}

interface LLMResult<T> {
  success: boolean;
  data?: T;
  error?: LLMError;
  metadata: {
    model: string;
    tokensUsed: number;
    duration: number;
    retries: number;
  };
}

interface LLMStats {
  totalCalls: number;
  successRate: number;
  averageTokens: number;
  averageDuration: number;
  retryRate: number;
  errorsByType: Record<LLMErrorType, number>;
}
```

---

## Summary

The LLM integration is **constrained, validated, and deterministic**:

✅ **4 call types** with single, well-defined tasks  
✅ **Strict input/output contracts** for each call type  
✅ **JSON-only or code-only outputs** - no mixed formats  
✅ **Thin, disposable prompts** that reference brain artifacts  
✅ **Exponential backoff retry** for transient errors  
✅ **5-layer hallucination prevention**:
  - Schema validation
  - Enum-only values
  - Capability bounds enforcement
  - Thin prompts (reference, not embed)
  - Post-generation validation
  - Low temperature (0.1-0.2)

**Typical build**: 2 LLM calls, ~1,130 tokens, ~3.3 seconds  
**Hallucination rate**: Near-zero (prevented by design)
