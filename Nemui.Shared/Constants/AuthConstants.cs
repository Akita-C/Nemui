namespace Nemui.Shared.Constants;

public static class AuthConstants
{
    public static class Roles
    {
        public const string User = "User";
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
    }
    
    public static class Security
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutMinutes = 30;
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 100;
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;
        public const int EmailMaxLength = 255;
        public const int TokenMaxLength = 500;
        public const int RefreshTokenByteSize = 64;
        public const int ExpiredTokenCleanupDays = 30;
        public const int BcryptSaltRounds = 12;
    }
    
    public static class ValidationMessages
    {
        public const string EmailRequired = "Email is required";
        public const string EmailInvalidFormat = "Invalid email format";
        public const string EmailTooLong = "Email cannot exceed {0} characters";
        public const string PasswordRequired = "Password is required";
        public const string PasswordTooShort = "Password must be at least {0} characters";
        public const string PasswordTooLong = "Password cannot exceed {0} characters";
        public const string PasswordComplexity = "Password must contain at least one uppercase letter, one lowercase letter, one digit and one special character";
        public const string NameRequired = "Name is required";
        public const string NameTooShort = "Name must be at least {0} characters";
        public const string NameTooLong = "Name cannot exceed {0} characters";
        public const string NameInvalidCharacters = "Name can only contain letters and spaces";
        public const string PasswordConfirmationRequired = "Password confirmation is required";
        public const string PasswordsDoNotMatch = "Passwords do not match";
        public const string RefreshTokenRequired = "Refresh token is required";
        public const string InvalidRefreshToken = "Invalid refresh token format";
    }
    
    public static class ErrorMessages
    {
        public const string InvalidCredentials = "Invalid email or password";
        public const string AccountLocked = "Account is locked until {0} UTC";
        public const string AccountDeactivated = "Account is deactivated";
        public const string AccountLockedTooManyAttempts = "Account locked due to too many failed login attempts";
        public const string UserAlreadyExists = "User with this email already exists";
        public const string InvalidRefreshToken = "Invalid or expired refresh token";
        public const string UserNotActive = "User account is not active";
        public const string CurrentPasswordIncorrect = "Current password is incorrect";
        public const string NewPasswordsDoNotMatch = "New passwords do not match";
        public const string PasswordSecurityRequirements = "New password does not meet security requirements";
    }
    
    public static class ClaimTypes
    {
        public const string EmailVerified = "email_verified";
        public const string JwtId = "jti";
    }
}