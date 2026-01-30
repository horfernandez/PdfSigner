namespace PdfSigner.Core.Models;

public sealed class SignResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
    public string? OutputPath { get; init; }
    public Exception? Exception { get; init; }

    public static SignResult Success(string outputPath, string message = "OK")
        => new() { Ok = true, OutputPath = outputPath, Message = message };

    public static SignResult Fail(string message, Exception? ex = null)
        => new() { Ok = false, Message = message, Exception = ex };
}
