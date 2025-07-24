using workwise.Models;

namespace workwise.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionPercentage { get; set; }
        public List<TaskItem> TodaysTasks { get; set; } = new List<TaskItem>();
        public List<Category> RecentCategories { get; set; } = new List<Category>();
    }
}