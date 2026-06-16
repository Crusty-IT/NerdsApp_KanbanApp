import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '../views/LoginPage';

vi.mock('../components/PrivacyPolicyModal', () => ({ default: () => null }));

function renderPage() {
    render(
        <MemoryRouter>
            <LoginPage />
        </MemoryRouter>
    );
}

describe('LoginPage', () => {
    it('renders email and password fields', () => {
        renderPage();
        expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    });

    it('submit button is enabled by default', () => {
        renderPage();
        expect(screen.getByRole('button', { name: /sign in/i })).not.toBeDisabled();
    });

    it('shows error message on failed login', async () => {
        const { default: api } = await import('../services/api');
        api.post.mockRejectedValueOnce({ response: { data: { message: 'Invalid credentials' } } });

        const user = userEvent.setup();
        renderPage();
        await user.type(screen.getByLabelText(/email/i), 'wrong@test.com');
        await user.type(screen.getByLabelText(/password/i), 'wrongpass');
        await user.click(screen.getByRole('button', { name: /sign in/i }));

        expect(await screen.findByText(/invalid credentials/i)).toBeInTheDocument();
    });
});
