using FeedbackFocus.Models;
using System.Text.Json;

namespace FeedbackFocus.Services
{
    public class FilterService
    {
        //Properties to represent every available option
        public List<string> Courses { get; set; }
        public List<string> Assessments { get; set; }
        public List<Semester> Semesters { get; set; }
        public List<string> Students { get; set; }
        public List<int> Years { get; set; }


        CourseService _courseService;
        private List<Course> _masterData;

        public List<Course> MasterData { get
            {
                return DeepCopy<List<Course>>(_masterData);
            }
        }
        public FilterService(CourseService crs)
        {
            _courseService = crs;
            //_masterData = crs.GetCourses().Result;
        }

        public async Task InitializeAsync()
        {

            _masterData = await _courseService.GetCourses();
            Courses = new();
            Semesters = new();
            Assessments = new();
            Students = new();
            Years = new();

            foreach (var course in _masterData)
            {
                if (!Courses.Contains(course.Name))
                    Courses.Add(course.Name);
                if (!Years.Contains(course.Year))
                    Years.Add(course.Year);
                foreach (var assessment in course.Assessments)
                {
                    if (!Assessments.Contains(assessment.Name))
                        Assessments.Add(assessment.Name);
                    if (!Semesters.Contains(course.Semester))
                        Semesters.Add(course.Semester);
                    foreach (var feedback in assessment.Feedback)
                    {
                        if (!Students.Contains(feedback.FirstName + " " + feedback.LastName))
                        {
                            Students.Add(feedback.FirstName + " " + feedback.LastName);
                        }
                    }
                }

            }

        }
        public List<string> CoursesExcluded = new();
        public List<int> CourseYearsExcluded = new();
        public List<Semester> CourseSemestersExcluded = new();

        public List<string> AssessmentNameExcluded = new();
        public List<string> AssessmentCourseExcluded = new();
        public List<int> AssessmentYearExcluded = new();
        public List<Semester> AssessmentSemesterExcluded = new();

        public List<string> StudentNameExcluded = new();
        public List<string> StudentCourseExcluded = new();
        public List<int> StudentYearExcluded = new();
        public List<Semester> StudentSemesterExcluded = new();



        public static T DeepCopy<T>(T obj)
        {
            var options = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve, WriteIndented = true };
            var serialized = JsonSerializer.Serialize(obj, options);
            return JsonSerializer.Deserialize<T>(serialized, options);
        }
    }
}
