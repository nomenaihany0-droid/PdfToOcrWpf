using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace PdfToOcrWpf.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private readonly object _lock = new();
    private Dictionary<string, Dictionary<string, string>> _data = new();
    private List<string> _availableLanguages = new();
    private string _currentLanguage = "fr";
    private string _defaultLanguage = "fr";

    private LocalizationManager() { }

    public IReadOnlyList<string> AvailableLanguages
    {
        get { lock (_lock) return new List<string>(_availableLanguages).AsReadOnly(); }
    }

    public string CurrentLanguage
    {
        get { lock (_lock) return _currentLanguage; }
        set
        {
            lock (_lock)
            {
                if (_currentLanguage == value) return;
                if (!_availableLanguages.Contains(value)) return;
                _currentLanguage = value;
            }
            ApplyCulture(value);
            RaiseAllChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get
        {
            lock (_lock)
            {
                if (!_data.TryGetValue(key, out var byLang))
                    return $"!{key}!";
                if (byLang.TryGetValue(_currentLanguage, out var value) && !string.IsNullOrEmpty(value))
                    return value;
                if (byLang.TryGetValue(_defaultLanguage, out var fallback) && !string.IsNullOrEmpty(fallback))
                    return fallback;
                return $"!{key}!";
            }
        }
    }

    public void LoadFromFile(string csvPath)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"CSV not found: {csvPath}", csvPath);

        var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        if (lines.Length == 0)
            throw new InvalidDataException("CSV is empty.");

        var headers = ParseCsvLine(lines[0]);
        if (headers.Count < 2 || !headers[0].Trim().Equals("key", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("CSV header must start with 'key'.");

        var newData = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var newLanguages = new List<string>();
        for (int i = 1; i < headers.Count; i++)
        {
            var lang = headers[i].Trim();
            if (!string.IsNullOrEmpty(lang)) newLanguages.Add(lang);
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = ParseCsvLine(lines[i]);
            if (cells.Count == 0) continue;

            var key = cells[0].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            var byLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c < headers.Count; c++)
            {
                var value = c < cells.Count ? cells[c].Trim() : "";
                if (!string.IsNullOrEmpty(value))
                    byLang[headers[c].Trim()] = value;
            }
            newData[key] = byLang;
        }

        var newDefault = newLanguages.Contains("fr") ? "fr"
                       : newLanguages.Count > 0 ? newLanguages[0] : "fr";

        lock (_lock)
        {
            _data = newData;
            _availableLanguages = newLanguages;
            _defaultLanguage = newDefault;
            if (!newLanguages.Contains(_currentLanguage) && newLanguages.Count > 0)
                _currentLanguage = newDefault;
        }

        ApplyCulture(_currentLanguage);
        RaiseAllChanged();
    }

    private static void ApplyCulture(string code)
    {
        try
        {
            var culture = new CultureInfo(code);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
        }
        catch (CultureNotFoundException) { }
    }

    private void RaiseAllChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}
