module.exports = {
    testEnvironment: 'jsdom',
    moduleFileExtensions: ['js', 'jsx'],
    moduleDirectories: ['node_modules', 'wwwroot/js'],
    testMatch: ['**/tests/**/*.test.js'],
    transform: {
        '^.+\\.jsx?$': 'babel-jest'
    },
    setupFiles: [
        "jest-canvas-mock"
    ],
    // Add these for better VS integration
    verbose: true,
    testTimeout: 30000
};