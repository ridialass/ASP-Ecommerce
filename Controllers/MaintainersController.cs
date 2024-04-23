﻿using ASP_Ecommerce.Models;
using ASP_Ecommerce.Models.Enums;
using ASP_Ecommerce.Models.ViewModels;
using ASP_Ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASP_Ecommerce.Controllers;

public class MaintainersController(ApplicationDbContext dbContext, UserManager<UserModel> userManager) : Controller
{
    public IActionResult Index(int? id)
    {
        if (id == null)
        {
            var maintainers = dbContext.Users
                .Where(u => u.MaintainerAddress != null)
                .AsNoTracking()
                .ToArray();
            
            return View(maintainers);
        }
        
        var user = dbContext.Users
            .Include(u => u.MaintainerProducts)
            .AsNoTracking()
            .FirstOrDefault(u => u.Id == id);

        if (user?.MaintainerAddress == null)
        {
            return NotFound();
        }
        
        // Remove circular reference so this object can be serialized and
        // passed to a Razor component.
        user.MaintainerProducts = user.MaintainerProducts.Select(p =>
        {
            p.Maintainer = null;
            return p;
        }).ToList();

        return View("Individual", user);
    }

    [Authorize]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public IActionResult Register([FromForm] BecomeMaintainerViewModel newMaintainerData)
    {
        if (!ModelState.IsValid)
        {
            return View(newMaintainerData);
        }
        
        // user can't be null because of the [Authorize] attribute
        var user = dbContext.Users.First(u => u.UserName == User.Identity!.Name);

        userManager.AddToRoleAsync(user, Enum.GetName(UserRole.Maintainer)!);
        user.MaintainerAddress = newMaintainerData.MaintainerAddress;
        user.City = newMaintainerData.City;
        user.PostalCode = newMaintainerData.PostalCode;
        user.Country = newMaintainerData.Country;
            
        dbContext.SaveChanges();
        
        return RedirectToAction("Index", new { id = user.Id });
    }
}