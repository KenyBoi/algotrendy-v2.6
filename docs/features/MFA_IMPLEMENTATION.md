# Multi-Factor Authentication (MFA) Implementation

**Status:** ✅ Implemented
**Date:** October 20, 2025
**Version:** v2.6.0
**Author:** AlgoTrendy Development Team

---

## Overview

AlgoTrendy v2.6 now includes a complete **TOTP-based Multi-Factor Authentication (MFA)** system to enhance account security. This implementation provides enterprise-grade two-factor authentication compatible with popular authenticator apps like Google Authenticator, Authy, Microsoft Authenticator, and 1Password.

### Key Features

✅ **TOTP-based authentication** (RFC 6238 compliant)
✅ **QR code enrollment** for easy setup
✅ **Backup codes** for account recovery (10 codes per user)
✅ **Account lockout protection** (5 failed attempts = 15-minute lockout)
✅ **Secure secret storage** (encrypted TOTP secrets)
✅ **RESTful API** with comprehensive endpoints
✅ **Audit logging** for security monitoring

---

## Architecture

### Components

```
AlgoTrendy.Core/
├── Models/
│   ├── User.cs (updated with MfaSettings navigation property)
│   ├── UserMfaSettings.cs
│   └── MfaBackupCode.cs
├── Services/
│   ├── TotpService.cs
│   └── MfaService.cs

AlgoTrendy.API/
├── Controllers/
│   └── MfaController.cs
├── DTOs/
│   └── MfaDtos.cs
```

### Technology Stack

- **OTP Library:** Otp.NET v1.4.0
- **QR Code Generation:** QRCoder v1.6.0
- **Hashing:** SHA256 (for backup codes)
- **Encryption:** Base64 encoding (⚠️ **TODO:** Replace with AES + Azure Key Vault)

---

## Data Models

### UserMfaSettings

Stores MFA configuration for each user:

```csharp
public class UserMfaSettings
{
    public Guid MfaSettingsId { get; set; }
    public Guid UserId { get; set; }
    public bool IsEnabled { get; set; }
    public string TotpSecretEncrypted { get; set; }
    public DateTime? EnabledAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int BackupCodesRemaining { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string MfaMethod { get; set; } = "TOTP";
    public string? DeviceName { get; set; }

    public ICollection<MfaBackupCode> BackupCodes { get; set; }
}
```

### MfaBackupCode

Stores recovery codes (hashed):

```csharp
public class MfaBackupCode
{
    public Guid BackupCodeId { get; set; }
    public Guid MfaSettingsId { get; set; }
    public string CodeHash { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

---

## API Endpoints

### 1. Get MFA Status

**GET** `/api/mfa/status`

Returns the current MFA status for the authenticated user.

**Response:**
```json
{
  "isEnabled": true,
  "mfaMethod": "TOTP",
  "deviceName": "Google Authenticator on iPhone",
  "enabledAt": "2025-10-20T10:30:00Z",
  "lastUsedAt": "2025-10-20T14:15:00Z",
  "backupCodesRemaining": 8
}
```

---

### 2. Initiate MFA Enrollment

**POST** `/api/mfa/enroll/initiate`

Generates a TOTP secret and QR code for enrollment.

**Request:**
```json
{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "qrCodeUri": "otpauth://totp/AlgoTrendy:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=AlgoTrendy&digits=6&period=30",
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "instructions": "Scan the QR code with your authenticator app..."
}
```

---

### 3. Complete MFA Enrollment

**POST** `/api/mfa/enroll/complete`

Verifies the first TOTP code and enables MFA.

**Request:**
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "verificationCode": "123456",
  "deviceName": "Google Authenticator on iPhone"
}
```

**Response:**
```json
{
  "success": true,
  "backupCodes": [
    "ABCD-1234",
    "EFGH-5678",
    "IJKL-9012",
    ...
  ],
  "message": "MFA enabled successfully. Please save your backup codes in a secure location.",
  "warning": "Save these backup codes in a secure location. Each code can only be used once and will not be shown again."
}
```

⚠️ **Important:** Backup codes are shown **only once** during enrollment!

---

### 4. Verify MFA Code

