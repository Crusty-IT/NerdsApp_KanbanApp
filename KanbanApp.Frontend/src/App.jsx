import { useState, useEffect } from 'react';
import { useNavigate, useLocation, NavLink, Outlet } from 'react-router-dom';
import api from './services/api';
import { useTopbar } from './context/TopbarContext';
import NotificationBell from './components/NotificationBell';
import './App.css';

export default function App() {
    const navigate = useNavigate();
    const location = useLocation();
    const [user, setUser] = useState(null);
    const { title, actions } = useTopbar();

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (!token) return;
        api.get('/api/users/me').then(res => setUser(res.data)).catch(() => {});
    }, [location.pathname]);

    const handleLogout = () => {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        navigate('/login');
    };

    const isAuthPage = ['/login', '/register', '/'].includes(location.pathname);
    if (isAuthPage) return <Outlet />;

    return (
        <div className="app-layout">
            <aside className="sidebar">
                <div className="sidebar-logo">
                    <img src="/logo.png" alt="KanbanApp logo" style={{ height: '36px', width: 'auto' }} />
                    <span className="sidebar-logo-text">
                        <span className="logo-shell">Shellty</span><span className="logo-ty">ty</span><span className="logo-dot">.kanban</span>
                    </span>
                </div>
                <nav className="sidebar-nav">
                    <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'active' : ''}>
                        🗂 Projects
                    </NavLink>
                    <NavLink to="/profile" className={({ isActive }) => isActive ? 'active' : ''}>
                        👤 Profile
                    </NavLink>
                </nav>
                <div className="sidebar-bottom">
                    {user && (
                        <div className="sidebar-user">
                            <div style={{
                                width: '28px', height: '28px', borderRadius: '50%',
                                overflow: 'hidden', background: 'var(--accent-indigo)',
                                display: 'flex', alignItems: 'center', justifyContent: 'center',
                                fontSize: '11px', fontWeight: '600', color: '#fff', flexShrink: 0
                            }}>
                                {user.profilePictureUrl
                                    ? <img src={user.profilePictureUrl} alt="avatar" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                    : user.userName?.slice(0, 2).toUpperCase()
                                }
                            </div>
                            <div style={{ overflow: 'hidden' }}>
                                <p style={{ fontSize: '13px', fontWeight: '500', color: 'var(--text-primary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                    {user.userName}
                                </p>
                                <p style={{ fontSize: '11px', color: 'var(--text-secondary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                    {user.email}
                                </p>
                            </div>
                        </div>
                    )}
                    <button onClick={handleLogout} className="btn-secondary" style={{ width: '100%', marginTop: '8px' }}>
                        ⏻ Logout
                    </button>
                </div>
            </aside>

            <div className="main-content">
                <div className="topbar">
                    <div style={{ flex: 1, display: 'flex', justifyContent: 'flex-start' }}>
                        {actions?.left}
                    </div>
                    <span className="topbar-title">{title}</span>
                    <div style={{ flex: 1, display: 'flex', justifyContent: 'flex-end', gap: '8px', alignItems: 'center' }}>
                        <NotificationBell />
                        {actions?.right}
                    </div>
                </div>
                <Outlet />
            </div>
        </div>
    );
}