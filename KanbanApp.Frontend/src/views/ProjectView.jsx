import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../services/api';
import { useTopbar } from '../context/TopbarContext';
import ConfirmDialog from '../components/ConfirmDialog';

const COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

export default function ProjectView() {
    const { projectId } = useParams();
    const navigate = useNavigate();
    const [project, setProject] = useState(null);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingBoard, setEditingBoard] = useState(null);
    const [boardName, setBoardName] = useState('');
    const [boardColor, setBoardColor] = useState(COLORS[0]);
    const [error, setError] = useState('');
    const [confirm, setConfirm] = useState(null);
    const { setTitle, setActions } = useTopbar();

    useEffect(() => {
        return () => { setTitle(''); setActions(null); };
    }, []);

    useEffect(() => {
        if (project) {
            setTitle(project.name);
            setActions({
                left: <button className="btn-secondary" onClick={() => navigate('/dashboard')} style={{ fontSize: '14px' }}>← Back</button>,
                right: null
            });
        }
    }, [project]);

    useEffect(() => {
        let ignore = false;
        async function fetchProject() {
            try {
                const response = await api.get(`/api/projects/${projectId}`);
                if (!ignore) setProject(response.data);
            } catch {
                console.error('Failed to fetch project');
            } finally {
                if (!ignore) setLoading(false);
            }
        }
        fetchProject();
        return () => { ignore = true; };
    }, [projectId]);

    const handleCreateBoard = async (e) => {
        e.preventDefault();
        if (!boardName.trim()) return;
        try {
            const response = await api.post('/api/boards', {
                boardName: boardName.trim(),
                projectId: parseInt(projectId),
                color: boardColor
            });
            setProject(prev => ({
                ...prev,
                boards: [...(prev.boards || []), response.data]
            }));
            closeForm();
        } catch {
            setError('Failed to create board');
        }
    };

    const handleEdit = (board) => {
        setEditingBoard(board);
        setBoardName(board.name);
        setBoardColor(board.color || COLORS[0]);
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
                boards: prev.boards.map(b => b.id === editingBoard.id ? { ...b, ...response.data } : b)
            }));
            closeForm();
        } catch {
            setError('Failed to update board');
        }
    };

    const handleDeleteClick = async (board) => {
        const details = await api.get(`/api/boards/${board.id}`);
        const cardCount = details.data.columns?.reduce((sum, col) => sum + (col.cards?.length ?? 0), 0) ?? 0;

        const message = cardCount > 0
            ? `"${board.name}" contains ${cardCount} card${cardCount > 1 ? 's' : ''}. This action cannot be undone.`
            : `Are you sure you want to delete "${board.name}"? This action cannot be undone.`;

        setConfirm({
            title: 'Delete Board',
            message,
            onConfirm: () => handleDeleteConfirmed(board.id)
        });
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
        setError('');
    };

    const formatDate = (dateStr) => {
        if (!dateStr) return '';
        const d = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
        if (isNaN(d.getTime())) return '';
        return d.toLocaleDateString('pl-PL');
    };

    if (loading) return <div className="loading">loading project...</div>;
    if (!project) return <div className="page-content"><p>Project not found.</p></div>;

    return (
        <div className="page-content fade-in">
            {error && <p className="error-msg" style={{ marginBottom: '12px' }}>{error}</p>}

            {confirm && (
                <ConfirmDialog
                    title={confirm.title}
                    message={confirm.message}
                    onConfirm={confirm.onConfirm}
                    onCancel={() => setConfirm(null)}
                />
            )}

            {showForm && (
                <div className="modal-overlay" onClick={closeForm}>
                    <div className="modal-box" onClick={e => e.stopPropagation()}>
                        <h2>{isEditing ? 'Edit Board' : 'New Board'}</h2>
                        {error && <p className="error-msg" style={{ marginBottom: '12px' }}>{error}</p>}
                        <form className="auth-form" onSubmit={isEditing ? handleUpdate : handleCreateBoard}>
                            <div className="form-group">
                                <label>Board Name</label>
                                <input type="text" value={boardName} onChange={e => setBoardName(e.target.value)} placeholder="e.g. Sprint 1" autoFocus required />
                            </div>
                            <div className="form-group">
                                <label>Color</label>
                                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginTop: '4px' }}>
                                    {COLORS.map(c => (
                                        <button key={c} type="button" onClick={() => setBoardColor(c)} style={{
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

            {(!project.boards || project.boards.length === 0) ? (
                <div className="empty-state">
                    <span className="empty-icon">📋</span>
                    <p>No boards yet. Create your first board.</p>
                    <button className="btn-primary" onClick={() => setShowForm(true)}>+ New Board</button>
                </div>
            ) : (
                <div className="projects-grid">
                    {project.boards.map(board => (
                        <div
                            key={board.id}
                            className="project-card"
                            style={{ '--project-color': board.color || project.color || COLORS[0], position: 'relative' }}
                            onClick={() => navigate(`/board/${board.id}`)}
                        >
                            <div style={{ position: 'absolute', top: '12px', right: '12px', display: 'flex', gap: '6px', zIndex: 10 }}>
                                <button className="btn-secondary" onClick={(e) => { e.stopPropagation(); handleEdit(board); }} style={{ padding: '4px 8px', fontSize: '12px' }}>✏️</button>
                                <button className="btn-danger" onClick={(e) => { e.stopPropagation(); handleDeleteClick(board); }} style={{ padding: '4px 8px', fontSize: '12px' }}>🗑️</button>
                            </div>
                            <h3 style={{ color: board.color || project.color || COLORS[0] }}>{board.name}</h3>
                            {board.description && <p>{board.description}</p>}
                            <div className="project-card-footer">
                                <span className="project-card-meta">{formatDate(board.createdAt)}</span>
                                <span className="project-color-dot" style={{ background: board.color || project.color || COLORS[0] }} />
                            </div>
                        </div>
                    ))}
                    <div
                        className="project-card"
                        style={{ '--project-color': 'var(--border)', border: '1px dashed var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '120px', cursor: 'pointer' }}
                        onClick={() => setShowForm(true)}
                    >
                        <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>+ New Board</span>
                    </div>
                </div>
            )}
        </div>
    );
}