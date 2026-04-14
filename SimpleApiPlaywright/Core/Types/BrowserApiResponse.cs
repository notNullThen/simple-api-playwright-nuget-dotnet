using Microsoft.Playwright;

namespace OwaspPlaywrightTests.Base.ApiClient.Types;

public class BrowserApiResponse<T>
{
    public required IResponse Response;
    public T? ResponseBody;
}
