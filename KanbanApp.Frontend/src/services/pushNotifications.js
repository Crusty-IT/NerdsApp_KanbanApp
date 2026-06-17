import api from './api';

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = window.atob(base64);
    return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)));
}

export function isPushSupported() {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
}

export async function getPushConfig() {
    const response = await api.get('/api/push/config');
    return response.data;
}

export async function getPushPreferences() {
    const response = await api.get('/api/notifications/preferences');
    return response.data;
}

export async function updatePushPreferences(preferences) {
    const response = await api.put('/api/notifications/preferences', preferences);
    return response.data;
}

async function getRegistration() {
    return navigator.serviceWorker.register('/push-sw.js');
}

export async function getCurrentPushSubscription() {
    if (!isPushSupported()) return null;
    const registration = await getRegistration();
    return registration.pushManager.getSubscription();
}

export async function subscribeToPush() {
    if (!isPushSupported()) {
        return { ok: false, reason: 'unsupported' };
    }

    const config = await getPushConfig();
    if (!config.isConfigured || !config.publicKey) {
        return { ok: false, reason: 'not-configured' };
    }

    const permission = await window.Notification.requestPermission();
    if (permission !== 'granted') {
        return { ok: false, reason: 'permission-denied' };
    }

    const registration = await getRegistration();
    let subscription = await registration.pushManager.getSubscription();
    if (!subscription) {
        subscription = await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(config.publicKey)
        });
    }

    await api.post('/api/push/subscriptions', {
        ...subscription.toJSON(),
        userAgent: navigator.userAgent
    });

    await updatePushPreferences({ webPushEnabled: true });
    return { ok: true, subscription };
}

export async function unsubscribeFromPush() {
    if (!isPushSupported()) return { ok: true };

    const subscription = await getCurrentPushSubscription();
    const endpoint = subscription?.endpoint;

    if (endpoint) {
        await api.delete('/api/push/subscriptions', { data: { endpoint } });
        await subscription.unsubscribe().catch(() => {});
    } else {
        await api.delete('/api/push/subscriptions', { data: {} }).catch(() => {});
    }

    await updatePushPreferences({ webPushEnabled: false });
    return { ok: true };
}

export async function removePushSubscriptionOnLogout() {
    if (!isPushSupported()) return;
    const subscription = await getCurrentPushSubscription().catch(() => null);
    if (!subscription?.endpoint) return;

    await api.delete('/api/push/subscriptions', {
        data: { endpoint: subscription.endpoint }
    }).catch(() => {});
    await subscription.unsubscribe().catch(() => {});
}
