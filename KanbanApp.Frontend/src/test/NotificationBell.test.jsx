import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import NotificationBell from '../components/NotificationBell';
import { TopbarProvider } from '../context/TopbarContext';

function renderBell() {
    return render(
        <TopbarProvider>
            <NotificationBell />
        </TopbarProvider>
    );
}

describe('NotificationBell', () => {
    it('renders the bell button', () => {
        renderBell();
        expect(screen.getByRole('button')).toBeInTheDocument();
    });

    it('shows empty state when notifications dropdown is opened with no notifications', async () => {
        renderBell();
        fireEvent.click(screen.getByRole('button'));
        expect(await screen.findByText(/no notifications/i)).toBeInTheDocument();
    });

    it('shows "Notifications" heading when dropdown is open', () => {
        renderBell();
        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByText('Notifications')).toBeInTheDocument();
    });

    it('dropdown is hidden initially', () => {
        renderBell();
        expect(screen.queryByText('Notifications')).not.toBeInTheDocument();
    });
});
