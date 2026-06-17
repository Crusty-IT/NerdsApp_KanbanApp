import { useState, useEffect, useRef } from 'react';
import api from '../services/api';
import {
    getCurrentPushSubscription,
    getPushConfig,
    getPushPreferences,
    isPushSupported,
    subscribeToPush,
    unsubscribeFromPush,
    updatePushPreferences
} from '../services/pushNotifications';

export default function ProfilePage() {
    const [profile, setProfile] = useState(null);
    const [loading, setLoading] = useState(true);
    const [fetchError, setFetchError] = useState(false);
    const [bio, setBio] = useState('');
    const [saving, setSaving] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [message, setMessage] = useState(null);
    const [isError, setIsError] = useState(false);
    const [pushConfig, setPushConfig] = useState({ isConfigured: false, publicKey: null });
    const [pushPreferences, setPushPreferences] = useState(null);
    const [pushEnabledOnDevice, setPushEnabledOnDevice] = useState(false);
    const [pushBusy, setPushBusy] = useState(false);
    const fileRef = useRef();

    useEffect(() => {
        let ignore = false;
        async function fetchProfile() {
            try {
                const response = await api.get('/api/users/me');
                if (!ignore) {
                    setProfile(response.data);
                    setBio(response.data.bio || '');
                }
            } catch {
                if (!ignore) setFetchError(true);
            } finally {
                if (!ignore) setLoading(false);
            }
        }
        fetchProfile();
        return () => { ignore = true; };
    }, []);

    useEffect(() => {
        let ignore = false;
        async function fetchPushSettings() {
            try {
                const [config, preferences, subscription] = await Promise.all([
                    getPushConfig(),
                    getPushPreferences(),
                    getCurrentPushSubscription().catch(() => null)
                ]);
                if (!ignore) {
                    setPushConfig(config);
                    setPushPreferences(preferences);
                    setPushEnabledOnDevice(Boolean(subscription));
                }
            } catch {
                if (!ignore) {
                    setPushPreferences({
                        webPushEnabled: false,
                        notifyCardCreated: true,
                        notifyCardUpdated: true,
                        notifyCardMoved: true,
                        notifyCardDeleted: true,
                        notifyCardAssigned: true,
                        includeCardDetails: true
                    });
                }
            }
        }
        fetchPushSettings();
        return () => { ignore = true; };
    }, []);

    const handleSave = async (e) => {
        e.preventDefault();
        setSaving(true);
        try {
            const response = await api.put('/api/users/me', { bio });
            setProfile(response.data);
            setMessage('Profile updated successfully');
            setIsError(false);
        } catch {
            setMessage('Failed to update profile');
            setIsError(true);
        } finally {
            setSaving(false);
            setTimeout(() => setMessage(null), 3000);
        }
    };

    const handleAvatarUpload = async (e) => {
        const file = e.target.files[0];
        if (!file) return;
        setUploading(true);
        try {
            const formData = new FormData();
            formData.append('file', file);
            const response = await api.post('/api/users/me/avatar', formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            setProfile(response.data);
            setMessage('Avatar updated');
            setIsError(false);
        } catch {
            setMessage('Failed to upload avatar');
            setIsError(true);
        } finally {
            setUploading(false);
            setTimeout(() => setMessage(null), 3000);
        }
    };

    const showMessage = (text, error = false) => {
        setMessage(text);
        setIsError(error);
        setTimeout(() => setMessage(null), 3000);
    };

    const handleEnablePush = async () => {
        setPushBusy(true);
        try {
            const result = await subscribeToPush();
            if (!result.ok) {
                const messages = {
                    unsupported: 'This browser does not support Web Push notifications.',
                    'not-configured': 'Web Push is not configured on the server yet.',
                    'permission-denied': 'Notifications permission was not granted.'
                };
                showMessage(messages[result.reason] || 'Failed to enable push notifications', true);
                return;
            }

            const [preferences, subscription] = await Promise.all([
                getPushPreferences(),
                getCurrentPushSubscription()
            ]);
            setPushPreferences(preferences);
            setPushEnabledOnDevice(Boolean(subscription));
            showMessage('Push notifications enabled');
        } catch {
            showMessage('Failed to enable push notifications', true);
        } finally {
            setPushBusy(false);
        }
    };

    const handleDisablePush = async () => {
        setPushBusy(true);
        try {
            await unsubscribeFromPush();
            const preferences = await getPushPreferences();
            setPushPreferences(preferences);
            setPushEnabledOnDevice(false);
            showMessage('Push notifications disabled');
        } catch {
            showMessage('Failed to disable push notifications', true);
        } finally {
            setPushBusy(false);
        }
    };

    const handlePreferenceChange = async (key, value) => {
        const previous = pushPreferences;
        const next = { ...pushPreferences, [key]: value };
        setPushPreferences(next);
        try {
            const updated = await updatePushPreferences({ [key]: value });
            setPushPreferences(updated);
        } catch {
            setPushPreferences(previous);
            showMessage('Failed to update notification settings', true);
        }
    };

    if (loading) return <div className="loading">Loading profile...</div>;
    if (fetchError || !profile) return (
        <div className="page-content">
            <p className="error-msg">Failed to load profile. Please refresh the page.</p>
        </div>
    );

    const initials = profile.userName?.slice(0, 2).toUpperCase() ?? '??';

    return (
        <div className="page-content fade-in">
            <div className="page-header">
                <h1>Profile</h1>
            </div>

            <div style={{ maxWidth: '720px' }}>
                <div style={{
                    background: 'var(--bg-card)',
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--radius-lg)',
                    padding: '28px',
                    marginBottom: '20px',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '20px'
                }}>
                    <div style={{ position: 'relative', flexShrink: 0 }}>
                        <button
                            type="button"
                            onClick={() => fileRef.current.click()}
                            aria-label="Change avatar"
                            style={{
                                width: '64px',
                                height: '64px',
                                borderRadius: '50%',
                                background: 'var(--accent-indigo)',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                fontSize: '22px',
                                fontWeight: '700',
                                color: '#fff',
                                cursor: 'pointer',
                                overflow: 'hidden',
                                border: '2px solid var(--border)',
                                position: 'relative',
                                padding: 0
                            }}
                        >
                            {profile.profilePictureUrl
                                ? <img src={profile.profilePictureUrl} alt="avatar" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                : initials
                            }
                        </button>
                        <div style={{
                            position: 'absolute',
                            bottom: 0,
                            right: 0,
                            width: '20px',
                            height: '20px',
                            borderRadius: '50%',
                            background: 'var(--bg-secondary)',
                            border: '2px solid var(--border)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: '10px',
                            pointerEvents: 'none'
                        }}>📷</div>
                        <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/webp" style={{ display: 'none' }} onChange={handleAvatarUpload} />
                        {uploading && <div style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '10px', color: 'var(--accent-cyan)' }}>...</div>}
                    </div>
                    <div>
                        <p style={{ fontWeight: '600', fontSize: '16px', marginBottom: '4px' }}>{profile.userName}</p>
                        <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>{profile.email}</p>
                        <span className="tag" style={{ marginTop: '6px', display: 'inline-block' }}>member</span>
                    </div>
                </div>

                <div style={{
                    background: 'var(--bg-card)',
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--radius-lg)',
                    padding: '28px'
                }}>
                    <h2 style={{ fontSize: '16px', marginBottom: '20px' }}>Edit Profile</h2>
                    {message && (
                        <p className={isError ? 'error-msg' : 'success-msg'} style={{ marginBottom: '16px' }}>
                            {message}
                        </p>
                    )}
                    <form className="auth-form" onSubmit={handleSave}>
                        <div className="form-group">
                            <label htmlFor="profile-bio">Bio</label>
                            <textarea
                                id="profile-bio"
                                value={bio}
                                onChange={e => setBio(e.target.value)}
                                placeholder="Tell something about yourself..."
                                rows={4}
                                style={{ resize: 'vertical' }}
                            />
                        </div>
                        <button type="submit" className="btn-primary" disabled={saving}>
                            {saving ? 'Saving...' : 'Save Changes'}
                        </button>
                    </form>
                </div>

                <div style={{
                    background: 'var(--bg-card)',
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--radius-lg)',
                    padding: '28px',
                    marginTop: '20px'
                }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: '16px', alignItems: 'flex-start', marginBottom: '18px' }}>
                        <div>
                            <h2 style={{ fontSize: '16px', marginBottom: '6px' }}>Push Notifications</h2>
                            <p style={{ color: 'var(--text-secondary)', fontSize: '13px', lineHeight: 1.5 }}>
                                Receive system notifications for Kanban changes on this device.
                            </p>
                        </div>
                        {pushEnabledOnDevice ? (
                            <button className="btn-secondary" onClick={handleDisablePush} disabled={pushBusy}>
                                {pushBusy ? 'Saving...' : 'Disable'}
                            </button>
                        ) : (
                            <button className="btn-primary" onClick={handleEnablePush} disabled={pushBusy || !isPushSupported() || !pushConfig.isConfigured}>
                                {pushBusy ? 'Saving...' : 'Enable'}
                            </button>
                        )}
                    </div>

                    {!isPushSupported() && (
                        <p className="error-msg" style={{ marginBottom: '12px' }}>
                            This browser does not support Web Push notifications.
                        </p>
                    )}
                    {isPushSupported() && !pushConfig.isConfigured && (
                        <p className="error-msg" style={{ marginBottom: '12px' }}>
                            Web Push keys are not configured on the server.
                        </p>
                    )}

                    {pushPreferences && (
                        <div style={{ display: 'grid', gap: '10px' }}>
                            {[
                                ['notifyCardCreated', 'Card created'],
                                ['notifyCardUpdated', 'Card updated'],
                                ['notifyCardMoved', 'Card moved'],
                                ['notifyCardDeleted', 'Card deleted'],
                                ['notifyCardAssigned', 'Assigned to me'],
                                ['includeCardDetails', 'Show card details in system notifications']
                            ].map(([key, label]) => (
                                <label key={key} style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'space-between',
                                    gap: '12px',
                                    padding: '10px 12px',
                                    background: 'var(--bg-secondary)',
                                    border: '1px solid var(--border)',
                                    borderRadius: 'var(--radius)',
                                    fontSize: '13px',
                                    color: 'var(--text-primary)'
                                }}>
                                    <span>{label}</span>
                                    <input
                                        type="checkbox"
                                        checked={Boolean(pushPreferences[key])}
                                        onChange={e => handlePreferenceChange(key, e.target.checked)}
                                    />
                                </label>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
