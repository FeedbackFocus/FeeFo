using FeedbackFocus.Components.CourseCharts.HelperClasses;
using FeedbackFocus.Data;
using FeedbackFocus.Models;
using Microsoft.EntityFrameworkCore;
using PSC.Blazor.Components.Chartjs.Models.Common;
using SqliteWasmHelper;
using System.Reflection;

namespace FeedbackFocus.Services
{
    public class CourseDashboardService
    {
        //Service for what has been filtered
        private FilterService _filterService;

        public bool ready = false;
        public int? TotalNumberOfCourses { get; set; } = null;
        public int? TotalNumberOfFeedbackItems { get; set; } = null;
        public decimal? AverageWordsInFeedback { get; set; } = null;
        public decimal? MinWordsInFeedback { get; set; } = null;
        public decimal? MaxWordsInFeedback { get; set; } = null;
        public decimal? StandardDeviationWordsInFeedback { get; set; } = null;
        private readonly ISqliteWasmDbContextFactory<AnalysisContext> _dbFactory;

        //Base data from which everything else is derived
        public List<Course>? CompleteCourseData { get; set; }

        //Data that has been distilled for visualizations
        public List<EmotionOverallCourseData>? OverallEmotionData { get; set; }
        public List<EmotionCourseData>? EmotionCourseData { get; set; }
        public List<YearlyCourseFeedbackWords>? YearlyCourseFeedbackWords { get; set; }
        public List<YearlyCourseGrades>? CourseAverageGradeDistribution { get; set; }
        public List<GradeDistribution>? CourseGradeDistribution { get; set; }
        public List<SentimentCourseData>? SentimentCourseData { get; set; }

        public CourseDashboardService(ISqliteWasmDbContextFactory<AnalysisContext> dbFactory,
            FilterService filterService)
        {
            _dbFactory = dbFactory;
            _filterService = filterService;
        }
        //Gets the base data from the database
        public async Task RefreshBaseCourseDashboardData()
        {
            var ctx = await _dbFactory.CreateDbContextAsync();
            CompleteCourseData = await ctx.Courses
                .Where(x => !_filterService.CoursesExcluded.Contains(x.CourseCode)
                && !_filterService.CourseSemestersExcluded.Contains(x.Semester)
                && !_filterService.CourseYearsExcluded.Contains(x.Year))
            .Include(a => a.Assessments)
                 .ThenInclude(b => b.Feedback)
                 .ThenInclude(c => c.Analysis)
                 .ToListAsync();
            //Apply the filtering
            CompleteCourseData.RemoveAll(x => _filterService.CoursesExcluded.Contains(x.Name));
            CompleteCourseData.RemoveAll(x => _filterService.CourseYearsExcluded.Contains(x.Year));
            CompleteCourseData.RemoveAll(x => _filterService.CourseSemestersExcluded.Contains(x.Semester));
            foreach (Course c in CompleteCourseData)
            {
                c.Assessments.RemoveAll(x => _filterService.AssessmentCourseExcluded.Contains(x.Name));
            }





            await RefreshTotalNumberOfFeedbackItems();
            await RefreshAverageWordsInFeedback();
            await RefreshMinimumWordsInFeedback();
            await RefreshMaximumWordsInFeedback();
            await RefreshStandardDeviationWordsInFeedback();
            await RefreshTotalCoursesInCollection();
            await RefreshOverallCourseEmotionChartData();
            await RefreshSentimentCourseChart();
            await RefreshCourseGradeDistributionChartData();
            await RefreshEmotionCourseChart();
            await RefreshYearlyAverageCourseGradeDistributionChartData();
            await RefreshYearlyAverageCourseFeedbackWordsChartData();
            
            ready = true;
        }

