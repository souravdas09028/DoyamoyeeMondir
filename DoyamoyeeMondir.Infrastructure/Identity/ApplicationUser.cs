using Microsoft.AspNetCore.Identity;

namespace DoyamoyeeMondir.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string NameBn { get; set; } = string.Empty;

    public string? NameEn { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? ProfilePicturePath { get; set; }
}