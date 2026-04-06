import { useState } from 'react';

const COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

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

export default function Card({ card, isDragging, onUpdate, onDelete, boardMembers }) {
    const [showModal, setShowModal] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [title, setTitle] = useState(card.title);
    const [description, setDescription] = useState(card.description || '');
    const [assignedTo, setAssignedTo] = useState(card.assignedToUserId || '');
    const [dueDate, setDueDate] = useState(card.dueDate ? card.dueDate.split('T')[0] : '');
    const [color, setColor] = useState(card.color || '');

    const assignedMember = boardMembers?.find(m => m.userId === card.assignedToUserId);
    const assignedLabel = assignedMember?.email || assignedMember?.userName || null;
    const initials = assignedLabel ? assignedLabel.slice(0, 2).toUpperCase() : null;
    const overdue = isOverdue(card.dueDate);
    const cardColor = card.color || null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        await onUpdate(card.id, {
            title: title.trim(),
            description: description.trim(),
            assignedToUserId: assignedTo || null,
            dueDate: dueDate || null,
            color: color || null
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
                    border: `1px solid ${isDragging ? 'var(--accent-cyan)' : cardColor ? cardColor + '66' : 'var(--border)'}`,
                    borderLeft: cardColor ? `3px solid ${cardColor}` : '1px solid var(--border)',
                    borderRadius: 'var(--radius)',
                    padding: '12px',
                    boxShadow: isDragging ? 'var(--glow-cyan)' : 'none',
                    transition: 'border-color 0.2s, box-shadow 0.2s',
                    cursor: 'pointer'
                }}
            >
                <p style={{ fontSize: '14px', fontWeight: '500', color: 'var(--text-primary)', marginBottom: card.description ? '6px' : '0' }}>
                    {card.title}
                </p>
                {card.description && (
                    <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '8px', lineHeight: '1.5' }}>
                        {card.description}
                    </p>
                )}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px', flexWrap: 'wrap', gap: '4px' }}>
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
                                <label>Due Date</label>
                                <input
                                    type="date"
                                    value={dueDate}
                                    onChange={e => setDueDate(e.target.value)}
                                    style={{ colorScheme: 'dark' }}
                                />
                            </div>
                            <div className="form-group">
                                <label>Card Color</label>
                                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px', marginTop: '4px' }}>
                                    <button type="button" onClick={() => setColor('')} style={{
                                        width: '22px', height: '22px', borderRadius: '50%',
                                        background: 'var(--bg-hover)',
                                        border: color === '' ? '3px solid #fff' : '2px solid var(--border)',
                                        outline: color === '' ? '2px solid var(--accent-cyan)' : 'none',
                                        padding: 0, cursor: 'pointer', fontSize: '10px', color: 'var(--text-secondary)'
                                    }}>✕</button>
                                    {COLORS.map(c => (
                                        <button key={c} type="button" onClick={() => setColor(c)} style={{
                                            width: '22px', height: '22px', borderRadius: '50%', background: c,
                                            border: color === c ? '3px solid #fff' : '2px solid transparent',
                                            outline: color === c ? `2px solid ${c}` : 'none',
                                            padding: 0, cursor: 'pointer'
                                        }} />
                                    ))}
                                </div>
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
                                    }}>
                                        Unassigned
                                    </div>
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
                                            }}>
                                                {(member.email || member.userName || '??').slice(0, 2).toUpperCase()}
                                            </div>
                                            <span style={{ fontSize: '13px', color: assignedTo === member.userId ? 'var(--accent-cyan)' : 'var(--text-primary)' }}>
                                                {member.email || member.userName}
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