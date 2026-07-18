import { useState, useRef, useEffect } from 'react';
import { NavLink, Link } from 'react-router-dom';
import NavIcon, { GripIcon } from './NavIcons';

const NAV = [
    { to: '/dashboard', label: 'Projects', icon: 'projects' },
    { to: '/profile', label: 'Profile', icon: 'profile' },
];

const POSITIONS = [
    { value: 'left', label: 'Pin to left', icon: 'panel-left' },
    { value: 'right', label: 'Pin to right', icon: 'panel-right' },
    { value: 'bottom', label: 'Pin to bottom', icon: 'panel-bottom' },
    { value: 'floating', label: 'Floating', icon: 'move' },
];

const EXPANDED = 240;
const COLLAPSED = 76;
const EDGE = 12;
const SNAP = 84;

const clamp = (v, min, max) => Math.min(Math.max(v, min), Math.max(min, max));

function initials(user) {
    if (!user) return 'U';
    return (user.userName || user.email || 'U').slice(0, 2).toUpperCase();
}

export default function Sidebar({ user, onLogout, onOpenPrivacy, dock, isMobile }) {
    const { mode, collapsed, position, setMode, setPosition, toggleCollapsed } = dock;

    const effectiveMode = isMobile ? 'bottom' : mode;
    const isBottom = effectiveMode === 'bottom';
    const isRight = effectiveMode === 'right';
    const isFloating = effectiveMode === 'floating';
    const isCollapsed = isMobile ? true : collapsed;

    const [pinOpen, setPinOpen] = useState(false);
    const [drag, setDrag] = useState(null);
    const pinRef = useRef(null);
    const panelRef = useRef(null);
    const dragOffset = useRef({ x: 0, y: 0 });
    const dragRef = useRef(null);
    const rafId = useRef(null);

    const panelWidth = isCollapsed ? COLLAPSED : EXPANDED;
    const dragging = drag !== null;

    useEffect(() => () => {
        if (rafId.current !== null) cancelAnimationFrame(rafId.current);
    }, []);

    useEffect(() => {
        if (!pinOpen) return;
        const handler = (e) => {
            if (pinRef.current && !pinRef.current.contains(e.target)) setPinOpen(false);
        };
        document.addEventListener('mousedown', handler);
        return () => document.removeEventListener('mousedown', handler);
    }, [pinOpen]);

    const startDrag = (e) => {
        if (isMobile || e.button !== 0) return;
        e.preventDefault();
        const rect = panelRef.current?.getBoundingClientRect();
        dragOffset.current = rect
            ? { x: clamp(e.clientX - rect.left, 12, rect.width - 24), y: clamp(e.clientY - rect.top, 8, 48) }
            : { x: 24, y: 24 };
        try { e.currentTarget.setPointerCapture(e.pointerId); } catch { void 0; }
        const next = { x: e.clientX - dragOffset.current.x, y: e.clientY - dragOffset.current.y, px: e.clientX, py: e.clientY };
        dragRef.current = next;
        setDrag(next);
    };

    const moveDrag = (e) => {
        if (!dragRef.current) return;
        const { clientX, clientY } = e;
        if (rafId.current !== null) cancelAnimationFrame(rafId.current);
        rafId.current = requestAnimationFrame(() => {
            rafId.current = null;
            const next = { x: clientX - dragOffset.current.x, y: clientY - dragOffset.current.y, px: clientX, py: clientY };
            dragRef.current = next;
            setDrag(next);
        });
    };

    const cancelDrag = () => {
        dragRef.current = null;
        setDrag(null);
    };

    const endDrag = (e) => {
        const current = dragRef.current;
        if (!current) return;
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        if (e.clientX <= SNAP) {
            setMode('left');
        } else if (vw - e.clientX <= SNAP) {
            setMode('right');
        } else if (vh - e.clientY <= SNAP) {
            setMode('bottom');
        } else {
            const h = panelRef.current?.getBoundingClientRect().height ?? 480;
            setPosition({
                x: clamp(current.x, EDGE, vw - panelWidth - EDGE),
                y: clamp(current.y, EDGE, vh - Math.min(h, vh - EDGE * 2) - EDGE),
            });
            setMode('floating');
        }
        cancelDrag();
    };

    const nearLeft = dragging && drag.px <= SNAP;
    const nearRight = dragging && window.innerWidth - drag.px <= SNAP;
    const nearBottom = dragging && !nearLeft && !nearRight && window.innerHeight - drag.py <= SNAP;

    const grip = (
        <button
            type="button"
            aria-label="Move menu"
            title="Drag to reposition"
            className={`nav-grip${dragging ? ' dragging' : ''}`}
            onPointerDown={startDrag}
            onPointerMove={moveDrag}
            onPointerUp={endDrag}
            onPointerCancel={cancelDrag}
        >
            <GripIcon />
        </button>
    );

    const pinMenu = (
        <div className="nav-pin" ref={pinRef}>
            <button
                type="button"
                className="nav-iconbtn"
                aria-label="Menu position"
                title="Menu position"
                onClick={() => setPinOpen((v) => !v)}
            >
                <NavIcon name="pin" size={15} />
            </button>
            {pinOpen && (
                <div className={`nav-pin-menu${isBottom ? ' up' : ''}${isRight ? ' left' : ''}`}>
                    <div className="nav-pin-title">Dock position</div>
                    {POSITIONS.map((p) => (
                        <button
                            key={p.value}
                            type="button"
                            className={`nav-pin-item${mode === p.value ? ' active' : ''}`}
                            onClick={() => { setMode(p.value); setPinOpen(false); }}
                        >
                            <NavIcon name={p.icon} size={16} />
                            <span>{p.label}</span>
                        </button>
                    ))}
                </div>
            )}
        </div>
    );

    const navItems = NAV.map((item) => (
        <NavLink
            key={item.to}
            to={item.to}
            title={isCollapsed ? item.label : undefined}
            className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
        >
            <span className="nav-item-icon"><NavIcon name={item.icon} /></span>
            {!isCollapsed && <span className="nav-item-label">{item.label}</span>}
        </NavLink>
    ));

    const avatar = (
        <div className="nav-avatar" title={user ? `${user.userName}\n${user.email}` : undefined}>
            {user?.profilePictureUrl
                ? <img src={user.profilePictureUrl} alt="avatar" />
                : initials(user)}
        </div>
    );

    const logoutBtn = (label) => (
        <button type="button" className="nav-iconbtn danger" onClick={onLogout} title="Logout" aria-label="Logout">
            <NavIcon name="logout" size={17} />
            {label && <span>Logout</span>}
        </button>
    );

    const brand = (
        <Link to="/dashboard" className="nav-brand" title="Shellty.Kanban">
            <img src="/logo.png" alt="KanbanApp logo" />
            {!isCollapsed && (
                <span className="nav-brand-text">
                    <span className="logo-shell">Shell</span><span className="logo-ty">ty</span><span className="logo-dot">.Kanban</span>
                </span>
            )}
        </Link>
    );

    const collapseIcon = isBottom
        ? (isCollapsed ? 'chevron-up' : 'chevron-down')
        : isCollapsed
            ? (isRight ? 'chevron-left' : 'chevron-right')
            : (isRight ? 'chevron-right' : 'chevron-left');

    const collapseBtn = (
        <button
            type="button"
            className="nav-iconbtn"
            title={isCollapsed ? 'Expand' : 'Collapse'}
            aria-label={isCollapsed ? 'Expand menu' : 'Collapse menu'}
            onClick={toggleCollapsed}
        >
            <NavIcon name={collapseIcon} size={16} />
        </button>
    );

    let panelBody;
    if (isBottom) {
        panelBody = (
            <div className={`nav-bar${isCollapsed ? ' is-collapsed' : ''}`}>
                {!isMobile && grip}
                {brand}
                <span className="nav-sep" />
                <nav className="nav-list row">{navItems}</nav>
                <span className="nav-sep" />
                {avatar}
                {logoutBtn(false)}
                {!isMobile && collapseBtn}
                {!isMobile && pinMenu}
            </div>
        );
    } else {
        panelBody = (
            <>
                <div className="nav-brandrow">
                    {brand}
                </div>

                <div className={`nav-toolbar${isCollapsed ? ' is-collapsed' : ''}`}>
                    {grip}
                    {!isCollapsed && <span className="nav-toolbar-spacer" />}
                    {pinMenu}
                    {collapseBtn}
                </div>

                {!isCollapsed && <div className="nav-section-label">Menu</div>}
                <nav className="nav-list">{navItems}</nav>

                <div className="nav-footer">
                    {isCollapsed ? (
                        <div className="nav-footer-collapsed">
                            {avatar}
                            {logoutBtn(false)}
                        </div>
                    ) : (
                        <>
                            {user && (
                                <div className="nav-user">
                                    {avatar}
                                    <div className="nav-user-info">
                                        <p className="nav-user-name">{user.userName}</p>
                                        <p className="nav-user-mail">{user.email}</p>
                                    </div>
                                </div>
                            )}
                            {logoutBtn(true)}
                            <div className="nav-legal">
                                <span>Copyright 2026 Shellty IT</span>
                                <button type="button" onClick={onOpenPrivacy}>Privacy Policy</button>
                            </div>
                        </>
                    )}
                </div>
            </>
        );
    }

    const dockStyle = dragging
        ? { left: drag.x, top: drag.y }
        : isFloating
            ? { left: position.x, top: position.y }
            : undefined;

    const dockClass = [
        'nav-dock',
        `nav-dock--${effectiveMode}`,
        isCollapsed ? 'is-collapsed' : '',
        dragging ? 'is-dragging' : '',
    ].filter(Boolean).join(' ');

    return (
        <>
            {dragging && (
                <div className="nav-snap-layer">
                    <div className={`nav-snap left${nearLeft ? ' active' : ''}`} />
                    <div className={`nav-snap right${nearRight ? ' active' : ''}`} />
                    <div className={`nav-snap bottom${nearBottom ? ' active' : ''}`} />
                </div>
            )}
            <aside className={dockClass} style={dockStyle}>
                <div className="nav-panel" ref={panelRef}>
                    {panelBody}
                </div>
            </aside>
        </>
    );
}
