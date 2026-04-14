using Microsoft.Playwright;
using OwaspPlaywrightTests.Base.ApiClient.Types;

namespace OwaspPlaywrightTests.Base.ApiClient;

public class ApiAction<T>
{
    private readonly IAPIRequestContext _context;
    private readonly IPage? _page;

    private readonly string _apiBaseUrl;
    private readonly RequestParameters _parameters;

    public ApiAction(string apiBaseUrl, RequestParameters parameters)
    {
        _apiBaseUrl = apiBaseUrl;
        _parameters = parameters;

        if (Test.Page == null && Test.Request == null)
        {
            throw new PlaywrightException(
                $"You need to provide at least '{nameof(Test.Page)}' or '{nameof(Test.Request)}' parameters to create an instance of '{nameof(ApiClient)}'."
            );
        }

        _context = Test.Request ?? Test.Page!.APIRequest;
        _page = Test.Page;
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
