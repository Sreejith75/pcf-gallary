# Brain Router Specification

## Executive Summary

The Brain Router is a deterministic file loader that maps BrainTask values to the minimal set of required AI Brain artifacts. It enforces context budget limits, caches frequently accessed files, and logs all routing decisions for auditability and testing.

**Core Principle**: Load only what's needed, when it's needed. Never load the full brain.

---

## 1. BrainTask Enum

```typescript
/**
 * BrainTask represents a specific operation requiring brain artifacts.
 * Each task maps to a minimal set of required files.
 */
export enum BrainTask {
  /**
   * Stage 1: Intent Interpretation
   * Convert natural language to GlobalIntent JSON
   */
  INTERPRET_INTENT = 'interpret-intent',
  
  /**
   * Stage 2: Capability Matching
   * Match GlobalIntent to a component capability
   */
  MATCH_CAPABILITY = 'match-capability',
  
  /**
   * Stage 3: Specification Generation
   * Generate ComponentSpec from intent and capability
   */
  GENERATE_SPEC = 'generate-spec',
  
  /**
   * Stage 4: Rules Validation
   * Execute all validation rules against spec
   */
  VALIDATE_RULES = 'validate-rules',
  
  /**
   * Stage 5: Final Validation
   * Cross-reference spec with capability constraints
   */
  VALIDATE_FINAL = 'validate-final',
  
  /**
   * Ad-hoc: Load specific schema by ID
   */
  LOAD_SCHEMA = 'load-schema',
  
  /**
   * Ad-hoc: Load specific capability by ID
   */
  LOAD_CAPABILITY = 'load-capability',
  
  /**
   * Ad-hoc: Load specific prompt template by ID
   */
  LOAD_PROMPT = 'load-prompt'
}
```

---

## 2. Routing Table

### 2.1 Task → Files Mapping

```typescript
/**
 * Routing table mapping BrainTask to required file paths.
 * Paths are relative to ai-brain/ directory.
 */
export const ROUTING_TABLE: Record<BrainTask, string[]> = {
  
  // ========================================================================
  // INTERPRET_INTENT
  // ========================================================================
  [BrainTask.INTERPRET_INTENT]: [
    'schemas/global-intent.schema.json',
    'intent/intent-mapping.rules.json',
    'intent/ambiguity-resolution.rules.json',
    'prompts/intent-interpreter.prompt.md'
  ],
  
  // ========================================================================
  // MATCH_CAPABILITY
  // ========================================================================
  [BrainTask.MATCH_CAPABILITY]: [
    'capabilities/registry.index.json',
    // Note: Specific capability file loaded dynamically based on match
    // e.g., 'capabilities/star-rating.capability.json'
  ],
  
  // ========================================================================
  // GENERATE_SPEC
  // ========================================================================
  [BrainTask.GENERATE_SPEC]: [
    'schemas/component-spec.schema.json',
    'prompts/component-spec-generator.prompt.md',
    // Note: Matched capability file from previous stage
  ],
  
  // ========================================================================
  // VALIDATE_RULES
  // ========================================================================
  [BrainTask.VALIDATE_RULES]: [
    'rules/pcf-core.rules.md',
    'rules/pcf-performance.rules.md',
    'rules/pcf-accessibility.rules.md'
  ],
  
  // ========================================================================
  // VALIDATE_FINAL
  // ========================================================================
  [BrainTask.VALIDATE_FINAL]: [
    'schemas/component-spec.schema.json',
    // Note: Matched capability file from Stage 2
  ],
  
  // ========================================================================
  // LOAD_SCHEMA (Ad-hoc)
  // ========================================================================
  [BrainTask.LOAD_SCHEMA]: [
    // Dynamically determined based on schemaId parameter
    // e.g., 'schemas/{schemaId}.schema.json'
  ],
  
  // ========================================================================
  // LOAD_CAPABILITY (Ad-hoc)
  // ========================================================================
  [BrainTask.LOAD_CAPABILITY]: [
    // Dynamically determined based on capabilityId parameter
    // e.g., 'capabilities/{capabilityId}.capability.json'
  ],
  
  // ========================================================================
  // LOAD_PROMPT (Ad-hoc)
  // ========================================================================
  [BrainTask.LOAD_PROMPT]: [
    // Dynamically determined based on promptId parameter
    // e.g., 'prompts/{promptId}.prompt.md'
  ]
};
```

