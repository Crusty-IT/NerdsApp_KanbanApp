export default function ConfirmDialog({ title, message, confirmLabel = 'Delete', onConfirm, onCancel }) {
    return (
        <div className="modal-overlay" onClick={onCancel}>
            <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: '400px' }}>
                <h2 style={{ color: 'var(--color-red)', marginBottom: '12px' }}>{title}</h2>
                <p style={{ fontSize: '14px', color: 'var(--text-secondary)', lineHeight: '1.6', marginBottom: '24px' }}>
                    {message}
                </p>
                <div className="modal-actions">
                    <button className="btn-danger" onClick={onConfirm}>{confirmLabel}</button>
                    <button className="btn-secondary" onClick={onCancel}>Cancel</button>
                </div>
            </div>
        </div>
    );
}