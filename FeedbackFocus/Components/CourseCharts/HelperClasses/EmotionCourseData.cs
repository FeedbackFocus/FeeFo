namespace FeedbackFocus.Components.CourseCharts.HelperClasses
{
    public class EmotionCourseData
    {
        public string Course { get; set; }
        public decimal Joy { get; set; }
        public decimal Surprise { get; set; }
        public decimal Neutral { get; set; }
        public decimal Sadness { get; set; }
        public decimal Anger { get; set; }
        public decimal Disgust { get; set; }
        public decimal Fear { get; set; }
    }
    public class EmotionOverallCourseData
    {
        public string Emotion { get; set; }
        public decimal Value { get; set; }

    }
    public class GradeDistribution
    {
        public string Bucket { get; set; }
        public decimal Value { get; set; }

    }

    public class YearlyCourseGrades
    {
        public int Year { get; set; }
        public string Course { get; set; }
        public decimal Grade { get; set; }

    }
    public class YearlyCourseFeedbackWords
    {
        public int Year { get; set; }
        public string Course { get; set; }
        public int WordCount { get; set; }

    }
}
