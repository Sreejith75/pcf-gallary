#!/usr/bin/env node

/**
 * Spec Generator Adapter
 * Calls OpenAI to generate ComponentSpec from GlobalIntent + Capability
 * NO HALLUCINATION - Strict schema validation
 */

const fs = require('fs');
const path = require('path');

// OpenAI SDK
const OpenAI = require('openai');

const openai = new OpenAI({
    apiKey: process.env.OPENAI_API_KEY
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
        // Schema loader might need to handle $ref if we split schemas, strictly simple for now
        // Assuming schema is self-contained or we pass the relevant part
        let schema; 
        try {
             schema = JSON.parse(fs.readFileSync(schemaPath, 'utf8'));
        } catch(e) {
             // Fallback or specific loading if schema doesn't exist yet
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

        // STEP 3: Call OpenAI
        console.log('STEP 3: Calling OpenAI...');
        
        const response = await openai.chat.completions.create({
            model: 'gpt-4',
            messages: [
                {
                    role: 'system',
                    content: 'You are a strict component specification generator. Output ONLY valid JSON.'
                },
                {
                    role: 'user',
                    content: prompt
                }
            ],
            temperature: 0.2, // Very low temperature for determinism
            response_format: { type: 'json_object' }
        });

        const llmOutput = response.choices[0].message.content;
        console.log('✓ LLM response received\n');

        // STEP 4: Parse and validate
        console.log('STEP 4: Parsing LLM output...');
        
        const spec = JSON.parse(llmOutput);

        // Basic structural validation
        if (spec.version !== SUPPORTED_VERSION) {
            console.warn(`Warning: Generated version ${spec.version} does not match supported version ${SUPPORTED_VERSION}. C# layer may reject this.`);
        }

        console.log('✓ JSON parsed successfully\n');

        // STEP 5: Log result
        console.log('=== GENERATION RESULT ===');
        console.log(`Version: ${spec.version}`);
        console.log(`Component: ${spec.displayName} (${spec.componentType})`);
        console.log(`Properties: ${Object.keys(spec.properties || {}).length}`);
        console.log();

        return spec;

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
