import { useEffect, useRef, useState } from 'react';
import { BASE_URL } from '../services/api';
import FocalPointPicker from './FocalPointPicker';

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

function getImageSrc(url) {
    if (!url) return '';
    return url.startsWith('http') ? url : `${BASE_URL}${url}`;
}

export default function Card({
                                 card,
                                 isDragging,
                                 onUpdate,
                                 onDelete,
                                 onUploadImage,
                                 onDeleteImage,
                                 boardMembers,
                                 columnColor,
                                 editingUser,
                                 onStartEditing,
                                 onStopEditing
                             }) {
    const [showModal, setShowModal] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [lightboxImage, setLightboxImage] = useState(null);
    const [title, setTitle] = useState(card.title);
    const [description, setDescription] = useState(card.description || '');
    const [assignedTo, setAssignedTo] = useState(card.assignedToUserId || '');
    const [dueDate, setDueDate] = useState(card.dueDate ? card.dueDate.split('T')[0] : '');
    const [priority, setPriority] = useState(card.priority || null);
    const [saving, setSaving] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [pendingImageFile, setPendingImageFile] = useState(null);
    const [pendingImageSrc, setPendingImageSrc] = useState(null);
    const fileRef = useRef(null);
    const editingStartedRef = useRef(false);

    const images = card.images || [];
    const firstImage = images[0];
    const assignedMember = boardMembers?.find(m => m.userId === card.assignedToUserId);
    const assignedLabel = assignedMember?.userName || assignedMember?.email || null;
    const initials = assignedLabel ? assignedLabel.slice(0, 2).toUpperCase() : null;
    const overdue = isOverdue(card.dueDate);
    const borderColor = columnColor ? `${columnColor}33` : 'var(--border)';
    const borderLeftColor = columnColor || 'var(--border)';
    const editingLabel = editingUser?.userName || 'Someone';

    const openCardModal = () => {
        setShowModal(true);
        if (!editingStartedRef.current) {
            editingStartedRef.current = true;
            onStartEditing?.(card.id);
        }
    };

    const closeCardModal = () => {
        setShowModal(false);
        setLightboxImage(null);
        if (editingStartedRef.current) {
            editingStartedRef.current = false;
            onStopEditing?.(card.id);
        }
    };

    useEffect(() => {
        return () => {
            if (editingStartedRef.current) {
                onStopEditing?.(card.id);
            }
        };
    }, [card.id, onStopEditing]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setSaving(true);
        try {
            await onUpdate(card.id, {
                title: title.trim(),
                description: description.trim(),
                assignedToUserId: assignedTo || null,
                dueDate: dueDate || null,
                priority: priority || null
            });
            closeCardModal();
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async () => {
        await onDelete(card.id);
        setShowConfirm(false);
        closeCardModal();
    };

    const handleImageChange = (e) => {
        const file = e.target.files?.[0];
        if (!file || !onUploadImage) return;
        setPendingImageFile(file);
        setPendingImageSrc(URL.createObjectURL(file));
        e.target.value = '';
    };

    const handlePickerConfirm = async (position) => {
        setPendingImageSrc(null);
        setUploading(true);
        await onUploadImage(card.id, pendingImageFile, position);
        setUploading(false);
        setPendingImageFile(null);
    };

    const handlePickerCancel = () => {
        if (pendingImageSrc) URL.revokeObjectURL(pendingImageSrc);
        setPendingImageSrc(null);
        setPendingImageFile(null);
    };

    return (
        <>
            <div
                onClick={openCardModal}
                style={{
                    background: isDragging ? 'var(--bg-hover)' : 'var(--bg-card)',
                    borderTop: `1px solid ${isDragging ? 'var(--accent-cyan)' : borderColor}`,
                    borderRight: `1px solid ${isDragging ? 'var(--accent-cyan)' : borderColor}`,
                    borderBottom: `1px solid ${isDragging ? 'var(--accent-cyan)' : borderColor}`,
                    borderLeft: `3px solid ${isDragging ? 'var(--accent-cyan)' : borderLeftColor}`,
                    borderRadius: 'var(--radius)',
                    padding: '12px',
                    boxShadow: isDragging ? 'var(--glow-cyan)' : 'none',
                    transition: 'border-color 0.2s, box-shadow 0.2s',
                    cursor: 'pointer'
                }}
            >
                <div style={{ display: 'flex', gap: '10px', alignItems: 'flex-start' }}>
                    <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '6px', marginBottom: card.description ? '6px' : '8px' }}>
                            <p style={{ fontSize: '15px', fontWeight: '600', color: 'var(--text-primary)', lineHeight: 1.35, overflowWrap: 'anywhere', margin: 0 }}>
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

                        {editingUser && (
                            <div className="card-editing-badge" title={`${editingLabel} is editing this card`}>
                                Editing: {editingLabel}
                            </div>
                        )}

                        {card.description && (
                            <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginBottom: '8px', lineHeight: '1.45', overflowWrap: 'anywhere' }}>
                                {card.description}
                            </p>
                        )}

                        <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: '6px' }}>
                            {card.dueDate && (
                                <span style={{
                                    fontSize: '11px',
                                    fontFamily: 'var(--font-mono)',
                                    color: overdue ? 'var(--color-red)' : 'var(--color-green)',
                                    background: overdue ? '#ef444422' : '#10b98122',
                                    padding: '2px 6px',
                                    borderRadius: '4px'
                                }}>
                                    {overdue ? 'Overdue ' : 'Due '}{formatDate(card.dueDate)}
                                </span>
                            )}
                            {assignedLabel && (
                                <div style={{ display: 'flex', alignItems: 'center', gap: '4px', marginLeft: 'auto' }}>
                                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)' }}>{assignedLabel}</span>
                                    <div style={{
                                        width: '22px', height: '22px', borderRadius: '50%',
                                        background: 'var(--accent-indigo)', color: '#fff',
                                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                                        fontSize: '9px', fontWeight: '600', fontFamily: 'var(--font-mono)', flexShrink: 0
                                    }}>
                                        {initials}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>

                    {firstImage && (
                        <div
                            onClick={e => { e.stopPropagation(); setLightboxImage(firstImage); }}
                            style={{ width: '64px', height: '64px', borderRadius: '6px', overflow: 'hidden', border: '1px solid var(--border)', flexShrink: 0, cursor: 'zoom-in', background: 'var(--bg-secondary)' }}
                        >
                            <img src={getImageSrc(firstImage.url)} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: firstImage.objectPosition || '50% 50%', display: 'block' }} />
                        </div>
                    )}
                </div>
            </div>

            {showModal && (
                <div className="modal-overlay" onClick={closeCardModal}>
                    <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '760px', maxHeight: '90vh', overflowY: 'auto' }}>
                        <h2>Edit Card</h2>
                        <form className="auth-form" onSubmit={handleSubmit}>
                            <div className="form-group">
                                <label htmlFor={`card-title-${card.id}`}>Title</label>
                                <input id={`card-title-${card.id}`} type="text" value={title} onChange={e => setTitle(e.target.value)} required autoFocus />
                            </div>
                            <div className="form-group">
                                <label htmlFor={`card-desc-${card.id}`}>Description</label>
                                <textarea id={`card-desc-${card.id}`} value={description} onChange={e => setDescription(e.target.value)} rows={4} style={{ resize: 'vertical' }} />
                            </div>
                            <div className="form-group">
                                <label>Image</label>
                                {firstImage ? (
                                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '4px' }}>
                                        <div onClick={() => setLightboxImage(firstImage)} style={{ aspectRatio: '16 / 9', borderRadius: 'var(--radius)', overflow: 'hidden', border: '1px solid var(--border)', background: 'var(--bg-secondary)', cursor: 'zoom-in' }}>
                                            <img src={getImageSrc(firstImage.url)} alt={firstImage.fileName} style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: firstImage.objectPosition || '50% 50%', display: 'block' }} />
                                        </div>
                                        <div style={{ display: 'flex', gap: '8px' }}>
                                            <button type="button" className="btn-secondary" onClick={() => fileRef.current?.click()} disabled={uploading} style={{ flex: 1 }}>
                                                {uploading ? 'Uploading...' : 'Replace'}
                                            </button>
                                            <button type="button" className="btn-danger" onClick={() => onDeleteImage?.(card.id, firstImage.id)} disabled={uploading}>
                                                Remove
                                            </button>
                                        </div>
                                    </div>
                                ) : (
                                    <button type="button" className="btn-secondary" onClick={() => fileRef.current?.click()} disabled={uploading} style={{ width: '100%', padding: '20px', borderStyle: 'dashed', marginTop: '4px' }}>
                                        {uploading ? 'Uploading...' : '+ Add Image'}
                                    </button>
                                )}
                                <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/webp" style={{ display: 'none' }} onChange={handleImageChange} />
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
                                <button type="submit" className="btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Update'}</button>
                                <button type="button" className="btn-danger" onClick={() => setShowConfirm(true)}>Delete</button>
                                <button type="button" className="btn-secondary" onClick={closeCardModal}>Cancel</button>
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

            {pendingImageSrc && (
                <FocalPointPicker
                    src={pendingImageSrc}
                    aspectRatio="4 / 3"
                    onConfirm={handlePickerConfirm}
                    onCancel={handlePickerCancel}
                />
            )}

            {lightboxImage && (
                <div className="modal-overlay" onClick={() => setLightboxImage(null)} style={{ zIndex: 1000 }}>
                    <div onClick={e => e.stopPropagation()} style={{ position: 'relative', maxWidth: '90vw', maxHeight: '90vh' }}>
                        <img
                            src={getImageSrc(lightboxImage.url)}
                            alt={lightboxImage.fileName}
                            style={{ maxWidth: '90vw', maxHeight: '90vh', objectFit: 'contain', display: 'block', borderRadius: 'var(--radius)' }}
                        />
                        <button
                            onClick={() => setLightboxImage(null)}
                            style={{ position: 'absolute', top: '8px', right: '8px', background: 'rgba(0,0,0,0.6)', border: 'none', color: '#fff', borderRadius: '50%', width: '32px', height: '32px', cursor: 'pointer', fontSize: '18px', lineHeight: 1, display: 'flex', alignItems: 'center', justifyContent: 'center' }}
                        >×</button>
                    </div>
                </div>
            )}
        </>
    );
}
