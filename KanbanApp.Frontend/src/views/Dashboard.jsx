import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { useTopbar } from '../context/TopbarContext';

const PROJECT_COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

export default function Dashboard() {
    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingProject, setEditingProject] = useState(null);
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [color, setColor] = useState(PROJECT_COLORS[0]);
    const [error, setError] = useState('');
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
            setProjects(prev => [response.data, ...prev]);
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
        setIsEditing(true);
        setShowForm(true);
    };

    const handleUpdate = async (e) => {
        e.preventDefault();
        if (!name.trim()) return;
        try {
            const response = await api.put(`/api/projects/${editingProject.id}`, { name: name.trim(), description: description.trim(), color });
            setProjects(prev => prev.map(p => p.id === editingProject.id ? { ...p, ...response.data } : p));
            closeForm();
        } catch {
            setError('Failed to update project');
        }
    };

    const handleDelete = async (project) => {
        try {
            await api.delete(`/api/projects/${project.id}`);
            setProjects(prev => prev.filter(p => p.id !== project.id));
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
        setError('');
    };

    const formatDate = (dateStr) => {
        if (!dateStr) return '';
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return '';
        return d.toLocaleDateString('pl-PL');
    };

    if (loading) return <div className="loading">loading projects...</div>;

    return (
        <div className="page-content fade-in">
            {error && <p className="error-msg" style={{ marginBottom: '12px' }}>{error}</p>}

            {showForm && (
                <div className="modal-overlay" onClick={closeForm}>
                    <div className="modal-box" onClick={e => e.stopPropagation()}>
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
                            style={{ '--project-color': project.color || PROJECT_COLORS[0], position: 'relative' }}
                            onClick={() => navigate(`/projects/${project.id}`)}
                        >
                            <div style={{ position: 'absolute', top: '12px', right: '12px', display: 'flex', gap: '6px', zIndex: 10 }}>
                                <button className="btn-secondary" onClick={(e) => { e.stopPropagation(); handleEdit(project); }} style={{ padding: '4px 8px', fontSize: '12px' }}>✏️</button>
                                <button className="btn-danger" onClick={(e) => { e.stopPropagation(); handleDelete(project); }} style={{ padding: '4px 8px', fontSize: '12px' }}>🗑️</button>
                            </div>
                            <h3 style={{ color: project.color || PROJECT_COLORS[0] }}>{project.name}</h3>
                            {project.description && <p>{project.description}</p>}
                            <div className="project-card-footer">
                                <span className="project-card-meta">{formatDate(project.createdAt)}</span>
                                <span className="project-color-dot" style={{ background: project.color || PROJECT_COLORS[0] }} />
                            </div>
                        </div>
                    ))}
                    <div
                        className="project-card"
                        style={{
                            '--project-color': 'var(--border)',
                            border: '1px dashed var(--border)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            minHeight: '120px',
                            cursor: 'pointer'
                        }}
                        onClick={() => setShowForm(true)}
                    >
                        <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>+ New Project</span>
                    </div>
                </div>
            )}
        </div>
    );
}