/**
 * Core Type Definitions for PCF Component Builder
 * 
 * This file contains all TypeScript interfaces and types used across the system.
 * These types ensure type safety and serve as contracts between services.
 */

// ============================================================================
// WORKFLOW TYPES
// ============================================================================

export enum WorkflowStage {
  INTENT_INTERPRETATION = 'intent-interpretation',
  CAPABILITY_MATCHING = 'capability-matching',
  SPEC_GENERATION = 'spec-generation',
  RULES_VALIDATION = 'rules-validation',
  FINAL_VALIDATION = 'final-validation',
  CODE_GENERATION = 'code-generation',
  PACKAGING = 'packaging'
}

export enum BuildStatus {
  SUCCESS = 'success',
  REJECTED = 'rejected',
  ERROR = 'error'
}

// ============================================================================
// AI BRAIN TYPES
// ============================================================================

export interface GlobalIntent {
  classification: 'input-control' | 'display-control' | 'data-visualization' | 
                  'navigation-control' | 'media-control' | 'container-control' | 
                  'utility-control';
  uiIntent: {
    primaryPurpose: string;
    visualStyle?: 'minimal' | 'standard' | 'rich' | 'custom';
    dataBinding: 'single-value' | 'multi-value' | 'dataset' | 'none';
  };
  behavior?: {
    interactivity?: 'read-only' | 'editable' | 'interactive' | 'passive';
    validation?: 'required' | 'optional' | 'none';
    persistence?: 'auto-save' | 'manual-save' | 'transient';
  };
  interaction: {
    inputMethod: Array<'click' | 'tap' | 'drag' | 'keyboard' | 'voice' | 'none'>;
    feedback?: Array<'visual-highlight' | 'animation' | 'sound' | 'haptic' | 'none'>;
  };
  accessibility?: {
    wcagLevel?: 'A' | 'AA' | 'AAA';
    keyboardNavigable?: boolean;
    screenReaderSupport?: boolean;
    highContrastMode?: boolean;
  };
  responsiveness?: {
    adaptiveLayout?: boolean;
    minWidth?: number;
    maxWidth?: number;
    aspectRatioLocked?: boolean;
  };
  constraints?: {
    performanceTarget?: 'lightweight' | 'standard' | 'data-intensive';
    offlineCapable?: boolean;
    externalDependencies?: string[];
  };
}

export interface ComponentSpec {
  componentId: string;
  componentName: string;
  namespace: string;
  displayName?: string;
  description?: string;
  capabilities: {
    capabilityId: string;
    features: string[];
    customizations?: Record<string, any>;
  };
  properties: ComponentProperty[];
  resources: {
    code: string;
    css?: string[];
    resx?: string[];
    img?: string[];
  };
  validation?: {
    rulesApplied?: string[];
    warnings?: string[];
    downgrades?: Downgrade[];
  };
}

export interface ComponentProperty {
  name: string;
  displayName: string;
  dataType: PCFDataType;
  usage: 'bound' | 'input' | 'output';
  required?: boolean;
  description?: string;
}

export type PCFDataType = 
  | 'SingleLine.Text'
  | 'Multiple'
  | 'Whole.None'
  | 'Decimal'
  | 'TwoOptions'
  | 'DateAndTime.DateOnly'
  | 'DateAndTime.DateAndTime'
  | 'Currency'
  | 'Lookup.Simple'
  | 'OptionSet'
  | 'MultiSelectOptionSet';

export interface Capability {
  capabilityId: string;
  componentType: string;
  description?: string;
  supportedFeatures: Feature[];
  limits: Record<string, any>;
  forbidden: ForbiddenBehavior[];
  dependencies?: {
    pcfVersion?: string;
    externalLibraries?: ExternalLibrary[];
  };
  examples?: CapabilityExample[];
}

export interface Feature {
  featureId: string;
  name: string;
  description?: string;
  required: boolean;
  configurable?: boolean;
  parameters?: Record<string, any>;
}

export interface ForbiddenBehavior {
  behavior: string;
  reason: string;
  alternative?: string;
}

export interface ExternalLibrary {
  name: string;
  version: string;
  justification?: string;
}

export interface CapabilityExample {
  scenario: string;
  userInput: string;
  expectedIntent: Partial<GlobalIntent>;
}

export interface ValidationRule {
  ruleId: string;
  category: 'pcf-compliance' | 'performance' | 'accessibility' | 'security' | 'best-practice';
  severity: 'error' | 'warning' | 'info';
  condition: RuleCondition;
  action: RuleAction;
  scope?: string[];
  documentation?: {
    rationale?: string;
    references?: string[];
  };
}

