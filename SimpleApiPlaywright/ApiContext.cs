using Microsoft.Playwright;

namespace SimpleApiPlaywright;

/// <summary>
/// Holds the Playwright objects used to send API requests and observe browser responses.
/// </summary>
public sealed class ApiContext
{
    private readonly IBrowserContext _browserContext;
    private readonly IAPIRequestContext? _context;
    private readonly IPage? _page;

    /// <summary>
    /// Initializes a Playwright API context.
    /// </summary>
    /// <param name="browserContext">The browser context used for tracing.</param>
    /// <param name="page">
    /// The page used to observe browser responses. Required when using
    /// <see cref="ApiClient.WaitAsync{T}"/>.
    /// </param>
    /// <param name="context">
    /// The request context used for direct API calls. When omitted, <paramref name="page"/>'s
    /// request context is used.
    /// </param>
    /// <exception cref="PlaywrightException">
    /// Thrown when neither <paramref name="page"/> nor <paramref name="context"/> is provided.
    /// </exception>
    public ApiContext(
        IBrowserContext browserContext,
        IPage? page = null,
        IAPIRequestContext? context = null
    )
    {
        if (page == null && context == null)
        {
            throw new PlaywrightException(
                $"You need to provide at least '{nameof(page)}' or '{nameof(context)}' parameters to create an instance of '{nameof(ApiClient)}'."
            );
        }

        _browserContext = browserContext;
        _context = context ?? page!.APIRequest;
        _page = page;
    }

    /// <summary>
    /// Gets the browser context used for tracing.
    /// </summary>
    public IBrowserContext BrowserContext => _browserContext;

    /// <summary>
    /// Gets the request context used for direct API calls.
    /// </summary>
    public IAPIRequestContext Context => _context!;

    /// <summary>
    /// Gets the page used to observe browser responses, if one was provided.
    /// </summary>
    public IPage? Page => _page;
}
