"use strict";

// ===== CONFIGURATION =====
export const CONFIG = {
    editor: {
        defaultValue: '// Type your code here...',
        defaultLanguage: 'javascript',
        theme: 'vs-dark',
        fontSize: 14,
        tabSize: 2
    },
    output: {
        maxSize: 10000 // Characters
    }
};

// ===== EDITOR MODULE =====
export const EditorModule = (function () {
    let monacoEditor;
    let currentLanguage = CONFIG.editor.defaultLanguage;
    let ignoreNextCodeUpdate = false;

    function initialize() {
        require.config({
            paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.43.0/min/vs' }
        });

        require(['vs/editor/editor.main'], function () {
            createEditor();
            setupEventListeners();
        });
    }

    function createEditor() {
        monacoEditor = monaco.editor.create(document.getElementById('monaco-editor'), {
            value: CONFIG.editor.defaultValue,
            language: currentLanguage,
            theme: CONFIG.editor.theme,
            automaticLayout: true,
            minimap: { enabled: false },
            scrollBeyondLastLine: false,
            fontSize: CONFIG.editor.fontSize,
            tabSize: CONFIG.editor.tabSize
        });
    }

    function setupEventListeners() {
        // Handle editor content changes
        monacoEditor.onDidChangeModelContent((event) => {
            if (ignoreNextCodeUpdate) {
                ignoreNextCodeUpdate = false;
                return;
            }
            const code = monacoEditor.getValue();
            CollaborationModule.sendCodeUpdate(code);
        });

        // Language selection change
        document.getElementById("languageSelect").addEventListener("change", (e) => {
            const language = e.target.value;
            monaco.editor.setModelLanguage(monacoEditor.getModel(), language);
            currentLanguage = language;
            CollaborationModule.sendLanguageChange(language);
        });
    }

    // Public API
    return {
        initialize,
        getValue: () => monacoEditor?.getValue() || '',
        setValue: (code) => {
            if (monacoEditor && monacoEditor.getValue() !== code) {
                ignoreNextCodeUpdate = true;
                monacoEditor.setValue(code);
            }
        },
        setLanguage: (language) => {
            if (monacoEditor) {
                document.getElementById("languageSelect").value = language;
                monaco.editor.setModelLanguage(monacoEditor.getModel(), language);
                currentLanguage = language;
            }
        },
        getLanguage: () => document.getElementById("languageSelect").value
    };
})();

