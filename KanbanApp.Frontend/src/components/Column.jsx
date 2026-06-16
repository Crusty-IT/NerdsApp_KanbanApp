import { useState } from 'react';
import { Droppable, Draggable } from '@hello-pangea/dnd';
import Card from './Card';

export default function Column({
                                   column,
                                   index,
                                   onCreateCard,
                                   onEdit,
                                   onDelete,
                                   onUpdateCard,
                                   onDeleteCard,
                                   onUploadCardImage,
                                   onDeleteCardImage,
                                   boardMembers,
                                   editingByCard = {},
                                   onStartEditingCard,
                                   onStopEditingCard
                               }) {
    const [showForm, setShowForm] = useState(false);
    const [title, setTitle] = useState('');
    const [showConfirmClear, setShowConfirmClear] = useState(false);
    const color = column.color || '#00d4ff';

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!title.trim()) return;
        await onCreateCard(column.id, title.trim());
        setTitle('');
        setShowForm(false);
    };

    const handleDelete = () => {
        if (column.cards.length > 0) {
            setShowConfirmClear(true);
        } else {
            onDelete(column.id);
        }
    };

    return (
        <Draggable draggableId={`col-${column.id}`} index={index}>
            {(provided, snapshot) => (
                <>
                    <div
                        ref={provided.innerRef}
                        {...provided.draggableProps}
                        style={{
                            background: snapshot.isDragging ? 'var(--bg-hover)' : 'var(--bg-secondary)',
                            borderTop: `3px solid ${color}`,
                            borderRight: `1px solid ${snapshot.isDragging ? 'var(--accent-cyan)' : 'var(--border)'}`,
                            borderBottom: `1px solid ${snapshot.isDragging ? 'var(--accent-cyan)' : 'var(--border)'}`,
                            borderLeft: `1px solid ${snapshot.isDragging ? 'var(--accent-cyan)' : 'var(--border)'}`,
                            borderRadius: 'var(--radius-lg)',
                            padding: '14px',
                            minWidth: '360px',
                            maxWidth: '360px',
                            flexShrink: 0,
                            display: 'flex',
                            flexDirection: 'column',
                            gap: '10px',
                            boxShadow: snapshot.isDragging ? 'var(--glow-cyan)' : 'none',
                            ...provided.draggableProps.style
                        }}
                    >
                        <div
                            {...provided.dragHandleProps}
                            style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 2px', cursor: 'grab' }}
                        >
                            <h3 style={{
                                fontSize: '13px', fontWeight: '600', color: color,
                                textTransform: 'uppercase', letterSpacing: '0.08em',
                                fontFamily: 'var(--font-mono)', flex: 1
                            }}>
                                {column.name}
                            </h3>
                            <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                                <span style={{
                                    fontSize: '11px', fontFamily: 'var(--font-mono)',
                                    color: 'var(--text-muted)', background: 'var(--bg-hover)',
                                    padding: '2px 8px', borderRadius: '4px'
                                }}>
                                    {column.cards.length}
                                </span>
                                <button onClick={() => onEdit(column)} aria-label={`Edit column ${column.name}`} className="btn-secondary" style={{ padding: '2px 6px', fontSize: '11px' }}>✏️</button>
                                <button onClick={handleDelete} aria-label={`Delete column ${column.name}`} className="btn-danger" style={{ padding: '2px 6px', fontSize: '11px' }}>🗑️</button>
                            </div>
                        </div>

                        <Droppable droppableId={String(column.id)} type="CARD">
                            {(provided, snapshot) => (
                                <div
                                    ref={provided.innerRef}
                                    {...provided.droppableProps}
                                    style={{
                                        minHeight: '60px',
                                        background: snapshot.isDraggingOver ? 'var(--bg-hover)' : 'transparent',
                                        borderRadius: 'var(--radius)',
                                        transition: 'background 0.2s',
                                        display: 'flex',
                                        flexDirection: 'column',
                                        gap: '8px',
                                        padding: '2px'
                                    }}
                                >
                                    {column.cards.map((card, index) => (
                                        <Draggable key={card.id} draggableId={String(card.id)} index={index}>
                                            {(provided, snapshot) => (
                                                <div ref={provided.innerRef} {...provided.draggableProps} {...provided.dragHandleProps}>
                                                    <Card
                                                        card={card}
                                                        isDragging={snapshot.isDragging}
                                                        onUpdate={onUpdateCard}
                                                        onDelete={onDeleteCard}
                                                        onUploadImage={onUploadCardImage}
                                                        onDeleteImage={onDeleteCardImage}
                                                        boardMembers={boardMembers}
                                                        columnColor={color}
                                                        editingUser={editingByCard[card.id]}
                                                        onStartEditing={onStartEditingCard}
                                                        onStopEditing={onStopEditingCard}
                                                    />
                                                </div>
                                            )}
                                        </Draggable>
                                    ))}
                                    {provided.placeholder}
                                </div>
                            )}
                        </Droppable>

                        {showForm ? (
                            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                                <label htmlFor={`new-card-${column.id}`} className="sr-only">Card title</label>
                                <input id={`new-card-${column.id}`} type="text" value={title} onChange={e => setTitle(e.target.value)} placeholder="Card title..." autoFocus />
                                <div style={{ display: 'flex', gap: '6px' }}>
                                    <button type="submit" className="btn-primary" style={{ flex: 1, padding: '8px' }}>Add</button>
                                    <button type="button" className="btn-secondary" style={{ padding: '8px 12px' }} onClick={() => { setShowForm(false); setTitle(''); }}>✕</button>
                                </div>
                            </form>
                        ) : (
                            <button onClick={() => setShowForm(true)} style={{
                                background: 'none',
                                border: `1px dashed ${color}44`,
                                color: 'var(--text-secondary)',
                                padding: '8px',
                                borderRadius: 'var(--radius)',
                                fontSize: '13px',
                                width: '100%',
                                textAlign: 'center'
                            }}>
                                + Add Card
                            </button>
                        )}
                    </div>

                    {showConfirmClear && (
                        <div className="modal-overlay" onClick={() => setShowConfirmClear(false)}>
                            <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '380px', textAlign: 'center' }}>
                                <p style={{ fontSize: '15px', marginBottom: '8px', color: 'var(--text-primary)' }}>
                                    Column "{column.name}" has {column.cards.length} card(s).
                                </p>
                                <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginBottom: '20px' }}>
                                    You must remove all cards before deleting the column. Clear all cards first?
                                </p>
                                <div style={{ display: 'flex', gap: '8px', justifyContent: 'center' }}>
                                    <button className="btn-danger" onClick={async () => {
                                        for (const card of column.cards) {
                                            await onDeleteCard(card.id);
                                        }
                                        setShowConfirmClear(false);
                                        onDelete(column.id);
                                    }}>
                                        Clear All & Delete Column
                                    </button>
                                    <button className="btn-secondary" onClick={() => setShowConfirmClear(false)}>Cancel</button>
                                </div>
                            </div>
                        </div>
                    )}
                </>
            )}
        </Draggable>
    );
}
