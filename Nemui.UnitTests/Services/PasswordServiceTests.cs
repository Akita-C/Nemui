using FluentAssertions;
using Nemui.Infrastructure.Services;

namespace Nemui.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_Should_Return_Non_Empty_Hash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password); // Hash should be different from original
    }

    [Fact]
    public void HashPassword_Should_Generate_Different_Hashes_For_Same_Password()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("VeryLongPassword123!")]
    public void HashPassword_Should_Handle_Various_Password_Lengths(string password)
    {
        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyPassword_Should_Return_True_For_Correct_Password()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_Should_Return_False_For_Wrong_Password()
    {
        // Arrange
        var correctPassword = "TestPassword123!";
        var wrongPassword = "WrongPassword456@";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Should_Return_False_For_Invalid_Hash()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid-hash";

        // Act
        var result = _passwordService.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "valid-hash")]
    [InlineData("password", "")]
    [InlineData("", "")]
    public void VerifyPassword_Should_Return_False_For_Empty_Inputs(string password, string hash)
    {
        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Password123!")] // Valid: uppercase, lowercase, digit, special
    [InlineData("MySecure@Pass1")] // Valid: all requirements met
    [InlineData("Strong$Password9")] // Valid: all requirements met
    public void IsPasswordStrong_Should_Return_True_For_Strong_Passwords(string password)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(" ")] // Whitespace
    [InlineData(null)] // Null
    [InlineData("short")] // Too short
    [InlineData("1234567")] // Too short (7 chars)
    public void IsPasswordStrong_Should_Return_False_For_Short_Or_Empty_Passwords(string password)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("password123!")] // Missing uppercase
    [InlineData("PASSWORD123!")] // Missing lowercase
    [InlineData("Password!")] // Missing digit
    [InlineData("Password123")] // Missing special character
    [InlineData("12345678!")] // Missing letters
    [InlineData("abcdefgh!")] // Missing uppercase and digit
    public void IsPasswordStrong_Should_Return_False_For_Weak_Passwords(string password)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Password123@")] // @ symbol
    [InlineData("Password123$")] // $ symbol
    [InlineData("Password123!")] // ! symbol
    [InlineData("Password123%")] // % symbol
    [InlineData("Password123*")] // * symbol
    [InlineData("Password123?")] // ? symbol
    [InlineData("Password123&")] // & symbol
    public void IsPasswordStrong_Should_Accept_Valid_Special_Characters(string password)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Password123#")] // # not in allowed special chars
    [InlineData("Password123+")] // + not in allowed special chars
    [InlineData("Password123=")] // = not in allowed special chars
    public void IsPasswordStrong_Should_Reject_Invalid_Special_Characters(string password)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_And_Verify_Should_Work_Together()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);
        var verifyResult = _passwordService.VerifyPassword(password, hash);

        // Assert
        verifyResult.Should().BeTrue();
    }

    [Fact]
    public void Strong_Password_Should_Hash_And_Verify_Successfully()
    {
        // Arrange
        var strongPassword = "VeryStrong@Password123";

        // Act
        var isStrong = _passwordService.IsPasswordStrong(strongPassword);
        var hash = _passwordService.HashPassword(strongPassword);
        var verifyResult = _passwordService.VerifyPassword(strongPassword, hash);

        // Assert
        isStrong.Should().BeTrue();
        verifyResult.Should().BeTrue();
    }
}