/**
 * Service Contract Interfaces
 * 
 * These interfaces define the contracts between services in the PCF Component Builder.
 * All services must implement these interfaces to ensure compatibility.
 */

import {
    WorkflowStage,
    BuildResult,
    BuilderConfig,
    StageContext,
    StageResult,
    BrainArtifacts,
    LoadContext,
    JSONSchema,
    Capability,
    ValidationRule,
    PromptTemplate,
    PromptContext,
    LLMResult,
    SchemaValidationResult,
    RuleValidationResult,
    ValidationReport,
    ComponentSpec,
    GeneratedFile,
    GenerationMetadata,
    FileTree,
    PackageManifest
} from './types';

// ============================================================================
// ORCHESTRATOR SERVICE CONTRACT
// ============================================================================

export interface IOrchestrator {
    /**
     * Build a PCF component from user prompt
     * @param userPrompt Natural language description
     * @param config Builder configuration
     * @returns Build result with spec or error
     */
    buildComponent(userPrompt: string, config: BuilderConfig): Promise<BuildResult>;

    /**
     * Execute a specific workflow stage
     * @param stage Workflow stage to execute
     * @param context Stage context with required data
     * @returns Stage result
     */
    executeStage(stage: WorkflowStage, context: StageContext): Promise<StageResult>;

    /**
     * Handle downgrade scenario
     * @param downgrade Downgrade information
     */
    handleDowngrade(downgrade: any): void;

    /**
     * Handle rejection scenario
     * @param rejection Rejection information
     * @returns Rejection response
     */
    handleRejection(rejection: any): any;
}

// ============================================================================
// BRAIN ROUTER SERVICE CONTRACT
// ============================================================================

export interface IBrainRouter {
    /**
     * Load brain artifacts for a specific workflow stage
     * @param stage Workflow stage
     * @returns Brain artifacts required for the stage
     */
    loadForStage(stage: WorkflowStage): Promise<BrainArtifacts>;

    /**
     * Load brain artifacts with custom context
     * @param context Load context specifying what to load
     * @returns Requested brain artifacts
     */
    loadWithContext(context: LoadContext): Promise<BrainArtifacts>;

    /**
     * Get a specific schema by ID
     * @param schemaId Schema identifier
     * @returns JSON Schema
     */
    getSchema(schemaId: string): Promise<JSONSchema>;

    /**
     * Get a specific capability by ID
     * @param capabilityId Capability identifier
     * @returns Capability definition
     */
    getCapability(capabilityId: string): Promise<Capability>;

    /**
     * Get rules by category
     * @param category Rule category
     * @returns Array of validation rules
     */
    getRules(category: string): Promise<ValidationRule[]>;

    /**
     * Get all rules
     * @returns Array of all validation rules
     */
    getAllRules(): Promise<ValidationRule[]>;

    /**
     * Get a prompt template by ID
     * @param promptId Prompt identifier
     * @returns Prompt template
     */
    getPromptTemplate(promptId: string): Promise<PromptTemplate>;

    /**
     * Clear cache (for testing or updates)
     */
    clearCache(): void;
}

// ============================================================================
// LLM ADAPTER SERVICE CONTRACT
// ============================================================================

export interface ILLMAdapter {
    /**
     * Execute a prompt with context
     * @param template Prompt template
     * @param context Prompt context data
     * @param schema Expected output schema
     * @returns LLM result
     */
    executePrompt(
        template: PromptTemplate,
        context: PromptContext,
        schema: JSONSchema
    ): Promise<LLMResult>;

    /**
     * Validate LLM response against schema
     * @param response LLM response string
     * @param schema Expected schema
     * @returns Validation result
     */
    validateResponse(response: string, schema: JSONSchema): SchemaValidationResult;

    /**
     * Retry operation with exponential backoff
     * @param operation Operation to retry
     * @param maxRetries Maximum retry attempts
     * @returns Operation result
     */
    retryWithBackoff<T>(operation: () => Promise<T>, maxRetries?: number): Promise<T>;

    /**
     * Set LLM provider configuration
     * @param provider Provider name
     * @param config Provider-specific configuration
     */
    setProvider(provider: string, config: any): void;
}

// ============================================================================
// VALIDATOR SERVICE CONTRACT
// ============================================================================