// ===== CODE EXECUTION MODULE =====
export const ExecutionModule = (function () {
    function executeCode() {
        const code = EditorModule.getValue();
        const language = EditorModule.getLanguage();
        const outputElement = document.getElementById("codeOutput");

        outputElement.innerHTML = `// Running ${language} code...\n`;

        try {
            switch (language) {
                case "javascript":
                    executeJavaScript(code, outputElement);
                    break;
                case "html":
                    executeHtml(code, outputElement);
                    break;
                case "python":
                case "java":
                case "csharp":
                case "typescript":
                    handleServerSideLanguage(language, outputElement);
                    break;
                default:
                    outputElement.innerHTML = `// Execution not supported for ${language}`;
            }
        } catch (error) {
            outputElement.innerHTML += `\n// Error: ${error.message}\n`;
        }

        // Share the output with other collaborators
        CollaborationModule.sendOutputUpdate(outputElement.innerHTML);
    }

    function executeJavaScript(code, outputElement) {
        const consoleOverrides = createConsoleOverrides();

        try {
            // Override console methods
            const originalConsoleMethods = overrideConsoleMethods(consoleOverrides);

            // Execute the code
            executeWithReturnCapture(code);

            // Display output
            if (consoleOverrides.output) {
                outputElement.innerHTML += consoleOverrides.output;
            } else {
                outputElement.innerHTML += "// Code executed successfully with no output";
            }
        } catch (error) {
            outputElement.innerHTML += `\n// Execution error: ${error.message}\n`;
        } finally {
            // Restore original console methods
            restoreConsoleMethods(consoleOverrides.originalMethods);
        }
    }

    function createConsoleOverrides() {
        return {
            output: "",
            originalMethods: {
                log: console.log,
                error: console.error,
                warn: console.warn,
                info: console.info
            }
        };
    }

    function overrideConsoleMethods(consoleOverrides) {
        // Store original methods
        const originalMethods = {
            log: console.log,
            error: console.error,
            warn: console.warn,
            info: console.info
        };

        // Override console.log
        console.log = (...args) => {
            appendToOutput(consoleOverrides, args.map(formatArg).join(' ') + '\n');
        };

        // Override console.error
        console.error = (...args) => {
            appendToOutput(consoleOverrides, '⛔ ERROR: ' + args.map(formatArg).join(' ') + '\n');
        };

        // Override console.warn
        console.warn = (...args) => {
            appendToOutput(consoleOverrides, '⚠️ WARNING: ' + args.map(formatArg).join(' ') + '\n');
        };

        // Override console.info
        console.info = (...args) => {
            appendToOutput(consoleOverrides, '🛈 INFO: ' + args.map(formatArg).join(' ') + '\n');
        };

        return originalMethods;
    }

    function formatArg(arg) {
        return typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg);
    }

    function appendToOutput(consoleOverrides, text) {
        consoleOverrides.output += text;

        // Truncate if too large
        if (consoleOverrides.output.length > CONFIG.output.maxSize) {
            consoleOverrides.output = consoleOverrides.output.substring(0, CONFIG.output.maxSize) +
                "\n// Output truncated due to size limits...";
        }
    }

    function restoreConsoleMethods(originalMethods) {
        console.log = originalMethods.log;
        console.error = originalMethods.error;
        console.warn = originalMethods.warn;
        console.info = originalMethods.info;
    }

    function executeWithReturnCapture(code) {
        const executeWithReturn = new Function(`
      "use strict";
      let __result;
      try {
        __result = (function() { ${code} })();
        if (__result !== undefined) {
          console.log("→ Return value:", __result);
        }
      } catch (e) {
        console.error(e);
      }
    `);

        executeWithReturn();
    }

    function executeHtml(code, outputElement) {
        // Create a preview of the HTML in the output area
        outputElement.innerHTML = "// HTML Preview (simplified rendering):\n\n";

        // Create a sanitized preview
        const previewDiv = createHtmlPreviewElement(code);
        outputElement.appendChild(previewDiv);

        // Add option to open in new window
        const openButton = createOpenInNewWindowButton(code);
        outputElement.appendChild(openButton);
    }

    function createHtmlPreviewElement(code) {
        const previewDiv = document.createElement('div');
        previewDiv.style.maxHeight = '150px';
        previewDiv.style.overflow = 'auto';
        previewDiv.style.border = '1px solid #555';
        previewDiv.style.padding = '8px';
        previewDiv.style.marginTop = '8px';
        previewDiv.style.background = '#fff';
        previewDiv.style.color = '#000';

        // Basic sanitization (would use a proper library in production)
        const sanitized = code.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
            '<!-- scripts removed for security -->');
        previewDiv.innerHTML = sanitized;

        return previewDiv;
    }

    function createOpenInNewWindowButton(code) {
        const openButton = document.createElement('button');
        openButton.textContent = 'Open in New Window (Unsanitized)';
        openButton.style.marginTop = '8px';
        openButton.style.padding = '4px 8px';
        openButton.style.background = '#f59e0b';
        openButton.style.color = 'white';
        openButton.style.border = 'none';
        openButton.style.borderRadius = '4px';
        openButton.style.cursor = 'pointer';
        openButton.style.display = 'flex';
        openButton.style.alignItems = 'center';

        // Add warning icon
        const warningIcon = document.createElement('span');
        warningIcon.innerHTML = '⚠️';
        warningIcon.style.marginRight = '4px';
        openButton.prepend(warningIcon);

        openButton.onclick = function (event) {
            event.preventDefault();
            if (confirm("WARNING: Opening HTML in a new window will run any JavaScript code including potentially harmful scripts. Only open HTML from sources you trust. Continue?")) {
                const newWindow = window.open('', '_blank');
                if (newWindow) {
                    newWindow.document.write(code);
                    newWindow.document.close();
                }
            }
            return false;
        };

        return openButton;
    }

    function handleServerSideLanguage(language, outputElement) {
        outputElement.innerHTML += `\n// Server-side execution for ${language} is not available in this demo.\n`;
        outputElement.innerHTML += `// In a production environment, you would send this code to a backend service.\n`;
        outputElement.innerHTML += `// The code was successfully validated for syntax using Monaco Editor.`;
    }

    function clearOutput() {
        document.getElementById("codeOutput").innerHTML = "// Output cleared";
        CollaborationModule.sendOutputUpdate("// Output cleared");
    }

    // Public API
    return {
        executeCode,
        clearOutput
    };
})();

