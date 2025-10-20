using AlgoTrendy.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace AlgoTrendy.Core.Services;

/// <summary>
/// High-level service for managing multi-factor authentication
/// Handles enrollment, verification, backup codes, and MFA settings
/// </summary>
public class MfaService
{
    private readonly TotpService _totpService;
    private const int BackupCodeCount = 10;
    private const int BackupCodeLength = 8;
    private const int MfaLockoutThreshold = 5;
    private const int MfaLockoutMinutes = 15;

    public MfaService(TotpService totpService)
    {
        _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
    }

    /// <summary>
    /// Initiate MFA enrollment for a user
    /// Returns secret and QR code for setting up authenticator app
    /// </summary>
    public MfaEnrollmentResult InitiateEnrollment(string userEmail)
    {
        var secret = _totpService.GenerateSecret();
        var qrCodeUri = _totpService.GenerateQrCodeUri(userEmail, secret);
        var qrCodeBase64 = _totpService.GenerateQrCodeBase64(userEmail, secret);

        return new MfaEnrollmentResult
        {
            Secret = secret,
            QrCodeUri = qrCodeUri,
            QrCodeBase64 = qrCodeBase64
        };
    }

    /// <summary>
    /// Complete MFA enrollment by verifying the first TOTP code
    /// Creates MfaSettings and generates backup codes
    /// </summary>
    public MfaEnrollmentCompleteResult CompleteEnrollment(
        Guid userId,
        string secret,
        string verificationCode,
        string? deviceName = null)
    {
        // Verify the TOTP code before enabling MFA
        if (!_totpService.VerifyCode(secret, verificationCode))
        {
            throw new InvalidOperationException("Invalid verification code. Please ensure your device time is synchronized.");
        }

        // Encrypt the secret (in production, use proper encryption service)
        var encryptedSecret = EncryptSecret(secret);

        // Generate backup codes
        var backupCodes = GenerateBackupCodes(BackupCodeCount);
        var hashedBackupCodes = backupCodes.Select(code => new MfaBackupCode
        {
            BackupCodeId = Guid.NewGuid(),
            CodeHash = HashBackupCode(code),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90) // Backup codes expire in 90 days
        }).ToList();

        // Create MFA settings
        var mfaSettings = new UserMfaSettings
        {
            MfaSettingsId = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            TotpSecretEncrypted = encryptedSecret,
            EnabledAt = DateTime.UtcNow,
            BackupCodesRemaining = backupCodes.Count,
            BackupCodes = hashedBackupCodes,
            MfaMethod = "TOTP",
            DeviceName = deviceName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return new MfaEnrollmentCompleteResult
        {
            MfaSettings = mfaSettings,
            BackupCodes = backupCodes, // Return plaintext backup codes ONCE for user to save
            Success = true,
            Message = "MFA enabled successfully. Please save your backup codes in a secure location."
        };
    }

    /// <summary>
    /// Verify MFA code (either TOTP or backup code)
    /// Updates MFA settings on success/failure
    /// </summary>
    public MfaVerificationResult VerifyMfaCode(
        UserMfaSettings mfaSettings,
        string code,
        string? ipAddress = null)
    {
        if (!mfaSettings.IsEnabled)
        {
            return new MfaVerificationResult
            {
                Success = false,
                Message = "MFA is not enabled for this account."
            };
        }

        // Check if account is locked
        if (mfaSettings.IsLocked())
        {
            return new MfaVerificationResult
            {
                Success = false,
                IsLocked = true,
                Message = $"Account temporarily locked due to multiple failed attempts. Try again after {mfaSettings.LockedUntil?.ToString("yyyy-MM-dd HH:mm:ss UTC")}."
            };
        }

        // Try TOTP verification first
        var decryptedSecret = DecryptSecret(mfaSettings.TotpSecretEncrypted);
        if (_totpService.VerifyCode(decryptedSecret, code))
        {
            mfaSettings.ResetFailedAttempts();
            mfaSettings.LastUsedAt = DateTime.UtcNow;
            mfaSettings.UpdatedAt = DateTime.UtcNow;

            return new MfaVerificationResult
            {
                Success = true,
                UsedBackupCode = false,
                Message = "MFA verification successful."
            };
        }

        // Try backup code verification
        var backupCodeResult = VerifyBackupCode(mfaSettings, code, ipAddress);
        if (backupCodeResult.Success)
        {
            mfaSettings.ResetFailedAttempts();
            mfaSettings.LastUsedAt = DateTime.UtcNow;
            mfaSettings.UpdatedAt = DateTime.UtcNow;
            return backupCodeResult;
        }

        // Both verifications failed - increment failed attempts
        mfaSettings.IncrementFailedAttempts(MfaLockoutThreshold, MfaLockoutMinutes);

        return new MfaVerificationResult
        {
            Success = false,
            Message = $"Invalid MFA code. {MfaLockoutThreshold - mfaSettings.FailedAttempts} attempts remaining before lockout."
        };
    }

