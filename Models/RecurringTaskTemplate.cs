using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace workwise.Models
{
    public class RecurringTaskTemplate
    {
        public int Id { get; set; }
        [BindNever]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = null!;        
        public string? Description { get; set; } = string.Empty;        
        public Priority Priority { get; set; } = Priority.Medium;       
        public int? CategoryId { get; set; }       
        [Required]
        public RecurrenceType RecurrenceType { get; set; }
        public int RecurrenceInterval { get; set; } = 1; 
        public string? DaysOfWeek { get; set; } 
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime NextDueDate { get; set; }
        public DateTime CreatedDate { get; set; }

        public Category? Category { get; set; }
    }

    public enum RecurrenceType
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4
    }
}