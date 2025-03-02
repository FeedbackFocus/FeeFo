using FeedbackFocus.Data;
using FeedbackFocus.Models;
using SqliteWasmHelper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedbackFocus.Services
{
    public class AnalysisItemService
    {
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        public AnalysisItemService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory) =>
            _dbFactory = dbFactory;

        // Fetch all AnalysisItems
        public async Task<List<AnalysisItem>> GetAnalysisItems()
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return ctx.Analyses.ToList();
        }

        // Fetch a specific AnalysisItem by its ID
        public async Task<AnalysisItem> GetAnalysisItemById(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Analyses.FindAsync(id);
        }

        // Delete an AnalysisItem by its ID
        public async Task<bool> Delete(Guid id)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            var tmp = await ctx.Analyses.FindAsync(id);
            ctx.Analyses.Remove(tmp);
            await ctx.SaveChangesAsync();
            return true;
        }

        // Add a new AnalysisItem
        public async Task<bool> AddAnalysisItem(AnalysisItem item)
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            ctx.Analyses.Add(item);
            await ctx.SaveChangesAsync();
            return true;
        }
    }
}
