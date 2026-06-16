import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import RegisterPage from '../views/RegisterPage';

vi.mock('../components/PrivacyPolicyModal', () => ({ default: () => null }));

function renderPage() {
    render(
        <MemoryRouter>
            <RegisterPage />
        </MemoryRouter>
    );
}

describe('RegisterPage', () => {
    it('submit button is disabled when password is invalid', () => {
        renderPage();
        expect(screen.getByRole('button', { name: /create account/i })).toBeDisabled();
    });

    it('shows password validation rules when typing', async () => {
        const user = userEvent.setup();
        renderPage();
        await user.type(screen.getByLabelText(/password/i), 'short');
        expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument();
    });

    it('enables submit when all password rules pass', async () => {
        const user = userEvent.setup();
        renderPage();
        await user.type(screen.getByLabelText(/password/i), 'ValidPass1!');
        expect(screen.getByRole('button', { name: /create account/i })).not.toBeDisabled();
    });

    it('shows green checkmark for passing rule', async () => {
        const user = userEvent.setup();
        renderPage();
        await user.type(screen.getByLabelText(/password/i), 'ValidPass1!');
        const checks = screen.getAllByText('✓');
        expect(checks.length).toBeGreaterThan(0);
    });
});
