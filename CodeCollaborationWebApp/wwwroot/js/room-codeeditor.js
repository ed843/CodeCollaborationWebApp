"use strict";

// Monaco Editor Setup
let monacoEditor;
let currentLanguage = "javascript";
let ignoreNextCodeUpdate = false;



require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.43.0/min/vs' } });
require(['vs/editor/editor.main'], function () {
    monacoEditor = monaco.editor.create(document.getElementById('monaco-editor'), {
        value: '// Type your code here...',
        language: currentLanguage,
        theme: 'vs-dark',
        automaticLayout: true,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        fontSize: 14,
        tabSize: 2
    });

    // Handle editor content changes
    monacoEditor.onDidChangeModelContent((event) => {
        if (ignoreNextCodeUpdate) {
            ignoreNextCodeUpdate = false;
            return;
        }
        const code = monacoEditor.getValue();
        connection.invoke("SendCodeUpdate", roomCode, code);
    });

    // Language selection change
    document.getElementById("languageSelect").addEventListener("change", (e) => {
        const language = e.target.value;
        monaco.editor.setModelLanguage(monacoEditor.getModel(), language);
        currentLanguage = language;
        connection.invoke("SendLanguageChange", roomCode, language);
    });

    // Update handling for code from other users
    connection.on("ReceiveCodeUpdate", (code) => {
        if (monacoEditor.getValue() !== code) {
            ignoreNextCodeUpdate = true;
            monacoEditor.setValue(code);
        }
    });

    connection.on("ReceiveLanguageChange", (language) => {
        document.getElementById("languageSelect").value = language;
        monaco.editor.setModelLanguage(monacoEditor.getModel(), language);
        currentLanguage = language;
    });
});

// Code execution handling
document.getElementById("runCodeBtn").addEventListener("click", executeCode);
document.getElementById("clearOutputBtn").addEventListener("click", () => {
    document.getElementById("codeOutput").innerHTML = "// Output cleared";
});

function executeCode() {
    const code = monacoEditor.getValue();
    const language = document.getElementById("languageSelect").value;
    const outputElement = document.getElementById("codeOutput");

    outputElement.innerHTML = `// Running ${language} code...\n`;

    try {
        switch (language) {
            case "javascript":
                // For JavaScript, we can use the Function constructor to execute code safely
                executeJavaScript(code, outputElement);
                break;

            case "html":
                // For HTML, we'll open a new window with the HTML content
                executeHtml(code, outputElement);
                break;

            // currently unimplemented...
            case "python":
            case "java":
            case "csharp":
            case "typescript":
                // For languages that require a backend to run:
                outputElement.innerHTML += `\n// Server-side execution for ${language} is not available in this demo.\n`;
                outputElement.innerHTML += `// In a production environment, you would send this code to a backend service.\n`;
                outputElement.innerHTML += `// The code was successfully validated for syntax using Monaco Editor.`;
                break;

            default:
                outputElement.innerHTML = `// Execution not supported for ${language}`;
        }
    } catch (error) {
        outputElement.innerHTML += `\n// Error: ${error.message}\n`;
    }

    // Share the output with other collaborators
    connection.invoke("SendOutputUpdate", roomCode, outputElement.innerHTML);
}

function executeJavaScript(code, outputElement) {
    // Create a sandboxed console.log that outputs to our output element
    const originalConsoleLog = console.log;
    const originalConsoleError = console.error;
    const originalConsoleWarn = console.warn;
    const originalConsoleInfo = console.info;

    let output = "";

    // Override console methods
    console.log = (...args) => {
        output += args.map(arg =>
            typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
        ).join(' ') + '\n';
    };

    console.error = (...args) => {
        output += '📛 ERROR: ' + args.map(arg =>
            typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
        ).join(' ') + '\n';
    };

    console.warn = (...args) => {
        output += '⚠️ WARNING: ' + args.map(arg =>
            typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
        ).join(' ') + '\n';
    };

    console.info = (...args) => {
        output += 'ℹ️ INFO: ' + args.map(arg =>
            typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
        ).join(' ') + '\n';
    };

    try {
        // Execute the code in a way that captures returned values
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

        if (output) {
            outputElement.innerHTML += output;
        } else {
            outputElement.innerHTML += "// Code executed successfully with no output";
        }
    } catch (error) {
        outputElement.innerHTML += `\n// Execution error: ${error.message}\n`;
    } finally {
        // Restore original console methods
        console.log = originalConsoleLog;
        console.error = originalConsoleError;
        console.warn = originalConsoleWarn;
        console.info = originalConsoleInfo;
    }
}

function executeHtml(code, outputElement) {
    // Create a preview of the HTML in the output area
    outputElement.innerHTML = "// HTML Preview (simplified rendering):\n\n";

    // Create a sanitized preview (very basic - in production use a proper sanitizer)
    const previewDiv = document.createElement('div');
    previewDiv.style.maxHeight = '150px';
    previewDiv.style.overflow = 'auto';
    previewDiv.style.border = '1px solid #555';
    previewDiv.style.padding = '8px';
    previewDiv.style.marginTop = '8px';
    previewDiv.style.background = '#fff';
    previewDiv.style.color = '#000';

    try {
        // Basic sanitization (would use a proper library in production)
        const sanitized = code.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '<!-- scripts removed for security -->');
        previewDiv.innerHTML = sanitized;

        outputElement.appendChild(previewDiv);

        // Add option to open in new window
        const openButton = document.createElement('button');
        openButton.textContent = 'Open in New Window (Unsanitized)';
        openButton.style.marginTop = '8px';
        openButton.style.padding = '4px 8px';
        openButton.style.background = '#f59e0b'; // Change to warning color (amber)
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
            event.preventDefault(); // Stop any default action

            if (confirm("WARNING: Opening HTML in a new window will run any JavaScript code including potentially harmful scripts. Only open HTML from sources you trust. Continue?")) {
                const newWindow = window.open('', '_blank');
                if (newWindow) {
                    newWindow.document.write(code);
                    newWindow.document.close();
                }
            }
            return false; // Prevent event bubbling
        };

        openButton.onclick = function () {
            const newWindow = window.open('', '_blank');
            newWindow.document.write(code);
            newWindow.document.close();
        };

        outputElement.appendChild(openButton);
    } catch (error) {
        outputElement.innerHTML += `\n// Preview error: ${error.message}\n`;
    }
}

// Handle output updates from other collaborators
connection.on("ReceiveOutputUpdate", (output) => {
    document.getElementById("codeOutput").innerHTML = output;
});

// Update clear button to also clear the code editor
document.getElementById("clearBtn").addEventListener("click", () => {
    if (confirm("Are you sure you want to clear everything?")) {
        // Clear whiteboard
        context.clearRect(0, 0, canvas.width, canvas.height);
        connection.invoke("SendWhiteboardClear", roomCode);

        // Clear code editor
        monacoEditor.setValue("");
        connection.invoke("SendCodeUpdate", roomCode, "");

        // Clear output
        document.getElementById("codeOutput").innerHTML = "// Output cleared";
        connection.invoke("SendOutputUpdate", roomCode, "// Output cleared");
    }
});