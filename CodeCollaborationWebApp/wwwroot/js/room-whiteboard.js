"use strict";

const canvas = document.getElementById("whiteboard");
const context = canvas.getContext('2d', { willReadFrequently: true });


// Set canvas size
function resizeCanvas() {
    const rect = canvas.parentElement.getBoundingClientRect();

    // Store the current drawing if any
    const imageData = context.getImageData(0, 0, canvas.width, canvas.height);

    canvas.width = rect.width;
    canvas.height = rect.height;

    // Restore drawing settings
    context.lineJoin = "round";
    context.lineCap = "round";
    context.lineWidth = 2;

    // Restore the previous drawing if there was one
    if (imageData.width > 0) {
        context.putImageData(imageData, 0, 0);
    }
}

window.addEventListener("resize", resizeCanvas);
resizeCanvas();

let drawing = false;
let lastX = 0;
let lastY = 0;
let currentTool = "pen";

// Tool selection
document.getElementById("penTool").addEventListener("click", () => {
    currentTool = "pen";
    document.getElementById("penTool").classList.add("active-tool");
    document.getElementById("eraserTool").classList.remove("active-tool");
    context.globalCompositeOperation = "source-over";
});

document.getElementById("eraserTool").addEventListener("click", () => {
    currentTool = "eraser";
    document.getElementById("eraserTool").classList.add("active-tool");
    document.getElementById("penTool").classList.remove("active-tool");
    context.globalCompositeOperation = "destination-out";
});

document.getElementById("colorPicker").addEventListener("input", (e) => {
    context.strokeStyle = e.target.value;
});

// Clear whiteboard
document.getElementById("clearBtn").addEventListener("click", () => {
    if (confirm("Are you sure you want to clear everything?")) {
        context.clearRect(0, 0, canvas.width, canvas.height);
        connection.invoke("SendWhiteboardClear", roomCode);
        connection.invoke("SendCodeUpdate", roomCode, "");
    }
});

// Drawing events
canvas.addEventListener("mousedown", startDrawing);
canvas.addEventListener("touchstart", (e) => {
    e.preventDefault();
    const touch = e.touches[0];
    const mouseEvent = new MouseEvent("mousedown", {
        clientX: touch.clientX,
        clientY: touch.clientY
    });
    canvas.dispatchEvent(mouseEvent);
});

canvas.addEventListener("mouseup", stopDrawing);
canvas.addEventListener("touchend", (e) => {
    e.preventDefault();
    const mouseEvent = new MouseEvent("mouseup", {});
    canvas.dispatchEvent(mouseEvent);
});

canvas.addEventListener("mouseleave", stopDrawing);

canvas.addEventListener("mousemove", draw);
canvas.addEventListener("touchmove", (e) => {
    e.preventDefault();
    const touch = e.touches[0];
    const mouseEvent = new MouseEvent("mousemove", {
        clientX: touch.clientX,
        clientY: touch.clientY
    });
    canvas.dispatchEvent(mouseEvent);
});

function getCanvasCoordinates(e) {
    const rect = canvas.getBoundingClientRect();
    // Calculate the scale factor between the CSS size and actual canvas pixels
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;

    // Get precise pointer position
    return {
        x: (e.clientX - rect.left) * scaleX,
        y: (e.clientY - rect.top) * scaleY
    };
}

function startDrawing(e) {
    drawing = true;
    const pos = getCanvasCoordinates(e);
    lastX = pos.x;
    lastY = pos.y;

    // Start a new path and move to the position
    context.beginPath();
    context.moveTo(lastX, lastY);
}

function draw(e) {
    if (!drawing) return;

    const pos = getCanvasCoordinates(e);
    const x = pos.x;
    const y = pos.y;

    context.lineTo(x, y);
    context.stroke();

    const updateData = JSON.stringify({
        x1: lastX,
        y1: lastY,
        x2: x,
        y2: y,
        color: context.strokeStyle,
        tool: currentTool
    });

    connection.invoke("SendWhiteboardUpdate", roomCode, updateData);

    lastX = x;
    lastY = y;
}

function stopDrawing() {
    drawing = false;
    context.beginPath();
}

connection.on("ReceiveWhiteboardUpdate", (updateData) => {
    const data = JSON.parse(updateData);

    const originalOperation = context.globalCompositeOperation;
    const originalColor = context.strokeStyle;

    if (data.tool === "eraser") {
        context.globalCompositeOperation = "destination-out";
    } else {
        context.globalCompositeOperation = "source-over";
        context.strokeStyle = data.color;
    }

    context.beginPath();
    context.moveTo(data.x1, data.y1);
    context.lineTo(data.x2, data.y2);
    context.stroke();

    context.globalCompositeOperation = originalOperation;
    context.strokeStyle = originalColor;
    context.beginPath();
});

connection.on("ReceiveWhiteboardClear", () => {
    context.clearRect(0, 0, canvas.width, canvas.height);
});