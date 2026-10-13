using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Only paths in compiler metadata are interpreted as paths. Public copyright names
// and SourceLink HTTPS URLs are not developer filesystem locations.
if (args.Length == 0) throw new ArgumentException("Pass DLL and portable PDB paths.");
int codeViews = 0, documents = 0;
foreach (var file in args)
{
    using var stream = File.OpenRead(file);
    if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
    {
        using var pe = new PEReader(stream);
        var entries = pe.ReadDebugDirectory().Where(e => e.Type == DebugDirectoryEntryType.CodeView).ToArray();
        if (entries.Length == 0) throw new InvalidDataException("Missing CodeView: " + file);
        foreach (var entry in entries)
        {
            var value = pe.ReadCodeViewDebugDirectoryData(entry).Path;
            RequireVirtual(value);
            codeViews++;
            Console.WriteLine($"{Path.GetFileName(file)} CodeView: {value}");
        }
    }
    else if (file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
    {
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var reader = provider.GetMetadataReader();
        foreach (var handle in reader.Documents)
        {
            RequireVirtual(reader.GetString(reader.GetDocument(handle).Name));
            documents++;
        }
        // Check SourceLink keys too: manually setting PathMap alone can leave
        // physical root names here, even when document names are virtual.
        foreach (var handle in reader.CustomDebugInformation)
        {
            var info = reader.GetCustomDebugInformation(handle);
            if (reader.GetGuid(info.Kind) != new Guid("cc110556-a091-4d38-9fec-25ab9a351a6a")) continue;
            using var sourceLink = JsonDocument.Parse(reader.GetBlobBytes(info.Value));
            foreach (var mapping in sourceLink.RootElement.GetProperty("documents").EnumerateObject())
            {
                RequireVirtual(mapping.Name);
                if (!Uri.TryCreate(mapping.Value.GetString(), UriKind.Absolute, out var uri) || uri.Scheme != "https")
                    throw new InvalidDataException("Unexpected SourceLink URL: " + file);
            }
        }
    }
    else throw new ArgumentException("Expected DLL or portable PDB: " + file);
    var bytes = File.ReadAllBytes(file);
    foreach (var text in new[] { Encoding.UTF8.GetString(bytes), Encoding.Unicode.GetString(bytes), Encoding.BigEndianUnicode.GetString(bytes) })
        if (Regex.IsMatch(text, @"(?<![A-Za-z0-9_])[A-Za-z]:[\\/]|(?<![\w/])/+(?:Users|home)/", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Local absolute path in artifact: " + file);
}
Console.WriteLine($"Verified {codeViews} CodeView entries and {documents} PDB documents; local absolute paths: 0.");

static void RequireVirtual(string value)
{
    if (!Regex.IsMatch(value.Replace('\\', '/'), @"^/_(?:[0-9]+)?/", RegexOptions.CultureInvariant))
        throw new InvalidDataException("Nonvirtual compiler path: " + value);
}
