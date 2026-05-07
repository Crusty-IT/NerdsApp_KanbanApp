import { useState } from 'react';

const PRIORITY_LABELS = { 1: 'Lowest', 2: 'Low', 3: 'Medium', 4: 'High', 5: 'Highest' };
const PRIORITY_COLORS = { 1: '#6b7280', 2: '#3b82f6', 3: '#f59e0b', 4: '#ef4444', 5: '#dc2626' };

const ConfirmModal = ({ message, onConfirm, onCancel }) => (
    <div className="modal-overlay" onClick={onCancel}>
        <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '360px', textAlign: 'center' }}>
            <p style={{ fontSize: '15px', marginBottom: '20px', color: 'var(--text-primary)' }}>{message}</p>
            <div style={{ display: 'flex', gap: '8px', justifyContent: 'center' }}>
                <button className="btn-danger" onClick={onConfirm}>Delete</button>
                <button className="btn-secondary" onClick={onCancel}>Cancel</button>
            </div>
        </div>
    </div>
);

function formatDate(dateStr) {
    if (!dateStr) return null;
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

function isOverdue(dateStr) {
    if (!dateStr) return false;
    return new Date(dateStr) < new Date();
}

export default function Card({ card, isDragging, onUpdate, onDelete, boardMembers, columnColor }) {
    const [showModal, setShowModal] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [title, setTitle] = useState(card.title);
    const [description, setDescription] = useState(card.description || '');
    const [assignedTo, setAssignedTo] = useState(card.assignedToUserId || '');
    const [dueDate, setDueDate] = useState(card.dueDate ? card.dueDate.split('T')[0] : '');
    const [priority, setPriority] = useState(card.priority || null);

    const assignedMember = boardMembers?.find(m => m.userId === card.assignedToUserId);
    const assignedLabel = assignedMember?.userName || assignedMember?.email || null;
    const initials = assignedLabel ? assignedLabel.slice(0, 2).toUpperCase() : null;
    const overdue = isOverdue(card.dueDate);
    const borderColor = columnColor ? `${columnColor}33` : 'var(--border)';
    const borderLeftColor = columnColor || 'var(--border)';

    const handleSubmit = async (e) => {
        e.preventDefault();
        await onUpdate(card.id, {
            title: title.trim(),
            description: description.trim() || null,
            columnId: card.columnId,
            assignedToUserId: assignedTo || null,
            dueDate: dueDate || null,
            priority: priority || null
        });
        setShowModal(false);
    };

    const handleDelete = async () => {
        await onDelete(card.id);
        setShowConfirm(false);
        setShowModal(false);
    };

    return (
        <>
            <div
                onClick={() => setShowModal(true)}
                style={{
                    background: isDragging ? 'var(--bg-hover)' : 'var(--bg-card)',
                    border: `1px solid ${isDragging ? 'var(--accent-cyan)' : borderColor}`,
                    borderLeft: `3px solid ${borderLeftColor}`,
                    borderRadius: 'var(--radius)',
                    padding: '12px',
                    boxShadow: isDragging ? 'var(--glow-cyan)' : 'none',
                    transition: 'border-color 0.2s, box-shadow 0.2s',
                    cursor: 'pointer'
                }}
            >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '8px', marginBottom: '6px' }}>
                    <p style={{ fontSize: '14px', fontWeight: '500', color: 'var(--text-primary)', flex: 1 }}>
                        {card.title}
                    </p>
                    {card.priority && (
                        <span style={{
                            fontSize: '10px',
                            fontFamily: 'var(--font-mono)',
                            color: PRIORITY_COLORS[card.priority],
                            background: `${PRIORITY_COLORS[card.priority]}22`,
                            padding: '2px 6px',
                            borderRadius: '4px',
                            whiteSpace: 'nowrap',
                            flexShrink: 0
                        }}>
                            P{card.priority}
                        </span>
                    )}
                </div>

                {card.description && (
                    <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '8px', lineHeight: '1.5' }}>
                        {card.description}
                    </p>
                )}

                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px', flexWrap: 'wrap', gap: '6px' }}>
                    {card.dueDate && (
                        <span style={{
                            fontSize: '11px',
                            fontFamily: 'var(--font-mono)',
                            color: overdue ? 'var(--color-red)' : 'var(--color-green)',
                            background: overdue ? '#ef444422' : '#10b98122',
                            padding: '2px 6px',
                            borderRadius: '4px'
                        }}>
                            {overdue ? '⚠ ' : '📅 '}{formatDate(card.dueDate)}
                        </span>
                    )}
                    {assignedLabel && (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginLeft: 'auto' }}>
                            <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)' }}>{assignedLabel}</span>
                            <div style={{
                                width: '24px', height: '24px', borderRadius: '50%',
                                background: 'var(--accent-indigo)', color: '#fff',
                                display: 'flex', alignItems: 'center', justifyContent: 'center',
                                fontSize: '10px', fontWeight: '600', fontFamily: 'var(--font-mono)'
                            }}>
                                {initials}
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '500px' }}>
                        <h2>Edit Card</h2>
                        <form className="auth-form" onSubmit={handleSubmit}>
                            <div className="form-group">
                                <label>Title</label>
                                <input type="text" value={title} onChange={e => setTitle(e.target.value)} required autoFocus />
                            </div>
                            <div className="form-group">
                                <label>Description</label>
                                <textarea value={description} onChange={e => setDescription(e.target.value)} rows={3} style={{ resize: 'vertical' }} />
                            </div>
                            <div className="form-group">
                                <label>Priority</label>
                                <div style={{ display: 'flex', gap: '6px', marginTop: '4px' }}>
                                    <button type="button" onClick={() => setPriority(null)} style={{
                                        flex: 1, padding: '8px', fontSize: '12px', borderRadius: 'var(--radius)',
                                        border: `1px solid ${priority === null ? 'var(--accent-cyan)' : 'var(--border)'}`,
                                        background: priority === null ? 'var(--bg-hover)' : 'var(--bg-secondary)',
                                        color: priority === null ? 'var(--accent-cyan)' : 'var(--text-secondary)',
                                        cursor: 'pointer'
                                    }}>None</button>
                                    {[1, 2, 3, 4, 5].map(p => (
                                        <button key={p} type="button" onClick={() => setPriority(p)} style={{
                                            flex: 1, padding: '8px', fontSize: '12px', borderRadius: 'var(--radius)',
                                            border: `1px solid ${priority === p ? PRIORITY_COLORS[p] : 'var(--border)'}`,
                                            background: priority === p ? `${PRIORITY_COLORS[p]}22` : 'var(--bg-secondary)',
                                            color: priority === p ? PRIORITY_COLORS[p] : 'var(--text-secondary)',
                                            cursor: 'pointer', fontFamily: 'var(--font-mono)'
                                        }}>P{p}</button>
                                    ))}
                                </div>
                            </div>
                            <div className="form-group">
                                <label>Due Date</label>
                                <input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} style={{ colorScheme: 'dark' }} />
                            </div>
                            <div className="form-group">
                                <label>Assign to</label>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', marginTop: '4px' }}>
                                    <div onClick={() => setAssignedTo('')} style={{
                                        padding: '8px 12px', borderRadius: 'var(--radius)',
                                        border: `1px solid ${assignedTo === '' ? 'var(--accent-cyan)' : 'var(--border)'}`,
                                        background: assignedTo === '' ? 'var(--bg-hover)' : 'var(--bg-secondary)',
                                        cursor: 'pointer', fontSize: '13px',
                                        color: assignedTo === '' ? 'var(--accent-cyan)' : 'var(--text-secondary)'
                                    }}>Unassigned</div>
                                    {boardMembers?.map(member => (
                                        <div key={member.userId} onClick={() => setAssignedTo(member.userId)} style={{
                                            padding: '8px 12px', borderRadius: 'var(--radius)',
                                            border: `1px solid ${assignedTo === member.userId ? 'var(--accent-cyan)' : 'var(--border)'}`,
                                            background: assignedTo === member.userId ? 'var(--bg-hover)' : 'var(--bg-secondary)',
                                            cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '10px'
                                        }}>
                                            <div style={{
                                                width: '28px', height: '28px', borderRadius: '50%',
                                                background: 'var(--accent-indigo)', color: '#fff',
                                                display: 'flex', alignItems: 'center', justifyContent: 'center',
                                                fontSize: '11px', fontWeight: '600', flexShrink: 0
                                            }}>{(member.userName || member.email || '??').slice(0, 2).toUpperCase()}</div>
                                            <span style={{ fontSize: '13px', color: assignedTo === member.userId ? 'var(--accent-cyan)' : 'var(--text-primary)' }}>
                                                {member.userName || member.email}
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            </div>
                            <div className="modal-actions">
                                <button type="submit" className="btn-primary">Update</button>
                                <button type="button" className="btn-danger" onClick={() => setShowConfirm(true)}>Delete</button>
                                <button type="button" className="btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showConfirm && (
                <ConfirmModal
                    message={`Delete card "${card.title}"? This cannot be undone.`}
                    onConfirm={handleDelete}
                    onCancel={() => setShowConfirm(false)}
                />
            )}
        </>
    );
}