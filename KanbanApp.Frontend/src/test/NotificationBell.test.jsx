import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { beforeEach, describe, it, expect, vi } from 'vitest';
import NotificationBell from '../components/NotificationBell';
import { TopbarProvider } from '../context/TopbarContext';
import api from '../services/api';

function renderBell() {
    return render(
        <TopbarProvider>
            <NotificationBell />
        </TopbarProvider>
    );
}

describe('NotificationBell', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    async function waitForNotificationsFetch() {
        await waitFor(() => expect(api.get).toHaveBeenCalledWith('/api/notifications'));
    }

    it('renders the bell button', async () => {
        renderBell();
        await waitForNotificationsFetch();
        expect(screen.getByRole('button')).toBeInTheDocument();
    });

    it('shows empty state when notifications dropdown is opened with no notifications', async () => {
        renderBell();
        await waitForNotificationsFetch();
        fireEvent.click(screen.getByRole('button'));
        expect(await screen.findByText(/no notifications/i)).toBeInTheDocument();
    });

    it('shows "Notifications" heading when dropdown is open', async () => {
        renderBell();
        await waitForNotificationsFetch();
        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByText('Notifications')).toBeInTheDocument();
    });

    it('dropdown is hidden initially', async () => {
        renderBell();
        await waitForNotificationsFetch();
        expect(screen.queryByText('Notifications')).not.toBeInTheDocument();
    });
});
