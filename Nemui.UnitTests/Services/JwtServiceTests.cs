using FluentAssertions;
using Microsoft.Extensions.Options;
using Nemui.Infrastructure.Configurations;
using Nemui.Infrastructure.Services;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Nemui.UnitTests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "this-is-a-very-long-secret-key-for-testing-purposes-that-is-at-least-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationInMinutes = 15,
            RefreshTokenExpirationInDays = 30
        };

        var options = Options.Create(_jwtSettings);
        _jwtService = new JwtService(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_JwtSettings_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new JwtService(null!));
        exception.ParamName.Should().Be("jwtSettings");
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_JwtSettings_Value_Is_Null()
    {
        // Arrange
        var options = Options.Create<JwtSettings>(null!);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new JwtService(options));
        exception.ParamName.Should().Be("jwtSettings");
    }

    #endregion

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_Should_Return_Valid_JWT_Token()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Verify it's a valid JWT format (3 parts separated by dots)
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateAccessToken_Should_Include_All_Required_Claims()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);

        // FIX: Use the actual claim names that appear in JWT tokens
        jsonToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == user.Id.ToString());
        jsonToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == user.Name);
        jsonToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jsonToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == user.Role);
        jsonToken.Claims.Should().Contain(c => c.Type == AuthConstants.ClaimTypes.EmailVerified && c.Value == user.IsEmailVerified.ToString());
        jsonToken.Claims.Should().Contain(c => c.Type == AuthConstants.ClaimTypes.JwtId);
    }

    [Fact]
    public void GenerateAccessToken_Should_Set_Correct_Token_Properties()
    {
        // Arrange
        var user = CreateTestUser();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);

        // Verify issuer and audience
        jsonToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jsonToken.Audiences.Should().Contain(_jwtSettings.Audience);

        // Verify expiration time (should be around 15 minutes from now)
        var expectedExpiry = beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes);
        jsonToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateAccessToken_Should_Handle_Empty_User_Properties(string emptyValue)
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = emptyValue ?? string.Empty,
            Email = emptyValue ?? string.Empty,
            Role = emptyValue ?? string.Empty,
            IsEmailVerified = false
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
    
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);
    
        // FIX: Use actual JWT claim names
        jsonToken.Claims.Should().Contain(c => c.Type == "unique_name");
        jsonToken.Claims.Should().Contain(c => c.Type == "email");
        jsonToken.Claims.Should().Contain(c => c.Type == "role");
    }

    [Fact]
    public void GenerateAccessToken_Should_Generate_Different_Tokens_For_Same_User()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token1 = _jwtService.GenerateAccessToken(user);
        var token2 = _jwtService.GenerateAccessToken(user);

        // Assert
        token1.Should().NotBe(token2); // Different JTI (JWT ID) should make them different
    }

    [Fact]
    public void GenerateAccessToken_Should_Generate_Different_Tokens_For_Different_Users()
    {
        // Arrange
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();

        // Act
        var token1 = _jwtService.GenerateAccessToken(user1);
        var token2 = _jwtService.GenerateAccessToken(user2);

        // Assert
        token1.Should().NotBe(token2);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_Should_Return_Non_Empty_String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_Base64_String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        // Should be able to convert from Base64 without exception
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64); // 64 bytes as specified in the service
    }

    [Fact]
    public void GenerateRefreshToken_Should_Generate_Different_Tokens()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateRefreshToken_Should_Generate_Tokens_Of_Expected_Length()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        // Base64 encoding of 64 bytes should be around 88 characters
        refreshToken.Length.Should().BeGreaterThan(80);
        refreshToken.Length.Should().BeLessThan(100);
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_Should_Return_True_For_Valid_Token()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var isValid = await _jwtService.ValidateTokenAsync(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid-token")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature")]
    public async Task ValidateTokenAsync_Should_Return_False_For_Invalid_Token(string invalidToken)
    {
        // Act
        var isValid = await _jwtService.ValidateTokenAsync(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_Should_Return_False_For_Expired_Token()
    {
        // Arrange - Create service with very short expiration
        var shortExpirySettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            AccessTokenExpirationInMinutes = -1 // Already expired
        };

        var shortExpiryService = new JwtService(Options.Create(shortExpirySettings));
        var user = CreateTestUser();
        var expiredToken = shortExpiryService.GenerateAccessToken(user);

        // Act
        var isValid = await _jwtService.ValidateTokenAsync(expiredToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_Should_Return_False_For_Token_With_Wrong_Secret()
    {
        // Arrange - Create token with different secret
        var differentSettings = new JwtSettings
        {
            SecretKey = "different-secret-key-that-is-at-least-32-characters-long-for-testing",
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            AccessTokenExpirationInMinutes = 15
        };

        var differentService = new JwtService(Options.Create(differentSettings));
        var user = CreateTestUser();
        var tokenWithDifferentSecret = differentService.GenerateAccessToken(user);

        // Act
        var isValid = await _jwtService.ValidateTokenAsync(tokenWithDifferentSecret);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region ValidateTokenAndGetPrincipalAsync Tests

    [Fact]
    public async Task ValidateTokenAndGetPrincipalAsync_Should_Return_ClaimsPrincipal_For_Valid_Token()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Identity.AuthenticationType.Should().Be("jwt");

        // Verify claims
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(user.Name);
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
        principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(user.Role);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid-token")]
    public async Task ValidateTokenAndGetPrincipalAsync_Should_Return_Null_For_Invalid_Token(string invalidToken)
    {
        // Act
        var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    #endregion

    #region GetUserIdFromTokenAsync Tests

    [Fact]
    public async Task GetUserIdFromTokenAsync_Should_Return_UserId_For_Valid_Token()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var userId = await _jwtService.GetUserIdFromTokenAsync(token);

        // Assert
        userId.Should().Be(user.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid-token")]
    public async Task GetUserIdFromTokenAsync_Should_Return_Null_For_Invalid_Token(string invalidToken)
    {
        // Act
        var userId = await _jwtService.GetUserIdFromTokenAsync(invalidToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public async Task GetUserIdFromTokenAsync_Should_Return_Null_For_Token_Without_NameIdentifier_Claim()
    {
        // This test is theoretical since our GenerateAccessToken always includes NameIdentifier
        // But it tests the robustness of the method
        
        // Arrange - Create a token manually without NameIdentifier claim
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com")
                // No NameIdentifier claim
            }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), 
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Act
        var userId = await _jwtService.GetUserIdFromTokenAsync(tokenString);

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region GetTokenExpirationTime Tests

    [Fact]
    public void GetTokenExpirationTime_Should_Return_Future_DateTime()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expirationTime = _jwtService.GetTokenExpirationTime();

        // Assert
        expirationTime.Should().BeAfter(beforeCall);
    }

    [Fact]
    public void GetTokenExpirationTime_Should_Return_Time_Based_On_Settings()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expirationTime = _jwtService.GetTokenExpirationTime();

        // Assert
        var expectedTime = beforeCall.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes);
        expirationTime.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetTokenExpirationTime_Should_Be_Consistent_With_Generated_Token_Expiry()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _jwtService.GenerateAccessToken(user);
        var methodExpirationTime = _jwtService.GetTokenExpirationTime();

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);
        
        // Should be very close (within a few seconds)
        jsonToken.ValidTo.Should().BeCloseTo(methodExpirationTime, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Complete_Token_Lifecycle_Should_Work_Together()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert - Generate Access Token
        var accessToken = _jwtService.GenerateAccessToken(user);
        accessToken.Should().NotBeNullOrEmpty();

        // Act & Assert - Generate Refresh Token
        var refreshToken = _jwtService.GenerateRefreshToken();
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBe(accessToken);

        // Act & Assert - Validate Access Token
        var isValid = await _jwtService.ValidateTokenAsync(accessToken);
        isValid.Should().BeTrue();

        // Act & Assert - Get Principal from Token
        var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(accessToken);
        principal.Should().NotBeNull();

        // Act & Assert - Get UserId from Token
        var userIdFromToken = await _jwtService.GetUserIdFromTokenAsync(accessToken);
        userIdFromToken.Should().Be(user.Id);

        // Act & Assert - Get Expiration Time
        var expirationTime = _jwtService.GetTokenExpirationTime();
        expirationTime.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Token_Should_Contain_All_User_Information()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john.doe@example.com",
            Role = AuthConstants.Roles.Admin,
            IsEmailVerified = true
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);
        var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be("John Doe");
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be("john.doe@example.com");
        principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(AuthConstants.Roles.Admin);
        principal.FindFirst(AuthConstants.ClaimTypes.EmailVerified)?.Value.Should().Be("True");
        principal.FindFirst(AuthConstants.ClaimTypes.JwtId)?.Value.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Role = AuthConstants.Roles.User,
            IsEmailVerified = false
        };
    }

    #endregion
}