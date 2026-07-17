import { useState, useEffect, useCallback } from 'react';

const STORAGE_KEY = 'kanban-nav-dock';

const DEFAULTS = { mode: 'left', collapsed: false, position: { x: 24, y: 96 } };

const VALID_MODES = ['left', 'right', 'bottom', 'floating'];

function load() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) return DEFAULTS;
        const parsed = JSON.parse(raw);
        return {
            mode: VALID_MODES.includes(parsed.mode) ? parsed.mode : DEFAULTS.mode,
            collapsed: Boolean(parsed.collapsed),
            position: parsed.position && typeof parsed.position.x === 'number' ? parsed.position : DEFAULTS.position,
        };
    } catch {
        return DEFAULTS;
    }
}

export function useNavDock() {
    const [state, setState] = useState(load);

    useEffect(() => {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        } catch {
            void 0;
        }
    }, [state]);

    const setMode = useCallback((mode) => setState((s) => ({ ...s, mode })), []);
    const setCollapsed = useCallback((collapsed) => setState((s) => ({ ...s, collapsed })), []);
    const setPosition = useCallback((position) => setState((s) => ({ ...s, position })), []);
    const toggleCollapsed = useCallback(() => setState((s) => ({ ...s, collapsed: !s.collapsed })), []);

    return { ...state, setMode, setCollapsed, setPosition, toggleCollapsed };
}
