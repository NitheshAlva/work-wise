namespace workwise.Models;

using Microsoft.AspNetCore.Mvc.ModelBinding;
public class Category
{
    public int Id { get; set; }
    [BindNever]
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public ICollection<TaskItem>? Tasks { get; set; }
}