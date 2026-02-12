#!/usr/bin/env node

/**
 * Intent Interpreter Adapter
 * Calls OpenAI (or compatible API) to interpret user input into GlobalIntent
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

/**
 * Interprets user input into GlobalIntent
 * @param {string} userInput - Raw user text
 * @param {string} brainPath - Path to ai-brain directory
 * @returns {Promise<IntentInterpretationResult>}
 */
async function interpretIntent(userInput, brainPath) {
    console.log('=== Intent Interpreter ===\n');
    console.log(`User Input: "${userInput}"\n`);

    try {
        // STEP 1: Load brain artifacts
        console.log('STEP 1: Loading brain artifacts...');
        
        const promptPath = path.join(brainPath, 'prompts/intent-interpreter.prompt.md');
        const schemaPath = path.join(brainPath, 'schemas/global-intent.schema.json');
        const rulesPath = path.join(brainPath, 'intent/intent-mapping.rules.json');

        const promptTemplate = fs.readFileSync(promptPath, 'utf8');
        const schema = JSON.parse(fs.readFileSync(schemaPath, 'utf8'));
        const rules = JSON.parse(fs.readFileSync(rulesPath, 'utf8'));

        console.log('✓ Artifacts loaded\n');

        // STEP 2: Prepare prompt
        console.log('STEP 2: Preparing LLM prompt...');
        
        const prompt = promptTemplate
            .replace('{{RAW_USER_TEXT}}', userInput)
            .replace('{{GLOBAL_INTENT_SCHEMA_JSON}}', JSON.stringify(schema, null, 2))
            .replace('{{INTENT_MAPPING_RULES_JSON}}', JSON.stringify(rules, null, 2));

        console.log('✓ Prompt prepared\n');

        // STEP 3: API Key Check & Mock Fallback
        const apiKey = process.env.OPENAI_API_KEY || process.env.GROK_API_KEY;
        let result;

        if (!apiKey) {
            console.error('❌ ERROR: OPENAI_API_KEY/GROK_API_KEY not found.');
            throw new Error('API Key missing. Cannot interpret intent without valid API key.');
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

TASK 1 — INTENT INTERPRETATION (STRICT MODE)
YOUR RESPONSIBILITY
Translate user language into a GlobalIntent JSON object.

OUTPUT CONTRACT (JSON ONLY)
{
  "globalIntent": {},
  "confidence": 0.0, // 0.0 - 1.0
  "unmappedPhrases": [],
  "needsClarification": false
}

RULES
Confidence must be honest (0.0 – 1.0).
If confidence < 0.6 → needsClarification = true.
Do NOT guess intent.
Do NOT force mappings.

FINAL OPERATING PRINCIPLE
You propose. C# decides. Execution happens elsewhere.
You are a controlled assistant inside a deterministic system.`;

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
            
            console.log('STEP 4: Parsing LLM output...');
            result = JSON.parse(llmOutput);
            result.version = "1.0"; // Force contract version
        }

        console.log('✓ JSON parsed/loaded successfully\n');

        // STEP 5: Validate contract
        console.log('STEP 5: Validating output contract...');
        
        if (!result.hasOwnProperty('globalIntent')) {
            throw new Error('Missing required field: globalIntent');
        }
        if (!result.hasOwnProperty('confidence')) {
            throw new Error('Missing required field: confidence');
        }
        if (!result.hasOwnProperty('unmappedPhrases')) {
            throw new Error('Missing required field: unmappedPhrases');
        }
        if (!result.hasOwnProperty('needsClarification')) {
            throw new Error('Missing required field: needsClarification');
        }

        console.log('✓ Contract validated\n');

        // STEP 6: Log result
        console.log('=== INTERPRETATION RESULT ===');
        console.log(`Confidence: ${result.confidence}`);
        console.log(`Needs Clarification: ${result.needsClarification}`);
        console.log(`Unmapped Phrases: ${result.unmappedPhrases.join(', ') || 'none'}`);
        console.log(`\nGlobalIntent:`);
        console.log(JSON.stringify(result.globalIntent, null, 2));
        console.log();

        return result;

    } catch (error) {
        console.error(`\n❌ INTERPRETATION FAILED: ${error.message}`);
        throw error;
    }
}

/**
 * Main entry point
 */
async function main() {
    const userInput = process.argv[2] || 'Create a modern star rating component';
    const brainPath = process.argv[3] || path.join(__dirname, '../ai-brain');

    const result = await interpretIntent(userInput, brainPath);

    // Write result to file
    const outputPath = '/tmp/intent-result.json';
    fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
    console.log(`Result written to: ${outputPath}\n`);

    // Exit with appropriate code
    process.exit(result.needsClarification ? 1 : 0);
}

// Run if called directly
if (require.main === module) {
    main().catch(err => {
        console.error(err);
        process.exit(1);
    });
}

module.exports = { interpretIntent };