### 2.2 File Size Estimates

```typescript
/**
 * Estimated file sizes in bytes (for context budget calculation)
 */
export const FILE_SIZE_ESTIMATES: Record<string, number> = {
  // Schemas
  'schemas/global-intent.schema.json': 4500,
  'schemas/component-spec.schema.json': 5200,
  'schemas/capability-registry.schema.json': 4800,
  'schemas/validation-rules.schema.json': 3600,
  
  // Capabilities
  'capabilities/registry.index.json': 1200,
  'capabilities/star-rating.capability.json': 6800,
  
  // Intent
  'intent/intent-mapping.rules.json': 3200,
  'intent/ambiguity-resolution.rules.json': 4100,
  
  // Rules
  'rules/pcf-core.rules.md': 8500,
  'rules/pcf-performance.rules.md': 5600,
  'rules/pcf-accessibility.rules.md': 4900,
  
  // Prompts
  'prompts/intent-interpreter.prompt.md': 3800,
  'prompts/component-spec-generator.prompt.md': 5400,
  
  // Knowledge
  'knowledge/pcf-lifecycle.md': 4200
};
```

### 2.3 Token Estimates

```typescript
/**
 * Estimated token counts (1 token ≈ 4 characters)
 */
export const TOKEN_ESTIMATES: Record<string, number> = {
  // Schemas
  'schemas/global-intent.schema.json': 1125,
  'schemas/component-spec.schema.json': 1300,
  
  // Capabilities
  'capabilities/registry.index.json': 300,
  'capabilities/star-rating.capability.json': 1700,
  
  // Intent
  'intent/intent-mapping.rules.json': 800,
  'intent/ambiguity-resolution.rules.json': 1025,
  
  // Rules
  'rules/pcf-core.rules.md': 2125,
  'rules/pcf-performance.rules.md': 1400,
  'rules/pcf-accessibility.rules.md': 1225,
  
  // Prompts
  'prompts/intent-interpreter.prompt.md': 950,
  'prompts/component-spec-generator.prompt.md': 1350
};
```

---

## 3. Context Budget Strategy

### 3.1 Budget Limits

```typescript
/**
 * Context budget configuration
 */
export const CONTEXT_BUDGET = {
  /**
   * Maximum tokens allowed per task
   * Based on typical LLM context windows (8k-128k)
   */
  MAX_TOKENS_PER_TASK: 5000,
  
  /**
   * Warning threshold (80% of max)
   */
  WARNING_THRESHOLD: 4000,
  
  /**
   * Maximum number of files per task
   */
  MAX_FILES_PER_TASK: 10,
  
  /**
   * Maximum total file size in bytes
   */
  MAX_TOTAL_SIZE: 50000 // ~50KB
};
```

### 3.2 Budget Calculation

