import '@testing-library/jest-dom';
import { vi } from 'vitest';

vi.mock('../services/api', () => ({
    default: {
        get: vi.fn().mockResolvedValue({ data: [] }),
        post: vi.fn().mockResolvedValue({ data: {} }),
        put: vi.fn().mockResolvedValue({ data: {} }),
        delete: vi.fn().mockResolvedValue({ data: {} }),
    },
    BASE_URL: 'http://localhost:5067',
}));

const mockConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    off: vi.fn(),
    invoke: vi.fn().mockResolvedValue(undefined),
    onreconnected: vi.fn(),
    state: 'Disconnected',
};

vi.mock('../services/signalr', () => ({
    getConnection: vi.fn(() => mockConnection),
    subscribeConnectionState: vi.fn(() => () => {}),
    trackBoard: vi.fn(),
    untrackBoard: vi.fn(),
    getCurrentUserId: vi.fn(() => null),
}));

vi.mock('../hooks/useSignalREvents', () => ({
    useSignalREvents: vi.fn(),
}));

vi.mock('../hooks/useSignalR', () => ({
    useSignalR: vi.fn(() => ({ connection: mockConnection })),
}));

Object.defineProperty(window, 'localStorage', {
    value: {
        getItem: vi.fn(() => 'mock-token'),
        setItem: vi.fn(),
        removeItem: vi.fn(),
        clear: vi.fn(),
    },
    writable: true,
});
