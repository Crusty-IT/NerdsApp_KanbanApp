import { useState, useEffect, useRef } from 'react';
import { HubConnectionState } from '@microsoft/signalr';
import { useTopbar } from '../context/TopbarContext';
import { useSignalREvents } from '../hooks/useSignalREvents';
import { getConnection } from '../services/signalr';

export default function NotificationBell() {
    const { notifications, unreadCount, fetchNotifications, markAllRead, markOneRead, addNotification, addToast } = useTopbar();
    const [open, setOpen] = useState(false);
    const ref = useRef(null);
    const [connection] = useState(getConnection);

    useEffect(() => {
        fetchNotifications();
    }, [fetchNotifications]);

    useEffect(() => {
        if (!localStorage.getItem('token')) return;

        let active = true;
        if (connection.state === HubConnectionState.Disconnected) {
            connection.start().catch(() => {});
        }

        connection.onreconnected(() => {
            if (active) fetchNotifications();
        });

        return () => { active = false; };
    }, [connection, fetchNotifications]);

    useSignalREvents({
        connection,
        eventName: 'NotificationReceived',
        handler: (notification) => {
            addNotification(notification);
            addToast(notification.message);
            if ('Notification' in window && window.Notification.permission === 'granted') {
                new window.Notification('Shellty.Kanban', { body: notification.message });
            }
        },
    });

    useEffect(() => {
        const handler = (e) => {
            if (ref.current && !ref.current.contains(e.target)) setOpen(false);
        };
        document.addEventListener('mousedown', handler);
        return () => document.removeEventListener('mousedown', handler);
    }, []);

    return (
        <div ref={ref} style={{ position: 'relative' }}>
            <button
                onClick={() => setOpen(prev => !prev)}
                style={{
                    background: 'var(--bg-secondary)',
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--radius)',
                    padding: '6px 10px',
                    cursor: 'pointer',
                    fontSize: '16px',
                    position: 'relative',
                    color: 'var(--text-primary)'
                }}
            >
                🔔
                {unreadCount > 0 && (
                    <span style={{
                        position: 'absolute',
                        top: '-6px',
                        right: '-6px',
                        background: '#ef4444',
                        color: '#fff',
                        fontSize: '10px',
                        fontWeight: '700',
                        borderRadius: '50%',
                        width: '16px',
                        height: '16px',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center'
                    }}>
                        {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                )}
            </button>

            {open && (
                <div style={{
                    position: 'absolute',
                    top: '38px',
                    right: 0,
                    width: '320px',
                    background: 'var(--bg-card)',
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--radius-lg)',
                    boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
                    zIndex: 1000,
                    overflow: 'hidden'
                }}>
                    <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '12px 16px',
                        borderBottom: '1px solid var(--border)'
                    }}>
                        <span style={{ fontSize: '13px', fontWeight: '600', color: 'var(--text-primary)' }}>
                            Notifications
                        </span>
                        {unreadCount > 0 && (
                            <button
                                onClick={markAllRead}
                                style={{
                                    fontSize: '11px',
                                    color: 'var(--accent-cyan)',
                                    background: 'none',
                                    border: 'none',
                                    cursor: 'pointer'
                                }}
                            >
                                Mark all as read
                            </button>
                        )}
                    </div>

                    <div style={{ maxHeight: '320px', overflowY: 'auto' }}>
                        {notifications.length === 0 ? (
                            <p style={{
                                padding: '20px',
                                textAlign: 'center',
                                fontSize: '13px',
                                color: 'var(--text-secondary)'
                            }}>
                                No notifications
                            </p>
                        ) : (
                            notifications.map(n => (
                                <div
                                    key={n.id}
                                    onClick={() => !n.isRead && markOneRead(n.id)}
                                    style={{
                                        padding: '12px 16px',
                                        borderBottom: '1px solid var(--border)',
                                        background: n.isRead ? 'transparent' : 'var(--bg-hover)',
                                        cursor: n.isRead ? 'default' : 'pointer',
                                        transition: 'background 0.2s'
                                    }}
                                >
                                    <p style={{ fontSize: '13px', color: 'var(--text-primary)', marginBottom: '4px' }}>
                                        {n.message}
                                    </p>
                                    <p style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                                        {new Date(n.createdAt).toLocaleString()}
                                    </p>
                                </div>
                            ))
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}
