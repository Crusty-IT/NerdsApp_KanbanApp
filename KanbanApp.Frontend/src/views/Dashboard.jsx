import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import api, { BASE_URL } from '../services/api';
import { useTopbar } from '../context/TopbarContext';
import ConfirmDialog from '../components/ConfirmDialog';
import FocalPointPicker from '../components/FocalPointPicker';

const PROJECT_COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

function getImageSrc(url) {
    if (!url) return '';
    return url.startsWith('http') ? url : `${BASE_URL}${url}`;
}

export default function Dashboard() {
    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingProject, setEditingProject] = useState(null);
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [color, setColor] = useState(PROJECT_COLORS[0]);
    const [coverImageUrl, setCoverImageUrl] = useState(null);
    const [coverObjectPosition, setCoverObjectPosition] = useState('50% 50%');
    const [uploadingCover, setUploadingCover] = useState(false);
    const [pendingCoverFile, setPendingCoverFile] = useState(null);
    const [pendingCoverSrc, setPendingCoverSrc] = useState(null);
    const [error, setError] = useState('');
    const [confirm, setConfirm] = useState(null);
    const fileInputRef = useRef(null);
    const navigate = useNavigate();
    const { setTitle, setActions } = useTopbar();

    useEffect(() => {
        setTitle('Projects');
        setActions({ left: null, right: null });
        return () => { setTitle(''); setActions(null); };
    }, []);

    useEffect(() => {
        let ignore = false;
        async function fetchProjects() {
            try {
                const response = await api.get('/api/projects');
                if (!ignore) setProjects(response.data);
            } catch (err) {
                console.error('Failed to fetch projects', err);
            } finally {
                if (!ignore) setLoading(false);
            }
        }
        fetchProjects();
        return () => { ignore = true; };
    }, []);

    const handleCreate = async (e) => {
        e.preventDefault();
        if (!name.trim()) return;
        try {
            const response = await api.post('/api/projects', { name: name.trim(), description: description.trim(), color });
            setProjects(prev => [{ ...response.data, description: description.trim(), createdAt: new Date().toISOString(), isOwner: true }, ...prev]);
            closeForm();
        } catch {
            setError('Failed to create project');
        }
    };

    const handleEdit = (project) => {
        setEditingProject(project);
        setName(project.name);
        setDescription(project.description || '');
        setColor(project.color || PROJECT_COLORS[0]);
        setCoverImageUrl(project.coverImageUrl || null);
        setCoverObjectPosition(project.coverObjectPosition || '50% 50%');
        setIsEditing(true);
        setShowForm(true);
    };

    const handleUpdate = async (e) => {
        e.preventDefault();
        if (!name.trim()) return;
        try {
            const response = await api.put(`/api/projects/${editingProject.id}`, { name: name.trim(), description: description.trim(), color });
            setProjects(prev => prev.map(p => p.id === editingProject.id
                ? { ...p, ...response.data, description: description.trim(), coverImageUrl, coverObjectPosition }
                : p));
            closeForm();
        } catch {
            setError('Failed to update project');
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
        if (!pendingCoverFile || !editingProject) return;
        setPendingCoverSrc(null);

        const formData = new FormData();
        formData.append('file', pendingCoverFile);
        setUploadingCover(true);
        try {
            const response = await api.post(
                `/api/projects/${editingProject.id}/cover?position=${encodeURIComponent(position)}`,
                formData
            );
            const { coverImageUrl: newUrl, coverObjectPosition: newPos } = response.data;
            setCoverImageUrl(newUrl);
            setCoverObjectPosition(newPos);
            setProjects(prev => prev.map(p => p.id === editingProject.id ? { ...p, coverImageUrl: newUrl, coverObjectPosition: newPos } : p));
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
        if (!editingProject) return;
        setUploadingCover(true);
        try {
            await api.delete(`/api/projects/${editingProject.id}/cover`);
            setCoverImageUrl(null);
            setCoverObjectPosition('50% 50%');
            setProjects(prev => prev.map(p => p.id === editingProject.id ? { ...p, coverImageUrl: null, coverObjectPosition: null } : p));
        } catch {
            setError('Failed to remove cover');
        } finally {
            setUploadingCover(false);
        }
    };

    const handleDeleteClick = async (project) => {
        const details = await api.get(`/api/projects/${project.id}`);
        const boardCount = details.data.boards?.length ?? 0;
        const message = boardCount > 0
            ? `"${project.name}" contains ${boardCount} board${boardCount > 1 ? 's' : ''} with all their cards. This action cannot be undone.`
            : `Are you sure you want to delete "${project.name}"? This action cannot be undone.`;
        setConfirm({ title: 'Delete Project', message, onConfirm: () => handleDeleteConfirmed(project.id) });
    };

    const handleDeleteConfirmed = async (projectId) => {
        setConfirm(null);
        try {
            await api.delete(`/api/projects/${projectId}`);
            setProjects(prev => prev.filter(p => p.id !== projectId));
        } catch {
            setError('Failed to delete project');
            setTimeout(() => setError(null), 3000);
        }
    };

    const closeForm = () => {
        setShowForm(false);
        setIsEditing(false);
        setEditingProject(null);
        setName('');
        setDescription('');
        setColor(PROJECT_COLORS[0]);
        setCoverImageUrl(null);
        setCoverObjectPosition('50% 50%');
        setPendingCoverFile(null);
        if (pendingCoverSrc) URL.revokeObjectURL(pendingCoverSrc);
        setPendingCoverSrc(null);
        setError('');
    };

    const formatDate = (dateStr) => {
        if (!dateStr) return '';
        const d = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
        if (isNaN(d.getTime())) return '';
        return d.toLocaleDateString('pl-PL');
    };

    if (loading) return <div className="loading">loading projects...</div>;

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
                    <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxHeight: '90vh', overflowY: 'auto' }}>
                        <h2>{isEditing ? 'Edit Project' : 'New Project'}</h2>
                        {error && <p className="error-msg" style={{ marginBottom: '12px' }}>{error}</p>}
                        <form className="auth-form" onSubmit={isEditing ? handleUpdate : handleCreate}>
                            <div className="form-group">
                                <label>Project Name</label>
                                <input type="text" value={name} onChange={e => setName(e.target.value)} placeholder="My awesome project" autoFocus required />
                            </div>
                            <div className="form-group">
                                <label>Description</label>
                                <input type="text" value={description} onChange={e => setDescription(e.target.value)} placeholder="What is this project about?" />
                            </div>
                            {isEditing && (
                                <div className="form-group">
                                    <label>Cover Image</label>
                                    {coverImageUrl ? (
                                        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '4px' }}>
                                            <div style={{ aspectRatio: '16 / 9', borderRadius: 'var(--radius)', overflow: 'hidden', border: '1px solid var(--border)', background: 'var(--bg-secondary)' }}>
                                                <img src={getImageSrc(coverImageUrl)} alt="Cover" style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: coverObjectPosition, display: 'block' }} />
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
                                    {PROJECT_COLORS.map(c => (
                                        <button key={c} type="button" onClick={() => setColor(c)} style={{
                                            width: '28px', height: '28px', borderRadius: '50%', background: c,
                                            border: color === c ? '3px solid #fff' : '2px solid transparent',
                                            outline: color === c ? `2px solid ${c}` : 'none', padding: 0, cursor: 'pointer'
                                        }} />
                                    ))}
                                </div>
                            </div>
                            <div className="modal-actions">
                                <button type="submit" className="btn-primary">{isEditing ? 'Update Project' : 'Create Project'}</button>
                                <button type="button" className="btn-secondary" onClick={closeForm}>Cancel</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {pendingCoverSrc && (
                <FocalPointPicker
                    src={pendingCoverSrc}
                    aspectRatio="16 / 9"
                    onConfirm={handlePickerConfirm}
                    onCancel={handlePickerCancel}
                />
            )}

            {projects.length === 0 ? (
                <div className="empty-state">
                    <span className="empty-icon">🗂</span>
                    <p>No projects yet. Create your first workspace.</p>
                    <button className="btn-primary" onClick={() => setShowForm(true)}>+ New Project</button>
                </div>
            ) : (
                <div className="projects-grid">
                    {projects.map(project => (
                        <div
                            key={project.id}
                            className="project-card"
                            style={{ '--project-color': project.color || PROJECT_COLORS[0], position: 'relative', overflow: 'hidden', padding: 0 }}
                            onClick={() => navigate(`/projects/${project.id}`)}
                        >
                            {project.coverImageUrl && (
                                <div style={{ aspectRatio: '16 / 9', width: '100%', background: 'var(--bg-secondary)' }}>
                                    <img src={getImageSrc(project.coverImageUrl)} alt={project.name} style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: project.coverObjectPosition || '50% 50%', display: 'block' }} />
                                </div>
                            )}
                            <div style={{ padding: '14px 16px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '8px' }}>
                                    <h3 style={{ color: project.color || PROJECT_COLORS[0], margin: 0, flex: 1 }}>{project.name}</h3>
                                    {project.isOwner ? (
                                        <div style={{ display: 'flex', gap: '6px', flexShrink: 0 }}>
                                            <button className="btn-secondary" onClick={(e) => { e.stopPropagation(); handleEdit(project); }} style={{ padding: '4px 8px', fontSize: '12px' }}>✏️</button>
                                            <button className="btn-danger" onClick={(e) => { e.stopPropagation(); handleDeleteClick(project); }} style={{ padding: '4px 8px', fontSize: '12px' }}>🗑️</button>
                                        </div>
                                    ) : (
                                        <span style={{ fontSize: '11px', background: 'rgba(255,255,255,0.1)', padding: '2px 8px', borderRadius: '10px', color: 'var(--text-secondary)', flexShrink: 0 }}>Member</span>
                                    )}
                                </div>
                                {project.description && <p style={{ marginTop: '6px' }}>{project.description}</p>}
                                <div className="project-card-footer">
                                    <span className="project-card-meta">{formatDate(project.createdAt)}</span>
                                    <span className="project-color-dot" style={{ background: project.color || PROJECT_COLORS[0] }} />
                                </div>
                            </div>
                        </div>
                    ))}
                    <div
                        className="project-card"
                        style={{ '--project-color': 'var(--border)', border: '1px dashed var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '120px', cursor: 'pointer' }}
                        onClick={() => setShowForm(true)}
                    >
                        <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>+ New Project</span>
                    </div>
                </div>
            )}
        </div>
    );
}
