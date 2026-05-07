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
                if (col) col.cards.push(response.data);
                return updated;
            });
        } catch (err) {
            console.error('Create card error:', err);
            setError('Failed to create card');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleUpdateCard = async (cardId, data) => {
        try {
            const card = board.columns.flatMap(c => c.cards).find(c => c.id === cardId);
            if (!card) {
                console.error('Card not found in local state:', cardId);
                setError('Card not found');
                setTimeout(() => setError(null), 3000);
                return;
            }

            const payload = {
                title: data.title,
                description: data.description || null,
                columnId: data.columnId ?? card.columnId,
                assignedToUserId: data.assignedToUserId || null,
                dueDate: data.dueDate || null,
                priority: data.priority || null
            };

            console.log('Update card payload:', { cardId, boardId, payload });

            const response = await api.put(`/api/boards/${boardId}/cards/${cardId}`, payload);

            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const target = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                if (target) {
                    Object.assign(target, {
                        ...data,
                        id: cardId,
                        columnId: payload.columnId
                    });
                }
                return updated;
            });
        } catch (err) {
            console.error('Update card error:', err);
            console.error('Error response:', err.response?.data);
            setError(err.response?.data?.message || 'Failed to update card');
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
        } catch (err) {
            console.error('Delete card error:', err);
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
        } catch (err) {
            console.error('Assign card error:', err);
            setError('Failed to assign card');
            setTimeout(() => setError(null), 3000);
        }
    };

    return { handleCreateCard, handleUpdateCard, handleDeleteCard, handleAssignCard };
}