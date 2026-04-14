using System.Runtime.Serialization;

namespace SimpleApiPlaywright.Core.Types;

public enum ApiHttpMethod
{
    [EnumMember(Value = "GET")]
    GET,

    [EnumMember(Value = "POST")]
    POST,

    [EnumMember(Value = "PUT")]
    PUT,

    [EnumMember(Value = "DELETE")]
    DELETE,

    [EnumMember(Value = "HEAD")]
    HEAD,

    [EnumMember(Value = "PATCH")]
    PATCH,
}
