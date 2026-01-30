namespace PdfSigner.Core.Services;

public sealed class CertificateItem
{
    public string Subject { get; init; } = "";
    public string Thumbprint { get; init; } = "";
    public DateTime NotAfter { get; init; }

    public override string ToString()
        => $"{Subject} (vence {NotAfter:yyyy-MM-dd})";
}

public interface ICertificateService
{
    List<CertificateItem> GetSigningCertificates();
}
