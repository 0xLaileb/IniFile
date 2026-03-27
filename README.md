![IniFile Logo](https://raw.githubusercontent.com/0xLaileb/IniFile/master/resources/logo.png)

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
- [📚 API Reference](#-api-reference)
- [🧪 Running Tests](#-running-tests)
- [🏗️ Project Structure](#️-project-structure)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)

---

## 📖 About

**IniFile** is a thin, zero-dependency wrapper around the Windows `kernel32.dll` Private Profile functions (`WritePrivateProfileString`, `GetPrivateProfileString`, `GetPrivateProfileInt`, etc.).

It lets you read and write classic **INI configuration files** with a simple, strongly-typed C# API — no parsing logic needed.

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
- **SDK:** [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- **Language:** C# 14

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
<PackageReference Include="Laileb.IniFile" Version="2.0.2" />
```

> 💡 Make sure to enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in your `.csproj` (required by `LibraryImport` source generation).

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

👉 See the full working demo in [`examples/IniFile.Example/Program.cs`](https://github.com/0xLaileb/IniFile/blob/master/examples/IniFile.Example/Program.cs).

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
Writes a string value. Returns `true` on success. Creates the file, section, and key if they don't exist.

#### 📖 `ReadString`
```csharp
public string ReadString(string key, string? section = null, string defaultValue = "", int bufferSize = 1024)
```
Returns the value for the key, or `defaultValue` if not found.

#### 🔢 `ReadInt`
```csharp
public int ReadInt(string key, string? section = null, int defaultValue = -1)
```
Returns the integer value for the key, or `defaultValue` if not found or not a valid integer.

#### ✅ `ReadBool`
```csharp
public bool ReadBool(string key, string? section = null, bool defaultValue = false)
```
Returns `true` for `"true"`, `"1"`, `"yes"`; `false` for `"false"`, `"0"`, `"no"` (case-insensitive). Returns `defaultValue` for anything else.

#### 📂 `GetAllSections`
```csharp
public string[] GetAllSections(int bufferSize = 32768)
```
Returns an array of all section names. Returns an empty array if the file has no sections.

#### 📋 `GetAllDataSection`
```csharp
public string[] GetAllDataSection(string section, int bufferSize = 32768)
```
Returns an array of `"key=value"` strings for every entry in the given section.

#### 🗑️ `DeleteKey`
```csharp
public bool DeleteKey(string key, string? section = null)
```
Removes a key and its value. Returns `true` on success.

#### 🧹 `DeleteSection`
```csharp
public bool DeleteSection(string? section = null)
```
Removes an entire section. Returns `true` on success.

#### 🔍 `KeyExists`
```csharp
public bool KeyExists(string key, string? section = null)
```
Returns `true` if the key exists in the section (including keys with empty values).

---

## 🧪 Running Tests

```bash
dotnet test
```

Tests are located in [`tests/IniFile.Tests/`](https://github.com/0xLaileb/IniFile/tree/master/tests/IniFile.Tests) and use **xUnit**. They create temporary INI files in the system temp directory and clean up after each run.

---

## 🏗️ Project Structure

```
IniFile/
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
├── 📄 Directory.Build.props      # Shared build settings
├── 📄 Directory.Packages.props   # Central package management
├── 📄 IniFile.slnx               # Solution file
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
