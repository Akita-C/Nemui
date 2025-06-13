using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using Moq;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Repositories;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Infrastructure.Services;
using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.Entities;

namespace Nemui.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<IValidator<LoginRequest>> _mockLoginValidator;
    private readonly Mock<IValidator<RegisterRequest>> _mockRegisterValidator;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IUserCacheService> _mockUserCacheService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockJwtService = new Mock<IJwtService>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockLoginValidator = new Mock<IValidator<LoginRequest>>();
        _mockRegisterValidator = new Mock<IValidator<RegisterRequest>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockUserCacheService = new Mock<IUserCacheService>();

        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(x => x.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);

        var jwtSettings = new JwtSettings
        {
            RefreshTokenExpirationInDays = 7
        };
        var authSettings = new AuthSettings();

        var jwtOptions = Options.Create(jwtSettings);
        var authOptions = Options.Create(authSettings);

        // _authService = new AuthService(
        //     _mockUnitOfWork.Object,
        //     _mockJwtService.Object,
        //     _mockPasswordService.Object,
        //     _mockLoginValidator.Object,
        //     _mockRegisterValidator.Object,
        //     jwtOptions,
        //     authOptions,
        //     _mockUserCacheService.Object);
    }
    
    [Fact]
    public async Task LoginAsync_Should_Return_AuthResponse_When_Valid_Credentials()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginRequest.Email,
            Name = "Test User",
            PasswordHash = "hashed-password",
            Role = "User",
            IsEmailVerified = true,
            IsActive = true
        };

        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var expirationTime = DateTime.UtcNow.AddHours(1);

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // Valid result

        _mockUserRepository.Setup(x => x.GetByEmailAsync(loginRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash))
            .Returns(true);

        _mockJwtService.Setup(x => x.GenerateAccessToken(user))
            .Returns(accessToken);

        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtService.Setup(x => x.GetTokenExpirationTime())
            .Returns(expirationTime);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _authService.LoginAsync(loginRequest);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.ExpiresAt.Should().Be(expirationTime);
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(user.Email);
        result.User.Name.Should().Be(user.Name);

        _mockLoginValidator.Verify(x => x.ValidateAsync(loginRequest, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.GetByEmailAsync(loginRequest.Email, It.IsAny<CancellationToken>()), Times.Once);
        _mockPasswordService.Verify(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash), Times.Once);
        _mockJwtService.Verify(x => x.GenerateAccessToken(user), Times.Once);
        _mockJwtService.Verify(x => x.GenerateRefreshToken(), Times.Once);
        _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task LoginAsync_Should_Throw_ValidationException_When_Invalid_Request()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "invalid-email",
            Password = ""
        };

        var validationErrors = new List<ValidationFailure>
        {
            new("Email", "Invalid email format"),
            new("Password", "Password is required")
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationErrors));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.LoginAsync(loginRequest));

        exception.Errors.Should().HaveCount(2);
        
        // Verify that no other services were called
        _mockUserRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPasswordService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedAccessException_When_User_Not_Found()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "notfound@example.com",
            Password = "ValidPassword123!"
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockUserRepository.Setup(x => x.GetByEmailAsync(loginRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // User not found

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginRequest));

        exception.Message.Should().Contain("Invalid email or password");

        // Verify password was not checked
        _mockPasswordService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedAccessException_When_Wrong_Password()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginRequest.Email,
            PasswordHash = "correct-hash",
            IsActive = true
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockUserRepository.Setup(x => x.GetByEmailAsync(loginRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash))
            .Returns(false); // Wrong password

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginRequest));

        exception.Message.Should().Contain("Invalid email or password");

        // Verify tokens were not generated
        _mockJwtService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        _mockJwtService.Verify(x => x.GenerateRefreshToken(), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Inactive()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginRequest.Email,
            PasswordHash = "hashed-password",
            IsActive = false // User is inactive
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockUserRepository.Setup(x => x.GetByEmailAsync(loginRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash))
            .Returns(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginRequest));

        exception.Message.Should().Contain("Account is deactivated");
    }
    
    [Fact]
    public async Task RegisterAsync_Should_Return_AuthResponse_When_Valid_Request()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "ValidPassword123!",
            PasswordConfirmation = "ValidPassword123!"
        };

        var hashedPassword = "hashed-password";
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var expirationTime = DateTime.UtcNow.AddHours(1);

        _mockRegisterValidator.Setup(x => x.ValidateAsync(registerRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockUserRepository.Setup(x => x.GetByEmailAsync(registerRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // Email not exists

        _mockPasswordService.Setup(x => x.HashPassword(registerRequest.Password))
            .Returns(hashedPassword);

        _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(accessToken);

        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtService.Setup(x => x.GetTokenExpirationTime())
            .Returns(expirationTime);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.ExpiresAt.Should().Be(expirationTime);
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(registerRequest.Email);
        result.User.Name.Should().Be(registerRequest.Name);

        // Verify user was created with correct properties
        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == registerRequest.Email &&
            u.Name == registerRequest.Name &&
            u.PasswordHash == hashedPassword &&
            u.Role == "User" &&
            u.IsEmailVerified == false &&
            u.IsActive == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_InvalidOperationException_When_Email_Already_Exists()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "John Doe",
            Email = "existing@example.com",
            Password = "ValidPassword123!",
            PasswordConfirmation = "ValidPassword123!"
        };

        var existingUser = new User { Email = registerRequest.Email };

        _mockRegisterValidator.Setup(x => x.ValidateAsync(registerRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockUserRepository.Setup(x => x.GetByEmailAsync(registerRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser); // Email already exists

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(registerRequest));

        exception.Message.Should().Contain("User with this email already exists");

        // Verify user was not created
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPasswordService.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task LogoutAsync_Should_Return_True_When_Valid_RefreshToken()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _authService.LogoutAsync(refreshToken);

        // Assert
        result.Should().BeTrue();

        // Verify token was revoked
        _mockRefreshTokenRepository.Verify(x => x.RevokeTokenAsync(refreshToken, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task LogoutAsync_Should_Return_False_When_Invalid_RefreshToken(string refreshToken)
    {
        // Act
        var result = await _authService.LogoutAsync(refreshToken);

        // Assert
        result.Should().BeFalse();

        // Verify no repository calls were made
        _mockRefreshTokenRepository.Verify(x => x.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}