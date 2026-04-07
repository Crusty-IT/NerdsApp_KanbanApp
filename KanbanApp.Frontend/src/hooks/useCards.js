import api from '../services/api';
import { useTopbar } from '../context/TopbarContext';

export function useCards(boardId, board, setBoard, setError) {
    const { fetchNotifications } = useTopbar();

    const handleCreateCard = async (columnId, title) => {
        try {
            const response = await api.post(`/api/boards/${boardId}/cards`, { title, columnId });
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const col = updated.columns.find(c => c.id === columnId);
                col.cards.push(response.data);
                return updated;
            });
        } catch {
            setError('Failed to create card');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleUpdateCard = async (cardId, data) => {
        try {
            const card = board.columns.flatMap(c => c.cards).find(c => c.id === cardId);
            await api.put(`/api/boards/${boardId}/cards/${cardId}`, { ...data, columnId: card.columnId });
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const target = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                Object.assign(target, data);
                return updated;
            });
        } catch {
            setError('Failed to update card');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleDeleteCard = async (cardId) => {
        try {
            await api.delete(`/api/boards/${boardId}/cards/${cardId}`);
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                updated.columns.forEach(col => {
                    col.cards = col.cards.filter(c => c.id !== cardId);
                });
                return updated;
            });
        } catch {
            setError('Failed to delete card');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleAssignCard = async (cardId, userId) => {
        try {
            await api.put(`/api/boards/${boardId}/cards/${cardId}/assign`, { userId });
            await fetchNotifications();
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const card = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                if (card) card.assignedToUserId = userId;
                return updated;
            });
        } catch {
            setError('Failed to assign card');
            setTimeout(() => setError(null), 3000);
        }
    };

    return { handleCreateCard, handleUpdateCard, handleDeleteCard, handleAssignCard };
}