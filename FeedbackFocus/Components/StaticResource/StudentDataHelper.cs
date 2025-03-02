using FeedbackFocus.Data;
using Microsoft.EntityFrameworkCore;

namespace FeedbackFocus.Components.StaticResource
{
    public class StudentDataHelper
    {
        private AnalysisContext _ctx;
        public static int Totalwords = 0;
        public StudentDataHelper(AnalysisContext ctx)
        {
            _ctx = ctx;
        }
        private DateTime _lastUpdated = DateTime.Now;
        private List<Student> _students = new();
        public string GetStudentFullName(string first, string last,string userIdentifier)
        {

            if (DateTime.Now.Subtract(_lastUpdated).TotalMinutes > 1 || _students.Count() == 0)
            {
                _students = _ctx.FeedbackItems
                    .Select(x => new Student { FirstName = x.FirstName, LastName = x.LastName, Username = x.Username })
                    .Distinct()
                    .ToList();
                _lastUpdated = DateTime.Now;
            }

            // Query the in-memory list instead of the database
            var student = _students.Where(x => x.FirstName == first && x.LastName == last);

            int count = student.Count();
            if (count == 0)
            {
                return "Unknown";
            }
            else if (count == 1)
            {
                return $"{first} {last}";
            }
            else
            {
                return $"{first} {last} ({userIdentifier})";
            }
        }
    }

    public class Student
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
    }
}
