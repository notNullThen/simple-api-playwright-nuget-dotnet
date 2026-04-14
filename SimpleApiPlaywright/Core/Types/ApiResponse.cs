using Microsoft.Playwright;

namespace OwaspPlaywrightTests.Base.ApiClient.Types;

public class ApiResponse<T>
{
    public required IAPIResponse Response;
    public T? ResponseBody;
}
