using System.Globalization;

namespace AutoSavePlugin;

/// <summary>Pure helpers that decide where and what is written. No IDE calls here.</summary>
public static class SavePaths
{
    private static readonly char[] InvalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>Makes a string usable as a single file or folder name
    /// (e.g. "host:1521/service" connection strings).</summary>
    public static string SafeName(string? value)
    {
        var chars = (value ?? "").Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] < ' ' || Array.IndexOf(InvalidChars, chars[i]) >= 0)
                chars[i] = '_';
        }

        // Windows silently drops trailing dots and spaces from folder names
        var result = new string(chars).TrimEnd('.', ' ');
        return result.Length == 0 ? "_" : result;
    }

    /// <summary>Timestamp folder shared by the Before and After copies of one execution.</summary>
    public static string Timestamp(DateTime time) =>
        time.ToString("yyyy'.'MM'.'dd HH'-'mm'-'ss'.'fff", CultureInfo.InvariantCulture);

    /// <summary>Package spec and body are stored together in one .pck file.</summary>
    public static bool IsPackage(string objectType) =>
        objectType is "PACKAGE" or "PACKAGE BODY";

    public static string FilePath(string root, string database, string owner, string objectType,
                                  string objectName, string timestamp, bool before)
    {
        var type = IsPackage(objectType) ? "PACKAGE" : objectType;
        var extension = IsPackage(objectType) ? "pck" : "sql";

        var folder = Path.Combine(
            root.TrimEnd('\\', '/'),
            SafeName(database),
            SafeName(owner),
            SafeName(type),
            SafeName(objectName),
            timestamp,
            before ? "Before" : "After");

        return Path.Combine(folder, SafeName(objectName) + "." + extension);
    }

    /// <summary>Joins package spec and body with a "/" line between them.</summary>
    public static byte[] JoinPackage(ReadOnlySpan<byte> spec, ReadOnlySpan<byte> body)
    {
        var end = spec.Length;
        while (end > 0 && spec[end - 1] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            end--;

        ReadOnlySpan<byte> separator = "\r\n/\r\n"u8;
        var result = new byte[end + separator.Length + body.Length];
        spec[..end].CopyTo(result);
        separator.CopyTo(result.AsSpan(end));
        body.CopyTo(result.AsSpan(end + separator.Length));
        return result;
    }
}
