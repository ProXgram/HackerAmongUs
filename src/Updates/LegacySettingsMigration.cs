#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HAU.Updates;

public sealed record SettingsMigrationFile(string SourcePath, string DestinationPath, byte[] OriginalBytes,
    byte[] Bytes, byte[]? PreviousDestinationBytes = null);
public sealed record SettingsMigrationResult(bool ConfigImported, bool ProfileImported, IReadOnlyList<string> ImportedPaths);


public static class LegacySettingsMigration
{
    private const int MaximumSettingsBytes = 2 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly string[] LegacyConfigs = { "MalumMenu.cfg", "MalumMenuEnhanced.cfg", "HaddadMenu.cfg" };

    public static string TranslateConfigText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Regex.Replace(text,
            @"(?m)^([ \t]*)\[(?:MalumMenuEnhanced|MalumMenu|HaddadMenu)(\.[^\]\r\n]+)?\]([ \t]*)(\r?)$",
            "$1[HAU$2]$3$4", RegexOptions.CultureInvariant);
    }


    public static IReadOnlyList<SettingsMigrationFile> PlanForInstall(string configRoot)
        => Plan(NormalizeConfigRoot(configRoot), false);


    public static SettingsMigrationResult ImportForStartup(string configRoot, string profilePath)
        => ImportForStartup(configRoot, profilePath, null);

    internal static SettingsMigrationResult ImportForStartup(string configRoot, string profilePath, Action<string>? beforeWrite)
    {
        var requestedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configRoot));
        if (!Path.GetFullPath(profilePath).Equals(Path.Combine(requestedRoot, "HAUProfile.txt"), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The HAU profile must be stored beside its configuration.");
        var root = NormalizeConfigRoot(requestedRoot);
        var files = Plan(root, true);
        if (files.Count == 0) return new(false, false, Array.Empty<string>());
        var createdDirectories = new List<string>();
        var written = new List<SettingsMigrationFile>();
        try
        {
            foreach (var file in files)
            {
                RequireUnchanged(file);
                EnsureDirectories(Path.GetDirectoryName(file.DestinationPath)!, createdDirectories);
                beforeWrite?.Invoke(file.DestinationPath);
                WriteAtomic(file.DestinationPath, file.Bytes, file.PreviousDestinationBytes != null);
                written.Add(file);
            }
        }
        catch (Exception importError)
        {
            var restoreErrors = new List<Exception>();
            foreach (var file in Enumerable.Reverse(written))
            {
                try
                {
                    CheckPath(file.DestinationPath);
                    if (file.PreviousDestinationBytes == null) File.Delete(file.DestinationPath);
                    else WriteAtomic(file.DestinationPath, file.PreviousDestinationBytes, true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { restoreErrors.Add(ex); }
            }
            foreach (var directory in Enumerable.Reverse(createdDirectories))
            {
                try { CheckPath(directory); Directory.Delete(directory, false); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { restoreErrors.Add(ex); }
            }
            if (restoreErrors.Count != 0)
                throw new IOException("The settings import stopped and could not fully restore its copied files. The legacy originals remain intact.",
                    new AggregateException(new[] { importError }.Concat(restoreErrors)));
            throw;
        }
        return new(written.Any(f => Path.GetFileName(f.DestinationPath) == "HAU.cfg"),
            written.Any(f => Path.GetFileName(f.DestinationPath) == "HAUProfile.txt"), written.Select(f => f.DestinationPath).ToArray());
    }

    private static IReadOnlyList<SettingsMigrationFile> Plan(string root, bool allowFreshConfig)
    {
        CheckPath(root);
        var result = new List<SettingsMigrationFile>();
        var config = Path.Combine(root, "HAU.cfg");
        CheckPath(config);
        if (Directory.Exists(config)) throw new IOException("A folder is blocking the HAU configuration file. Its contents were preserved.");
        byte[]? previous = null;
        var configAbsent = !File.Exists(config) && !Directory.Exists(config);
        if (!configAbsent && allowFreshConfig && File.Exists(config))
        {
            previous = ReadBytes(config);
            var contents = Decode(previous);
            configAbsent = contents.Split('\n').All(line => string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal));
            if (!configAbsent) previous = null;
        }
        if (configAbsent)
        {
            foreach (var name in LegacyConfigs)
            {
                var source = Path.Combine(root, name);
                CheckPath(source);
                if (!File.Exists(source)) continue;
                var original = ReadBytes(source);
                result.Add(new(source, config, original, Utf8.GetBytes(TranslateConfigText(Decode(original))), previous));
                break;
            }
        }
        var profile = Path.Combine(root, "HAUProfile.txt");
        var legacyProfile = Path.Combine(root, "MalumProfile.txt");
        CheckPath(profile); CheckPath(legacyProfile);
        if (Directory.Exists(profile)) throw new IOException("A folder is blocking the HAU profile file. Its contents were preserved.");
        if (!File.Exists(profile) && !Directory.Exists(profile) && File.Exists(legacyProfile))
        {
            var original = ReadBytes(legacyProfile);
            result.Add(new(legacyProfile, profile, original, original));
        }
        return result;
    }

    public static void RequireUnchanged(SettingsMigrationFile file)
    {
        CheckPath(file.SourcePath); CheckPath(file.DestinationPath);
        if (!ReadBytes(file.SourcePath).SequenceEqual(file.OriginalBytes))
            throw new IOException("Your legacy settings changed during the import. They were preserved; try again.");
        if (file.PreviousDestinationBytes == null)
        {
            if (File.Exists(file.DestinationPath) || Directory.Exists(file.DestinationPath))
                throw new IOException("HAU settings appeared during the import and were preserved.");
        }
        else if (!ReadBytes(file.DestinationPath).SequenceEqual(file.PreviousDestinationBytes))
            throw new IOException("Your HAU settings changed during the import and were preserved.");
    }

    private static byte[] ReadBytes(string path)
    {
        CheckPath(path);
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length > MaximumSettingsBytes) throw new InvalidDataException("The saved settings file is larger than expected.");
        var bytes = new byte[(int)input.Length];
        var position = 0;
        while (position < bytes.Length)
        {
            var read = input.Read(bytes, position, bytes.Length - position);
            if (read == 0) throw new IOException("The saved settings changed while reading.");
            position += read;
        }
        if (input.ReadByte() != -1) throw new IOException("The saved settings changed while reading.");
        return bytes;
    }

    private static string Decode(byte[] bytes)
    {
        using var input = new MemoryStream(bytes, false);
        using var reader = new StreamReader(input, Utf8, true);
        return reader.ReadToEnd();
    }

    private static void WriteAtomic(string destination, byte[] bytes, bool replace)
    {
        CheckPath(destination);
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!, ".hau-import-" + Guid.NewGuid().ToString("N") + ".tmp");
        CheckPath(temporary);
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
            CheckPath(destination);
            File.Move(temporary, destination, replace);
        }
        finally { CheckPath(temporary); if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void EnsureDirectories(string path, List<string> created)
    {
        CheckPath(path);
        if (Directory.Exists(path)) return;
        var parent = Path.GetDirectoryName(path);
        if (parent != null) EnsureDirectories(parent, created);
        Directory.CreateDirectory(path);
        created.Add(path);
    }

    internal static string NormalizeConfigRoot(string configRoot, string? windowsAppsRoot = null)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configRoot));
        var bepInEx = Path.GetDirectoryName(full);
        var game = bepInEx == null ? null : Path.GetDirectoryName(bepInEx);
        if (game != null && Path.GetFileName(full).Equals("config", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileName(bepInEx)?.Equals("BepInEx", StringComparison.OrdinalIgnoreCase) == true)
        {
            var package = new DirectoryInfo(game);
            windowsAppsRoot ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            if (package.Parent?.FullName.Equals(Path.GetFullPath(windowsAppsRoot), StringComparison.OrdinalIgnoreCase) == true &&
                package.Name.Length <= 160 && Regex.IsMatch(package.Name,
                    @"^Innersloth[.]AmongUs_[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+_x64__fw5x688tam7rm$", RegexOptions.CultureInvariant) &&
                (package.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                CheckPath(windowsAppsRoot);
                var target = package.ResolveLinkTarget(true) as DirectoryInfo
                    ?? throw new IOException("The Xbox game settings target could not be resolved.");
                var targetRoot = target.FullName;
                if (targetRoot.StartsWith(@"\\?\", StringComparison.Ordinal) && targetRoot.Length >= 7 && targetRoot[5] == ':') targetRoot = targetRoot[4..];
                targetRoot = Path.GetFullPath(targetRoot);
                CheckPath(targetRoot);
                var info = Path.Combine(targetRoot, "Among Us_Data", "app.info");
                var bytes = ReadBytes(info);
                if (bytes.Length > 4096 || Decode(bytes).Replace("\r", "").TrimEnd('\n') != "Innersloth\nAmong Us")
                    throw new InvalidDataException("The Xbox settings target does not identify Among Us.");
                full = Path.Combine(targetRoot, "BepInEx", "config");
            }
        }
        CheckPath(full);
        return full;
    }

    private static void CheckPath(string path)
    {
        string? current = Path.GetFullPath(path);
        while (current != null)
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked saved-settings files or folders cannot be imported safely.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            current = Path.GetDirectoryName(current);
        }
    }
}
