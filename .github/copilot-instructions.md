# PdfSigner – Copilot Instructions

## Contexto del repositorio
Este repo implementa una app de escritorio WPF para firmar PDFs con certificados del Windows Certificate Store.
Solución .NET 9 con 3 proyectos:
- `PdfSigner.Core`: lógica de negocio (firma, certificados, utilidades).
- `PdfSigner.App`: UI WPF (selección de PDFs, preview, selección de rectángulo, armado de SignRequest).
- `PdfSigner.Tests`: tests unitarios (mapeo de coordenadas y utilidades).

## Arquitectura y flujo principal
### Modelos (Core/Models)
- `SignRequest`: request principal. Campos clave: `InputPath`, `OutputPath`, `CertificateThumbprint`, `PageNumber`, `Rect (SignRect)`, `Reason`.
- `SignRect`: rectángulo en **coordenadas PDF** (puntos), origen abajo-izquierda.
- `SignResult`: resultado de firma. Usar `Ok` (no `IsSuccess`). Métodos: `Success(...)`, `Fail(...)`.

### Servicios (Core/Services)
- `PdfSigningService` implementa `IPdfSigningService.Sign(SignRequest)`.
  - Firma CADES (invisible) con iText.
  - Usa certificado por thumbprint y RSA privada (GetRSAPrivateKey).
  - Wrapper `DotNetRsaSignature` implementa `iText.Signatures.IExternalSignature` con:
    - `GetDigestAlgorithmName()`
    - `GetSignatureAlgorithmName()`
    - `GetSignatureMechanismParameters()`
    - `Sign(byte[])`

- `CertificateService` implementa `ICertificateService` para listar certificados firmables del store.

### Utilidades (Core/Utilities)
- `CoordinateMapper.MapUiRectToPdfRect(RectangleF uiRect, SizeF renderedSizePx, SizeF pdfPageSizePt)`:
  - Convierte selección UI (px, origen arriba-izq) a PDF (pt, origen abajo-izq).
  - Inversión de Y: `pdfY = (uiHeight - uiY - uiH) * scaleY`.

- `PdfPageInfoReader.GetPageSizePoints(pdfPath, pageNumber)`:
  - Lee tamaño real de página con iText.
  - Fallback a página 1 si pageNumber fuera de rango.

## UI WPF: reglas y convenciones
### MainWindow (App)
- WPF muestra:
  - Lista de PDFs (`FilesList`).
  - Preview: `WebView2` si hay 1 PDF, hoja A4 si hay múltiples.
  - Overlay de selección: `SelectionCanvas`.
- La selección del rectángulo se hace con mouse drag:
  - `SelectionCanvas_MouseLeftButtonDown/Move/Up`.
  - Se dibuja un `System.Windows.Shapes.Rectangle` (alias `WpfRectangle`).
  - Al soltar, mapear a PDF usando `CoordinateMapper` y guardar en `SignaturePlacementState`.

### Estado de selección
- `SignaturePlacementState` mantiene:
  - si hay selección (`HasSelection`)
  - el rectángulo PDF (`PdfRect`)
  - la página (`PageNumber`)
- Al tener selección válida:
  - Actualizar los TextBox `XBox/YBox/WBox/HBox/PageBox`
  - Habilitar botón “Limpiar selección”.
- Si no hay selección:
  - Permitir modo manual (inputs editables).

### Tipos a evitar (ambigüedad)
En WPF pueden chocar nombres:
- `Rectangle` (System.Windows.Shapes vs System.Drawing)
- `Path` (System.Windows.Shapes vs System.IO)
Usar aliases explícitos cuando haya UI + IO:
- `using WpfRectangle = System.Windows.Shapes.Rectangle;`
- `using DrawingRectangleF = System.Drawing.RectangleF;`
- `System.IO.Path` siempre fully-qualified si hay `System.Windows.Shapes`.

## Reglas de cambios (muy importante)
1. Mantener `PdfSigner.Core` sin dependencias WPF. Core usa `System.Drawing` para tamaños/rectángulos (SizeF/RectangleF).
2. No introducir `System.Windows.Rect` o `System.Windows.Size` en Core ni Tests. Tests deben usar `System.Drawing`.
3. Al tocar `MainWindow.xaml`, mantener tags bien balanceados (Grid/StackPanel/Border).
4. En UI, nunca asumir que `FilesList.Items[0]` existe sin validar.
5. WebView2: inicializar con `EnsureCoreWebView2Async` y manejar errores (modo A4 como fallback).

## Workflows
Desde la carpeta raíz (donde está `PdfSigner.sln`):
- Build:
  - `dotnet build PdfSigner.sln -c Debug`
- Tests:
  - `dotnet test PdfSigner.sln -c Debug`
- Run UI:
  - `dotnet run --project .\PdfSigner.App\PdfSigner.App.csproj`

## Tareas típicas para Copilot (preferencias)
- Al proponer cambios grandes, dividir en pasos pequeños y compilables.
- Para bugs, primero reproducir con logs/validaciones, luego fix.
- Cambios de UI: preferir pequeñas modificaciones y compilar.
- Mantener comentarios y textos en español (estilo del repo).
