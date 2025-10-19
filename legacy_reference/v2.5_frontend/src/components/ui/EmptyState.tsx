// COMPLETED: Reusable empty state component
// COMPLETED: Customizable icon, title, description, and action button
// COMPLETED: Responsive design with proper spacing
// TODO: Add animated illustrations
// TODO: Add dark mode support
import React from 'react';
import { LucideIcon } from 'lucide-react';

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon: Icon,
  title,
  description,
  actionLabel,
  onAction,
  className = '',
}) => {
  return (
    <div className={`flex flex-col items-center justify-center py-12 px-4 text-center ${className}`}>
      {/* Icon */}
      {Icon && (
        <div className="mb-4">
          <Icon size={64} className="text-gray-300" strokeWidth={1.5} />
        </div>
      )}

      {/* Title */}
      <h3 className="text-xl font-semibold text-gray-700 mb-2">{title}</h3>

      {/* Description */}
      {description && (
        <p className="text-neutral text-sm max-w-md mb-6">{description}</p>
      )}

      {/* Action Button */}
      {actionLabel && onAction && (
        <button
          onClick={onAction}
          className="px-6 py-2 bg-accent hover:bg-accent/90 text-white font-medium rounded-lg transition-colors"
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
};
