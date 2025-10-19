// COMPLETED: Password strength indicator component
// COMPLETED: Visual strength meter with colors
// COMPLETED: Strength calculation (weak, medium, strong, very strong)
// COMPLETED: Requirements checklist
// TODO: Add custom strength rules
// TODO: Add password suggestions
import React from 'react';

interface PasswordStrengthProps {
  password: string;
  showRequirements?: boolean;
}

export const PasswordStrength: React.FC<PasswordStrengthProps> = ({
  password,
  showRequirements = true,
}) => {
  const calculateStrength = (pwd: string): {
    score: number;
    label: string;
    color: string;
  } => {
    let score = 0;

    if (!pwd) return { score: 0, label: '', color: '' };

    // Length check
    if (pwd.length >= 8) score++;
    if (pwd.length >= 12) score++;

    // Has lowercase
    if (/[a-z]/.test(pwd)) score++;

    // Has uppercase
    if (/[A-Z]/.test(pwd)) score++;

    // Has numbers
    if (/[0-9]/.test(pwd)) score++;

    // Has special characters
    if (/[^A-Za-z0-9]/.test(pwd)) score++;

    // Map score to strength
    if (score <= 2) return { score: 1, label: 'Weak', color: 'bg-red-500' };
    if (score <= 4) return { score: 2, label: 'Medium', color: 'bg-yellow-500' };
    if (score <= 5) return { score: 3, label: 'Strong', color: 'bg-green-500' };
    return { score: 4, label: 'Very Strong', color: 'bg-green-600' };
  };

  const strength = calculateStrength(password);

  const requirements = [
    { label: 'At least 8 characters', met: password.length >= 8 },
    { label: 'Contains uppercase letter', met: /[A-Z]/.test(password) },
    { label: 'Contains lowercase letter', met: /[a-z]/.test(password) },
    { label: 'Contains number', met: /[0-9]/.test(password) },
    { label: 'Contains special character', met: /[^A-Za-z0-9]/.test(password) },
  ];

  if (!password) return null;

  return (
    <div className="mt-2">
      {/* Strength Meter */}
      <div className="mb-2">
        <div className="flex items-center justify-between mb-1">
          <span className="text-xs text-neutral">Password Strength:</span>
          {strength.label && (
            <span className={`text-xs font-semibold ${strength.label === 'Weak' ? 'text-red-600' : strength.label === 'Medium' ? 'text-yellow-600' : 'text-green-600'
              }`}>
              {strength.label}
            </span>
          )}
        </div>
        <div className="flex gap-1">
          {[1, 2, 3, 4].map((level) => (
            <div
              key={level}
              className={`h-1 flex-1 rounded-full transition-colors ${level <= strength.score ? strength.color : 'bg-gray-200'
                }`}
            />
          ))}
        </div>
      </div>

      {/* Requirements Checklist */}
      {showRequirements && (
        <div className="space-y-1">
          {requirements.map((req, index) => (
            <div key={index} className="flex items-center gap-2">
              <div
                className={`w-4 h-4 rounded-full flex items-center justify-center ${req.met ? 'bg-green-500' : 'bg-gray-200'
                  }`}
              >
                {req.met && (
                  <svg
                    className="w-3 h-3 text-white"
                    fill="none"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path d="M5 13l4 4L19 7"></path>
                  </svg>
                )}
              </div>
              <span className={`text-xs ${req.met ? 'text-green-600' : 'text-neutral'}`}>
                {req.label}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
