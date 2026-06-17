import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TopbarProvider } from '../context/TopbarContext';
import { useCards } from '../hooks/useCards';
import { useSignalREvents } from '../hooks/useSignalREvents';

const wrapper = ({ children }) => <TopbarProvider>{children}</TopbarProvider>;

function createBoard() {
    return {
        id: 1,
        columns: [
            {
                id: 10,
                name: 'Todo',
                position: 0,
                cards: [
                    { id: 1, title: 'First', columnId: 10, position: 0 },
                    { id: 2, title: 'Second', columnId: 10, position: 1 },
                ],
            },
            {
                id: 20,
                name: 'Done',
                position: 1,
                cards: [],
            },
        ],
    };
}

function renderUseCards(initialBoard) {
    const handlers = new Map();
    useSignalREvents.mockImplementation(({ eventName, handler }) => {
        handlers.set(eventName, handler);
    });

    let board = initialBoard;
    const setBoard = vi.fn(updater => {
        board = typeof updater === 'function' ? updater(board) : updater;
    });

    renderHook(
        () => useCards(1, board, setBoard, vi.fn(), { connectionId: 'test' }),
        { wrapper }
    );

    return {
        handlers,
        getBoard: () => board,
    };
}

describe('useCards SignalR handlers', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('reorders a moved card within the same column', () => {
        const { handlers, getBoard } = renderUseCards(createBoard());

        act(() => {
            handlers.get('CardMoved')({
                cardId: 1,
                fromColumnId: 10,
                toColumnId: 10,
                newPosition: 1,
                movedByUserId: 'other-user',
            });
        });

        const cards = getBoard().columns[0].cards;
        expect(cards.map(card => card.id)).toEqual([2, 1]);
        expect(cards.map(card => card.position)).toEqual([0, 1]);
        expect(cards[1].columnId).toBe(10);
    });

    it('moves a card to the column from CardUpdated when CardMoved was missed', () => {
        const { handlers, getBoard } = renderUseCards(createBoard());

        act(() => {
            handlers.get('CardUpdated')({
                card: {
                    id: 1,
                    title: 'First updated',
                    columnId: 20,
                    position: 0,
                },
            });
        });

        expect(getBoard().columns[0].cards.map(card => card.id)).toEqual([2]);
        expect(getBoard().columns[1].cards.map(card => card.id)).toEqual([1]);
        expect(getBoard().columns[1].cards[0].title).toBe('First updated');
    });
});
