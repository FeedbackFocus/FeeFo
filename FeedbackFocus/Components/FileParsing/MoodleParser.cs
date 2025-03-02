using FeedbackFocus.Models;
using FeedbackFocus.Services;
using System.Text.RegularExpressions;

namespace FeedbackFocus.Components.FileParsing
{
    public class MoodleParser : CsvParserBase
    {
        private readonly FeedbackService _feedbackService;
        private readonly AssessmentService _assessmentService;
        private readonly Guid _assessmentId;

        public MoodleParser(char separator, FeedbackService feedbackService, AssessmentService assessmentService, Guid assessmentId)
            : base(separator)
        {
            _feedbackService = feedbackService;
            _assessmentService = assessmentService;
            _assessmentId = assessmentId;
        }

        public override async Task ProcessRows(List<string[]> rows)
        {
            int maxScore = 0;
            var tmp = rows.SelectMany(r => r).ToList();

            for (int i = 11; i < tmp.Count(); i += 11)
            {
                try
                {
                    decimal tmpMax = string.IsNullOrEmpty(tmp[i + 4]) ? 0 : Decimal.Parse(tmp[i + 4]);
                    if (maxScore < tmpMax)
                    {
                        maxScore = (int)tmpMax;
                    }
                }
                finally
                {

                }

                try
                {

                    FeedbackItem item = new()
                    {
                        LastName = tmp[i + 1].Split(" ")[1],
                        FirstName = tmp[i + 1].Split(" ")[0],
                        Username = tmp[i + 2],
                        Grade = string.IsNullOrEmpty(tmp[i + 4]) ? 0 : Decimal.Parse(tmp[i + 4]),
                        FeedbackToLearner = tmp[i+10],
                        AssessmentId = _assessmentId
                    };
                    await _feedbackService.AddFeedback(item);
                }
                catch (Exception ex)
                {
                    // Handle exceptions if necessary
                }
            }

            try
            {
                var assessment = await _assessmentService.GetAssignmentById(_assessmentId);
                assessment.MaxScore = maxScore;
                await _assessmentService.EditAssessment(assessment);
            }
            catch
            {

            }

        }
    }


}
