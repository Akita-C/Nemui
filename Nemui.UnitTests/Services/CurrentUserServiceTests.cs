using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Nemui.Infrastructure.Services;
using System.Security.Claims;

namespace Nemui.UnitTests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockClaimsPrincipal;
    private readonly Mock<ClaimsIdentity> _mockClaimsIdentity;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockClaimsPrincipal = new Mock<ClaimsPrincipal>();
        _mockClaimsIdentity = new Mock<ClaimsIdentity>();

        _currentUserService = new CurrentUserService(_mockHttpContextAccessor.Object);
    }

    #region UserId Tests

    [Fact]
    public void UserId_Should_Return_UserId_When_Valid_Claims_Present()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, expectedUserId),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var userId = _currentUserService.UserId;

        // Assert
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_Should_Return_Null_When_No_NameIdentifier_Claim()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
            // No NameIdentifier claim
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var userId = _currentUserService.UserId;

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Return_Null_When_HttpContext_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var userId = _currentUserService.UserId;

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Return_Null_When_User_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);

        // Act
        var userId = _currentUserService.UserId;

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region Email Tests

    [Fact]
    public void Email_Should_Return_Email_When_Valid_Claims_Present()
    {
        // Arrange
        var expectedEmail = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, expectedEmail),
            new(ClaimTypes.Name, "Test User")
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var email = _currentUserService.Email;

        // Assert
        email.Should().Be(expectedEmail);
    }

    [Fact]
    public void Email_Should_Return_Null_When_No_Email_Claim()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "Test User")
            // No Email claim
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var email = _currentUserService.Email;

        // Assert
        email.Should().BeNull();
    }

    [Fact]
    public void Email_Should_Return_Null_When_HttpContext_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var email = _currentUserService.Email;

        // Assert
        email.Should().BeNull();
    }

    #endregion

    #region Name Tests

    [Fact]
    public void Name_Should_Return_Name_When_Valid_Claims_Present()
    {
        // Arrange
        var expectedName = "John Doe";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, expectedName)
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var name = _currentUserService.Name;

        // Assert
        name.Should().Be(expectedName);
    }

    [Fact]
    public void Name_Should_Return_Null_When_No_Name_Claim()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com")
            // No Name claim
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var name = _currentUserService.Name;

        // Assert
        name.Should().BeNull();
    }

    [Fact]
    public void Name_Should_Return_Null_When_HttpContext_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var name = _currentUserService.Name;

        // Assert
        name.Should().BeNull();
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_Should_Return_True_When_User_Is_Authenticated()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act
        var isAuthenticated = _currentUserService.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_Should_Return_False_When_User_Is_Not_Authenticated()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: false);

        // Act
        var isAuthenticated = _currentUserService.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_Should_Return_False_When_HttpContext_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var isAuthenticated = _currentUserService.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_Should_Return_False_When_User_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);

        // Act
        var isAuthenticated = _currentUserService.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_Should_Return_False_When_Identity_Is_Null()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(_mockClaimsPrincipal.Object);
        _mockClaimsPrincipal.Setup(x => x.Identity).Returns((ClaimsIdentity?)null);

        // Act
        var isAuthenticated = _currentUserService.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void All_Properties_Should_Work_Together_For_Authenticated_User()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var expectedEmail = "john.doe@example.com";
        var expectedName = "John Doe";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, expectedUserId),
            new(ClaimTypes.Email, expectedEmail),
            new(ClaimTypes.Name, expectedName),
            new(ClaimTypes.Role, "User")
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act & Assert
        _currentUserService.UserId.Should().Be(expectedUserId);
        _currentUserService.Email.Should().Be(expectedEmail);
        _currentUserService.Name.Should().Be(expectedName);
        _currentUserService.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void All_Properties_Should_Return_Null_Or_False_For_Unauthenticated_User()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: false);

        // Act & Assert
        _currentUserService.UserId.Should().NotBeNull(); // Claims still accessible
        _currentUserService.Email.Should().NotBeNull();
        _currentUserService.Name.Should().NotBeNull();
        _currentUserService.IsAuthenticated.Should().BeFalse(); // But not authenticated
    }

    [Fact]
    public void All_Properties_Should_Handle_Null_HttpContext_Gracefully()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act & Assert
        _currentUserService.UserId.Should().BeNull();
        _currentUserService.Email.Should().BeNull();
        _currentUserService.Name.Should().BeNull();
        _currentUserService.IsAuthenticated.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Properties_Should_Handle_Empty_Claim_Values(string emptyValue)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, emptyValue),
            new(ClaimTypes.Email, emptyValue),
            new(ClaimTypes.Name, emptyValue)
        };

        SetupHttpContextWithClaims(claims, isAuthenticated: true);

        // Act & Assert
        _currentUserService.UserId.Should().Be(emptyValue);
        _currentUserService.Email.Should().Be(emptyValue);
        _currentUserService.Name.Should().Be(emptyValue);
        _currentUserService.IsAuthenticated.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private void SetupHttpContextWithClaims(List<Claim> claims, bool isAuthenticated)
    {
        var claimsIdentity = new ClaimsIdentity(claims, isAuthenticated ? "Bearer" : null);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
    }

    #endregion
}