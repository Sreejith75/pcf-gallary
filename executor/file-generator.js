#!/usr/bin/env node

/**
 * File Generator Executor
 * Renders Handlebars templates based on deterministic plan.
 * NO AI - Pure mechanical transformation.
 */

const fs = require('fs');
const path = require('path');
// Note: You must install handlebars: npm install handlebars
const Handlebars = require('handlebars');

/**
 * Generates files based on the provided plan and spec.
 * @param {object} input - { version, componentSpec, fileGenerationPlan }
 * @param {string} outputDir - Target directory for generation
 * @param {string} templatesDir - Directory containing .hbs templates
 */
function generateFiles(input, outputDir, templatesDir) {
    console.log('=== File Generator (Deterministic) ===\n');

    try {
        const { version, componentSpec, fileGenerationPlan } = input;

        // 1. Validation
        if (!componentSpec || !fileGenerationPlan) {
            throw new Error('Missing componentSpec or fileGenerationPlan');
        }
        if (!fileGenerationPlan.steps || !Array.isArray(fileGenerationPlan.steps)) {
            throw new Error('Invalid fileGenerationPlan: steps array missing');
        }

        console.log(`Component: ${componentSpec.componentName} (${componentSpec.componentType})`);
        console.log(`Plan: ${fileGenerationPlan.steps.length} files to generate`);
        console.log(`Output: ${outputDir}\n`);

        // Ensure output dir exists
        if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir, { recursive: true });
        }

        // 2. Execute Steps
        for (const step of fileGenerationPlan.steps) {
            const templateName = step.templateName;
            const outputRelPath = step.outputPath;
            
            console.log(`[${step.order}/${fileGenerationPlan.steps.length}] Generating ${outputRelPath}...`);

            // a. Load Template
            const templatePath = path.join(templatesDir, templateName);
            if (!fs.existsSync(templatePath)) {
                throw new Error(`Template not found: ${templatePath}`);
            }
            const templateSource = fs.readFileSync(templatePath, 'utf8');

            // b. Compile Template
            const template = Handlebars.compile(templateSource);

            // c. Render (Context = ComponentSpec)
            const renderedContent = template(componentSpec);

            // d. Write File
            const outputPath = path.join(outputDir, outputRelPath);
            const outputDirName = path.dirname(outputPath);
            
            if (!fs.existsSync(outputDirName)) {
                fs.mkdirSync(outputDirName, { recursive: true });
            }

            fs.writeFileSync(outputPath, renderedContent);

            // e. Log Strict
            console.log(JSON.stringify({
                step: "GenerateFile",
                template: templateName,
                output: outputRelPath,
                status: "Success"
            }));
        }

        console.log('\n✓ File generation complete.\n');
        
    } catch (error) {
        console.error(`\n❌ GENERATION FAILED: ${error.message}`);
        process.exit(1);
    }
}

/**
 * Main entry point
 */
function main() {
    const inputFilePath = process.argv[2];
    const outputDir = process.argv[3];
    const templatesDir = process.argv[4] || path.join(__dirname, '../ai-brain/templates');

    if (!inputFilePath || !outputDir) {
        console.error('Usage: node file-generator.js <input-json-file> <output-dir> [templates-dir]');
        process.exit(1);
    }

    if (!fs.existsSync(inputFilePath)) {
        console.error(`Input file not found: ${inputFilePath}`);
        process.exit(1);
    }

    const inputJson = JSON.parse(fs.readFileSync(inputFilePath, 'utf8'));
    generateFiles(inputJson, outputDir, templatesDir);
}

// Run if called directly
if (require.main === module) {
    main();
}

module.exports = { generateFiles };
