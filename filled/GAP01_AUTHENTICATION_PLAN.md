# GAP01: AUTHENTICATION SYSTEM IMPLEMENTATION

**Priority:** 🔴 CRITICAL - SHOWSTOPPER
**Effort:** 3-4 days
**Score Impact:** +8 points (22 → 30 security score)
**Dependencies:** NONE - Can start immediately
**Blocks:** GAP02 (needs User table), GAP04 (needs auth endpoints), GAP13-15 (security)

---

## EXECUTIVE SUMMARY

**Current State:** ❌ No authentication - API completely unsecured
**Target State:** ✅ JWT authentication with role-based access control
**Source:** v2.5 has complete implementation (`auth.py`, 130 LOC)
**Strategy:** Copy & port to C# .NET 8

**Impact:**
- Secures all API endpoints
- Enables multi-user system
- Foundation for audit trail
- Required for production deployment

---

## REFERENCE IMPLEMENTATION (V2.5)

### Source Files
```bash
/root/algotrendy_v2.5/algotrendy-api/app/auth.py
  - JWT token generation (HS256 algorithm)
  - Bcrypt password hashing
  - Token validation middleware
  - 4 demo users (admin, demo, trader, test)
  - Role-based access control

Key Functions:
- create_access_token() - Generate JWT
- verify_token() - Validate JWT
- get_password_hash() - Bcrypt hash
- verify_password() - Bcrypt verify
- get_current_user() - Extract user from token
```

### V2.5 Authentication Flow
```python
1. User submits credentials → /api/auth/login
2. Verify username + password (bcrypt)
3. Generate JWT token (HS256, 30-min expiry)
4. Return token to client
5. Client includes token in Authorization header
6. Middleware validates token on each request
7. User object attached to request context
```

---

## IMPLEMENTATION PLAN

### DAY 1: Core Authentication Service

#### Morning: Project Setup

**Install NuGet Packages:**
```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.3
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
```

#### Afternoon: Create User Model

**File:** `AlgoTrendy.Core/Models/User.cs`
```csharp
namespace AlgoTrendy.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "trader"; // admin, trader, viewer
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
}
```

**File:** `AlgoTrendy.Core/DTOs/LoginRequest.cs`
```csharp
namespace AlgoTrendy.Core.DTOs;

public record LoginRequest(
    string Username,
    string Password
);

public record LoginResponse(
    string Token,
    string Username,
    string Role,
    DateTime ExpiresAt
);

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string Role = "trader"
);
```

#### Evening: Create Auth Service Interface

**File:** `AlgoTrendy.Core/Interfaces/IAuthService.cs`
```csharp
namespace AlgoTrendy.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<User?> RegisterAsync(RegisterRequest request);
    Task<User?> ValidateTokenAsync(string token);
    string GenerateJwtToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
```

---

### DAY 2: Authentication Service Implementation

**File:** `AlgoTrendy.API/Services/AuthService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Core.DTOs;

namespace AlgoTrendy.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Find user by username
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Username} not found", request.Username);
            return null;
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {Username}", request.Username);
            return null;
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {Username} is inactive", request.Username);
            return null;
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        _logger.LogInformation("User {Username} logged in successfully", request.Username);

        return new LoginResponse(token, user.Username, user.Role, expiresAt);
    }

    public async Task<User?> RegisterAsync(RegisterRequest request)
    {
        // Check if username already exists
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: Username {Username} already exists", request.Username);
            return null;
        }

        // Check if email already exists
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
        if (existingEmail != null)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
            return null;
        }

        // Create new user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        _logger.LogInformation("User {Username} registered successfully", request.Username);

        return user;
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured");
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<User?> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSecret = _configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured");
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            return await _userRepository.GetByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
```

---

### DAY 3: Authentication Controller & Middleware

#### Morning: Create Auth Controller

**File:** `AlgoTrendy.API/Controllers/AuthController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.DTOs;

namespace AlgoTrendy.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required" });
        }

        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized(new { error = "Invalid username or password" });
        }

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username, email, and password are required" });
        }

        var user = await _authService.RegisterAsync(request);
        if (user == null)
        {
            return Conflict(new { error = "Username or email already exists" });
        }

        return Created($"/api/users/{user.Id}", new { user.Id, user.Username, user.Email, user.Role });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] string token)
    {
        var user = await _authService.ValidateTokenAsync(token);
        if (user == null)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        return Ok(new { user.Id, user.Username, user.Email, user.Role });
    }
}
```

#### Afternoon: Configure JWT Middleware

**File:** `AlgoTrendy.API/Program.cs` (add to existing)

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// ... existing code ...

// Add JWT Authentication
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "dev-secret-key-change-in-production";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();

// ... existing code ...

var app = builder.Build();

// ... existing code ...

app.UseAuthentication(); // Add before UseAuthorization
app.UseAuthorization();

// ... rest of pipeline ...
```

---

### DAY 4: Secure Existing Endpoints

#### Tasks:

1. **Add [Authorize] to Controllers:**

```csharp
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/orders")]
[Authorize] // Require authentication for all endpoints
public class OrdersController : ControllerBase
{
    // ... existing code ...

    [HttpPost]
    [Authorize(Roles = "admin,trader")] // Only admin and trader can place orders
    public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
    {
        // Get current user from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // ... rest of logic ...
    }
}
```

2. **Add Role-Based Access:**

```csharp
// Admin-only endpoints
[HttpDelete("{id}")]
[Authorize(Roles = "admin")]
public async Task<IActionResult> DeleteOrder(string id) { ... }

// Trader + Admin endpoints
[HttpPost]
[Authorize(Roles = "admin,trader")]
public async Task<IActionResult> PlaceOrder(...) { ... }

