namespace IniFile;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Provides methods to read and write Windows INI files
/// using the kernel32 Private Profile API (P/Invoke).
/// </summary>
/// <remarks>
/// This class is a thin wrapper around the native Windows API functions
/// <c>WritePrivateProfileString</c>, <c>GetPrivateProfileString</c>,
/// <c>GetPrivateProfileInt</c>, <c>GetPrivateProfileSection</c>, and
/// <c>GetPrivateProfileSectionNames</c>.
/// <para/>
/// Source: <see href="https://github.com/0xLaileb/IniFile"/>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class IniFile
{
    /// <summary>Default buffer size (in characters) used for string reads.</summary>
    private const int DefaultBufferSize = 1024;

    /// <summary>
    /// Maximum buffer size (in characters) used for section enumeration.
    /// Matches the Windows API maximum for INI files (32 KB).
    /// </summary>
    private const int MaxSectionBufferSize = 32768;

    /// <summary>The fully-qualified path to the INI file.</summary>
    private readonly string _filePath;

    private static string NormalizeSection(string? section) => section ?? string.Empty;

    private static void ValidateBufferSize(int bufferSize, int minimumSize = 2)
    {
        if (bufferSize < minimumSize || bufferSize > MaxSectionBufferSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferSize),
                bufferSize,
                $"Buffer size must be between {minimumSize} and {MaxSectionBufferSize} characters.");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFile"/> class
    /// bound to the specified file path.
    /// </summary>
    /// <param name="filePath">
    /// Relative or absolute path to the INI file.
    /// The path is resolved to a full path internally.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public IniFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
    }

    /// <summary>Gets the fully-qualified path to the INI file.</summary>
    public string FilePath => _filePath;

    #region Native P/Invoke declarations (LibraryImport, .NET 7+)

    /// <summary>
    /// Sets a string value for the specified key in the given section of an INI file.
    /// If the file does not exist it is created automatically.
    /// </summary>
    /// <returns><c>true</c> on success; <c>false</c> on failure.</returns>
    [LibraryImport("kernel32", EntryPoint = "WritePrivateProfileStringW",
        SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeWritePrivateProfileString(
        string? lpAppName,
        string? lpKeyName,
        string? lpString,
        string lpFileName);

    /// <summary>
    /// Retrieves a string value from an INI file.
    /// If the key is not found the default value is copied to the buffer.
    /// </summary>
    /// <returns>The number of characters copied to <paramref name="lpReturnedString"/>, excluding the terminating null.</returns>
    [LibraryImport("kernel32", EntryPoint = "GetPrivateProfileStringW",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial int NativeGetPrivateProfileString(
        string? lpAppName,
        string? lpKeyName,
        string? lpDefault,
        [Out] char[] lpReturnedString,
        int nSize,
        string lpFileName);

    /// <summary>
    /// Retrieves an integer value from an INI file.
    /// If the key is not found, <paramref name="nDefault"/> is returned.
    /// </summary>
    [LibraryImport("kernel32", EntryPoint = "GetPrivateProfileIntW",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial int NativeGetPrivateProfileInt(
        string? lpAppName,
        string lpKeyName,
        int nDefault,
        string lpFileName);

    /// <summary>
    /// Retrieves all key/value pairs for a given section of an INI file.
    /// Pairs are written as null-terminated strings; the list ends with a double null.
    /// </summary>
    /// <returns>The number of characters copied, excluding the trailing null.</returns>
    [LibraryImport("kernel32", EntryPoint = "GetPrivateProfileSectionW",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial int NativeGetPrivateProfileSection(
        string lpAppName,
        nint lpReturnedString,
        int nSize,
        string lpFileName);

    /// <summary>
    /// Retrieves the names of all sections in an INI file.
    /// Names are written as null-terminated strings; the list ends with a double null.
    /// </summary>
    /// <returns>The number of characters copied, excluding the trailing null.</returns>
    [LibraryImport("kernel32", EntryPoint = "GetPrivateProfileSectionNamesW",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial int NativeGetPrivateProfileSectionNames(
        nint lpReturnedString,
        int nSize,
        string lpFileName);

    #endregion

    #region Public API

    /// <summary>
    /// Writes a string value for the specified key in the given section of the INI file.
    /// If the file, section, or key does not exist, it is created automatically.
    /// Numeric values can be written as strings (e.g. <c>"1"</c>).
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The string value to write.</param>
    /// <param name="section">
    /// The section name. Pass <c>null</c> to target the empty section name, which Windows serializes as <c>[]</c>.
    /// </param>
    /// <returns><c>true</c> if the write succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public bool Write(string key, string? value, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return NativeWritePrivateProfileString(NormalizeSection(section), key, value, _filePath);
    }

    /// <summary>
    /// Reads a string value from the specified key in the given section of the INI file.
    /// </summary>
    /// <param name="key">
    /// The key name. If <c>null</c>, all key names in <paramref name="section"/> are returned
    /// as a single null-separated string. If both <paramref name="key"/> and
    /// <paramref name="section"/> are <c>null</c>, all section names are returned.
    /// </param>
    /// <param name="section">
    /// The section name. When <paramref name="key"/> is not <c>null</c>, pass <c>null</c>
    /// to target the empty section name, which Windows serializes as <c>[]</c>.
    /// </param>
    /// <param name="defaultValue">
    /// The value returned when the key is not found. Defaults to an empty string.
    /// </param>
    /// <param name="bufferSize">
    /// The initial size of the internal read buffer in characters. Defaults to <see cref="DefaultBufferSize"/>.
    /// The buffer doubles automatically if the value exceeds it, up to <see cref="MaxSectionBufferSize"/>.
    /// For native key/section enumeration, the buffer must be at least 3 characters.
    /// </param>
    /// <returns>
    /// The value associated with the key, or <paramref name="defaultValue"/> if the key was not found.
    /// </returns>
    public string ReadString(string? key, string? section = null,
        string defaultValue = "", int bufferSize = DefaultBufferSize)
    {
        ValidateBufferSize(bufferSize, key is null ? 3 : 2);

        string? nativeSection = key is null ? section : NormalizeSection(section);

        while (true)
        {
            char[] buffer = new char[bufferSize];
            int length = NativeGetPrivateProfileString(nativeSection, key, defaultValue, buffer, bufferSize, _filePath);
            int truncationLength = key is null ? bufferSize - 2 : bufferSize - 1;

            // String reads signal truncation at nSize - 1; enumeration reads signal it at nSize - 2.
            if (length >= truncationLength && bufferSize < MaxSectionBufferSize)
            {
                bufferSize = Math.Min(bufferSize * 2, MaxSectionBufferSize);
                continue;
            }

            if (key is null && length >= truncationLength)
            {
                throw new InvalidOperationException(
                    $"INI data exceeds the maximum buffer size of {MaxSectionBufferSize} characters.");
            }

            return new string(buffer, 0, length);
        }
    }

    /// <summary>
    /// Reads an integer value from the specified key in the given section of the INI file.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="section">The section name, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <param name="defaultValue">
    /// The value returned when the key is not found. Defaults to <c>-1</c>.
    /// </param>
    /// <returns>
    /// The integer value of the key, or <paramref name="defaultValue"/> if the key was not found.
    /// Malformed numeric values follow the native <c>GetPrivateProfileInt</c> parsing behavior
    /// and may return <c>0</c> instead of <paramref name="defaultValue"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public int ReadInt(string key, string? section = null, int defaultValue = -1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return NativeGetPrivateProfileInt(NormalizeSection(section), key, defaultValue, _filePath);
    }

    /// <summary>
    /// Reads a boolean value from the specified key in the given section of the INI file.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="section">The section name, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <param name="defaultValue">
    /// The value returned when the key is not found or cannot be parsed. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> for stored values <c>"true"</c>, <c>"1"</c>, or <c>"yes"</c> (case-insensitive);
    /// <c>false</c> for <c>"false"</c>, <c>"0"</c>, or <c>"no"</c> (case-insensitive);
    /// otherwise <paramref name="defaultValue"/>.
    /// </returns>
    public bool ReadBool(string key, string? section = null, bool defaultValue = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        string value = ReadString(key, section);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Retrieves all key/value pairs for the specified section of the INI file.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="bufferSize">
    /// The size of the internal read buffer in characters. Defaults to <see cref="MaxSectionBufferSize"/>.
    /// </param>
    /// <returns>
    /// An array of strings in <c>"key=value"</c> format for each entry in the section.
    /// Returns an empty array if the section does not exist or contains no keys.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="section"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the section data exceeds the maximum supported buffer size.
    /// </exception>
    public string[] GetAllDataSection(string section, int bufferSize = MaxSectionBufferSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ValidateBufferSize(bufferSize);

        nint pMem = Marshal.AllocHGlobal(bufferSize * sizeof(char));
        try
        {
            int count = NativeGetPrivateProfileSection(section, pMem, bufferSize, _filePath);

            if (count <= 0)
            {
                return [];
            }

            // If count == bufferSize - 2, the buffer was too small and data was truncated.
            if (count >= bufferSize - 2)
            {
                throw new InvalidOperationException(
                    $"Section '{section}' data exceeds the maximum buffer size of {bufferSize} characters.");
            }

            string raw = Marshal.PtrToStringUni(pMem, count)!;
            return raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }
        finally
        {
            Marshal.FreeHGlobal(pMem);
        }
    }

    /// <summary>
    /// Retrieves the names of all sections in the INI file.
    /// </summary>
    /// <param name="bufferSize">
    /// The size of the internal read buffer in characters. Defaults to <see cref="MaxSectionBufferSize"/>.
    /// </param>
    /// <returns>
    /// An array of section names. Returns an empty array if the file has no sections.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the total section names data exceeds the maximum supported buffer size.
    /// </exception>
    public string[] GetAllSections(int bufferSize = MaxSectionBufferSize)
    {
        ValidateBufferSize(bufferSize);

        nint pMem = Marshal.AllocHGlobal(bufferSize * sizeof(char));
        try
        {
            int count = NativeGetPrivateProfileSectionNames(pMem, bufferSize, _filePath);

            if (count <= 0)
            {
                return [];
            }

            // If count == bufferSize - 2, the buffer was too small and data was truncated.
            if (count >= bufferSize - 2)
            {
                throw new InvalidOperationException(
                    $"Section names data exceeds the maximum buffer size of {bufferSize} characters.");
            }

            string raw = Marshal.PtrToStringUni(pMem, count)!;
            return raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }
        finally
        {
            Marshal.FreeHGlobal(pMem);
        }
    }

    /// <summary>
    /// Deletes the specified key (and its value) from the given section of the INI file.
    /// </summary>
    /// <param name="key">The key name to delete.</param>
    /// <param name="section">The section containing the key, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <returns><c>true</c> if the operation succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public bool DeleteKey(string key, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return NativeWritePrivateProfileString(NormalizeSection(section), key, null, _filePath);
    }

    /// <summary>
    /// Deletes the specified section and all of its keys from the INI file.
    /// </summary>
    /// <param name="section">The section name to delete, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <returns><c>true</c> if the operation succeeded; otherwise <c>false</c>.</returns>
    public bool DeleteSection(string? section = null)
        => NativeWritePrivateProfileString(NormalizeSection(section), null, null, _filePath);

    /// <summary>
    /// Checks whether the specified key exists in the given section of the INI file.
    /// </summary>
    /// <param name="key">The key name to check.</param>
    /// <param name="section">The section containing the key, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <returns>
    /// <c>true</c> if the key exists (even if its value is empty); otherwise <c>false</c>.
    /// </returns>
    public bool KeyExists(string key, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        char[] buffer = new char[MaxSectionBufferSize];
        // Passing null for lpKeyName returns all key names in the section separated by \0.
        int length = NativeGetPrivateProfileString(NormalizeSection(section), null, null, buffer, MaxSectionBufferSize, _filePath);
        if (length <= 0) return false;

        string raw = new string(buffer, 0, length);
        string[] keys = raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        return Array.Exists(keys, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
