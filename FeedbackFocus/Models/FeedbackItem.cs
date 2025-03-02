using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackFocus.Models
{
    public class FeedbackItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [ForeignKey("Assessment")]
        public Guid AssessmentId { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string Username { get; set; }
        public decimal Grade { get; set; }
        //public string GradingNotes { get; set; }
        //public string NotesFormat { get; set; }
        public string FeedbackToLearner { get; set; }
        //public string FeedbackFormat { get; set; }
        public List<AnalysisItem> Analysis { get; set; } = new();

    }
}
