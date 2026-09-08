using System.ComponentModel.DataAnnotations;
using DoyamoyeeMondir.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoyamoyeeMondir.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; set; } = "/";
    public class InputModel
    {
        [Required(ErrorMessage = "ইমেইল ঠিকানা লিখুন।")]
        [EmailAddress(ErrorMessage = "সঠিক ইমেইল ঠিকানা লিখুন।")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "পাসওয়ার্ড লিখুন।"), DataType(DataType.Password)]
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }
    public void OnGet(string? returnUrl = null) => ReturnUrl = SafeReturnUrl(returnUrl);
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (!ModelState.IsValid) return Page();
        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        var result = user is { IsActive: true }
            ? await signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true)
            : Microsoft.AspNetCore.Identity.SignInResult.Failed;
        if (result.Succeeded) return LocalRedirect(ReturnUrl);
        if (result.RequiresTwoFactor) return RedirectToPage("./LoginWith2fa", new { ReturnUrl, Input.RememberMe });
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "বারবার ভুল পাসওয়ার্ড দেওয়ায় প্রবেশ সাময়িকভাবে বন্ধ। কিছুক্ষণ পরে আবার চেষ্টা করুন।"
            : "প্রবেশ করা যায়নি। ইমেইল ও পাসওয়ার্ড যাচাই করুন অথবা প্রশাসকের সঙ্গে যোগাযোগ করুন।");
        return Page();
    }
    private string SafeReturnUrl(string? value) => Url.IsLocalUrl(value) ? value! : Url.Content("~/");
}
