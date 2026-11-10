using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PDFtoImage;
using SkiaSharp;
using Tesseract;

namespace PdfToOcrWpf.Services;

public sealed record ConversionProgress(int PageNumber, int TotalPages, TimeSpan Elapsed);

public sealed class OcrService
{
    public async Task ConvertAsync(
        string pdfPath,
        string txtPath,
        string language,
        int dpi,
        string tessDataPath,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken)
    {
        using var pdfStream = File.OpenRead(pdfPath);
        int totalPages = Conversion.GetPageCount(pdfStream);

        if (totalPages == 0)
            throw new InvalidOperationException("Le PDF ne contient aucune page.");

        var start = DateTime.UtcNow;
        var tempPath = txtPath + ".tmp";

        using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);

        try
        {
            await using (var writer = new StreamWriter(
                tempPath, append: false,
                encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int pageNumber = pageIndex + 1;

                    SKBitmap? skBitmap = null;
                    try
                    {
                        using var pageStream = File.OpenRead(pdfPath);
                        skBitmap = Conversion.ToImage(pageStream, pageIndex,
                            options: new RenderOptions { Dpi = dpi });
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        await TryWriteAsync(writer,
                            new string('=', 60) + Environment.NewLine +
                            $"PAGE {pageNumber} / {totalPages}" + Environment.NewLine +
                            new string('=', 60) + Environment.NewLine +
                            Environment.NewLine +
                            $"[ERREUR rendu page {pageNumber} : {ex.Message}]" + Environment.NewLine);

                        progress.Report(new ConversionProgress(
                            pageNumber, totalPages, DateTime.UtcNow - start));
                        continue;
                    }

                    using (skBitmap)
                    {
                        string text;
                        try
                        {
                            text = await Task.Run(
                                () => RunTesseract(engine, skBitmap), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            text = $"[ERREUR OCR page {pageNumber} : {ex.Message}]";
                        }

                        await TryWriteAsync(writer,
                            new string('=', 60) + Environment.NewLine +
                            $"PAGE {pageNumber} / {totalPages}" + Environment.NewLine +
                            new string('=', 60) + Environment.NewLine +
                            Environment.NewLine +
                            text + Environment.NewLine);
                    }

                    progress.Report(new ConversionProgress(
                        pageNumber, totalPages, DateTime.UtcNow - start));
                }
            }

            MoveIntoPlace(tempPath, txtPath);
        }
        catch
        {
            MoveIntoPlace(tempPath, txtPath);
            throw;
        }
    }

    private static void MoveIntoPlace(string tempPath, string finalPath)
    {
        if (!File.Exists(tempPath)) return;
        try
        {
            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tempPath, finalPath);
        }
        catch { }
    }

    private static async Task TryWriteAsync(StreamWriter writer, string text)
    {
        try { await writer.WriteAsync(text); }
        catch { }
    }

    private static string RunTesseract(TesseractEngine engine, SKBitmap bitmap)
    {
        // SKBitmap -> PNG bytes -> Tesseract Pix
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        using var pix = Pix.LoadFromMemory(ms.ToArray());
        using var page = engine.Process(pix);
        return page.GetText() ?? string.Empty;
    }
}
