namespace SimpleApiPlaywright.Core.Types;

public class RequestParameters
{
    public string? Url { get; set; }
    public ApiHttpMethod Method;
    public int[]? ExpectedStatusCodes { get; set; }
    public object? Body { get; set; }
    public int? apiWaitTimeout;
}
