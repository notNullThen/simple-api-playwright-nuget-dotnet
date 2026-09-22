using System.Runtime.Serialization;

namespace SimpleApiPlaywright.Types;

/// <summary>
/// Defines the HTTP methods supported by an API request or response wait.
/// </summary>
public enum ApiHttpMethod
{
    /// <summary>
    /// Requests a representation of the target resource.
    /// </summary>
    [EnumMember(Value = "GET")]
    GET,

    /// <summary>
    /// Submits data to the target resource.
    /// </summary>
    [EnumMember(Value = "POST")]
    POST,

    /// <summary>
    /// Replaces the target resource with the supplied representation.
    /// </summary>
    [EnumMember(Value = "PUT")]
    PUT,

    /// <summary>
    /// Deletes the target resource.
    /// </summary>
    [EnumMember(Value = "DELETE")]
    DELETE,

    /// <summary>
    /// Requests the target resource's headers without its response body.
    /// </summary>
    [EnumMember(Value = "HEAD")]
    HEAD,

    /// <summary>
    /// Applies a partial modification to the target resource.
    /// </summary>
    [EnumMember(Value = "PATCH")]
    PATCH,
}
