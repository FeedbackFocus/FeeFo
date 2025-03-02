using FeedbackFocus.Models;
using FeedbackFocus.Services;
using System.Text.RegularExpressions;

namespace FeedbackFocus.Components.FileParsing
{
    public class BlackboardParser : CsvParserBase
    {
        private readonly FeedbackService _feedbackService;
        private readonly AssessmentService _assessmentService;
        private readonly Guid _assessmentId;

        public BlackboardParser(char separator, FeedbackService feedbackService, AssessmentService assessmentService, Guid assessmentId)
            : base(separator)
        {
            _feedbackService = feedbackService;
            _assessmentService = assessmentService;
            _assessmentId = assessmentId;
        }

        public override async Task ProcessRows(List<string[]> rows)
        {
            var tmp = rows.SelectMany(r => r).ToList();
            string assessmentName = tmp[4];

            var regex = new Regex(@"^(?<name>.*?)(?:\s\[Total Pts:\s(?<points>\d+))");
            var match = regex.Match(assessmentName);
            if (match.Success)
            {
                string assignmentName = match.Groups["name"].Value.Trim();
                int totalPoints = int.Parse(match.Groups["points"].Value);
                if (assessmentName.ToLower().Contains("percentage"))
                {
                    totalPoints = 100;
                }

                var assessment = await _assessmentService.GetAssignmentById(_assessmentId);
                assessment.Name = assignmentName;
                assessment.MaxScore = totalPoints;
                await _assessmentService.EditAssessment(assessment);
            }

            for (int i = 9; i < tmp.Count(); i += 9)
            {
                try
                {
                    FeedbackItem item = new()
                    {
                        LastName = tmp[i],
                        FirstName = tmp[i + 1],
                        Username = tmp[i + 2],
                        Grade = string.IsNullOrEmpty(tmp[i + 4]) ? 0 : Decimal.Parse(tmp[i + 4]),
                        FeedbackToLearner = tmp[i + 7],
                        AssessmentId = _assessmentId
                    };
                    await _feedbackService.AddFeedback(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    // Handle exceptions if necessary
                }
            }
        }
    }


}
