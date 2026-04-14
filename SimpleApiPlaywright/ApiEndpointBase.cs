using SimpleApiPlaywright.Types;

namespace SimpleApiPlaywright;

public abstract class ApiEndpointBase(string apiBaseUrl)
{
    public ApiAction<T> Action<T>(RequestParameters parameters) => new(apiBaseUrl, parameters);
}
