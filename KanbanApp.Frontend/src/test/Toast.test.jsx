import { render, screen, act, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import Toast from '../components/Toast';

describe('Toast', () => {
    beforeEach(() => {
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('renders the message', () => {
        render(<Toast message="Operation succeeded" onClose={vi.fn()} />);
        expect(screen.getByText('Operation succeeded')).toBeInTheDocument();
    });

    it('has role=alert for screen readers', () => {
        render(<Toast message="Test" onClose={vi.fn()} />);
        expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    it('calls onClose when dismiss button is clicked', () => {
        const onClose = vi.fn();
        render(<Toast message="Dismiss me" onClose={onClose} />);
        fireEvent.click(screen.getByRole('button', { name: /dismiss/i }));
        expect(onClose).toHaveBeenCalledOnce();
    });

    it('calls onClose after duration', () => {
        const onClose = vi.fn();
        render(<Toast message="Auto close" duration={1000} onClose={onClose} />);
        act(() => { vi.advanceTimersByTime(1000); });
        expect(onClose).toHaveBeenCalledOnce();
    });
});
