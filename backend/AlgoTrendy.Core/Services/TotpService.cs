using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace AlgoTrendy.Core.Services;

/// <summary>
/// Service for generating and verifying Time-based One-Time Passwords (TOTP)
/// Implements RFC 6238 standard for two-factor authentication
/// </summary>
public class TotpService
{
    private readonly string _issuer;
    private readonly int _totpSize;
    private readonly int _timeStepSeconds;
    private readonly int _toleranceSteps;

    /// <summary>
    /// Initialize TOTP service with configuration
    /// </summary>
    /// <param name="issuer">Issuer name (e.g., "AlgoTrendy")</param>
    /// <param name="totpSize">Length of TOTP code (default 6)</param>
    /// <param name="timeStepSeconds">Time step in seconds (default 30)</param>
    /// <param name="toleranceSteps">Number of time steps to allow for clock drift (default 1)</param>
    public TotpService(
        string issuer = "AlgoTrendy",
        int totpSize = 6,
        int timeStepSeconds = 30,
        int toleranceSteps = 1)
    {
        _issuer = issuer;
        _totpSize = totpSize;
        _timeStepSeconds = timeStepSeconds;
        _toleranceSteps = toleranceSteps;
    }

    /// <summary>
    /// Generate a new random secret key for TOTP
    /// Returns base32-encoded secret (compatible with Google Authenticator)
    /// </summary>
    public string GenerateSecret()
    {
        // Generate 20 random bytes (160 bits) for the secret
        var secretBytes = new byte[20];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secretBytes);
        }

        // Encode as base32 (standard for TOTP)
        return Base32Encoding.ToString(secretBytes);
    }

    /// <summary>
    /// Generate TOTP code from secret
    /// Used for testing or server-side code generation
    /// </summary>
    public string GenerateCode(string secret)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes, step: _timeStepSeconds, totpSize: _totpSize);
        return totp.ComputeTotp();
    }

    /// <summary>
    /// Verify a TOTP code against a secret
    /// Includes time window tolerance for clock drift
    /// </summary>
    /// <param name="secret">Base32-encoded secret key</param>
    /// <param name="code">6-digit TOTP code to verify</param>
    /// <returns>True if code is valid, false otherwise</returns>
    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: _timeStepSeconds, totpSize: _totpSize);

            // Verify with time window tolerance
            // This allows for slight clock differences between server and client
            long timeStepMatched;
            var verificationWindow = new VerificationWindow(
                previous: _toleranceSteps,
                future: _toleranceSteps
            );

            return totp.VerifyTotp(code, out timeStepMatched, verificationWindow);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generate QR code URI for authenticator apps
    /// Format: otpauth://totp/Issuer:user@example.com?secret=BASE32SECRET&amp;issuer=Issuer
    /// </summary>
    /// <param name="userEmail">User's email address</param>
    /// <param name="secret">Base32-encoded secret key</param>
    /// <returns>URI string for QR code generation</returns>
    public string GenerateQrCodeUri(string userEmail, string secret)
    {
        var encodedIssuer = Uri.EscapeDataString(_issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);
        var encodedSecret = Uri.EscapeDataString(secret);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedSecret}&issuer={encodedIssuer}&digits={_totpSize}&period={_timeStepSeconds}";
    }

    /// <summary>
    /// Generate QR code as base64-encoded PNG image
    /// Can be embedded directly in HTML: <img src="data:image/png;base64,{result}" />
    /// </summary>
    /// <param name="userEmail">User's email address</param>
    /// <param name="secret">Base32-encoded secret key</param>
    /// <param name="pixelsPerModule">Size of QR code (default 4)</param>
    /// <returns>Base64-encoded PNG image</returns>
    public string GenerateQrCodeBase64(string userEmail, string secret, int pixelsPerModule = 4)
    {
        var qrCodeUri = GenerateQrCodeUri(userEmail, secret);

        using (var qrGenerator = new QRCodeGenerator())
        using (var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q))
        using (var qrCode = new PngByteQRCode(qrCodeData))
        {
            var qrCodeImage = qrCode.GetGraphic(pixelsPerModule);
            return Convert.ToBase64String(qrCodeImage);
        }
    }

    /// <summary>
    /// Get remaining time in seconds until current TOTP code expires
    /// Useful for UI countdown timers
    /// </summary>
    public int GetRemainingSeconds()
    {
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secondsIntoTimeStep = (int)(unixTimestamp % _timeStepSeconds);
        return _timeStepSeconds - secondsIntoTimeStep;
    }

    /// <summary>
    /// Validate secret format (must be valid base32)
    /// </summary>
    public bool IsValidSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        try
        {
            Base32Encoding.ToBytes(secret);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
