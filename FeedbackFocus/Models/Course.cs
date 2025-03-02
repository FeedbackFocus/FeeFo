namespace FeedbackFocus.Models
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public int Year { get; set; } =DateTime.Now.Year;
        public Semester Semester { get; set; }
        public string CourseCode { get; set; }
        public virtual List<Assessment> Assessments { get; set; }

    }
    public enum Semester {
        Spring,
        Fall,
        Winter,
        Summer
    }
}
