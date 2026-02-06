#!/usr/bin/env node

/**
 * Node.js Executor for PCF Component Builder
 * Receives ComponentExecutionPlan from C# and generates PCF component
 * NO AI/LLM CALLS - Pure file generation and PCF CLI execution
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Contract version
const SUPPORTED_VERSION = '1.0';

async function main() {
    console.log('=== Node.js Executor (No AI) ===\n');

    try {
        // STEP 1: Load execution plan
        const planPath = process.argv[2] || '/tmp/pcf-test/plan.json';
        console.log(`STEP 1: Loading execution plan from ${planPath}...`);

        if (!fs.existsSync(planPath)) {
            throw new Error(`Plan file not found: ${planPath}`);
        }

        const planJson = fs.readFileSync(planPath, 'utf8');
        const plan = JSON.parse(planJson);

        console.log(`✓ Plan loaded`);
        console.log(`  Version: ${plan.version}`);
        console.log(`  BuildId: ${plan.buildId}`);
        console.log(`  Component: ${plan.componentSpec.componentName}\n`);

        // STEP 2: Validate contract version
        console.log('STEP 2: Validating contract version...');
        
        if (plan.version !== SUPPORTED_VERSION) {
            throw new Error(`Unsupported contract version: ${plan.version} (expected: ${SUPPORTED_VERSION})`);
        }

        console.log(`✓ Contract version validated\n`);

        // STEP 3: Create output directory
        const outputDir = `/tmp/pcf-output/${plan.buildId}`;
        console.log(`STEP 3: Creating output directory: ${outputDir}...`);
        
        if (fs.existsSync(outputDir)) {
            fs.rmSync(outputDir, { recursive: true });
        }
        fs.mkdirSync(outputDir, { recursive: true });

        console.log(`✓ Output directory created\n`);

        // STEP 4: Generate files
        console.log('STEP 4: Generating files...');
        
        for (const fileStep of plan.filesToGenerate) {
            console.log(`  [${fileStep.step}/${plan.filesToGenerate.length}] ${fileStep.fileName}...`);
            
            const filePath = path.join(outputDir, fileStep.outputPath);
            const fileDir = path.dirname(filePath);
            
            if (!fs.existsSync(fileDir)) {
                fs.mkdirSync(fileDir, { recursive: true });
            }

            // Generate file content (hardcoded for now)
            const content = generateFileContent(fileStep, plan.componentSpec);
            fs.writeFileSync(filePath, content, 'utf8');
            
            console.log(`    ✓ Created: ${filePath}`);
        }

        console.log(`✓ All files generated\n`);

        // STEP 5: Run PCF CLI
        console.log('STEP 5: Running PCF CLI...');
        console.log('  Note: Skipping PCF CLI for now (requires pac pcf installation)\n');

        // STEP 6: Create ZIP
        console.log('STEP 6: Creating ZIP package...');
        
        const zipName = `${plan.componentSpec.componentName}_${plan.buildId}.zip`;
        const zipPath = `/tmp/pcf-output/${zipName}`;

        // Simple ZIP creation (requires zip command)
        try {
            execSync(`cd ${outputDir} && zip -r ${zipPath} .`, { stdio: 'inherit' });
            console.log(`✓ ZIP created: ${zipPath}\n`);
        } catch (err) {
            console.log(`  Note: ZIP creation skipped (zip command not available)\n`);
        }

        // STEP 7: Summary
        console.log('=== EXECUTION SUMMARY ===');
        console.log(`✓ Plan validated`);
        console.log(`✓ ${plan.filesToGenerate.length} files generated`);
        console.log(`✓ Output directory: ${outputDir}`);
        console.log(`\nResult: SUCCESS (No AI used)\n`);

        return 0;
    } catch (error) {
        console.error(`\n❌ EXECUTION FAILED: ${error.message}`);
        console.error(error.stack);
        return 1;
    }
}

function generateFileContent(fileStep, spec) {
    const fileName = fileStep.fileName;

    // Generate appropriate content based on file type
    if (fileName === 'ControlManifest.Input.xml') {
        return generateManifest(spec);
    } else if (fileName === 'package.json') {
        return generatePackageJson(spec);
    } else if (fileName === 'tsconfig.json') {
        return generateTsConfig();
    } else if (fileName === 'index.ts') {
        return generateIndexTs(spec);
    } else if (fileName.endsWith('.css')) {
        return generateCss(spec);
    } else if (fileName.endsWith('.resx')) {
        return generateResx(spec);
    } else if (fileName === 'README.md') {
        return generateReadme(spec);
    } else if (fileName === '.gitignore') {
        return generateGitignore();
    }

    return `// Generated file: ${fileName}`;
}

function generateManifest(spec) {
    return `<?xml version="1.0" encoding="utf-8" ?>
<manifest>
  <control namespace="${spec.namespace}" constructor="${spec.componentName}" version="1.0.0" display-name-key="${spec.componentName}_Display_Key" description-key="${spec.componentName}_Desc_Key" control-type="standard">
    ${spec.properties.map(prop => `
    <property name="${prop.name}" display-name-key="${prop.name}_Display_Key" description-key="${prop.name}_Desc_Key" of-type="${prop.dataType}" usage="${prop.usage}" required="${prop.required}" />
    `).join('')}
    <resources>
      <code path="${spec.resources.code}" order="1"/>
      ${spec.resources.css.map(css => `<css path="${css}" order="1" />`).join('\n      ')}
      ${spec.resources.resx.map(resx => `<resx path="${resx}" version="1.0.0" />`).join('\n      ')}
    </resources>
  </control>
</manifest>`;
}

function generatePackageJson(spec) {
    return JSON.stringify({
        name: `pcf-${spec.componentId}`,
        version: "1.0.0",
        description: spec.description,
        scripts: {
            build: "pcf-scripts build",
            clean: "pcf-scripts clean",
            rebuild: "pcf-scripts rebuild",
            start: "pcf-scripts start"
        },
        dependencies: {},
        devDependencies: {
            "@types/node": "^18.0.0",
            "@types/powerapps-component-framework": "^1.3.0",
            "pcf-scripts": "^1.0.0",
            "pcf-start": "^1.0.0"
        }
    }, null, 2);
}

function generateTsConfig() {
    return JSON.stringify({
        compilerOptions: {
            module: "ESNext",
            target: "ES6",
            moduleResolution: "node",
            strict: true,
            esModuleInterop: true,
            skipLibCheck: true,
            forceConsistentCasingInFileNames: true
        },
        include: ["index.ts"]
    }, null, 2);
}

function generateIndexTs(spec) {
    return `import { IInputs, IOutputs } from "./generated/ManifestTypes";

export class ${spec.componentName} implements ComponentFramework.StandardControl<IInputs, IOutputs> {
    private _container: HTMLDivElement;
    private _value: number;

    constructor() {
        this._value = 0;
    }

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this._container = container;
        this.renderStars();
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        this._value = context.parameters.value.raw || 0;
        this.renderStars();
    }

    public getOutputs(): IOutputs {
        return {
            value: this._value
        };
    }

    public destroy(): void {
        // Cleanup
    }

    private renderStars(): void {
        this._container.innerHTML = \`
            <div class="star-rating">
                <span>★★★★★</span>
                <div>Rating: \${this._value}</div>
            </div>
        \`;
    }
}`;
}

function generateCss(spec) {
    return `.star-rating {
    font-size: 24px;
    color: #FFD700;
}

.star-rating span {
    cursor: pointer;
}`;
}

function generateResx(spec) {
    return `<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="${spec.componentName}_Display_Key" xml:space="preserve">
    <value>${spec.displayName}</value>
  </data>
  <data name="${spec.componentName}_Desc_Key" xml:space="preserve">
    <value>${spec.description}</value>
  </data>
</root>`;
}

function generateReadme(spec) {
    return `# ${spec.displayName}

${spec.description}

## Properties

${spec.properties.map(p => `- **${p.displayName}**: ${p.description}`).join('\n')}

## Build

\`\`\`bash
npm install
npm run build
\`\`\`
`;
}

function generateGitignore() {
    return `node_modules/
out/
generated/
*.log
.DS_Store`;
}

// Run
main().then(code => process.exit(code));