// Any authenticated user
[HttpGet]
[Authorize]
public async Task<IActionResult> GetOrders() { ... }

// Public endpoint (no authentication)
[HttpGet("health")]
[AllowAnonymous]
public IActionResult Health() { ... }
```

3. **Create Demo Users Seed Data:**

```csharp
// Create seed data in UserRepository or migration
public static class DemoUsers
{
    public static async Task SeedAsync(IUserRepository userRepository, IAuthService authService)
    {
        var demoUsers = new[]
        {
            new { Username = "admin", Email = "admin@algotrendy.com", Password = "admin123", Role = "admin" },
            new { Username = "trader", Email = "trader@algotrendy.com", Password = "trader123", Role = "trader" },
            new { Username = "viewer", Email = "viewer@algotrendy.com", Password = "viewer123", Role = "viewer" },
            new { Username = "demo", Email = "demo@algotrendy.com", Password = "demo123", Role = "trader" }
        };

        foreach (var demo in demoUsers)
        {
            var existing = await userRepository.GetByUsernameAsync(demo.Username);
            if (existing == null)
            {
                await authService.RegisterAsync(new RegisterRequest(
                    demo.Username,
                    demo.Email,
                    demo.Password,
                    demo.Role
                ));
            }
        }
    }
}
```

---

### DAY 5: Testing & Documentation

#### Unit Tests

**File:** `AlgoTrendy.Tests/Unit/AuthServiceTests.cs`

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using AlgoTrendy.API.Services;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.DTOs;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<AuthService>>();

        mockConfig.Setup(c => c["JWT_SECRET"]).Returns("test-secret-key-32-characters-min");

        var authService = new AuthService(mockUserRepo.Object, mockConfig.Object, mockLogger.Object);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = authService.HashPassword("password123"),
            Role = "trader",
            IsActive = true
        };

        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var request = new LoginRequest("testuser", "password123");

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("testuser");
        result.Role.Should().Be("trader");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<AuthService>>();

        var authService = new AuthService(mockUserRepo.Object, mockConfig.Object, mockLogger.Object);

        var user = new User
        {
            Username = "testuser",
            PasswordHash = authService.HashPassword("correct-password"),
            IsActive = true
        };

        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var request = new LoginRequest("testuser", "wrong-password");

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HashPassword_CreatesValidHash()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(null!, mockConfig.Object, mockLogger.Object);

        var password = "mySecurePassword123";

        // Act
        var hash = authService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        authService.VerifyPassword(password, hash).Should().BeTrue();
    }
}
```

#### Integration Tests

**File:** `AlgoTrendy.Tests/Integration/AuthenticationTests.cs`

```csharp
[Fact]
public async Task LoginEndpoint_ValidCredentials_ReturnsToken()
{
    // Arrange
    var client = _factory.CreateClient();
    var loginRequest = new LoginRequest("admin", "admin123");

    // Act
    var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
    result.Should().NotBeNull();
    result!.Token.Should().NotBeNullOrEmpty();
}

[Fact]
public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/orders");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();

    // Login first
    var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
        new LoginRequest("admin", "admin123"));
    var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

    // Add token to request
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", loginResult!.Token);

    // Act
    var response = await client.GetAsync("/api/orders");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---

## ENVIRONMENT CONFIGURATION

**Add to `.env`:**
```bash
JWT_SECRET=your-secret-key-min-32-characters-change-in-production
JWT_EXPIRY_MINUTES=30
```

**Add to `appsettings.json`:**
```json
{
  "JWT_SECRET": "",
  "JWT_EXPIRY_MINUTES": 30,
  "Authentication": {
    "RequireHttpsMetadata": true,
    "SaveToken": true
  }
}
```

---

## TESTING CHECKLIST

### Manual Testing

```bash
# 1. Register new user
curl -X POST http://localhost:5002/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"test123"}'

# 2. Login
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'

# Response: {"token":"eyJ...", "username":"testuser", "role":"trader", ...}

# 3. Use token to access protected endpoint
curl -X GET http://localhost:5002/api/orders \
  -H "Authorization: Bearer <token-from-step-2>"

# 4. Try without token (should fail)
curl -X GET http://localhost:5002/api/orders
# Response: 401 Unauthorized
```

### Automated Testing

```bash
# Run all tests
dotnet test

# Run only auth tests
dotnet test --filter "FullyQualifiedName~Auth"

# Check code coverage
dotnet test /p:CollectCoverage=true
```

---

## ACCEPTANCE CRITERIA

**Gap is complete when:**
- [ ] JWT authentication working (login/logout)
- [ ] 4 demo users seeded (admin, trader, viewer, demo)
- [ ] All API endpoints secured with [Authorize]
- [ ] Role-based access control working
- [ ] Password hashing with BCrypt (work factor 12)
- [ ] Token expiry working (30 minutes)
- [ ] 15+ unit tests passing
- [ ] 5+ integration tests passing
- [ ] Manual testing successful
- [ ] Documentation updated
- [ ] Security audit passed (no hardcoded secrets)

---

## ROLLBACK PLAN

**If implementation fails:**
1. Keep authentication optional with feature flag
2. Allow unauthenticated access in development
3. Defer to Week 2 if blocking other work
4. Minimal fallback: API key authentication

---

## POST-COMPLETION

**Next steps after GAP01:**
1. GAP02 can proceed (UserRepository complete)
2. GAP04 can proceed (auth endpoints available)
3. GAP13-15 can build on this foundation
4. Update API documentation (Swagger)
5. Train team on authentication flow

---

**STATUS:** READY TO IMPLEMENT
**ESTIMATED TIME:** 3-4 days
**CONFIDENCE:** VERY HIGH (v2.5 implementation exists)

---

**END OF GAP01 AUTHENTICATION PLAN**
