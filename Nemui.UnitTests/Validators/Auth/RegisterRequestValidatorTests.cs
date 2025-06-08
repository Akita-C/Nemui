using FluentAssertions;
using Nemui.Application.Validators.Auth;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.UnitTests.Validators.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator;

    public RegisterRequestValidatorTests()
    {
        _validator = new RegisterRequestValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_RegisterRequest()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Password = "ValidPass123!",
            PasswordConfirmation = "ValidPass123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var request = CreateValidRequest();
        request.Name = "";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Theory]
    [InlineData("A")] // Quá ngắn (1 ký tự)
    public void Should_Fail_When_Name_Is_Too_Short(string name)
    {
        var request = CreateValidRequest();
        request.Name = name;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        var request = CreateValidRequest();
        request.Name = new string('A', 101); // Quá dài (101 ký tự)

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Theory]
    [InlineData("John123")] // Có số
    [InlineData("John@Doe")] // Có ký tự đặc biệt
    [InlineData("John_Doe")] // Có underscore
    public void Should_Fail_When_Name_Contains_Invalid_Characters(string name)
    {
        var request = CreateValidRequest();
        request.Name = name;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Theory]
    [InlineData("John")] // Tên đơn
    [InlineData("John Doe")] // Tên có khoảng trắng
    [InlineData("Mary Jane Smith")] // Tên dài có khoảng trắng
    public void Should_Pass_When_Name_Is_Valid(string name)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = name;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var request = CreateValidRequest();
        request.Email = "";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid-email")] // Không có @
    [InlineData("@example.com")] // Thiếu local part
    [InlineData("user@")] // Thiếu domain
    public void Should_Fail_When_Email_Format_Is_Invalid(string email)
    {
        var request = CreateValidRequest();
        request.Email = email;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Too_Long()
    {
        var request = CreateValidRequest();
        var longEmail = new string('a', 250) + "@example.com"; // Quá dài
        request.Email = longEmail;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var request = CreateValidRequest();
        request.Password = "";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }

    [Theory]
    [InlineData("1234567")] // Quá ngắn (7 ký tự)
    [InlineData("Pass12!")] // Quá ngắn (7 ký tự)
    public void Should_Fail_When_Password_Is_Too_Short(string password)
    {
        var request = CreateValidRequest();
        request.Password = password;
        request.PasswordConfirmation = password;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Too_Long()
    {
        var request = CreateValidRequest();
        var longPassword = new string('A', 101) + "a1!"; // Quá dài (101+ ký tự)
        request.Password = longPassword;
        request.PasswordConfirmation = longPassword;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }

    [Theory]
    [InlineData("password123!")] // Thiếu chữ hoa
    [InlineData("PASSWORD123!")] // Thiếu chữ thường
    [InlineData("Password!")] // Thiếu số
    [InlineData("Password123")] // Thiếu ký tự đặc biệt
    [InlineData("12345678!")] // Thiếu chữ cái
    public void Should_Fail_When_Password_Does_Not_Meet_Complexity_Requirements(string password)
    {
        var request = CreateValidRequest();
        request.Password = password;
        request.PasswordConfirmation = password;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }

    [Theory]
    [InlineData("ValidPass123!")] // Đầy đủ yêu cầu
    [InlineData("MySecure@Pass1")] // Đầy đủ yêu cầu
    [InlineData("Strong$Password9")] // Đầy đủ yêu cầu
    public void Should_Pass_When_Password_Meets_All_Requirements(string password)
    {
        var request = CreateValidRequest();
        request.Password = password;
        request.PasswordConfirmation = password;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_PasswordConfirmation_Is_Empty()
    {
        var request = CreateValidRequest();
        request.PasswordConfirmation = "";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PasswordConfirmation");
    }

    [Fact]
    public void Should_Fail_When_Passwords_Do_Not_Match()
    {
        var request = CreateValidRequest();
        request.Password = "ValidPass123!";
        request.PasswordConfirmation = "DifferentPass456@";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PasswordConfirmation");
    }

    [Fact]
    public void Should_Pass_When_Passwords_Match()
    {
        var request = CreateValidRequest();
        var password = "ValidPass123!";
        request.Password = password;
        request.PasswordConfirmation = password;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    private static RegisterRequest CreateValidRequest()
    {
        return new RegisterRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Password = "ValidPass123!",
            PasswordConfirmation = "ValidPass123!"
        };
    }
}