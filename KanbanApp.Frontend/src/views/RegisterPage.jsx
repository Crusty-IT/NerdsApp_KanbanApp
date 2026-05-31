import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../services/api';
import PrivacyPolicyModal from '../components/PrivacyPolicyModal';

const PASSWORD_RULES = [
    { test: p => p.length >= 8, label: 'At least 8 characters' },
    { test: p => /[A-Z]/.test(p), label: 'At least one uppercase letter' },
    { test: p => /[0-9]/.test(p), label: 'At least one number' },
    { test: p => /[^a-zA-Z0-9]/.test(p), label: 'At least one special character' },
];

export default function RegisterPage() {
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const [privacyOpen, setPrivacyOpen] = useState(false);
    const navigate = useNavigate();

    const passwordValid = PASSWORD_RULES.every(r => r.test(password));

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!passwordValid) return;
        setError('');
        setLoading(true);
        try {
            await api.post('/register', { userName: username, email, password });
            const loginResponse = await api.post('/login', { email, password });
            localStorage.setItem('token', loginResponse.data.accessToken);
            navigate('/dashboard');
        } catch (err) {
            setError(err.response?.data?.message || 'Registration failed');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-layout">
            <div className="auth-card fade-in">
                <h1>Create account</h1>
                {error && <p className="error-msg" style={{ marginBottom: '16px' }}>{error}</p>}
                <form className="auth-form" onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label>Username</label>
                        <input
                            type="text"
                            value={username}
                            onChange={e => setUsername(e.target.value)}
                            placeholder="johndoe"
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label>Email</label>
                        <input
                            type="email"
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            placeholder="you@example.com"
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label>Password</label>
                        <input
                            type="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            placeholder="••••••••"
                            required
                        />
                        {password.length > 0 && (
                            <div style={{ marginTop: '8px', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                {PASSWORD_RULES.map(rule => (
                                    <div key={rule.label} style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px' }}>
                                        <span style={{ color: rule.test(password) ? 'var(--color-green)' : 'var(--color-red)' }}>
                                            {rule.test(password) ? '✓' : '✗'}
                                        </span>
                                        <span style={{ color: rule.test(password) ? 'var(--color-green)' : 'var(--text-secondary)' }}>
                                            {rule.label}
                                        </span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                    <button type="submit" className="btn-primary" disabled={loading || !passwordValid}>
                        {loading ? 'Creating...' : 'Create Account →'}
                    </button>
                </form>
                <p className="auth-footer">
                    Already have an account? <Link to="/login">Sign in</Link>
                </p>
                <p style={{ textAlign: 'center', fontSize: '11px', color: 'var(--text-secondary)', marginTop: '12px' }}>
                    By registering you agree to our{' '}
                    <button
                        type="button"
                        onClick={() => setPrivacyOpen(true)}
                        style={{ background: 'none', border: 'none', color: 'var(--accent-cyan)', cursor: 'pointer', fontSize: '11px', textDecoration: 'underline', padding: 0 }}
                    >
                        Privacy Policy
                    </button>
                </p>
            </div>

            <PrivacyPolicyModal isOpen={privacyOpen} onClose={() => setPrivacyOpen(false)} />
        </div>
    );
}