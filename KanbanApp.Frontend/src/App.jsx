import { useState, useEffect } from 'react';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import api from './services/api';
import { useTopbar } from './context/TopbarContext';
import NotificationBell from './components/NotificationBell';
import PrivacyPolicyModal from './components/PrivacyPolicyModal';
import Toast from './components/Toast';
import BackendWakeNotice from './components/BackendWakeNotice';
import Sidebar from './components/Sidebar';
import { useToast } from './hooks/useToast';
import { useNavDock } from './hooks/useNavDock';
import { useIsMobile } from './hooks/useIsMobile';

function contentStyle(mode, collapsed, isMobile) {
    if (isMobile) return { marginLeft: 0, marginRight: 0, paddingBottom: '84px' };
    if (mode === 'bottom') return { marginLeft: 0, marginRight: 0, paddingBottom: '96px' };
    if (mode === 'floating') return { marginLeft: 0, marginRight: 0 };
    const offset = (collapsed ? 76 : 240) + 24;
    return mode === 'right'
        ? { marginLeft: 0, marginRight: offset }
        : { marginLeft: offset, marginRight: 0 };
}

export default function App() {
    const navigate = useNavigate();
    const location = useLocation();
    const [user, setUser] = useState(null);
    const [privacyOpen, setPrivacyOpen] = useState(false);
    const { title, actions } = useTopbar();
    const { toasts, removeToast } = useToast();
    const dock = useNavDock();
    const isMobile = useIsMobile();

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

    return (
        <>
            <BackendWakeNotice />
            {isAuthPage ? (
                <Outlet />
            ) : (
                <div className="app-layout">
                    <Sidebar
                        user={user}
                        onLogout={handleLogout}
                        onOpenPrivacy={() => setPrivacyOpen(true)}
                        dock={dock}
                        isMobile={isMobile}
                    />

                    <PrivacyPolicyModal isOpen={privacyOpen} onClose={() => setPrivacyOpen(false)} />

                    <div style={{ position: 'fixed', bottom: '24px', right: '24px', display: 'flex', flexDirection: 'column-reverse', gap: '8px', zIndex: 9999 }}>
                        {toasts.map(t => (
                            <Toast key={t.id} message={t.message} onClose={() => removeToast(t.id)} />
                        ))}
                    </div>

                    <div className="main-content" style={contentStyle(dock.mode, dock.collapsed, isMobile)}>
                        <div className="topbar">
                            <div className="topbar-left">
                                {actions?.left}
                            </div>
                            <span className="topbar-title">{title}</span>
                            <div className="topbar-right">
                                <NotificationBell />
                                {actions?.right}
                            </div>
                        </div>
                        <Outlet />
                    </div>
                </div>
            )}
        </>
    );
}
