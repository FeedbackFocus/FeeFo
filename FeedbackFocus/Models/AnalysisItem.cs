using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackFocus.Models
{
    public class AnalysisItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [ForeignKey("FeedbackItem")]
        public Guid FeedbackItemId { get; set; }
        public string AnalysisType { get; set; }
        public string Label { get; set; }
        public decimal Value { get; set; }
    }
}
