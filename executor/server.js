const express = require('express');
const path = require('path');
const { interpretIntent } = require('./intent-interpreter');
const { generateSpec } = require('./spec-generator');
const { generateFiles } = require('./file-generator');
const { buildPcf } = require('./build-executor');

const app = express();
const PORT = process.env.PORT || 5001;

// Increase payload limit for large plans/specs
app.use(express.json({ limit: '50mb' }));

// Middleware to log requests
app.use((req, res, next) => {
    console.log(`[${new Date().toISOString()}] ${req.method} ${req.url}`);
    next();
});

/**
 * POST /interpret
 * Body: { userInput, brainPath }
 */
app.post('/interpret', async (req, res) => {
    try {
        const { userInput, brainPath } = req.body;
        if (!userInput || !brainPath) {
            return res.status(400).json({ error: 'Missing userInput or brainPath' });
        }

        const result = await interpretIntent(userInput, brainPath);
        res.json(result);
    } catch (error) {
        console.error('Interpret Error:', error);
        res.status(500).json({ error: error.message });
    }
});

/**
 * POST /spec
 * Body: { inputJson, brainPath }
 */
app.post('/spec', async (req, res) => {
    try {
        const { inputJson, brainPath } = req.body;
        if (!inputJson || !brainPath) {
            return res.status(400).json({ error: 'Missing inputJson or brainPath' });
        }

        const result = await generateSpec(inputJson, brainPath);
        res.json(result);
    } catch (error) {
        console.error('Spec Error:', error);
        res.status(500).json({ error: error.message });
    }
});

/**
 * POST /files
 * Body: { inputJson, outputDir, templatesDir }
 */
app.post('/files', (req, res) => {
    try {
        const { inputJson, outputDir, templatesDir } = req.body;
        if (!inputJson || !outputDir) {
            return res.status(400).json({ error: 'Missing inputJson or outputDir' });
        }

        // Default templates dir relative to server.js
        const effectiveTemplatesDir = templatesDir || path.join(__dirname, '../ai-brain/templates');

        // generateFiles is synchronous
        generateFiles(inputJson, outputDir, effectiveTemplatesDir);
        res.json({ status: 'Success' });
    } catch (error) {
        console.error('Files Error:', error);
        res.status(500).json({ error: error.message });
    }
});

/**
 * POST /build
 * Body: { workingDir, componentName }
 */
app.post('/build', (req, res) => {
    try {
        const { workingDir, componentName } = req.body;
        if (!workingDir || !componentName) {
            return res.status(400).json({ error: 'Missing workingDir or componentName' });
        }

        // buildPcf is synchronous
        buildPcf(workingDir, componentName);
        res.json({ status: 'Success' });
    } catch (error) {
        console.error('Build Error:', error);
        res.status(500).json({ error: error.message });
    }
});

// Health check
app.get('/health', (req, res) => {
    res.json({ status: 'ok' });
});

app.listen(PORT, () => {
    console.log(`Executor Server running on port ${PORT}`);
});
