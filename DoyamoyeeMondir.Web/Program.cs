using System.Globalization;
using DoyamoyeeMondir.Application;
using DoyamoyeeMondir.Application.Interfaces.Identity;
using DoyamoyeeMondir.Infrastructure;
using DoyamoyeeMondir.Web.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<UserAdministration>();
builder.Services.Configure<Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
        await Microsoft.AspNetCore.Identity.SecurityStampValidator.ValidatePrincipalAsync(context);
        if(context.Principal?.Identity?.IsAuthenticated == true)
        {
            var manager=context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<DoyamoyeeMondir.Infrastructure.Identity.ApplicationUser>>();
            var user=await manager.GetUserAsync(context.Principal);
            if(user is null || !user.IsActive) context.RejectPrincipal();
        }
    };
});

var app = builder.Build();

await DoyamoyeeMondir.Infrastructure.Persistence.Seed
    .ApplicationDbContextSeeder.InitialiseAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

var supportedCultures = new[]
{
    new CultureInfo("bn-BD"),
    new CultureInfo("en-US")
};

app.UseRequestLocalization(
    new RequestLocalizationOptions
    {
        DefaultRequestCulture =
            new RequestCulture("bn-BD"),

        SupportedCultures = supportedCultures,

        SupportedUICultures = supportedCultures
    });

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
