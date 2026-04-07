import { createContext, useContext, useState, useCallback } from 'react';
import api from '../services/api';

const TopbarContext = createContext(null);

export function TopbarProvider({ children }) {
    const [title, setTitle] = useState('');
    const [actions, setActions] = useState(null);
    const [notifications, setNotifications] = useState([]);
    const [unreadCount, setUnreadCount] = useState(0);

    const fetchNotifications = useCallback(async () => {
        try {
            const res = await api.get('/api/notifications');
            setNotifications(res.data);
            setUnreadCount(res.data.filter(n => !n.isRead).length);
        } catch {
        }
    }, []);

    const markAllRead = async () => {
        try {
            await api.put('/api/notifications/read-all');
            setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
            setUnreadCount(0);
        } catch {}
    };

    const markOneRead = async (id) => {
        try {
            await api.put(`/api/notifications/${id}/read`);
            setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
            setUnreadCount(prev => Math.max(0, prev - 1));
        } catch {}
    };

    return (
        <TopbarContext.Provider value={{
            title, setTitle,
            actions, setActions,
            notifications, unreadCount,
            fetchNotifications, markAllRead, markOneRead
        }}>
            {children}
        </TopbarContext.Provider>
    );
}

export function useTopbar() {
    return useContext(TopbarContext);
}