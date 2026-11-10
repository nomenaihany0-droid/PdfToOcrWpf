using System;
using System.IO;
using System.Windows;
using PdfToOcrWpf.Localization;

namespace PdfToOcrWpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var csvPath = Path.Combine(AppContext.BaseDirectory, "Localization", "Strings.csv");
        try
        {
            LocalizationManager.Instance.LoadFromFile(csvPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load translations:\n{csvPath}\n\n{ex.Message}",
                "Localization error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
