using System;
using System.IO;
using System.Text.Json;

namespace PdfToOcrWpf.Services;

public sealed class AppSettings
{
    public string TesseractExePath { get; set; } = "";
    public string Language { get; set; } = "fra";
    public int Dpi { get; set; } = 300;

    private static string SettingsFile =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PdfToOcrWpf", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* fall back to defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllText(SettingsFile,
                JsonSerializer.Serialize(this,
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-fatal */ }
    }
}
