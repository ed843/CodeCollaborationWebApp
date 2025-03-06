"use strict";

document.getElementById('createRoomBtn').addEventListener('click', async () => {
    // Add loading state
    const btn = document.getElementById('createRoomBtn');
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Creating...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/room/create');
        const data = await response.json();
        window.location.href = `/Room?code=${data.roomCode}`;
    } catch (error) {
        btn.innerHTML = originalText;
        btn.disabled = false;
        alert('Failed to create room. Please try again.');
    }
});

document.getElementById('joinRoomBtn').addEventListener('click', async () => {
    const code = document.getElementById('roomCodeInput').value.toUpperCase();
    if (code.length === 5) {
        // Add loading state
        const btn = document.getElementById('joinRoomBtn');
        const originalContent = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        btn.disabled = true;

        try {
            // Check if room exists before navigating
            const response = await fetch(`/api/room/verify?code=${code}`);
            const data = await response.json();

            if (data.exists) {
                window.location.href = `/Room?code=${code}`;
            } else {
                document.getElementById('roomCodeInput').classList.add('border-red-500');
                setTimeout(() => {
                    document.getElementById('roomCodeInput').classList.remove('border-red-500');
                }, 2000);
                alert('Room not found. Please check the code and try again.');
            }
        } catch (error) {
            alert('Error verifying room. Please try again.');
        } finally {
            btn.innerHTML = originalContent;
            btn.disabled = false;
        }
    } else {
        document.getElementById('roomCodeInput').classList.add('border-red-500');
        setTimeout(() => {
            document.getElementById('roomCodeInput').classList.remove('border-red-500');
        }, 2000);
        alert('Please enter a valid 5-letter code.');
    }
});

// Also update the keypress event handler to match
document.getElementById('roomCodeInput').addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        document.getElementById('joinRoomBtn').click();
    }
});

// Also update the keypress event handler to match
document.getElementById('roomCodeInput').addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        document.getElementById('joinRoomBtn').click();
    }
});