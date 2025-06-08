using FluentAssertions;
using Nemui.Application.Validators.Auth;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.UnitTests.Validators.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_LoginRequest()
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123"
        };
        
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var request = new LoginRequest
        {
            Email = "", // Email rỗng
            Password = "ValidPassword123"
        };
        
        var result = _validator.Validate(request);
        
        result.IsValid.Should().BeFalse();
        result.Errors[0].PropertyName.Should().Be("Email");
    }
    
    [Fact]
    public void Should_Fail_When_Email_Is_Invalid_Format()
    {
        var request = new LoginRequest
        {
            Email = "invalid-email", // Email sai format
            Password = "ValidPassword123"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
    }
    
    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "" // Password rỗng
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }
    
    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Should_Fail_When_Password_Is_Too_Short(string password)
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = password
        };
        
        var result = _validator.Validate(request);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }
}