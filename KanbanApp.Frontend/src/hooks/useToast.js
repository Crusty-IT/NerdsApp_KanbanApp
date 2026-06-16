import { useTopbar } from '../context/TopbarContext';

export function useToast() {
    const { toasts, addToast, removeToast } = useTopbar();
    return { toasts, addToast, removeToast };
}
