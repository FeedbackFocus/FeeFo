using FeedbackFocus.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace FeedbackFocus.Data
{
    public class AnalysisContext : DbContext
    {

        public AnalysisContext(DbContextOptions<AnalysisContext> options) : base(options)
        {
            try
            {

           Database.EnsureCreated();
            } catch
            {

            }
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<FeedbackItem> FeedbackItems { get; set; }
        public DbSet<Assessment> Assignments { get; set; }
        public DbSet<AnalysisItem> Analyses { get; set; }
    }
}
