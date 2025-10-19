// COMPLETED: Toast container with positioning
// COMPLETED: Fixed position at top-right
// COMPLETED: Stack multiple toasts vertically
// TODO: Add position variants (top-left, bottom-right, etc.)
// TODO: Add max toast limit
import React from 'react';
import { Toast, ToastProps } from './Toast';

interface ToastContainerProps {
  toasts: ToastProps[];
  onClose: (id: string) => void;
}

export const ToastContainer: React.FC<ToastContainerProps> = ({ toasts, onClose }) => {
  return (
    <div className="fixed top-4 right-4 z-50 space-y-2">
      {toasts.map((toast) => (
        <Toast key={toast.id} {...toast} onClose={onClose} />
      ))}
    </div>
  );
};
