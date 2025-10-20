using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AlgoTrendy.API.Middleware;

/// <summary>
/// Middleware for JWT token authentication
/// Validates JWT tokens in Authorization header
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractTokenFromHeader(context);

        if (!string.IsNullOrEmpty(token))
        {
            await ValidateAndAttachUser(context, token);
        }

        await _next(context);
    }

    private string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        // Bearer token format: "Bearer {token}"
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    private async Task ValidateAndAttachUser(HttpContext context, string token)
    {
        try
        {
            var jwtSecret = _configuration["JWT:Secret"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? throw new InvalidOperationException("JWT secret not configured");

            var jwtIssuer = _configuration["JWT:Issuer"] ?? "AlgoTrendy.API";
            var jwtAudience = _configuration["JWT:Audience"] ?? "AlgoTrendy.Client";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes of clock skew
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Attach user principal to context
            context.User = principal;

            // Log successful authentication
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            _logger.LogInformation("User {UserId} authenticated via JWT token", userId);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token expired for request to {Path}", context.Request.Path);
            // Don't attach user, let Authorization handle it
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid JWT token for request to {Path}", context.Request.Path);
            // Don't attach user, let Authorization handle it
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token for request to {Path}", context.Request.Path);
            // Don't attach user, let Authorization handle it
        }
    }
}

/// <summary>
/// Extension methods for JwtAuthenticationMiddleware
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