export interface IValidator {
    /**
     * Validate data against JSON schema
     * @param data Data to validate
     * @param schema JSON Schema
     * @returns Schema validation result
     */
    validateSchema(data: unknown, schema: JSONSchema): SchemaValidationResult;

    /**
     * Execute validation rules against component spec
     * @param spec Component specification
     * @param rules Array of validation rules
     * @returns Rule validation result
     */
    executeRules(spec: ComponentSpec, rules: ValidationRule[]): Promise<RuleValidationResult>;

    /**
     * Apply downgrade to component spec
     * @param spec Component specification
     * @param downgrade Downgrade rule
     * @returns Updated component spec
     */
    applyDowngrade(spec: ComponentSpec, downgrade: any): ComponentSpec;

    /**
     * Generate validation report
     * @param results Array of validation results
     * @returns Validation report
     */
    generateReport(results: any[]): ValidationReport;

    /**
     * Cross-reference spec with capability
     * @param spec Component specification
     * @param capability Capability definition
     * @returns Validation result
     */
    crossReference(spec: ComponentSpec, capability: Capability): SchemaValidationResult;
}

// ============================================================================
// CODE GENERATOR SERVICE CONTRACT
// ============================================================================

export interface ICodeGenerator {
    /**
     * Generate PCF component implementation
     * @param spec Component specification
     * @param capability Capability definition
     * @returns Generated files
     */
    generate(spec: ComponentSpec, capability: Capability): Promise<{
        files: GeneratedFile[];
        metadata: GenerationMetadata;
    }>;

    /**
     * Generate TypeScript implementation
     * @param spec Component specification
     * @returns TypeScript file
     */
    generateImplementation(spec: ComponentSpec): Promise<GeneratedFile>;

    /**
     * Generate PCF manifest XML
     * @param spec Component specification
     * @returns Manifest file
     */
    generateManifest(spec: ComponentSpec): Promise<GeneratedFile>;

    /**
     * Generate CSS styles
     * @param spec Component specification
     * @returns CSS file
     */
    generateStyles(spec: ComponentSpec): Promise<GeneratedFile>;

    /**
     * Generate localization resources
     * @param spec Component specification
     * @returns RESX files
     */
    generateResources(spec: ComponentSpec): Promise<GeneratedFile[]>;

    /**
     * Apply template with data
     * @param template Code template
     * @param spec Component specification
     * @returns Rendered content
     */
    applyTemplate(template: any, spec: ComponentSpec): string;
}

// ============================================================================
// PACKAGER SERVICE CONTRACT
// ============================================================================

export interface IPackager {
    /**
     * Create ZIP package from generated files
     * @param files Generated files
     * @param spec Component specification
     * @returns ZIP buffer and manifest
     */
    createZip(files: GeneratedFile[], spec: ComponentSpec): Promise<{
        zipBuffer: Buffer;
        manifest: PackageManifest;
    }>;

    /**
     * Create package folder structure
     * @param files Generated files
     * @returns File tree
     */
    createPackageStructure(files: GeneratedFile[]): FileTree;

    /**
     * Generate package.json
     * @param spec Component specification
     * @returns package.json content
     */
    generatePackageJson(spec: ComponentSpec): any;

    /**
     * Bundle resources
     * @param files Generated files
     * @returns Bundled resources
     */
    bundleResources(files: GeneratedFile[]): any;

    /**
     * Validate package structure
     * @param zipBuffer ZIP buffer
     * @returns Validation result
     */
    validatePackage(zipBuffer: Buffer): SchemaValidationResult;
}

// ============================================================================
// AUDIT LOGGER SERVICE CONTRACT
// ============================================================================

export interface IAuditLogger {
    /**
     * Log build event
     * @param buildId Build identifier
     * @param stage Workflow stage
     * @param action Action performed
     * @param data Event data
     */
    log(buildId: string, stage: WorkflowStage, action: string, data: any): Promise<void>;

    /**
     * Get audit trail for build
     * @param buildId Build identifier
     * @returns Array of audit log entries
     */
    getAuditTrail(buildId: string): Promise<any[]>;

    /**
     * Clear old audit logs
     * @param olderThan Date threshold
     */
    cleanup(olderThan: Date): Promise<void>;
}
