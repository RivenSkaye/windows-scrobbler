namespace Scrobbler.Util;

public static class AsyncHelper
{
    /// <summary>
    /// Retry a function until it returns true
    /// </summary>
    /// <param name="func">The function to try</param>
    /// <param name="retryTimes">Max number of times to retry. If 0, infinite</param>
    /// <param name="baseBackOff">The base number of milliseconds to back off. For a try N, the waittime will be between <c>baseBackOff</c> and <c>min(baseBackOff &lt;&lt; N, maxBackOff)</c> </param>
    /// <returns>true if the function succeeded, false if the function failed the maximum allowed times</returns>
    public static async Task<bool> RetryExponentialBackOffAsync(Func<Task<bool>> func, int retryTimes = 0, int baseBackOff = 1000, int maxBackOff = 16000)
    {
        for (var i = 0; retryTimes == 0 || i < retryTimes; i++)
        {
            if (await func())
                return true;

            await Task.Delay(Math.Min(baseBackOff << i, maxBackOff));
        }

        return false;
    }
}