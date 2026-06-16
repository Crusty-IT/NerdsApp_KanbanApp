import { useCallback, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import api from '../services/api';
import Column from '../components/Column';
import ColumnForm from '../components/ColumnForm';
import InviteModal from '../components/InviteModal';
import { useBoardData } from '../hooks/useBoardData';
import { useColumns } from '../hooks/useColumns';
import { useCards } from '../hooks/useCards';
import { useCardSearch } from '../hooks/useCardSearch';
import { useDragDrop } from '../hooks/useDragDrop';
import { useBoardTopbar } from '../hooks/useBoardTopbar';
import { useSignalR } from '../hooks/useSignalR';
import { useSignalREvents } from '../hooks/useSignalREvents';
import { getCurrentUserId } from '../services/signalr';

export default function BoardView() {
    const { boardId } = useParams();
    const navigate = useNavigate();
    const [showInvite, setShowInvite] = useState(false);
    const [showMembers, setShowMembers] = useState(false);
    const [projectMembers, setProjectMembers] = useState([]);
    const [error, setError] = useState(null);
    const [filterUserId, setFilterUserId] = useState('');
    const [presenceState, setPresenceState] = useState({ boardId: null, users: [] });
    const [editingState, setEditingState] = useState({ boardId: null, byCard: {} });

    const { board, setBoard, boardMembers, loading, fetchError, refreshMembers } = useBoardData(boardId);
    const { connection } = useSignalR(boardId);
    const { handleDragEnd } = useDragDrop(boardId, board, setBoard, setError);
    const columns = useColumns(boardId, board, setBoard, connection);
    const { handleCreateCard, handleUpdateCard, handleDeleteCard, handleAssignCard, handleUploadCardImage, handleDeleteCardImage } = useCards(boardId, board, setBoard, setError, connection);
    const { searchQuery, setSearchQuery, searchResults, handleSearch, clearSearch } = useCardSearch(boardId, setError);
    const currentUserId = getCurrentUserId();
    const projectId = board?.projectId;
    const presenceUsers = presenceState.boardId === boardId ? presenceState.users : [];
    const editingByCard = editingState.boardId === boardId ? editingState.byCard : {};

    useSignalREvents({
        connection,
        eventName: 'PresenceUpdated',
        handler: (users) => {
            setPresenceState({ boardId, users: Array.isArray(users) ? users : [] });
        },
    });

    useSignalREvents({
        connection,
        eventName: 'CardEditingStarted',
        handler: ({ cardId, userId, userName }) => {
            if (userId && userId === currentUserId) return;
            setEditingState(prev => {
                const byCard = prev.boardId === boardId ? prev.byCard : {};
                return {
                    boardId,
                    byCard: {
                        ...byCard,
                        [cardId]: { userId, userName: userName || 'Someone' }
                    }
                };
            });
        },
    });

    useSignalREvents({
        connection,
        eventName: 'CardEditingStopped',
        handler: ({ cardId, userId }) => {
            setEditingState(prev => {
                if (prev.boardId !== boardId) return prev;
                if (!prev.byCard[cardId] || (userId && prev.byCard[cardId].userId !== userId)) return prev;
                const next = { ...prev.byCard };
                delete next[cardId];
                return { boardId, byCard: next };
            });
        },
    });

    const startEditingCard = useCallback((cardId) => {
        connection.invoke('StartEditingCard', String(boardId), cardId).catch(() => {});
    }, [boardId, connection]);

    const stopEditingCard = useCallback((cardId) => {
        connection.invoke('StopEditingCard', String(boardId), cardId).catch(() => {});
    }, [boardId, connection]);

    const handleInvite = async (email) => {
        try {
            await api.post(`/api/boards/${boardId}/members`, { email });
            await refreshMembers();
            return { success: true, message: 'User invited successfully!' };
        } catch (err) {
            return { success: false, message: err.response?.data || 'Failed to invite user' };
        }
    };

    const fetchProjectMembers = useCallback(async () => {
        if (!projectId) return;
        try {
            const response = await api.get(`/api/projects/${projectId}/members`);
            setProjectMembers(response.data);
        } catch (err) {
            console.error('Failed to fetch project members', err);
        }
    }, [projectId]);

    const openMembers = useCallback(() => {
        setShowMembers(true);
        fetchProjectMembers();
    }, [fetchProjectMembers]);

    const handleRemoveMember = async (memberId) => {
        if (!projectId) return;
        try {
            await api.delete(`/api/projects/${projectId}/members/${memberId}`);
            setProjectMembers(prev => prev.filter(m => m.userId !== memberId));
        } catch {
            setError('Failed to remove member');
        }
    };

    useBoardTopbar({
        board,
        boardMembers,
        filterUserId,
        setFilterUserId,
        searchQuery,
        setSearchQuery,
        handleSearch,
        clearSearch,
        navigate,
        setShowInvite,
        setShowMembers: openMembers,
        presenceUsers
    });

    if (loading) return <div className="loading">Loading board...</div>;
    if (fetchError) return (
        <div className="page-content">
            <p className="error-msg">Failed to load board. Please refresh the page or try again later.</p>
        </div>
    );
    if (!board) return <div className="page-content"><p>Board not found.</p></div>;

    const sortedColumns = [...board.columns].sort((a, b) => a.position - b.position);
    const filteredColumns = sortedColumns.map(col => ({
        ...col,
        cards: col.cards.filter(card => {
            const matchesUser = filterUserId ? card.assignedToUserId === filterUserId : true;
            const matchesSearch = searchResults ? searchResults.some(r => r.id === card.id) : true;
            return matchesUser && matchesSearch;
        })
    }));

    const pendingDeleteColumn = board.columns.find(c => c.id === columns.pendingDeleteColumnId);

    return (
        <div className="fade-in" style={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>
            {error && <div style={{ padding: '8px 24px' }}><span className="error-msg">{error}</span></div>}

            {searchResults && (
                <div style={{ padding: '8px 24px', background: 'var(--bg-secondary)', borderBottom: '1px solid var(--border)', fontSize: '13px', color: 'var(--text-secondary)', display: 'flex', gap: '12px', alignItems: 'center' }}>
                    <span>🔍 Found <strong style={{ color: 'var(--text-primary)' }}>{searchResults.length}</strong> card(s) for "{searchQuery}"</span>
                    <button onClick={clearSearch} style={{ fontSize: '12px', color: 'var(--accent-cyan)', background: 'none', border: 'none', cursor: 'pointer' }}>Clear search</button>
                </div>
            )}

            {board.columns.length === 0 && (
                <div className="empty-state" style={{ margin: '40px 24px' }}>
                    <span className="empty-icon">📋</span>
                    <p>No columns yet. Add your first column to get started.</p>
                </div>
            )}

            <div style={{ flex: 1, overflowX: 'auto', padding: '24px', display: 'flex', gap: '16px', alignItems: 'flex-start' }}>
                <DragDropContext onDragEnd={handleDragEnd}>
                    <Droppable droppableId="board" type="COLUMN" direction="horizontal">
                        {(provided) => (
                            <div ref={provided.innerRef} {...provided.droppableProps} style={{ display: 'flex', gap: '16px', alignItems: 'flex-start' }}>
                                {filteredColumns.map((column, index) => (
                                    <Column
                                        key={column.id}
                                        column={column}
                                        index={index}
                                        onCreateCard={handleCreateCard}
                                        onEdit={columns.handleEditColumn}
                                        onDelete={columns.handleDeleteColumn}
                                        onUpdateCard={handleUpdateCard}
                                        onDeleteCard={handleDeleteCard}
                                        onUploadCardImage={handleUploadCardImage}
                                        onDeleteCardImage={handleDeleteCardImage}
                                        onAssignCard={handleAssignCard}
                                        boardMembers={boardMembers}
                                        editingByCard={editingByCard}
                                        onStartEditingCard={startEditingCard}
                                        onStopEditingCard={stopEditingCard}
                                    />
                                ))}
                                {provided.placeholder}
                            </div>
                        )}
                    </Droppable>
                </DragDropContext>

                {columns.showColumnForm ? (
                    <ColumnForm
                        columnName={columns.columnName}
                        setColumnName={columns.setColumnName}
                        columnColor={columns.columnColor}
                        setColumnColor={columns.setColumnColor}
                        isEditing={columns.isEditingColumn}
                        onSubmit={columns.isEditingColumn ? columns.handleUpdateColumn : columns.handleCreateColumn}
                        onClose={columns.closeColumnForm}
                        COLORS={columns.COLORS}
                    />
                ) : (
                    <button className="btn-secondary" onClick={() => columns.setShowColumnForm(true)} style={{ minWidth: '360px', flexShrink: 0, padding: '14px' }}>
                        + Add Column
                    </button>
                )}
            </div>

            {columns.showConfirmDelete && (
                <div className="modal-overlay" onClick={() => columns.setShowConfirmDelete(false)}>
                    <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '360px', textAlign: 'center' }}>
                        <p style={{ fontSize: '15px', marginBottom: '20px', color: 'var(--text-primary)' }}>
                            Delete column "{pendingDeleteColumn?.name}"? This cannot be undone.
                        </p>
                        <div style={{ display: 'flex', gap: '8px', justifyContent: 'center' }}>
                            <button className="btn-danger" onClick={columns.confirmDeleteColumn}>Delete</button>
                            <button className="btn-secondary" onClick={() => columns.setShowConfirmDelete(false)}>Cancel</button>
                        </div>
                    </div>
                </div>
            )}

            <InviteModal isOpen={showInvite} onClose={() => setShowInvite(false)} onInvite={handleInvite} />

            {showMembers && projectId && (
                <div className="modal-overlay" onClick={() => setShowMembers(false)}>
                    <div className="modal-box" onClick={e => e.stopPropagation()}>
                        <h2>Project Members</h2>
                        {projectMembers.length === 0 ? (
                            <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>No members yet.</p>
                        ) : (
                            <ul style={{ listStyle: 'none', padding: 0, margin: '12px 0', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                {projectMembers.map(m => (
                                    <li key={m.userId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '10px' }}>
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                                            {m.profilePictureUrl
                                                ? <img src={m.profilePictureUrl} alt="" style={{ width: '32px', height: '32px', borderRadius: '50%', objectFit: 'cover' }} />
                                                : <div style={{ width: '32px', height: '32px', borderRadius: '50%', background: 'var(--surface-2)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '14px' }}>👤</div>
                                            }
                                            <div>
                                                <div style={{ fontSize: '13px', fontWeight: 500 }}>{m.userName}</div>
                                                <div style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>{m.role}</div>
                                            </div>
                                        </div>
                                        {board.isOwner && (
                                            <button className="btn-danger" onClick={() => handleRemoveMember(m.userId)} style={{ padding: '2px 8px', fontSize: '12px' }}>Remove</button>
                                        )}
                                    </li>
                                ))}
                            </ul>
                        )}
                        <div className="modal-actions">
                            <button className="btn-secondary" onClick={() => setShowMembers(false)}>Close</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
