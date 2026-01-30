using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using PdfSigner.Core.Models;

using iText.Bouncycastleconnector;
using iText.Commons.Bouncycastle;
using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Pdf;
using iText.Signatures;

namespace PdfSigner.Core.Services;

public sealed class PdfSigningService : IPdfSigningService
{
    public SignResult Sign(SignRequest request)
    {
        try
        {
            // Validaciones mínimas
            if (string.IsNullOrWhiteSpace(request.InputPath) || !File.Exists(request.InputPath))
                return SignResult.Fail($"No existe el PDF de entrada: {request.InputPath}");

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return SignResult.Fail("OutputPath vacío.");

            if (string.IsNullOrWhiteSpace(request.CertificateThumbprint))
                return SignResult.Fail("CertificateThumbprint vacío.");

            var cert = FindCertificateByThumbprint(request.CertificateThumbprint);
            if (cert is null)
                return SignResult.Fail($"No se encontró el certificado: {request.CertificateThumbprint}");

            if (!cert.HasPrivateKey)
                return SignResult.Fail("El certificado no tiene clave privada (HasPrivateKey = false).");

            if (cert.NotAfter <= DateTime.Now)
                return SignResult.Fail($"Certificado vencido: {cert.NotAfter:yyyy-MM-dd HH:mm}");

            // Asegurar carpeta destino
            var outDir = System.IO.Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrWhiteSpace(outDir))
                Directory.CreateDirectory(outDir);

            // iText BC factory (solo para armar IX509Certificate[])
            IBouncyCastleFactory bc = BouncyCastleFactoryCreator.GetFactory();

            // Convertimos cert chain .NET -> iText IX509Certificate[]
            IX509Certificate[] chain = BuildITextChain(cert, bc);

            // Clave privada RSA desde el certificado
            using RSA? rsa = cert.GetRSAPrivateKey();
            if (rsa is null)
                return SignResult.Fail("La clave privada no es RSA o no es accesible (GetRSAPrivateKey devolvió null).");

            // iText: digest + firma externa (RSA .NET)
            IExternalDigest digest = new BouncyCastleDigest();
            IExternalSignature signature = new DotNetRsaSignature(rsa, "SHA256");

            using var reader = new PdfReader(request.InputPath);
            using var os = new FileStream(request.OutputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var signer = new iText.Signatures.PdfSigner(reader, os, new StampingProperties().UseAppendMode());

            // Firma invisible (detached)
            signer.SignDetached(
                digest,
                signature,
                chain,
                null,
                null,
                null,
                0,
                iText.Signatures.PdfSigner.CryptoStandard.CADES
            );

            return SignResult.Success(request.OutputPath, "PDF firmado (firma invisible).");
        }
        catch (Exception ex)
        {
            return SignResult.Fail("Error firmando el PDF.", ex);
        }
    }

    private static X509Certificate2? FindCertificateByThumbprint(string thumbprint)
    {
        var tp = thumbprint.Replace(" ", "").Trim().ToUpperInvariant();

        X509Certificate2? TryFind(StoreLocation loc)
        {
            using var store = new X509Store(StoreName.My, loc);
            store.Open(OpenFlags.ReadOnly);

            foreach (var c in store.Certificates)
            {
                var ctp = (c.Thumbprint ?? "").Replace(" ", "").Trim().ToUpperInvariant();
                if (ctp == tp) return c;
            }

            return null;
        }

        return TryFind(StoreLocation.CurrentUser) ?? TryFind(StoreLocation.LocalMachine);
    }

    private static IX509Certificate[] BuildITextChain(X509Certificate2 leaf, IBouncyCastleFactory bc)
    {
        var chain = new X509Chain
        {
            ChainPolicy =
            {
                RevocationMode = X509RevocationMode.NoCheck,
                RevocationFlag = X509RevocationFlag.ExcludeRoot
            }
        };

        chain.Build(leaf);

        var list = new List<IX509Certificate>();

        foreach (var element in chain.ChainElements)
            list.Add(bc.CreateX509Certificate(element.Certificate.RawData));

        if (list.Count == 0)
            list.Add(bc.CreateX509Certificate(leaf.RawData));

        return list.ToArray();
    }

    private sealed class DotNetRsaSignature : iText.Signatures.IExternalSignature
    {
        private readonly RSA _rsa;
        private readonly string _digest;

        public DotNetRsaSignature(RSA rsa, string digestAlgorithm = "SHA256")
        {
            _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
            _digest = digestAlgorithm ?? throw new ArgumentNullException(nameof(digestAlgorithm));
        }

        public string GetDigestAlgorithmName() => _digest;

        public string GetSignatureAlgorithmName() => "RSA";

        public ISignatureMechanismParams GetSignatureMechanismParameters()
        {
            // RSA PKCS#1 no requiere parámetros extra
            return null!;
        }

        public byte[] Sign(byte[] message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            // iText nos pasa "message" para firmar; usamos SHA256 + PKCS#1
            return _rsa.SignData(
                message,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
        }
    }
}
