using workwise.Models;
using workwise.Data;
using Microsoft.EntityFrameworkCore;

namespace workwise.Services;
public interface IRecurringTaskService
{
    Task<List<RecurringTaskTemplate>> GetUserTemplatesAsync(string userId);
    Task<RecurringTaskTemplate?> GetTemplateByIdAsync(int id, string userId);
    Task CreateTemplateAsync(RecurringTaskTemplate template);
    Task UpdateTemplateAsync(RecurringTaskTemplate template);
    Task DeleteTemplateAsync(int id, string userId);
    Task ToggleTemplateStatusAsync(int id, string userId);
    Task GenerateTasksForAllActiveTemplatesAsync();
    Task GenerateTasksForTemplateAsync(RecurringTaskTemplate template);
}

public class RecurringTaskService : IRecurringTaskService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RecurringTaskService> _logger;

    public RecurringTaskService(ApplicationDbContext context, ILogger<RecurringTaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateTemplateAsync(RecurringTaskTemplate template)
    {
        template.CreatedDate = DateTime.UtcNow;
        template.StartDate = DateTime.SpecifyKind(template.StartDate, DateTimeKind.Utc);
        template.NextDueDate = DateTime.SpecifyKind(template.StartDate, DateTimeKind.Utc);
        
        if (template.EndDate.HasValue)
        {
            template.EndDate = DateTime.SpecifyKind(template.EndDate.Value, DateTimeKind.Utc);
        }
        
        _context.RecurringTaskTemplates.Add(template);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created recurring task template: {Title} for user: {UserId}", 
            template.Title, template.UserId);
    }

    public async Task UpdateTemplateAsync(RecurringTaskTemplate template)
    {
        if (template.EndDate.HasValue)
        {
            template.EndDate = DateTime.SpecifyKind(template.EndDate.Value, DateTimeKind.Utc);
        }
        
        _context.RecurringTaskTemplates.Update(template);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated recurring task template: {Id} for user: {UserId}", 
            template.Id, template.UserId);
    }

    public async Task GenerateTasksForTemplateAsync(RecurringTaskTemplate template)
    {
        var today = DateTime.UtcNow.Date;
        var tasksGenerated = 0;

        while (template.NextDueDate.Date <= today && ShouldContinueGenerating(template))
        {
            var task = new TaskItem
            {
                UserId = template.UserId,
                Title = template.Title,
                Description = template.Description,
                DueDate = DateTime.SpecifyKind(template.NextDueDate, DateTimeKind.Utc),
                Priority = template.Priority,
                CategoryId = template.CategoryId,
                IsCompleted = false,
                CreatedDate = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            tasksGenerated++;

            template.LastGeneratedDate = DateTime.SpecifyKind(template.NextDueDate, DateTimeKind.Utc);
            template.NextDueDate = DateTime.SpecifyKind(
                CalculateNextDueDate(template.NextDueDate, template), 
                DateTimeKind.Utc);

            // Check if we should stop generating
            if (!ShouldContinueGenerating(template))
            {
                template.IsActive = false;
                break;
            }
        }

        if (tasksGenerated > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Generated {Count} tasks for template: {Title}", 
                tasksGenerated, template.Title);
        }
    }

    private bool ShouldContinueGenerating(RecurringTaskTemplate template)
    {
        // Check end date
        if (template.EndDate.HasValue && template.NextDueDate.Date > template.EndDate.Value.Date)
            return false;

        // Check max occurrences
        if (template.MaxOccurrences.HasValue)
        {
            var generatedCount = GetGeneratedTaskCount(template);
            return generatedCount < template.MaxOccurrences.Value;
        }

        return true;
    }

    private int GetGeneratedTaskCount(RecurringTaskTemplate template)
    {
        return _context.Tasks.Count(t => 
            t.UserId == template.UserId && 
            t.Title == template.Title &&
            t.CreatedDate >= template.CreatedDate);
    }

    private DateTime CalculateNextDueDate(DateTime currentDate, RecurringTaskTemplate template)
    {
        var nextDate = template.RecurrenceType switch
        {
            RecurrenceType.Daily => currentDate.AddDays(template.RecurrenceInterval),
            RecurrenceType.Weekly => CalculateNextWeeklyDate(currentDate, template),
            RecurrenceType.Monthly => currentDate.AddMonths(template.RecurrenceInterval),
            RecurrenceType.Yearly => currentDate.AddYears(template.RecurrenceInterval),
            _ => currentDate.AddDays(1)
        };

        // Ensure the calculated date is UTC
        return DateTime.SpecifyKind(nextDate, DateTimeKind.Utc);
    }

    private DateTime CalculateNextWeeklyDate(DateTime currentDate, RecurringTaskTemplate template)
    {
        if (string.IsNullOrEmpty(template.DaysOfWeek))
        {
            return currentDate.AddDays(7 * template.RecurrenceInterval);
        }

        var daysOfWeek = template.DaysOfWeek
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .OrderBy(d => d)
            .ToList();

        var currentDayOfWeek = (int)currentDate.DayOfWeek;
        
        // Find next day in current week
        var nextDay = daysOfWeek.FirstOrDefault(d => d > currentDayOfWeek);

        if (nextDay > 0)
        {
            // Found a day later this week
            return currentDate.AddDays(nextDay - currentDayOfWeek);
        }
        else
        {
            // Go to first day of next interval week
            var daysToAdd = (7 * template.RecurrenceInterval) - currentDayOfWeek + daysOfWeek.First();
            return currentDate.AddDays(daysToAdd);
        }
    }

    public async Task<List<RecurringTaskTemplate>> GetUserTemplatesAsync(string userId)
    {
        return await _context.RecurringTaskTemplates
            .Include(r => r.Category)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Title)
            .ToListAsync();
    }

    public async Task<RecurringTaskTemplate?> GetTemplateByIdAsync(int id, string userId)
    {
        return await _context.RecurringTaskTemplates
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task DeleteTemplateAsync(int id, string userId)
    {
        var template = await GetTemplateByIdAsync(id, userId);
        if (template != null)
        {
            _context.RecurringTaskTemplates.Remove(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted recurring task template: {Id} for user: {UserId}", 
                id, userId);
        }
    }

    public async Task ToggleTemplateStatusAsync(int id, string userId)
    {
        var template = await GetTemplateByIdAsync(id, userId);
        if (template != null)
        {
            template.IsActive = !template.IsActive;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Toggled template {Id} status to: {IsActive}", 
                id, template.IsActive);
        }
    }

    public async Task GenerateTasksForAllActiveTemplatesAsync()
    {
        var today = DateTime.UtcNow.Date;
        
        var templatesToProcess = await _context.RecurringTaskTemplates
            .Where(r => r.IsActive && r.NextDueDate.Date <= today)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} recurring templates", templatesToProcess.Count);

        foreach (var template in templatesToProcess)
        {
            try
            {
                await GenerateTasksForTemplateAsync(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template {TemplateId}", template.Id);
            }
        }
    }
}
