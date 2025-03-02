using FeedbackFocus;
using FeedbackFocus.Data;
using FeedbackFocus.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using SqliteWasmHelper;

using Blazorise;
using Blazorise.Bootstrap;
using FeedbackFocus.Components.StaticResource;
using FeedbackFocus.Components.FileParsing;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
//Add interactive web assembly components

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<StudentDataHelper>();
builder.Services.AddScoped<AssessmentService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<EmotionAnalysisService>();
builder.Services.AddScoped<SentimentAnalysisService>();
builder.Services.AddScoped<AnalysisItemService>();
builder.Services.AddScoped<FilterService>();
builder.Services.AddScoped<BrowserCache>();
builder.Services.AddScoped<CourseDashboardService>();
builder.Services.AddScoped<AssessmentDashboardService>();
builder.Services.AddScoped<StudentDashboardService>();
builder.Services.AddScoped<CsvParserFactory>();
builder.Services.AddScoped<StudentObfuscator>();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSqliteWasmDbContextFactory<AnalysisContext>(opts =>
opts.UseSqlite("Data Source=analysis2.sqlite3"));
//opts.UseSqlServer("server=localhost;database=appDB;user id=sa;password=P@ssword!;encrypt=true;"));

builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
    })
    .AddBootstrapProviders()
  ;

await builder.Build().RunAsync();