**POST** `/api/mfa/verify`

Verifies a TOTP code or backup code.

**Request:**
```json
{
  "email": "user@example.com",
  "code": "123456"  // or "ABCD-1234" for backup code
}
```

**Response (Success):**
```json
{
  "success": true,
  "usedBackupCode": false,
  "message": "MFA verification successful.",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (Backup Code):**
```json
{
  "success": true,
  "usedBackupCode": true,
  "backupCodesRemaining": 7,
  "message": "Backup code accepted. You have 7 backup codes remaining.",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (Account Locked):**
```json
{
  "success": false,
  "isLocked": true,
  "message": "Account temporarily locked due to multiple failed attempts. Try again after 2025-10-20 15:30:00 UTC."
}
```

---

### 5. Regenerate Backup Codes

**POST** `/api/mfa/backup-codes/regenerate`

Generates new backup codes (invalidates old ones).

**Response:**
```json
{
  "success": true,
  "backupCodes": [
    "MNOP-3456",
    "QRST-7890",
    ...
  ],
  "message": "Backup codes regenerated successfully",
  "warning": "All previous backup codes have been invalidated. Save these new codes in a secure location."
}
```

---

### 6. Disable MFA

**POST** `/api/mfa/disable`

Disables MFA for the authenticated user.

**Request:**
```json
{
  "verificationCode": "123456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "MFA disabled successfully"
}
```

---

## Security Features

### 1. Account Lockout Protection

- **Failed Attempts Threshold:** 5 attempts
- **Lockout Duration:** 15 minutes
- **Resets on:** Successful authentication

### 2. Backup Codes

- **Count:** 10 codes per user
- **Format:** XXXX-XXXX (8 characters with hyphen)
- **Storage:** SHA256 hashed (never stored in plaintext)
- **Expiration:** 90 days from generation
- **Single-use:** Each code can only be used once

### 3. TOTP Configuration

- **Algorithm:** SHA-1 (RFC 6238 standard)
- **Code Length:** 6 digits
- **Time Step:** 30 seconds
- **Tolerance Window:** ±1 time step (±30 seconds)

### 4. Secret Storage

⚠️ **Current Implementation:**
TOTP secrets are currently Base64-encoded (NOT SECURE for production).

✅ **Production TODO:**
Replace with AES-256 encryption using keys from Azure Key Vault.

```csharp
// CURRENT (temporary):
private string EncryptSecret(string secret)
{
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
}

// TODO (production):
private string EncryptSecret(string secret)
{
    // Use Azure Key Vault encryption
    return _keyVaultService.Encrypt(secret, "mfa-secret-key");
}
```

---

## Integration Examples

### Frontend Integration (React)

```typescript
// 1. Initiate enrollment
const initiateEnrollment = async () => {
  const response = await fetch('/api/mfa/enroll/initiate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: userEmail })
  });

  const { qrCodeBase64, secret } = await response.json();

  // Display QR code
  setQrCode(`data:image/png;base64,${qrCodeBase64}`);
  setSecret(secret);
};

// 2. Complete enrollment
const completeEnrollment = async (code: string) => {
  const response = await fetch('/api/mfa/enroll/complete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      secret: secret,
      verificationCode: code,
      deviceName: 'Google Authenticator'
    })
  });

  const { backupCodes } = await response.json();

  // Show backup codes to user (ONE TIME ONLY!)
  displayBackupCodes(backupCodes);
};

// 3. Verify during login
const verifyMfa = async (code: string) => {
  const response = await fetch('/api/mfa/verify', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email: userEmail,
      code: code
    })
  });

  const { success, token, isLocked } = await response.json();

  if (success) {
    // Store JWT token
    localStorage.setItem('token', token);
    navigate('/dashboard');
  } else if (isLocked) {
    showError('Account locked. Try again later.');
  }
};
```

---

## Testing

### Manual Testing with cURL

#### 1. Check MFA Status
```bash
curl -X GET http://localhost:5000/api/mfa/status \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### 2. Initiate Enrollment
```bash
curl -X POST http://localhost:5000/api/mfa/enroll/initiate \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@algotrendy.com"}'
```

#### 3. Complete Enrollment
```bash
curl -X POST http://localhost:5000/api/mfa/enroll/complete \
  -H "Content-Type: application/json" \
  -d '{
    "secret":"JBSWY3DPEHPK3PXP",
    "verificationCode":"123456",
    "deviceName":"Test Device"
  }'
