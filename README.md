```markdown
# PDF → OCR → TXT (WPF)

A Windows desktop application that converts each page of a PDF into an image,
extracts the text with Tesseract OCR, and writes everything to a single `.txt`
file.

Built with **C# / WPF / .NET 8**.

---

## Features

- Page-by-page conversion — memory usage stays constant regardless of page count
- Live progress bar with estimated time remaining
- Cancellation that preserves partial output
- Configurable OCR language (`fra`, `eng`, `fra+eng`, `deu`, `spa`) and DPI (200–500)
- Per-page error recovery — a single bad page does not abort the run
- Output written to a temporary file and moved into place on completion, so a
  cancelled conversion never destroys the user's previous output
- Settings persisted in `%APPDATA%\PdfToOcrWpf\settings.json`
- Single-file, self-contained Windows publish

---

## Requirements

- **Windows 10 or later**
- **.NET 8 SDK**
- **Tesseract 5.x** installed, with the language packs you intend to use
  (e.g. `fra.traineddata`, `eng.traineddata`)

---

## Usage

1. Point the application to your Tesseract executable.
2. Select a source PDF.
3. Choose an output `.txt` path.
4. Pick the OCR language and DPI (300 is a sensible default).
5. Click **Démarrer la conversion**. You can cancel at any time — pages already
   processed are kept.

### Run from source

```bash
dotnet run
```

### Build a single-file Windows executable

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable is written to
`bin/Release/net8.0-windows/win-x64/publish/PdfToOcrWpf.exe`.

---

## Known limitations

- **No in-flight cancellation.** Clicking Cancel stops after the current page
  finishes OCR. This is a Tesseract limitation, not an application one.
- **Poor OCR on low-quality scans.** Handwriting, low contrast, and skewed pages
  will produce noisy text. This is the OCR engine, not the application.
- **One PDF at a time.** There is no batch mode yet.
- **No installer.** Distribute the single-file publish output, or build your own
  installer.

---

## Repository layout

```
.
├── PdfToOcrWpf.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Helpers/
│   └── RelayCommand.cs
├── Services/
│   ├── AppSettings.cs
│   └── OcrService.cs
└── ViewModels/
    └── MainViewModel.cs
```

---

## Dependencies

| Package | Purpose |
|---|---|
| `PDFtoImage` | Renders PDF pages to bitmaps (PDFium-based) |
| `Tesseract` | .NET wrapper around the Tesseract OCR engine |
| `System.Drawing.Common` | Bitmap handling |

---

## License

MIT — see [LICENSE](LICENSE).
```
