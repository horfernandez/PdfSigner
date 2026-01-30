namespace PdfSigner.Core.Models;

public sealed class SignRequest
{
    public string InputPath { get; init; } = "";
    public string OutputPath { get; init; } = "";

    // Página 1-based (más amigable para humanos)
    public int PageNumber { get; init; } = 1;

    public SignRect Rect { get; init; } = new();

    // Para Store de Windows: Thumbprint
    public string CertificateThumbprint { get; init; } = "";

    // Opcional para mostrar texto / nombre visible en firma
    public string Reason { get; init; } = "Firmado digitalmente";
}
