using FeedbackFocus.Components.CourseCharts.HelperClasses;
using FeedbackFocus.Data;
using FeedbackFocus.Models;
using FeedbackFocus.Pages;
using Microsoft.EntityFrameworkCore;
using PSC.Blazor.Components.Chartjs.Models.Common;
using SqliteWasmHelper;
using System.Linq;
using System.Reflection;

namespace FeedbackFocus.Services
{
    public class AssessmentDashboardService
    {
        private Dictionary<string, decimal> _emotionInAssessmentsTotal = new Dictionary<string, decimal>();
        //Service for what has been filtered
        private FilterService _filterService;

        public bool ready = false;
        private CourseService _courseService;
        public string SelectedAssessments { get; set; } = "";
        public decimal? AverageWordsInEntireDataSetFeedback { get; set; } = null;
        public string AverageWordsText { get; set; } = "";
        public decimal? AverageWordsInFeedback { get; set; } = null;
        public int TotalNumberOfFeedbackItems { get; set; } = 0;
        public int TotalNumberOfAssessments { get; set; } = 0;
        public decimal AverageWordsInFeedbackByAssessment { get; set; } = 0;
        public decimal? MinWordsInFeedback { get; set; } = null;

        public decimal TotalWordsInFeedback { get; set; } = 0;
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        //Base data from which everything else is derived
        public List<Assessment>? CompleteAssessmentData { get; set; }
        public List<Course>? CourseData { get; set; }

        public string MostPrevalentEmotion
        {
            get
            {
                string theKey = "";
                foreach(var x in _emotionInAssessmentsTotal)
                {
                    if (x.Value == _emotionInAssessmentsTotal.Values.Max())
                        theKey = x.Key;
                }
                switch (theKey)
                {
                    case "anger":
                        return "Anger 😡";
                    case "disgust":
                        return "Disgust 🤢";
                    case "fear":
                        return "Fear 😨";
                    case "joy":
                        return "Joy 😃";
                    case "sadness":
                        return "Sadness 😢";
                    case "surprise":
                        return "Surprise 😲";
                    case "neutral":
                        return "Neutral 😐";


                }
                return "";
            }
        }
        //Data that has been distilled for visualizations
        public List<EmotionOverallCourseData>? OverallEmotionData { get; set; }
        public List<EmotionCourseData>? EmotionCourseData { get; set; }
        public List<YearlyCourseFeedbackWords>? YearlyAssessmentFeedbackWords { get; set; }
        public List<YearlyCourseGrades>? AssessmentAverageGradeDistribution { get; set; }
        public List<GradeDistribution>? AssessmentGradeDistribution { get; set; }
        public List<SentimentCourseData>? SentimentAssessmentData { get; set; }


        public AssessmentDashboardService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory,
            FilterService filterService, CourseService courseService)
        {
            _dbFactory = dbFactory;
            _filterService = filterService;
            _emotionInAssessmentsTotal.Add("anger", 0);
            _emotionInAssessmentsTotal.Add("disgust", 0);
            _emotionInAssessmentsTotal.Add("fear", 0);
            _emotionInAssessmentsTotal.Add("joy", 0);
            _emotionInAssessmentsTotal.Add("sadness", 0);
            _emotionInAssessmentsTotal.Add("surprise", 0);
            _emotionInAssessmentsTotal.Add("neutral", 0);
            _courseService = courseService;

            
        }

        //Gets the base data from the database
        public async Task RefreshBaseAssessmentDashboardData()
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            //Get the base data filters considered
            CourseData = await ctx.Courses
                .Include(c => c.Assessments)
                .ThenInclude(a => a.Feedback)
                .ThenInclude(f => f.Analysis)
               .ToListAsync();

            CourseData.RemoveAll(x => _filterService.AssessmentCourseExcluded.Contains(x.Name));
            CourseData.RemoveAll(x => _filterService.AssessmentYearExcluded.Contains(x.Year));
            CourseData.RemoveAll(x => _filterService.AssessmentSemesterExcluded.Contains(x.Semester));
            foreach (Course c in CourseData)
            {
                c.Assessments.RemoveAll(x => _filterService.AssessmentNameExcluded.Contains(x.Name));
            }


