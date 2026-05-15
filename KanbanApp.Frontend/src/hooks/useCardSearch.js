import { useCallback, useState } from 'react';
import api from '../services/api';

export function useCardSearch(boardId, setError) {
    const [searchQuery, setSearchQuery] = useState('');
    const [searchResults, setSearchResults] = useState(null);

    const handleSearch = useCallback(async () => {
        if (!searchQuery.trim()) return;
        try {
            const res = await api.get(`/api/boards/${boardId}/cards/search`, {
                params: { q: searchQuery.trim() }
            });
            setSearchResults(res.data);
        } catch {
            setError('Search failed');
            setTimeout(() => setError(null), 3000);
        }
    }, [boardId, searchQuery, setError]);

    const clearSearch = useCallback(() => {
        setSearchQuery('');
        setSearchResults(null);
    }, []);

    return { searchQuery, setSearchQuery, searchResults, handleSearch, clearSearch };
}
