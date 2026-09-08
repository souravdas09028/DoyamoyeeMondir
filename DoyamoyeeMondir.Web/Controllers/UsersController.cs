using System.Security.Claims;
using DoyamoyeeMondir.Infrastructure.Identity;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using DoyamoyeeMondir.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;
[Authorize(Roles="SuperAdmin")]
public class UsersController(ApplicationDbContext db,UserManager<ApplicationUser> users,UserAdministration administration) : Controller
{
    public async Task<IActionResult> Index(string? search,int page=1)
    {
        page=Math.Clamp(page,1,1000000); ViewBag.Page=page; ViewBag.Search=search;
        var query=db.Users.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search)) query=query.Where(x=>x.NameBn.Contains(search) || x.Email!.Contains(search));
        ViewBag.HasNext=await query.CountAsync()>page*30;
        var records=await query.OrderBy(x=>x.NameBn).ThenBy(x=>x.Id).Skip((page-1)*30).Take(30).ToListAsync();
        var rows=new List<UserListRow>();
        foreach(var u in records) rows.Add(new(u.Id,u.NameBn,u.Email??"",u.IsActive,u.LockoutEnabled && u.LockoutEnd>DateTimeOffset.UtcNow,(await users.GetRolesAsync(u)).ToList()));
        return View(rows);
    }
    public async Task<IActionResult> Edit(string? id)
    {
        if(id is null) return View(new UserForm());
        var u=await users.FindByIdAsync(id); if(u is null) return NotFound();
        return View(new UserForm { Id=u.Id,Stamp=u.ConcurrencyStamp,NameBn=u.NameBn,Email=u.Email??"",IsActive=u.IsActive,Roles=(await users.GetRolesAsync(u)).ToList() });
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserForm f)
    {
        if(ModelState.IsValid)
        {
            try { await administration.Save(User.FindFirstValue(ClaimTypes.NameIdentifier)!,f); TempData["Success"]="ব্যবহারকারীর তথ্য সংরক্ষিত হয়েছে।"; return RedirectToAction(nameof(Index)); }
            catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        }
        ClearPassword(nameof(f.Password)); f.Password=null; return View(f);
    }
    public async Task<IActionResult> ResetPassword(string id)
    {
        var u=await users.FindByIdAsync(id); if(u is null) return NotFound();
        ViewBag.Email=u.Email; return View(new ResetUserPasswordForm { Id=u.Id,Stamp=u.ConcurrencyStamp! });
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetUserPasswordForm f)
    {
        var u=await users.FindByIdAsync(f.Id); if(u is null) return NotFound(); ViewBag.Email=u.Email;
        if(ModelState.IsValid)
        {
            try { await administration.Reset(User.FindFirstValue(ClaimTypes.NameIdentifier)!,f); TempData["Success"]="নতুন পাসওয়ার্ড সংরক্ষিত হয়েছে।"; return RedirectToAction(nameof(Index)); }
            catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        }
        ClearPassword(nameof(f.Password)); ClearPassword(nameof(f.ConfirmPassword)); f.Password=""; f.ConfirmPassword=""; return View(f);
    }
    public IActionResult Roles() => View();
    private void ClearPassword(string field)
    {
        var errors=ModelState[field]?.Errors.Select(e=>e.ErrorMessage).ToArray() ?? [];
        ModelState.Remove(field);
        foreach(var error in errors) ModelState.AddModelError("",error);
    }
    public async Task<IActionResult> Audit(int page=1)
    {
        page=Math.Clamp(page,1,1000000); ViewBag.Page=page; ViewBag.HasNext=await db.UserAudits.CountAsync()>page*30;
        ViewBag.Names=await db.Users.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Email??x.NameBn);
        return View(await db.UserAudits.AsNoTracking().OrderByDescending(x=>x.Id).Skip((page-1)*30).Take(30).ToListAsync());
    }
}
