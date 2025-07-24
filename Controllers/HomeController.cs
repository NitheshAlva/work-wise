using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using workwise.Models;
using workwise.ViewModels;
using workwise.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace workwise.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User);
        ViewBag.UserName = User.Identity?.Name;

        var viewModel = new DashboardViewModel();

        var allTasks = await _context.Tasks
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .ToListAsync();

        viewModel.TotalTasks = allTasks.Count;
        viewModel.CompletedTasks = allTasks.Count(t => t.IsCompleted);
        viewModel.PendingTasks = allTasks.Count(t => !t.IsCompleted);
        viewModel.OverdueTasks = allTasks.Count(t => !t.IsCompleted &&
                                                    t.DueDate.HasValue &&
                                                    t.DueDate < DateTime.Now);

        viewModel.CompletionPercentage = viewModel.TotalTasks > 0
            ? (double)viewModel.CompletedTasks / viewModel.TotalTasks * 100
            : 0;

        var today = DateTime.Today;
        viewModel.TodaysTasks = allTasks
            .Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.Priority)
            .ToList();

        viewModel.RecentCategories = await _context.Categories
            .Include(c => c.Tasks)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedDate)
            .Take(5)
            .ToListAsync();
        Console.WriteLine($"Recent Categories Count: {viewModel.RecentCategories.Count}");
        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
