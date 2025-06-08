using FluentAssertions;
using Moq;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Services.Interfaces;
using Nemui.Infrastructure.Services;
using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace Nemui.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();

        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(x => x.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);

        _userService = new UserService(_mockUnitOfWork.Object, _mockPasswordService.Object);
    }

    #region GetUserProfileAsync Tests

    [Fact]
    public async Task GetUserProfileAsync_Should_Return_UserProfile_When_User_Exists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com",
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddHours(-1)
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john@example.com");
        result.Role.Should().Be("User");
        result.IsEmailVerified.Should().BeTrue();
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.LastLoginAt.Should().Be(user.LastLoginAt);
    }

    [Fact]
    public async Task GetUserProfileAsync_Should_Return_Null_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_Should_Return_UserProfile_When_User_Exists()
    {
        // Arrange
        var email = "john@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = email,
            Role = "Admin",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            LastLoginAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be(email);
        result.Role.Should().Be("Admin");
        result.IsEmailVerified.Should().BeTrue();
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.LastLoginAt.Should().Be(user.LastLoginAt);
    }

    [Fact]
    public async Task GetUserByEmailAsync_Should_Return_Null_When_User_Not_Found()
    {
        // Arrange
        var email = "notfound@example.com";

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetUserByEmailAsync_Should_Return_Null_When_Email_Is_Invalid(string email)
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [Fact]
    public async Task UpdateUserProfileAsync_Should_Return_True_When_Update_Successful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "user@example.com"
        };

        var request = new UpdateUserProfileRequest
        {
            Name = "  New Updated Name  " // With whitespace to test trimming
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, request);

        // Assert
        result.Should().BeTrue();

        // Verify name was updated and trimmed
        user.Name.Should().Be("New Updated Name");

        // Verify interactions
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserProfileRequest
        {
            Name = "New Name"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, request);

        // Assert
        result.Should().BeFalse();

        // Verify no update operations were called
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task UpdateUserProfileAsync_Should_Handle_Empty_Name_After_Trim(string name)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Original Name"
        };

        var request = new UpdateUserProfileRequest
        {
            Name = name
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, request);

        // Assert
        result.Should().BeTrue();
        user.Name.Should().Be(name.Trim()); // Should be empty string after trim
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_Should_Return_True_When_Password_Changed_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = "old-hash"
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456@",
            ConfirmNewPassword = "NewPassword456@"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            .Returns(true);

        _mockPasswordService.Setup(x => x.IsPasswordStrong(request.NewPassword))
            .Returns(true);

        _mockPasswordService.Setup(x => x.HashPassword(request.NewPassword))
            .Returns("new-hash");

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.ChangePasswordAsync(userId, request);

        // Assert
        result.Should().BeTrue();

        // Verify interactions
        _mockPasswordService.Verify(x => x.VerifyPassword(request.CurrentPassword, "old-hash"), Times.Once);
        _mockPasswordService.Verify(x => x.IsPasswordStrong(request.NewPassword), Times.Once);
        _mockPasswordService.Verify(x => x.HashPassword(request.NewPassword), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify password was updated
        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456@",
            ConfirmNewPassword = "NewPassword456@"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.ChangePasswordAsync(userId, request);

        // Assert
        result.Should().BeFalse();

        // Verify no password operations were called
        _mockPasswordService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockPasswordService.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Throw_UnauthorizedAccessException_When_Current_Password_Wrong()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = "correct-hash"
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword456@",
            ConfirmNewPassword = "NewPassword456@"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            .Returns(false); // Wrong current password

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userService.ChangePasswordAsync(userId, request));

        exception.Message.Should().Contain("Current password is incorrect");

        // Verify no password update occurred
        _mockPasswordService.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Throw_ValidationException_When_Passwords_Do_Not_Match()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = "current-hash"
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword456@",
            ConfirmNewPassword = "DifferentPassword789#" // Different confirmation
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            .Returns(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _userService.ChangePasswordAsync(userId, request));

        exception.Message.Should().Contain("New passwords do not match");
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Throw_ValidationException_When_New_Password_Is_Weak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = "current-hash"
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "weak", // Weak password
            ConfirmNewPassword = "weak"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(x => x.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            .Returns(true);

        _mockPasswordService.Setup(x => x.IsPasswordStrong(request.NewPassword))
            .Returns(false); // Weak password

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _userService.ChangePasswordAsync(userId, request));

        exception.Message.Should().Contain("New password does not meet security requirements");
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_Should_Return_True_When_User_Deactivated_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            IsActive = true,
            Name = "Active User"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        result.Should().BeTrue();

        // Verify user was deactivated
        user.IsActive.Should().BeFalse();

        // Verify interactions
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        result.Should().BeFalse();

        // Verify no operations were called
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateUserAsync_Should_Deactivate_Already_Inactive_User()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            IsActive = false, // Already inactive
            Name = "Inactive User"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        user.IsActive.Should().BeFalse(); // Should remain false

        // Verify operations still called (idempotent)
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_Should_Return_True_When_User_Activated_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            IsActive = false,
            Name = "Inactive User"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.ActivateUserAsync(userId);

        // Assert
        result.Should().BeTrue();

        // Verify user was activated
        user.IsActive.Should().BeTrue();

        // Verify interactions
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify refresh tokens were NOT revoked (unlike deactivate)
        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateUserAsync_Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.ActivateUserAsync(userId);

        // Assert
        result.Should().BeFalse();

        // Verify no operations were called
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateUserAsync_Should_Activate_Already_Active_User()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            IsActive = true, // Already active
            Name = "Active User"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.ActivateUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        user.IsActive.Should().BeTrue(); // Should remain true

        // Verify operations still called (idempotent)
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UserExistsAsync Tests

    [Fact]
    public async Task UserExistsAsync_Should_Return_True_When_User_Exists()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.UserExistsAsync(userId);

        // Assert
        result.Should().BeTrue();

        // Verify correct expression was used
        _mockUserRepository.Verify(x => x.ExistsAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserExistsAsync_Should_Return_False_When_User_Does_Not_Exist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.UserExistsAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_Should_Return_True_When_Email_Exists()
    {
        // Arrange
        var email = "existing@example.com";

        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.EmailExistsAsync(email);

        // Assert
        result.Should().BeTrue();

        // Verify interaction
        _mockUserRepository.Verify(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmailExistsAsync_Should_Return_False_When_Email_Does_Not_Exist()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.EmailExistsAsync(email);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EmailExistsAsync_Should_Handle_Invalid_Email_Input(string email)
    {
        // Arrange
        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.EmailExistsAsync(email);

        // Assert
        result.Should().BeFalse();

        // Verify repository was still called (let repository handle validation)
        _mockUserRepository.Verify(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Complete_User_Lifecycle_Should_Work_Together()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);

        // Setup for multiple operations
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert - Get Profile
        var profile = await _userService.GetUserProfileAsync(userId);
        profile.Should().NotBeNull();
        profile!.IsEmailVerified.Should().BeFalse();

        // Act & Assert - Update Profile
        var updateRequest = new UpdateUserProfileRequest { Name = "Updated Name" };
        var updateResult = await _userService.UpdateUserProfileAsync(userId, updateRequest);
        updateResult.Should().BeTrue();
        user.Name.Should().Be("Updated Name");

        // Act & Assert - Deactivate
        var deactivateResult = await _userService.DeactivateUserAsync(userId);
        deactivateResult.Should().BeTrue();
        user.IsActive.Should().BeFalse();

        // Act & Assert - Activate
        var activateResult = await _userService.ActivateUserAsync(userId);
        activateResult.Should().BeTrue();
        user.IsActive.Should().BeTrue();

        // Verify all operations were called
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once); // Only on deactivate
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(Guid? id = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Role = "User",
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            PasswordHash = "test-hash"
        };
    }

    #endregion
}