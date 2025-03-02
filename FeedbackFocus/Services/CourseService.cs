using FeedbackFocus.Components.CourseCharts.HelperClasses;
using FeedbackFocus.Data;
using FeedbackFocus.Models;
using FeedbackFocus.Pages;
using Microsoft.EntityFrameworkCore;
using SqliteWasmHelper;


namespace FeedbackFocus.Services
{
    public class CourseService
    {
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        public CourseService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory) =>
            _dbFactory = dbFactory;
        List<Course> courseCollection;
        public async Task<List<Course>> BackupDatabaseToJSON()
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return ctx.Courses.Include(a => a.Assessments).ThenInclude(b => b.Feedback).ThenInclude(c=> c.Analysis).ToList();
        }
        public async Task<bool> LoadDataFromBackupFile()
        {
            return false;
        }
        
        public async Task<List<Course>> GetCourses()
        {
            List<Course> tmp = new List<Course>();
            try
            {
                var ctx = await _dbFactory.CreateDbContextAsync();
                tmp = ctx.Courses.Include(x => x.Assessments).ThenInclude(f => f.Feedback).ThenInclude(a => a.Analysis).ToList();
                if (tmp == null || tmp.Count == 0)
                    return new List<Course>();
                else
                    return tmp;
            } catch (Exception ex)
            {
                return tmp;
            }
        }

        public async Task<Course> GetCourseById(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Courses.FindAsync(id);
        }

        public async Task<int?> GetCourseYearByFeedbackId(Guid feedbackId)
        {
            if(courseCollection == null)
                courseCollection = await this.GetCourses();
            int result = courseCollection.SelectMany(course => course.Assessments, (course, assessment) => new { course, assessment })
                .SelectMany(courseAssessment => courseAssessment.assessment.Feedback, (courseAssessment, feedback) => new { courseAssessment.course, feedback })
                .Where(courseFeedback => courseFeedback.feedback.Id == feedbackId)
                .Select(courseFeedback => courseFeedback.course.Year)
                .FirstOrDefault();

            return result;
        }

        public async Task<bool> Delete(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            var tmp = await ctx.Courses.FindAsync(id);
            ctx.Courses.Remove(tmp);
            await ctx.SaveChangesAsync();
            return true;
        }
        //Add a new course
        public async Task<bool> AddCourse(Course c)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            ctx.Courses.Add(c);
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditCourse(Course c)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            Course tmp = ctx.Courses.Find(c.Id);
            if(tmp == null)
                return false;
            tmp.CourseCode = c.CourseCode;
            tmp.Name = c.Name;
            tmp.Year = c.Year;
            tmp.Semester = c.Semester;
            await ctx.SaveChangesAsync();
            return true;
        }

       
    }
}
