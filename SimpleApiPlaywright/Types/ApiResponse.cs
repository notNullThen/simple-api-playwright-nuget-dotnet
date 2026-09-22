using Microsoft.Playwright;

namespace SimpleApiPlaywright.Types;

/// <summary>
/// Contains a direct Playwright API response and its optional deserialized body.
/// </summary>
/// <typeparam name="T">The response body type.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// The underlying Playwright API response.
    /// </summary>
    public required IAPIResponse Response;

    /// <summary>
    /// The deserialized response body, or <see langword="null"/> when no body was requested.
    /// </summary>
    public T? ResponseBody;
}
