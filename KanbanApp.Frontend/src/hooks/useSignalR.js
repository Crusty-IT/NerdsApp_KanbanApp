import { useState, useEffect } from 'react';
import { HubConnectionState } from '@microsoft/signalr';
import { getConnection, subscribeConnectionState, trackBoard, untrackBoard } from '../services/signalr';

export function useSignalR(boardId) {
    const connection = getConnection();
    const [connectionState, setConnectionState] = useState(connection.state);

    useEffect(() => {
        const unsubscribe = subscribeConnectionState(setConnectionState);
        return unsubscribe;
    }, []);

    useEffect(() => {
        if (!boardId) return;
        let active = true;

        const start = async () => {
            if (connection.state === HubConnectionState.Disconnected) {
                await connection.start();
            } else if (connection.state === HubConnectionState.Connecting) {
                await new Promise(resolve => {
                    const interval = setInterval(() => {
                        if (connection.state !== HubConnectionState.Connecting) {
                            clearInterval(interval);
                            resolve();
                        }
                    }, 50);
                });
            }
            if (!active || connection.state !== HubConnectionState.Connected) return;
            setConnectionState(HubConnectionState.Connected);
            trackBoard(boardId);
            await connection.invoke('JoinBoard', String(boardId));
        };

        start().catch(() => {});

        return () => {
            active = false;
            untrackBoard(boardId);
            connection.invoke('LeaveBoard', String(boardId)).catch(() => {});
        };
    }, [boardId]);

    return {
        connection,
        isConnected: connectionState === HubConnectionState.Connected,
        connectionState,
    };
}