```typescript
interface BudgetCalculation {
  task: BrainTask;
  files: string[];
  totalTokens: number;
  totalSize: number;
  withinBudget: boolean;
  warnings: string[];
}

/**
 * Calculate context budget for a task
 */
function calculateBudget(task: BrainTask, files: string[]): BudgetCalculation {
  let totalTokens = 0;
  let totalSize = 0;
  const warnings: string[] = [];
  
  for (const file of files) {
    const tokens = TOKEN_ESTIMATES[file] || 0;
    const size = FILE_SIZE_ESTIMATES[file] || 0;
    
    totalTokens += tokens;
    totalSize += size;
  }
  
  // Check limits
  if (totalTokens > CONTEXT_BUDGET.MAX_TOKENS_PER_TASK) {
    warnings.push(`Token count ${totalTokens} exceeds limit ${CONTEXT_BUDGET.MAX_TOKENS_PER_TASK}`);
  }
  
  if (totalTokens > CONTEXT_BUDGET.WARNING_THRESHOLD) {
    warnings.push(`Token count ${totalTokens} exceeds warning threshold ${CONTEXT_BUDGET.WARNING_THRESHOLD}`);
  }
  
  if (files.length > CONTEXT_BUDGET.MAX_FILES_PER_TASK) {
    warnings.push(`File count ${files.length} exceeds limit ${CONTEXT_BUDGET.MAX_FILES_PER_TASK}`);
  }
  
  if (totalSize > CONTEXT_BUDGET.MAX_TOTAL_SIZE) {
    warnings.push(`Total size ${totalSize} exceeds limit ${CONTEXT_BUDGET.MAX_TOTAL_SIZE}`);
  }
  
  return {
    task,
    files,
    totalTokens,
    totalSize,
    withinBudget: warnings.length === 0,
    warnings
  };
}
```

### 3.3 Budget Enforcement

```typescript
/**
 * Enforce context budget before loading files
 */
function enforceBudget(calculation: BudgetCalculation): void {
  if (!calculation.withinBudget) {
    throw new BudgetExceededError(
      `Context budget exceeded for task ${calculation.task}`,
      calculation.warnings
    );
  }
}
```

---

## 4. Routing Logic

### 4.1 Core Routing Function

```typescript
interface RoutingContext {
  task: BrainTask;
  parameters?: {
    schemaId?: string;
    capabilityId?: string;
    promptId?: string;
  };
}

interface RoutingResult {
  files: string[];
  budget: BudgetCalculation;
  cached: string[];
  toLoad: string[];
}

/**
 * Route a task to required brain files
 */
async function route(context: RoutingContext): Promise<RoutingResult> {
  // 1. Get base files from routing table
  let files = [...ROUTING_TABLE[context.task]];
  
  // 2. Add dynamic files based on parameters
  files = addDynamicFiles(files, context);
  
  // 3. Calculate budget
  const budget = calculateBudget(context.task, files);
  
  // 4. Enforce budget
  enforceBudget(budget);
  
  // 5. Check cache
  const { cached, toLoad } = await checkCache(files);
  
  // 6. Log routing decision
  logRoutingDecision(context, files, budget, cached, toLoad);
  
  return {
    files,
    budget,
    cached,
    toLoad
  };
}
```

### 4.2 Dynamic File Resolution

```typescript
/**
 * Add dynamic files based on task parameters
 */
function addDynamicFiles(
  files: string[],
  context: RoutingContext
): string[] {
  const result = [...files];
  
  switch (context.task) {
    case BrainTask.LOAD_SCHEMA:
      if (!context.parameters?.schemaId) {
        throw new Error('schemaId required for LOAD_SCHEMA task');
      }
      result.push(`schemas/${context.parameters.schemaId}.schema.json`);
      break;
      
    case BrainTask.LOAD_CAPABILITY:
      if (!context.parameters?.capabilityId) {
        throw new Error('capabilityId required for LOAD_CAPABILITY task');
      }
      result.push(`capabilities/${context.parameters.capabilityId}.capability.json`);
      break;
      
    case BrainTask.LOAD_PROMPT:
      if (!context.parameters?.promptId) {
        throw new Error('promptId required for LOAD_PROMPT task');
      }
      result.push(`prompts/${context.parameters.promptId}.prompt.md`);
      break;
      
    case BrainTask.MATCH_CAPABILITY:
      // Capability file added after registry query
      if (context.parameters?.capabilityId) {
        result.push(`capabilities/${context.parameters.capabilityId}.capability.json`);
      }
      break;
      
    case BrainTask.GENERATE_SPEC:
    case BrainTask.VALIDATE_FINAL:
      // Capability file from previous stage
      if (context.parameters?.capabilityId) {
        result.push(`capabilities/${context.parameters.capabilityId}.capability.json`);
      }
      break;
  }
  
  return result;
}
```

