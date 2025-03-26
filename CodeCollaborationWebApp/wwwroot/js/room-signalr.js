"use strict";

// Constants and DOM elements
const connectionStatus = document.getElementById("connectionStatus");
const toast = document.getElementById("toast");
const copyRoomBtn = document.getElementById("copyRoomBtn");
const RECONNECT_INTERVAL = 30000; // 30 seconds

// SignalR connection setup
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/collaborationHub")
    .configureLogging(signalR.LogLevel.Debug)
    .build();

// State variables
let userCount = 0;

// Helper functions
function showToast(message, duration = 2000) {
    toast.textContent = message;
    toast.classList.remove("hidden");
    setTimeout(() => toast.classList.add("hidden"), duration);
}

function updateConnectionStatus(status, color) {
    connectionStatus.innerHTML = `<span class="w-2 h-2 rounded-full bg-${color}-500 mr-2"></span> ${status}`;
}

function updateUserCounter() {
    const userText = userCount === 1 ? 'user' : 'users';
    connectionStatus.innerHTML = `
        <span class="w-2 h-2 rounded-full bg-green-500 mr-2"></span>
        Connected <span class="ml-2 bg-gray-100 px-2 py-1 rounded-full text-xs font-medium">${userCount} ${userText}</span>
    `;
}

// Connection handling
async function initializeConnection() {
    try {
        await connection.start();
        connection.invoke("JoinRoom", roomCode);
        updateConnectionStatus("Connecting...", "green");
    } catch (err) {
        console.error("Connection failed:", err.toString());
        updateConnectionStatus("Disconnected", "red");
    }
}

async function tryReconnect(attempt = 1) {
    console.log(`Attempting to reconnect (attempt ${attempt})...`);
    try {
        await connection.start();
        console.log("Reconnected successfully!");
        connection.invoke("JoinRoom", roomCode);
        updateConnectionStatus("Connected", "green");
        showToast("Reconnected successfully!", 3000);
    } catch (err) {
        console.error("Reconnection failed:", err);
        const delay = Math.min(1000 * Math.pow(1.5, attempt), 30000);
        console.log(`Retrying in ${delay / 1000} seconds...`);
        updateConnectionStatus(`Reconnecting in ${Math.round(delay / 1000)}s`, "red");
        setTimeout(() => tryReconnect(attempt + 1), delay);
    }
}

// Event listeners
copyRoomBtn.addEventListener("click", () => {
    navigator.clipboard.writeText(roomCode).then(() => showToast("Room code copied!"));
});

window.addEventListener('beforeunload', () => connection.stop());

// SignalR event handlers
connection.onclose(async (error) => {
    console.error("Connection closed with error:", error);
    updateConnectionStatus("Disconnected", "red");
    console.error("Connection state:", connection.state);
    console.error("Error details:", error ? error.toString() : "No error details");
    showToast("Connection lost. Attempting to reconnect...");
    await tryReconnect();
});

connection.on("UpdateUserCount", (count) => {
    userCount = count;
    updateUserCounter();
});

connection.on("UserJoined", (count) => {
    userCount = count;
    updateUserCounter();
    showToast("Another user joined the room");
});

connection.on("UserLeft", (count) => {
    userCount = count;
    updateUserCounter();
    showToast("A user left the room");
});

connection.on("RoomNotFound", () => {
    alert("This room does not exist. You will be redirected to the home page.");
    window.location.href = "/";
});

connection.on("RoomTerminated", () => {
    alert("This room has been terminated. You will be redirected to the home page.");
    window.location.href = "/";
});

// Room verification
async function verifyRoom() {
    if (connection.state !== "Connected") return;
    try {
        const response = await fetch(`/api/room/verify?code=${roomCode}`);
        const data = await response.json();
        if (!data.exists) {
            alert("This room no longer exists. You will be redirected to the home page.");
            window.location.href = "/";
        }
    } catch (error) {
        console.error("Error checking room status:", error);
    }
}

// Initialization
initializeConnection();
setInterval(verifyRoom, RECONNECT_INTERVAL);
