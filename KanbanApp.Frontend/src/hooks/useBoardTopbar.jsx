import { useEffect } from 'react';
import { useTopbar } from '../context/TopbarContext';
import PresenceBar from '../components/PresenceBar';

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
                                   setShowMembers,
                                   presenceUsers = []
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
                <button className="btn-secondary topbar-back-button" onClick={() => navigate(-1)}>
                    Back
                </button>
            ),
            right: (
                <div className="board-topbar-actions">
                    <PresenceBar users={presenceUsers} />

                    <div className="board-search">
                        <input
                            type="text"
                            aria-label="Search cards"
                            value={searchQuery}
                            onChange={e => setSearchQuery(e.target.value)}
                            onKeyDown={e => e.key === 'Enter' && handleSearch()}
                            placeholder="Search cards..."
                            className="board-search-input"
                        />
                        {searchQuery && (
                            <button className="btn-secondary board-search-clear" onClick={clearSearch} aria-label="Clear search">
                                x
                            </button>
                        )}
                    </div>

                    <select
                        aria-label="Filter by member"
                        value={filterUserId}
                        onChange={e => setFilterUserId(e.target.value)}
                        className="board-member-filter"
                    >
                        <option value="">All members</option>
                        {boardMembers.map(m => (
                            <option key={m.userId} value={m.userId}>
                                {m.userName || m.email}
                            </option>
                        ))}
                    </select>

                    <button className="btn-secondary board-action-button" onClick={() => setShowInvite(true)}>
                        Invite
                    </button>

                    {board.projectId && (
                        <button className="btn-secondary board-action-button" onClick={() => setShowMembers(true)}>
                            Members
                        </button>
                    )}
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
        setShowMembers,
        presenceUsers,
        setTitle,
        setActions
    ]);
}
