using SimpleApiPlaywright.Types;

namespace SimpleApiPlaywright;

/// <summary>
/// Provides a base class for grouping typed API operations under a shared API base URL.
/// </summary>
/// <param name="apiBaseUrl">The API-specific base URL or path used by created actions.</param>
public abstract class ApiEndpointBase(string apiBaseUrl)
{
    /// <summary>
    /// Creates a typed API action using this endpoint's base URL.
    /// </summary>
    /// <typeparam name="T">The expected response body type.</typeparam>
    /// <param name="parameters">The request and response-matching configuration.</param>
    /// <returns>A configured API action.</returns>
    public ApiAction<T> Action<T>(RequestParameters parameters) => new(apiBaseUrl, parameters);
}
