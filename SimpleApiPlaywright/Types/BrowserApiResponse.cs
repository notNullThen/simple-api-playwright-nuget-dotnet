using Microsoft.Playwright;

namespace SimpleApiPlaywright.Types;

public sealed class BrowserApiResponse<T>
{
    public required IResponse Response;
    public T? ResponseBody;
}
