using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using workwise.Data;
using workwise.Models;

namespace workwise.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public CategoriesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var categories = await _context.Categories
            .Include(c => c.Tasks)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        return View(categories);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var category = await _context.Categories
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    public IActionResult Create()
    {
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Color")] Category category)
    {
        if (ModelState.IsValid)
        {
            var userId = _userManager.GetUserId(User);
            
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == category.Name.ToLower());
            
            if (existingCategory != null)
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
                return View(category);
            }

            category.UserId = userId!;
            category.CreatedDate = DateTime.UtcNow;

            _context.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        
        return View(category);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Color")] Category category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        
        if (existingCategory == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var duplicateCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == userId && 
                                        c.Name.ToLower() == category.Name.ToLower() && 
                                        c.Id != id);
            
            if (duplicateCategory != null)
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
                return View(category);
            }

            try
            {
                existingCategory.Name = category.Name;
                existingCategory.Color = category.Color;
                
                _context.Update(existingCategory);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        return View(category);
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var userId = _userManager.GetUserId(User);
    var category = await _context.Categories
        .Include(c => c.Tasks)
        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    if (category == null)
    {
        TempData["ErrorMessage"] = "Category not found.";
        return RedirectToAction(nameof(Index));
    }

    var taskCount = category.Tasks?.Count ?? 0;
    var categoryName = category.Name;

    if (category.Tasks != null && category.Tasks.Any())
    {
        foreach (var task in category.Tasks)
        {
            task.CategoryId = null;
        }
    }

    _context.Categories.Remove(category);
    await _context.SaveChangesAsync();

    if (taskCount > 0)
    {
        TempData["SuccessMessage"] = $"Category '{categoryName}' deleted. {taskCount} task{(taskCount != 1 ? "s" : "")} moved to uncategorized.";
    }
    else
    {
        TempData["SuccessMessage"] = $"Category '{categoryName}' deleted successfully.";
    }

    return RedirectToAction(nameof(Index));
}

    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.Id == id);
    }
}
