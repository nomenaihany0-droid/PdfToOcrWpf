## Installing Tesseract OCR (Windows)

This application requires Tesseract OCR to be installed on your machine before it can extract text from PDF pages.

### Step 1 — Download the Installer

Download the latest **UB Mannheim** Tesseract installer for Windows (64-bit):

- Download page: https://github.com/UB-Mannheim/tesseract/wiki
- Choose the file named `tesseract-ocr-w64-setup-5.x.x.exe` (the exact version number may vary).

### Step 2 — Run the Installer

1. Double-click the downloaded `.exe` file.
2. Accept the default installation path: `C:\Program Files\Tesseract-OCR`.
3. If prompted, make sure the option **"Add Tesseract to system PATH"** is checked.
4. On the "Select additional language data" screen, tick the languages you need for OCR (e.g. **French**, **English**).
5. Complete the installation.

### Step 3 — Add Tesseract to the System PATH (if not done automatically)

1. Open the Start Menu, search for **"Environment Variables"**, and select **"Edit the system environment variables"**.
2. Click **"Environment Variables…"**.
3. Under **"System variables"**, select **`Path`** and click **"Edit"**.
4. Click **"New"** and add:
   ```
   C:\Program Files\Tesseract-OCR
   ```
5. Click OK on all windows.
6. Restart your terminal and any IDE (e.g. Visual Studio) so the new PATH is loaded.

### Step 4 — Verify the Installation

Open a new Command Prompt or PowerShell window and run:

```cmd
tesseract --version
```

You should see output similar to:

```
tesseract 5.x.x
 leptonica-1.x.x
  libgif 5.x.x : libjpeg 8d : libpng 1.6.x ...
```

If you see the version number, Tesseract is installed and accessible.

### Step 5 — Configure the Application

When you launch the PDF to OCR application for the first time, enter the following path when asked for the Tesseract executable:

```
C:\Program Files\Tesseract-OCR\tesseract.exe
```

The application saves this path and reuses it on subsequent launches.

---

### Troubleshooting

| Problem | Cause | Solution |
|---|---|---|
| `tesseract: command not found` | Tesseract folder is not on PATH | Repeat **Step 3** and restart your terminal |
| Application says "Tesseract introuvable" | The path entered does not point to an existing `tesseract.exe` | Verify the file exists at `C:\Program Files\Tesseract-OCR\tesseract.exe` |
| OCR returns empty text | Language data not installed | Re-run the installer and tick the required language packs (e.g. French, English) |
| OCR returns garbled text | Missing language data for the selected language | Ensure the correct `.traineddata` file exists inside `C:\Program Files\Tesseract-OCR\tessdata` |
