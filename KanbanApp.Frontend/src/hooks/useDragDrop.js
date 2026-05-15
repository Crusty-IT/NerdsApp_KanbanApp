import api from '../services/api';

export function useDragDrop(boardId, board, setBoard, setError) {
    const handleDragEnd = async (result) => {
        const { source, destination, draggableId, type } = result;
        if (!destination) return;
        if (source.droppableId === destination.droppableId && source.index === destination.index) return;

        if (type === 'COLUMN') {
            const previousBoard = JSON.parse(JSON.stringify(board));
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const sorted = [...updated.columns].sort((a, b) => a.position - b.position);
                const [moved] = sorted.splice(source.index, 1);
                sorted.splice(destination.index, 0, moved);
                sorted.forEach((col, i) => { col.position = i; });
                updated.columns = sorted;
                return updated;
            });
            try {
                const colId = parseInt(draggableId.replace('col-', ''));
                await api.put(`/api/boards/${boardId}/columns/${colId}`, {
                    name: previousBoard.columns.find(c => c.id === colId).name,
                    color: previousBoard.columns.find(c => c.id === colId).color,
                    position: destination.index
                });
            } catch {
                setBoard(previousBoard);
                setError('Failed to reorder columns');
                setTimeout(() => setError(null), 3000);
            }
            return;
        }

        const cardId = parseInt(draggableId);
        const sourceColumnId = parseInt(source.droppableId);
        const destColumnId = parseInt(destination.droppableId);
        const previousBoard = JSON.parse(JSON.stringify(board));

        setBoard(prev => {
            const updated = JSON.parse(JSON.stringify(prev));
            const sourceCol = updated.columns.find(c => c.id === sourceColumnId);
            const destCol = updated.columns.find(c => c.id === destColumnId);
            const [movedCard] = sourceCol.cards.splice(sourceCol.cards.findIndex(c => c.id === cardId), 1);
            destCol.cards.splice(destination.index, 0, movedCard);
            return updated;
        });

        const card = previousBoard.columns.find(c => c.id === sourceColumnId).cards.find(c => c.id === cardId);
        try {
            await api.put(`/api/boards/${boardId}/cards/${cardId}`, {
                title: card.title, description: card.description, columnId: destColumnId,
                assignedToUserId: card.assignedToUserId || null, dueDate: card.dueDate || null, priority: card.priority || null
            });
        } catch {
            setBoard(previousBoard);
            setError('Failed to move card');
            setTimeout(() => setError(null), 3000);
        }
    };

    return { handleDragEnd };
}
