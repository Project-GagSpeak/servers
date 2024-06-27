namespace GagspeakAuthentication;

/// <summary>
/// Internal record that keeps track of the number of failed attempts to authorize a secret key.
/// </summary>
internal record SecretKeyFailedAuthorization
{
    /// <summary>
    /// This field keeps track of the number of failed attempts
    /// </summary>
    private int failedAttempts = 1;

    /// <summary>
    /// This property exposes the number of failed attempts
    /// </summary>
    public int FailedAttempts => failedAttempts;

    /// <summary>
    /// This property holds a Task that can be used to reset the failed attempts
    /// </summary>
    public Task ResetTask { get; set; }

    /// <summary>
    /// This method increases the number of failed attempts in a thread-safe manner
    /// </summary>
    public void IncreaseFailedAttempts()
    {
        // Interlocked.Increment is used to safely increment the failedAttempts field
        // even in a multi-threaded environment
        Interlocked.Increment(ref failedAttempts);
    }
}