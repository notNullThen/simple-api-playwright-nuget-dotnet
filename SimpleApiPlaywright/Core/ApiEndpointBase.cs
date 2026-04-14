using OwaspPlaywrightTests.Base.ApiClient.Types;

namespace OwaspPlaywrightTests.Base.ApiClient;

public abstract class ApiEndpointBase(string apiBaseUrl)
{
    public ApiAction<T> Action<T>(RequestParameters parameters) => new(apiBaseUrl, parameters);
}
