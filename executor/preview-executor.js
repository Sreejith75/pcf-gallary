#!/usr/bin/env node

/**
 * Preview Executor for PCF Component
 * Generates a browser-based preview harness by wrapping the compiled bundle.
 * NO Rebuild - Pure wrapping of existing artifacts.
 */

const fs = require('fs');
const path = require('path');

async function main() {
    console.log('=== Preview Executor (Bundle Wrapper) ===\n');

    try {
        const buildId = process.argv[2];
        const buildDir = process.argv[3] || `/tmp/pcf-build/${buildId}`; // Default path if not passed

        if (!buildId) {
            throw new Error('Usage: node preview-executor.js <buildId> [buildDir]');
        }

        console.log(`Build ID: ${buildId}`);
        console.log(`Build Dir: ${buildDir}`);

        if (!fs.existsSync(buildDir)) {
            throw new Error(`Build directory not found: ${buildDir}`);
        }

        // 1. Locate Control
        // We know structure is out/controls/<Namespace>.<ControlName>/
        const controlsDir = path.join(buildDir, 'out/controls');
        if (!fs.existsSync(controlsDir)) {
             throw new Error(`Controls directory not found: ${controlsDir}`);
        }

        const controlFolders = fs.readdirSync(controlsDir).filter(f => fs.statSync(path.join(controlsDir, f)).isDirectory());
        if (controlFolders.length !== 1) {
            throw new Error(`Expected exactly 1 control folder in ${controlsDir}, found ${controlFolders.length}: ${controlFolders.join(', ')}`);
        }

        const controlFolderName = controlFolders[0]; // e.g., Bytestrone.StarRating
        const controlDir = path.join(controlsDir, controlFolderName);
        console.log(`✓ Located control: ${controlFolderName}`);

        // 2. Parse Manifest for Metadata
        const manifestPath = path.join(controlDir, 'ControlManifest.xml');
        if (!fs.existsSync(manifestPath)) {
            throw new Error(`ControlManifest.xml not found in ${controlDir}`);
        }
        const manifestContent = fs.readFileSync(manifestPath, 'utf8');

        // Simple Regex Extraction (avoiding XML parser dependency for robustness/speed)
        const namespaceMatch = manifestContent.match(/namespace="([^"]+)"/);
        const constructorMatch = manifestContent.match(/constructor="([^"]+)"/);
        const versionMatch = manifestContent.match(/version="([^"]+)"/);
        
        const namespace = namespaceMatch ? namespaceMatch[1] : null;
        const constructorName = constructorMatch ? constructorMatch[1] : null;
        const version = versionMatch ? versionMatch[1] : '1.0.0';

        if (!namespace || !constructorName) {
            throw new Error('Failed to parse namespace or constructor from ControlManifest.xml');
        }

        console.log(`  Namespace: ${namespace}`);
        console.log(`  Constructor: ${constructorName}`);
        console.log(`  Version: ${version}`);

        // Extract Properties
        // <property name="value" display-name-key="..." description-key="..." of-type="..." usage="..." required="..." />
        const propertyRegex = /<property\s+name="([^"]+)"[^>]*of-type="([^"]+)"[^>]*usage="([^"]+)"/g;
        let match;
        const properties = [];
        while ((match = propertyRegex.exec(manifestContent)) !== null) {
            properties.push({
                name: match[1],
                type: match[2],
                usage: match[3]
            });
        }
        console.log(`  Properties: ${properties.map(p => p.name).join(', ')}`);

        // 3. Prepare Preview Directory
        const previewDir = path.join(buildDir, 'preview');
        if (fs.existsSync(previewDir)) {
            fs.rmSync(previewDir, { recursive: true });
        }
        fs.mkdirSync(previewDir, { recursive: true });

        // 4. Copy Bundle
        // check flat or nested css/
        const flatBundle = path.join(controlDir, 'bundle.js');
        const nestedBundle = path.join(controlDir, 'css', 'bundle.js');
        let bundleSrc;

        if (fs.existsSync(flatBundle)) bundleSrc = flatBundle;
        else if (fs.existsSync(nestedBundle)) bundleSrc = nestedBundle;
        else throw new Error('bundle.js not found in control directory');

        fs.copyFileSync(bundleSrc, path.join(previewDir, 'bundle.js'));
        console.log(`✓ Copied bundle.js`);

        // 5. Generate Runtime
        const runtimeContent = generateRuntime(namespace, constructorName, properties);
        fs.writeFileSync(path.join(previewDir, 'preview-runtime.js'), runtimeContent);
        console.log(`✓ Generated preview-runtime.js`);

        // 6. Generate Index
        const indexContent = generateIndex(controlFolderName, version);
        fs.writeFileSync(path.join(previewDir, 'index.html'), indexContent);
        console.log(`✓ Generated index.html`);

        console.log(`\nPREVIEW READY: ${path.join(previewDir, 'index.html')}\n`);

    } catch (error) {
        console.error(`\n❌ PREVIEW FAILED: ${error.message}`);
        process.exit(1);
    }
}

