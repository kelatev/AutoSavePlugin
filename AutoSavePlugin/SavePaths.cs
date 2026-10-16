using System.Globalization;

namespace AutoSavePlugin;

/// <summary>Pure helpers that decide where and what is written. No IDE calls here.</summary>
/// <remarks>
/// Kept free of IDE and Win32 calls so it can be covered by unit tests
/// (see AutoSavePlugin.Tests). Saved files are laid out as
/// <c>root\database\owner\type\name\timestamp\Before|After\name.sql|pck</c>.
/// </remarks>
public static class SavePaths
{
    // Characters Windows does not allow in file and folder names
    private static readonly char[] InvalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>Makes a string usable as a single file or folder name
    /// (e.g. "host:1521/service" connection strings).</summary>
    /// <remarks>
    /// Invalid and control characters become "_", so a value can never add extra folder levels
    /// or escape the root folder. An empty result becomes "_" so no path segment is lost.
    /// </remarks>
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
    /// <remarks>
    /// Fixed-width fields (e.g. "2026.01.02 03-04-05.007") make folders sort chronologically
    /// by name. ":" is not allowed in paths, so "-" separates the time parts.
    /// </remarks>
    public static string Timestamp(DateTime time) =>
        time.ToString("yyyy'.'MM'.'dd HH'-'mm'-'ss'.'fff", CultureInfo.InvariantCulture);

    /// <summary>Package spec and body are stored together in one .pck file.</summary>
    public static bool IsPackage(string objectType) =>
        objectType is "PACKAGE" or "PACKAGE BODY";

    /// <summary>Full path of the file for one saved copy of an object.</summary>
    /// <param name="root">Root folder from the plug-in preferences.</param>
    /// <param name="database">Database (TNS alias or connect string) of the window's connection.</param>
    /// <param name="owner">Schema that owns the object.</param>
    /// <param name="objectType">Object type as reported by the IDE, e.g. "PROCEDURE" or "PACKAGE BODY".</param>
    /// <param name="objectName">Object name.</param>
    /// <param name="timestamp">Execution timestamp from <see cref="Timestamp"/>.</param>
    /// <param name="before">true for the copy taken before compilation, false for the one after.</param>
    public static string FilePath(string root, string database, string owner, string objectType,
                                  string objectName, string timestamp, bool before)
    {
        // Spec and body of a package share one folder and one .pck file, the same format
        // PL/SQL Developer uses when it saves a package to a file.
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
    /// <remarks>
    /// Trailing blank lines of the spec are dropped so "/" always sits on its own line,
    /// which is what SQL*Plus and PL/SQL Developer need to run the file as two statements.
    /// Works on raw bytes to keep the IDE's encoding untouched.
    /// </remarks>
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
