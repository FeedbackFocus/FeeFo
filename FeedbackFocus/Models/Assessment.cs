using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackFocus.Models
{
    public class Assessment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [ForeignKey("Course")]
        public Guid CourseId { get; set; }
        public string Name { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime DueDate { get; set; } = DateTime.Now;
        public virtual List<FeedbackItem> Feedback { get; set; } = new();
    }
}
