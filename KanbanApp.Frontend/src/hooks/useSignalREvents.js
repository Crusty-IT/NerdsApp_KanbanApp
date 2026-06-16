import { useEffect, useRef } from 'react';

export function useSignalREvents({ connection, eventName, handler }) {
    const handlerRef = useRef(handler);

    useEffect(() => {
        handlerRef.current = handler;
    });

    useEffect(() => {
        if (!connection || !eventName) return;

        const stableHandler = (...args) => handlerRef.current(...args);
        connection.on(eventName, stableHandler);
        return () => connection.off(eventName, stableHandler);
    }, [connection, eventName]);
}