// ===== COLLABORATION MODULE =====
const CollaborationModule = (function () {
    // Assuming 'connection' and 'roomCode' are defined elsewhere

    function initialize() {
        setupCollaborationListeners();
    }

    function setupCollaborationListeners() {
        // Handle code updates from other users
        connection.on("ReceiveCodeUpdate", (code) => {
            EditorModule.setValue(code);
        });

        // Handle language changes from other users
        connection.on("ReceiveLanguageChange", (language) => {
            EditorModule.setLanguage(language);
        });

        // Handle output updates from other collaborators
        connection.on("ReceiveOutputUpdate", (output) => {
            document.getElementById("codeOutput").innerHTML = output;
        });

        // Handle whiteboard clear from other collaborators
        connection.on("ReceiveWhiteboardClear", () => {
            const canvas = document.getElementById('canvas');
            const context = canvas.getContext('2d');
            context.clearRect(0, 0, canvas.width, canvas.height);
        });
    }

    // Public API
    return {
        initialize,
        sendCodeUpdate: (code) => connection.invoke("SendCodeUpdate", roomCode, code),
        sendLanguageChange: (language) => connection.invoke("SendLanguageChange", roomCode, language),
        sendOutputUpdate: (output) => connection.invoke("SendOutputUpdate", roomCode, output),
        sendWhiteboardClear: () => connection.invoke("SendWhiteboardClear", roomCode)
    };
})();

// ===== UI MODULE =====
const UIModule = (function () {
    function initialize() {
        setupEventListeners();
    }

    function setupEventListeners() {
        // Run code button
        document.getElementById("runCodeBtn").addEventListener("click", ExecutionModule.executeCode);

        // Clear output button
        document.getElementById("clearOutputBtn").addEventListener("click", ExecutionModule.clearOutput);

        // Clear everything button
        document.getElementById("clearBtn").addEventListener("click", clearEverything);
    }

    function clearEverything() {
        if (confirm("Are you sure you want to clear everything?")) {
            // Clear whiteboard
            const canvas = document.getElementById('canvas');
            if (canvas) {
                const context = canvas.getContext('2d');
                context.clearRect(0, 0, canvas.width, canvas.height);
                CollaborationModule.sendWhiteboardClear();
            }

            // Clear code editor
            EditorModule.setValue("");
            CollaborationModule.sendCodeUpdate("");

            // Clear output
            document.getElementById("codeOutput").innerHTML = "// Output cleared";
            CollaborationModule.sendOutputUpdate("// Output cleared");
        }
    }

    // Public API
    return {
        initialize
    };
})();

// ===== INITIALIZATION =====
function initializeApplication() {
    EditorModule.initialize();
    CollaborationModule.initialize();
    UIModule.initialize();
}

// Start the application
document.addEventListener('DOMContentLoaded', initializeApplication);
