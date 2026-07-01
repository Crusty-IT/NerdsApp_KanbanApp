import { useEffect, useState } from 'react';
import { BASE_URL } from '../services/api';

const NOTICE_DELAY_MS = 700;
const WAKE_ENDPOINT = '/health';
const MAX_ATTEMPTS = 8;
const INITIAL_RETRY_DELAY_MS = 1500;
const MAX_RETRY_DELAY_MS = 12000;
const SESSION_KEY = 'backendWakeConfirmed';

export default function BackendWakeNotice() {
    const [visible, setVisible] = useState(() => sessionStorage.getItem(SESSION_KEY) !== 'true');

    useEffect(() => {
        if (sessionStorage.getItem(SESSION_KEY) === 'true') {
            setVisible(false);
            return;
        }

        let isMounted = true;
        let retryId;
        const noticeDelayId = window.setTimeout(() => {
            if (isMounted) setVisible(true);
        }, NOTICE_DELAY_MS);

        const pingBackend = async (attempt = 0) => {
            const controller = new AbortController();

            try {
                const response = await fetch(`${BASE_URL}${WAKE_ENDPOINT}`, {
                    method: 'GET',
                    cache: 'no-store',
                    signal: controller.signal
                });

                if (response.ok) {
                    sessionStorage.setItem(SESSION_KEY, 'true');
                    window.clearTimeout(noticeDelayId);
                    if (isMounted) setVisible(false);
                    return;
                }
            } catch {
                // Cold starts, transient network failures, or extensions can block the probe.
            } finally {
                controller.abort();
            }

            if (attempt >= MAX_ATTEMPTS || !isMounted) {
                window.clearTimeout(noticeDelayId);
                if (isMounted) setVisible(false);
                return;
            }

            const retryDelay = Math.min(
                INITIAL_RETRY_DELAY_MS * 2 ** attempt,
                MAX_RETRY_DELAY_MS
            );

            if (isMounted) {
                retryId = window.setTimeout(() => pingBackend(attempt + 1), retryDelay);
            }
        };

        pingBackend();

        return () => {
            isMounted = false;
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
