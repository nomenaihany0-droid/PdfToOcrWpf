using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PDFtoImage;
using Tesseract;

namespace PdfToOcrWpf.Services;

public sealed record ConversionProgress(int PageNumber, int TotalPages, TimeSpan Elapsed);

public sealed class OcrService
{
    /// <summary>
    /// Renders each PDF page to a bitmap, OCRs it, and writes it to <paramref name="txtPath"/>.
    /// One TesseractEngine per conversion (not per page). Output is written to
    /// "&lt;txtPath&gt;.tmp" and moved into place at the end, so a cancel never destroys
    /// the user's previous file and partial results are still preserved.
    /// </summary>
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

                    Bitmap? bitmap = null;
                    try
                    {
                        using var pageStream = File.OpenRead(pdfPath);
                        bitmap = Conversion.ToImage(
                            pageStream, pageIndex,
                            options: new RenderOptions { Dpi = dpi });
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // PDF shrank under us (file replaced mid-run) — stop cleanly.
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

                    using (bitmap)
                    {
                        string text;
                        try
                        {
                            text = await Task.Run(
                                () => RunTesseract(engine, bitmap), cancellationToken);
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
            // Cancellation or unexpected error: preserve partial output, then rethrow.
            MoveIntoPlace(tempPath, txtPath);
            throw;
        }
    }

    private static void MoveIntoPlace(string tempPath, string finalPath)
    {
        if (!File.Exists(tempPath))
            return;

        try
        {
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(tempPath, finalPath);
        }
        catch
        {
            // Best effort; leave the .tmp behind rather than crash the caller.
        }
    }

    private static async Task TryWriteAsync(StreamWriter writer, string text)
    {
        try { await writer.WriteAsync(text); }
        catch { /* output file unwritable; keep going */ }
    }

    private static string RunTesseract(TesseractEngine engine, Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        using var pix = Pix.LoadFromMemory(ms.ToArray());
        using var page = engine.Process(pix);
        return page.GetText() ?? string.Empty;
    }
}
