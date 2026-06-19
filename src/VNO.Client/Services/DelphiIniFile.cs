using System;
using System.Collections.Generic;
using System.IO;

namespace VNO.Client.Services;

/// <summary>
/// Minimal reader for the Delphi style ini files the legacy client shipped
/// </summary>
/// <remarks>
/// The legacy data files (settings.ini and each theme design.ini) are plain
/// [Section] key=value text with free form comment lines. Windows ini lookups
/// are case insensitive and return the first occurrence of a duplicated key,
/// so this reader keeps both behaviors. A missing file yields an empty
/// document so every read falls back to its default, matching how the
/// original client tolerated absent data files
/// </remarks>
public sealed class DelphiIniFile
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    private DelphiIniFile()
    {
    }

    /// <summary>
    /// Loads an ini file, returning an empty document when the file is missing or unreadable
    /// </summary>
    public static DelphiIniFile Load(string path)
    {
        var ini = new DelphiIniFile();
        string[] lines;
        try
        {
            if (!File.Exists(path))
            {
                return ini;
            }
            lines = File.ReadAllLines(path);
        }
        catch (IOException)
        {
            return ini;
        }
        catch (UnauthorizedAccessException)
        {
            return ini;
        }

        Dictionary<string, string>? current = null;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) ||
                line.StartsWith(@"\\", StringComparison.Ordinal) || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var name = line[1..^1].Trim();
                if (!ini._sections.TryGetValue(name, out current))
                {
                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    ini._sections[name] = current;
                }
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0 || current is null)
            {
                continue;
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            // first occurrence wins, like GetPrivateProfileString
            current.TryAdd(key, value);
        }

        return ini;
    }

    /// <summary>
    /// Reads a string value or the fallback when the section or key is absent
    /// </summary>
    public string ReadString(string section, string key, string fallback)
    {
        if (_sections.TryGetValue(section, out var values) &&
            values.TryGetValue(key, out var value) && value.Length > 0)
        {
            return value;
        }
        return fallback;
    }

    /// <summary>
    /// Reads a numeric value or the fallback when absent or unparseable
    /// </summary>
    public double ReadDouble(string section, string key, double fallback)
    {
        var text = ReadString(section, key, string.Empty);
        return double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    /// <summary>
    /// Reads an integer value or the fallback when absent or unparseable
    /// </summary>
    public int ReadInteger(string section, string key, int fallback)
    {
        var text = ReadString(section, key, string.Empty);
        return int.TryParse(text, out var value) ? value : fallback;
    }

    /// <summary>
    /// Updates or adds one value in the loaded document
    /// </summary>
    public void SetValue(string section, string key, string value)
    {
        if (!_sections.TryGetValue(section, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = values;
        }
        values[key] = value;
    }

    /// <summary>
    /// Updates one value in an ini file on disk, editing in place so the free
    /// form comment lines the legacy files carry survive the write
    /// </summary>
    public static void WriteValue(string path, string section, string key, string value)
    {
        var lines = File.Exists(path)
            ? new List<string>(File.ReadAllLines(path))
            : new List<string>();

        var sectionStart = -1;
        var sectionEnd = lines.Count;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith('[') || !line.EndsWith(']'))
            {
                continue;
            }
            if (sectionStart >= 0)
            {
                sectionEnd = i;
                break;
            }
            if (string.Equals(line[1..^1].Trim(), section, StringComparison.OrdinalIgnoreCase))
            {
                sectionStart = i;
            }
        }

        if (sectionStart < 0)
        {
            if (lines.Count > 0 && lines[^1].Trim().Length > 0)
            {
                lines.Add(string.Empty);
            }
            lines.Add($"[{section}]");
            lines.Add($"{key}={value}");
        }
        else
        {
            var replaced = false;
            for (var i = sectionStart + 1; i < sectionEnd; i++)
            {
                var line = lines[i].Trim();
                var eq = line.IndexOf('=');
                if (eq <= 0 || line.StartsWith("//", StringComparison.Ordinal) ||
                    line.StartsWith(@"\\", StringComparison.Ordinal) || line.StartsWith(';'))
                {
                    continue;
                }
                if (string.Equals(line[..eq].Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{key}={value}";
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
            {
                lines.Insert(sectionStart + 1, $"{key}={value}");
            }
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllLines(path, lines);
    }
}
