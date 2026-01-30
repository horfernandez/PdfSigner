using PdfSigner.Core.Models;

namespace PdfSigner.Core.Services;

public interface IPdfSigningService
{
    SignResult Sign(SignRequest request);
}
