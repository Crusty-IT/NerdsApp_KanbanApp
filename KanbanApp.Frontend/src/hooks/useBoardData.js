import { useState, useEffect } from 'react';
import api from '../services/api';

export function useBoardData(boardId) {
    const [board, setBoard] = useState(null);
    const [boardMembers, setBoardMembers] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let ignore = false;
        async function fetchBoard() {
            try {
                const [boardRes, membersRes] = await Promise.all([
                    api.get(`/api/boards/${boardId}`),
                    api.get(`/api/boards/${boardId}/members`).catch(() => ({ data: [] }))
                ]);
                if (!ignore) {
                    setBoard(boardRes.data);
                    setBoardMembers(membersRes.data);
                }
            } catch {
                console.error('Failed to fetch board');
            } finally {
                if (!ignore) setLoading(false);
            }
        }
        fetchBoard();
        return () => { ignore = true; };
    }, [boardId]);

    const refreshMembers = async () => {
        const res = await api.get(`/api/boards/${boardId}/members`).catch(() => ({ data: [] }));
        setBoardMembers(res.data);
    };

    return { board, setBoard, boardMembers, loading, refreshMembers };
}