---

## 5. Caching Strategy

### 5.1 Cache Configuration

```typescript
export const CACHE_CONFIG = {
  /**
   * Cache backend
   */
  BACKEND: 'redis' as 'redis' | 'memory',
  
  /**
   * Cache key prefix
   */
  KEY_PREFIX: 'brain:',
  
  /**
   * Time-to-live in seconds (1 hour)
   */
  TTL: 3600,
  
  /**
   * Maximum cache size (100MB)
   */
  MAX_SIZE: 100 * 1024 * 1024,
  
  /**
   * Files to always cache (hot files)
   */
  HOT_FILES: [
    'schemas/global-intent.schema.json',
    'schemas/component-spec.schema.json',
    'capabilities/registry.index.json',
    'intent/intent-mapping.rules.json'
  ]
};
```

### 5.2 Cache Operations

```typescript
interface CacheEntry {
  content: string;
  loadedAt: Date;
  size: number;
  hits: number;
}

/**
 * Check which files are cached
 */
async function checkCache(files: string[]): Promise<{
  cached: string[];
  toLoad: string[];
}> {
  const cached: string[] = [];
  const toLoad: string[] = [];
  
  for (const file of files) {
    const key = `${CACHE_CONFIG.KEY_PREFIX}${file}`;
    const exists = await redis.exists(key);
    
    if (exists) {
      cached.push(file);
      // Increment hit counter
      await redis.hincrby(key, 'hits', 1);
    } else {
      toLoad.push(file);
    }
  }
  
  return { cached, toLoad };
}

/**
 * Load file and cache it
 */
async function loadAndCache(file: string): Promise<string> {
  // Read from file system
  const content = await fs.readFile(`ai-brain/${file}`, 'utf-8');
  
  // Cache entry
  const entry: CacheEntry = {
    content,
    loadedAt: new Date(),
    size: content.length,
    hits: 0
  };
  
  // Store in cache
  const key = `${CACHE_CONFIG.KEY_PREFIX}${file}`;
  await redis.setex(
    key,
    CACHE_CONFIG.TTL,
    JSON.stringify(entry)
  );
  
  return content;
}

/**
 * Get file from cache or load
 */
async function getFile(file: string): Promise<string> {
  const key = `${CACHE_CONFIG.KEY_PREFIX}${file}`;
  const cached = await redis.get(key);
  
  if (cached) {
    const entry: CacheEntry = JSON.parse(cached);
    return entry.content;
  }
  
  return loadAndCache(file);
}
```

### 5.3 Cache Warming

```typescript
/**
 * Warm cache with hot files on startup
 */
async function warmCache(): Promise<void> {
  console.log('Warming cache with hot files...');
  
  for (const file of CACHE_CONFIG.HOT_FILES) {
    await loadAndCache(file);
  }
  
  console.log(`Cache warmed with ${CACHE_CONFIG.HOT_FILES.length} files`);
}
```

---

## 6. Routing Decision Logging

### 6.1 Log Entry Structure

```typescript
interface RoutingLogEntry {
  timestamp: Date;
  buildId: string;
  task: BrainTask;
  parameters?: Record<string, any>;
  files: string[];
  budget: {
    totalTokens: number;
    totalSize: number;
    withinBudget: boolean;
    warnings: string[];
  };
  cache: {
    cached: string[];
    toLoad: string[];
    hitRate: number;
  };
  duration: number; // ms
}
```

### 6.2 Logging Function

