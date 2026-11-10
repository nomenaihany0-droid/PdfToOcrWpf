using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PdfToOcrWpf.Helpers;
using PdfToOcrWpf.Services;

namespace PdfToOcrWpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly OcrService _ocr = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
        BrowsePdfCommand = new RelayCommand(BrowsePdf, () => !IsRunning);
        BrowseTxtCommand = new RelayCommand(BrowseTxt, () => !IsRunning);

        StartCommand = new RelayCommand(
            async () =>
            {
                try { await StartAsync(); }
                catch (Exception ex)
                {
                    IsRunning = false;
                    StatusText = "Erreur.";
                    MessageBox.Show($"Erreur inattendue :\n{ex}",
                                    "Erreur",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            },
            () => !IsRunning && HasInputs);

        CancelCommand = new RelayCommand(Cancel, () => IsRunning);

        // Initialise backing fields directly — do NOT go through the setters,
        // or we'd write settings.json on every launch.
        _pdfPath = string.Empty;
        _txtPath = string.Empty;
        _tesseractExePath = _settings.TesseractExePath;
        _language = _settings.Language;
        _dpi = _settings.Dpi;
        _statusText = "Prêt.";
    }

    // --- Bindable properties ---------------------------------------

    private string _pdfPath;
    public string PdfPath
    {
        get => _pdfPath;
        set { Set(ref _pdfPath, value); RefreshCommands(); }
    }

    private string _txtPath;
    public string TxtPath
    {
        get => _txtPath;
        set { Set(ref _txtPath, value); RefreshCommands(); }
    }

    private string _tesseractExePath;
    public string TesseractExePath
    {
        get => _tesseractExePath;
        set => Set(ref _tesseractExePath, value);
    }

    private string _language;
    public string Language
    {
        get => _language;
        set => Set(ref _language, value);   // settings saved later, on Start
    }

    private int _dpi;
    public int Dpi
    {
        get => _dpi;
        set => Set(ref _dpi, value);        // settings saved later, on Start
    }

    private string _statusText;
    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set => Set(ref _progressValue, value);
    }

    private double _progressMax = 1;
    public double ProgressMax
    {
        get => _progressMax;
        set => Set(ref _progressMax, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set { Set(ref _isRunning, value); RefreshCommands(); }
    }

    private bool HasInputs =>
        !string.IsNullOrWhiteSpace(PdfPath) &&
        File.Exists(PdfPath) &&
        !string.IsNullOrWhiteSpace(TxtPath) &&
        !string.IsNullOrWhiteSpace(TesseractExePath);

    public string[] Languages { get; } = { "fra", "eng", "fra+eng", "deu", "spa" };
    public int[] Dpis { get; } = { 200, 300, 400, 500 };

    // --- Commands --------------------------------------------------

    public RelayCommand BrowsePdfCommand { get; }
    public RelayCommand BrowseTxtCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand CancelCommand { get; }

    // --- Actions ---------------------------------------------------

    private void BrowsePdf()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Choisir un fichier PDF",
            Filter = "Fichiers PDF (*.pdf)|*.pdf|Tous les fichiers (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        PdfPath = dlg.FileName;
        if (string.IsNullOrWhiteSpace(TxtPath))
        {
            var baseName = Path.GetFileNameWithoutExtension(dlg.FileName);
            TxtPath = Path.Combine(Path.GetDirectoryName(dlg.FileName) ?? "",
                                   $"{baseName}_extrait.txt");
        }
    }

    private void BrowseTxt()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Enregistrer le texte sous…",
            Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            DefaultExt = ".txt",
            AddExtension = true
        };
        if (dlg.ShowDialog() == true) TxtPath = dlg.FileName;
    }

    private async Task StartAsync()
    {
        // Defensive: CanExecute already prevents UI re-entry, but a direct
        // Execute() call would otherwise race on _cts and double-dispose it.
        if (IsRunning) return;

        if (!File.Exists(PdfPath))
        {
            MessageBox.Show("Le fichier PDF sélectionné est introuvable.",
                            "Fichier introuvable",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (!File.Exists(TesseractExePath))
        {
            MessageBox.Show("Le chemin vers tesseract.exe est invalide.",
                            "Tesseract introuvable",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Path.GetDirectoryName can return null (root paths, malformed paths).
        var tessDataPath = Path.GetDirectoryName(TesseractExePath);
        if (string.IsNullOrEmpty(tessDataPath))
        {
            MessageBox.Show("Impossible de déterminer le dossier de tesseract.exe.",
                            "Tesseract invalide",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Persist options once per run — the only place that touches disk.
        _settings.TesseractExePath = TesseractExePath;
        _settings.Language = Language;
        _settings.Dpi = Dpi;
        _settings.Save();

        IsRunning = true;
        ProgressValue = 0;
        ProgressMax = 1;
        StatusText = "Ouverture du PDF…";
        _cts = new CancellationTokenSource();

        var progress = new Progress<ConversionProgress>(p =>
        {
            ProgressMax = p.TotalPages;
            ProgressValue = p.PageNumber;
            var elapsed = p.Elapsed;
            var remaining = p.PageNumber > 0
                ? TimeSpan.FromSeconds(elapsed.TotalSeconds / p.PageNumber
                                       * (p.TotalPages - p.PageNumber))
                : TimeSpan.Zero;
            StatusText = $"Page {p.PageNumber}/{p.TotalPages} — " +
                         $"écoulé {elapsed.TotalSeconds:F0}s, " +
                         $"reste ~{remaining.TotalSeconds:F0}s";
        });

        try
        {
            await _ocr.ConvertAsync(
                PdfPath, TxtPath, Language, Dpi, tessDataPath, progress, _cts.Token);

            StatusText = "Conversion terminée !";
            MessageBox.Show($"Fichier texte :\n{TxtPath}",
                            "Conversion terminée !",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Conversion annulée.";
            MessageBox.Show($"Le texte extrait jusqu'ici a été enregistré dans :\n{TxtPath}",
                            "Conversion annulée",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText = "Erreur.";
            MessageBox.Show($"Erreur inattendue :\n{ex.Message}",
                            "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "Annulation demandée…";
    }

    // --- Plumbing --------------------------------------------------

    private void RefreshCommands()
    {
        BrowsePdfCommand.RaiseCanExecuteChanged();
        BrowseTxtCommand.RaiseCanExecuteChanged();
        StartCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
