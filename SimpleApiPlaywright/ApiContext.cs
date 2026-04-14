using Microsoft.Playwright;

namespace SimpleApiPlaywright;

public sealed class ApiContext
{
    private readonly IBrowserContext _browserContext;
    private readonly IAPIRequestContext? _context;
    private readonly IPage? _page;

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

    public IBrowserContext BrowserContext => _browserContext;
    public IAPIRequestContext Context => _context!;
    public IPage? Page => _page;
}