```typescript
/**
 * Log routing decision for auditability
 */
function logRoutingDecision(
  context: RoutingContext,
  files: string[],
  budget: BudgetCalculation,
  cached: string[],
  toLoad: string[]
): void {
  const entry: RoutingLogEntry = {
    timestamp: new Date(),
    buildId: context.buildId || 'unknown',
    task: context.task,
    parameters: context.parameters,
    files,
    budget: {
      totalTokens: budget.totalTokens,
      totalSize: budget.totalSize,
      withinBudget: budget.withinBudget,
      warnings: budget.warnings
    },
    cache: {
      cached,
      toLoad,
      hitRate: cached.length / files.length
    },
    duration: 0 // Set after loading
  };
  
  // Write to audit log
  auditLogger.info('routing-decision', entry);
  
  // Emit metrics
  metrics.increment('brain_router.routing_decisions', {
    task: context.task
  });
  
  metrics.gauge('brain_router.files_loaded', files.length, {
    task: context.task
  });
  
  metrics.gauge('brain_router.cache_hit_rate', entry.cache.hitRate, {
    task: context.task
  });
}
```

### 6.3 Log Output Example

```json
{
  "timestamp": "2026-02-06T15:27:15Z",
  "buildId": "abc123",
  "task": "interpret-intent",
  "parameters": {},
  "files": [
    "schemas/global-intent.schema.json",
    "intent/intent-mapping.rules.json",
    "intent/ambiguity-resolution.rules.json",
    "prompts/intent-interpreter.prompt.md"
  ],
  "budget": {
    "totalTokens": 3900,
    "totalSize": 15600,
    "withinBudget": true,
    "warnings": []
  },
  "cache": {
    "cached": [
      "schemas/global-intent.schema.json",
      "intent/intent-mapping.rules.json"
    ],
    "toLoad": [
      "intent/ambiguity-resolution.rules.json",
      "prompts/intent-interpreter.prompt.md"
    ],
    "hitRate": 0.5
  },
  "duration": 45
}
```

---

## 7. Testing Strategy

### 7.1 Test Cases

