// COMPLETED: Toast notification component
// COMPLETED: Success, error, warning, info variants
// COMPLETED: Auto-dismiss with configurable duration
// COMPLETED: Custom icons and colors per variant
// COMPLETED: Smooth animations (slide in/out)
// TODO: Add progress bar for duration
// TODO: Add action buttons (undo, retry, etc.)
// TODO: Add stacking for multiple toasts
import React, { useEffect, useState } from 'react';
import { CheckCircle, XCircle, AlertCircle, Info, X } from 'lucide-react';

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface ToastProps {
  id: string;
  message: string;
  variant?: ToastVariant;
  duration?: number;
  onClose: (id: string) => void;
}

export const Toast: React.FC<ToastProps> = ({
  id,
  message,
  variant = 'info',
  duration = 5000,
  onClose,
}) => {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // Trigger animation on mount
    setTimeout(() => setIsVisible(true), 10);

    // Auto dismiss
    if (duration > 0) {
      const timer = setTimeout(() => {
        handleClose();
      }, duration);
      return () => clearTimeout(timer);
    }
  }, [duration]);

  const handleClose = () => {
    setIsVisible(false);
    setTimeout(() => onClose(id), 300); // Wait for animation
  };

  const variants = {
    success: {
      icon: CheckCircle,
      bgColor: 'bg-green-50',
      borderColor: 'border-green-500',
      textColor: 'text-green-800',
      iconColor: 'text-green-600',
    },
    error: {
      icon: XCircle,
      bgColor: 'bg-red-50',
      borderColor: 'border-red-500',
      textColor: 'text-red-800',
      iconColor: 'text-red-600',
    },
    warning: {
      icon: AlertCircle,
      bgColor: 'bg-yellow-50',
      borderColor: 'border-yellow-500',
      textColor: 'text-yellow-800',
      iconColor: 'text-yellow-600',
    },
    info: {
      icon: Info,
      bgColor: 'bg-blue-50',
      borderColor: 'border-blue-500',
      textColor: 'text-blue-800',
      iconColor: 'text-blue-600',
    },
  };

  const config = variants[variant];
  const Icon = config.icon;

  return (
    <div
      className={`flex items-center gap-3 p-4 rounded-lg border-l-4 shadow-lg transition-all duration-300 transform ${
        isVisible ? 'translate-x-0 opacity-100' : 'translate-x-full opacity-0'
      } ${config.bgColor} ${config.borderColor}`}
      style={{ minWidth: '320px', maxWidth: '500px' }}
    >
      <Icon className={`flex-shrink-0 ${config.iconColor}`} size={24} />
      <p className={`flex-1 text-sm font-medium ${config.textColor}`}>{message}</p>
      <button
        onClick={handleClose}
        className={`flex-shrink-0 ${config.textColor} hover:opacity-70 transition-opacity`}
      >
        <X size={18} />
      </button>
    </div>
  );
};
