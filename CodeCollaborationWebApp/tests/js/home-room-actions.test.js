/**
 * @jest-environment jsdom
 */

// Import functions to test (assuming you've exported them)
import {
    setButtonLoading,
    resetButton,
    highlightInputError,
    createRoom,
    verifyRoom,
    handleCreateRoom,
    handleJoinRoom,
    handleRoomCodeKeypress
} from '../../wwwroot/js/home-room-actions';

// Mock fetch API
global.fetch = jest.fn();

// Mock DOM elements
document.body.innerHTML = `
  <button id="createRoomBtn">Create Room</button>
  <button id="joinRoomBtn">Join Room</button>
  <input id="roomCodeInput" type="text" />
`;

// Mock window.location
delete window.location;
window.location = { href: '' };

// Mock alert
global.alert = jest.fn();

describe('Helper Functions', () => {
    test('setButtonLoading should update button state', () => {
        const button = document.getElementById('createRoomBtn');
        const originalText = button.innerHTML;

        const result = setButtonLoading(button, 'Loading...');

        expect(result).toBe(originalText);
        expect(button.disabled).toBe(true);
        expect(button.innerHTML).toContain('Loading...');
        expect(button.innerHTML).toContain('fa-spinner');
    });

    test('resetButton should restore button state', () => {
        const button = document.getElementById('createRoomBtn');
        const originalText = 'Create Room';

        resetButton(button, originalText);

        expect(button.innerHTML).toBe(originalText);
        expect(button.disabled).toBe(false);
    });

    test('highlightInputError should add and remove error class', () => {
        jest.useFakeTimers();
        const input = document.getElementById('roomCodeInput');

        highlightInputError(input);

        expect(input.classList.contains('border-red-500')).toBe(true);

        jest.advanceTimersByTime(2000);

        expect(input.classList.contains('border-red-500')).toBe(false);
        jest.useRealTimers();
    });
});

describe('API Functions', () => {
    beforeEach(() => {
        fetch.mockClear();
    });

    test('createRoom should call the API and return room code', async () => {
        const mockResponse = { roomCode: 'ABCDE' };
        fetch.mockResolvedValueOnce({
            json: jest.fn().mockResolvedValueOnce(mockResponse)
        });

        const result = await createRoom();

        expect(fetch).toHaveBeenCalledWith('/api/room/create');
        expect(result).toBe('ABCDE');
    });

    test('verifyRoom should call the API and return existence status', async () => {
        const mockResponse = { exists: true };
        fetch.mockResolvedValueOnce({
            json: jest.fn().mockResolvedValueOnce(mockResponse)
        });

        const result = await verifyRoom('ABCDE');

        expect(fetch).toHaveBeenCalledWith('/api/room/verify?code=ABCDE');
        expect(result).toBe(true);
    });
});

describe('Event Handlers', () => {
    beforeEach(() => {
        fetch.mockClear();
        alert.mockClear();
        window.location.href = '';
    });

    test('handleCreateRoom should redirect on success', async () => {
        const mockResponse = { roomCode: 'ABCDE' };
        fetch.mockResolvedValueOnce({
            json: jest.fn().mockResolvedValueOnce(mockResponse)
        });

        await handleCreateRoom();

        expect(fetch).toHaveBeenCalledWith('/api/room/create');
        expect(window.location.href).toBe('/Room?code=ABCDE');
    });

    test('handleCreateRoom should show alert on error', async () => {
        fetch.mockRejectedValueOnce(new Error('Network error'));

        await handleCreateRoom();

        expect(alert).toHaveBeenCalledWith('Failed to create room. Please try again.');
        expect(window.location.href).toBe('');
    });

    test('handleJoinRoom should redirect for valid existing room', async () => {
        document.getElementById('roomCodeInput').value = 'ABCDE';

        const mockResponse = { exists: true };
        fetch.mockResolvedValueOnce({
            json: jest.fn().mockResolvedValueOnce(mockResponse)
        });

        await handleJoinRoom();

        expect(fetch).toHaveBeenCalledWith('/api/room/verify?code=ABCDE');
        expect(window.location.href).toBe('/Room?code=ABCDE');
    });

    test('handleJoinRoom should show alert for non-existent room', async () => {
        document.getElementById('roomCodeInput').value = 'ABCDE';

        const mockResponse = { exists: false };
        fetch.mockResolvedValueOnce({
            json: jest.fn().mockResolvedValueOnce(mockResponse)
        });

        await handleJoinRoom();

        expect(alert).toHaveBeenCalledWith('Room not found. Please check the code and try again.');
        expect(window.location.href).toBe('');
    });

    test('handleJoinRoom should validate room code length', async () => {
        document.getElementById('roomCodeInput').value = 'ABC';

        await handleJoinRoom();

        expect(fetch).not.toHaveBeenCalled();
        expect(alert).toHaveBeenCalledWith('Please enter a valid 5-letter code.');
    });

    test('handleRoomCodeKeypress should trigger join button click on Enter', () => {
        const joinBtn = document.getElementById('joinRoomBtn');
        joinBtn.click = jest.fn();

        const enterEvent = { key: 'Enter' };
        handleRoomCodeKeypress(enterEvent);

        expect(joinBtn.click).toHaveBeenCalled();
    });

    test('handleRoomCodeKeypress should not trigger join button click on other keys', () => {
        const joinBtn = document.getElementById('joinRoomBtn');
        joinBtn.click = jest.fn();

        const otherEvent = { key: 'A' };
        handleRoomCodeKeypress(otherEvent);

        expect(joinBtn.click).not.toHaveBeenCalled();
    });
});