    /// <summary>
    /// Verify backup code
    /// </summary>
    private MfaVerificationResult VerifyBackupCode(
        UserMfaSettings mfaSettings,
        string code,
        string? ipAddress = null)
    {
        foreach (var backupCode in mfaSettings.BackupCodes.Where(bc => bc.IsValid()))
        {
            if (VerifyBackupCodeHash(code, backupCode.CodeHash))
            {
                backupCode.MarkAsUsed(ipAddress);
                mfaSettings.BackupCodesRemaining--;

                return new MfaVerificationResult
                {
                    Success = true,
                    UsedBackupCode = true,
                    BackupCodesRemaining = mfaSettings.BackupCodesRemaining,
                    Message = $"Backup code accepted. You have {mfaSettings.BackupCodesRemaining} backup codes remaining."
                };
            }
        }

        return new MfaVerificationResult
        {
            Success = false,
            Message = "Invalid backup code."
        };
    }

    /// <summary>
    /// Generate new backup codes (invalidates old ones)
    /// </summary>
    public List<string> RegenerateBackupCodes(UserMfaSettings mfaSettings)
    {
        // Clear existing backup codes
        mfaSettings.BackupCodes.Clear();

        // Generate new backup codes
        var newBackupCodes = GenerateBackupCodes(BackupCodeCount);
        var hashedBackupCodes = newBackupCodes.Select(code => new MfaBackupCode
        {
            BackupCodeId = Guid.NewGuid(),
            MfaSettingsId = mfaSettings.MfaSettingsId,
            CodeHash = HashBackupCode(code),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90)
        }).ToList();

        mfaSettings.BackupCodes = hashedBackupCodes;
        mfaSettings.BackupCodesRemaining = newBackupCodes.Count;
        mfaSettings.UpdatedAt = DateTime.UtcNow;

        return newBackupCodes;
    }

    /// <summary>
    /// Disable MFA for a user
    /// Requires verification code before disabling
    /// </summary>
    public bool DisableMfa(UserMfaSettings mfaSettings, string verificationCode)
    {
        var decryptedSecret = DecryptSecret(mfaSettings.TotpSecretEncrypted);
        if (!_totpService.VerifyCode(decryptedSecret, verificationCode))
        {
            return false;
        }

        mfaSettings.IsEnabled = false;
        mfaSettings.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Generate random backup codes
    /// Format: XXXX-XXXX (8 characters with hyphen)
    /// </summary>
    private List<string> GenerateBackupCodes(int count)
    {
        var backupCodes = new List<string>();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        using (var rng = RandomNumberGenerator.Create())
        {
            for (int i = 0; i < count; i++)
            {
                var code = new char[BackupCodeLength];
                var buffer = new byte[BackupCodeLength];
                rng.GetBytes(buffer);

                for (int j = 0; j < BackupCodeLength; j++)
                {
                    code[j] = chars[buffer[j] % chars.Length];
                }

                // Format: XXXX-XXXX
                var formattedCode = $"{new string(code, 0, 4)}-{new string(code, 4, 4)}";
                backupCodes.Add(formattedCode);
            }
        }

        return backupCodes;
    }

    /// <summary>
    /// Hash backup code using SHA256
    /// In production, use bcrypt or similar
    /// </summary>
    private string HashBackupCode(string code)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Verify backup code against hash
    /// </summary>
    private bool VerifyBackupCodeHash(string code, string hash)
    {
        var computedHash = HashBackupCode(code);
        return computedHash == hash;
    }

    /// <summary>
    /// Encrypt TOTP secret
    /// TODO: Replace with proper encryption service (Azure Key Vault, etc.)
    /// </summary>
    private string EncryptSecret(string secret)
    {
        // TEMPORARY: Base64 encoding (NOT SECURE!)
        // In production, use AES encryption with key from Azure Key Vault
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
    }

    /// <summary>
    /// Decrypt TOTP secret
    /// TODO: Replace with proper encryption service
    /// </summary>
    private string DecryptSecret(string encryptedSecret)
    {
        // TEMPORARY: Base64 decoding (NOT SECURE!)
        // In production, use AES decryption with key from Azure Key Vault
        return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedSecret));
    }
}

/// <summary>
/// Result of MFA enrollment initiation
/// </summary>
public class MfaEnrollmentResult
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
}

/// <summary>
/// Result of completing MFA enrollment
/// </summary>
public class MfaEnrollmentCompleteResult
{
    public UserMfaSettings? MfaSettings { get; set; }
    public List<string> BackupCodes { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of MFA code verification
/// </summary>
public class MfaVerificationResult
{
    public bool Success { get; set; }
    public bool UsedBackupCode { get; set; }
    public int? BackupCodesRemaining { get; set; }
    public bool IsLocked { get; set; }
    public string Message { get; set; } = string.Empty;
}
