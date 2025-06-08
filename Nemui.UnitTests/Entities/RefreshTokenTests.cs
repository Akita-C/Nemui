using FluentAssertions;
using Nemui.Shared.Entities;

namespace Nemui.UnitTests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_Should_Return_True_When_Token_Is_Expired()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            UserId = Guid.NewGuid()
        };
        
        var isExpired = refreshToken.IsExpired();
        
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Should_Return_False_When_Token_Is_Not_Expired()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1), // Expires tomorrow
            UserId = Guid.NewGuid()
        };
        
        var isExpired = refreshToken.IsExpired();
        
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_Should_Use_Provided_CurrentTime()
    {
        var specificTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = specificTime.AddHours(-1), // Expired 1 hour before specific time
            UserId = Guid.NewGuid()
        };
        
        var isExpired = refreshToken.IsExpired(specificTime);
        
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Should_Return_True_When_ExpiresAt_Equals_CurrentTime()
    {
        var specificTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = specificTime, // Expires exactly at specific time
            UserId = Guid.NewGuid()
        };
        
        var isExpired = refreshToken.IsExpired(specificTime);
        
        isExpired.Should().BeTrue(); // >= comparison, so equal time means expired
    }

    [Fact]
    public void IsRevoked_Should_Return_True_When_Token_Is_Revoked()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow, // Token is revoked
            UserId = Guid.NewGuid()
        };
        
        var isRevoked = refreshToken.IsRevoked;
        
        isRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_Should_Return_False_When_Token_Is_Not_Revoked()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null, // Token is not revoked
            UserId = Guid.NewGuid()
        };
        
        var isRevoked = refreshToken.IsRevoked;
        
        isRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Should_Return_True_When_Token_Is_Not_Expired_And_Not_Revoked()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1), // Not expired
            RevokedAt = null, // Not revoked
            UserId = Guid.NewGuid()
        };
        
        var isActive = refreshToken.IsActive();
        
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Should_Return_False_When_Token_Is_Expired()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            RevokedAt = null, // Not revoked
            UserId = Guid.NewGuid()
        };
        
        var isActive = refreshToken.IsActive();
        
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Should_Return_False_When_Token_Is_Revoked()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1), // Not expired
            RevokedAt = DateTime.UtcNow, // Revoked
            UserId = Guid.NewGuid()
        };
        
        var isActive = refreshToken.IsActive();
        
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Should_Return_False_When_Token_Is_Both_Expired_And_Revoked()
    {
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            RevokedAt = DateTime.UtcNow, // Revoked
            UserId = Guid.NewGuid()
        };
        
        var isActive = refreshToken.IsActive();
        
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Should_Use_Provided_CurrentTime()
    {
        var specificTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            ExpiresAt = specificTime.AddHours(1), // Expires 1 hour after specific time
            RevokedAt = null, // Not revoked
            UserId = Guid.NewGuid()
        };
        
        var isActive = refreshToken.IsActive(specificTime);
        
        isActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Should_Initialize_Properties_With_Default_Values()
    {
        var refreshToken = new RefreshToken();
        
        refreshToken.Token.Should().Be(string.Empty);
        refreshToken.ExpiresAt.Should().Be(default(DateTime));
        refreshToken.RevokedAt.Should().BeNull();
        refreshToken.UserId.Should().Be(Guid.Empty);
        refreshToken.User.Should().BeNull();
    }

    [Fact]
    public void Properties_Should_Be_Settable()
    {
        var token = "test-refresh-token";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var revokedAt = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        
        var refreshToken = new RefreshToken
        {
            Token = token,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            UserId = userId,
            User = user
        };
        
        refreshToken.Token.Should().Be(token);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.RevokedAt.Should().Be(revokedAt);
        refreshToken.UserId.Should().Be(userId);
        refreshToken.User.Should().Be(user);
    }
}