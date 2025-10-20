namespace AlgoTrendy.API.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses
/// Implements OWASP security best practices
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Content Security Policy (CSP)
        // Restricts sources of content to prevent XSS attacks
        var cspPolicy = _configuration["Security:ContentSecurityPolicy"]
            ?? "default-src 'self'; " +
               "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
               "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
               "font-src 'self' https://fonts.gstatic.com data:; " +
               "img-src 'self' data: https:; " +
               "connect-src 'self' ws: wss: https://api.binance.com https://api.bybit.com https://api.okx.com https://api.coinbase.com https://api.kraken.com; " +
               "frame-ancestors 'none'; " +
               "base-uri 'self'; " +
               "form-action 'self'";

        headers["Content-Security-Policy"] = cspPolicy;

        // HTTP Strict Transport Security (HSTS)
        // Forces HTTPS connections for 1 year
        if (_configuration.GetValue<bool>("Security:EnableHSTS", true))
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // X-Content-Type-Options
        // Prevents MIME-sniffing attacks
        headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options
        // Prevents clickjacking attacks
        headers["X-Frame-Options"] = "DENY";

        // X-XSS-Protection
        // Legacy XSS protection (most browsers rely on CSP now)
        headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy
        // Controls referrer information sent with requests
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions-Policy (formerly Feature-Policy)
        // Restricts browser features
        headers["Permissions-Policy"] =
            "accelerometer=(), " +
            "camera=(), " +
            "geolocation=(), " +
            "gyroscope=(), " +
            "magnetometer=(), " +
            "microphone=(), " +
            "payment=(), " +
            "usb=()";

        // Remove server identification header (security through obscurity)
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        // Add custom security headers
        headers["X-Security-Headers"] = "enabled";
        headers["X-API-Version"] = "v2.6.0";

        _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
    }
}

/// <summary>
/// Extension methods for SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
