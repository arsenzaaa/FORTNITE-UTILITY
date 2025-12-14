using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FortniteUtility;

internal record VersionSnapshot(string Time, string Version, string Skip);

internal static class VersionManager
{
    private const string CurrentVersion = "0.0.1";
    private const string VersionUrl = "https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/main/FortniteUtility/version.txt";
    public const string ReleaseUrl = "https://github.com/arsenzaaa/FORTNITE-UTILITY/releases";
    private static readonly string VersionDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteUtility");
    private static readonly string VersionFile = Path.Combine(VersionDir, "version.txt");
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };

    public static string Current => CurrentVersion;
    public static string VersionFilePath => VersionFile;

    public static VersionSnapshot EnsureSnapshot()
    {
        Directory.CreateDirectory(VersionDir);
        if (!File.Exists(VersionFile))
        {
            SaveSnapshot(DateTime.Now, CurrentVersion, string.Empty);
            return new VersionSnapshot(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), CurrentVersion, string.Empty);
        }

        var snapshot = ReadSnapshot();
        if (!string.Equals(snapshot.Version, CurrentVersion, StringComparison.OrdinalIgnoreCase))
        {
            var time = DateTime.Now;
            if (DateTime.TryParse(snapshot.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                time = parsed;
            }

            SaveSnapshot(time, CurrentVersion, snapshot.Skip);
            return new VersionSnapshot(time.ToString("yyyy-MM-dd HH:mm:ss"), CurrentVersion, snapshot.Skip);
        }

        return snapshot;
    }

    public static async Task<(string? RemoteVersion, VersionSnapshot Snapshot)> CheckForUpdateAsync(bool softCheck)
    {
        var snapshot = EnsureSnapshot();
        bool shouldCheck = true;

        if (softCheck && !string.IsNullOrWhiteSpace(snapshot.Time))
        {
            if (DateTime.TryParse(snapshot.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastCheck))
            {
                if (Math.Floor((DateTime.Now - lastCheck).TotalMinutes) <= 360)
                {
                    shouldCheck = false;
                }
            }
        }

        string? remote = null;
        if (shouldCheck)
        {
            remote = await FetchRemoteVersionAsync();
            SaveSnapshot(DateTime.Now, snapshot.Version, snapshot.Skip);
        }

        return (remote, snapshot);
    }

    public static bool IsUpdateAvailable(string? remoteVersion, VersionSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(remoteVersion))
        {
            return false;
        }

        var remoteNormalized = NormalizeVersion(remoteVersion);
        var installedNormalized = NormalizeVersion(snapshot.Version);
        var skipNormalized = NormalizeVersion(snapshot.Skip);

        if (!string.IsNullOrWhiteSpace(skipNormalized) &&
            string.Equals(remoteNormalized, skipNormalized, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Version.TryParse(remoteNormalized, out var remote) &&
            Version.TryParse(installedNormalized, out var installed))
        {
            return remote > installed;
        }

        return !string.Equals(remoteNormalized, installedNormalized, StringComparison.OrdinalIgnoreCase);
    }

    public static void RecordSkip(string skipVersion, VersionSnapshot snapshot)
    {
        SaveSnapshot(DateTime.Now, snapshot.Version, skipVersion);
    }

    public static void RecordInstalled(string version, VersionSnapshot snapshot)
    {
        SaveSnapshot(DateTime.Now, version, snapshot.Skip);
    }

    private static VersionSnapshot ReadSnapshot()
    {
        string? time = null;
        string? version = null;
        string? skip = null;

        foreach (var line in File.ReadAllLines(VersionFile))
        {
            if (line.StartsWith("time", StringComparison.OrdinalIgnoreCase))
            {
                time = line.Split(':', 2)[1].Trim();
            }
            else if (line.StartsWith("ver", StringComparison.OrdinalIgnoreCase))
            {
                version = line.Split(':', 2)[1].Trim();
            }
            else if (line.StartsWith("skip", StringComparison.OrdinalIgnoreCase))
            {
                skip = line.Split(':', 2)[1].Trim();
            }
        }

        return new VersionSnapshot(time ?? string.Empty, string.IsNullOrWhiteSpace(version) ? CurrentVersion : version!, skip ?? string.Empty);
    }

    private static async Task<string?> FetchRemoteVersionAsync()
    {
        try
        {
            var content = await Http.GetStringAsync(VersionUrl).ConfigureAwait(false);
            var firstLine = content.Split('\n')[0].Trim();
            return string.IsNullOrWhiteSpace(firstLine) ? null : firstLine;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeVersion(string version)
    {
        var normalized = (version ?? string.Empty).Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..].Trim();
        }

        return normalized;
    }

    private static void SaveSnapshot(DateTime time, string version, string skip)
    {
        var lines = new[]
        {
            $"time: {time:yyyy-MM-dd HH:mm:ss}",
            $"ver: {version}",
            $"skip: {skip}"
        };

        Directory.CreateDirectory(VersionDir);
        File.WriteAllLines(VersionFile, lines);
    }
}
