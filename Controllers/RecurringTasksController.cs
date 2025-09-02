using workwise.Models;
using workwise.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using workwise.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace workwise.Controllers;
    [Authorize]
public class RecurringTasksController : Controller
{
    private readonly IRecurringTaskService _recurringTaskService;
    private readonly ApplicationDbContext _context;

    public RecurringTasksController(
        IRecurringTaskService recurringTaskService,
        ApplicationDbContext context)
    {
        _recurringTaskService = recurringTaskService;
        _context = context;
    }

    public async Task<IActionResult> Create()
    {
        await LoadCategoriesForView();
        var template = new RecurringTaskTemplate
        {
            StartDate = DateTime.Today, 
            RecurrenceInterval = 1
        };
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecurringTaskTemplate template)
    {
        if (ModelState.IsValid)
        {
            template.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            template.StartDate = DateTime.SpecifyKind(template.StartDate.Date, DateTimeKind.Utc);
            if (template.EndDate.HasValue)
            {
                template.EndDate = DateTime.SpecifyKind(template.EndDate.Value.Date, DateTimeKind.Utc);
            }

            await _recurringTaskService.CreateTemplateAsync(template);
            
            TempData["Success"] = "Recurring task template created successfully!";
            return RedirectToAction(nameof(Index));
        }

        await LoadCategoriesForView();
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RecurringTaskTemplate template)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existingTemplate = await _recurringTaskService.GetTemplateByIdAsync(template.Id, userId);
            
            if (existingTemplate == null)
            {
                return NotFound();
            }

            existingTemplate.Title = template.Title;
            existingTemplate.Description = template.Description;
            existingTemplate.Priority = template.Priority;
            existingTemplate.CategoryId = template.CategoryId;
            existingTemplate.RecurrenceType = template.RecurrenceType;
            existingTemplate.RecurrenceInterval = template.RecurrenceInterval;
            existingTemplate.DaysOfWeek = template.DaysOfWeek;
            existingTemplate.MaxOccurrences = template.MaxOccurrences;

            if (template.EndDate.HasValue)
            {
                existingTemplate.EndDate = DateTime.SpecifyKind(template.EndDate.Value.Date, DateTimeKind.Utc);
            }
            else
            {
                existingTemplate.EndDate = null;
            }

            await _recurringTaskService.UpdateTemplateAsync(existingTemplate);
            
            TempData["Success"] = "Recurring task template updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        await LoadCategoriesForView();
        return View(template);
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var templates = await _recurringTaskService.GetUserTemplatesAsync(userId);
        return View(templates);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var template = await _recurringTaskService.GetTemplateByIdAsync(id, userId);
        
        if (template == null)
        {
            return NotFound();
        }

        await LoadCategoriesForView();
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _recurringTaskService.ToggleTemplateStatusAsync(id, userId);
        
        TempData["Success"] = "Template status updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _recurringTaskService.DeleteTemplateAsync(id, userId);
        
        TempData["Success"] = "Recurring task template deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateNow(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var template = await _recurringTaskService.GetTemplateByIdAsync(id, userId);
        
        if (template != null && template.IsActive)
        {
            await _recurringTaskService.GenerateTasksForTemplateAsync(template);
            TempData["Success"] = "Tasks generated successfully!";
        }
        else
        {
            TempData["Error"] = "Template not found or inactive.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesForView()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        ViewBag.Categories = new SelectList(categories, "Id", "Name");
    }
}