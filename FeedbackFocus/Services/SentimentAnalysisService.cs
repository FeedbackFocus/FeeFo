using FeedbackFocus.Data;
using FeedbackFocus.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SqliteWasmHelper;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace FeedbackFocus.Services;


public class SentimentAnalysisService
{
    private static readonly HttpClient client = new HttpClient();
    private const string API_URL = "https://api-inference.huggingface.co/models/cardiffnlp/twitter-roberta-base-sentiment-latest";
    private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;
    private IConfiguration _config;
    private static string HFApiKey = "";
    public SentimentAnalysisService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _config = config;
        HFApiKey = _config.GetValue<string>("HuggingFaceApiKey");
    }

    public async Task<bool> AnalyzeAssignmentFeedback(FeedbackItem f)
    {
        try
        {

       
        var output = await Query(new
        {
            inputs = StripHtml(f.FeedbackToLearner)
        });
        var abc123 = JsonConvert.DeserializeObject<dynamic>(output);

        List<List<EmotionScore>> emotionsWrapper = JsonConvert.DeserializeObject<List<List<EmotionScore>>>(output);


        List<EmotionScore> emotionsList = emotionsWrapper[0];
        var ctx = await _dbFactory.CreateDbContextAsync();
        foreach (EmotionScore e1 in emotionsList)
        {
            AnalysisItem ai1 = new AnalysisItem()
            {
                AnalysisType = "sentiment",
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
        await Task.Delay(750);
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
