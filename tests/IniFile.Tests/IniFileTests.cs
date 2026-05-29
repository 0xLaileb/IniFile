using IniFileLib = IniFile.IniFile;

namespace IniFile.Tests;

public sealed class IniFileTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly IniFileLib _ini;

    public IniFileTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"IniFileTest_{Guid.NewGuid():N}.ini");
        _ini = new IniFileLib(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void Constructor_ValidPath_ResolvesFullPath()
    {
        // Act
        var ini = new IniFileLib("relative.ini");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(ini.FilePath));
        Assert.True(Path.IsPathRooted(ini.FilePath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new IniFileLib(path!));
    }

    [Fact]
    public void Write_And_ReadString_RoundTrip()
    {
        // Arrange & Act
        _ini.Write("Name", "Alice", "User");

        // Act
        string result = _ini.ReadString("Name", "User");

        // Assert
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void Write_ReturnsTrue_OnSuccess()
    {
        // Act
        bool ok = _ini.Write("Key", "Value", "Section");

        // Assert
        Assert.True(ok);
    }

    [Fact]
    public void Write_WithoutSection_RoundTripsThroughDefaultSection()
    {
        // Act
        bool ok = _ini.Write("Key", "Value");

        // Assert
        Assert.True(ok);
        Assert.Equal("Value", _ini.ReadString("Key"));
        Assert.True(_ini.KeyExists("Key"));
        Assert.Contains("[]", File.ReadAllText(_testFilePath));
    }

    [Fact]
    public void Write_NullOrEmptyKey_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.Write(null!, "Value"));
        Assert.ThrowsAny<ArgumentException>(() => _ini.Write("", "Value"));
        Assert.ThrowsAny<ArgumentException>(() => _ini.Write("   ", "Value"));
    }

    [Fact]
    public void Write_MultipleSections_StoresIndependently()
    {
        // Arrange
        _ini.Write("Color", "Red", "SectionA");
        _ini.Write("Color", "Blue", "SectionB");

        // Act
        string resultA = _ini.ReadString("Color", "SectionA");
        string resultB = _ini.ReadString("Color", "SectionB");

        // Assert
        Assert.Equal("Red", resultA);
        Assert.Equal("Blue", resultB);
    }

    [Fact]
    public void Write_OverwritesExistingValue()
    {
        // Arrange
        _ini.Write("Key", "First", "Section");

        // Act
        _ini.Write("Key", "Second", "Section");

        // Assert
        Assert.Equal("Second", _ini.ReadString("Key", "Section"));
    }

    [Fact]
    public void ReadString_NonExistentKey_ReturnsDefault()
    {
        // Act
        string result = _ini.ReadString("Missing", "NoSection", defaultValue: "fallback");

        // Assert
        Assert.Equal("fallback", result);
    }

    [Fact]
    public void ReadString_NonExistentKey_ReturnsEmptyByDefault()
    {
        // Act
        string result = _ini.ReadString("Missing", "NoSection");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32769)]
    public void ReadString_InvalidBufferSize_ThrowsArgumentOutOfRangeException(int bufferSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _ini.ReadString("Key", "Section", bufferSize: bufferSize));
    }

    [Fact]
    public void ReadString_NullKey_WithSmallBuffer_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _ini.ReadString(null, "Section", bufferSize: 2));
    }

    [Fact]
    public void ReadInt_ValidInteger_ReturnsValue()
    {
        // Arrange
        _ini.Write("Port", "8080", "Server");

        // Act
        int port = _ini.ReadInt("Port", "Server");

        // Assert
        Assert.Equal(8080, port);
    }

    [Fact]
    public void ReadInt_NonExistentKey_ReturnsDefault()
    {
        // Act
        int result = _ini.ReadInt("Missing", "NoSection", defaultValue: 42);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ReadInt_NullOrEmptyKey_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.ReadInt(null!));
        Assert.ThrowsAny<ArgumentException>(() => _ini.ReadInt(""));
        Assert.ThrowsAny<ArgumentException>(() => _ini.ReadInt("   "));
    }

    [Fact]
    public void ReadInt_NonNumericValue_ReturnsZero()
    {
        // Arrange
        // Windows API: if the value does not begin with a digit, GetPrivateProfileInt returns 0.
        _ini.Write("Key", "NotANumber", "Section");

        // Act
        int result = _ini.ReadInt("Key", "Section", defaultValue: -1);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("Yes", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("No", false)]
    public void ReadBool_RecognizedValues_ReturnExpected(string stored, bool expected)
    {
        // Arrange
        _ini.Write("Flag", stored, "Section");

        // Act
        bool result = _ini.ReadBool("Flag", "Section");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReadBool_UnrecognizedValue_ReturnsDefault()
    {
        // Arrange
        _ini.Write("Flag", "maybe", "Section");

        // Act & Assert
        Assert.False(_ini.ReadBool("Flag", "Section", defaultValue: false));
        Assert.True(_ini.ReadBool("Flag", "Section", defaultValue: true));
    }

    [Fact]
    public void ReadBool_NonExistentKey_ReturnsDefault()
    {
        // Act
        bool result = _ini.ReadBool("Missing", "NoSection", defaultValue: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ReadBool_NullOrEmptyKey_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.ReadBool(null!));
        Assert.ThrowsAny<ArgumentException>(() => _ini.ReadBool(""));
        Assert.ThrowsAny<ArgumentException>(() => _ini.ReadBool("   "));
    }

    [Fact]
    public void GetAllSections_MultipleSections_ReturnsAll()
    {
        // Arrange
        _ini.Write("K", "V", "Alpha");
        _ini.Write("K", "V", "Beta");
        _ini.Write("K", "V", "Gamma");

        // Act
        string[] sections = _ini.GetAllSections();

        // Assert
        Assert.Contains("Alpha", sections);
        Assert.Contains("Beta", sections);
        Assert.Contains("Gamma", sections);
        Assert.Equal(3, sections.Length);
    }

    [Fact]
    public void GetAllSections_EmptyFile_ReturnsEmptyArray()
    {
        // Act
        string[] sections = _ini.GetAllSections();

        // Assert
        Assert.Empty(sections);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32769)]
    public void GetAllSections_InvalidBufferSize_ThrowsArgumentOutOfRangeException(int bufferSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ini.GetAllSections(bufferSize));
    }

    [Fact]
    public void GetAllDataSection_MultipleKeys_ReturnsAllPairs()
    {
        // Arrange
        _ini.Write("Host", "localhost", "DB");
        _ini.Write("Port", "5432", "DB");
        _ini.Write("Name", "mydb", "DB");

        // Act
        string[] pairs = _ini.GetAllDataSection("DB");

        // Assert
        Assert.Equal(3, pairs.Length);
        Assert.Contains("Host=localhost", pairs);
        Assert.Contains("Port=5432", pairs);
        Assert.Contains("Name=mydb", pairs);
    }

    [Fact]
    public void GetAllDataSection_NonExistentSection_ReturnsEmptyArray()
    {
        // Act
        string[] pairs = _ini.GetAllDataSection("Ghost");

        // Assert
        Assert.Empty(pairs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32769)]
    public void GetAllDataSection_InvalidBufferSize_ThrowsArgumentOutOfRangeException(int bufferSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ini.GetAllDataSection("Section", bufferSize));
    }

    [Fact]
    public void DeleteKey_RemovesKeyFromSection()
    {
        // Arrange
        _ini.Write("Temp", "123", "Cache");
        Assert.True(_ini.KeyExists("Temp", "Cache"));

        // Act
        _ini.DeleteKey("Temp", "Cache");

        // Assert
        Assert.False(_ini.KeyExists("Temp", "Cache"));
    }

    [Fact]
    public void DeleteKey_ReturnsTrue_OnSuccess()
    {
        // Arrange
        _ini.Write("Key", "Val", "Sec");

        // Act
        bool ok = _ini.DeleteKey("Key", "Sec");

        // Assert
        Assert.True(ok);
    }

    [Fact]
    public void DeleteKey_WithoutSection_RemovesKeyFromDefaultSection()
    {
        // Arrange
        _ini.Write("Key", "Val");

        // Act
        bool ok = _ini.DeleteKey("Key");

        // Assert
        Assert.True(ok);
        Assert.False(_ini.KeyExists("Key"));
    }

    [Fact]
    public void DeleteSection_RemovesEntireSection()
    {
        // Arrange
        _ini.Write("A", "1", "Temp");
        _ini.Write("B", "2", "Temp");

        // Act
        _ini.DeleteSection("Temp");

        // Assert
        Assert.False(_ini.KeyExists("A", "Temp"));
        Assert.False(_ini.KeyExists("B", "Temp"));
        string[] sections = _ini.GetAllSections();
        Assert.DoesNotContain("Temp", sections);
    }

    [Fact]
    public void DeleteSection_ReturnsTrue_OnSuccess()
    {
        // Arrange
        _ini.Write("K", "V", "ToDelete");

        // Act
        bool ok = _ini.DeleteSection("ToDelete");

        // Assert
        Assert.True(ok);
    }

    [Fact]
    public void DeleteSection_WithoutSection_RemovesDefaultSection()
    {
        // Arrange
        _ini.Write("K", "V");

        // Act
        bool ok = _ini.DeleteSection();

        // Assert
        Assert.True(ok);
        Assert.False(_ini.KeyExists("K"));
    }

    [Fact]
    public void KeyExists_ExistingKey_ReturnsTrue()
    {
        // Arrange
        _ini.Write("Language", "CSharp", "Prefs");

        // Act & Assert
        Assert.True(_ini.KeyExists("Language", "Prefs"));
    }

    [Fact]
    public void KeyExists_NonExistentKey_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_ini.KeyExists("Ghost", "NoSection"));
    }

    [Fact]
    public void KeyExists_NullOrEmptyKey_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.KeyExists(null!));
        Assert.ThrowsAny<ArgumentException>(() => _ini.KeyExists(""));
        Assert.ThrowsAny<ArgumentException>(() => _ini.KeyExists("   "));
    }

    [Fact]
    public void KeyExists_AfterDeletion_ReturnsFalse()
    {
        // Arrange
        _ini.Write("Key", "Value", "Section");

        // Act
        _ini.DeleteKey("Key", "Section");

        // Assert
        Assert.False(_ini.KeyExists("Key", "Section"));
    }

    [Fact]
    public void FilePath_ReturnsFullPath()
    {
        // Act & Assert
        Assert.Equal(_testFilePath, _ini.FilePath);
    }

    [Fact]
    public void ReadString_SpecialCharacters_PreservedCorrectly()
    {
        // Arrange
        const string value = "Hello, World! @#$%^&*()";
        _ini.Write("Special", value, "Test");

        // Act
        string result = _ini.ReadString("Special", "Test");

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Write_EmptyValue_ReadStringReturnsEmpty()
    {
        // Arrange
        _ini.Write("Empty", "", "Section");

        // Act
        string result = _ini.ReadString("Empty", "Section");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ReadString_LongValue_HandledCorrectly()
    {
        // Arrange
        string longValue = new('X', 500);
        _ini.Write("Long", longValue, "Section");

        // Act
        string result = _ini.ReadString("Long", "Section");

        // Assert
        Assert.Equal(longValue, result);
    }

    [Fact]
    public void ReadString_ValueExceedsDefaultBuffer_AutoExpandsAndReturnsFullValue()
    {
        // Arrange: value longer than default buffer (1024 chars)
        string longValue = new('Z', 1500);
        _ini.Write("Big", longValue, "Section");

        // Act: default bufferSize=1024, should auto-expand
        string result = _ini.ReadString("Big", "Section");

        // Assert
        Assert.Equal(longValue, result);
    }

    [Theory]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(32766)]
    [InlineData(32767)]
    public void ReadString_ValueAtBufferBoundary_ReturnsFullValue(int length)
    {
        // Arrange
        string value = new('A', length);
        _ini.Write("Boundary", value, "Section");

        // Act
        string result = _ini.ReadString("Boundary", "Section");

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void KeyExists_KeyWithEmptyValue_ReturnsTrue()
    {
        // Arrange: write a key with empty value
        _ini.Write("Empty", "", "Section");

        // Act & Assert: key exists even with empty value
        Assert.True(_ini.KeyExists("Empty", "Section"));
    }

    [Fact]
    public void KeyExists_AfterWriteEmptyValue_StillExists()
    {
        // Arrange
        _ini.Write("Key", "Value", "Section");
        _ini.Write("Key", "", "Section"); // overwrite with empty

        // Act & Assert
        Assert.True(_ini.KeyExists("Key", "Section"));
    }

    [Fact]
    public void ReadInt_NegativeValue_ReturnsNegative()
    {
        // Windows API GetPrivateProfileInt correctly parses negative integer strings.
        _ini.Write("Neg", "-42", "Section");

        int result = _ini.ReadInt("Neg", "Section", defaultValue: 99);

        Assert.Equal(-42, result);
    }

    [Fact]
    public void ReadInt_MaxIntValue_ReturnsCorrectly()
    {
        // Arrange
        _ini.Write("Max", "2147483647", "Section");

        // Act
        int result = _ini.ReadInt("Max", "Section");

        // Assert
        Assert.Equal(2147483647, result);
    }

    [Fact]
    public void ReadInt_HexValue_ParsedByWindowsApi()
    {
        // Windows API natively parses 0x-prefixed hex values
        _ini.Write("Hex", "0xFF", "Section");

        int result = _ini.ReadInt("Hex", "Section");

        Assert.Equal(255, result);
    }

    [Fact]
    public void ReadString_NullKey_ReturnsKeyNamesForSection()
    {
        // Arrange
        _ini.Write("First", "1", "Alpha");
        _ini.Write("Second", "2", "Alpha");

        // Act
        string keyNames = _ini.ReadString(null, "Alpha");
        string[] keys = keyNames.Split('\0', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.Contains("First", keys);
        Assert.Contains("Second", keys);
    }

    [Fact]
    public void DeleteKey_NullOrEmptyKey_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.DeleteKey(null!));
        Assert.ThrowsAny<ArgumentException>(() => _ini.DeleteKey(""));
        Assert.ThrowsAny<ArgumentException>(() => _ini.DeleteKey("   "));
    }

    [Fact]
    public void GetAllDataSection_NullSection_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.GetAllDataSection(null!));
    }

    [Fact]
    public void GetAllDataSection_EmptySection_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => _ini.GetAllDataSection(""));
    }
}
