import { useEffect, useState } from 'react';
import { BASE_URL } from '../services/api';

const PING_INTERVAL_MS = 3500;
const NOTICE_DELAY_MS = 700;

export default function BackendWakeNotice() {
    const [visible, setVisible] = useState(false);

    useEffect(() => {
        let isMounted = true;
        let retryId;
        const controller = new AbortController();
        const noticeDelayId = window.setTimeout(() => {
            if (isMounted) setVisible(true);
        }, NOTICE_DELAY_MS);

        const pingBackend = async () => {
            try {
                const response = await fetch(`${BASE_URL}/health`, {
                    method: 'GET',
                    cache: 'no-store',
                    signal: controller.signal
                });

                if (response.ok) {
                    window.clearTimeout(noticeDelayId);
                    if (isMounted) setVisible(false);
                    return;
                }
            } catch {
                // Render cold starts and CORS/preflight failures surface here until the API is reachable.
            }

            if (isMounted) {
                retryId = window.setTimeout(pingBackend, PING_INTERVAL_MS);
            }
        };

        pingBackend();

        return () => {
            isMounted = false;
            controller.abort();
            window.clearTimeout(noticeDelayId);
            window.clearTimeout(retryId);
        };
    }, []);

    if (!visible) return null;

    return (
        <div className="backend-wake-notice" role="status" aria-live="polite">
            <span className="backend-wake-notice__dot" aria-hidden="true" />
            Connecting to backend...
        </div>
    );
}
