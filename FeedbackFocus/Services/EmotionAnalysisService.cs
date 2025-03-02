using FeedbackFocus.Data;
using FeedbackFocus.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SqliteWasmHelper;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace FeedbackFocus.Services;

public class EmotionScoreWrapper
{
    public List<EmotionScore> EmotionScores { get; set; }
}
public class EmotionScore
{
    public string label { get; set; }
    public decimal score { get; set; }
}

public class EmotionAnalysisService
{
    private static readonly HttpClient client = new HttpClient();
    private const string API_URL = "https://api-inference.huggingface.co/models/j-hartmann/emotion-english-distilroberta-base";
    private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;
    private IConfiguration _config;
    private static string HFApiKey = "";

    public EmotionAnalysisService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _config = config;
        HFApiKey = _config.GetValue<string>("HuggingFaceApiKey");
    }

    public async Task<List<AnalysisItem>> GetEmotionAnalysis(FeedbackItem f)
    {
        try
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return ctx.Analyses.Where(x => x.FeedbackItemId == f.Id && x.AnalysisType == "emotion").ToList();
        }
        catch
        {
            return new List<AnalysisItem>();
        }
    }
    public async Task<List<AnalysisItem>> GetSentimentAnalysis(FeedbackItem f)
    {
        try
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            return ctx.Analyses.Where(x => x.FeedbackItemId == f.Id && x.AnalysisType == "sentiment").ToList();
        }
        catch
        {
            return new List<AnalysisItem>();
        }
    }

    public async Task<bool> AnalyzeAssignmentFeedback(FeedbackItem f)
    {
        try
        {
            var output = await Query(new
            {
                inputs = StripHtml(f.FeedbackToLearner)
            });
            List<List<EmotionScore>> emotionsWrapper = JsonConvert.DeserializeObject<List<List<EmotionScore>>>(output);
            List<EmotionScore> emotionsList = emotionsWrapper[0];
            var ctx = await _dbFactory.CreateDbContextAsync();
            foreach (EmotionScore e1 in emotionsList)
            {
                AnalysisItem ai1 = new AnalysisItem()
                {
                    AnalysisType = "emotion",
                    FeedbackItemId = f.Id,
                    Label = e1.label,
                    Value = e1.score
                };
                ctx.Analyses.Add(ai1);
                await ctx.SaveChangesAsync();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
    private string StripHtml(string source)
    {
        string output;

        //get rid of HTML tags
        output = Regex.Replace(source, "<[^>]*>", string.Empty);

        //get rid of multiple blank lines
        output = Regex.Replace(output, @"^\s*$\n", string.Empty, RegexOptions.Multiline);

        return output;
    }
    static async Task<string> Query(object payload)
    {
        var json = JsonConvert.SerializeObject(payload);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HFApiKey);
        var response = await client.PostAsync(API_URL, data);

        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
