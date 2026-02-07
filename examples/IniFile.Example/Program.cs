using Ini = IniFile.IniFile;

// ---------------------------------------------------------------------------
//  IniFile — Example Application
//  Demonstrates every public method of the IniFile library.
// ---------------------------------------------------------------------------

const string fileName = "example.ini";

// Clean up any leftover file from a previous run.
if (File.Exists(fileName))
{
    File.Delete(fileName);
}

var ini = new Ini(fileName);

Console.WriteLine("=== IniFile Example ===");
Console.WriteLine($"Working with: {ini.FilePath}");
Console.WriteLine();

// 1. Write string values
Console.WriteLine("--- 1. Writing values ---");
ini.Write("Host", "localhost", "Database");
ini.Write("Port", "5432", "Database");
ini.Write("Name", "app_db", "Database");
ini.Write("Username", "admin", "Database");

ini.Write("Level", "Information", "Logging");
ini.Write("Enabled", "true", "Logging");

ini.Write("Width", "1920", "Window");
ini.Write("Height", "1080", "Window");
ini.Write("Fullscreen", "0", "Window");

Console.WriteLine("Values written to [Database], [Logging], and [Window] sections.");
Console.WriteLine();

// 2. Read string values
Console.WriteLine("--- 2. Reading string values ---");
string host = ini.ReadString("Host", "Database");
string port = ini.ReadString("Port", "Database");
Console.WriteLine($"  Database Host = {host}");
Console.WriteLine($"  Database Port = {port}");
Console.WriteLine();

// 3. Read integer values
Console.WriteLine("--- 3. Reading integer values ---");
int width = ini.ReadInt("Width", "Window");
int height = ini.ReadInt("Height", "Window");
Console.WriteLine($"  Window size = {width} x {height}");

int missing = ini.ReadInt("NonExistent", "Window", defaultValue: 60);
Console.WriteLine($"  Missing key with default = {missing}");
Console.WriteLine();

// 4. Read boolean values  (supports true/false, 1/0, yes/no)
Console.WriteLine("--- 4. Reading boolean values ---");
bool loggingEnabled = ini.ReadBool("Enabled", "Logging");
bool fullscreen = ini.ReadBool("Fullscreen", "Window");
Console.WriteLine($"  Logging enabled = {loggingEnabled}");
Console.WriteLine($"  Fullscreen      = {fullscreen}");
Console.WriteLine();

// 5. Read with default for a missing key
Console.WriteLine("--- 5. Default value for missing key ---");
string theme = ini.ReadString("Theme", "UI", defaultValue: "dark");
Console.WriteLine($"  Theme (missing) = \"{theme}\"");
Console.WriteLine();

// 6. List all sections
Console.WriteLine("--- 6. All sections ---");
string[] sections = ini.GetAllSections();
Console.WriteLine($"  Sections ({sections.Length}): {string.Join(", ", sections)}");
Console.WriteLine();

// 7. List all key=value pairs in a section
Console.WriteLine("--- 7. All entries in [Database] ---");
string[] entries = ini.GetAllDataSection("Database");
foreach (string entry in entries)
{
    Console.WriteLine($"  {entry}");
}

Console.WriteLine();

// 8. Check key existence
Console.WriteLine("--- 8. Key existence ---");
Console.WriteLine($"  KeyExists(\"Host\", \"Database\")   = {ini.KeyExists("Host", "Database")}");
Console.WriteLine($"  KeyExists(\"Ghost\", \"Database\")  = {ini.KeyExists("Ghost", "Database")}");
Console.WriteLine();

// 9. Overwrite a value
Console.WriteLine("--- 9. Overwriting a value ---");
Console.WriteLine($"  Before: Level = {ini.ReadString("Level", "Logging")}");
ini.Write("Level", "Debug", "Logging");
Console.WriteLine($"  After:  Level = {ini.ReadString("Level", "Logging")}");
Console.WriteLine();

// 10. Delete a key
Console.WriteLine("--- 10. Deleting a key ---");
Console.WriteLine($"  Before delete — Username exists: {ini.KeyExists("Username", "Database")}");
ini.DeleteKey("Username", "Database");
Console.WriteLine($"  After delete  — Username exists: {ini.KeyExists("Username", "Database")}");
Console.WriteLine();

// 11. Delete a section
Console.WriteLine("--- 11. Deleting a section ---");
Console.WriteLine($"  Sections before: {string.Join(", ", ini.GetAllSections())}");
ini.DeleteSection("Window");
Console.WriteLine($"  Sections after:  {string.Join(", ", ini.GetAllSections())}");
Console.WriteLine();

// 12. Print the raw INI file contents
Console.WriteLine("--- 12. Raw INI file contents ---");
Console.WriteLine(File.ReadAllText(ini.FilePath));

// Clean up
File.Delete(fileName);
Console.WriteLine("Done. Temporary file deleted.");
