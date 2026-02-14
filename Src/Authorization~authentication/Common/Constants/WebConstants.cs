namespace Authorization_authentication.Common.Constants;

/// <summary>
///     Constants for web routing
/// </summary>
public static class WebConstants
{
    /// <summary>
    ///     Route segment for API version in URL: v{version:apiVersion}
    /// </summary>
    public const string ApiVersionSegment = "v{version:apiVersion}";

    /// <summary>
    ///     Base route template for versioned API controllers: api/v{version:apiVersion}/[controller]
    /// </summary>
    public const string ApiControllerRoute = "api/" + ApiVersionSegment + "/[controller]";
}
