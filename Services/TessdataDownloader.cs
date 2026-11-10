using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToOcrWpf.Services;

public static class TessdataDownloader
{
    private const string BaseUrl =
        "https://github.com/tesseract-ocr/tessdata_fast/raw/main";

    // Taille minimale plausible pour un fichier .traineddata valide.
    // En dessous, c'est probablement une page d'erreur HTML.
    private const long MinPlausibleBytes = 100_000;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// Télécharge le fichier {language}.traineddata dans tessDataPath s'il est absent.
    /// Retourne true si un téléchargement a eu lieu, false si le fichier était déjà présent.
    /// </summary>
    public static async Task<bool> EnsureLanguageAsync(
        string tessDataPath,
        string language,
        IProgress<string>? status,
        CancellationToken cancellationToken)
    {
        // Une langue comme "fra+eng" correspond à deux fichiers.
        var languages = language.Split('+', StringSplitOptions.RemoveEmptyEntries);
        bool downloaded = false;

        foreach (var lang in languages)
        {
            var target = Path.Combine(tessDataPath, $"{lang}.traineddata");

            if (File.Exists(target) && new FileInfo(target).Length >= MinPlausibleBytes)
                continue;

            status?.Report($"Téléchargement de {lang}.traineddata…");

            var url = $"{BaseUrl}/{lang}.traineddata";
            var partial = target + ".partial";

            try
            {
                using var response = await Http.GetAsync(
                    url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var destination = File.Create(partial))
                {
                    await source.CopyToAsync(destination, cancellationToken);
                }

                var size = new FileInfo(partial).Length;
                if (size < MinPlausibleBytes)
                {
                    File.Delete(partial);
                    throw new InvalidOperationException(
                        $"Le fichier {lang}.traineddata téléchargé est invalide ({size} octets). " +
                        "Vérifiez votre connexion réseau.");
                }

                if (File.Exists(target)) File.Delete(target);
                File.Move(partial, target);
                downloaded = true;
            }
            catch
            {
                if (File.Exists(partial))
                {
                    try { File.Delete(partial); } catch { }
                }
                throw;
            }
        }

        return downloaded;
    }
}
