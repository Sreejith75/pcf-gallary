#!/usr/bin/env node

/**
 * Intent Interpreter Adapter
 * Calls OpenAI to interpret user input into GlobalIntent
 * NO HALLUCINATION - Strict schema validation
 */

const fs = require('fs');
const path = require('path');

// OpenAI SDK (install: npm install openai)
const OpenAI = require('openai');

const openai = new OpenAI({
    apiKey: process.env.OPENAI_API_KEY
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

        // STEP 3: Call OpenAI
        console.log('STEP 3: Calling OpenAI...');
        
        const response = await openai.chat.completions.create({
            model: 'gpt-4',
            messages: [
                {
                    role: 'system',
                    content: 'You are an intent interpreter. Output ONLY valid JSON. No explanations.'
                },
                {
                    role: 'user',
                    content: prompt
                }
            ],
            temperature: 0.3, // Low temperature for consistency
            response_format: { type: 'json_object' }
        });

        const llmOutput = response.choices[0].message.content;
        console.log('✓ LLM response received\n');

        // STEP 4: Parse and validate
        console.log('STEP 4: Parsing LLM output...');
        
        const result = JSON.parse(llmOutput);

        console.log('✓ JSON parsed successfully\n');

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