export interface RuleCondition {
  type: 'property-check' | 'capability-check' | 'resource-check' | 
        'dependency-check' | 'pattern-match';
  target: string;
  operator: 'equals' | 'not-equals' | 'contains' | 'not-contains' | 
            'greater-than' | 'less-than' | 'matches-regex' | 'exists' | 'not-exists';
  value?: any;
}

export interface RuleAction {
  type: 'reject' | 'downgrade' | 'warn' | 'auto-fix';
  message: string;
  suggestion?: string;
  autoFixValue?: any;
}

export interface Downgrade {
  feature: string;
  reason: string;
  alternative?: string;
  requestedValue?: any;
  appliedValue?: any;
}

// ============================================================================
// BUILD RESULT TYPES
// ============================================================================

export interface BuildResult {
  status: BuildStatus;
  spec?: ComponentSpec;
  zipBuffer?: Buffer;
  errors?: BuildError[];
  warnings?: string[];
  downgrades?: Downgrade[];
  metadata?: BuildMetadata;
}

export interface BuildError {
  stage: WorkflowStage;
  code: string;
  message: string;
  userMessage: string;
  suggestion?: string;
  alternatives?: string[];
  details?: any;
}

export interface BuildMetadata {
  buildId: string;
  timestamp: Date;
  duration: number;
  stagesCompleted: WorkflowStage[];
  llmCalls: number;
  rulesExecuted: number;
}

// ============================================================================
// SERVICE INTERFACE TYPES
// ============================================================================

export interface StageContext {
  stage: WorkflowStage;
  intent?: GlobalIntent;
  capability?: Capability;
  spec?: ComponentSpec;
  config: BuilderConfig;
}

export interface StageResult {
  success: boolean;
  data?: any;
  errors?: BuildError[];
  warnings?: string[];
}

export interface BuilderConfig {
  namespace: string;
  defaultVisualStyle?: string;
  defaultWcagLevel?: string;
  llmProvider: 'openai' | 'anthropic' | 'azure';
  llmModel: string;
  maxRetries?: number;
  timeout?: number;
}

// ============================================================================
// BRAIN ROUTER TYPES
// ============================================================================

export interface BrainArtifacts {
  schemas?: JSONSchema[];
  rules?: ValidationRule[];
  capabilities?: Capability[];
  prompts?: PromptTemplate[];
}

export interface JSONSchema {
  $schema: string;
  $id: string;
  title: string;
  description?: string;
  type: string;
  [key: string]: any;
}

export interface LoadContext {
  stage: WorkflowStage;
  capabilityId?: string;
  ruleCategories?: string[];
}

// ============================================================================
// LLM ADAPTER TYPES
// ============================================================================

export interface PromptTemplate {
  id: string;
  content: string;
  requiredContext: string[];
}

export interface PromptContext {
  userInput?: string;
  intent?: GlobalIntent;
  capability?: Capability;
  schema?: JSONSchema;
  rules?: ValidationRule[];
  [key: string]: any;
}

export interface LLMResult {
  result: any;
  metadata: ExecutionMetadata;
}

export interface ExecutionMetadata {
  model: string;
  tokensUsed: number;
  duration: number;
  retries: number;
}

// ============================================================================
// VALIDATOR TYPES
// ============================================================================

export interface SchemaValidationResult {
  valid: boolean;
  errors: ValidationError[];
}

export interface ValidationError {
  path: string;
  message: string;
  expected?: any;
  actual?: any;
}

export interface RuleValidationResult {
  valid: boolean;
  errors: BuildError[];
  warnings: string[];
  downgrades: Downgrade[];
  rulesExecuted: number;
}

export interface ValidationReport {
  schemaValidation: SchemaValidationResult;
  ruleValidation: RuleValidationResult;
  timestamp: Date;
}

// ============================================================================
// CODE GENERATOR TYPES
// ============================================================================

export interface GeneratedFile {
  path: string;
  content: string;
  type: 'typescript' | 'xml' | 'css' | 'resx' | 'json' | 'markdown';
}

export interface CodeTemplate {
  id: string;
  path: string;
  content: string;
  engine: 'handlebars' | 'ejs';
}

export interface GenerationMetadata {
  templateVersion: string;
  generatedAt: Date;
  filesGenerated: number;
}

// ============================================================================
// PACKAGER TYPES
// ============================================================================

export interface FileTree {
  [path: string]: string | FileTree;
}

export interface PackageJson {
  name: string;
  version: string;
  description: string;
  dependencies: Record<string, string>;
  devDependencies: Record<string, string>;
  scripts: Record<string, string>;
}

export interface PackageManifest {
  componentId: string;
  version: string;
  files: string[];
  size: number;
  checksum: string;
}

// ============================================================================
// AUDIT LOG TYPES
// ============================================================================

export interface AuditLogEntry {
  buildId: string;
  timestamp: Date;
  stage: WorkflowStage;
  action: string;
  data: any;
  userId?: string;
}
