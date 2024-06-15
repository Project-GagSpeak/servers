// Namespace for the authentication service
namespace GagspeakServer.Authentication;

// This class represents a failed authorization attempt using a secret key
internal record SecretKeyFailedAuthorization
{
    // This field keeps track of the number of failed attempts
    private int failedAttempts = 1;

    // This property exposes the number of failed attempts
    public int FailedAttempts => failedAttempts;

    // This property holds a Task that can be used to reset the failed attempts
    public Task ResetTask { get; set; }

    // This method increases the number of failed attempts in a thread-safe manner
    public void IncreaseFailedAttempts()
    {
        // Interlocked.Increment is used to safely increment the failedAttempts field
        // even in a multi-threaded environment
        Interlocked.Increment(ref failedAttempts);
    }
}