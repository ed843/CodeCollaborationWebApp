"use strict";

export class Whiteboard {
    constructor(canvasId, connection, roomCode) {
        this.canvas = document.getElementById(canvasId);
        this.context = this.canvas.getContext('2d', { willReadFrequently: true });
        this.connection = connection;
        this.roomCode = roomCode;

        this.isDrawingInitialized = false;
        this.drawing = false;
        this.lastX = 0;
        this.lastY = 0;
        this.currentTool = "pen";

        this.initializeEventListeners();
        this.initializeSignalRHandlers();
        this.resizeCanvas();
    }

    initializeEventListeners() {
        window.addEventListener("resize", () => this.resizeCanvas());

        document.getElementById("penTool").addEventListener("click", () => this.setTool("pen"));
        document.getElementById("eraserTool").addEventListener("click", () => this.setTool("eraser"));
        document.getElementById("colorPicker").addEventListener("input", (e) => {
            this.context.strokeStyle = e.target.value;
        });
        document.getElementById("clearBtn").addEventListener("click", () => this.clearWhiteboard());

        this.canvas.addEventListener("mousedown", (e) => this.startDrawing(e));
        this.canvas.addEventListener("mouseup", () => this.stopDrawing());
        this.canvas.addEventListener("mouseleave", () => this.stopDrawing());
        this.canvas.addEventListener("mousemove", (e) => this.draw(e));

        this.canvas.addEventListener("touchstart", (e) => this.handleTouchStart(e));
        this.canvas.addEventListener("touchend", (e) => this.handleTouchEnd(e));
        this.canvas.addEventListener("touchmove", (e) => this.handleTouchMove(e));
    }

    initializeSignalRHandlers() {
        this.connection.on("InitializeWhiteboard", (stateData) => this.initializeWhiteboard(stateData));
        this.connection.on("ReceiveWhiteboardUpdate", (updateData) => this.receiveWhiteboardUpdate(updateData));
        this.connection.on("ReceiveWhiteboardClear", () => this.receiveWhiteboardClear());
    }

    resizeCanvas() {
        const rect = this.canvas.parentElement.getBoundingClientRect();
        const imageData = this.context.getImageData(0, 0, this.canvas.width, this.canvas.height);

        this.canvas.width = rect.width;
        this.canvas.height = rect.height;

        this.restoreDrawingSettings();

        if (imageData.width > 0) {
            this.context.putImageData(imageData, 0, 0);
        }
    }

    restoreDrawingSettings() {
        this.context.lineJoin = "round";
        this.context.lineCap = "round";
        this.context.lineWidth = 2;
    }

    captureWhiteboardState() {
        return this.canvas.toDataURL('image/png');
    }

    saveWhiteboardState() {
        if (this.isDrawingInitialized) {
            const state = this.captureWhiteboardState();
            this.connection.invoke("SendWhiteboardState", this.roomCode, state);
        }
    }

    getCanvasCoordinates(e) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;

        return {
            x: (e.clientX - rect.left) * scaleX,
            y: (e.clientY - rect.top) * scaleY
        };
    }

    startDrawing(e) {
        this.drawing = true;
        const pos = this.getCanvasCoordinates(e);
        this.lastX = pos.x;
        this.lastY = pos.y;

        this.context.beginPath();
        this.context.moveTo(this.lastX, this.lastY);
    }

    draw(e) {
        if (!this.drawing) return;

        const pos = this.getCanvasCoordinates(e);
        const x = pos.x;
        const y = pos.y;

        this.context.lineTo(x, y);
        this.context.stroke();

        this.sendDrawingUpdate(x, y);

        this.lastX = x;
        this.lastY = y;
    }

    stopDrawing() {
        if (this.drawing) {
            this.drawing = false;
            this.context.beginPath();
            this.saveWhiteboardState();
        }
    }

    sendDrawingUpdate(x, y) {
        const updateData = JSON.stringify({
            x1: this.lastX,
            y1: this.lastY,
            x2: x,
            y2: y,
            color: this.context.strokeStyle,
            tool: this.currentTool
        });

        this.connection.invoke("SendWhiteboardUpdate", this.roomCode, updateData);
        this.isDrawingInitialized = true;
    }

    setTool(tool) {
        this.currentTool = tool;
        document.getElementById(`${tool}Tool`).classList.add("active-tool");
        document.getElementById(`${tool === "pen" ? "eraser" : "pen"}Tool`).classList.remove("active-tool");
        this.context.globalCompositeOperation = tool === "pen" ? "source-over" : "destination-out";
    }

    clearWhiteboard() {
        if (confirm("Are you sure you want to clear everything?")) {
            this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
            this.connection.invoke("SendWhiteboardClear", this.roomCode);
            this.connection.invoke("SendCodeUpdate", this.roomCode, "");
            this.saveWhiteboardState();
        }
    }

    handleTouchStart(e) {
        e.preventDefault();
        const touch = e.touches[0];
        const mouseEvent = new MouseEvent("mousedown", {
            clientX: touch.clientX,
            clientY: touch.clientY
        });
        this.canvas.dispatchEvent(mouseEvent);
    }

    handleTouchEnd(e) {
        e.preventDefault();
        const mouseEvent = new MouseEvent("mouseup", {});
        this.canvas.dispatchEvent(mouseEvent);
    }

    handleTouchMove(e) {
        e.preventDefault();
        const touch = e.touches[0];
        const mouseEvent = new MouseEvent("mousemove", {
            clientX: touch.clientX,
            clientY: touch.clientY
        });
        this.canvas.dispatchEvent(mouseEvent);
    }

    initializeWhiteboard(stateData) {
        if (!this.isDrawingInitialized && stateData) {
            const img = new Image();
            img.onload = () => {
                this.context.drawImage(img, 0, 0);
                this.isDrawingInitialized = true;
            };
            img.src = stateData;
        }
    }

    receiveWhiteboardUpdate(updateData) {
        const data = JSON.parse(updateData);

        const originalOperation = this.context.globalCompositeOperation;
        const originalColor = this.context.strokeStyle;

        this.context.globalCompositeOperation = data.tool === "eraser" ? "destination-out" : "source-over";
        this.context.strokeStyle = data.color;

        this.context.beginPath();
        this.context.moveTo(data.x1, data.y1);
        this.context.lineTo(data.x2, data.y2);
        this.context.stroke();

        this.context.globalCompositeOperation = originalOperation;
        this.context.strokeStyle = originalColor;
        this.context.beginPath();
    }

    receiveWhiteboardClear() {
        this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.isDrawingInitialized = true;
    }
}

// Only create the instance if not in a test environment
if (typeof connection !== 'undefined' && typeof roomCode !== 'undefined') {
    const whiteboard = new Whiteboard("whiteboard", connection, roomCode);
}