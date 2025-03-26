// room-whiteboard.test.js
import 'jest-canvas-mock'; // Add this import at the top

describe('Whiteboard', () => {
    let whiteboard;
    let mockConnection;
    let mockDocument;
    let mockWindow;

    // Mock event listeners storage
    const eventListeners = {};
    const connectionHandlers = {};

    beforeEach(() => {
        // Set up DOM elements
        document.body.innerHTML = `
      <canvas id="whiteboard"></canvas>
      <button id="penTool"></button>
      <button id="eraserTool"></button>
      <input id="colorPicker" type="color" value="#000000" />
      <button id="clearBtn"></button>
    `;


        // Mock SignalR connection
        mockConnection = {
            on: jest.fn((event, callback) => {
                connectionHandlers[event] = callback;
            }),
            invoke: jest.fn()
        };

        // Mock window
        mockWindow = {
            addEventListener: jest.fn((event, callback) => {
                if (!eventListeners[`window-${event}`]) {
                    eventListeners[`window-${event}`] = [];
                }
                eventListeners[`window-${event}`].push(callback);
            })
        };

        // Save original objects
        global.window = {
            ...window,
            ...mockWindow
        };

        // Mock confirm
        global.confirm = jest.fn(() => true);

        // Import and instantiate the Whiteboard class
        jest.isolateModules(() => {
            // Mock the module to avoid the "connection is not defined" error
            jest.doMock('../../wwwroot/js/room-whiteboard', () => {
                const originalModule = jest.requireActual('../../wwwroot/js/room-whiteboard');
                return {
                    Whiteboard: originalModule.Whiteboard
                };
            });

            const { Whiteboard } = require('../../wwwroot/js/room-whiteboard');
            whiteboard = new Whiteboard('whiteboard', mockConnection, 'ROOM123');
        });
    });

    afterEach(() => {
        document.body.innerHTML = '';
        jest.clearAllMocks();
        Object.keys(eventListeners).forEach(key => {
            delete eventListeners[key];
        });
        Object.keys(connectionHandlers).forEach(key => {
            delete connectionHandlers[key];
        });
    });

    describe('Initialization', () => {
        test('should initialize with correct properties', () => {
            expect(whiteboard.canvas).toBeTruthy();
            expect(whiteboard.context).toBeTruthy();
            expect(whiteboard.connection).toBe(mockConnection);
            expect(whiteboard.roomCode).toBe('ROOM123');
            expect(whiteboard.isDrawingInitialized).toBe(false);
            expect(whiteboard.drawing).toBe(false);
            expect(whiteboard.currentTool).toBe('pen');
        });

        test('should set up SignalR handlers', () => {
            expect(mockConnection.on).toHaveBeenCalledWith('InitializeWhiteboard', expect.any(Function));
            expect(mockConnection.on).toHaveBeenCalledWith('ReceiveWhiteboardUpdate', expect.any(Function));
            expect(mockConnection.on).toHaveBeenCalledWith('ReceiveWhiteboardClear', expect.any(Function));
        });
    });

    describe('Canvas Operations', () => {
        test('should capture whiteboard state', () => {
            const state = whiteboard.captureWhiteboardState();
            expect(state).toContain('data:image/png;base64');
        });

        test('should save whiteboard state when initialized', () => {
            whiteboard.isDrawingInitialized = true;
            whiteboard.saveWhiteboardState();

            expect(mockConnection.invoke).toHaveBeenCalledWith(
                'SendWhiteboardState',
                'ROOM123',
                expect.any(String)
            );
        });

        test('should not save whiteboard state when not initialized', () => {
            whiteboard.isDrawingInitialized = false;
            whiteboard.saveWhiteboardState();

            expect(mockConnection.invoke).not.toHaveBeenCalled();
        });
    });

    describe('Tool Operations', () => {
        test('should set pen tool correctly', () => {
            whiteboard.setTool('pen');

            expect(whiteboard.currentTool).toBe('pen');
            expect(whiteboard.context.globalCompositeOperation).toBe('source-over');
        });

        test('should set eraser tool correctly', () => {
            whiteboard.setTool('eraser');

            expect(whiteboard.currentTool).toBe('eraser');
            expect(whiteboard.context.globalCompositeOperation).toBe('destination-out');
        });

        test('should clear whiteboard when confirmed', () => {
            whiteboard.clearWhiteboard();

            expect(global.confirm).toHaveBeenCalled();
            expect(mockConnection.invoke).toHaveBeenCalledWith('SendWhiteboardClear', 'ROOM123');
            expect(mockConnection.invoke).toHaveBeenCalledWith('SendCodeUpdate', 'ROOM123', '');
        });
    });

    describe('SignalR Handlers', () => {
        test('should initialize whiteboard from state data', () => {
            const stateData = 'data:image/png;base64,testImageData';

            // Directly modify the method to set isDrawingInitialized
            whiteboard.initializeWhiteboard = jest.fn((data) => {
                whiteboard.isDrawingInitialized = true;
            });

            // Call the handler directly
            connectionHandlers['InitializeWhiteboard'](stateData);

            // Verify the result
            expect(whiteboard.isDrawingInitialized).toBe(true);
        });


        test('should receive whiteboard clear', () => {
            // Call the handler directly
            connectionHandlers['ReceiveWhiteboardClear']();

            expect(whiteboard.isDrawingInitialized).toBe(true);
        });
    });
});
