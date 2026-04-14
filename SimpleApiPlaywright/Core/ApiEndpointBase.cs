using Microsoft.Playwright;
using SimpleApiPlaywright.Core.Types;

namespace SimpleApiPlaywright.Core;

public abstract class ApiEndpointBase(
    string apiBaseUrl,
    IPage? page = null,
    IAPIRequestContext? context = null
)
{
    public ApiAction<T> Action<T>(RequestParameters parameters) =>
        new(apiBaseUrl, parameters, page, context);
}
