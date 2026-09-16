using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace InvoicePortal.DbInit;

/// <summary>
/// Produces a copy of an Azure SQL bacpac that can be imported into on-premises SQL Server.
/// Azure-only security objects (Entra users, role memberships, grants, master key, scoped
/// credentials) are removed from model.xml and the TDE flag is cleared. The model checksum in
/// Origin.xml is recomputed so DacFx accepts the modified package. The source file is never modified.
/// </summary>
public static class BacpacSanitizer
{
    private static readonly HashSet<string> ElementTypesToRemove = new(StringComparer.Ordinal)
    {
        "SqlUser",
        "SqlRoleMembership",
        "SqlPermissionStatement",
        "SqlDatabaseCredential",
        "SqlMasterKey",
    };

    public static void Create(string sourcePath, string destinationPath, Action<string> log)
    {
        using var source = ZipFile.OpenRead(sourcePath);

        var modelEntry = source.GetEntry("model.xml") ?? throw new InvalidOperationException("model.xml not found in bacpac.");
        var originEntry = source.GetEntry("Origin.xml") ?? throw new InvalidOperationException("Origin.xml not found in bacpac.");

        var modelBytes = SanitizeModel(modelEntry, log);
        var checksum = Convert.ToHexString(SHA256.HashData(modelBytes));
        var originBytes = RewriteOriginChecksum(originEntry, checksum);

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        using var destinationStream = File.Create(destinationPath);
        using var destination = new ZipArchive(destinationStream, ZipArchiveMode.Create);

        foreach (var entry in source.Entries)
        {
            var target = destination.CreateEntry(entry.FullName, CompressionLevel.Optimal);
            using var output = target.Open();

            if (entry.FullName == "model.xml")
            {
                output.Write(modelBytes);
            }
            else if (entry.FullName == "Origin.xml")
            {
                output.Write(originBytes);
            }
            else
            {
                using var input = entry.Open();
                input.CopyTo(output);
            }
        }

        log($"Sanitized bacpac written to {destinationPath} (model.xml SHA-256 {checksum}).");
    }

    private static byte[] SanitizeModel(ZipArchiveEntry modelEntry, Action<string> log)
    {
        XDocument doc;
        using (var stream = modelEntry.Open())
        {
            doc = XDocument.Load(stream);
        }

        var root = doc.Root ?? throw new InvalidOperationException("model.xml has no root element.");
        XNamespace ns = root.GetDefaultNamespace();

        var toRemove = doc.Descendants(ns + "Element")
            .Where(e => ElementTypesToRemove.Contains((string?)e.Attribute("Type") ?? string.Empty))
            .ToList();

        foreach (var group in toRemove.GroupBy(e => (string?)e.Attribute("Type")))
        {
            log($"Removing {group.Count()} x {group.Key}");
        }

        foreach (var element in toRemove)
        {
            element.Remove();
        }

        var encryptionFlags = doc.Descendants(ns + "Element")
            .Where(e => (string?)e.Attribute("Type") == "SqlDatabaseOptions")
            .Elements(ns + "Property")
            .Where(p => (string?)p.Attribute("Name") == "IsEncryptionOn")
            .ToList();

        foreach (var flag in encryptionFlags)
        {
            flag.SetAttributeValue("Value", "False");
            log("Set SqlDatabaseOptions.IsEncryptionOn = False");
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    private static byte[] RewriteOriginChecksum(ZipArchiveEntry originEntry, string checksum)
    {
        XDocument doc;
        using (var stream = originEntry.Open())
        {
            doc = XDocument.Load(stream);
        }

        var checksumElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Checksum" && (string?)e.Attribute("Uri") == "/model.xml")
            ?? throw new InvalidOperationException("Origin.xml has no checksum entry for /model.xml.");

        checksumElement.Value = checksum;

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }
}
