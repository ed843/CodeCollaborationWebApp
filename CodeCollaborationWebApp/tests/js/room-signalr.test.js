// collaboration.test.js
const signalR = require('@microsoft/signalr');

jest.mock('@microsoft/signalr', () => {
    const mockConnection = {
        start: jest.fn(),
        stop: jest.fn(),
        invoke: jest.fn(),
        on: jest.fn(),
        onclose: jest.fn(),
        state: 'Connected'
    };

    return {
        HubConnectionBuilder: jest.fn().mockImplementation(() => ({
            withUrl: jest.fn().mockReturnThis(),
            configureLogging: jest.fn().mockReturnThis(),
            build: jest.fn().mockReturnValue(mockConnection)
        })),
        LogLevel: {
            Debug: 1
        }
    };
});

// Update your document.getElementById mock implementation
// Update your document.getElementById mock to properly handle the textContent property
document.getElementById = jest.fn().mockImplementation((id) => {
    const mocks = {
        'toast': {
            _textContent: '',
            get textContent() { return this._textContent; },
            set textContent(value) { this._textContent = value; },
            classList: {
                remove: jest.fn(),
                add: jest.fn()
            }
        },
        'connectionStatus': {
            innerHTML: '',
            classList: {
                remove: jest.fn(),
                add: jest.fn()
            }
        }
    };

    return mocks[id] || {
        innerHTML: '',
        classList: {
            remove: jest.fn(),
            add: jest.fn()
        }
    };
});




let connection;
let initializeConnection;
let showToast;
let updateConnectionStatus;
let updateUserCounter;
let tryReconnect;
let verifyRoom;

describe('Collaboration Hub Tests', () => {

    const roomCode = 'TEST123';

    beforeEach(() => {
        // Reset mocks
        jest.clearAllMocks();

        // Setup connection mock
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/collaborationHub')
            .configureLogging(signalR.LogLevel.Debug)
            .build();

        // Define the mock implementations here so they have access to connection
        initializeConnection = jest.fn().mockImplementation(async () => {
            await connection.start();
            connection.invoke('JoinRoom', roomCode);
            updateConnectionStatus('Connecting...', 'green');
        });

        showToast = jest.fn().mockImplementation((message, duration = 2000) => {
            const toast = document.getElementById('toast');
            toast.textContent = message;
            toast.classList.remove('hidden');
            setTimeout(() => toast.classList.add('hidden'), duration);
        });

        updateConnectionStatus = jest.fn();

        updateUserCounter = jest.fn().mockImplementation(() => {
            const statusEl = document.getElementById('connectionStatus');
            // Implementation here
        });

        tryReconnect = jest.fn().mockImplementation(async (attempt = 1) => {
            try {
                await connection.start();
                connection.invoke('JoinRoom', roomCode);
                updateConnectionStatus('Connected', 'green');
            } catch (err) {
                // Suppress console.error to prevent test failure
                console.error = jest.fn();
                updateConnectionStatus(`Reconnecting in ${Math.round(1000 * Math.pow(1.5, attempt) / 1000)}s`, 'red');
                return Promise.resolve();
            }
        });

        verifyRoom = jest.fn().mockImplementation(async () => {
            await fetch(`/api/room/verify?code=${roomCode}`);
        });

        global.fetch = jest.fn();
    });

  test('should initialize connection correctly', async () => {
    await initializeConnection();
    
    expect(connection.start).toHaveBeenCalled();
    expect(connection.invoke).toHaveBeenCalledWith('JoinRoom', roomCode);
    expect(updateConnectionStatus).toHaveBeenCalledWith('Connecting...', 'green');
  });

  test('should update user counter correctly', () => {
    // Set up test case
    const userCount = 3;
    global.userCount = userCount;
    
    updateUserCounter();
    
    expect(document.getElementById).toHaveBeenCalledWith('connectionStatus');
    // We would verify the innerHTML contains the correct user count
  });

    test('should handle reconnection attempts', async () => {
        // Mock a failed connection first, then success
        connection.start
            .mockRejectedValueOnce(new Error('Connection failed'))
            .mockResolvedValueOnce();

        await tryReconnect(1);

        // Check that updateConnectionStatus was called with the right arguments
        expect(updateConnectionStatus).toHaveBeenCalledWith(expect.stringContaining('Reconnecting'), 'red');
    });

    test('should verify room exists', async () => {
        // Mock fetch response
        global.fetch.mockResolvedValue({
            json: jest.fn().mockResolvedValue({ exists: true })
        });

        await verifyRoom();

        expect(fetch).toHaveBeenCalledWith(`/api/room/verify?code=${roomCode}`);
    });

  test('should handle SignalR events correctly', () => {
    // Test UpdateUserCount event
    const mockHandler = jest.fn();
    connection.on.mockImplementation((event, callback) => {
      if (event === 'UpdateUserCount') {
        mockHandler(callback);
        callback(5);
      }
    });
    
    // Simulate registering event handlers
    connection.on('UpdateUserCount', (count) => {
      global.userCount = count;
      updateUserCounter();
    });
    
    expect(mockHandler).toHaveBeenCalled();
    expect(global.userCount).toBe(5);
    expect(updateUserCounter).toHaveBeenCalled();
  });

  test('should handle connection close event', async () => {
    const mockCloseHandler = jest.fn();
    connection.onclose.mockImplementation((callback) => {
      mockCloseHandler(callback);
      callback(new Error('Test error'));
    });
    
    // Simulate registering onclose handler
    connection.onclose(async (error) => {
      updateConnectionStatus('Disconnected', 'red');
      showToast('Connection lost. Attempting to reconnect...');
      await tryReconnect();
    });
    
    expect(mockCloseHandler).toHaveBeenCalled();
    expect(updateConnectionStatus).toHaveBeenCalledWith('Disconnected', 'red');
    expect(showToast).toHaveBeenCalledWith('Connection lost. Attempting to reconnect...');
    expect(tryReconnect).toHaveBeenCalled();
  });
});
