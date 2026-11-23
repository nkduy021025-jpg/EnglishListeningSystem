namespace EnglishListeningSystem.Models;

public sealed record StudentLesson(
    int Id, string Title, string Level, int SentenceCount, double AverageRating, int RatingCount);

public sealed record StudentLessonsViewModel(
    int CategoryId,
    string CategoryName,
    string Search,
    string Level,
    IReadOnlyList<string> Levels,
    IReadOnlyList<StudentLesson> Lessons);
