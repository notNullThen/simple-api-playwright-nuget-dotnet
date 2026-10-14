using Microsoft.Playwright;

namespace SimpleApiPlaywright.Types;

/// <summary>
/// Owns a fetched API response that has not yet been delivered to the browser.
/// Use <see cref="ReleaseAsync"/> to deliver it, or dispose it to abort the pending request.
/// </summary>
public sealed class ApiResponse : IAsyncDisposable
{
    private readonly IRoute _route;
    private readonly TimeSpan _timeout;
    private readonly Func<Task> _removeRoute;
    private readonly TaskCompletionSource _completed = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private IAPIResponse? _heldResponse;

    internal ApiResponse(
        IRoute route,
        IAPIResponse response,
        TimeSpan timeout,
        Func<Task> removeRoute
    )
    {
        _route = route;
        _heldResponse = response;
        Response = response;
        _timeout = timeout;
        _removeRoute = removeRoute;
    }

    /// <summary>
    /// The fetched server response. Its body can be read until this handle is released or disposed.
    /// </summary>
    public IAPIResponse Response { get; }

    /// <summary>
    /// The intercepted browser request.
    /// </summary>
    public IRequest Request => _route.Request;

    /// <summary>
    /// The delivered browser response, populated by a successful strict release.
    /// </summary>
    public IResponse? BrowserResponse { get; private set; }

    internal Task Completion => _completed.Task;

    /// <summary>
    /// Delivers the fetched response and removes this handle's route.
    /// </summary>
    /// <param name="failIfNotReleased">
    /// When true, also waits for successful browser delivery and completion within the configured
    /// API timeout. The default permits a request that the browser has already canceled.
    /// </param>
    /// <returns>The release and cleanup operation.</returns>
    public async Task ReleaseAsync(bool failIfNotReleased = false)
    {
        var heldResponse =
            Interlocked.Exchange(ref _heldResponse, null)
            ?? throw new InvalidOperationException(
                "The response has already been released or disposed."
            );
        try
        {
            await _route.FulfillAsync(new() { Response = heldResponse });
            if (failIfNotReleased)
            {
                BrowserResponse =
                    await Request.ResponseAsync().WaitAsync(_timeout)
                    ?? throw new PlaywrightException(
                        $"The held response was not delivered: {Request.Failure}"
                    );
                await BrowserResponse.FinishedAsync().WaitAsync(_timeout);
                if (Request.Failure != null)
                {
                    throw new PlaywrightException($"The held request failed: {Request.Failure}");
                }
            }
        }
        catch (PlaywrightException) when (!failIfNotReleased && Request.Failure != null)
        {
            // The browser may cancel a request while its response is held.
        }
        finally
        {
            await CleanupAsync(heldResponse);
        }
    }

    /// <summary>
    /// Aborts the request and cleans up if its response has not been released.
    /// </summary>
    /// <returns>The cleanup operation.</returns>
    public async ValueTask DisposeAsync()
    {
        var heldResponse = Interlocked.Exchange(ref _heldResponse, null);
        if (heldResponse == null)
        {
            return;
        }

        try
        {
            await _route.AbortAsync();
        }
        finally
        {
            await CleanupAsync(heldResponse);
        }
    }

    private async Task CleanupAsync(IAPIResponse heldResponse)
    {
        _completed.TrySetResult();
        try
        {
            await heldResponse.DisposeAsync();
        }
        finally
        {
            await _removeRoute();
        }
    }
}
