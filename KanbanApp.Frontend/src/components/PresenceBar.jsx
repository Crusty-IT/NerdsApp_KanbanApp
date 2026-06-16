import { useState } from 'react';

function getDisplayName(user) {
    return user.userName || user.email || 'Unknown user';
}

function getInitials(name) {
    return name
        .trim()
        .split(/\s+/)
        .map(part => part[0])
        .join('')
        .slice(0, 2)
        .toUpperCase() || '??';
}

export default function PresenceBar({ users = [] }) {
    const [open, setOpen] = useState(false);
    const uniqueUsers = Array.from(new Map(users.map(user => [user.userId, user])).values());
    const visibleUsers = uniqueUsers.slice(0, 4);
    const hiddenCount = Math.max(0, uniqueUsers.length - visibleUsers.length);
    const label = uniqueUsers.length === 1 ? '1 person online' : `${uniqueUsers.length} people online`;

    return (
        <div className="presence-bar">
            <button
                type="button"
                className="presence-trigger"
                aria-label={label}
                aria-expanded={open}
                onClick={() => setOpen(prev => !prev)}
                onBlur={(event) => {
                    if (!event.currentTarget.parentElement?.contains(event.relatedTarget)) {
                        setOpen(false);
                    }
                }}
            >
                <span className="presence-dot" aria-hidden="true" />
                {visibleUsers.map(user => {
                    const name = getDisplayName(user);
                    return (
                        <span key={user.userId} className="presence-avatar" title={name}>
                            {getInitials(name)}
                        </span>
                    );
                })}
                {hiddenCount > 0 && <span className="presence-more">+{hiddenCount}</span>}
                {uniqueUsers.length === 0 && <span className="presence-empty">0</span>}
            </button>

            {open && (
                <div className="presence-popover" role="status">
                    <p>{label}</p>
                    {uniqueUsers.length > 0 ? (
                        <ul>
                            {uniqueUsers.map(user => (
                                <li key={user.userId}>{getDisplayName(user)}</li>
                            ))}
                        </ul>
                    ) : (
                        <span>No one is online yet</span>
                    )}
                </div>
            )}
        </div>
    );
}
