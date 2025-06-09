namespace Nemui.Infrastructure.Configurations;

public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public CloudinaryFolders Folders { get; set; } = new();
    public CloudinaryTransformations Transformations { get; set; } = new();
}

public class CloudinaryFolders
{
    public string UserAvatars { get; set; } = "akita/users/avatars";
    public string General { get; set; } = "akita/general";
}

public class CloudinaryTransformations
{
    public string AvatarSmall { get; set; } = "c_fill,w_150,h_150,q_auto,f_auto";
    public string AvatarMedium { get; set; } = "c_fill,w_300,h_300,q_auto,f_auto";
    public string AvatarLarge { get; set; } = "c_fill,w_500,h_500,q_auto,f_auto";
    public string DocumentThumbnail { get; set; } = "c_fit,w_200,h_200,q_auto,f_auto";
}