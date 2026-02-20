using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Features.FileUpload.Models;

 public sealed record PreparedFile(
    Stream Stream,
    string Mime,
    string ObjectName,
    IActionResult? Error = null)
{
    public static PreparedFile Fail(IActionResult error) =>
        new(Stream.Null, string.Empty, string.Empty, error);
}
