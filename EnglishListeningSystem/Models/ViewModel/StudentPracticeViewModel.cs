namespace EnglishListeningSystem.Models
{
    public sealed class DictationSentence
    {
        public string Text { get; }
        public double Start { get; }
        public double End { get; }

        public DictationSentence(string text, double start, double end)
        {
            Text = text;
            Start = start;
            End = end;
        }
    }

    public sealed class StudentPracticeViewModel
    {
        public int LessonId { get; }
        public int CategoryId { get; }
        public string CategoryName { get; }
        public string Title { get; }
        public string Level { get; }
        public string? AudioUrl { get; }
        public string? VideoUrl { get; }
        public IReadOnlyList<DictationSentence> Sentences { get; }
        public int SentencePosition { get; }

        public StudentPracticeViewModel(
            int lessonId,
            int categoryId,
            string categoryName,
            string title,
            string level,
            string? audioUrl,
            string? videoUrl,
            IReadOnlyList<DictationSentence> sentences,
            int sentencePosition)
        {
            LessonId = lessonId;
            CategoryId = categoryId;
            CategoryName = categoryName;
            Title = title;
            Level = level;
            AudioUrl = audioUrl;
            VideoUrl = videoUrl;
            Sentences = sentences;
            SentencePosition = sentencePosition;
        }
    }
}
