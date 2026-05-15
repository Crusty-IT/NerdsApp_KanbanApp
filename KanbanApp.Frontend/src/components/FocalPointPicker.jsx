import { useState, useRef, useEffect } from 'react';

export default function FocalPointPicker({ src, onConfirm, onCancel, aspectRatio = '16 / 9' }) {
    const [pos, setPos] = useState({ x: 50, y: 50 });
    const containerRef = useRef(null);

    useEffect(() => {
        return () => {
            if (src && src.startsWith('blob:')) URL.revokeObjectURL(src);
        };
    }, [src]);

    const updatePos = (clientX, clientY) => {
        const rect = containerRef.current.getBoundingClientRect();
        const x = Math.round(Math.max(0, Math.min(100, ((clientX - rect.left) / rect.width) * 100)));
        const y = Math.round(Math.max(0, Math.min(100, ((clientY - rect.top) / rect.height) * 100)));
        setPos({ x, y });
    };

    const handlePointerDown = (e) => {
        e.currentTarget.setPointerCapture(e.pointerId);
        updatePos(e.clientX, e.clientY);
    };

    const handlePointerMove = (e) => {
        if (e.buttons > 0) updatePos(e.clientX, e.clientY);
    };

    return (
        <div className="modal-overlay" onClick={onCancel}>
            <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '640px', maxHeight: '90vh', overflowY: 'auto' }}>
                <h2>Set focal point</h2>
                <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginBottom: '12px' }}>
                    Click or drag on the image to set the center of the visible area.
                </p>

                <div
                    ref={containerRef}
                    onPointerDown={handlePointerDown}
                    onPointerMove={handlePointerMove}
                    style={{ position: 'relative', userSelect: 'none', cursor: 'crosshair', borderRadius: 'var(--radius)', overflow: 'hidden', border: '1px solid var(--border)' }}
                >
                    <img src={src} alt="" style={{ width: '100%', display: 'block', maxHeight: '360px', objectFit: 'contain', background: 'var(--bg-secondary)' }} draggable={false} />

                    <div style={{
                        position: 'absolute',
                        left: `${pos.x}%`, top: `${pos.y}%`,
                        transform: 'translate(-50%, -50%)',
                        width: '22px', height: '22px',
                        borderRadius: '50%',
                        background: 'rgba(255,255,255,0.95)',
                        border: '2px solid var(--accent-cyan)',
                        boxShadow: '0 0 0 2px rgba(0,0,0,0.45)',
                        pointerEvents: 'none'
                    }} />
                    <div style={{ position: 'absolute', left: `${pos.x}%`, top: 0, bottom: 0, width: '1px', background: 'rgba(255,255,255,0.35)', transform: 'translateX(-50%)', pointerEvents: 'none' }} />
                    <div style={{ position: 'absolute', top: `${pos.y}%`, left: 0, right: 0, height: '1px', background: 'rgba(255,255,255,0.35)', transform: 'translateY(-50%)', pointerEvents: 'none' }} />
                </div>

                <p style={{ fontSize: '12px', color: 'var(--text-secondary)', margin: '12px 0 6px' }}>Preview</p>
                <div style={{ aspectRatio, borderRadius: 'var(--radius)', overflow: 'hidden', border: '1px solid var(--border)', background: 'var(--bg-secondary)' }}>
                    <img src={src} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', objectPosition: `${pos.x}% ${pos.y}%`, display: 'block' }} />
                </div>

                <div className="modal-actions" style={{ marginTop: '16px' }}>
                    <button className="btn-primary" onClick={() => onConfirm(`${pos.x}% ${pos.y}%`)}>Use this</button>
                    <button className="btn-secondary" onClick={onCancel}>Cancel</button>
                </div>
            </div>
        </div>
    );
}
