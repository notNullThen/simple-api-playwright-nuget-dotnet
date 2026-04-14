using Microsoft.Playwright;
using SimpleApiPlaywright.Core.Types;

namespace SimpleApiPlaywright.Core;

public abstract class ApiEndpointBase(string apiBaseUrl)
{
    public ApiAction<T> Action<T>(RequestParameters parameters) => new(apiBaseUrl, parameters);
}
