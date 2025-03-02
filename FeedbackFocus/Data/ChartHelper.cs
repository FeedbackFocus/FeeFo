using Blazorise.Charts;

namespace FeedbackFocus.Data
{
    public static class ChartHelper
    {
        static ChartColor angerColor = ChartColor.FromRgba(153, 102, 255, 0.8f);
        static ChartColor fearColor = ChartColor.FromRgba(255, 70, 100, 1f);
        static ChartColor surpriseColor = ChartColor.FromRgba(54, 162, 235, 0.8f);
        static ChartColor neutralColor = ChartColor.FromRgba(255, 206, 86, 0.8f);
        static ChartColor joyColor = ChartColor.FromRgba(255, 99, 132, 0.8f);
        static ChartColor sadnessColor = ChartColor.FromRgba(75, 192, 192, 0.8f);
        static ChartColor disgustColor = ChartColor.FromRgba(255, 159, 64, 1f);
        public static ChartColor[] EmotionColors = new ChartColor[] { angerColor, fearColor, surpriseColor, neutralColor, joyColor, sadnessColor, disgustColor };
    }
}
