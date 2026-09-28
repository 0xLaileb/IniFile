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
/// Encoding: write and delete methods create a missing or empty file as UTF-16 LE with a byte order
/// mark, the only Unicode encoding the Windows profile API supports, so any Unicode text round-trips.
/// This is best effort: if the file is locked by another process at that moment, Windows decides.
/// Files with content keep their encoding: Windows stores text in files without the UTF-16 LE byte
/// order mark (ANSI, UTF-8) in the system ANSI code page, replacing other characters with <c>?</c>,
/// and does not recognize the first section of a UTF-8 file with a byte order mark.
/// <para/>
/// Values longer than 32,767 characters are truncated on read.
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
    /// Matches the Windows API maximum for INI files (32,768 characters).
    /// </summary>
    private const int MaxSectionBufferSize = 32768;

    /// <summary>
    /// Smallest buffer for list reads (key names, section names, section entries):
    /// with a 2-character buffer Windows reports truncation as 0 characters, which is
    /// indistinguishable from an empty list.
    /// </summary>
    private const int MinListBufferSize = 3;

    /// <summary>UTF-16 LE byte order mark. Windows keeps Unicode text only in files that start with it.</summary>
    private static ReadOnlySpan<byte> Utf16LeByteOrderMark => [0xFF, 0xFE];

    /// <summary>
    /// Serializes file preparation and native writes within the process. Without it, one thread
    /// could hold the file open in <see cref="EnsureUnicodeFile"/> while another thread's native
    /// write fails with a sharing violation. Windows serializes profile writes anyway.
    /// </summary>
    private static readonly Lock WriteLock = new();

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

    /// <summary>
    /// Makes a missing or empty file UTF-16 LE by writing a byte order mark before the native write.
    /// Otherwise Windows creates the file in the ANSI code page and loses non-ANSI characters.
    /// Files with content keep their encoding. Best effort: must be called under <see cref="WriteLock"/>.
    /// </summary>
    private void EnsureUnicodeFile()
    {
        var info = new FileInfo(_filePath);
        if (info.Exists && info.Length > 0)
        {
            return;
        }

        try
        {
            using var stream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            // Devices such as NUL are not seekable and have no encoding to set.
            if (stream.CanSeek && stream.Length == 0)
            {
                stream.Write(Utf16LeByteOrderMark);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The file is locked by another process or cannot be created here (missing directory, no access).
            // The native write that follows reports real failures through its return value
            // and last error, which keeps the bool-returning contract of the write methods.
        }
    }

    /// <summary>Prepares the file encoding and performs a native write as one step.</summary>
    private bool WriteProfileString(string section, string? key, string? value)
    {
        lock (WriteLock)
        {
            EnsureUnicodeFile();
            return NativeWritePrivateProfileString(section, key, value, _filePath);
        }
    }

    /// <summary>
    /// Returns <c>true</c> when Windows yields <paramref name="probe"/> itself, i.e. the key is missing
    /// or its value equals the probe. A 3-character buffer is enough to tell the two apart from
    /// any longer value without reading it completely.
    /// </summary>
    private bool ReadsAsDefault(string key, string section, string probe)
    {
        char[] buffer = new char[MinListBufferSize];
        int length = NativeGetPrivateProfileString(section, key, probe, buffer, buffer.Length, _filePath);
        return length == 1 && buffer[0] == probe[0];
    }

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
        [Out] char[] lpReturnedString,
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
        [Out] char[] lpReturnedString,
        int nSize,
        string lpFileName);

    #endregion

    /// <summary>
    /// Calls a native function that fills a buffer with null-separated strings
    /// and splits the result into an array.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the data does not fit into the buffer.</exception>
    private static string[] ReadList(int bufferSize, Func<char[], int, int> nativeRead, string dataDescription)
    {
        ValidateBufferSize(bufferSize, MinListBufferSize);

        char[] buffer = new char[bufferSize];
        int count = nativeRead(buffer, bufferSize);

        // Windows signals a truncated list by returning nSize - 2.
        if (count >= bufferSize - 2)
        {
            throw new InvalidOperationException(
                $"{dataDescription} exceeds the buffer size of {bufferSize} characters.");
        }

        return count <= 0
            ? []
            : new string(buffer, 0, count).Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    #region Public API

    /// <summary>
    /// Writes a string value for the specified key in the given section of the INI file.
    /// If the file, section, or key does not exist, it is created automatically.
    /// A new or empty file is created as UTF-16 LE with a byte order mark so that any Unicode text is preserved.
    /// Numeric values can be written as strings (e.g. <c>"1"</c>).
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The string value to write. <c>null</c> deletes the key.</param>
    /// <param name="section">
    /// The section name. Pass <c>null</c> to target the empty section name, which Windows serializes as <c>[]</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the write succeeded; otherwise <c>false</c>. On failure, call
    /// <see cref="Marshal.GetLastPInvokeError"/> immediately to get the Win32 error code
    /// (for example, <c>3</c> when the directory does not exist).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public bool Write(string key, string? value, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return WriteProfileString(NormalizeSection(section), key, value);
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
    /// Values longer than 32,767 characters are truncated.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize"/> is outside the supported range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when enumerated names (<paramref name="key"/> is <c>null</c>) exceed 32,768 characters.
    /// </exception>
    public string ReadString(string? key, string? section = null,
        string defaultValue = "", int bufferSize = DefaultBufferSize)
    {
        ValidateBufferSize(bufferSize, key is null ? MinListBufferSize : 2);

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
    /// The size of the internal read buffer in characters, from 3 to 32768.
    /// Defaults to <see cref="MaxSectionBufferSize"/>.
    /// </param>
    /// <returns>
    /// An array of strings in <c>"key=value"</c> format for each entry in the section.
    /// Returns an empty array if the section does not exist or contains no keys.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="section"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize"/> is outside the supported range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the section data does not fit into <paramref name="bufferSize"/> characters.
    /// </exception>
    public string[] GetAllDataSection(string section, int bufferSize = MaxSectionBufferSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        return ReadList(
            bufferSize,
            (buffer, size) => NativeGetPrivateProfileSection(section, buffer, size, _filePath),
            $"Section '{section}' data");
    }

    /// <summary>
    /// Retrieves the names of all sections in the INI file.
    /// </summary>
    /// <param name="bufferSize">
    /// The size of the internal read buffer in characters, from 3 to 32768.
    /// Defaults to <see cref="MaxSectionBufferSize"/>.
    /// </param>
    /// <returns>
    /// An array of section names. Returns an empty array if the file has no sections.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize"/> is outside the supported range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the section names do not fit into <paramref name="bufferSize"/> characters.
    /// </exception>
    public string[] GetAllSections(int bufferSize = MaxSectionBufferSize)
        => ReadList(
            bufferSize,
            (buffer, size) => NativeGetPrivateProfileSectionNames(buffer, size, _filePath),
            "Section names data");

    /// <summary>
    /// Deletes the specified key (and its value) from the given section of the INI file.
    /// </summary>
    /// <param name="key">The key name to delete.</param>
    /// <param name="section">The section containing the key, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <returns>
    /// <c>true</c> if the operation succeeded; otherwise <c>false</c>. On failure, call
    /// <see cref="Marshal.GetLastPInvokeError"/> immediately to get the Win32 error code.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public bool DeleteKey(string key, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return WriteProfileString(NormalizeSection(section), key, null);
    }

    /// <summary>
    /// Deletes the specified section and all of its keys from the INI file.
    /// </summary>
    /// <param name="section">The section name to delete, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <returns>
    /// <c>true</c> if the operation succeeded; otherwise <c>false</c>. On failure, call
    /// <see cref="Marshal.GetLastPInvokeError"/> immediately to get the Win32 error code.
    /// </returns>
    public bool DeleteSection(string? section = null)
        => WriteProfileString(NormalizeSection(section), null, null);

    /// <summary>
    /// Checks whether the specified key exists in the given section of the INI file.
    /// </summary>
    /// <param name="key">The key name to check.</param>
    /// <param name="section">The section containing the key, or <c>null</c> for the empty section name serialized as <c>[]</c>.</param>
    /// <returns>
    /// <c>true</c> if the key exists (even if its value is empty); otherwise <c>false</c>.
    /// Key matching is performed by Windows, exactly as in <see cref="ReadString"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public bool KeyExists(string key, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // A missing key yields whichever default is supplied, while an existing key yields its
        // stored value for both calls. Unlike enumerating key names, this has no section size limit
        // and matches key names exactly the way ReadString does.
        string nativeSection = NormalizeSection(section);
        return !ReadsAsDefault(key, nativeSection, "\u0001")
            || !ReadsAsDefault(key, nativeSection, "\u0002");
    }

    #endregion
}
