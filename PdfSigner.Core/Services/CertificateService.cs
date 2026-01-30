using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PdfSigner.Core.Services
{
    public sealed class CertificateService : ICertificateService
    {
        public List<CertificateItem> GetSigningCertificates()
        {
            try
            {
                var now = DateTime.Now;
                var all = new List<CertificateItem>();

                // Local function to read certificates from a store location
                void ReadFrom(StoreLocation location)
                {
                    var store = new X509Store(StoreName.My, location);
                    try
                    {
                        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                        foreach (var cert in store.Certificates.Cast<X509Certificate2>())
                        {
                            try
                            {
                                if (!cert.HasPrivateKey)
                                    continue;

                                if (cert.NotAfter <= now)
                                    continue;

                                all.Add(new CertificateItem
                                {
                                    Subject = cert.Subject ?? string.Empty,
                                    Thumbprint = cert.Thumbprint ?? string.Empty,
                                    NotAfter = cert.NotAfter
                                });
                            }
                            catch
                            {
                                // ignore individual certificate errors
                            }
                        }
                    }
                    finally
                    {
                        try { store.Close(); } catch { }
                    }
                }

                // Read from CurrentUser first
                ReadFrom(StoreLocation.CurrentUser);

                // If no certificates found in CurrentUser, try LocalMachine
                if (!all.Any())
                {
                    ReadFrom(StoreLocation.LocalMachine);
                }

                // Deduplicate by thumbprint (case-insensitive). If thumbprint is empty, keep as-is.
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var deduped = new List<CertificateItem>();
                foreach (var item in all)
                {
                    var tp = (item.Thumbprint ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(tp))
                    {
                        deduped.Add(item);
                        continue;
                    }

                    if (seen.Add(tp))
                        deduped.Add(item);
                }

                return deduped.OrderBy(c => c.Subject).ToList();
            }
            catch
            {
                // On any failure return an empty list as requested
                return new List<CertificateItem>();
            }
        }
    }
}
