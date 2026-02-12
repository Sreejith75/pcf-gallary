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

        // STEP 1: Generate .pcfproj (Native in workingDir)
        console.log('\nSTEP 1: Generating .pcfproj...');
        
        // We move existing files to a backup, run init, then restore.
        // This ensures 'pac pcf init' succeeds (needs empty dir) and creates a native .pcfproj.
        const backupDir = path.join(workingDir, '_backup_files');
        if (!fs.existsSync(backupDir)) fs.mkdirSync(backupDir);
        
        const filesToBackup = fs.readdirSync(workingDir).filter(f => f !== '_backup_files' && f !== 'node_modules');
        for (const file of filesToBackup) {
            fs.renameSync(path.join(workingDir, file), path.join(backupDir, file));
        }
        
        try {
            // Run init in the now "empty" (except node_modules) workingDir
            runCommand(`pac pcf init --namespace ${namespace} --name ${controlName} --template field`, workingDir);
            
            // Native init created a .pcfproj named after the directory (usually).
            // We want it to be named after the control for clarity and consistency.
            const allProjFiles = fs.readdirSync(workingDir).filter(f => f.endsWith('.pcfproj'));
            const targetProject = `${controlName}.pcfproj`;
            
            // Assume the one file created is the one we want to rename, unless it's already correct
            if (allProjFiles.length > 0) {
                const generatedProj = allProjFiles[0];
                if (generatedProj !== targetProject) {
                    fs.renameSync(path.join(workingDir, generatedProj), path.join(workingDir, targetProject));
                    console.log(`✓ Renamed project file: ${generatedProj} -> ${targetProject}`);
                } else {
                    console.log(`✓ Project file name is already correct: ${targetProject}`);
                }
            } else {
                console.error('❌ No .pcfproj file found after init!');
            }

            // REMOVE default 'strings' folder created by pac init
            const stringsDir = path.join(workingDir, 'strings');
            if (fs.existsSync(stringsDir)) {
                 console.log('- Removing default strings directory to prevent build errors');
                 fs.rmSync(stringsDir, { recursive: true, force: true });
            }

            // CREATE Directory.Build.props to enforce PcfBuildOutDir
            // We force the output into a specific subfolder matching the control name.
            // This ensures out/controls/{ControlName} structure, preventing flattened output issues.
            const propsContent = `<Project>
  <PropertyGroup>
    <PcfBuildOutDir>$(MSBuildThisFileDirectory)out/controls/${controlName}</PcfBuildOutDir>
  </PropertyGroup>
</Project>`;
            fs.writeFileSync(path.join(workingDir, 'Directory.Build.props'), propsContent);
            console.log('✓ Created Directory.Build.props to enforce nested output directory');

            // Removed Directory.Build.targets creation - relying on .cdsproj patching instead

            // Restore backed up files with intelligent merge for package.json
            const backedUpFiles = fs.readdirSync(backupDir);
            for (const file of backedUpFiles) {
                const targetPath = path.join(workingDir, file);
                const backupPath = path.join(backupDir, file);
                
                if (file === 'package.json') {
                    console.log('--- Merging package.json ---');
                    const aiPkg = JSON.parse(fs.readFileSync(backupPath, 'utf8'));
                    const pacPkg = JSON.parse(fs.readFileSync(targetPath, 'utf8'));
                    
                    // Merge deps: PAC (authority) + AI (additions)
                    const merged = {
                        ...aiPkg,
                        dependencies: { ...pacPkg.dependencies, ...aiPkg.dependencies },
                        devDependencies: { ...pacPkg.devDependencies, ...aiPkg.devDependencies },
                        
                        // CRITICAL: Force 'build' script to be CLEAN and use MSBuild source.
                        // This ensures Directory.Build.props is respected.
                        scripts: {
                            ...aiPkg.scripts,
                            build: "pcf-scripts build --buildSource MSBuild",
                            clean: "pcf-scripts clean",
                            rebuild: "pcf-scripts clean && pcf-scripts build --buildSource MSBuild"
                        }
                    };
                    
                    fs.writeFileSync(targetPath, JSON.stringify(merged, null, 2));
                    console.log('✓ package.json merged (scripts sanitized to use MSBuild)');
                    continue;
                }

                if (fs.existsSync(targetPath)) {
                    // Overwrite files created by pac init (index.ts, ControlManifest, etc.)
                    const stat = fs.statSync(backupPath);
                    if (stat.isFile()) {
                        fs.copyFileSync(backupPath, targetPath);
                    }
                } else {
                    fs.renameSync(backupPath, targetPath);
                }
            }
            console.log('✓ Managed to overlay generated files onto native PCF project.');
        } finally {
            // Cleanup backup
            if (fs.existsSync(backupDir)) {
                fs.rmSync(backupDir, { recursive: true, force: true });
            }
        }

        // STEP 2: Building Control (NPM)
        console.log('\nSTEP 2: Building Control (NPM)...');
        runCommand('npm install', workingDir);
        
        console.log('\n--- Building PCF Control ---');
        // We use npm run build which maps to 'pcf-scripts build --buildSource MSBuild'
        // This forces pcf-scripts to read Directory.Build.props for the output path.
        runCommand('npm run build', workingDir);

        // Validate Output IMMEDIATELY after build
        console.log('\n--- Validating PCF Output ---');

        // Recursive search for PCF control root (Matches Azure DevOps PCF pipeline logic)
        // Looks for a folder containing ControlManifest.xml AND bundle.js (root OR css/)
        // Starts at workingDir to catch variations like bin/Debug/out or StarRating/out
        function findPcfControlRoots(rootDir) {
            const results = [];
            const visited = new Set();

            function walk(dir, depth) {
                if (depth > 5) return; // safe upper bound for nested build outputs
                
                // Prevent cycles or traversing into node_modules (performance)
                if (visited.has(dir)) return;
                visited.add(dir);
                if (path.basename(dir) === 'node_modules') return;

                let entries;
                try {
                    entries = fs.readdirSync(dir, { withFileTypes: true });
                } catch (e) {
                    return;
                }

                // Check strict PCF invariant: Manifest + bundle.js (root or css/)
                const manifest = path.join(dir, 'ControlManifest.xml');
                const bundleAtRoot = path.join(dir, 'bundle.js');
                const bundleInCss = path.join(dir, 'css', 'bundle.js');

                // Check manifest first
                if (fs.existsSync(manifest)) {
                    // Check bundle location
                    if (fs.existsSync(bundleAtRoot)) {
                         results.push({ path: dir, bundle: bundleAtRoot });
                    } else if (fs.existsSync(bundleInCss)) {
                         results.push({ path: dir, bundle: bundleInCss });
                    }
                }

                for (const entry of entries) {
                    if (entry.isDirectory()) {
                         const fullPath = path.join(dir, entry.name);
                         walk(fullPath, depth + 1);
                    }
                }
            }

            walk(rootDir, 0);
            return results;
        }

        const validControls = findPcfControlRoots(workingDir);

        if (validControls.length !== 1) {
          throw new Error(
            `PCF build invalid. Expected exactly 1 control root under '${workingDir}' (depth<=5).\n` +
            `Searched for ControlManifest.xml + bundle.js (root or css/).\n` +
            `Found: ${JSON.stringify(validControls, null, 2)}`
          );
        }

        const pcfControl = validControls[0];
        console.log(`✓ PCF control validated at: ${pcfControl.path}`);
        console.log(`✓ Bundle used: ${pcfControl.bundle}`);
        console.log('✓ Output invariant satisfied.');

        // FIX: Normalize flattened PCF output
        // MSBuild fails if out/controls/ contains files directly (flattened structure).
        // It expects out/controls/<ControlName>/ControlManifest.xml.
        // If we detect a flattened output (Manifest directly in controls/), we wrap it.
        function normalizePcfOutput(controlName) {
            const controlsDir = path.join(workingDir, 'out', 'controls');
            // Namespace is usually Bytestrone.{ControlName} or similar, but folder name
            // just needs to be unique. MSBuild uses the folder name.
            // We'll use the control name to be safe.
            const controlFolderName = `Bytestrone.${controlName}`;
            const controlRoot = path.join(controlsDir, controlFolderName);

            const manifestAtRoot = path.join(controlsDir, 'ControlManifest.xml');

            if (fs.existsSync(manifestAtRoot)) {
                console.log("⚠️ Flattened PCF output detected. Normalizing into proper control folder...");

                if (!fs.existsSync(controlRoot)) {
                    fs.mkdirSync(controlRoot, { recursive: true });
                }

                const entries = fs.readdirSync(controlsDir);
                for (const entry of entries) {
                    // Don't move the destination folder into itself
                    if (entry === controlFolderName) continue;

                    const src = path.join(controlsDir, entry);
                    const dest = path.join(controlRoot, entry);
                    
                    // Move everything! bundle.js, css/, strings/, ControlManifest.xml
                    try {
                        fs.renameSync(src, dest);
                    } catch (e) {
                         console.warn(`! Failed to move '${entry}': ${e.message}`);
                    }
                }
                console.log(`✅ Normalized artifacts into ${controlFolderName}/`);
            }
        }
        
        // Apply Normalization
        normalizePcfOutput(componentName);

        // STEP 3: Solution Packaging
        console.log('\nSTEP 3: Packaging Solution...');
        
        // PAC CLI (v1.x/v2.x) uses the folder name as the solution name by default.
        // We must name the folder exactly what we want the solution to be.
        const solutionName = `${controlName}_Solution`;
        // FIX: Create solution folder BESIDE the project folder (sibling), not inside.
        const solutionDir = path.resolve(workingDir, '..', solutionName);
        
        if (fs.existsSync(solutionDir)) {
            fs.rmSync(solutionDir, { recursive: true, force: true });
        }
        fs.mkdirSync(solutionDir);

        const publisherName = namespace;
        // Prefix must be 2-8 chars, alphanumeric
        let publisherPrefix = namespace.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
        if (publisherPrefix.length > 8) publisherPrefix = publisherPrefix.substring(0, 8);
        if (publisherPrefix.length < 2) publisherPrefix = 'comp'; // fallback

        // a. Init Solution
        runCommand(`pac solution init --publisher-name ${publisherName} --publisher-prefix ${publisherPrefix}`, solutionDir);

        // b. Add Reference to Control
        // Since solution is sibling, path to control is just the folder name of workingDir
        const controlDirName = path.basename(workingDir);
        runCommand(`pac solution add-reference --path ../${controlDirName}`, solutionDir);

        // c.0 CRITICAL FIX: Patch .pcfproj to DISABLE PCF BUILD (Double Safety)
        // This ensures that when the solution build triggers the project, it does NOTHING.
        console.log('--- Patching PCF Project (.pcfproj) to DISABLE build ---');
        const pcfProjFiles = fs.readdirSync(workingDir).filter(f => f.endsWith('.pcfproj'));
        if (pcfProjFiles.length > 0) {
            const pcfProjPath = path.join(workingDir, pcfProjFiles[0]);
            let pcfProjContent = fs.readFileSync(pcfProjPath, 'utf8');
            
            // 1. Inject flags into the first PropertyGroup
            const flagInjection = `
    <SkipPCFBuild>true</SkipPCFBuild>
    <PCFBuild>false</PCFBuild>
  `;
            if (pcfProjContent.includes('<PropertyGroup>')) {
                pcfProjContent = pcfProjContent.replace('<PropertyGroup>', `<PropertyGroup>${flagInjection}`);
            }

            // 2. Inject Target override to neutralize the MSBuild PcfBuild target
            const targetInjection = `
  <Target Name="PcfBuild">
    <Message Text="PCF build intentionally disabled – using Node executor" Importance="high" />
  </Target>
`;
            if (pcfProjContent.includes('</Project>')) {
                pcfProjContent = pcfProjContent.replace('</Project>', `${targetInjection}\n</Project>`);
            }

            fs.writeFileSync(pcfProjPath, pcfProjContent);
            console.log('✓ Patched .pcfproj with SkipPCFBuild properties and PcfBuild Target override');
        }

        // c.1 CRITICAL FIX: Patch .cdsproj to exclude strings folder and skip PCF rebuild
        console.log('--- Patching Solution Project (.cdsproj) ---');
        const solutionFiles = fs.readdirSync(solutionDir).filter(f => f.endsWith('.cdsproj'));
        if (solutionFiles.length === 0) {
             throw new Error('No .cdsproj found after solution init');
        }
        const cdsProjPath = path.join(solutionDir, solutionFiles[0]);
        let cdsProjContent = fs.readFileSync(cdsProjPath, 'utf8');
        
        // Inject exclusions and property overrides
        // We use relative paths from the solution folder (..)
        // Note: DisablePcfBuild is an older/alternative flag, adding both for safety.
        const patchContent = `
  <ItemGroup>
    <None Remove="..\\out\\controls\\strings\\**\\*" />
  </ItemGroup>
  <PropertyGroup>
    <SkipPcfBuild>true</SkipPcfBuild>
    <DisablePcfBuild>true</DisablePcfBuild>
  </PropertyGroup>
</Project>`;

        
        cdsProjContent = cdsProjContent.replace('</Project>', patchContent);
        fs.writeFileSync(cdsProjPath, cdsProjContent);
        console.log('✓ Patched .cdsproj to exclude strings and skip PCF rebuild');

        // c. Solution Build (Packaging Only)
        // Note: PCF build and validation already happened in Step 2.
        // The .cdsproj patch above prevents implicit rebuilds.
        runCommand('dotnet restore', solutionDir);
        // Important: Solution build might trigger PCF build, but since we just built it,
        // incremental build logic should skip re-running pcf-scripts, preserving our cleanup.
        // USE RELEASE BUILD - Force UNMANAGED solution output
        runCommand('dotnet build -c Release /p:SolutionPackageType=Unmanaged', solutionDir);


        // STEP 4: Finalizing Artifact...
        console.log('\nSTEP 4: Finalizing Artifact...');
        
        // Expected location: output/bin/Release/{SolutionName}.zip
        const binRelease = path.join(solutionDir, 'bin', 'Release');
        
        if (!fs.existsSync(binRelease)) {
             throw new Error(`Solution build output directory not found: ${binRelease}`);
        }

        // Validate Solution ZIP was generated
        // We specifically want the UNMANAGED solution (no _managed in name).
        const allZips = fs.readdirSync(binRelease).filter(f => f.endsWith('.zip'));
        
        let targetZip = allZips.find(f => !f.includes('_managed.zip'));
        
        if (!targetZip && allZips.length > 0) {
            // Fallback: If for some reason only managed exists (shouldn't happen with /p param), verify.
            console.warn('! Unmanaged ZIP not found, checking for any ZIP...');
            targetZip = allZips[0];
        }

        if (!targetZip) {
             throw new Error('No Solution ZIP found in build output');
        }
        
        const sourceZip = path.join(binRelease, targetZip);
        console.log(`✓ Solution ZIP verified: ${sourceZip}`);
        
        // Target: {componentName}_{buildId}.zip
        // buildId corresponds to the folder name usually, or passed context.
        // We will respect the previous logic for naming.
        const buildId = path.basename(workingDir); 
        const targetZipName = `${componentName}_${buildId}.zip`;
        const finalZipPath = path.join(workingDir, targetZipName);

        // Copy
        fs.copyFileSync(sourceZip, finalZipPath);
        console.log(`✓ Artifact finalized: ${finalZipPath}\n`);

        // STEP 5: Generate Preview
        console.log('\nSTEP 5: Generating Preview Harness...');
        try {
            // Locate preview-executor relative to this script
            const previewExecutorPath = path.join(__dirname, 'preview-executor.js');
            
            if (fs.existsSync(previewExecutorPath)) {
                // Pass buildId and workingDir
                runCommand(`node ${previewExecutorPath} "${buildId}" "${workingDir}"`, workingDir);
                console.log('✓ Preview generation complete');
            } else {
                 console.warn('! Preview executor not found, skipping preview generation');
            }
        } catch (e) {
             console.warn(`! Preview generation failed (non-fatal): ${e.message}`);
        }
        
        fs.copyFileSync(sourceZip, finalZipPath);
        console.log(`✓ Artifact ready: ${finalZipPath}`);

        console.log(JSON.stringify({
            step: "PCFBuild",
            status: "Success",
            zipPath: finalZipPath,
            solutionZip: sourceZip
        }));

        console.log("✓ BUILD COMPLETED SUCCESSFULLY");
        process.exit(0);

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
