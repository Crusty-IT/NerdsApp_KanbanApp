self.addEventListener('push', event => {
    let payload = {};
    try {
        payload = event.data ? event.data.json() : {};
    } catch {
        payload = {};
    }

    const title = payload.title || 'Shellty.Kanban';
    const options = {
        body: payload.body || 'You have a new Kanban notification.',
        icon: '/android-chrome-192x192.png',
        badge: '/android-chrome-192x192.png',
        data: {
            url: payload.url || '/dashboard',
            cardId: payload.cardId || null,
            eventType: payload.eventType || null
        }
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const url = event.notification.data?.url || '/dashboard';

    event.waitUntil((async () => {
        const allClients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        const targetUrl = new URL(url, self.location.origin).href;

        for (const client of allClients) {
            if ('focus' in client && client.url.startsWith(self.location.origin)) {
                await client.focus();
                if ('navigate' in client) await client.navigate(targetUrl);
                return;
            }
        }

        if (self.clients.openWindow) await self.clients.openWindow(targetUrl);
    })());
});