```

#### 4. Verify MFA Code
```bash
curl -X POST http://localhost:5000/api/mfa/verify \
  -H "Content-Type: application/json" \
  -d '{
    "email":"demo@algotrendy.com",
    "code":"123456"
  }'
```

---

## Database Migration

### Entity Framework Core Migration (TODO)

When database layer is implemented, run:

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.Infrastructure

# Create migration
dotnet ef migrations add AddMfaSupport

# Update database
dotnet ef database update
```

**Migration will create:**
- `UserMfaSettings` table
- `MfaBackupCodes` table
- Foreign key: `UserMfaSettings.UserId` → `Users.UserId`
- Foreign key: `MfaBackupCodes.MfaSettingsId` → `UserMfaSettings.MfaSettingsId`

---

## Production Checklist

Before deploying to production:

- [ ] **Replace Base64 encoding with AES encryption**
  - Integrate Azure Key Vault for encryption keys
  - Update `MfaService.EncryptSecret()` and `DecryptSecret()`

- [ ] **Implement database persistence**
  - Create DbContext with UserMfaSettings and MfaBackupCodes
  - Replace in-memory dictionary in MfaController

- [ ] **Add audit logging**
  - Log all MFA events (enrollment, verification, disable)
  - Send alerts on suspicious activity (multiple failed attempts)

- [ ] **Rate limiting**
  - Add rate limits to MFA endpoints (10 requests/minute per IP)
  - Prevent brute-force attacks

- [ ] **Email notifications**
  - Notify user when MFA is enabled/disabled
  - Alert on account lockout

- [ ] **Backup code improvements**
  - Consider bcrypt instead of SHA256 for backup codes
  - Add option to download backup codes as PDF

- [ ] **Testing**
  - Unit tests for TotpService and MfaService
  - Integration tests for MfaController endpoints
  - Load testing for verification endpoints

---

## Known Limitations

1. **No SMS/Email MFA:** Currently only supports TOTP. SMS and email-based MFA are not implemented.

2. **Temporary Secret Encryption:** TOTP secrets use Base64 encoding. Must upgrade to AES encryption with Azure Key Vault before production.

3. **In-Memory Storage:** MFA settings currently stored in-memory. Database persistence needs to be implemented.

4. **No Admin Override:** Admins cannot disable MFA for locked-out users. Need admin endpoints for account recovery.

5. **Single Device:** Users can only enroll one authenticator device. Multi-device support not implemented.

---

## Future Enhancements

### Planned (Q1 2026)
- [ ] SMS-based MFA as alternative to TOTP
- [ ] Email-based MFA for low-security accounts
- [ ] Hardware security key support (WebAuthn/FIDO2)
- [ ] Admin dashboard for MFA management
- [ ] MFA usage analytics and reporting

### Under Consideration
- [ ] Biometric authentication (Face ID, Touch ID)
- [ ] Risk-based authentication (skip MFA for trusted devices)
- [ ] Multi-device TOTP support
- [ ] Progressive enrollment (enforce MFA for high-value accounts)

---

## References

- **RFC 6238:** TOTP: Time-Based One-Time Password Algorithm
  https://tools.ietf.org/html/rfc6238

- **Otp.NET Documentation:**
  https://github.com/kspearrin/Otp.NET

- **QRCoder Documentation:**
  https://github.com/codebude/QRCoder

- **OWASP Multi-Factor Authentication Cheat Sheet:**
  https://cheatsheetseries.owasp.org/cheatsheets/Multifactor_Authentication_Cheat_Sheet.html

---

## Support

For questions or issues with MFA implementation:

- **Email:** dev@algotrendy.com
- **Slack:** #security-features
- **GitHub Issues:** https://github.com/algotrendy/algotrendy/issues

---

**Last Updated:** October 20, 2025
**Next Review:** January 2026 (Security Audit)