function generateRuntime(namespace, constructorName, properties) {
    // Generate safe default values
    const mockState = {};
    properties.forEach(p => {
        if (p.usage === 'bound' || p.usage === 'input') {
            switch(p.type) {
                case 'Whole.None': mockState[p.name] = 0; break;
                case 'TwoOptions': mockState[p.name] = false; break;
                case 'SingleLine.Text': mockState[p.name] = "Sample Text"; break;
                default: mockState[p.name] = null;
            }
        }
    });

    return `
/**
 * Mock PCF Runtime
 */
console.log("Mock Runtime Initializing...");

const MOCK_STATE = ${JSON.stringify(mockState, null, 2)};

// Mock Context
const context = {
    parameters: {},
    mode: {
        isControlDisabled: false,
        isVisible: true
    },
    utils: {
        getEntityMetadata: () => ({}),
        getFormatter: () => ({})
    },
    resources: {
        getString: (id) => id
    }
};

// Populate parameters with getters
Object.keys(MOCK_STATE).forEach(key => {
    context.parameters[key] = {
        raw: MOCK_STATE[key],
        type: typeof MOCK_STATE[key]
    };
});

// Mock NotifyOutputChanged
const notifyOutputChanged = () => {
    console.log("⚡ [PCF] notifyOutputChanged called");
    const outputs = control.getOutputs();
    console.log("   Outputs:", outputs);
    
    // Update local state if bound
    Object.keys(outputs).forEach(k => {
        if (MOCK_STATE.hasOwnProperty(k)) {
            MOCK_STATE[k] = outputs[k];
            context.parameters[k].raw = outputs[k]; // Sync back to context
            // Re-render to reflect change (like canvas apps do)
            control.updateView(context);
        }
    });
};

// Instantiate Control
let control;
try {
    console.log("Instantiating ${namespace}.${constructorName}...");
    if (typeof ${namespace} === 'undefined' || typeof ${namespace}.${constructorName} === 'undefined') {
        throw new Error("Namespace or Constructor not found in global scope. Did bundle.js load?");
    }
    
    control = new ${namespace}.${constructorName}();
    
    const container = document.getElementById('pcf-container');
    
    // Init
    console.log("Calling init()...");
    control.init(context, notifyOutputChanged, {}, container);
    
    // Initial View
    console.log("Calling updateView()...");
    control.updateView(context);
    
    if(window.removeLoading) window.removeLoading();
    console.log("✅ Control Ready");

} catch (err) {
    console.error("❌ Runtime Error:", err);
    document.body.innerHTML += '<div style="color:red; padding:20px">Runtime Error: ' + err.message + '</div>';
}
`;
}

function generateIndex(title, version) {
    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Preview: ${title}</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; background: #f0f2f5; margin: 0; padding: 20px; display: flex; flex-direction: column; align-items: center; }
        .metadata { font-size: 12px; color: #666; margin-bottom: 20px; }
        .container-wrapper {
            background: white;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            width: 400px; /* Default canvas size */
            height: 300px;
            display: flex;
            justify-content: center;
            align-items: center;
            resize: both;
            overflow: auto;
            position: relative;
        }
        #pcf-container {
            width: 100%;
            height: 100%;
        }
        .console-log {
            margin-top: 20px;
            width: 100%;
            max-width: 600px;
            background: #222;
            color: #0f0;
            padding: 10px;
            font-family: monospace;
            font-size: 12px;
            height: 150px;
            overflow-y: auto;
            border-radius: 4px;
        }
        #error-banner {
            display: none;
            width: 100%;
            max-width: 600px;
            background: #fee2e2;
            color: #b91c1c;
            padding: 10px;
            border-radius: 4px;
            margin-bottom: 20px;
            border: 1px solid #f87171;
        }
        #loading {
            position: absolute;
            font-size: 14px;
            color: #666;
        }
    </style>
    <script>
        window.onerror = function(msg, url, line, col, error) {
            const banner = document.getElementById('error-banner');
            if (banner) {
                banner.style.display = 'block';
                banner.innerText = "Global Error: " + msg + "\\n" + (url || '') + ":" + (line || '?');
            }
            console.error("Global Error:", msg, error);
            return false;
        };
    </script>
</head>
<body>

    <h2>${title}</h2>
    <div class="metadata">Version: ${version} | Environment: Mock Harness</div>

    <div id="error-banner"></div>

    <div class="container-wrapper">
        <div id="loading">Loading Control...</div>
        <div id="pcf-container"></div>
    </div>

    <!-- Console Output for visual verification -->
    <div class="console-log" id="console-output">
        <div>> Mock Runtime Ready</div>
    </div>

    <script>
        // Redirect console.log to UI
        const oldLog = console.log;
        const logContainer = document.getElementById('console-output');
        console.log = function(...args) {
            oldLog.apply(console, args);
            const line = document.createElement('div');
            line.textContent = '> ' + args.map(a => {
                try { return typeof a === 'object' ? JSON.stringify(a) : a; } catch(e) { return '[Circular]'; }
            }).join(' ');
            logContainer.insertBefore(line, logContainer.firstChild); // Prepend
        };
        
        window.removeLoading = function() {
            const loading = document.getElementById('loading');
            if(loading) loading.style.display = 'none';
        }
    </script>

    <!-- Load Bundle -->
    <script src="bundle.js" onerror="console.error('Failed to load bundle.js'); document.getElementById('error-banner').style.display='block'; document.getElementById('error-banner').innerText='Failed to load bundle.js';"></script>
    
    <!-- Load Runtime -->
    <script src="preview-runtime.js" onerror="console.error('Failed to load preview-runtime.js'); document.getElementById('error-banner').style.display='block'; document.getElementById('error-banner').innerText='Failed to load preview-runtime.js';"></script>

</body>
</html>`;
}

// Run if called directly
if (require.main === module) {
    main();
}
