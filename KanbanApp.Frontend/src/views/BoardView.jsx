import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import Column from '../components/Column';
import ColumnForm from '../components/ColumnForm';
import InviteModal from '../components/InviteModal';
import { useBoardData } from '../hooks/useBoardData';
import { useColumns } from '../hooks/useColumns';
import { useCards } from '../hooks/useCards';
import { useCardSearch } from '../hooks/useCardSearch';
import { useDragDrop } from '../hooks/useDragDrop';
import { useBoardTopbar } from '../hooks/useBoardTopbar';

export default function BoardView() {
    const { boardId } = useParams();
    const navigate = useNavigate();
    const [showInvite, setShowInvite] = useState(false);
    const [error, setError] = useState(null);
    const [filterUserId, setFilterUserId] = useState('');

    const { board, setBoard, boardMembers, loading, refreshMembers } = useBoardData(boardId);
    const columns = useColumns(boardId, board, setBoard);
    const { handleCreateCard, handleUpdateCard, handleDeleteCard, handleAssignCard } = useCards(boardId, board, setBoard, setError);
    const { searchQuery, setSearchQuery, searchResults, handleSearch, clearSearch } = useCardSearch(boardId, setError);
    const { handleDragEnd } = useDragDrop(boardId, board, setBoard, setError);

    const handleInvite = async (email) => {
        try {
            await import('../services/api').then(m => m.default.post(`/api/boards/${boardId}/members`, { email }));
            await refreshMembers();
            return { success: true, message: 'User invited successfully!' };
        } catch (err) {
            return { success: false, message: err.response?.data || 'Failed to invite user' };
        }
    };

    useBoardTopbar({ board, boardMembers, filterUserId, setFilterUserId, searchQuery, setSearchQuery, handleSearch, clearSearch, navigate, setShowInvite });

    if (loading) return <div className="loading">loading board...</div>;
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
        <div style={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>
            {error && <div style={{ padding: '8px 24px' }}><span className="error-msg">{error}</span></div>}

            {searchResults && (
                <div style={{ padding: '8px 24px', background: 'var(--bg-secondary)', borderBottom: '1px solid var(--border)', fontSize: '13px', color: 'var(--text-secondary)', display: 'flex', gap: '12px', alignItems: 'center' }}>
                    <span>🔍 Found <strong style={{ color: 'var(--text-primary)' }}>{searchResults.length}</strong> card(s) for "{searchQuery}"</span>
                    <button onClick={clearSearch} style={{ fontSize: '12px', color: 'var(--accent-cyan)', background: 'none', border: 'none', cursor: 'pointer' }}>Clear search</button>
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
                                        onAssignCard={handleAssignCard}
                                        boardMembers={boardMembers}
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
                    <button className="btn-secondary" onClick={() => columns.setShowColumnForm(true)} style={{ minWidth: '260px', flexShrink: 0, padding: '14px' }}>
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
        </div>
    );
}