        //Refresh the data for the data boxes at the top of the dashboard
        public async Task<Task> RefreshTotalNumberOfFeedbackItems()
        {
            await Task.Yield();
            TotalNumberOfFeedbackItems = CompleteCourseData.SelectMany(c => c.Assessments).SelectMany(a => a.Feedback).Count();

            return Task.CompletedTask;
        }
        public async Task<Task> RefreshTotalCoursesInCollection()
        {
            await Task.Yield();
            TotalNumberOfCourses = CompleteCourseData.Count();
            return Task.CompletedTask;
        }
        public async Task<Task> RefreshAverageWordsInFeedback()
        {
            await Task.Yield();
            var items = CompleteCourseData.SelectMany(c => c.Assessments).SelectMany(a => a.Feedback).ToList();
            int totalWords = 0;
            foreach (var item in items)
            {
                totalWords += item.FeedbackToLearner.Split(' ').Length;
            }
            if (TotalNumberOfFeedbackItems != 0)
                AverageWordsInFeedback = Math.Round((decimal)totalWords / (decimal)TotalNumberOfFeedbackItems, 2);
            else
                AverageWordsInFeedback = 0;
            return Task.CompletedTask;
        }
        public async Task<Task> RefreshMinimumWordsInFeedback()
        {
            await Task.Yield();
            var items = CompleteCourseData.SelectMany(c => c.Assessments).SelectMany(a => a.Feedback).ToList();
            int minWords = int.MaxValue;
            if (items.Count == 0)
                minWords = 0;
            else
                foreach (var item in items)
                {
                    if (item.FeedbackToLearner.Split(' ').Length < minWords)
                    {
                        minWords = item.FeedbackToLearner.Split(' ').Length;
                    }
                }
            MinWordsInFeedback = minWords;
            return Task.CompletedTask;
        }
        public async Task<Task> RefreshMaximumWordsInFeedback()
        {
            await Task.Yield();
            var items = CompleteCourseData.SelectMany(c => c.Assessments).SelectMany(a => a.Feedback).ToList();
            int maxWords = int.MinValue;
            if (items.Count == 0)
                maxWords = 0;
            else
                foreach (var item in items)
                {
                    if (item.FeedbackToLearner.Split(' ').Length > maxWords)
                    {
                        maxWords = item.FeedbackToLearner.Split(' ').Length;
                    }
                }
            MaxWordsInFeedback = maxWords;
            return Task.CompletedTask;
        }
        public async Task RefreshStandardDeviationWordsInFeedback()
        {
            await Task.Yield();
            var items = CompleteCourseData.SelectMany(c => c.Assessments).SelectMany(a => a.Feedback).ToList();

            if (items.Count == 0)
            {
                StandardDeviationWordsInFeedback = 0;
                return;
            }

            // Calculate the average (mean) number of words
            int totalWords = items.Sum(item => item.FeedbackToLearner.Split(' ').Length);
            decimal mean = (decimal)totalWords / items.Count;

            // Calculate the variance
            decimal sumOfSquares = items.Sum(item =>
            {
                int wordCount = item.FeedbackToLearner.Split(' ').Length;
                return (wordCount - mean) * (wordCount - mean);
            });

            decimal variance = sumOfSquares / items.Count;

            // Calculate the standard deviation
            StandardDeviationWordsInFeedback = Math.Round((decimal)Math.Sqrt((double)variance), 2);
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
           foreach(var course in CompleteCourseData)
                foreach(var assessment in course.Assessments)
                    foreach(var feedback in assessment.Feedback)
                        foreach(var analysis in feedback.Analysis)
                        {
                            if (analysis.AnalysisType == "emotion")
                            {
                                string firstCaptial = analysis.Label.Substring(0, 1).ToUpper() + analysis.Label.Substring(1,analysis.Label.Length-1);
                                emotionSums[firstCaptial] += analysis.Value;
                            }
                        }


            //// Sum up all emotion values from the source data
            //foreach (var data in EmotionCourseData)
            //{
            //    emotionSums["Joy"] += data.Joy;
            //    emotionSums["Surprise"] += data.Surprise;
            //    emotionSums["Neutral"] += data.Neutral;
            //    emotionSums["Sadness"] += data.Sadness;
            //    emotionSums["Anger"] += data.Anger;
            //    emotionSums["Disgust"] += data.Disgust;
            //    emotionSums["Fear"] += data.Fear;
            //}

            // Convert dictionary back to list
            var overallDataList = emotionSums.Select(kvp => new EmotionOverallCourseData
            {
                Emotion = kvp.Key,
                Value = kvp.Value
            }).ToList();

            OverallEmotionData = overallDataList;

        }
        //Refreshes the data for the individual courses
        public async Task<List<EmotionCourseData>> RefreshEmotionCourseChart()
        {
            await Task.Yield();
            List<EmotionCourseData> lstItems = new List<EmotionCourseData>();

            foreach (var course in CompleteCourseData)
            {
                EmotionCourseData tnp = new EmotionCourseData();
                tnp.Course = course.CourseCode;

                int feedbackCount = 0; // Counter for feedback records

                foreach (var assessment in course.Assessments)
                {
                    foreach (var feedback in assessment.Feedback)
                    {
                        feedbackCount++; // Increment feedback counter

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

            foreach (var course in CompleteCourseData)
            {
                SentimentCourseData tnp = new SentimentCourseData();
                tnp.Course = course.CourseCode;
                int posCount = 0;
                int negCount = 0;
                int neuCount = 0;
                foreach (var assessment in course.Assessments)
                {
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
            SentimentCourseData = mergedData;
            return SentimentCourseData;
        }
        //Refreshes the data in the grade distribution chart overall
        public async Task<List<GradeDistribution>> RefreshCourseGradeDistributionChartData()
        {
            await Task.Yield();
            //CourseGradeDistribution = new List<GradeDistribution>();
            var gradeSums = new Dictionary<string, decimal>();

            // Populate the dictionary with predefined buckets
            var gradeRanges = new[] { "0-50%", "51-59%", "60-69%", "70-79%", "80-89%", "90-100%" };
            foreach (var emotion in gradeRanges)
            {
                gradeSums[emotion] = 0;
            }
            decimal MaxScore = 0;
            // Sum up all emotion values from the source data
            foreach (var course in CompleteCourseData)
            {
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
                        else if (studentGrade <= 1.0m)
                        {
                            gradeSums["90-100%"] += 1;
                        }
                    }
                }
            }
            var overallDataList = gradeSums.Select(kvp => new GradeDistribution
            {
                Bucket = kvp.Key,
                Value = kvp.Value
            }).ToList();
            CourseGradeDistribution = overallDataList;
            return overallDataList;
        }
        //Refreshes the data in the yearly grade distribution chart by course
        public async Task<List<YearlyCourseGrades>> RefreshYearlyAverageCourseGradeDistributionChartData()
        {
            await Task.Yield();
            CourseAverageGradeDistribution = new List<YearlyCourseGrades>();
            List<YearlyCourseGrades> yearlyCourseGrades = new List<YearlyCourseGrades>();

            //add the grade received on each feedback item to the course's total
            foreach (var course in CompleteCourseData)
            {
                foreach (var assignment in course.Assessments)
                {
                    foreach (var feedback in assignment.Feedback)
                    {
                        YearlyCourseGrades tnp = new YearlyCourseGrades();
                        tnp.Course = course.CourseCode;
                        if (assignment.MaxScore == 0)
                            tnp.Grade = 0;
                        else
                            tnp.Grade = (feedback.Grade / assignment.MaxScore);
                        tnp.Year = course.Year;
                        yearlyCourseGrades.Add(tnp);
                    }
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
                CourseAverageGradeDistribution.Add(grd);
            }
            return CourseAverageGradeDistribution;
        }
        //Refreshes the number of words in feedback per course over years
        public async Task<List<YearlyCourseFeedbackWords>> RefreshYearlyAverageCourseFeedbackWordsChartData()
        {
            await Task.Yield();
            YearlyCourseFeedbackWords = new List<YearlyCourseFeedbackWords>();
            List<YearlyCourseFeedbackWords> yearlyCourseFeedback = new List<YearlyCourseFeedbackWords>();

            //add the grade received on each feedback item to the course's total
            foreach (var course in CompleteCourseData)
            {
                foreach (var assignment in course.Assessments)
                {
                    foreach (var feedback in assignment.Feedback)
                    {
                        YearlyCourseFeedbackWords tnp = new YearlyCourseFeedbackWords();
                        tnp.Course = course.CourseCode;

                        if (feedback.FeedbackToLearner.Length ==0 || feedback.FeedbackToLearner.Trim() == "")
                        {
                            tnp.WordCount = 0;
                        }
                        else
                        {
                            tnp.WordCount = feedback.FeedbackToLearner.Split(' ').Length;
                        }
                        tnp.Year = course.Year;
                        tnp.Course = course.CourseCode;
                        yearlyCourseFeedback.Add(tnp);
                    }
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
            YearlyCourseFeedbackWords = averageWordCounts;
            return YearlyCourseFeedbackWords;
        }

    }

}

