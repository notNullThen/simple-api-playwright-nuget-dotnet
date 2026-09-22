namespace SimpleApiPlaywright;

/// <summary>
/// Stores an API authorization token within the current asynchronous execution flow.
/// </summary>
public static class TokenStorage
{
    private class TokenContainer
    {
        public string? Token { get; set; }
    }

    private static readonly AsyncLocal<TokenContainer> _apiToken = new();

    /// <summary>
    /// Initializes an empty token container for the current asynchronous execution flow.
    /// </summary>
    public static void Init()
    {
        _apiToken.Value = new TokenContainer();
    }

    /// <summary>
    /// Sets the token for the current asynchronous execution flow.
    /// </summary>
    /// <param name="token">The token value to store.</param>
    public static void Set(string token)
    {
        _apiToken.Value ??= new TokenContainer();

        _apiToken.Value.Token = token;
    }

    /// <summary>
    /// Gets the token for the current asynchronous execution flow.
    /// </summary>
    /// <returns>The stored token, or <see langword="null"/> when no token has been set.</returns>
    public static string? Get() => _apiToken.Value?.Token;
}
