using FeedbackFocus.Data;
using FeedbackFocus.Models;
using Microsoft.EntityFrameworkCore;
using SqliteWasmHelper;

namespace FeedbackFocus.Services
{
    public class AssessmentService
    {
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        public AssessmentService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory) =>
            _dbFactory = dbFactory;

        public async Task<List<Assessment>> GetAssessments()
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            //if (ctx.Courses.Any())
            //{
            //    await ctx.SaveChangesAsync();
            //}
            var ret = await ctx.Assignments.Include(x => x.Feedback).ThenInclude(x=>x.Analysis).ToListAsync();
            return ret;
        }

        public async Task<Assessment> GetAssignmentById(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Assignments.FindAsync(id);
        }

        public async Task<bool> Delete(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            var tmp = await ctx.Assignments.FindAsync(id);
            ctx.Assignments.Remove(tmp);
            await ctx.SaveChangesAsync();
            return true;
        }
        //Add a new course
        public async Task<bool> AddAssignment(Assessment c)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            ctx.Assignments.Add(c);
            await ctx.SaveChangesAsync();
            return true;
        }
        public async Task<bool> EditAssessment(Assessment c)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            var tmp = ctx.Find<Assessment>(c.Id);
            if(tmp==null)
            {
                return false;
            }
            tmp.Name = c.Name;
            tmp.MaxScore = c.MaxScore;
            tmp.DueDate = c.DueDate;
            await ctx.SaveChangesAsync();
            return true;
        }
    }
}
