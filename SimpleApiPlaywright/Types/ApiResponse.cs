using Microsoft.Playwright;

namespace SimpleApiPlaywright.Types;

public sealed class ApiResponse<T>
{
    public required IAPIResponse Response;
    public T? ResponseBody;
}
