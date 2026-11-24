namespace EnglishListeningSystem.Models
{
    public sealed class StudentLesson
    {
        public int Id { get; }
        public string Title { get; }
        public string Level { get; }
        public int SentenceCount { get; }
        public double AverageRating { get; }
        public int RatingCount { get; }

        public StudentLesson(
            int id,
            string title,
            string level,
            int sentenceCount,
            double averageRating,
            int ratingCount)
        {
            Id = id;
            Title = title;
            Level = level;
            SentenceCount = sentenceCount;
            AverageRating = averageRating;
            RatingCount = ratingCount;
        }
    }

    public sealed class StudentLessonsViewModel
    {
        public int CategoryId { get; }
        public string CategoryName { get; }
        public string Search { get; }
        public string Level { get; }
        public IReadOnlyList<string> Levels { get; }
        public IReadOnlyList<StudentLesson> Lessons { get; }

        public StudentLessonsViewModel(
            int categoryId,
            string categoryName,
            string search,
            string level,
            IReadOnlyList<string> levels,
            IReadOnlyList<StudentLesson> lessons)
        {
            CategoryId = categoryId;
            CategoryName = categoryName;
            Search = search;
            Level = level;
            Levels = levels;
            Lessons = lessons;
        }
    }
}
