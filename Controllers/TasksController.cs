using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using workwise.Data;
using workwise.Models;

namespace workwise.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public TasksController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string sortOrder, string searchString, int? categoryFilter, string statusFilter)
    {
        var userId = _userManager.GetUserId(User);
        
        ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
        ViewData["PrioritySortParm"] = sortOrder == "Priority" ? "priority_desc" : "Priority";
        ViewData["CurrentFilter"] = searchString;
        ViewData["CurrentCategory"] = categoryFilter;
        ViewData["CurrentStatus"] = statusFilter;

        var tasksQuery = _context.Tasks
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        // Apply filters
        if (!String.IsNullOrEmpty(searchString))
        {
            tasksQuery = tasksQuery.Where(t => t.Title.Contains(searchString) 
                || t.Description.Contains(searchString));
        }

        if (categoryFilter.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.CategoryId == categoryFilter);
        }

        if (!String.IsNullOrEmpty(statusFilter))
        {
            switch (statusFilter.ToLower())
            {
                case "completed":
                    tasksQuery = tasksQuery.Where(t => t.IsCompleted);
                    break;
                case "pending":
                    tasksQuery = tasksQuery.Where(t => !t.IsCompleted);
                    break;
                case "overdue":
                    tasksQuery = tasksQuery.Where(t => !t.IsCompleted && t.DueDate < DateTime.Today);
                    break;
            }
        }

        // Apply sorting
        switch (sortOrder)
        {
            case "title_desc":
                tasksQuery = tasksQuery.OrderByDescending(t => t.Title);
                break;
            case "Date":
                tasksQuery = tasksQuery.OrderBy(t => t.DueDate);
                break;
            case "date_desc":
                tasksQuery = tasksQuery.OrderByDescending(t => t.DueDate);
                break;
            case "Priority":
                tasksQuery = tasksQuery.OrderBy(t => t.Priority);
                break;
            case "priority_desc":
                tasksQuery = tasksQuery.OrderByDescending(t => t.Priority);
                break;
            default:
                tasksQuery = tasksQuery.OrderBy(t => t.Title);
                break;
        }

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();

        var tasks = await tasksQuery.ToListAsync();
        return View(tasks);
    }

    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);
        
        ViewBag.Categories = await GetCategoriesSelectList(userId!);
        ViewBag.Priorities = GetPrioritiesSelectList();
        
        var task = new TaskItem
        {
            DueDate = DateTime.Today.AddDays(1).ToUniversalTime() 
        };
        
        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItem task)
    {
        var userId = _userManager.GetUserId(User)!;
        
        if (ModelState.IsValid)
        {
            task.UserId = userId;
            task.CreatedDate = DateTime.UtcNow;
            task.IsCompleted = false;
            task.DueDate = task.DueDate?.ToUniversalTime();
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Task created successfully!";
            return RedirectToAction(nameof(Index));
        }
        // Console.WriteLine("Model state is invalid: " + ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault());

        ViewBag.Categories = await GetCategoriesSelectList(userId);
        ViewBag.Priorities = GetPrioritiesSelectList();
        return View(task);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User)!;
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        
        if (task == null)
        {
            return NotFound();
        }

        ViewBag.Categories = await GetCategoriesSelectList(userId, task.CategoryId);
        ViewBag.Priorities = GetPrioritiesSelectList(task.Priority);
        
        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskItem task)
    {
        if (id != task.Id)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User)!;
        var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        
        if (existingTask == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.DueDate = task.DueDate?.ToUniversalTime();
                existingTask.Priority = task.Priority;
                existingTask.CategoryId = task.CategoryId;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Task updated successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(task.Id, userId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await GetCategoriesSelectList(userId, task.CategoryId);
        ViewBag.Priorities = GetPrioritiesSelectList(task.Priority);
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = _userManager.GetUserId(User);
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return Json(new { success = false, message = "Task not found" });
        }

        task.IsCompleted = !task.IsCompleted;
        await _context.SaveChangesAsync();

        return Json(new { 
            success = true, 
            isCompleted = task.IsCompleted,
            message = task.IsCompleted ? "Task marked as completed!" : "Task marked as pending!"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Task deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var task = await _context.Tasks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        return View(task);
    }

    private async Task<SelectList> GetCategoriesSelectList(string userId, int? selectedValue = null)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var categoryList = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        }).ToList();

        categoryList.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "-- No Category --"
        });

        return new SelectList(categoryList, "Value", "Text", selectedValue?.ToString());
    }

    private SelectList GetPrioritiesSelectList(Priority? selectedValue = null)
    {
        var priorities = Enum.GetValues(typeof(Priority))
            .Cast<Priority>()
            .Select(p => new SelectListItem
            {
                Value = ((int)p).ToString(),
                Text = p.ToString()
            });

        return new SelectList(priorities, "Value", "Text", selectedValue?.ToString());
    }

    private bool TaskExists(int id, string userId)
    {
        return _context.Tasks.Any(e => e.Id == id && e.UserId == userId);
    }
}
