using FluentValidation;
using Microsoft.Extensions.Options;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Repositories;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IJwtBlacklistService _jwtBlacklistService;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly JwtSettings _jwtSettings;
    private readonly AuthSettings _authSettings;
    private readonly IUserCacheService _userCacheService;
    private readonly IImageService _imageService;
    
    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordService passwordService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IOptions<JwtSettings> jwtSettings,
        IOptions<AuthSettings> authSettings, 
        IUserCacheService userCacheService,
        IJwtBlacklistService jwtBlacklistService, IImageService imageService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _jwtBlacklistService = jwtBlacklistService;
        _imageService = imageService;
        _passwordService = passwordService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _userCacheService = userCacheService;
        _jwtBlacklistService = jwtBlacklistService;
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _authSettings = authSettings.Value ?? throw new ArgumentNullException(nameof(authSettings));
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null) throw new UnauthorizedAccessException("Invalid email or password");

        // Check if user is locked out
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new UnauthorizedAccessException($"Account is locked until {user.LockoutEnd:yyyy-MM-dd HH:mm:ss} UTC");
    
        // Check if user is active
        if (!user.IsActive) throw new UnauthorizedAccessException("Account is deactivated");
    
        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            await _unitOfWork.Users.IncrementFailedLoginAttemptsAsync(user.Id, cancellationToken);
    
            // Lock account if too many failed attempts
            if (user.FailedLoginAttempts >= _authSettings.MaxFailedLoginAttempts)
            {
                var lockoutEnd = DateTime.UtcNow.AddMinutes(_authSettings.LockoutMinutes);
                await _unitOfWork.Users.SetLockoutAsync(user.Id, lockoutEnd, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedAccessException("Account locked due to many failed attempts");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Reset failed login attempts on successful login
        if (user.FailedLoginAttempts > 0)
        {
            await _unitOfWork.Users.ResetFailedLoginAttemptsAsync(user.Id, cancellationToken);
        }
    
        // Update last login
        await _unitOfWork.Users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, cancellationToken);
    
        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
    
        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userProfile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt.DateTime,
            LastLoginAt = user.LastLoginAt,
            AvatarUrl = user.AvatarUrl
        };
        
        if (!string.IsNullOrEmpty(user.AvatarPublicId))
        {
            userProfile.AvatarTransformations = new Dictionary<string, string>
            {
                ["small"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId,
                    "c_fill,w_150,h_150,q_auto,f_auto", cancellationToken),
                ["medium"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId,
                    "c_fill,w_300,h_300,q_auto,f_auto", cancellationToken),
                ["large"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId,
                    "c_fill,w_500,h_500,q_auto,f_auto", cancellationToken)
            };
        }
        
        await _userCacheService.SetUserProfileAsync(userProfile.Id, userProfile, cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _jwtService.GetTokenExpirationTime(),
            User = userProfile
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null) throw new InvalidOperationException("User with this email already exists");

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Role = "User",
            IsEmailVerified = false,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        
        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _jwtService.GetTokenExpirationTime(),
            User = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt.DateTime,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;
        
        var tokenEntity = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (tokenEntity?.User != null) await _jwtBlacklistService.BlacklistAllUserTokensAsync(tokenEntity.User.Id, cancellationToken);
        
        await _unitOfWork.RefreshTokens.RevokeTokenAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new UnauthorizedAccessException("Invalid refresh token");
        
        var tokenEntity = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (tokenEntity == null || !tokenEntity.IsActive()) throw new UnauthorizedAccessException("Invalid or expired refresh token");
        
        var user = tokenEntity.User;
        if (user == null || !user.IsActive) throw new UnauthorizedAccessException("User account is not active");
        
        // Revoke old refresh token
        await _unitOfWork.RefreshTokens.RevokeTokenAsync(refreshToken, cancellationToken);
        
        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _jwtService.GetTokenExpirationTime(),
            User = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt.DateTime,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var tokenEntity = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (tokenEntity?.User != null) await _jwtBlacklistService.BlacklistAllUserTokensAsync(tokenEntity.User.Id, cancellationToken);
        
        await _unitOfWork.RefreshTokens.RevokeTokenAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _jwtBlacklistService.BlacklistAllUserTokensAsync(userId, cancellationToken);
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
    
    public async Task<bool> BlacklistAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return false;

        await _jwtBlacklistService.BlacklistTokenAsync(accessToken, cancellationToken);
        return true;
    }
}