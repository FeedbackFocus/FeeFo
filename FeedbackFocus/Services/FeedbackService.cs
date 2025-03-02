using FeedbackFocus.Data;
using FeedbackFocus.Models;
using Microsoft.EntityFrameworkCore;
using SqliteWasmHelper;

namespace FeedbackFocus.Services
{
    public class FeedbackService
    {
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        public FeedbackService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory) =>
            _dbFactory = dbFactory;

        public async Task<List<FeedbackItem>> GetFeedback()
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return ctx.FeedbackItems.Include(x => x.Analysis).ToList();
        }
        public async Task<FeedbackItem> GetFeedbackById(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.FeedbackItems.FindAsync(id);
        }
        public async Task<bool> Delete(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            var tmp = await ctx.FeedbackItems.FindAsync(id);
            ctx.FeedbackItems.Remove(tmp);
            await ctx.SaveChangesAsync();
            return true;
        }
        public async Task<bool> AddFeedback(FeedbackItem c)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            ctx.FeedbackItems.Add(c);
            await ctx.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SaveFeedback(FeedbackItem c)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            var tmp = await ctx.FeedbackItems.FindAsync(c.Id);
 

            tmp.FeedbackToLearner = c.FeedbackToLearner;
            tmp.FirstName = c.FirstName;
            tmp.LastName = c.LastName;
            tmp.Username = c.Username;
            tmp.Grade = c.Grade;
            await ctx.SaveChangesAsync();
            return true;
        }
    }
}
