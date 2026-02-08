#!/usr/bin/env node

/**
 * Spec Generator Adapter
 * Calls OpenAI (or compatible API) to generate ComponentSpec from GlobalIntent + Capability
 * NO HALLUCINATION - Strict schema validation
 */

const fs = require('fs');
const path = require('path');

// OpenAI SDK
const OpenAI = require('openai');

const openai = new OpenAI({
    apiKey: process.env.OPENAI_API_KEY || process.env.GROK_API_KEY || 'mock-key-for-testing',
    baseURL: process.env.OPENAI_BASE_URL || 'https://api.x.ai/v1' // Default to xAI if not set
});

const SUPPORTED_VERSION = '1.0';

/**
 * Generates ComponentSpec from Intent and Capability
 * @param {object} inputJson - Input object containing intent and capability
 * @param {string} brainPath - Path to ai-brain directory
 * @returns {Promise<object>} Generated ComponentSpec
 */
async function generateSpec(inputJson, brainPath) {
    console.log('=== Spec Generator ===\n');

    try {
        const { globalIntent, capability } = inputJson;

        if (!globalIntent || !capability) {
            throw new Error('Input must contain globalIntent and capability');
        }

        console.log(`Intent Classification: ${globalIntent.classification}`);
        console.log(`Target Capability: ${capability.capabilityId}\n`);

        // STEP 1: Load brain artifacts
        console.log('STEP 1: Loading brain artifacts...');
        
        const promptPath = path.join(brainPath, 'prompts/spec-generator.prompt.md');
        const schemaPath = path.join(brainPath, 'schemas/component-spec.schema.json');

        const promptTemplate = fs.readFileSync(promptPath, 'utf8');
        let schema; 
        try {
             schema = JSON.parse(fs.readFileSync(schemaPath, 'utf8'));
        } catch(e) {
             console.warn('Schema file not found or invalid, proceeding without injection into prompt (LLM validation still applies)');
             schema = { "note": "Schema validation enforced by C# layer" };
        }

        console.log('✓ Artifacts loaded\n');

        // STEP 2: Prepare prompt
        console.log('STEP 2: Preparing LLM prompt...');
        
        const prompt = promptTemplate
            .replace('{{GLOBAL_INTENT_JSON}}', JSON.stringify(globalIntent, null, 2))
            .replace('{{COMPONENT_CAPABILITY_JSON}}', JSON.stringify(capability, null, 2))
            .replace('{{COMPONENT_SPEC_SCHEMA_JSON}}', JSON.stringify(schema, null, 2));

        console.log('✓ Prompt prepared\n');

        // STEP 3: API Key Check & Mock Fallback
        const apiKey = process.env.OPENAI_API_KEY || process.env.GROK_API_KEY;
        let result;

        if (!apiKey) {
            console.warn('⚠️ WARNING: OPENAI_API_KEY not found. Using MOCK response for testing.');
            
            // Mock Spec
            result = {
                version: "1.0",
                componentType: "star-rating",
                componentName: "StarRating",
                namespace: "Contoso",
                displayName: "Star Rating",
                description: "A modern star rating component with hover effects.",
                capabilities: {
                    capabilityId: "star-rating",
                    features: ["rating-display", "click-to-rate", "hover-effect"],
                    customizations: { color: "#FFD700" }
                },
                properties: [
                    { name: "value", displayName: "Value", dataType: "Whole.None", usage: "bound", required: true, description: "The current rating value." },
                    { name: "maxRating", displayName: "Max Rating", dataType: "Whole.None", usage: "input", required: false, description: "Maximum number of stars." }
                ],
                resources: {
                    code: "index.ts",
                    css: ["css/StarRating.css"],
                    resx: ["strings/StarRating.resx"]
                },
                validation: {
                    rulesApplied: ["pcf-naming", "accessibility-check"],
                    warnings: [],
                    downgrades: []
                }
            };
            console.log('✓ Mock response prepared');
        } else {
            console.log('STEP 3: Calling OpenAI (Model: grok-4-fast)...');
            
            const systemPrompt = `ROLE
You are Grok AI (grok-4-fast) operating inside a strictly governed enterprise pipeline.
You are NOT an autonomous agent.
You act only as a bounded transformation engine under C# authority.

SYSTEM CONTEXT
This system is a PCF Component Builder. C# is the sole authority. Node.js executes but never decides.
All AI output is treated as untrusted and validated against schemas.

GLOBAL SAFETY RULES
❌ Do NOT invent fields
❌ Do NOT invent enums
❌ Do NOT invent capabilities
❌ Do NOT invent defaults not allowed by schema
❌ Do NOT generate explanations
❌ Do NOT generate comments
❌ Do NOT reference system internals
If information is missing → choose minimal safe defaults or signal low confidence.

TASK 2 — COMPONENT SPEC GENERATION (STRICT MODE)
YOUR RESPONSIBILITY
Generate a ComponentSpec JSON that:
Fully conforms to schema. Respects capability constraints. Uses minimal safe defaults.

OUTPUT CONTRACT (JSON ONLY)
{
  "version": "1.0.0",
  "componentType": "",
  "displayName": "",
  "description": "",
  "properties": [
    {
      "name": "exampleProperty",
      "displayName": "Example Property",
      "dataType": "SingleLine.Text",
      "usage": "bound",
      "required": true,
      "description": "Example description"
    }
  ],
  "events": [],
  "visual": {},
  "interaction": {},
  "accessibility": {},
  "responsiveness": {}
}

RULES
Ignore unsupported intent silently.
Do NOT add extra fields.
Do NOT explain choices.
Do NOT infer future behavior.

VERSIONING RULE
All outputs must include: "version": "1.0". If version mismatches → output will be rejected.

FAILURE AWARENESS
Your output may be rejected. Rejection is expected behavior. Correctness > Completion.

FINAL OPERATING PRINCIPLE
You propose. C# decides. Execution happens elsewhere.`;

            const response = await openai.chat.completions.create({
                model: 'grok-4-fast',
                messages: [
                    {
                        role: 'system',
                        content: systemPrompt
                    },
                    {
                        role: 'user',
                        content: prompt
                    }
                ],
                temperature: 0.2, // Determinism
                response_format: { type: 'json_object' }
            });

            const llmOutput = response.choices[0].message.content;
            console.log('✓ LLM response received\n');
            console.log('RAW LLM OUTPUT:', llmOutput); // DEBUG LOG
            
            console.log('STEP 4: Parsing LLM output...');
            result = JSON.parse(llmOutput);
        }

        console.log('✓ JSON parsed/loaded successfully\n');

        // STEP 5: Validate contract (version check)
        console.log('STEP 5: Validating output contract...');
        
        if (!result.version || result.version !== '1.0') {
             console.error('FAILED RESULT:', JSON.stringify(result, null, 2));
             throw new Error(`Invalid version: ${result.version || 'MISSING'}. Expected 1.0`);
        }
        if (!result.componentType) {
             console.error('FAILED RESULT:', JSON.stringify(result, null, 2));
             throw new Error('Missing componentType');
        }

        console.log('✓ Contract validated\n');

        // STEP 6: Log result
        console.log('=== GENERATION RESULT ===');
        console.log(JSON.stringify(result, null, 2));
        console.log();

        return result;

    } catch (error) {
        console.error(`\n❌ GENERATION FAILED: ${error.message}`);
        throw error;
    }
}

/**
 * Main entry point
 */
async function main() {
    // Input is expected to be a file path containing JSON with { globalIntent, capability }
    const inputFilePath = process.argv[2];
    const brainPath = process.argv[3] || path.join(__dirname, '../ai-brain');

    if (!inputFilePath) {
        console.error('Usage: node spec-generator.js <input-json-file> [brain-path]');
        process.exit(1);
    }

    if (!fs.existsSync(inputFilePath)) {
        console.error(`Input file not found: ${inputFilePath}`);
        process.exit(1);
    }

    const inputJson = JSON.parse(fs.readFileSync(inputFilePath, 'utf8'));
    const result = await generateSpec(inputJson, brainPath);

    // Write result to file
    const outputPath = '/tmp/spec-result.json';
    fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
    console.log(`Result written to: ${outputPath}\n`);
}

// Run if called directly
if (require.main === module) {
    main().catch(err => {
        console.error(err);
        process.exit(1);
    });
}

module.exports = { generateSpec };
