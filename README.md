# TrimOnKeyword

A lightweight Windows utility for batch-renaming files by removing everything **from a keyword to the end of the filename**, while preserving the file extension.  
Designed for fast visual scanning, selective renaming, and safe preview before changes.

Hypothetically, suppose someone whom does not exist has a bunch of questionably sourced music that came from some sort of youtube downloader where all filenames are appended with ".someyoutubemp3downloader.mp3" and you want that gone.

Consider this a scaffolding. Currently for one specific use-case, take it, chop it up, make it do what you need.

---

## Features

- **Keyword-based trimming**  
  Example:  
  `aaa.bbb.ccc.ddd.mp3` → keyword `ccc` → becomes `aaa.bbb.mp3`

- **Recursive folder scanning**

- **Safe preview mode**
  - Current name vs. new name  
  - Tooltips show full paths  
  - Checkboxes to select which files will be renamed

- **Collision-safe renaming**  
  Automatically creates:
  ```
  filename (1).ext
  filename (2).ext
  ```

- **Dark modern UI** with custom title bar  

- **Single-file portable EXE** (no install required)

---

## Building

TrimOnKeyword targets **.NET 9** and uses WPF.

### Publish as a portable EXE

```powershell
dotnet publish -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:PublishTrimmed=false `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Published binary output:

```
bin/Release/net9.0-windows/win-x64/publish/TrimOnKeyword.exe
```

---

## Safety

- No files are modified until **Go!** is clicked.  
- A confirmation dialog appears before renaming.  
- Collision handling prevents overwriting files.

---


## 📄 License

MIT — do whatever
