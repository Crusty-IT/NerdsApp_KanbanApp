import api from '../services/api';
import { useTopbar } from '../context/TopbarContext';
import { useSignalREvents } from './useSignalREvents';
import { getCurrentUserId } from '../services/signalr';

export function useCards(boardId, board, setBoard, setError, connection) {
    const { fetchNotifications } = useTopbar();

    useSignalREvents({
        connection,
        eventName: 'CardMoved',
        handler: ({ cardId, toColumnId, newPosition, movedByUserId }) => {
            if (movedByUserId && movedByUserId === getCurrentUserId()) return;
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const toCol = updated.columns.find(c => c.id === toColumnId);
                if (!toCol) return prev;

                let movedCard = null;
                updated.columns.forEach(col => {
                    const idx = col.cards.findIndex(c => c.id === cardId);
                    if (idx === -1) return;
                    const [card] = col.cards.splice(idx, 1);
                    movedCard = card;
                    col.cards.forEach((c, i) => { c.position = i; });
                });

                if (!movedCard) return prev;
                movedCard.columnId = toColumnId;
                const insertAt = Math.max(0, Math.min(newPosition, toCol.cards.length));
                toCol.cards.splice(insertAt, 0, movedCard);
                toCol.cards.forEach((c, i) => { c.position = i; });
                return updated;
            });
        },
    });

    useSignalREvents({
        connection,
        eventName: 'CardCreated',
        handler: ({ card, columnId }) => {
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const col = updated.columns.find(c => c.id === columnId);
                if (!col) return prev;
                const existingIdx = col.cards.findIndex(c => c.id === card.id);
                if (existingIdx !== -1) {
                    col.cards[existingIdx] = { ...col.cards[existingIdx], ...card };
                } else {
                    col.cards.push(card);
                }
                return updated;
            });
        },
    });

    useSignalREvents({
        connection,
        eventName: 'CardUpdated',
        handler: ({ card }) => {
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                let target = null;
                updated.columns.forEach(col => {
                    const idx = col.cards.findIndex(c => c.id === card.id);
                    if (idx === -1) return;
                    const [existing] = col.cards.splice(idx, 1);
                    target = { ...existing, ...card };
                    col.cards.forEach((c, i) => { c.position = i; });
                });

                const targetColumn = updated.columns.find(c => c.id === card.columnId);
                if (!target || !targetColumn) return prev;

                const insertAt = Math.max(0, Math.min(target.position ?? targetColumn.cards.length, targetColumn.cards.length));
                targetColumn.cards.splice(insertAt, 0, target);
                targetColumn.cards.forEach((c, i) => { c.position = i; });
                return updated;
            });
        },
    });

    useSignalREvents({
        connection,
        eventName: 'CardDeleted',
        handler: ({ cardId }) => {
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                updated.columns.forEach(col => {
                    col.cards = col.cards.filter(c => c.id !== cardId);
                });
                return updated;
            });
        },
    });

    useSignalREvents({
        connection,
        eventName: 'CardAssigned',
        handler: ({ cardId, assignedToUserId }) => {
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const card = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                if (card) card.assignedToUserId = assignedToUserId;
                return updated;
            });
        },
    });

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
            if (!card) {
                setError('Card not found');
                setTimeout(() => setError(null), 3000);
                return;
            }

            const payload = {
                title: data.title,
                description: data.description,
                columnId: data.columnId ?? card.columnId,
                assignedToUserId: data.assignedToUserId,
                dueDate: data.dueDate,
                priority: data.priority,
                position: data.position ?? card.position
            };

            const response = await api.put(`/api/boards/${boardId}/cards/${cardId}`, payload);

            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const target = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                if (target) Object.assign(target, response.data);
                return updated;
            });
        } catch (err) {
            console.error('Update card error:', err);
            setError(err.response?.data?.message || 'Failed to update card');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleUploadCardImage = async (cardId, file, position) => {
        try {
            const formData = new FormData();
            formData.append('file', file);
            const qs = position ? `?position=${encodeURIComponent(position)}` : '';
            const response = await api.post(`/api/boards/${boardId}/cards/${cardId}/images${qs}`, formData);
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const card = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                if (card) {
                    card.images = [...(card.images || []), response.data];
                }
                return updated;
            });
            return response.data;
        } catch (err) {
            setError(err.response?.data || 'Failed to upload image');
            setTimeout(() => setError(null), 3000);
            return null;
        }
    };

    const handleDeleteCardImage = async (cardId, imageId) => {
        try {
            await api.delete(`/api/boards/${boardId}/cards/${cardId}/images/${imageId}`);
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const card = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                if (card) {
                    card.images = (card.images || []).filter(image => image.id !== imageId);
                }
                return updated;
            });
        } catch {
            setError('Failed to delete image');
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

    return { handleCreateCard, handleUpdateCard, handleDeleteCard, handleAssignCard, handleUploadCardImage, handleDeleteCardImage };
}
