// home-room-actions.js
"use strict";

// Constants
export const CREATE_ROOM_BTN_ID = 'createRoomBtn';
export const JOIN_ROOM_BTN_ID = 'joinRoomBtn';
export const ROOM_CODE_INPUT_ID = 'roomCodeInput';
export const ROOM_CODE_LENGTH = 5;

// Helper Functions
export function setButtonLoading(button, loadingText) {
    const originalText = button.innerHTML;
    button.innerHTML = `<i class="fas fa-spinner fa-spin mr-2"></i>${loadingText}`;
    button.disabled = true;
    return originalText;
}

export function resetButton(button, originalText) {
    button.innerHTML = originalText;
    button.disabled = false;
}

// Export other functions similarly
export function highlightInputError(input) {
    input.classList.add('border-red-500');
    setTimeout(() => {
        input.classList.remove('border-red-500');
    }, 2000);
}

// API Functions
export async function createRoom() {
    const response = await fetch('/api/room/create');
    const data = await response.json();
    return data.roomCode;
}

export async function verifyRoom(code) {
    const response = await fetch(`/api/room/verify?code=${code}`);
    const data = await response.json();
    return data.exists;
}

// Event Handlers
export async function handleCreateRoom() {
    const btn = document.getElementById(CREATE_ROOM_BTN_ID);
    const originalText = setButtonLoading(btn, 'Creating...');

    try {
        const roomCode = await createRoom();
        window.location.href = `/Room?code=${roomCode}`;
    } catch (error) {
        resetButton(btn, originalText);
        alert('Failed to create room. Please try again.');
    }
}

export async function handleJoinRoom() {
    const code = document.getElementById(ROOM_CODE_INPUT_ID).value.toUpperCase();
    if (code.length === ROOM_CODE_LENGTH) {
        const btn = document.getElementById(JOIN_ROOM_BTN_ID);
        const originalContent = setButtonLoading(btn, '');

        try {
            const roomExists = await verifyRoom(code);
            if (roomExists) {
                window.location.href = `/Room?code=${code}`;
            } else {
                highlightInputError(document.getElementById(ROOM_CODE_INPUT_ID));
                alert('Room not found. Please check the code and try again.');
            }
        } catch (error) {
            alert('Error verifying room. Please try again.');
        } finally {
            resetButton(btn, originalContent);
        }
    } else {
        highlightInputError(document.getElementById(ROOM_CODE_INPUT_ID));
        alert('Please enter a valid 5-letter code.');
    }
}

export function handleRoomCodeKeypress(e) {
    if (e.key === 'Enter') {
        document.getElementById(JOIN_ROOM_BTN_ID).click();
    }
}

// Initialize event listeners if running in browser (not in test environment)
if (typeof document !== 'undefined') {
    document.addEventListener('DOMContentLoaded', () => {
        const createBtn = document.getElementById(CREATE_ROOM_BTN_ID);
        const joinBtn = document.getElementById(JOIN_ROOM_BTN_ID);
        const roomCodeInput = document.getElementById(ROOM_CODE_INPUT_ID);

        if (createBtn) createBtn.addEventListener('click', handleCreateRoom);
        if (joinBtn) joinBtn.addEventListener('click', handleJoinRoom);
        if (roomCodeInput) roomCodeInput.addEventListener('keypress', handleRoomCodeKeypress);
    });
}
