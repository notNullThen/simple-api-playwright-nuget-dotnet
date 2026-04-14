using Microsoft.Playwright;

namespace SimpleApiPlaywright.Core.Types;

public class ApiResponse<T>
{
    public required IAPIResponse Response;
    public T? ResponseBody;
}
