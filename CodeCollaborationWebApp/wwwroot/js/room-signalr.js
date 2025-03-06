"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/collaborationHub")
    .build();

// Connection status handling
const connectionStatus = document.getElementById("connectionStatus");

connection.start().then(() => {
    connection.invoke("JoinRoom", roomCode);
    connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-green-500 mr-2"></span> Connected';
}).catch(err => {
    console.error(err.toString());
    connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-red-500 mr-2"></span> Disconnected';
});

connection.onclose(() => {
    connectionStatus.innerHTML = '<span class="w-2 h-2 rounded-full bg-red-500 mr-2"></span> Disconnected';
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