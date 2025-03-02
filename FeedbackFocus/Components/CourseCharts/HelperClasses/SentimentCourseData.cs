namespace FeedbackFocus.Components.CourseCharts.HelperClasses
{
    public class SentimentCourseData
    {
        public string Course { get; set; }
        public decimal Positive { get; set; }
        public decimal Neutral { get; set; }
        public decimal Negative { get; set; }
        public int PositiveCount { get; set; }
        public int NeutralCount { get; set; }
        public int NegativeCount { get; set; }
    }
}
