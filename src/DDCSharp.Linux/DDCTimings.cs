namespace DDCSharp.Linux;

/// <summary>
/// Delays and retries used for DDC/CI over I2C. Defaults follow the DDC/CI specification;
/// some displays work with shorter delays and some need longer ones.
/// </summary>
public sealed record DDCTimings
{
    /// <summary>Default timings from the DDC/CI specification.</summary>
    public static DDCTimings Default { get; } = new();

    /// <summary>Wait between sending a Get VCP Feature request and reading the reply.</summary>
    public TimeSpan GetReplyDelay { get; init; } = TimeSpan.FromMilliseconds(40);

    /// <summary>Wait between sending a capabilities request and reading the reply.</summary>
    public TimeSpan CapabilitiesReplyDelay { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Minimum idle time on the bus between the end of one command and the start of the next.</summary>
    public TimeSpan CommandInterval { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Extra wait before retrying a failed command.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>How many times a command is tried before giving up.</summary>
    public int MaxAttempts { get; init; } = 3;
}
