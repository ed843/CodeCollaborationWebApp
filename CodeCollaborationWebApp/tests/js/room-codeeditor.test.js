import { CONFIG, EditorModule } from '../../wwwroot/js/room-codeeditor';

describe('CONFIG', () => {
    test('should have correct editor configuration', () => {
        expect(CONFIG.editor).toEqual({
            defaultValue: '// Type your code here...',
            defaultLanguage: 'javascript',
            theme: 'vs-dark',
            fontSize: 14,
            tabSize: 2
        });
    });

    test('should have correct output configuration', () => {
        expect(CONFIG.output).toEqual({
            maxSize: 10000
        });
    });
});
// Mock dependencies

