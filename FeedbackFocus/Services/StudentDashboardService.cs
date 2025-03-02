using FeedbackFocus.Components.CourseCharts;
using FeedbackFocus.Components.CourseCharts.HelperClasses;
using FeedbackFocus.Components.StaticResource;
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
    public class StudentDashboardService
    {
        private Dictionary<string, decimal> _sentimentInAssessmentsTotal = new Dictionary<string, decimal>();
        //Service for what has been filtered
        private FilterService _filterService;
        int _sentimentAnalysisCount = 0;
        public bool ready = false;
        //private CourseService _courseService;
        public string SelectedStudents { get; set; } = "";
        public int CountSelectedStudents { get; set; } = 0;
        public int CountCourses { get; set; } = 0;
        public int CountWords { get; set; } = 0;
        public decimal? AverageWordsInEntireDataSetFeedback { get; set; } = 0;
        public string AverageWordsText { get; set; } = "";
        public decimal? AverageWordsInFeedback { get; set; } = 0;
        public int TotalNumberOfFeedbackItems { get; set; } = 0;
        public decimal? MinWordsInFeedback { get; set; } = 0;
        public decimal? AverageSentimentPositive { get; set; } = 0;
        public decimal? AverageSentimentNeutral { get; set; } = 0;
        public decimal? AverageSentimentNegative { get; set; } = 0;

        public decimal TotalWordsInFeedback { get; set; } = 0;
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        //Base data from which everything else is derived
        public List<Assessment>? CompleteAssessmentData { get; set; }
        public List<Course>? CourseData { get; set; }

        StudentDataHelper _fullNameHelper;
        public string MostPrevalentEmotion
        {
            get
            {
                string theKey = "";
                foreach (var x in _sentimentInAssessmentsTotal)
                {
                    if (x.Value == _sentimentInAssessmentsTotal.Values.Max())
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
        public List<YearlyCourseFeedbackWords>? YearlyStudentFeedbackWords { get; set; }
        public List<YearlyCourseGrades>? StudentAverageGradeDistribution { get; set; }
        public List<GradeDistribution>? StudentGradeDistribution { get; set; }
        public List<SentimentCourseData>? SentimentStudentData { get; set; }



        public StudentDashboardService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory,
            FilterService filterService,
            StudentDataHelper studentDataHelper
            //, CourseService courseService
            )
        {
            _dbFactory = dbFactory;
            _filterService = filterService;
            _sentimentInAssessmentsTotal.Add("positive", 0);
            _sentimentInAssessmentsTotal.Add("neutral", 0);
            _sentimentInAssessmentsTotal.Add("negative", 0);
            _fullNameHelper = studentDataHelper;
            //_courseService = courseService;
        }

        //Gets the base data from the database
        public async Task RefreshBaseStudentDashboardData()
        {
            await _filterService.InitializeAsync();

            var ctx = await _dbFactory.CreateDbContextAsync();
            //Get the base data filters considered
            CourseData = await ctx.Courses
    .Include(c => c.Assessments)
    .ThenInclude(a => a.Feedback)
    .ThenInclude(f => f.Analysis)
    .ToListAsync();
            //Apply the filters
            CourseData.RemoveAll(x => _filterService.StudentCourseExcluded.Contains(x.Name));
            CourseData.RemoveAll(x => _filterService.StudentYearExcluded.Contains(x.Year));
            CourseData.RemoveAll(x => _filterService.StudentSemesterExcluded.Contains(x.Semester));
            CourseData = CourseData
                .Where(c => c.Assessments != null && c.Assessments
                .Any(a => a.Feedback != null && a.Feedback
                .Any(f => !string.IsNullOrWhiteSpace(f.FeedbackToLearner))))
    .ToList();

            foreach (Course c in CourseData)
            {

                foreach (Assessment a in c.Assessments)
                {
                    a.Feedback.RemoveAll(x => _filterService.StudentNameExcluded.Contains(x.FirstName + " " + x.LastName));
                }

            }

            //CourseData.RemoveAll(x => x.Assignments.Count == 0);

            await RefreshSelectedStudents();
            await RefreshAverageSentimentScore();
            await RefreshTotalNumberOfWordsInFeedback();
            await RefreshOverallStudentEmotionChartData();
            await RefreshEmotionStudentChart();
            await RefreshSentimentStudentChart();
            await RefreshStudentGradeDistributionChartData();
 
            await RefreshYearlyAverageStudentGradeDistributionChartData();
            await RefreshYearlyTotalStudentFeedbackWordsChartData();
           
            ready = true;

        }


        //Refresh the data for the data boxes at the top of the dashboard
        public async Task<Task> RefreshAverageSentimentScore()
        {
            _sentimentAnalysisCount = 0;
            _sentimentInAssessmentsTotal["positive"] = 0;
            _sentimentInAssessmentsTotal["neutral"] = 0;
            _sentimentInAssessmentsTotal["negative"] = 0;

            await Task.Yield();

            foreach (Course c in CourseData)
            {
                foreach (var assessment in c.Assessments)
                {
                    if (assessment.Feedback != null)
                    {
                        foreach (var feedback in assessment.Feedback)
                        {
                            TotalNumberOfFeedbackItems += 1;
                            if (feedback.FeedbackToLearner != null && feedback.FeedbackToLearner.Length > 0)
                            {
                                bool hasSentimentAnalysis = false;
                                foreach (AnalysisItem a in feedback.Analysis)
                                {
                                    if (a.AnalysisType == "sentiment")
                                    {
                                        if (a.Label == "positive")
                                        {
                                            _sentimentInAssessmentsTotal["positive"] += a.Value;
                                            hasSentimentAnalysis = true;
                                        }
                                        else if (a.Label == "neutral")
                                        {
                                            _sentimentInAssessmentsTotal["neutral"] += a.Value;
                                            hasSentimentAnalysis = true;
                                        }
                                        else if (a.Label == "negative")
                                        {
                                            _sentimentInAssessmentsTotal["negative"] += a.Value;
                                            hasSentimentAnalysis = true;
                                        }
                                    }
                                }

                                if (hasSentimentAnalysis)
                                {
                                    _sentimentAnalysisCount++;
                                }
                            }
                        }
                    }
                }
            }

            if (_sentimentAnalysisCount > 0)
            {
                AverageSentimentPositive = Math.Round(_sentimentInAssessmentsTotal["positive"] / _sentimentAnalysisCount, 2);
                AverageSentimentNeutral = Math.Round(_sentimentInAssessmentsTotal["neutral"] / _sentimentAnalysisCount, 2);
                AverageSentimentNegative = Math.Round(_sentimentInAssessmentsTotal["negative"] / _sentimentAnalysisCount, 2);
            }
            else
            {
                AverageSentimentPositive = 0;
                AverageSentimentNeutral = 0;
                AverageSentimentNegative = 0;
            }

            return Task.CompletedTask;
        }





        //Refresh the data for the data boxes at the top of the dashboard
        public async Task<Task> RefreshTotalNumberOfWordsInFeedback()
        {
            TotalNumberOfFeedbackItems = 0;
            TotalWordsInFeedback = 0;
            AverageWordsInEntireDataSetFeedback = 0;
            await Task.Yield();
            foreach (Course c in CourseData)
                foreach (var Assessment in c.Assessments)
                {
                    if (Assessment.Feedback != null)
                    {
                        foreach (var Feedback in Assessment.Feedback)
                        {
                            TotalNumberOfFeedbackItems += 1;

                            if (Feedback.FeedbackToLearner != null)
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
                            // Populate the dictionary with predefined emotions
                            var emotions = new[] { "Joy", "Surprise", "Neutral", "Sadness", "Anger", "Disgust", "Fear" };
                            foreach (var emotion in emotions)
                            {
                                _sentimentInAssessmentsTotal[emotion.ToLower()] = 0;
                            }
                            foreach (var emotion in Feedback.Analysis.Where(x => x.AnalysisType == "emotion").ToList())
                            {
                                _sentimentInAssessmentsTotal[emotion.Label] += emotion.Value;
                            }

                        }
                    }
                }
            //Output
            if (TotalNumberOfFeedbackItems > 0)
            {
                AverageWordsInFeedback = TotalWordsInFeedback / TotalNumberOfFeedbackItems;
            }



            //Get all records from DB
            var ctx = await _dbFactory.CreateDbContextAsync();
            //Get the base data filters considered
            var allCourses = ctx.Courses.Include(x => x.Assessments).ThenInclude(z => z.Feedback);
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

                            if (Feedback.FeedbackToLearner != null)
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
            if(totalFeedbackItemsInSet > 0 ) 
            AverageWordsInEntireDataSetFeedback = (decimal)totalFeedbackWordsInSet / totalFeedbackItemsInSet;


            if (AverageWordsInFeedback > AverageWordsInEntireDataSetFeedback)
            {
                AverageWordsText = "Above Average";
            }
            else if (AverageWordsInFeedback < AverageWordsInEntireDataSetFeedback)
            {
                AverageWordsText = "Below Average";
            }
            else if ((AverageWordsInFeedback == AverageWordsInEntireDataSetFeedback))
            {
                AverageWordsText = "Identical";
            }
            return Task.CompletedTask;
        }
        //Refreshes the data for all courses combined
        public async Task RefreshOverallStudentEmotionChartData()
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
            foreach (var crs in CourseData)
                foreach (var assessment in crs.Assessments)
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
        public async Task<List<EmotionCourseData>> RefreshEmotionStudentChart()
        {
            await Task.Yield();
            List<EmotionCourseData> lstItems = new List<EmotionCourseData>();

            foreach (var course in CourseData)
            {
                foreach (var assessment in course.Assessments)
                {
                    foreach (var feedback in assessment.Feedback)
                    {
                        EmotionCourseData tnp = new EmotionCourseData();
                        var existingItem = lstItems.FirstOrDefault(x => x.Course == _fullNameHelper.GetStudentFullName(feedback.FirstName, feedback.LastName, feedback.Username));

                        if (existingItem != null)
                        {
                            tnp = existingItem;
                            lstItems.Remove(existingItem);
                        }

                        tnp.Course = _fullNameHelper.GetStudentFullName(feedback.FirstName, feedback.LastName, feedback.Username);

                        foreach (var analysis in feedback.Analysis)
                        {
                            if (analysis.AnalysisType == "emotion")
                            {
                                switch (analysis.Label)
                                {
                                    case "anger":
                                        tnp.Anger += analysis.Value;
                                        break;
                                    case "disgust":
                                        tnp.Disgust += analysis.Value;
                                        break;
                                    case "fear":
                                        tnp.Fear += analysis.Value;
                                        break;
                                    case "joy":
                                        tnp.Joy += analysis.Value;
                                        break;
                                    case "sadness":
                                        tnp.Sadness += analysis.Value;
                                        break;
                                    case "surprise":
                                        tnp.Surprise += analysis.Value;
                                        break;
                                    case "neutral":
                                        tnp.Neutral += analysis.Value;
                                        break;
                                }
                            }
                        }

                        lstItems.Add(tnp);
                    }
                }
            }

            // Group by course name and calculate the average emotion values
            var groupedItems = lstItems
                .GroupBy(item => item.Course)
                .Select(group =>
                {
                    var averageItem = new EmotionCourseData
                    {
                        Course = group.Key,
                        Anger = group.Average(item => item.Anger),
                        Disgust = group.Average(item => item.Disgust),
                        Fear = group.Average(item => item.Fear),
                        Joy = group.Average(item => item.Joy),
                        Sadness = group.Average(item => item.Sadness),
                        Surprise = group.Average(item => item.Surprise),
                        Neutral = group.Average(item => item.Neutral)
                    };

                    // Normalize each emotion by dividing by the total sum of emotions
                    var totalEmotions = averageItem.Anger + averageItem.Disgust + averageItem.Fear +
                                        averageItem.Joy + averageItem.Sadness + averageItem.Surprise +
                                        averageItem.Neutral;

                    if (totalEmotions > 0)
                    {
                        averageItem.Anger /= totalEmotions;
                        averageItem.Disgust /= totalEmotions;
                        averageItem.Fear /= totalEmotions;
                        averageItem.Joy /= totalEmotions;
                        averageItem.Sadness /= totalEmotions;
                        averageItem.Surprise /= totalEmotions;
                        averageItem.Neutral /= totalEmotions;
                    }

                    return averageItem;
                })
                .ToList();

            EmotionCourseData = groupedItems;
            return groupedItems;
        }


        //Refreshes the sentiment score for each course
        public async Task<List<SentimentCourseData>> RefreshSentimentStudentChart()
        {
            await Task.Yield();
            List<SentimentCourseData> lstItems = new List<SentimentCourseData>();

            foreach (var course in CourseData)
                foreach (var assessment in course.Assessments)
                {
                    foreach (var feedback in assessment.Feedback)
                    {
                        SentimentCourseData tnp = new SentimentCourseData();
                        tnp.Course = _fullNameHelper.GetStudentFullName(feedback.FirstName, feedback.LastName, feedback.Username);
                        foreach (var analysis in feedback.Analysis)
                        {
                            if (analysis.AnalysisType == "sentiment")
                            {
                                if (analysis.Label == "positive")
                                {
                                    tnp.Positive += analysis.Value;
                                    tnp.PositiveCount++;

                                }
                                else if (analysis.Label == "neutral")
                                {
                                    tnp.Neutral += analysis.Value;
                                    tnp.NeutralCount++;
                                }
                                else if (analysis.Label == "negative")
                                {
                                    tnp.Negative += analysis.Value;
                                    tnp.NegativeCount++;
                                }
                            }
                        }
                    lstItems.Add(tnp);
                    }
                }
            var mergedData = lstItems
.GroupBy(d => d.Course)
.Select(g => new SentimentCourseData
{
    Course = g.Key,
    Positive = g.Sum(d => d.PositiveCount) == 0 ? 0 : g.Sum(d => d.Positive) / g.Sum(d => d.PositiveCount),
    Neutral = g.Sum(d => d.NeutralCount) == 0 ? 0 : g.Sum(d => d.Neutral) / g.Sum(d => d.NeutralCount),
    Negative = g.Sum(d => d.NegativeCount) == 0 ? 0 : g.Sum(d => d.Negative) / g.Sum(d => d.NegativeCount)
})
.ToList();
            SentimentStudentData = mergedData;
            return SentimentStudentData;
        }


        //Refreshes the data in the grade distribution chart overall
        public async Task<List<GradeDistribution>> RefreshStudentGradeDistributionChartData()
        {
            await Task.Yield();
            StudentGradeDistribution = new List<GradeDistribution>();
            var gradeSums = new Dictionary<string, decimal>();

            // Populate the dictionary with predefined buckets
            var gradeRanges = new[] { "0-50%", "51-59%", "60-69%", "70-79%", "80-89%", "90-100%" };
            foreach (var emotion in gradeRanges)
            {
                gradeSums[emotion] = 0;
            }
            decimal MaxScore = 0;
            // Sum up all emotion values from the source data
            foreach(var course in CourseData)
            foreach (var assignment in course.Assessments)
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
            StudentGradeDistribution = overallDataList;
            return overallDataList;
        }

        //Refreshes the data in the yearly grade distribution chart by course
        public async Task<List<YearlyCourseGrades>> RefreshYearlyAverageStudentGradeDistributionChartData()
        {
            await Task.Yield();
            StudentAverageGradeDistribution = new List<YearlyCourseGrades>();
            List<YearlyCourseGrades> yearlyCourseGrades = new List<YearlyCourseGrades>();

            //add the grade received on each feedback item to the course's total
            foreach(var course in CourseData)
            foreach (var assignment in course.Assessments)
            {
                foreach (var feedback in assignment.Feedback)
                {
                    YearlyCourseGrades tnp = new YearlyCourseGrades();
                        tnp.Year = course.Year;
                        tnp.Course = _fullNameHelper.GetStudentFullName(feedback.FirstName, feedback.LastName, feedback.Username);
                    if (assignment.MaxScore == 0)
                        tnp.Grade = 0;
                    else
                        tnp.Grade = (feedback.Grade / assignment.MaxScore);
                    //tnp.Year = (int)await _courseService.GetCourseYearByFeedbackId(feedback.Id);
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
                StudentAverageGradeDistribution.Add(grd);
            }
            return StudentAverageGradeDistribution;
        }

        //Refreshes the number of words in feedback per course over years
        public async Task<List<YearlyCourseFeedbackWords>> RefreshYearlyTotalStudentFeedbackWordsChartData()
        {
            await Task.Yield();
            YearlyStudentFeedbackWords = new List<YearlyCourseFeedbackWords>();
            List<YearlyCourseFeedbackWords> yearlyCourseFeedback = new List<YearlyCourseFeedbackWords>();

            //add the grade received on each feedback item to the course's total
            foreach(var course in CourseData)
            foreach (var assignment in course.Assessments)
            {
                foreach (var feedback in assignment.Feedback)
                {
                    YearlyCourseFeedbackWords tnp = new YearlyCourseFeedbackWords();
                    
                        tnp.Course = _fullNameHelper.GetStudentFullName(feedback.FirstName,feedback.LastName,feedback.Username);
                        if (feedback.FeedbackToLearner.Length == 0 || feedback.FeedbackToLearner.Trim() == "")
                    {
                        tnp.WordCount = 0;
                    }
                    else
                    {
                        tnp.WordCount = feedback.FeedbackToLearner.Split(' ').Length;

                    }
                    tnp.Year = course.Year;

                    yearlyCourseFeedback.Add(tnp);
                }

            }


            var averageWordCounts = yearlyCourseFeedback
            .GroupBy(x => new { x.Year, x.Course })
            .Select(g => new YearlyCourseFeedbackWords
            {
                Year = g.Key.Year,
                Course = g.Key.Course,
                WordCount = (int)g.Sum(x => x.WordCount) // Cast to int if needed
            })
            .ToList();
            YearlyStudentFeedbackWords = averageWordCounts;
            return YearlyStudentFeedbackWords;
        }



        //to list selected assessments
        public async Task RefreshSelectedStudents()
        {
            SelectedStudents = "";
            CountSelectedStudents = 0;
            CountWords = 0;
            CountCourses = 0;

            foreach (var x in CourseData)
            {
                CountCourses++;
                foreach (var assig in x.Assessments)
                {
                    foreach (var feed in assig.Feedback)
                    {
                        // Remove the check that skips counting words for students already in SelectedStudents
                        // Now, every feedback item will be counted, regardless of the student

                        if (feed.FeedbackToLearner != null)
                        {
                            // Handle multiple spaces by replacing them with a single space
                            var items = feed.FeedbackToLearner
                                .Replace("  ", " ")
                                .Replace("  ", " ")
                                .Split(' ');
                            CountWords += items.Length;
                        }

                        // Add the student's full name to SelectedStudents without checking for duplicates
                        if (!SelectedStudents.Contains(_fullNameHelper.GetStudentFullName(feed.FirstName, feed.LastName, feed.Username)))
                        {
                            SelectedStudents += _fullNameHelper.GetStudentFullName(feed.FirstName, feed.LastName, feed.Username) + ", ";
                            CountSelectedStudents++;
                        }
                    }
                }
                // Artificial asynchronous operation
                await Task.Yield();
            }
            // Remove the trailing comma and space
            SelectedStudents = SelectedStudents.TrimEnd(' ').TrimEnd(',');

        }


            //to list selected assessments
            public async Task CountSelectedCourses()
        {
            //MERID: Should a course that has no assessments or no feedback be counted?

            foreach (var x in CourseData)
            {
                foreach (var assig in x.Assessments)
                {
                    foreach (var feed in assig.Feedback)
                    {

                    }
                }
                // Artificial asynchronous operation
                await Task.Yield();
            }

        }

    }
}



