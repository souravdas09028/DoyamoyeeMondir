using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Identity;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Services;
public class UserAdministration(ApplicationDbContext db,UserManager<ApplicationUser> users)
{
    public Task Save(string actor,UserForm f) => Execute(actor,async () =>
    {
        if(f.Roles.Count==0 || f.Roles.Any(r=>!TempleRoles.Labels.ContainsKey(r))) throw new InvalidOperationException("বৈধ ভূমিকা নির্বাচন করুন।");
        var roles=f.Roles.Distinct().ToArray();
        var creating=string.IsNullOrEmpty(f.Id);
        var user=creating ? new ApplicationUser { UserName=f.Email.Trim(),Email=f.Email.Trim(),CreatedAt=DateTime.UtcNow } : await Existing(f.Id!,f.Stamp);
        var oldRoles=creating ? new List<string>() : (await users.GetRolesAsync(user)).ToList();
        if(!creating && actor==user.Id && (!f.IsActive || !roles.Contains("SuperAdmin"))) throw new InvalidOperationException("নিজের প্রধান প্রশাসকের প্রবেশাধিকার সরানো যাবে না।");
        if(oldRoles.Contains("SuperAdmin") && (!f.IsActive || !roles.Contains("SuperAdmin")))
        {
            var others=await users.GetUsersInRoleAsync("SuperAdmin");
            if(!others.Any(x=>x.Id!=user.Id && x.IsActive)) throw new InvalidOperationException("অন্তত একজন সক্রিয় প্রধান প্রশাসক রাখতে হবে।");
        }
        // Email is the login identifier; changing it is deliberately a separate future workflow.
        if(!creating && !string.Equals(user.Email,f.Email.Trim(),StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("বিদ্যমান লগইন ইমেইল পরিবর্তন করা যাবে না।");
        user.NameBn=f.NameBn.Trim(); user.IsActive=f.IsActive;
        if(creating) Check(await users.CreateAsync(user,f.Password!));
        else Check(await users.UpdateAsync(user));
        Check(await users.RemoveFromRolesAsync(user,oldRoles.Except(roles)));
        Check(await users.AddToRolesAsync(user,roles.Except(oldRoles)));
        Check(await users.UpdateSecurityStampAsync(user));
        db.UserAudits.Add(new UserAudit { UserId=user.Id,Action=$"{(creating?"অ্যাকাউন্ট তৈরি":"অ্যাকাউন্ট পরিবর্তন")}; সক্রিয়={user.IsActive}; ভূমিকা: {string.Join(",",oldRoles)} → {string.Join(",",roles)}" });
    });
    public Task Reset(string actor,ResetUserPasswordForm f) => Execute(actor,async () =>
    {
        var user=await Existing(f.Id,f.Stamp);
        if(user.Id==actor) throw new InvalidOperationException("নিজের পাসওয়ার্ড প্রোফাইলের পাসওয়ার্ড পরিবর্তন থেকে পরিবর্তন করুন।");
        Check(await users.ResetPasswordAsync(user,await users.GeneratePasswordResetTokenAsync(user),f.Password));
        Check(await users.SetLockoutEndDateAsync(user,null));
        Check(await users.ResetAccessFailedCountAsync(user));
        db.UserAudits.Add(new UserAudit { UserId=user.Id,Action="প্রশাসক পাসওয়ার্ড পুনর্নির্ধারণ করেছেন এবং সাময়িক লক খুলেছেন।" });
    });
    private async Task<ApplicationUser> Existing(string id,string? stamp)
    {
        var user=await users.FindByIdAsync(id) ?? throw new InvalidOperationException("ব্যবহারকারী পাওয়া যায়নি।");
        if(string.IsNullOrEmpty(stamp) || user.ConcurrencyStamp!=stamp) throw new InvalidOperationException("তথ্য পরিবর্তিত হয়েছে। তালিকা থেকে ফর্মটি আবার খুলুন।");
        return user;
    }
    private async Task Execute(string actor,Func<Task> change)
    {
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction=await db.Database.BeginTransactionAsync();
            // Serialize administrative mutations across all web processes, including last-admin checks.
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'TempleUserAdministration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'User administration is busy. Retry.', 1;");
            var administrator=await users.FindByIdAsync(actor);
            if(administrator is null || !administrator.IsActive || !await users.IsInRoleAsync(administrator,"SuperAdmin")) throw new InvalidOperationException("এই কাজের জন্য প্রধান প্রশাসকের অনুমতি প্রয়োজন।");
            await change();
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }
    private static void Check(IdentityResult result)
    {
        if(result.Succeeded) return;
        var messages=result.Errors.Select(e=>e.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName"=>"এই ইমেইল ইতিমধ্যে ব্যবহৃত হচ্ছে।",
            "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresUniqueChars"=>"পাসওয়ার্ডে অন্তত ৮ অক্ষর, ইংরেজি বড় ও ছোট অক্ষর এবং সংখ্যা ব্যবহার করুন।",
            "ConcurrencyFailure"=>"তথ্য পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।",
            _=>"তথ্য সংরক্ষণ করা যায়নি। ইমেইল, পাসওয়ার্ড ও ভূমিকা যাচাই করুন।"
        });
        throw new InvalidOperationException(string.Join(" ",messages.Distinct()));
    }
}
