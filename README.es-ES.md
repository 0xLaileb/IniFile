# 💾 IniFile

**Una biblioteca ligera de .NET para leer y escribir archivos INI de Windows a través de la API nativa kernel32.**

[![Release](https://img.shields.io/github/v/release/0xLaileb/IniFile?color=%231DC8EE&label=Release&style=flat-square)](https://github.com/0xLaileb/IniFile/releases)
[![NuGet](https://img.shields.io/nuget/v/Laileb.IniFile?color=%231DC8EE&label=NuGet&style=flat-square&logo=nuget)](https://www.nuget.org/packages/Laileb.IniFile)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Laileb.IniFile?color=%231DC8EE&label=Downloads&style=flat-square&logo=nuget)](https://www.nuget.org/packages/Laileb.IniFile)
[![Last Commit](https://img.shields.io/github/last-commit/0xLaileb/IniFile?color=%231DC8EE&label=Last%20Commit&style=flat-square)](https://github.com/0xLaileb/IniFile/commits)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D4?style=flat-square&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

---

## 📋 Tabla de Contenidos

- [📖 Acerca de](#-acerca-de)
- [✨ Características](#-características)
- [🚀 Primeros Pasos](#-primeros-pasos)
  - [📌 Prerrequisitos](#-prerrequisitos)
  - [📦 Instalación](#-instalación)
- [💡 Uso](#-uso)
- [📚 Referencia de la API](#-referencia-de-la-api)
- [🧪 Ejecución de Pruebas](#-ejecución-de-pruebas)
- [🏗️ Estructura del Proyecto](#️-estructura-del-proyecto)
- [🤝 Contribución](#-contribución)
- [📄 Licencia](#-licencia)

---

## 📖 Acerca de

**IniFile** es un envoltorio (wrapper) ligero y sin dependencias sobre las funciones Private Profile de `kernel32.dll` de Windows (`WritePrivateProfileString`, `GetPrivateProfileString`, `GetPrivateProfileInt`, etc.).

Permite leer y escribir los clásicos **archivos de configuración INI** con una API de C# simple y fuertemente tipada, sin necesidad de implementar lógica de análisis (parsing).

> ⚠️ **Nota:** Esta biblioteca utiliza llamadas P/Invoke exclusivas de Windows y **no es multiplataforma**.

---

## ✨ Características

| Método | Descripción |
|---|---|
| `Write` | ✏️ Escribe un valor de cadena en una clave dentro de una sección dada |
| `ReadString` | 📖 Lee un valor de cadena (con valor predeterminado opcional) |
| `ReadInt` | 🔢 Lee un valor entero (con valor predeterminado opcional) |
| `ReadBool` | ✅ Lee un booleano — soporta `true/false`, `1/0`, `yes/no` |
| `GetAllSections` | 📂 Lista todos los nombres de sección en el archivo |
| `GetAllDataSection` | 📋 Lista todos los pares `clave=valor` en una sección |
| `DeleteKey` | 🗑️ Elimina una clave específica de una sección |
| `DeleteSection` | 🧹 Elimina una sección completa y sus claves |
| `KeyExists` | 🔍 Verifica si una clave existe en una sección |

---

## 🚀 Primeros Pasos

### 📌 Prerrequisitos

- **SO:** Windows 10 / 11 o Windows Server 2016+
- **SDK:** [.NET 10 SDK](https://dotnet.microsoft.com/download) o posterior
- **Lenguaje:** C# 14

### 📦 Instalación

#### NuGet Package Manager

```
dotnet add package Laileb.IniFile
```

O a través de la Consola del Administrador de Paquetes en Visual Studio:

```
Install-Package Laileb.IniFile
```

O añádelo directamente a tu `.csproj`:

```xml
<PackageReference Include="Laileb.IniFile" Version="2.0.4" />
```

> 💡 Asegúrate de habilitar `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` en tu `.csproj` (requerido por la generación de código fuente de `LibraryImport`).

---

## 💡 Uso

```csharp
using Ini = IniFile.IniFile;

var ini = new Ini("config.ini");

// Escribir valores
ini.Write("Host", "localhost", "Database");
ini.Write("Port", "5432", "Database");
ini.Write("Debug", "true", "Logging");

// Leer valores
string host = ini.ReadString("Host", "Database"); // "localhost"
int port = ini.ReadInt("Port", "Database"); // 5432
bool dbg  = ini.ReadBool("Debug", "Logging"); // true
string miss = ini.ReadString("Missing", "Nope", defaultValue: "N/A"); // "N/A"

// Enumerar
string[] sections = ini.GetAllSections(); // ["Database", "Logging"]
string[] entries  = ini.GetAllDataSection("Database"); // ["Host=localhost", "Port=5432"]

// Verificar y eliminar
bool exists = ini.KeyExists("Host", "Database"); // true
ini.DeleteKey("Host", "Database");
ini.DeleteSection("Logging");
```

👉 Mira la demo completa y funcional en [`examples/IniFile.Example/Program.cs`](https://github.com/0xLaileb/IniFile/blob/master/examples/IniFile.Example/Program.cs).

---

## 📚 Referencia de la API

### 🔨 Constructor

```csharp
public IniFile(string filePath)
```

Crea una nueva instancia vinculada a la ruta de archivo proporcionada. La ruta se resuelve internamente a una ruta absoluta. Lanza `ArgumentException` si la ruta es nula, vacía o contiene solo espacios en blanco.

### 🏷️ Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `FilePath` | `string` | La ruta totalmente calificada al archivo INI. |

### ⚙️ Métodos

#### ✏️ `Write`
```csharp
public bool Write(string key, string? value, string? section = null)
```
Escribe un valor de cadena. Devuelve `true` si tiene éxito. Crea el archivo, la sección y la clave si no existen. Cuando `section` se omite o es `null`, el envoltorio utiliza un nombre de sección vacío, que Windows serializa como `[]`.

#### 📖 `ReadString`
```csharp
public string ReadString(string? key, string? section = null, string defaultValue = "", int bufferSize = 1024)
```
Devuelve el valor de la clave, o `defaultValue` si no se encuentra. Cuando `section` se omite o es `null`, las lecturas de clave utilizan un nombre de sección vacío, que Windows serializa como `[]`. Pasar `null` en `key` conserva el comportamiento de enumeración nativo: nombres de claves para una sección, o nombres de sección cuando tanto `key` como `section` son `null`.

#### 🔢 `ReadInt`
```csharp
public int ReadInt(string key, string? section = null, int defaultValue = -1)
```
Devuelve el valor entero de la clave, o `defaultValue` si la clave no se encuentra. Los valores numéricos mal formados siguen el comportamiento de análisis nativo de `GetPrivateProfileInt` y pueden devolver `0` en lugar de `defaultValue`. Cuando `section` se omite o es `null`, el envoltorio utiliza un nombre de sección vacío, que Windows serializa como `[]`.

#### ✅ `ReadBool`
```csharp
public bool ReadBool(string key, string? section = null, bool defaultValue = false)
```
Devuelve `true` para `"true"`, `"1"`, `"yes"`; `false` para `"false"`, `"0"`, `"no"` (insensible a mayúsculas/minúsculas). Devuelve `defaultValue` para cualquier otra cosa. Cuando `section` se omite o es `null`, el envoltorio utiliza un nombre de sección vacío, que Windows serializa como `[]`.

#### 📂 `GetAllSections`
```csharp
public string[] GetAllSections(int bufferSize = 32768)
```
Devuelve un array con todos los nombres de las secciones. Devuelve un array vacío si el archivo no tiene secciones.

#### 📋 `GetAllDataSection`
```csharp
public string[] GetAllDataSection(string section, int bufferSize = 32768)
```
Devuelve un array de cadenas `"clave=valor"` para cada entrada en la sección dada.

#### 🗑️ `DeleteKey`
```csharp
public bool DeleteKey(string key, string? section = null)
```
Elimina una clave y su valor. Devuelve `true` si tiene éxito. Cuando `section` se omite o es `null`, el envoltorio utiliza un nombre de sección vacío, que Windows serializa como `[]`.

#### 🧹 `DeleteSection`
```csharp
public bool DeleteSection(string? section = null)
```
Elimina una sección completa. Devuelve `true` si tiene éxito. Cuando `section` se omite o es `null`, el envoltorio elimina la sección vacía `[]`.

#### 🔍 `KeyExists`
```csharp
public bool KeyExists(string key, string? section = null)
```
Devuelve `true` si la clave existe en la sección (incluidas las claves con valores vacíos). Cuando `section` se omite o es `null`, el envoltorio utiliza un nombre de sección vacío, que Windows serializa como `[]`.

---

## 🧪 Ejecución de Pruebas

```bash
dotnet test
```

Las pruebas se encuentran en [`tests/IniFile.Tests/`](https://github.com/0xLaileb/IniFile/tree/master/tests/IniFile.Tests) y utilizan **xUnit**. Crean archivos INI temporales en el directorio temporal del sistema y los limpian después de cada ejecución.

---

## 🏗️ Estructura del Proyecto

```
IniFile/
├── 📁 src/
│   └── 📁 IniFile/              # Código fuente de la biblioteca
│       ├── 📄 IniFile.cs
│       └── 📄 IniFile.csproj
├── 📁 tests/
│   └── 📁 IniFile.Tests/        # Pruebas xUnit
│       ├── 📄 IniFileTests.cs
│       └── 📄 IniFile.Tests.csproj
├── 📁 examples/
│   └── 📁 IniFile.Example/      # Aplicación de demo de consola
│       ├── 📄 Program.cs
│       └── 📄 IniFile.Example.csproj
├── 📄 Directory.Build.props      # Configuración de compilación compartida
├── 📄 Directory.Packages.props   # Gestión centralizada de paquetes
├── 📄 IniFile.slnx               # Archivo de solución
└── 📄 README.md
```

---

## 🤝 Contribución

¡Las contribuciones son bienvenidas! Para comenzar:

1. 🍴 Haz un fork del repositorio
2. 🌿 Crea una rama para la característica (`git checkout -b feature/mi-caracteristica`)
3. ✏️ Realiza tus cambios y añade pruebas
4. ✅ Ejecuta `dotnet test` para verificar que todo pase
5. 📬 Abre un Pull Request

---

## 📄 Licencia

Este proyecto está licenciado bajo la [Licencia MIT](https://github.com/0xLaileb/IniFile/blob/master/LICENSE).
