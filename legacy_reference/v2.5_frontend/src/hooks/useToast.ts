// COMPLETED: useToast hook for managing toasts
// COMPLETED: Add, remove toast functions
// COMPLETED: Auto-generated IDs
// COMPLETED: Convenient success/error/warning/info helpers
// TODO: Add persistent toasts (survive navigation)
// TODO: Add toast queue management
import { useState, useCallback } from 'react';
import { ToastProps, ToastVariant } from '@/components/ui/Toast';

let toastId = 0;

export const useToast = () => {
  const [toasts, setToasts] = useState<ToastProps[]>([]);

  const addToast = useCallback((
    message: string,
    variant: ToastVariant = 'info',
    duration = 5000
  ) => {
    const id = `toast-${++toastId}`;
    const newToast: ToastProps = {
      id,
      message,
      variant,
      duration,
      onClose: removeToast,
    };

    setToasts((prev) => [...prev, newToast]);
    return id;
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const success = useCallback((message: string, duration?: number) => {
    return addToast(message, 'success', duration);
  }, [addToast]);

  const error = useCallback((message: string, duration?: number) => {
    return addToast(message, 'error', duration);
  }, [addToast]);

  const warning = useCallback((message: string, duration?: number) => {
    return addToast(message, 'warning', duration);
  }, [addToast]);

  const info = useCallback((message: string, duration?: number) => {
    return addToast(message, 'info', duration);
  }, [addToast]);

  return {
    toasts,
    addToast,
    removeToast,
    success,
    error,
    warning,
    info,
  };
};
