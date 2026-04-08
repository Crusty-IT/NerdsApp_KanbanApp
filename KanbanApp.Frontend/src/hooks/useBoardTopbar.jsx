import { useEffect } from 'react';
import { useTopbar } from '../context/TopbarContext';

export function useBoardTopbar({
                                   board,
                                   boardMembers,
                                   filterUserId,
                                   setFilterUserId,
                                   searchQuery,
                                   setSearchQuery,
                                   handleSearch,
                                   clearSearch,
                                   navigate,
                                   setShowInvite
                               }) {
    const { setTitle, setActions } = useTopbar();

    useEffect(() => {
        return () => {
            setTitle('');
            setActions(null);
        };
    }, [setTitle, setActions]);

    useEffect(() => {
        if (!board) return;

        setTitle(board.name);
        setActions({
            left: (
                <button className="btn-secondary" onClick={() => navigate(-1)} style={{ fontSize: '14px' }}>
                    ← Back
                </button>
            ),
            right: (
                <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    <div style={{ display: 'flex', gap: '4px' }}>
                        <input
                            type="text"
                            value={searchQuery}
                            onChange={e => setSearchQuery(e.target.value)}
                            onKeyDown={e => e.key === 'Enter' && handleSearch()}
                            placeholder="Search cards..."
                            style={{
                                background: 'var(--bg-secondary)',
                                border: '1px solid var(--border)',
                                borderRadius: 'var(--radius)',
                                color: 'var(--text-primary)',
                                padding: '6px 10px',
                                fontSize: '13px',
                                width: '160px'
                            }}
                        />
                        {searchQuery && (
                            <button className="btn-secondary" onClick={clearSearch} style={{ padding: '6px 8px' }}>
                                ✕
                            </button>
                        )}
                    </div>

                    <select
                        value={filterUserId}
                        onChange={e => setFilterUserId(e.target.value)}
                        style={{
                            background: 'var(--bg-secondary)',
                            border: '1px solid var(--border)',
                            borderRadius: 'var(--radius)',
                            color: 'var(--text-primary)',
                            padding: '6px 10px',
                            fontSize: '13px',
                            cursor: 'pointer'
                        }}
                    >
                        <option value="">All members</option>
                        {boardMembers.map(m => (
                            <option key={m.userId} value={m.userId}>
                                {m.userName || m.email}
                            </option>
                        ))}
                    </select>

                    <button className="btn-secondary" onClick={() => setShowInvite(true)}>
                        👥 Invite
                    </button>
                </div>
            )
        });
    }, [
        board,
        boardMembers,
        filterUserId,
        searchQuery,
        setFilterUserId,
        setSearchQuery,
        handleSearch,
        clearSearch,
        navigate,
        setShowInvite,
        setTitle,
        setActions
    ]);
}