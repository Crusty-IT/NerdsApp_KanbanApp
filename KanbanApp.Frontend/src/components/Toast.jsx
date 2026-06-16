import { useState, useEffect } from 'react';

export default function Toast({ message, duration = 3000, onClose }) {
    const [visible, setVisible] = useState(false);

    useEffect(() => {
        const raf = requestAnimationFrame(() => setVisible(true));
        const fadeOut = setTimeout(() => setVisible(false), duration - 300);
        const close = setTimeout(onClose, duration);
        return () => {
            cancelAnimationFrame(raf);
            clearTimeout(fadeOut);
            clearTimeout(close);
        };
    }, []);

    return (
        <div
            role="alert"
            aria-live="polite"
            style={{
                opacity: visible ? 1 : 0,
                transform: visible ? 'translateY(0)' : 'translateY(16px)',
                transition: 'opacity 0.3s ease, transform 0.3s ease',
                background: 'var(--bg-card)',
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius)',
                padding: '12px 40px 12px 16px',
                maxWidth: '320px',
                fontSize: '13px',
                color: 'var(--text-primary)',
                boxShadow: '0 4px 16px rgba(0,0,0,0.3)',
                position: 'relative',
                pointerEvents: 'auto',
            }}
        >
            {message}
            <button
                onClick={onClose}
                aria-label="Dismiss notification"
                style={{
                    position: 'absolute',
                    top: '8px',
                    right: '8px',
                    background: 'none',
                    border: 'none',
                    color: 'var(--text-secondary)',
                    cursor: 'pointer',
                    fontSize: '14px',
                    lineHeight: 1,
                    padding: '2px 4px',
                    borderRadius: '4px',
                }}
            >
                ×
            </button>
        </div>
    );
}
