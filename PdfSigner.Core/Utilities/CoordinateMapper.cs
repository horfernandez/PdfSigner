using System;
using System.Drawing;
using PdfSigner.Core.Models;

namespace PdfSigner.Core.Utilities;

public static class CoordinateMapper
{
    /// <summary>
    /// Mapea un rectángulo en coordenadas de UI (pixeles) a coordenadas PDF (puntos),
    /// ajustando escala y el origen (UI: arriba-izq, PDF: abajo-izq).
    /// </summary>
    public static SignRect MapUiRectToPdfRect(RectangleF uiRect, SizeF renderedSizePx, SizeF pdfPageSizePt)
    {
        if (renderedSizePx.Width <= 0 || renderedSizePx.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderedSizePx), "RenderedSize inválido.");

        if (pdfPageSizePt.Width <= 0 || pdfPageSizePt.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(pdfPageSizePt), "PdfPageSize inválido.");

        // Escala px -> pt
        var scaleX = pdfPageSizePt.Width / renderedSizePx.Width;
        var scaleY = pdfPageSizePt.Height / renderedSizePx.Height;

        // UI (0,0) arriba-izq; PDF (0,0) abajo-izq.
        var pdfX = uiRect.X * scaleX;
        var pdfW = uiRect.Width * scaleX;

        // Invertir Y
        var pdfYTop = uiRect.Y * scaleY;
        var pdfH = uiRect.Height * scaleY;

        var pdfY = pdfPageSizePt.Height - pdfYTop - pdfH;

        return new SignRect
        {
            X = (float)pdfX,
            Y = (float)pdfY,
            Width = (float)pdfW,
            Height = (float)pdfH
        };
    }
}
