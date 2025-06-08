using FluentAssertions;
using Nemui.Application.Validators.Auth;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.UnitTests.Validators.Auth;

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator;

    public RefreshTokenRequestValidatorTests()
    {
        _validator = new RefreshTokenRequestValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_RefreshTokenRequest()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token-here"
        };
        
        var result = _validator.Validate(request);
        
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData(" ")] // Whitespace
    [InlineData(null)] // Null
    public void Should_Fail_When_RefreshToken_Is_Empty_Or_Null(string refreshToken)
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };
        
        var result = _validator.Validate(request);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "RefreshToken");
        result.Errors.Should().Contain(x => x.ErrorMessage == "Refresh token is required");
    }

    [Fact]
    public void Should_Fail_When_RefreshToken_Is_Too_Long()
    {
        var longToken = new string('a', 501); // 501 characters (exceeds 500 limit)
        var request = new RefreshTokenRequest
        {
            RefreshToken = longToken
        };
        
        var result = _validator.Validate(request);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "RefreshToken");
        result.Errors.Should().Contain(x => x.ErrorMessage == "Invalid refresh token format");
    }

    [Theory]
    [InlineData("short-token")] // Valid short token
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9")] // JWT-like token
    public void Should_Pass_When_RefreshToken_Is_Valid_Length(string refreshToken)
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_RefreshToken_Is_Maximum_Length()
    {
        var maxLengthToken = new string('a', 500); // Exactly 500 characters
        var request = new RefreshTokenRequest
        {
            RefreshToken = maxLengthToken
        };
        
        var result = _validator.Validate(request);
        
        result.IsValid.Should().BeTrue();
    }
}