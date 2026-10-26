namespace UrlShortener.Orchestration.Reliability;

public class RetryPolicy
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; init; } = 2.0;
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
    public IReadOnlyList<Type> RetryableExceptions { get; init; } = [typeof(Exception)];

    public static readonly RetryPolicy Default = new();

    public static readonly RetryPolicy NoRetry = new() { MaxAttempts = 1 };

    public static readonly RetryPolicy Aggressive = new()
    {
        MaxAttempts = 5,
        InitialDelay = TimeSpan.FromMilliseconds(500),
        BackoffMultiplier = 1.5,
        MaxDelay = TimeSpan.FromSeconds(15)
    };

    public TimeSpan GetDelay(int attempt)
    {
        var delay = InitialDelay * Math.Pow(BackoffMultiplier, attempt - 1);
        return delay > MaxDelay ? MaxDelay : delay;
    }

    public bool ShouldRetry(Exception ex, int attemptNumber)
    {
        if (attemptNumber >= MaxAttempts) return false;
        return RetryableExceptions.Any(t => t.IsInstanceOfType(ex));
    }
}
