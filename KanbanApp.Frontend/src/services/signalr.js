import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { BASE_URL } from './api';

let _connection = null;
const _trackedBoards = new Set();
const _stateListeners = new Set();

export function getConnection() {
    if (!_connection) {
        _connection = new HubConnectionBuilder()
            .withUrl(`${BASE_URL}/hubs/kanban`, {
                accessTokenFactory: () => localStorage.getItem('token'),
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        _connection.onreconnecting(() => {
            _stateListeners.forEach(l => l(HubConnectionState.Reconnecting));
        });

        _connection.onreconnected(async () => {
            _stateListeners.forEach(l => l(HubConnectionState.Connected));
            for (const boardId of _trackedBoards) {
                try {
                    await _connection.invoke('JoinBoard', boardId);
                } catch {
                    continue;
                }
            }
        });
    }
    return _connection;
}

export function subscribeConnectionState(listener) {
    _stateListeners.add(listener);
    return () => _stateListeners.delete(listener);
}

export function trackBoard(boardId) {
    _trackedBoards.add(String(boardId));
}

export function untrackBoard(boardId) {
    _trackedBoards.delete(String(boardId));
}

export function getCurrentUserId() {
    const token = localStorage.getItem('token');
    if (!token) return null;
    try {
        const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
        const payload = JSON.parse(atob(base64));
        return payload.sub
            ?? payload.nameid
            ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
            ?? null;
    } catch { return null; }
}
