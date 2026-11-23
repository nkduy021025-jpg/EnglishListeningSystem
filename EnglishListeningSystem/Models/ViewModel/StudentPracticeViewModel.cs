namespace EnglishListeningSystem.Models;

public sealed record DictationSentence(string Text, double Start, double End);

public sealed record StudentPracticeViewModel(
    int LessonId,
    int CategoryId,
    string CategoryName,
    string Title,
    string Level,
    string? AudioUrl,
    string? VideoUrl,
    IReadOnlyList<DictationSentence> Sentences,
    int SentencePosition);
