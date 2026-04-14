using Microsoft.Playwright;
using SimpleApiPlaywright.Core;
using SimpleApiPlaywright.Core.Types;

namespace SimpleApiPlaywright.Core;

public class ApiAction<T>
{
    private readonly IAPIRequestContext _context;
    private readonly IPage? _page;

    private readonly string _apiBaseUrl;
    private readonly RequestParameters _parameters;

    public ApiAction(
        string apiBaseUrl,
        RequestParameters parameters,
        IPage? page,
        IAPIRequestContext? context
    )
    {
        _apiBaseUrl = apiBaseUrl;
        _parameters = parameters;

        if (page == null && context == null)
        {
            throw new PlaywrightException(
                $"You need to provide at least '{nameof(page)}' or '{nameof(context)}' parameters to create an instance of '{nameof(ApiClient)}'."
            );
        }

        _context = context ?? page!.APIRequest;
        _page = page;
    }

    public async Task<ApiResponse<T>> RequestAsync()
    {
        return await new ApiClient(_apiBaseUrl, _parameters).RequestAsync<T>(_context);
    }

    public async Task<BrowserApiResponse<T>> WaitAsync()
    {
        if (_page == null)
        {
            throw new PlaywrightException(
                $"You can use {nameof(WaitAsync)}() only in the context of UI Tests (The '{nameof(IPage)}' should be available)."
            );
        }

        return await new ApiClient(_apiBaseUrl, _parameters).WaitAsync<T>(_page);
    }
}
