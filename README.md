# 💾 IniFile

**A lightweight .NET library for reading and writing Windows INI files via the native kernel32 API.**

[![Release](https://img.shields.io/github/v/release/0xLaileb/IniFile?color=%231DC8EE&label=Release&style=flat-square)](https://github.com/0xLaileb/IniFile/releases)
[![NuGet](https://img.shields.io/nuget/v/Laileb.IniFile?color=%231DC8EE&label=NuGet&style=flat-square&logo=nuget)](https://www.nuget.org/packages/Laileb.IniFile)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Laileb.IniFile?color=%231DC8EE&label=Downloads&style=flat-square&logo=nuget)](https://www.nuget.org/packages/Laileb.IniFile)
[![Last Commit](https://img.shields.io/github/last-commit/0xLaileb/IniFile?color=%231DC8EE&label=Last%20Commit&style=flat-square)](https://github.com/0xLaileb/IniFile/commits)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D4?style=flat-square&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

---

## 📋 Table of Contents

- [📖 About](#-about)
- [✨ Features](#-features)
- [🚀 Getting Started](#-getting-started)
  - [📌 Prerequisites](#-prerequisites)
  - [📦 Installation](#-installation)
- [💡 Usage](#-usage)
- [🚧 Limitations](#-limitations)
- [📚 API Reference](#-api-reference)
- [🧪 Running Tests](#-running-tests)
- [🧱 Project Structure](#-project-structure)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)

---

## 📖 About

**IniFile** is a thin, zero-dependency wrapper around the Windows `kernel32.dll` Private Profile functions (`WritePrivateProfileString`, `GetPrivateProfileString`, `GetPrivateProfileInt`, etc.).

It lets you read and write classic **INI configuration files** with a simple, strongly-typed C# API — no parsing logic needed. New files are created as UTF-16, so any Unicode text is preserved.

> ⚠️ **Note:** This library uses Windows-only P/Invoke calls and is **not cross-platform**.

---

## ✨ Features

| Method | Description |
|---|---|
| `Write` | ✏️ Write a string value to a key in a given section |
| `ReadString` | 📖 Read a string value (with optional default) |
| `ReadInt` | 🔢 Read an integer value (with optional default) |
| `ReadBool` | ✅ Read a boolean — supports `true/false`, `1/0`, `yes/no` |
| `GetAllSections` | 📂 List all section names in the file |
| `GetAllDataSection` | 📋 List all `key=value` pairs in a section |
| `DeleteKey` | 🗑️ Remove a specific key from a section |
| `DeleteSection` | 🧹 Remove an entire section and its keys |
| `KeyExists` | 🔍 Check whether a key exists in a section |

---

## 🚀 Getting Started

### 📌 Prerequisites

- **OS:** Windows 10 / 11 or Windows Server 2016+
- **Target framework:** `net10.0` or later ([.NET 10 SDK](https://dotnet.microsoft.com/download))

### 📦 Installation

#### NuGet Package Manager

```
dotnet add package Laileb.IniFile
```

Or via the Package Manager Console in Visual Studio:

```
Install-Package Laileb.IniFile
```

Or add directly to your `.csproj`:

```xml
<PackageReference Include="Laileb.IniFile" Version="2.1.1" />
```

> 💡 No extra project settings are needed: the P/Invoke marshalling code is generated inside the library itself.

---

## 💡 Usage

```csharp
using Ini = IniFile.IniFile;

var ini = new Ini("config.ini");

// Write values
ini.Write("Host", "localhost", "Database");
ini.Write("Port", "5432", "Database");
ini.Write("Debug", "true", "Logging");

// Read values
string host = ini.ReadString("Host", "Database"); // "localhost"
int port = ini.ReadInt("Port", "Database"); // 5432
bool dbg  = ini.ReadBool("Debug", "Logging"); // true
string miss = ini.ReadString("Missing", "Nope", defaultValue: "N/A"); // "N/A"

// Enumerate
string[] sections = ini.GetAllSections(); // ["Database", "Logging"]
string[] entries  = ini.GetAllDataSection("Database"); // ["Host=localhost", "Port=5432"]

// Check & delete
bool exists = ini.KeyExists("Host", "Database"); // true
ini.DeleteKey("Host", "Database");
ini.DeleteSection("Logging");
```

👉 See the full working demo in [`examples/IniFile.Example/Program.cs`](https://github.com/0xLaileb/IniFile/blob/master/examples/IniFile.Example/Program.cs). Run it from the repository root:

```bash
dotnet run --project examples/IniFile.Example
```

---

## 🚧 Limitations

These come from the underlying Windows API:

- **Encoding.** New (missing or empty) files are created as **UTF-16 LE with a byte order mark**, so any Unicode text (Cyrillic, CJK, emoji) round-trips. This is the only Unicode encoding the Windows profile API supports: it does not understand UTF-8. Files that already have content keep their encoding; in files without the UTF-16 LE byte order mark (ANSI or UTF-8), Windows stores text in the system ANSI code page and replaces other characters with `?`, and in a UTF-8 file with a byte order mark it does not recognize the first section. To make such a file Unicode-capable, re-save it as UTF-16 LE with a byte order mark; for a UTF-8 file: `File.WriteAllText(path, File.ReadAllText(path), System.Text.Encoding.Unicode)`. If you need an ANSI file for a legacy tool, create it yourself with some content (for example, a `; comment` line) before the first write: an empty file is converted to UTF-16.
- **Values.** On read, Windows trims whitespace around values and removes one pair of surrounding double or single quotes: `"quoted"` is read back as `quoted`. Trailing whitespace of `defaultValue` is trimmed as well.

- **Size.** Values longer than 32,767 characters are truncated on read. Listing keys, sections, or section entries throws `InvalidOperationException` when the data does not fit into the 32,768-character buffer.
- **Empty section.** When `section` is `null`, keys go to the empty section `[]`. All methods, including `GetAllDataSection()`, read it the same way, but `GetAllSections` does not list it.
- **Errors.** `Write`, `DeleteKey`, and `DeleteSection` report I/O failures by returning `false` instead of throwing (invalid arguments still throw `ArgumentException`). Call `Marshal.GetLastPInvokeError()` right after a failed call to get the Win32 error code (for example, `3` when the directory does not exist).
- **Case.** Windows matches section and key names ignoring ASCII letter case; case-insensitive matching of non-ASCII letters (for example, Cyrillic) is not guaranteed.
- **Threads.** Writes from several threads of one process are serialized by the library.

---

## 📚 API Reference

### 🔨 Constructor

```csharp
public IniFile(string filePath)
```

Creates a new instance bound to the given file path. The path is resolved to an absolute path internally. Throws `ArgumentException` if the path is null, empty, or whitespace.

### 🏷️ Properties

| Property | Type | Description |
|---|---|---|
| `FilePath` | `string` | The fully-qualified path to the INI file. |

### ⚙️ Methods

#### ✏️ `Write`
```csharp
public bool Write(string key, string? value, string? section = null)
```
Writes a string value. Returns `true` on success, `false` on failure (see [Limitations](#-limitations) for error codes). Creates the file (as UTF-16 LE, see [Limitations](#-limitations)), section, and key if they don't exist; the directory must already exist. Passing `null` as `value` deletes the key. When `section` is omitted or `null`, the wrapper uses an empty section name, which Windows serializes as `[]`.

#### 📖 `ReadString`
```csharp
public string ReadString(string? key, string? section = null, string defaultValue = "", int bufferSize = 1024)
```
Returns the value for the key, or `defaultValue` if not found (Windows normalizes both, see [Limitations](#-limitations)). When `section` is omitted or `null`, key reads use an empty section name, which Windows serializes as `[]`. Passing `null` for `key` preserves the native enumeration behavior: key names for a section, or section names when both `key` and `section` are `null`, returned as one `\0`-separated string.

`bufferSize` is the initial buffer (2 to 32768 characters, at least 3 for enumeration); it doubles automatically when a value does not fit.

#### 🔢 `ReadInt`
```csharp
public int ReadInt(string key, string? section = null, int defaultValue = -1)
```
Returns the integer value for the key, or `defaultValue` if the key is missing or its value is empty. Parsing follows the native `GetPrivateProfileInt`: decimal and `0x` hexadecimal values are supported, only the leading number is used (`12abc` → `12`), a value that does not start with a number returns `0`, and values outside the `int` range overflow without an error. When `section` is omitted or `null`, the wrapper uses an empty section name, which Windows serializes as `[]`.

#### ✅ `ReadBool`
```csharp
public bool ReadBool(string key, string? section = null, bool defaultValue = false)
```
Returns `true` for `"true"`, `"1"`, `"yes"`; `false` for `"false"`, `"0"`, `"no"` (case-insensitive). Returns `defaultValue` for anything else, including a missing key. When `section` is omitted or `null`, the wrapper uses an empty section name, which Windows serializes as `[]`.

#### 📂 `GetAllSections`
```csharp
public string[] GetAllSections(int bufferSize = 32768)
```
Returns an array of all section names, except the empty `[]` section (read it with `GetAllDataSection()`). Returns an empty array if the file has no sections. `bufferSize` must be from 3 to 32768; `InvalidOperationException` is thrown when the names do not fit.

#### 📋 `GetAllDataSection`
```csharp
public string[] GetAllDataSection(string? section = null, int bufferSize = 32768)
```
Returns an array of `"key=value"` strings for every entry in the given section, or an empty array if the section does not exist. Comment lines are skipped; lines without `=` are returned as they are. When `section` is omitted or `null`, returns the entries of the empty `[]` section. `bufferSize` must be from 3 to 32768; `InvalidOperationException` is thrown when the entries do not fit.

#### 🗑️ `DeleteKey`
```csharp
public bool DeleteKey(string key, string? section = null)
```
Removes a key and its value. Returns `true` on success, including when the key does not exist. Like all write operations, it creates a missing file (Windows does so even when there is nothing to delete). When `section` is omitted or `null`, the wrapper uses an empty section name, which Windows serializes as `[]`.

#### 🧹 `DeleteSection`
```csharp
public bool DeleteSection(string? section = null)
```
Removes an entire section. Returns `true` on success, including when the section does not exist; a missing file is created, as with `DeleteKey`. When `section` is omitted or `null`, the wrapper deletes the empty `[]` section.

#### 🔍 `KeyExists`
```csharp
public bool KeyExists(string key, string? section = null)
```
Returns `true` if the key exists in the section (including keys with empty values). Keys are matched exactly as `ReadString` matches them, and the section size is not limited. When `section` is omitted or `null`, the wrapper uses an empty section name, which Windows serializes as `[]`.

---

## 🧪 Running Tests

```bash
dotnet test
```

Tests call the Windows API, so they run only on Windows (with the .NET 10 SDK). They are located in [`tests/IniFile.Tests/`](https://github.com/0xLaileb/IniFile/tree/master/tests/IniFile.Tests) and use **xUnit**. They create temporary INI files in the system temp directory and delete them afterwards.

---

## 🧱 Project Structure

```
IniFile/
├── 📁 .github/workflows/        # CI: build, test, pack, publish to NuGet on tags
├── 📁 src/
│   └── 📁 IniFile/              # Library source
│       ├── 📄 IniFile.cs
│       └── 📄 IniFile.csproj
├── 📁 tests/
│   └── 📁 IniFile.Tests/        # xUnit tests
│       ├── 📄 IniFileTests.cs
│       └── 📄 IniFile.Tests.csproj
├── 📁 examples/
│   └── 📁 IniFile.Example/      # Console demo app
│       ├── 📄 Program.cs
│       └── 📄 IniFile.Example.csproj
├── 📁 resources/                # NuGet package icon
├── 📄 Directory.Build.props      # Shared build settings
├── 📄 Directory.Packages.props   # Central package management
├── 📄 IniFile.slnx               # Solution file
├── 📄 LICENSE
└── 📄 README.md
```

---

## 🤝 Contributing

Contributions are welcome! To get started:

1. 🍴 Fork the repository
2. 🌿 Create a feature branch (`git checkout -b feature/my-feature`)
3. ✏️ Make your changes and add tests
4. ✅ Run `dotnet test` to verify everything passes
5. 📬 Open a Pull Request

---

## 📄 License

This project is licensed under the [MIT License](https://github.com/0xLaileb/IniFile/blob/master/LICENSE).
