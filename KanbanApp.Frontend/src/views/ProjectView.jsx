import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api, { BASE_URL } from '../services/api';
import { useTopbar } from '../context/TopbarContext';
import ConfirmDialog from '../components/ConfirmDialog';
import FocalPointPicker from '../components/FocalPointPicker';

const COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

function getImageSrc(url) {
    if (!url) return '';
    return url.startsWith('http') ? url : `${BASE_URL}${url}`;
}

export default function ProjectView() {
    const { projectId } = useParams();
    const navigate = useNavigate();
    const [project, setProject] = useState(null);
    const [members, setMembers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [fetchError, setFetchError] = useState(false);
    const [showForm, setShowForm] = useState(false);
    const [showInvite, setShowInvite] = useState(false);
    const [showMembers, setShowMembers] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingBoard, setEditingBoard] = useState(null);
    const [boardName, setBoardName] = useState('');
    const [boardColor, setBoardColor] = useState(COLORS[0]);
    const [boardCoverImageUrl, setBoardCoverImageUrl] = useState(null);
    const [boardCoverObjectPosition, setBoardCoverObjectPosition] = useState('50% 50%');
    const [uploadingCover, setUploadingCover] = useState(false);
    const [pendingCoverFile, setPendingCoverFile] = useState(null);
    const [pendingCoverSrc, setPendingCoverSrc] = useState(null);
    const [inviteEmail, setInviteEmail] = useState('');
    const [inviteError, setInviteError] = useState('');
    const [inviteSuccess, setInviteSuccess] = useState('');
    const [error, setError] = useState('');
    const [confirm, setConfirm] = useState(null);
    const fileInputRef = useRef(null);
    const { setTitle, setActions } = useTopbar();

    useEffect(() => {
        return () => { setTitle(''); setActions(null); };
    }, []);

    useEffect(() => {
        if (!project) return;
        setTitle(project.name);
        setActions({
            left: <button className="btn-secondary" onClick={() => navigate('/dashboard')} style={{ fontSize: '14px' }}>← Back</button>,
            right: project.isOwner ? (
                <div style={{ display: 'flex', gap: '8px' }}>
                    <button className="btn-secondary" onClick={() => { setShowMembers(true); fetchMembers(); }} style={{ fontSize: '13px' }}>👥 Members</button>
                    <button className="btn-primary" onClick={() => setShowInvite(true)} style={{ fontSize: '13px' }}>+ Invite</button>
                </div>
            ) : (
                <button className="btn-secondary" onClick={() => { setShowMembers(true); fetchMembers(); }} style={{ fontSize: '13px' }}>👥 Members</button>
            )
        });
    }, [project]);

    useEffect(() => {
        let ignore = false;
        async function fetchProject() {
            try {
                const response = await api.get(`/api/projects/${projectId}`);
                if (!ignore) setProject(response.data);
            } catch {
                if (!ignore) setFetchError(true);
            } finally {
                if (!ignore) setLoading(false);
            }
        }
        fetchProject();
        return () => { ignore = true; };
    }, [projectId]);

    const fetchMembers = async () => {
        try {
            const response = await api.get(`/api/projects/${projectId}/members`);
            setMembers(response.data);
        } catch {
            console.error('Failed to fetch members');
        }
    };

    const handleInvite = async (e) => {
        e.preventDefault();
        setInviteError('');
        setInviteSuccess('');
        try {
            await api.post(`/api/projects/${projectId}/members`, { email: inviteEmail.trim() });
            setInviteSuccess('User invited successfully!');
            setInviteEmail('');
        } catch (err) {
            setInviteError(err.response?.data?.message || 'Failed to invite user');
        }
    };

    const handleRemoveMember = async (memberId) => {
        try {
            await api.delete(`/api/projects/${projectId}/members/${memberId}`);
            setMembers(prev => prev.filter(m => m.userId !== memberId));
        } catch {
            setInviteError('Failed to remove member');
        }
    };

    const handleCreateBoard = async (e) => {
        e.preventDefault();
        if (!boardName.trim()) return;
        try {
            const response = await api.post('/api/boards', {
                boardName: boardName.trim(),
                projectId: parseInt(projectId),
                color: boardColor
            });
            setProject(prev => ({ ...prev, boards: [...(prev.boards || []), response.data] }));
            closeForm();
        } catch {
            setError('Failed to create board');
        }
    };

    const handleEdit = (board) => {
        setEditingBoard(board);
        setBoardName(board.name);
        setBoardColor(board.color || COLORS[0]);
        setBoardCoverImageUrl(board.coverImageUrl || null);
        setBoardCoverObjectPosition(board.coverObjectPosition || '50% 50%');
        setIsEditing(true);
        setShowForm(true);
    };

    const handleUpdate = async (e) => {
        e.preventDefault();
        if (!boardName.trim()) return;
        try {
            const response = await api.put(`/api/boards/${editingBoard.id}`, {
                name: boardName.trim(),
                description: editingBoard.description || '',
                color: boardColor
            });
            setProject(prev => ({
                ...prev,
                boards: prev.boards.map(b => b.id === editingBoard.id
                    ? { ...b, ...response.data, coverImageUrl: boardCoverImageUrl, coverObjectPosition: boardCoverObjectPosition }
                    : b)
            }));
            closeForm();
        } catch {
            setError('Failed to update board');
        }
    };

    const handleFileChange = (e) => {
        const file = e.target.files?.[0];
        if (!file) return;
        setPendingCoverFile(file);
        setPendingCoverSrc(URL.createObjectURL(file));
        e.target.value = '';
    };

    const handlePickerConfirm = async (position) => {
        if (!pendingCoverFile || !editingBoard) return;
        setPendingCoverSrc(null);

        const formData = new FormData();
        formData.append('file', pendingCoverFile);
        setUploadingCover(true);
        try {
            const response = await api.post(
                `/api/boards/${editingBoard.id}/cover?position=${encodeURIComponent(position)}`,
                formData
            );
            const { coverImageUrl: newUrl, coverObjectPosition: newPos } = response.data;
            setBoardCoverImageUrl(newUrl);
            setBoardCoverObjectPosition(newPos);
            setProject(prev => ({
                ...prev,
                boards: prev.boards.map(b => b.id === editingBoard.id ? { ...b, coverImageUrl: newUrl, coverObjectPosition: newPos } : b)
            }));
        } catch (err) {
            setError(err.response?.data || 'Failed to upload cover');
        } finally {
            setUploadingCover(false);
            setPendingCoverFile(null);
        }
    };

    const handlePickerCancel = () => {
        if (pendingCoverSrc) URL.revokeObjectURL(pendingCoverSrc);
        setPendingCoverSrc(null);
        setPendingCoverFile(null);
    };

    const handleCoverRemove = async () => {
        if (!editingBoard) return;
        setUploadingCover(true);
        try {
            await api.delete(`/api/boards/${editingBoard.id}/cover`);
            setBoardCoverImageUrl(null);
            setBoardCoverObjectPosition('50% 50%');
            setProject(prev => ({
                ...prev,
                boards: prev.boards.map(b => b.id === editingBoard.id ? { ...b, coverImageUrl: null, coverObjectPosition: null } : b)
            }));
        } catch {
            setError('Failed to remove cover');
        } finally {
            setUploadingCover(false);
        }
    };

    const handleDeleteClick = async (board) => {
        const details = await api.get(`/api/boards/${board.id}`);
        const cardCount = details.data.columns?.reduce((sum, col) => sum + (col.cards?.length ?? 0), 0) ?? 0;
        const message = cardCount > 0
            ? `"${board.name}" contains ${cardCount} card${cardCount > 1 ? 's' : ''}. This action cannot be undone.`
            : `Are you sure you want to delete "${board.name}"? This action cannot be undone.`;
        setConfirm({ title: 'Delete Board', message, onConfirm: () => handleDeleteConfirmed(board.id) });
    };

    const handleDeleteConfirmed = async (boardId) => {
        setConfirm(null);
        try {
            await api.delete(`/api/boards/${boardId}`);
            setProject(prev => ({ ...prev, boards: prev.boards.filter(b => b.id !== boardId) }));
        } catch {
            setError('Failed to delete board');
            setTimeout(() => setError(null), 3000);
        }
    };

    const closeForm = () => {
        setShowForm(false);
        setIsEditing(false);
        setEditingBoard(null);
        setBoardName('');
        setBoardColor(COLORS[0]);
        setBoardCoverImageUrl(null);
        setBoardCoverObjectPosition('50% 50%');
        setPendingCoverFile(null);
        if (pendingCoverSrc) URL.revokeObjectURL(pendingCoverSrc);
        setPendingCoverSrc(null);
        setError('');
    };

    const formatDate = (dateStr) => {
        if (!dateStr) return '';
        const d = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
        if (isNaN(d.getTime())) return '';
        return d.toLocaleDateString('en-GB');
    };

    if (loading) return <div className="loading">Loading project...</div>;
    if (fetchError) return (
        <div className="page-content">
            <p className="error-msg">Failed to load project. Please refresh the page.</p>
        </div>
    );
    if (!project) return <div className="page-content"><p>Project not found.</p></div>;

    return (
        <div className="page-content fade-in">
            {error && <p className="error-msg" style={{ marginBottom: '12px' }}>{error}</p>}

            {confirm && (
                <ConfirmDialog title={confirm.title} message={confirm.message} onConfirm={confirm.onConfirm} onCancel={() => setConfirm(null)} />
            )}

            {showInvite && (
                <div className="modal-overlay" onClick={() => { setShowInvite(false); setInviteEmail(''); setInviteError(''); setInviteSuccess(''); }}>
                    <div className="modal-box" onClick={e => e.stopPropagation()}>
                        <h2>Invite to Project</h2>
                        <form className="auth-form" onSubmit={handleInvite}>
                            <div className="form-group">
                                <label htmlFor="invite-email">Email address</label>
                                <input id="invite-email" type="email" value={inviteEmail} onChange={e => setInviteEmail(e.target.value)} placeholder="user@example.com" autoFocus required />
                            </div>
                            {inviteError && <p className="error-msg">{inviteError}</p>}
                            {inviteSuccess && <p className="success-msg">{inviteSuccess}</p>}
                            <div className="modal-actions">
                                <button type="submit" className="btn-primary">Send Invite</button>
                                <button type="button" className="btn-secondary" onClick={() => { setShowInvite(false); setInviteEmail(''); setInviteError(''); setInviteSuccess(''); }}>Close</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showMembers && (
                <div className="modal-overlay" onClick={() => setShowMembers(false)}>
                    <div className="modal-box" onClick={e => e.stopPropagation()}>
                        <h2>Project Members</h2>
                        {members.length === 0 ? (
                            <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>No members yet.</p>
                        ) : (
                            <ul style={{ listStyle: 'none', padding: 0, margin: '12px 0', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                {members.map(m => (
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
                                        {project.isOwner && (
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

            {showForm && (
                <div className="modal-overlay" onClick={closeForm}>
                    <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxHeight: '90vh', overflowY: 'auto' }}>
                        <h2>{isEditing ? 'Edit Board' : 'New Board'}</h2>
                        {error && <p className="error-msg" style={{ marginBottom: '12px' }}>{error}</p>}
                        <form className="auth-form" onSubmit={isEditing ? handleUpdate : handleCreateBoard}>
                            <div className="form-group">
                                <label htmlFor="board-name">Board Name</label>
                                <input id="board-name" type="text" value={boardName} onChange={e => setBoardName(e.target.value)} placeholder="e.g. Sprint 1" autoFocus required />
                            </div>
                            {isEditing && (
                                <div className="form-group">
                                    <label>Cover Image</label>
                                    {boardCoverImageUrl ? (
                                        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '4px' }}>
                                            <div style={{ aspectRatio: '16 / 9', borderRadius: 'var(--radius)', overflow: 'hidden', border: '1px solid var(--border)', background: 'var(--bg-secondary)' }}>
                                                <img src={getImageSrc(boardCoverImageUrl)} alt="Cover" style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: boardCoverObjectPosition, display: 'block' }} />
                                            </div>
                                            <div style={{ display: 'flex', gap: '8px' }}>
                                                <button type="button" className="btn-secondary" onClick={() => fileInputRef.current?.click()} disabled={uploadingCover}>
                                                    {uploadingCover ? 'Uploading...' : 'Replace'}
                                                </button>
                                                <button type="button" className="btn-danger" onClick={handleCoverRemove} disabled={uploadingCover}>Remove</button>
                                            </div>
                                        </div>
                                    ) : (
                                        <button type="button" className="btn-secondary" onClick={() => fileInputRef.current?.click()} disabled={uploadingCover} style={{ width: '100%', padding: '20px', borderStyle: 'dashed' }}>
                                            {uploadingCover ? 'Uploading...' : '+ Add Cover Image'}
                                        </button>
                                    )}
                                    <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp" style={{ display: 'none' }} onChange={handleFileChange} />
                                </div>
                            )}
                            <div className="form-group">
                                <label>Color</label>
                                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginTop: '4px' }}>
                                    {COLORS.map(c => (
                                        <button key={c} type="button" onClick={() => setBoardColor(c)} aria-label={`Select color ${c}`} aria-pressed={boardColor === c} style={{
                                            width: '28px', height: '28px', borderRadius: '50%', background: c,
                                            border: boardColor === c ? '3px solid #fff' : '2px solid transparent',
                                            outline: boardColor === c ? `2px solid ${c}` : 'none', padding: 0, cursor: 'pointer'
                                        }} />
                                    ))}
                                </div>
                            </div>
                            <div className="modal-actions">
                                <button type="submit" className="btn-primary">{isEditing ? 'Update Board' : 'Create Board'}</button>
                                <button type="button" className="btn-secondary" onClick={closeForm}>Cancel</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {pendingCoverSrc && (
                <FocalPointPicker src={pendingCoverSrc} aspectRatio="16 / 9" onConfirm={handlePickerConfirm} onCancel={handlePickerCancel} />
            )}

            {(!project.boards || project.boards.length === 0) ? (
                <div className="empty-state">
                    <span className="empty-icon">📋</span>
                    <p>No boards yet. Create your first board.</p>
                    {project.isOwner && <button className="btn-primary" onClick={() => setShowForm(true)}>+ New Board</button>}
                </div>
            ) : (
                <div className="projects-grid">
                    {project.boards.map(board => (
                        <div
                            key={board.id}
                            className="project-card"
                            style={{ '--project-color': board.color || project.color || COLORS[0], position: 'relative', overflow: 'hidden', padding: 0 }}
                            onClick={() => navigate(`/board/${board.id}`)}
                        >
                            {board.coverImageUrl && (
                                <div style={{ aspectRatio: '16 / 9', width: '100%', background: 'var(--bg-secondary)' }}>
                                    <img src={getImageSrc(board.coverImageUrl)} alt={board.name} style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: board.coverObjectPosition || '50% 50%', display: 'block' }} />
                                </div>
                            )}
                            <div style={{ padding: '14px 16px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '8px' }}>
                                    <h3 style={{ color: board.color || project.color || COLORS[0], margin: 0, flex: 1 }}>{board.name}</h3>
                                    {project.isOwner && (
                                        <div style={{ display: 'flex', gap: '6px', flexShrink: 0 }}>
                                            <button aria-label={`Edit ${board.name}`} className="btn-secondary" onClick={(e) => { e.stopPropagation(); handleEdit(board); }} style={{ padding: '4px 8px', fontSize: '12px' }}>✏️</button>
                                            <button aria-label={`Delete ${board.name}`} className="btn-danger" onClick={(e) => { e.stopPropagation(); handleDeleteClick(board); }} style={{ padding: '4px 8px', fontSize: '12px' }}>🗑️</button>
                                        </div>
                                    )}
                                </div>
                                {board.description && <p style={{ marginTop: '6px' }}>{board.description}</p>}
                                <div className="project-card-footer">
                                    <span className="project-card-meta">{formatDate(board.createdAt)}</span>
                                    <span className="project-color-dot" style={{ background: board.color || project.color || COLORS[0] }} />
                                </div>
                            </div>
                        </div>
                    ))}
                    {project.isOwner && (
                        <div
                            className="project-card"
                            style={{ '--project-color': 'var(--border)', border: '1px dashed var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '120px', cursor: 'pointer' }}
                            onClick={() => setShowForm(true)}
                        >
                            <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>+ New Board</span>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
