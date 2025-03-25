"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/collaborationHub")
    .configureLogging(signalR.LogLevel.Debug)
    .build();

// Connection status handling
const connectionStatus = document.getElementById("connectionStatus");

connection.onclose(async (error) => {
    console.error("Connection closed with error:", error);
    connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-red-500 mr-2"></span> Disconnected';

    // Log detailed error information
    console.error("Connection state:", connection.state);
    console.error("Error details:", error ? error.toString() : "No error details");

    // Show user-friendly notification
    const toast = document.getElementById("toast");
    toast.textContent = "Connection lost. Attempting to reconnect...";
    toast.classList.remove("hidden");

    // Attempt to reconnect
    await tryReconnect();
});
// Copy room code functionality
document.getElementById("copyRoomBtn").addEventListener("click", () => {
    navigator.clipboard.writeText(roomCode).then(() => {
        const toast = document.getElementById("toast");
        toast.classList.remove("hidden");
        setTimeout(() => {
            toast.classList.add("hidden");
        }, 2000);
    });
});

let userCount = 0;

// Handle window/tab close events
window.addEventListener('beforeunload', () => {
    // This will trigger the OnDisconnectedAsync in the hub
    connection.stop();
});

// Add these connection handlers
connection.on("UpdateUserCount", (count) => {
    // Called when a user first joins to set the initial count
    userCount = count;
    updateUserCounter();
});

connection.on("UserJoined", (count) => {
    // Called when another user joins
    userCount = count;
    updateUserCounter();

    // Show notification
    const toast = document.getElementById("toast");
    toast.textContent = "Another user joined the room";
    toast.classList.remove("hidden");
    setTimeout(() => {
        toast.classList.add("hidden");
    }, 2000);
});

connection.on("UserLeft", (count) => {
    // Called when a user leaves
    userCount = count;
    updateUserCounter();

    // Show notification
    const toast = document.getElementById("toast");
    toast.textContent = "A user left the room";
    toast.classList.remove("hidden");
    setTimeout(() => {
        toast.classList.add("hidden");
    }, 2000);
});

connection.on("RoomNotFound", () => {
    alert("This room does not exist. You will be redirected to the home page.");
    window.location.href = "/";
});

// Update the user counter display
function updateUserCounter() {
    const statusEl = document.getElementById("connectionStatus");
    statusEl.innerHTML = `
                <span class="w-2 h-2 rounded-full bg-green-500 mr-2"></span>
                Connected <span class="ml-2 bg-gray-100 px-2 py-1 rounded-full text-xs font-medium">${userCount} ${userCount === 1 ? 'user' : 'users'}</span>
            `;
}

// Initialize the connection
connection.start().then(() => {
    connection.invoke("JoinRoom", roomCode);
    connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-green-500 mr-2"></span> Connecting...';
}).catch(err => {
    console.error(err.toString());
    connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-red-500 mr-2"></span> Disconnected';
});


connection.on("RoomTerminated", () => {
    alert("This room has been terminated. You will be redirected to the home page.");
    window.location.href = "/";
});

// Periodically check if room still exists (optional, as a backup)
setInterval(async () => {
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
}, 30000);


// Function to handle reconnection attempts
async function tryReconnect(attempt = 1) {
    console.log(`Attempting to reconnect (attempt ${attempt})...`);
    try {
        await connection.start();
        console.log("Reconnected successfully!");

        // Rejoin the room
        connection.invoke("JoinRoom", roomCode);
        connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-green-500 mr-2"></span> Connected';

        // Show success notification
        const toast = document.getElementById("toast");
        toast.textContent = "Reconnected successfully!";
        setTimeout(() => {
            toast.classList.add("hidden");
        }, 3000);
    } catch (err) {
        console.error("Reconnection failed:", err);

        // Exponential backoff for retry (max 30 seconds)
        const delay = Math.min(1000 * Math.pow(1.5, attempt), 30000);
        console.log(`Retrying in ${delay / 1000} seconds...`);

        // Update status
        connectionStatus.innerHTML = `<span class="w-2 h-2 rounded-full bg-red-500 mr-2"></span> Reconnecting in ${Math.round(delay / 1000)}s`;

        // Try again after delay with incremented attempt count
        setTimeout(() => tryReconnect(attempt + 1), delay);
    }
}