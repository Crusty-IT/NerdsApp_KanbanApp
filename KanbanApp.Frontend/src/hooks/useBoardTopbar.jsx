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
                                   setShowInvite,
                                   setShowMembers
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
                <button className="btn-secondary" onClick={() => navigate(-1)} style={{ fontSize: '14px', padding: '8px 16px' }}>
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
                                padding: '8px 12px',
                                fontSize: '13px',
                                width: '160px'
                            }}
                        />
                        {searchQuery && (
                            <button className="btn-secondary" onClick={clearSearch} style={{ padding: '8px 16px', fontSize: '13px' }}>
                                ✕
                            </button>
                        )}
                    </div>

                    <button className="btn-secondary" onClick={() => setShowMembers(true)} style={{ fontSize: '13px', padding: '8px 16px' }}>
                        Members
                    </button>

                    <button className="btn-secondary" onClick={() => setShowInvite(true)} style={{ fontSize: '13px', padding: '8px 16px' }}>
                        👥 Invite
                    </button>
                </div>
            )
        });
    }, [board?.id, board?.name, boardMembers.length, filterUserId, searchQuery]);
}