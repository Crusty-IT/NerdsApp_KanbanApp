export default function ColumnForm({ columnName, setColumnName, columnColor, setColumnColor, isEditing, onSubmit, onClose, COLORS }) {
    return (
        <div style={{ minWidth: '260px', background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 'var(--radius-lg)', padding: '14px', flexShrink: 0 }}>
            <h3 style={{ fontSize: '13px', marginBottom: '10px', color: 'var(--text-primary)' }}>
                {isEditing ? 'Edit Column' : 'New Column'}
            </h3>
            <form onSubmit={onSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                <input
                    type="text"
                    value={columnName}
                    onChange={e => setColumnName(e.target.value)}
                    placeholder="Column name..."
                    autoFocus
                />
                <div>
                    <p style={{ fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '6px', fontFamily: 'var(--font-mono)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                        Color
                    </p>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                        {COLORS.map(c => (
                            <button key={c} type="button" onClick={() => setColumnColor(c)} style={{
                                width: '22px', height: '22px', borderRadius: '50%', background: c,
                                border: columnColor === c ? '3px solid #fff' : '2px solid transparent',
                                outline: columnColor === c ? `2px solid ${c}` : 'none',
                                padding: 0, cursor: 'pointer'
                            }} />
                        ))}
                    </div>
                </div>
                <div style={{ display: 'flex', gap: '6px' }}>
                    <button type="submit" className="btn-primary" style={{ flex: 1 }}>
                        {isEditing ? 'Update' : 'Add'}
                    </button>
                    <button type="button" className="btn-secondary" onClick={onClose}>✕</button>
                </div>
            </form>
        </div>
    );
}