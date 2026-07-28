using DoyamoyeeMondir.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoyamoyeeMondir.Infrastructure.Persistence.Seed;

public static class ApplicationDbContextSeeder
{
    private static readonly string[] Roles =
    [
        "SuperAdmin",
        "TempleAdmin",
        "President",
        "GeneralSecretary",
        "Treasurer",
        "Accountant",
        "InventoryManager",
        "DonationCollector",
        "DocumentManager",
        "Auditor",
        "Viewer"
    ];

    public static async Task InitialiseAsync(
        IServiceProvider serviceProvider)
    {
        await using var scope =
            serviceProvider.CreateAsyncScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            ", ",
                            result.Errors.Select(x => x.Description)));
                }
            }
        }

        const string adminEmail =
            "admin@doyamoyeemondir.local";

        var administrator =
            await userManager.FindByEmailAsync(adminEmail);

        if (administrator is null)
        {
            administrator = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NameBn = "সিস্টেম অ্যাডমিন",
                NameEn = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(
                administrator,
                "Admin@12345");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(
                administrator,
                "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(
                administrator,
                "SuperAdmin");
        }
    }
}