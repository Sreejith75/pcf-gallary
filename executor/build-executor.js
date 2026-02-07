#!/usr/bin/env node

/**
 * Build Executor
 * Executes PCF CLI commands and creates ZIP package.
 * NO AI - Pure execution.
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

/**
 * Executes a shell command in the specified directory.
 */
function runCommand(command, cwd) {
    console.log(`> ${command}`);
    try {
        execSync(command, { cwd, stdio: 'inherit', encoding: 'utf8' });
    } catch (error) {
        throw new Error(`Command failed: ${command}`);
    }
}

/**
 * Main build function.
 * @param {string} workingDir - Directory containing generated files
 * @param {string} componentName - Name for artifact naming
 */
/**
 * Main build function.
 * @param {string} workingDir - Directory containing generated files
 * @param {string} componentName - Name for artifact naming
 */
function buildPcf(workingDir, componentName) {
    console.log('=== PCF Build Executor (Solution Aware) ===\n');
    console.log(`Directory: ${workingDir}`);
    console.log(`Component: ${componentName}\n`);

    if (!fs.existsSync(workingDir)) {
        throw new Error(`Working directory not found: ${workingDir}`);
    }

    try {
        // STEP 0: Metadata Extraction
        // We need Namespace and Name to generate the .pcfproj correctly using 'pac pcf init'
        const manifestPath = path.join(workingDir, 'ControlManifest.Input.xml');
        if (!fs.existsSync(manifestPath)) {
            throw new Error(`ControlManifest.Input.xml not found at ${manifestPath}`);
        }
        
        const manifestContent = fs.readFileSync(manifestPath, 'utf8');
        // Simple regex to extract namespace and constructor
        const nsMatch = manifestContent.match(/namespace="([^"]+)"/);
        const nameMatch = manifestContent.match(/constructor="([^"]+)"/);
        
        const namespace = nsMatch ? nsMatch[1] : 'AppWeaver';
        const controlName = nameMatch ? nameMatch[1] : componentName;

        console.log(`Detected Namespace: ${namespace}`);
        console.log(`Detected Control: ${controlName}`);

        // STEP 1: Generate .pcfproj (Missing link for Solution)
        // file-generator.js creates files but NOT the .pcfproj required for 'pac solution add-reference'.
        // We run 'pac pcf init' in a temp folder and copy the project file over.
        console.log('\nSTEP 1: Generating .pcfproj...');
        
        const tempInitDir = path.join(workingDir, '_temp_init');
        if (fs.existsSync(tempInitDir)) {
             fs.rmSync(tempInitDir, { recursive: true, force: true });
        }
        fs.mkdirSync(tempInitDir);
        
        // Run init in temp
        runCommand(`pac pcf init --namespace ${namespace} --name ${controlName} --template field`, tempInitDir);
        
        // Find and copy .pcfproj
        const files = fs.readdirSync(tempInitDir);
        const projFile = files.find(f => f.endsWith('.pcfproj'));
        
        if (projFile) {
            fs.copyFileSync(path.join(tempInitDir, projFile), path.join(workingDir, projFile));
            console.log(`✓ Copied project file: ${projFile}`);
        } else {
            console.warn('⚠️ No .pcfproj found in temp init. Solution build might fail.');
        }

        // Cleanup temp
        fs.rmSync(tempInitDir, { recursive: true, force: true });

        // STEP 2: NPM Build (Control Build)
        console.log('\nSTEP 2: Building Control (NPM)...');
        runCommand('npm install', workingDir);
        runCommand('npm run build', workingDir);

        // STEP 3: Solution Packaging
        console.log('\nSTEP 3: Packaging Solution...');
        
        // PAC CLI (v1.x/v2.x) uses the folder name as the solution name by default.
        // We must name the folder exactly what we want the solution to be.
        const solutionName = `${controlName}_Solution`;
        const solutionDir = path.join(workingDir, solutionName);
        
        if (fs.existsSync(solutionDir)) {
            fs.rmSync(solutionDir, { recursive: true, force: true });
        }
        fs.mkdirSync(solutionDir);

        const publisherName = 'Bytestrone';
        const publisherPrefix = 'bts';

        // a. Init Solution
        // Removed --solution-name as it causes "unknown argument" error in some PAC versions.
        runCommand(`pac solution init --publisher-name ${publisherName} --publisher-prefix ${publisherPrefix}`, solutionDir);

        // b. Add Reference to Control
        // The control is in the parent directory of the solution folder
        runCommand(`pac solution add-reference --path ..`, solutionDir);

        // c. Build Solution (Generates ZIP)
        // 'dotnet build' in the solution directory produces the Managed/Unmanaged zip
        runCommand('dotnet restore', solutionDir);
        runCommand('dotnet build', solutionDir);


        // STEP 4: Extract Artifact
        console.log('\nSTEP 4: Finalizing Artifact...');
        
        // Expected location: output/bin/Debug/{SolutionName}.zip
        const binDebug = path.join(solutionDir, 'bin', 'Debug');
        
        if (!fs.existsSync(binDebug)) {
             throw new Error(`Solution build output directory not found: ${binDebug}`);
        }

        // Find the zip
        const solutionZips = fs.readdirSync(binDebug).filter(f => f.endsWith('.zip'));
        if (solutionZips.length === 0) {
            throw new Error('No Solution ZIP found in build output');
        }
        
        const sourceZip = path.join(binDebug, solutionZips[0]);
        
        // Target: {componentName}_{buildId}.zip
        // buildId corresponds to the folder name usually, or passed context.
        // We will respect the previous logic for naming.
        const buildId = path.basename(workingDir); 
        const targetZipName = `${componentName}_${buildId}.zip`;
        const finalZipPath = path.join(workingDir, targetZipName);
        
        fs.copyFileSync(sourceZip, finalZipPath);
        console.log(`✓ Artifact ready: ${finalZipPath}`);

        console.log(JSON.stringify({
            step: "PCFBuild",
            status: "Success",
            zipPath: finalZipPath,
            solutionZip: sourceZip
        }));

    } catch (error) {
        console.error(`\n❌ BUILD FAILED: ${error.message}`);
        console.error(error.stack);
        process.exit(1);
    }
}

/**
 * Main entry point
 */
function main() {
    const workingDir = process.argv[2];
    const componentName = process.argv[3] || 'Component';

    if (!workingDir) {
        console.error('Usage: node build-executor.js <working-dir> [component-name]');
        process.exit(1);
    }

    buildPcf(workingDir, componentName);
}

if (require.main === module) {
    main();
}

module.exports = { buildPcf };
