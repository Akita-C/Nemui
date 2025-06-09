namespace Nemui.Shared.Constants;

public static class DatabaseConstants
{
    public static class TableNames
    {
        public const string Users = "Users";
        public const string RefreshTokens = "RefreshTokens";
    }

    public static class FieldLengths
    {
        public const int EmailMaxLength = 255;
        public const int NameMaxLength = 255;
        public const int RoleMaxLength = 50;
        public const int TokenMaxLength = 500;
        public const int CloudinaryPublicIdMaxLength = 200;
        public const int UrlMaxLength = 2000;
    }

    public static class Indexes
    {
        public const string UserEmailUniqueIndex = "IX_Users_Email_Unique";
        public const string UserIsDeletedIndex = "IX_Users_IsDeleted";
        public const string UserCreatedAtIndex = "IX_Users_CreatedAt";
        public const string RefreshTokenTokenUniqueIndex = "IX_RefreshTokens_Token_Unique";
        public const string RefreshTokenUserIdIndex = "IX_RefreshTokens_UserId";
        public const string RefreshTokenExpiresAtIndex = "IX_RefreshTokens_ExpiresAt";
    }

    public static class Filters
    {
        public const string SoftDeleteFilter = "\"IsDeleted\" = false";
    }
}