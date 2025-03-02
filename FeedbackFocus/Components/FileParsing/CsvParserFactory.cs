using FeedbackFocus.Services;
namespace FeedbackFocus.Components.FileParsing
{
    public class CsvParserFactory
    {
        private FeedbackService feedbackService;
        private AssessmentService assessmentService;
        public CsvParserFactory(FeedbackService feedback, AssessmentService assessment)
        {
            feedbackService = feedback;
            assessmentService = assessment;
        }
        public CsvParserBase CreateParser(string lmsType, char separator, Guid assessmentId)
        {
            return lmsType switch
            {
                "Blackboard" => new BlackboardParser(separator, feedbackService, assessmentService, assessmentId),
                "Moodle" => new MoodleParser(separator, feedbackService, assessmentService, assessmentId), // Assuming MoodleParser will also use the AssessmentId
                _ => throw new NotSupportedException("Unsupported LMS type")
            };
        }
    }

}
