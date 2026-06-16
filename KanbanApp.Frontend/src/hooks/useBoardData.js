import { useState, useEffect } from 'react';
import api from '../services/api';

export function useBoardData(boardId) {
    const [board, setBoard] = useState(null);
    const [boardMembers, setBoardMembers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [fetchError, setFetchError] = useState(false);

    useEffect(() => {
        let ignore = false;
        async function fetchBoard() {
            setLoading(true);
            setFetchError(false);
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
                if (!ignore) {
                    setBoard(null);
                    setFetchError(true);
                }
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

    return { board, setBoard, boardMembers, loading, fetchError, refreshMembers };
}
