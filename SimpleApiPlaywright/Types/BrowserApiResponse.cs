using Microsoft.Playwright;

namespace SimpleApiPlaywright.Types;

/// <summary>
/// Contains a response observed on a Playwright page and its optional deserialized body.
/// </summary>
/// <typeparam name="T">The response body type.</typeparam>
public sealed class BrowserApiResponse<T>
{
    /// <summary>
    /// The underlying Playwright browser response.
    /// </summary>
    public required IResponse Response;

    /// <summary>
    /// The deserialized response body, or <see langword="null"/> when no body was requested.
    /// </summary>
    public T? ResponseBody;
}