            //CompleteAssessmentData = CourseData.Where(x => !_filterService.AssessmentNameExcluded.Contains(x.Name)
            //).SelectMany(z => z.Assignments).ToList();
            CompleteAssessmentData = CourseData
           .SelectMany(course => course.Assessments)
           .Where(assessment => !_filterService.AssessmentNameExcluded.Contains(assessment.Name))
           .OrderBy(x=>x.DueDate)
           .ToList();

            await RefreshTotalNumberOfWordsInFeedback();
            await RefreshOverallCourseEmotionChartData();
            await RefreshEmotionAssessmentChart();
            await RefreshSentimentCourseChart();
            await RefreshAssessmentGradeDistributionChartData();
            await RefreshYearlyAverageAssessmentGradeDistributionChartData();
            await RefreshYearlyAverageCourseFeedbackWordsChartData();
            await RefreshSelectedAssessments();
            ready = true;
        }


        //Refresh the data for the data boxes at the top of the dashboard
        public async Task<Task> RefreshTotalNumberOfWordsInFeedback()
        {
            TotalNumberOfFeedbackItems = 0;
            TotalNumberOfAssessments = 0;
            TotalWordsInFeedback = 0;
            AverageWordsInEntireDataSetFeedback = 0;
            await Task.Yield();
            foreach (Course c in CourseData)
                foreach (var Assessment in c.Assessments)
                {
                    TotalNumberOfAssessments++;
                    if (Assessment.Feedback != null)
                    {
                        foreach (var Feedback in Assessment.Feedback)
                        {
                            TotalNumberOfFeedbackItems += 1;

                            if (Feedback.FeedbackToLearner != null && Feedback.FeedbackToLearner.Trim() != "")
                            {
                                var items = Feedback
                                    //In case there are extra spaces, let's remove them before we count the words
                                    .FeedbackToLearner
                                    .Replace("  ", " ")
                                    .Replace("  ", " ")
                                    .Split(' ');
                                TotalWordsInFeedback += items.Length;

                            }
                            //To collect each emotion
                            foreach (var emotion in Feedback.Analysis.Where(x => x.AnalysisType == "emotion").ToList())
                            {
                                _emotionInAssessmentsTotal[emotion.Label] += emotion.Value;
                            }

                        }
                    }
                }
            //Output
            if (TotalNumberOfFeedbackItems > 0)
            {
                AverageWordsInFeedback = TotalWordsInFeedback / TotalNumberOfFeedbackItems;
                AverageWordsInFeedbackByAssessment = TotalWordsInFeedback / TotalNumberOfAssessments;
            }



            //Get all records from DB
            var ctx = await _dbFactory.CreateDbContextAsync();
            //Get the base data filters considered
            var allCourses = ctx.Courses.Include(x=>x.Assessments).ThenInclude(z=>z.Feedback);
            int totalFeedbackItemsInSet = 0;
            int totalFeedbackWordsInSet = 0;

            foreach (Course c in allCourses)
                foreach (var Assessment in c.Assessments)
                {
                    if (Assessment.Feedback != null)
                    {
                        foreach (var Feedback in Assessment.Feedback)
                        {
                            totalFeedbackItemsInSet++;

                            if (Feedback.FeedbackToLearner != null && Feedback.FeedbackToLearner.Trim()!="")
                            {
                                var items = Feedback
                                    //In case there are extra spaces, let's remove them before we count the words
                                    .FeedbackToLearner
                                    .Replace("  ", " ")
                                    .Replace("  ", " ")
                                    .Split(' ');
                                totalFeedbackWordsInSet += items.Length;

                            }
                        }
                    }
                }
            if (totalFeedbackItemsInSet > 0)
            {
                AverageWordsInEntireDataSetFeedback = (decimal)totalFeedbackWordsInSet / TotalNumberOfAssessments;
            }

            if(AverageWordsInFeedbackByAssessment > AverageWordsInEntireDataSetFeedback)
            {
                AverageWordsText = "Above Average";
            } else if (AverageWordsInFeedbackByAssessment < AverageWordsInEntireDataSetFeedback)
            {
                AverageWordsText = "Below Average";
            } else if((AverageWordsInFeedbackByAssessment == AverageWordsInEntireDataSetFeedback))
            {
                AverageWordsText = "Identical";
            }
            return Task.CompletedTask;
        }
        //Refreshes the data for all courses combined
        public async Task RefreshOverallCourseEmotionChartData()
        {
            // Artificial asynchronous operation
            await Task.Yield();
            var emotionSums = new Dictionary<string, decimal>();

            // Populate the dictionary with predefined emotions
            var emotions = new[] { "Joy", "Surprise", "Neutral", "Sadness", "Anger", "Disgust", "Fear" };
            foreach (var emotion in emotions)
            {
                emotionSums[emotion] = 0;
            }
            
                foreach (var assessment in CompleteAssessmentData)
                    foreach (var feedback in assessment.Feedback)
                        foreach (var analysis in feedback.Analysis)
                        {
                            if (analysis.AnalysisType == "emotion")
                            {
                                string firstCaptial = analysis.Label.Substring(0, 1).ToUpper() + analysis.Label.Substring(1, analysis.Label.Length - 1);
                                emotionSums[firstCaptial] += analysis.Value;
                            }
                        }
            // Convert dictionary back to list
            var overallDataList = emotionSums.Select(kvp => new EmotionOverallCourseData
            {
                Emotion = kvp.Key,
                Value = kvp.Value
            }).ToList();

            OverallEmotionData = overallDataList;

        }

        //Refreshes the data for the individual courses
        public async Task<List<EmotionCourseData>> RefreshEmotionAssessmentChart()
        {
            await Task.Yield();
            List<EmotionCourseData> lstItems = new List<EmotionCourseData>();

            foreach (var assessment in CompleteAssessmentData)
            {
                EmotionCourseData tnp = new EmotionCourseData();
                int feedbackCount = 0; // Counter for feedback records
                if (lstItems.Contains(lstItems.Find(x => x.Course == assessment.Name)))
                {
                    tnp = lstItems.Where(lstItems => lstItems.Course == assessment.Name).FirstOrDefault();
                    lstItems.Remove(tnp);
                }
                else
                {
                    //tnp = new EmotionCourseData();
                }
                tnp.Course = assessment.Name;

                    foreach (var feedback in assessment.Feedback)
                    {
                    feedbackCount++; // Increment feedback counter
                    foreach (var analysis in feedback.Analysis)
                        {
                            if (analysis.AnalysisType == "emotion")
                            {
                                if (analysis.Label == "anger")
                                {
                                    tnp.Anger += analysis.Value;
                                }
                                else if (analysis.Label == "disgust")
                                {
                                    tnp.Disgust += analysis.Value;
                                }
                                else if (analysis.Label == "fear")
                                {
                                    tnp.Fear += analysis.Value;
                                }
                                else if (analysis.Label == "joy")
                                {
                                    tnp.Joy += analysis.Value;
                                }
                                else if (analysis.Label == "sadness")
                                {
                                    tnp.Sadness += analysis.Value;
                                }
                                else if (analysis.Label == "surprise")
                                {
                                    tnp.Surprise += analysis.Value;
                                }
                                else if (analysis.Label == "neutral")
                                {
                                    tnp.Neutral += analysis.Value;
                                }


                            }
                        }
                    
                }
                // Normalize the values by dividing by the number of feedback records
                if (feedbackCount > 0)
                {
                    tnp.Anger /= feedbackCount;
                    tnp.Disgust /= feedbackCount;
                    tnp.Fear /= feedbackCount;
                    tnp.Joy /= feedbackCount;
                    tnp.Sadness /= feedbackCount;
                    tnp.Surprise /= feedbackCount;
                    tnp.Neutral /= feedbackCount;
                }
                lstItems.Add(tnp);
            }
            EmotionCourseData = lstItems;
            return lstItems;
        }

        //Refreshes the sentiment score for each course
        public async Task<List<SentimentCourseData>> RefreshSentimentCourseChart()
        {
            await Task.Yield();
            List<SentimentCourseData> lstItems = new List<SentimentCourseData>();


                foreach (var assessment in CompleteAssessmentData)
                {
                    SentimentCourseData tnp = new SentimentCourseData();
                    tnp.Course = assessment.Name;
                    int posCount = 0;
                    int negCount = 0;
                    int neuCount = 0;
                    foreach (var feedback in assessment.Feedback)
                    {
                        foreach (var analysis in feedback.Analysis)
                        {
                            if (analysis.AnalysisType == "sentiment")
                            {
                                if (analysis.Label == "positive")
                                {
                                    tnp.Positive += analysis.Value;
                                    posCount++;
                                }
                                else if (analysis.Label == "neutral")
                                {
                                    tnp.Neutral += analysis.Value;
                                    neuCount++;
                                }
                                else if (analysis.Label == "negative")
                                {
                                    tnp.Negative += analysis.Value;
                                    negCount++;
                                }
                            }
                        }

                    
                }
                if (negCount != 0)
                    tnp.Negative = tnp.Negative / negCount;
                else
                    tnp.Negative = 0;
                if (posCount != 0)
                    tnp.Positive = tnp.Positive / posCount;
                else
                    tnp.Positive = 0;
                if (neuCount != 0)
                    tnp.Neutral = tnp.Neutral / neuCount;
                else
                    tnp.Neutral = 0;
                lstItems.Add(tnp);
            }
            var mergedData = lstItems
            .GroupBy(d => d.Course)
            .Select(g => new SentimentCourseData
            {
                Course = g.Key,
                Positive = g.Sum(d => d.Positive),
                Neutral = g.Sum(d => d.Neutral),
                Negative = g.Sum(d => d.Negative)
            })
            .ToList();
            SentimentAssessmentData = mergedData;
            return SentimentAssessmentData;
        }


        //Refreshes the data in the grade distribution chart overall
        public async Task<List<GradeDistribution>> RefreshAssessmentGradeDistributionChartData()
        {
            await Task.Yield();
            AssessmentGradeDistribution = new List<GradeDistribution>();
            var gradeSums = new Dictionary<string, decimal>();

            // Populate the dictionary with predefined buckets
            var gradeRanges = new[] { "0-50%", "51-59%", "60-69%", "70-79%", "80-89%", "90-100%" };
            foreach (var emotion in gradeRanges)
            {
                gradeSums[emotion] = 0;
            }
            decimal MaxScore = 0;
            // Sum up all emotion values from the source data
   
                foreach (var assignment in CompleteAssessmentData)
                {
                    MaxScore = assignment.MaxScore;
                    foreach (var feedback in assignment.Feedback)
                    {
                        decimal studentGrade = feedback.Grade / MaxScore;
                        if (studentGrade <= 0.5m)
                        {
                            gradeSums["0-50%"] += 1;
                        }
                        else if (studentGrade <= 0.59m)
                        {
                            gradeSums["51-59%"] += 1;
                        }
                        else if (studentGrade <= 0.69m)
                        {
                            gradeSums["60-69%"] += 1;
                        }
                        else if (studentGrade <= 0.79m)
                        {
                            gradeSums["70-79%"] += 1;
                        }
                        else if (studentGrade <= 0.89m)
                        {
                            gradeSums["80-89%"] += 1;
                        }
                        else
                        {
                            gradeSums["90-100%"] += 1;
                        }
                    }
                
            }
            var overallDataList = gradeSums.Select(kvp => new GradeDistribution
            {
                Bucket = kvp.Key,
                Value = kvp.Value
            }).ToList();
            AssessmentGradeDistribution = overallDataList;
            return overallDataList;
        }

        //Refreshes the data in the yearly grade distribution chart by course
        public async Task<List<YearlyCourseGrades>> RefreshYearlyAverageAssessmentGradeDistributionChartData()
        {
            await Task.Yield();
            AssessmentAverageGradeDistribution = new List<YearlyCourseGrades>();
            List<YearlyCourseGrades> yearlyCourseGrades = new List<YearlyCourseGrades>();

            //add the grade received on each feedback item to the course's total

                foreach (var assignment in CompleteAssessmentData)
                {
                    foreach (var feedback in assignment.Feedback)
                    {
                        YearlyCourseGrades tnp = new YearlyCourseGrades();
                    tnp.Course = assignment.Name;
                        if (assignment.MaxScore == 0)
                            tnp.Grade = 0;
                        else
                            tnp.Grade = (feedback.Grade / assignment.MaxScore);
                    tnp.Year =(int)await _courseService.GetCourseYearByFeedbackId(feedback.Id);
                        yearlyCourseGrades.Add(tnp);
                    }
                
            }
            var averageGrades = yearlyCourseGrades
            .GroupBy(g => new { g.Year, g.Course })
            .Select(g => new
            {
                Year = g.Key.Year,
                Course = g.Key.Course,
                AverageGrade = g.Average(x => x.Grade)
            });

            foreach (var avgGrade in averageGrades)
            {
                YearlyCourseGrades grd = new YearlyCourseGrades();
                grd.Course = avgGrade.Course;
                grd.Grade = avgGrade.AverageGrade;
                grd.Year = avgGrade.Year;
                AssessmentAverageGradeDistribution.Add(grd);
            }
            return AssessmentAverageGradeDistribution;
        }

        //Refreshes the number of words in feedback per course over years
        public async Task<List<YearlyCourseFeedbackWords>> RefreshYearlyAverageCourseFeedbackWordsChartData()
        {
            await Task.Yield();
            YearlyAssessmentFeedbackWords = new List<YearlyCourseFeedbackWords>();
            List<YearlyCourseFeedbackWords> yearlyCourseFeedback = new List<YearlyCourseFeedbackWords>();

            //add the grade received on each feedback item to the course's total
           
                foreach (var assignment in CompleteAssessmentData)
                {
                    foreach (var feedback in assignment.Feedback)
                    {
                        YearlyCourseFeedbackWords tnp = new YearlyCourseFeedbackWords();
                        tnp.Course = assignment.Name;

                        if (feedback.FeedbackToLearner.Length == 0 || feedback.FeedbackToLearner.Trim() == "")
                        {
                            tnp.WordCount = 0;
                        }
                        else
                        {
                            tnp.WordCount = feedback.FeedbackToLearner.Split(' ').Length;
                        }
                        tnp.Year = (int)await _courseService.GetCourseYearByFeedbackId(feedback.Id);
                        
                        yearlyCourseFeedback.Add(tnp);
                    }
                
            }


            var averageWordCounts = yearlyCourseFeedback
            .GroupBy(x => new { x.Year, x.Course })
            .Select(g => new YearlyCourseFeedbackWords
            {
                Year = g.Key.Year,
                Course = g.Key.Course,
                WordCount = (int)g.Average(x => x.WordCount) // Cast to int if needed
            })
            .ToList();
            YearlyAssessmentFeedbackWords = averageWordCounts;
            return YearlyAssessmentFeedbackWords;
        }

        //to list selected assessments
        public async Task RefreshSelectedAssessments()
        {
            SelectedAssessments = "";
            // Artificial asynchronous operation
            await Task.Yield();
          foreach(var x in CompleteAssessmentData)
            {
                if(SelectedAssessments.Contains(x.Name + ", "))
                {
                    continue;
                }
                SelectedAssessments += x.Name + ", ";
            }
          if(SelectedAssessments.Length > 2)
            SelectedAssessments = SelectedAssessments.Substring(0, SelectedAssessments.Length - 2);

        }

    }
}



