import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import api from '../services/api';
import Column from '../components/Column';
import InviteModal from '../components/InviteModal';
import { useTopbar } from '../context/TopbarContext';

const COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

export default function BoardView() {
    const { boardId } = useParams();
    const navigate = useNavigate();
    const [board, setBoard] = useState(null);
    const [boardMembers, setBoardMembers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [showInvite, setShowInvite] = useState(false);
    const [columnName, setColumnName] = useState('');
    const [columnColor, setColumnColor] = useState(COLORS[0]);
    const [showColumnForm, setShowColumnForm] = useState(false);
    const [isEditingColumn, setIsEditingColumn] = useState(false);
    const [editingColumn, setEditingColumn] = useState(null);
    const [showConfirmDelete, setShowConfirmDelete] = useState(false);
    const [pendingDeleteColumnId, setPendingDeleteColumnId] = useState(null);
    const [filterUserId, setFilterUserId] = useState('');
    const { setTitle, setActions } = useTopbar();

    useEffect(() => {
        return () => { setTitle(''); setActions(null); };
    }, []);

    useEffect(() => {
        if (board) {
            setTitle(board.name);
            setActions({
                left: <button className="btn-secondary" onClick={() => navigate(-1)} style={{ fontSize: '14px' }}>← Back</button>,
                right: (
                    <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                        <select
                            value={filterUserId}
                            onChange={e => setFilterUserId(e.target.value)}
                            style={{
                                background: 'var(--bg-secondary)',
                                border: '1px solid var(--border)',
                                borderRadius: 'var(--radius)',
                                color: 'var(--text-primary)',
                                padding: '6px 10px',
                                fontSize: '13px',
                                cursor: 'pointer'
                            }}
                        >
                            <option value=''>All members</option>
                            {boardMembers.map(m => (
                                <option key={m.userId} value={m.userId}>
                                    {m.userName || m.email}
                                </option>
                            ))}
                        </select>
                        <button className="btn-secondary" onClick={() => setShowInvite(true)}>👥 Invite</button>
                    </div>
                )
            });
        }
    }, [board, boardMembers, filterUserId]);

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

    const handleCreateColumn = async (e) => {
        e.preventDefault();
        if (!columnName.trim()) return;
        try {
            const position = board.columns.length;
            const response = await api.post(`/api/boards/${boardId}/columns`, {
                name: columnName.trim(), position, color: columnColor
            });
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                updated.columns.push({ ...response.data, cards: [] });
                return updated;
            });
            closeColumnForm();
        } catch {
            setError('Failed to create column');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleEditColumn = (column) => {
        setEditingColumn(column);
        setColumnName(column.name);
        setColumnColor(column.color);
        setIsEditingColumn(true);
        setShowColumnForm(true);
    };

    const handleUpdateColumn = async (e) => {
        e.preventDefault();
        if (!columnName.trim()) return;
        try {
            const response = await api.put(`/api/boards/${boardId}/columns/${editingColumn.id}`, {
                name: columnName.trim(), color: columnColor
            });
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const col = updated.columns.find(c => c.id === editingColumn.id);
                col.name = response.data.name;
                col.color = response.data.color;
                return updated;
            });
            closeColumnForm();
        } catch {
            setError('Failed to update column');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleDeleteColumn = (columnId) => {
        setPendingDeleteColumnId(columnId);
        setShowConfirmDelete(true);
    };

    const confirmDeleteColumn = async () => {
        try {
            await api.delete(`/api/boards/${boardId}/columns/${pendingDeleteColumnId}`);
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                updated.columns = updated.columns.filter(c => c.id !== pendingDeleteColumnId);
                return updated;
            });
        } catch {
            setError('Failed to delete column');
            setTimeout(() => setError(null), 3000);
        } finally {
            setShowConfirmDelete(false);
            setPendingDeleteColumnId(null);
        }
    };

    const closeColumnForm = () => {
        setShowColumnForm(false);
        setIsEditingColumn(false);
        setEditingColumn(null);
        setColumnName('');
        setColumnColor(COLORS[0]);
        setError(null);
    };

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
                const targetCard = updated.columns.flatMap(c => c.cards).find(c => c.id === cardId);
                Object.assign(targetCard, data);
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
                updated.columns.forEach(col => { col.cards = col.cards.filter(c => c.id !== cardId); });
                return updated;
            });
        } catch {
            setError('Failed to delete card');
            setTimeout(() => setError(null), 3000);
        }
    };

    const handleInvite = async (email) => {
        try {
            await api.post(`/api/boards/${boardId}/members`, { email });
            const membersRes = await api.get(`/api/boards/${boardId}/members`).catch(() => ({ data: [] }));
            setBoardMembers(membersRes.data);
            return { success: true, message: 'User invited successfully!' };
        } catch (err) {
            const msg = err.response?.data || 'Failed to invite user';
            return { success: false, message: msg };
        }
    };

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
                dueDate: card.dueDate || null, color: card.color || null
            });
        } catch {
            setBoard(previousBoard);
            setError('Failed to move card');
            setTimeout(() => setError(null), 3000);
        }
    };

    if (loading) return <div className="loading">loading board...</div>;
    if (!board) return <div className="page-content"><p>Board not found.</p></div>;

    const pendingDeleteColumn = board.columns.find(c => c.id === pendingDeleteColumnId);
    const sortedColumns = [...board.columns].sort((a, b) => a.position - b.position);

    const filteredColumns = sortedColumns.map(col => ({
        ...col,
        cards: filterUserId
            ? col.cards.filter(card => card.assignedToUserId === filterUserId)
            : col.cards
    }));

    return (
        <div style={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>
            {error && <div style={{ padding: '8px 24px' }}><span className="error-msg">{error}</span></div>}

            <div style={{ flex: 1, overflowX: 'auto', padding: '24px', display: 'flex', gap: '16px', alignItems: 'flex-start' }}>
                <DragDropContext onDragEnd={handleDragEnd}>
                    <Droppable droppableId="board" type="COLUMN" direction="horizontal">
                        {(provided) => (
                            <div
                                ref={provided.innerRef}
                                {...provided.droppableProps}
                                style={{ display: 'flex', gap: '16px', alignItems: 'flex-start' }}
                            >
                                {filteredColumns.map((column, index) => (
                                    <Column
                                        key={column.id}
                                        column={column}
                                        index={index}
                                        onCreateCard={handleCreateCard}
                                        onEdit={handleEditColumn}
                                        onDelete={handleDeleteColumn}
                                        onUpdateCard={handleUpdateCard}
                                        onDeleteCard={handleDeleteCard}
                                        boardMembers={boardMembers}
                                    />
                                ))}
                                {provided.placeholder}
                            </div>
                        )}
                    </Droppable>
                </DragDropContext>

                {showColumnForm ? (
                    <div style={{ minWidth: '260px', background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 'var(--radius-lg)', padding: '14px', flexShrink: 0 }}>
                        <h3 style={{ fontSize: '13px', marginBottom: '10px', color: 'var(--text-primary)' }}>
                            {isEditingColumn ? 'Edit Column' : 'New Column'}
                        </h3>
                        <form onSubmit={isEditingColumn ? handleUpdateColumn : handleCreateColumn} style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                            <input type="text" value={columnName} onChange={e => setColumnName(e.target.value)} placeholder="Column name..." autoFocus />
                            <div>
                                <p style={{ fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '6px', fontFamily: 'var(--font-mono)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>Color</p>
                                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                                    {COLORS.map(c => (
                                        <button key={c} type="button" onClick={() => setColumnColor(c)} style={{
                                            width: '22px', height: '22px', borderRadius: '50%', background: c,
                                            border: columnColor === c ? '3px solid #fff' : '2px solid transparent',
                                            outline: columnColor === c ? `2px solid ${c}` : 'none',
                                            padding: 0, cursor: 'pointer'
                                        }} />
                                    ))}
                                </div>
                            </div>
                            <div style={{ display: 'flex', gap: '6px' }}>
                                <button type="submit" className="btn-primary" style={{ flex: 1 }}>{isEditingColumn ? 'Update' : 'Add'}</button>
                                <button type="button" className="btn-secondary" onClick={closeColumnForm}>✕</button>
                            </div>
                        </form>
                    </div>
                ) : (
                    <button className="btn-secondary" onClick={() => setShowColumnForm(true)} style={{ minWidth: '260px', flexShrink: 0, padding: '14px' }}>
                        + Add Column
                    </button>
                )}
            </div>

            {showConfirmDelete && (
                <div className="modal-overlay" onClick={() => setShowConfirmDelete(false)}>
                    <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '360px', textAlign: 'center' }}>
                        <p style={{ fontSize: '15px', marginBottom: '20px', color: 'var(--text-primary)' }}>
                            Delete column "{pendingDeleteColumn?.name}"? This cannot be undone.
                        </p>
                        <div style={{ display: 'flex', gap: '8px', justifyContent: 'center' }}>
                            <button className="btn-danger" onClick={confirmDeleteColumn}>Delete</button>
                            <button className="btn-secondary" onClick={() => setShowConfirmDelete(false)}>Cancel</button>
                        </div>
                    </div>
                </div>
            )}

            <InviteModal isOpen={showInvite} onClose={() => setShowInvite(false)} onInvite={handleInvite} />
        </div>
    );
}