```typescript
describe('BrainRouter', () => {
  
  // ========================================================================
  // ROUTING TABLE TESTS
  // ========================================================================
  
  describe('Routing Table', () => {
    it('should have entries for all BrainTask values', () => {
      const tasks = Object.values(BrainTask);
      for (const task of tasks) {
        expect(ROUTING_TABLE[task]).toBeDefined();
      }
    });
    
    it('should map INTERPRET_INTENT to correct files', () => {
      const files = ROUTING_TABLE[BrainTask.INTERPRET_INTENT];
      expect(files).toEqual([
        'schemas/global-intent.schema.json',
        'intent/intent-mapping.rules.json',
        'intent/ambiguity-resolution.rules.json',
        'prompts/intent-interpreter.prompt.md'
      ]);
    });
    
    it('should map VALIDATE_RULES to all rule files', () => {
      const files = ROUTING_TABLE[BrainTask.VALIDATE_RULES];
      expect(files).toContain('rules/pcf-core.rules.md');
      expect(files).toContain('rules/pcf-performance.rules.md');
      expect(files).toContain('rules/pcf-accessibility.rules.md');
    });
  });
  
  // ========================================================================
  // BUDGET CALCULATION TESTS
  // ========================================================================
  
  describe('Budget Calculation', () => {
    it('should calculate budget for INTERPRET_INTENT', () => {
      const files = ROUTING_TABLE[BrainTask.INTERPRET_INTENT];
      const budget = calculateBudget(BrainTask.INTERPRET_INTENT, files);
      
      expect(budget.totalTokens).toBe(3900);
      expect(budget.withinBudget).toBe(true);
      expect(budget.warnings).toHaveLength(0);
    });
    
    it('should warn when approaching token limit', () => {
      const files = ROUTING_TABLE[BrainTask.VALIDATE_RULES];
      const budget = calculateBudget(BrainTask.VALIDATE_RULES, files);
      
      expect(budget.totalTokens).toBe(4750);
      expect(budget.warnings).toContain(
        expect.stringContaining('exceeds warning threshold')
      );
    });
    
    it('should reject when exceeding token limit', () => {
      const files = [
        ...ROUTING_TABLE[BrainTask.INTERPRET_INTENT],
        ...ROUTING_TABLE[BrainTask.VALIDATE_RULES]
      ];
      
      expect(() => {
        const budget = calculateBudget(BrainTask.INTERPRET_INTENT, files);
        enforceBudget(budget);
      }).toThrow(BudgetExceededError);
    });
  });
  
  // ========================================================================
  // DYNAMIC FILE RESOLUTION TESTS
  // ========================================================================
  
  describe('Dynamic File Resolution', () => {
    it('should add schema file for LOAD_SCHEMA task', () => {
      const context: RoutingContext = {
        task: BrainTask.LOAD_SCHEMA,
        parameters: { schemaId: 'global-intent' }
      };
      
      const files = addDynamicFiles([], context);
      expect(files).toContain('schemas/global-intent.schema.json');
    });
    
    it('should add capability file for LOAD_CAPABILITY task', () => {
      const context: RoutingContext = {
        task: BrainTask.LOAD_CAPABILITY,
        parameters: { capabilityId: 'star-rating' }
      };
      
      const files = addDynamicFiles([], context);
      expect(files).toContain('capabilities/star-rating.capability.json');
    });
    
    it('should throw error if required parameter missing', () => {
      const context: RoutingContext = {
        task: BrainTask.LOAD_SCHEMA,
        parameters: {}
      };
      
      expect(() => addDynamicFiles([], context)).toThrow(
        'schemaId required for LOAD_SCHEMA task'
      );
    });
  });
  
  // ========================================================================
  // DETERMINISM TESTS
  // ========================================================================
  
  describe('Determinism', () => {
    it('should return same files for same task', async () => {
      const context: RoutingContext = {
        task: BrainTask.INTERPRET_INTENT
      };
      
      const result1 = await route(context);
      const result2 = await route(context);
      
      expect(result1.files).toEqual(result2.files);
    });
    
    it('should return same budget for same task', async () => {
      const context: RoutingContext = {
        task: BrainTask.INTERPRET_INTENT
      };
      
      const result1 = await route(context);
      const result2 = await route(context);
      
      expect(result1.budget.totalTokens).toBe(result2.budget.totalTokens);
    });
  });
  
  // ========================================================================
  // CACHE TESTS
  // ========================================================================
  
  describe('Caching', () => {
    it('should cache loaded files', async () => {
      const file = 'schemas/global-intent.schema.json';
      
      // First load
      await loadAndCache(file);
      
      // Check cache
      const key = `${CACHE_CONFIG.KEY_PREFIX}${file}`;
      const cached = await redis.get(key);
      
      expect(cached).toBeDefined();
    });
    
    it('should return cached content on second load', async () => {
      const file = 'schemas/global-intent.schema.json';
      
      // First load
      const content1 = await getFile(file);
      
      // Second load (from cache)
      const content2 = await getFile(file);
      
      expect(content1).toBe(content2);
    });
    
    it('should warm cache with hot files', async () => {
      await warmCache();
      
      for (const file of CACHE_CONFIG.HOT_FILES) {
        const key = `${CACHE_CONFIG.KEY_PREFIX}${file}`;
        const cached = await redis.exists(key);
        expect(cached).toBe(1);
      }
    });
  });
  
  // ========================================================================
  // LOGGING TESTS
  // ========================================================================
  
  describe('Logging', () => {
    it('should log routing decision', async () => {
      const context: RoutingContext = {
        task: BrainTask.INTERPRET_INTENT,
        buildId: 'test-123'
      };
      
      const spy = jest.spyOn(auditLogger, 'info');
      
      await route(context);
      
      expect(spy).toHaveBeenCalledWith(
        'routing-decision',
        expect.objectContaining({
          buildId: 'test-123',
          task: BrainTask.INTERPRET_INTENT
        })
      );
    });
  });
});
```

