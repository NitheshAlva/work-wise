using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace workwise.Models;

public class TaskItem
{
    public int Id { get; set; }
    [BindNever]
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; }
    public int? CategoryId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public Category? Category { get; set; }
}