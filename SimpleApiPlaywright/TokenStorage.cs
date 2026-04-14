namespace SimpleApiPlaywright;

public static class TokenStorage
{
    private class TokenContainer
    {
        public string? Token { get; set; }
    }

    private static readonly AsyncLocal<TokenContainer> _apiToken = new();

    public static void Init()
    {
        _apiToken.Value = new TokenContainer();
    }

    public static void Set(string token)
    {
        _apiToken.Value ??= new TokenContainer();

        _apiToken.Value.Token = token;
    }

    public static string? Get() => _apiToken.Value?.Token;
}