### 7.2 Integration Tests

```typescript
describe('BrainRouter Integration', () => {
  it('should load correct files for full workflow', async () => {
    // Stage 1: Intent Interpretation
    const stage1 = await route({
      task: BrainTask.INTERPRET_INTENT
    });
    expect(stage1.files).toHaveLength(4);
    
    // Stage 2: Capability Matching
    const stage2 = await route({
      task: BrainTask.MATCH_CAPABILITY,
      parameters: { capabilityId: 'star-rating' }
    });
    expect(stage2.files).toContain('capabilities/star-rating.capability.json');
    
    // Stage 3: Spec Generation
    const stage3 = await route({
      task: BrainTask.GENERATE_SPEC,
      parameters: { capabilityId: 'star-rating' }
    });
    expect(stage3.files).toContain('schemas/component-spec.schema.json');
    
    // Stage 4: Rules Validation
    const stage4 = await route({
      task: BrainTask.VALIDATE_RULES
    });
    expect(stage4.files).toHaveLength(3);
    
    // Stage 5: Final Validation
    const stage5 = await route({
      task: BrainTask.VALIDATE_FINAL,
      parameters: { capabilityId: 'star-rating' }
    });
    expect(stage5.files).toHaveLength(2);
  });
  
  it('should never exceed context budget in any stage', async () => {
    const tasks = [
      BrainTask.INTERPRET_INTENT,
      BrainTask.MATCH_CAPABILITY,
      BrainTask.GENERATE_SPEC,
      BrainTask.VALIDATE_RULES,
      BrainTask.VALIDATE_FINAL
    ];
    
    for (const task of tasks) {
      const result = await route({ task });
      expect(result.budget.withinBudget).toBe(true);
    }
  });
});
```

---

## 8. Implementation Interface

```typescript
/**
 * Brain Router Service Interface
 */
export interface IBrainRouter {
  /**
   * Route a task to required brain files
   */
  route(context: RoutingContext): Promise<RoutingResult>;
  
  /**
   * Load files for a task
   */
  loadForTask(task: BrainTask, parameters?: Record<string, any>): Promise<BrainArtifacts>;
  
  /**
   * Get a specific file
   */
  getFile(path: string): Promise<string>;
  
  /**
   * Clear cache
   */
  clearCache(): Promise<void>;
  
  /**
   * Warm cache with hot files
   */
  warmCache(): Promise<void>;
  
  /**
   * Get routing statistics
   */
  getStats(): RoutingStats;
}

interface BrainArtifacts {
  schemas: JSONSchema[];
  rules: ValidationRule[];
  capabilities: Capability[];
  prompts: PromptTemplate[];
  raw: Record<string, string>; // file path → content
}

interface RoutingStats {
  totalRequests: number;
  cacheHitRate: number;
  averageTokens: number;
  averageFiles: number;
  budgetViolations: number;
}
```

---

## Summary

The Brain Router is a **deterministic, minimal-loading, cacheable** file loader that:

✅ Maps **8 BrainTask values** to specific file sets  
✅ Enforces **context budget** (max 5000 tokens per task)  
✅ **Never loads full brain** - only required files  
✅ **Caches frequently accessed files** in Redis  
✅ **Logs all routing decisions** for auditability  
✅ **100% deterministic** - same task → same files  
✅ **Fully testable** - comprehensive test suite  

**Token Usage by Task**:
- INTERPRET_INTENT: 3,900 tokens (4 files)
- MATCH_CAPABILITY: 2,000 tokens (2 files)
- GENERATE_SPEC: 3,350 tokens (3 files)
- VALIDATE_RULES: 4,750 tokens (3 files)
- VALIDATE_FINAL: 3,000 tokens (2 files)

**All tasks stay within 5,000 token budget.**
