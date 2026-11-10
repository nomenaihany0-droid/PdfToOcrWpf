# PDF → OCR → TXT (WPF)

A Windows desktop application that converts each page of a PDF into an image,
extracts the text with Tesseract OCR, and writes everything to a single `.txt`
file.

Built with **C# / WPF / .NET 8**.

---

## Table of Contents

1. [Overview](#overview)
2. [Why this tool exists](#why-this-tool-exists)
3. [Features](#features)
4. [Technologies used](#technologies-used)
5. [Technologies to install before building](#technologies-to-install-before-building)
6. [Installation — step by step](#installation--step-by-step)
7. [Build and run from source](#build-and-run-from-source)
8. [Publish a standalone executable](#publish-a-standalone-executable)
9. [Usage](#usage)
10. [Interface language](#interface-language)
11. [OCR language and DPI](#ocr-language-and-dpi)
12. [The tessdata folder](#the-tessdata-folder)
13. [Output format](#output-format)
14. [Settings](#settings)
15. [Localization — adding a new language](#localization--adding-a-new-language)
16. [Repository layout](#repository-layout)
17. [File reference](#file-reference)
18. [Troubleshooting](#troubleshooting)
19. [Known limitations](#known-limitations)
20. [Roadmap](#roadmap)
21. [License](#license)

---

## Overview

PDF → OCR → TXT is a Windows desktop application that turns any PDF — digital
or scanned — into a plain `.txt` file that can be searched, copied, indexed,
translated, or fed to an AI.

It works entirely offline, handles hundreds of pages with constant memory,
recovers from per-page errors, and can be cancelled at any time without losing
the pages already processed.

The interface is available in four languages (French, English, German,
Spanish) and can be switched instantly with radio buttons at the top of the
window.

---

## Why this tool exists

Scanned PDFs are images of text. No search engine, no AI, no accessibility
tool can read them — the text is trapped in pixels.

This tool runs Tesseract OCR locally on each page of a PDF, extracts the text,
and writes it to a single `.txt` file. It runs **offline**, handles hundreds of
pages with **constant memory**, recovers from **per-page errors**, preserves
**partial output on cancellation**, and needs **no Tesseract installation** —
only a `tessdata` folder that it creates and populates itself.

The result is a plain text file you own, that any tool — and any AI — can read.

---

## Features

- **Page-by-page conversion** — memory usage stays constant regardless of page count.
- **Live progress bar** with estimated time remaining.
- **Cancellation** that preserves partial output.
- **Configurable OCR language** (`fra`, `eng`, `fra+eng`, `deu`, `spa`) and DPI (200–500).
- **Per-page error recovery** — a single bad page does not abort the run.
- **Temporary output file** — the final `.txt` is only replaced when the
  conversion completes. A cancelled run never destroys the previous file.
- **No Tesseract installation required** — the engine is embedded. Only a
  `tessdata` folder is needed.
- **Four interface languages** (French, English, German, Spanish), switchable
  with radio buttons, instant, no restart.
- **Persistent settings** in `%APPDATA%\PdfToOcrWpf\settings.json`.
- **Single-file, self-contained** Windows publish.

---

## Technologies used

### Language and framework

| Technology | Version | Role |
|---|---|---|
| **C#** | 12 (via .NET 8) | Main language |
| **.NET** | 8.0 | Runtime and SDK |
| **WPF** | .NET 8 | Windows GUI framework |
| **XAML** | — | UI description language |

### NuGet packages

| Package | Version | Role | Native dependency |
|---|---|---|---|
| **PDFtoImage** | 5.4.0 | Renders PDF pages to bitmaps (via PDFium) | `pdfium.dll` |
| **Tesseract** | 5.2.0 | .NET wrapper around Tesseract OCR engine | `tesseract50.dll` + `leptonica-1.82.0.dll` |
| **SkiaSharp** | 4.150.1 (transitive) | 2D graphics library used by PDFtoImage | `libSkiaSharp.dll` |
| **bblanchon.PDFium.Win32** | 152.0.7961 (transitive) | PDFium binaries for Windows | `pdfium.dll` |
| **System.Reflection.Emit** | 4.7.0 (transitive) | Dependency of the Tesseract wrapper | — |

### Architecture and patterns

| Pattern / principle | Usage |
|---|---|
| **MVVM** | Separation of UI, logic, and data |
| **`INotifyPropertyChanged`** | Notification of changes to the XAML |
| **`ICommand` / `RelayCommand`** | Buttons bound to commands |
| **Data binding** | All UI fields bound to the ViewModel |
| **`IProgress<T>`** | Thread-safe progress reporting |
| **`CancellationToken`** | Clean cancellation of asynchronous operations |
| **`async` / `await`** | The whole OCR pipeline is asynchronous |
| **`Task.Run`** | OCR (CPU-bound) runs off the UI thread |
| **`MarkupExtension`** | `{loc:Loc Key}` for localization |
| **`lock`** | Thread-safe state in `LocalizationManager` |

### Formats

| Format | Role |
|---|---|
| **PDF** | Input format |
| **PNG** | Intermediate format between PDFium and Tesseract (in-memory only) |
| **UTF-8** (no BOM) | Output `.txt` encoding |
| **CSV** | Translation storage (`Localization/Strings.csv`) |
| **JSON** | Settings storage (`%APPDATA%\PdfToOcrWpf\settings.json`) |
| **HTTPS** | Optional download of language files from GitHub |
| **`traineddata`** | Tesseract language model format |

### Publication

| Technology | Role |
|---|---|
| **Single-file publish** | One `.exe`, no scattered DLLs |
| **Self-contained** | Embeds .NET 8 — end users do not install .NET |
| **RuntimeIdentifier `win-x64`** | Targets Windows 64-bit |

---

## Technologies to install before building

To build the project from source, you need the following tools. End users
running the published `.exe` do **not** need any of them.

### Required

| Tool | Version | Purpose | Download |
|---|---|---|---|
| **Windows** | 10 or later | Operating system | Preinstalled |
| **.NET 8 SDK** | 8.0.x | Compiles C# and WPF | https://dotnet.microsoft.com/download/dotnet/8.0 |

**Verify the .NET SDK:**

```cmd
dotnet --list-sdks
