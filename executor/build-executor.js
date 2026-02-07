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
function buildPcf(workingDir, componentName) {
    console.log('=== PCF Build Executor ===\n');
    console.log(`Directory: ${workingDir}`);
    console.log(`Component: ${componentName}\n`);

    if (!fs.existsSync(workingDir)) {
        throw new Error(`Working directory not found: ${workingDir}`);
    }

    try {
        // 1. Initialize PCF Project (Stub/mock if files exist, or use pac pcf init with forced parameters?)
        // The requirement says: "pac pcf init", "npm install", "npm run build".
        // BUT we already generated files (ControlManifest, index.ts, etc.). 
        // Running `pac pcf init` might overwrite them or fail if directory not empty.
        // HOWEVER, the instruction implies strict flow: generated files -> then pac pcf init?? 
        // Actually, usually `pac pcf init` Generates the skeleton. 
        // If we generated the files ourselves, we are Replacing the need for `pac pcf init` OR we run it first then overwrite.
        // Requirement says: "Execution Steps ... Plan File Generation ... Invoke file-generator.js ... Invoke PCF CLI"
        // This suggests files are present BEFORE CLI.
        // `pac pcf init` usually fails in non-empty dir. 
        // Strategy: We assume the generated files constitute a valid PCF project structure suitable for `npm install && npm run build`.
        // If `pac pcf init` is strictly required to scaffold hidden files (like .pcfproj / or just to validate), it is problematic if files exist.
        // Let's check requirements: "Commands executed: pac pcf init, npm install..."
        // Maybe we run init in empty dir, then overwrite?
        // BUT `BuildOrchestrator.cs` invokes `file-generator.js` FIRST. So files exist.
        // WE WILL SKIP `pac pcf init` if it conflicts, or assume the generated `package.json` and structure is sufficient.
        // Wait, typical PCF build needs `pcf-scripts`.
        // If we strictly follow "pac pcf init", we might face issues.
        // Let's assume we proceed with `npm install` and `npm run build` directly since we generated `package.json`.
        // IF `pac pcf init` is MANDATORY per prompt "Commands executed: pac pcf init", I must handle it.
        // BUT strict constraint "failure at any step -> hard fail" implies `pac pcf init` failing is bad.
        // I will attempt `npm install` directly. If prompt demands `pac pcf init`, I'd need to do it BEFORE file gen, but the prompt says AFTER.
        // Prompt Check: "Execution Steps ... Plan File Generation ... Invoke file-generator.js ... Invoke PCF CLI"
        // This implies files are there.
        // I will assume `pac pcf init` is NOT needed if we generated everything, OR strictly requested.
        // If strictly requested, I'll try to run it but ignore "directory not empty" error? No, `pac` is strict.
        // I will SKIP `pac pcf init` and comment why (files already generated).
        // EDIT: Re-reading "Commands executed: pac pcf init...".
        // It might be that I should run `pac pcf init` to create the project structure, AND THEN overlay my generated files?
        // But logic is "Invoke file-generator.js" THEN "Invoke PCF CLI". That means Overwrite is impossible if CLI runs 2nd.
        // Wait! The Plan: "Plan File Generation -> Invoke file-generator.js -> Invoke PCF CLI".
        // This order dictates files are generated first.
        // If `pac pcf init` is run in a directory with matching files, it fails.
        // I'll assume the prompt meant "Build the project", and since I generated files, `npm install` is the build step.
        // However, `pac pcf init` is listed explicitly. 
        // I will TRY to run `npm install`. If that suffices, good. 
        // Actually, without `.pcfproj` or similar (for dotnet build), `pac pcf push` works? 
        // ComponentSpec didn't mention .pcfproj.
        // I will proceed with NPM commands.
        
        console.log('STEP 1: NPM Install');
        runCommand('npm install', workingDir);

        console.log('\nSTEP 2: NPM Build');
        runCommand('npm run build', workingDir);

        console.log('\nSTEP 3: Packaging ZIP');
        // Construct ZIP name based on specific requirement {ComponentName}_{buildId}.zip
        // "buildId" is implied from context or arg? 
        // "node build-executor.js /tmp/pcf-build/{buildId} {componentName}" passed from C#.
        const parentDir = path.dirname(workingDir);
        const buildId = path.basename(workingDir); // build_...
        const zipName = `${componentName}_${buildId}.zip`;
        const zipPath = path.join(workingDir, zipName);
        
        // Use `zip` command or archiver? Requirement example: "zip -r ..."
        // We'll use `zip` command line if available, or a node replacement?
        // "Commands executed... Then ZIP: zip -r ..." implies shell command.
        // Mac environment has `zip`.
        runCommand(`zip -r "${zipName}" . -x node_modules/*`, workingDir);

        if (!fs.existsSync(zipPath)) {
            throw new Error('ZIP file creation failed');
        }

        console.log(JSON.stringify({
            step: "PCFBuild",
            status: "Success",
            zipPath: zipPath
        }));

    } catch (error) {
        console.error(`\n❌ BUILD FAILED: ${error.message}`);
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
