using Microsoft.Playwright;

namespace SimpleApiPlaywright.Core.Types;

public class BrowserApiResponse<T>
{
    public required IResponse Response;
    public T? ResponseBody;
}
