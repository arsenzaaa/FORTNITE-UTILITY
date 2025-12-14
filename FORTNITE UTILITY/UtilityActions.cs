using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace FortniteUtility;

internal static class UtilityActions
{
    public static string? ConsumeLastError()
    {
        var error = LastError;
        LastError = null;
        return error;
    }

    public static string? LastError { get; private set; }

    private static void ClearLastError() => LastError = null;

    private static void SetLastError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            LastError = message;
        }
    }

    private static void SetLastErrorIfEmpty(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(LastError))
        {
            LastError = message;
        }
    }

    private static string ExplainException(Exception ex)
    {
        var sb = new StringBuilder();
        sb.Append(ex.GetType().Name);
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            sb.Append(": ").Append(ex.Message);
        }

        if (ex.InnerException is Exception inner)
        {
            sb.AppendLine();
            sb.Append("Inner: ").Append(inner.GetType().Name);
            if (!string.IsNullOrWhiteSpace(inner.Message))
            {
                sb.Append(": ").Append(inner.Message);
            }
        }

        return sb.ToString();
    }

    private static string FormatPathError(string action, string path, Exception ex) =>
        $"{action}: {path}{Environment.NewLine}{ExplainException(ex)}";

    private static string FormatPathErrorWithLockers(string action, string path, Exception ex)
    {
        var baseError = FormatPathError(action, path, ex);
        var lockers = GetLockingProcesses(path)
            .Where(IsLikelyGameProcess)
            .Select(FormatLockingProcessSummary)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (lockers.Count == 0)
        {
            return baseError;
        }

        var sb = new StringBuilder();
        sb.AppendLine(baseError);
        sb.AppendLine("Locking processes:");
        foreach (var locker in lockers)
        {
            sb.Append("- ").AppendLine(locker);
        }
        return sb.ToString().TrimEnd();
    }

    private const int RestartManagerErrorMoreData = 234;
    private const int CchRmMaxAppName = 255;
    private const int CchRmMaxSvcName = 63;

    private const int MoveFileDelayUntilReboot = 0x00000004;

    private enum RmAppType
    {
        UnknownApp = 0,
        MainWindow = 1,
        OtherWindow = 2,
        Service = 3,
        Explorer = 4,
        Console = 5,
        Critical = 1000
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RmUniqueProcess
    {
        public int ProcessId;
        public FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RmProcessInfo
    {
        public RmUniqueProcess Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchRmMaxAppName + 1)]
        public string AppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchRmMaxSvcName + 1)]
        public string ServiceShortName;

        public RmAppType ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public bool Restartable;
    }

    private readonly struct LockingProcessInfo
    {
        public int ProcessId { get; }
        public string? ProcessName { get; }
        public string? ExecutablePath { get; }
        public string? AppName { get; }

        public LockingProcessInfo(int processId, string? processName, string? executablePath, string? appName)
        {
            ProcessId = processId;
            ProcessName = processName;
            ExecutablePath = executablePath;
            AppName = appName;
        }
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint sessionHandle, int sessionFlags, string sessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint sessionHandle,
        uint fileCount,
        string[] filePaths,
        uint applicationCount,
        RmUniqueProcess[]? applications,
        uint serviceCount,
        string[]? serviceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        uint sessionHandle,
        out uint processInfoNeeded,
        ref uint processInfoCount,
        [In, Out] RmProcessInfo[]? affectedApps,
        ref uint rebootReasons);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint sessionHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);

    private static readonly string[] GamePathMarkers =
    {
        "\\steamapps\\common\\",
        "\\epic games\\",
        "\\riot games\\",
        "\\gog galaxy\\games\\",
        "\\ubisoft game launcher\\games\\",
        "\\ea games\\",
        "\\origin games\\",
        "\\xboxgames\\"
    };

    private static readonly string[] GamePathMarkerExclusions =
    {
        "\\epic games\\launcher\\",
        "\\riot games\\riot client\\",
        "\\steam\\steam.exe",
        "\\battle.net\\"
    };

    private static bool IsLikelyGameProcess(LockingProcessInfo process)
    {
        if (!string.IsNullOrWhiteSpace(process.ProcessName))
        {
            var name = process.ProcessName;
            if (FortniteProcessNameSet.Contains(name))
            {
                return true;
            }

            if (name.Contains("win64-shipping", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var exePath = process.ExecutablePath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return false;
        }

        if (exePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var exclude in GamePathMarkerExclusions)
        {
            if (exePath.Contains(exclude, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        foreach (var marker in GamePathMarkers)
        {
            if (exePath.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatLockingProcessSummary(LockingProcessInfo process)
    {
        var display = !string.IsNullOrWhiteSpace(process.ProcessName)
            ? process.ProcessName + ".exe"
            : process.AppName;

        if (string.IsNullOrWhiteSpace(display))
        {
            display = "PID " + process.ProcessId;
        }

        return $"{display} (PID {process.ProcessId})";
    }

    private static bool TryScheduleDeleteAtReboot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return MoveFileEx(path, null, MoveFileDelayUntilReboot);
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<LockingProcessInfo> GetLockingProcesses(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Array.Empty<LockingProcessInfo>();
        }

        uint handle;
        var sessionKey = Guid.NewGuid().ToString("N");
        int result = RmStartSession(out handle, 0, sessionKey);
        if (result != 0)
        {
            return Array.Empty<LockingProcessInfo>();
        }

        try
        {
            var resources = new[] { path };
            result = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);
            if (result != 0)
            {
                return Array.Empty<LockingProcessInfo>();
            }

            uint processInfoNeeded = 0;
            uint processInfoCount = 0;
            uint rebootReasons = 0;

            result = RmGetList(handle, out processInfoNeeded, ref processInfoCount, null, ref rebootReasons);
            if (result == 0 || processInfoNeeded == 0)
            {
                return Array.Empty<LockingProcessInfo>();
            }

            if (result != RestartManagerErrorMoreData)
            {
                return Array.Empty<LockingProcessInfo>();
            }

            var processInfo = new RmProcessInfo[processInfoNeeded];
            processInfoCount = processInfoNeeded;

            result = RmGetList(handle, out processInfoNeeded, ref processInfoCount, processInfo, ref rebootReasons);
            if (result != 0 || processInfoCount == 0)
            {
                return Array.Empty<LockingProcessInfo>();
            }

            var seenPids = new HashSet<int>();
            var entries = new List<LockingProcessInfo>((int)processInfoCount);

            for (int i = 0; i < processInfoCount; i++)
            {
                int pid = processInfo[i].Process.ProcessId;
                if (!seenPids.Add(pid))
                {
                    continue;
                }

                string? processName = null;
                string? executablePath = null;
                {
                    try
                    {
                        var running = Process.GetProcessById(pid);
                        processName = running.ProcessName;
                        try
                        {
                            executablePath = running.MainModule?.FileName;
                        }
                        catch
                        {
                        }
                    }
                    catch
                    {
                    }
                }

                entries.Add(new LockingProcessInfo(pid, processName, executablePath, processInfo[i].AppName));
            }

            entries.Sort((a, b) =>
            {
                var nameCompare = StringComparer.OrdinalIgnoreCase.Compare(a.ProcessName ?? a.AppName ?? string.Empty, b.ProcessName ?? b.AppName ?? string.Empty);
                return nameCompare != 0 ? nameCompare : a.ProcessId.CompareTo(b.ProcessId);
            });

            return entries;
        }
        catch
        {
            return Array.Empty<LockingProcessInfo>();
        }
        finally
        {
            _ = RmEndSession(handle);
        }
    }

    internal enum ShaderBackupResult
    {
        Success,
        NotFound,
        Failed
    }

    private const string GusUrl = "https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/main/FortniteUtility/GameUserSettings.ini";
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(12);

    private static readonly string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string GusDirectory = Path.Combine(UserProfile, "AppData", "Local", "FortniteGame", "Saved", "Config", "WindowsClient");
    private static readonly string GusPath = Path.Combine(GusDirectory, "GameUserSettings.ini");
    private static readonly string GusDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "GameUserSettings.ini");
    private static readonly string BackupRoot = Path.Combine(AppContext.BaseDirectory, "ShadersBackup");

    public static string GameUserSettingsPath => GusPath;

    internal sealed class AdvancedSettingsPayload
    {
        public int LatencyMode { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int FullscreenMode { get; init; }
        public float FrameRateLimit { get; init; }
        public int ResolutionQuality { get; init; }
        public string DxRhi { get; init; } = "dx12";
        public string DxFeature { get; init; } = "es31";
    }

    internal sealed class SettingsSnapshot
    {
        public bool Exists { get; init; }
        public int Width { get; init; } = 1920;
        public int Height { get; init; } = 1080;
        public int FullscreenMode { get; init; } = 0;
        public float FrameRateLimit { get; init; } = 0f;
        public int ResolutionQuality { get; init; } = 100;
        public string DxRhi { get; init; } = "dx12";
        public string DxFeature { get; init; } = "es31";
        public int LatencyMode { get; init; } = 0;
    }

    private static readonly string[] FortniteProcessNames =
    {
        "FortniteLauncher",
        "FortniteClient-Win64-Shipping_EAC_EOS",
        "FortniteClient-Win64-Shipping",
        "CrashReportClient"
    };

    private static readonly HashSet<string> FortniteProcessNameSet = new(FortniteProcessNames, StringComparer.OrdinalIgnoreCase);

    private static bool EnsureFortniteNotRunning()
    {
        var running = new List<(string Name, int Pid)>();
        var seen = new HashSet<int>();

        foreach (var name in FortniteProcessNames)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    if (!seen.Add(process.Id))
                    {
                        continue;
                    }

                    running.Add((process.ProcessName, process.Id));
                }
            }
            catch
            {
            }
        }

        if (running.Count == 0)
        {
            return true;
        }

        running.Sort((a, b) =>
        {
            var nameCompare = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            return nameCompare != 0 ? nameCompare : a.Pid.CompareTo(b.Pid);
        });

        var sb = new StringBuilder();
        sb.AppendLine("Fortnite is currently running. Close the game and try again.");
        sb.AppendLine("Running processes:");
        foreach (var (name, pid) in running)
        {
            sb.Append("- ").Append(name).Append(".exe (PID ").Append(pid).AppendLine(")");
        }

        SetLastErrorIfEmpty(sb.ToString().TrimEnd());
        return false;
    }

    public static Task<bool> ClearCacheAsync()
    {
        ClearLastError();
        return Task.Run(() =>
        {
            var errors = new List<string>();
            if (!EnsureFortniteNotRunning())
            {
                return false;
            }
            RefreshGameUserSettingsLocation(errors);
            PurgeCacheDirectories(errors);

            if (errors.Count == 0)
            {
                return true;
            }

            SetLastError(string.Join(Environment.NewLine + Environment.NewLine, errors));
            return false;
        });
    }

    public static Task<ShaderBackupResult> BackupShadersAsync()
    {
        ClearLastError();
        return Task.Run(() =>
        {
            if (!EnsureFortniteNotRunning())
            {
                return ShaderBackupResult.Failed;
            }

            List<string> shaderDirs = new();
            foreach (var path in ShaderCachePaths())
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }

                if (!TryHasMeaningfulDirectoryContent(path, out var hasContent, out var error))
                {
                    SetLastErrorIfEmpty(error ?? $"Could not read directory: {path}");
                    return ShaderBackupResult.Failed;
                }

                if (!hasContent)
                {
                    continue;
                }

                shaderDirs.Add(path);
            }

            if (shaderDirs.Count == 0)
            {
                return ShaderBackupResult.NotFound;
            }

            var tempRoot = Path.Combine(AppContext.BaseDirectory, $"ShadersBackup_tmp_{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(tempRoot);
            }
            catch (Exception ex)
            {
                SetLastErrorIfEmpty(FormatPathError("Could not create backup folder", tempRoot, ex));
                return ShaderBackupResult.Failed;
            }

            try
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var filesCopied = 0;
                var copyErrors = new List<string>();

                foreach (var path in shaderDirs)
                {
                    var name = GetUniqueShaderDirectoryName(path, usedNames);

                    var target = Path.Combine(tempRoot, name);
                    filesCopied += CopyDirectoryBestEffort(path, target, copyErrors);
                }

                if (filesCopied <= 0)
                {
                    var details = copyErrors.Count > 0
                        ? string.Join(Environment.NewLine + Environment.NewLine, copyErrors)
                        : "No shader files copied.";
                    SetLastErrorIfEmpty(details);
                    throw new IOException("No shader files copied.");
                }

                if (Directory.Exists(BackupRoot))
                {
                    _ = TryDeleteDirectory(BackupRoot);
                    if (Directory.Exists(BackupRoot))
                    {
                        throw new IOException("Could not replace existing backup.");
                    }
                }

                Directory.Move(tempRoot, BackupRoot);
                return ShaderBackupResult.Success;
            }
            catch (Exception ex)
            {
                SetLastErrorIfEmpty(ExplainException(ex));
                _ = TryDeleteDirectory(tempRoot);
                return ShaderBackupResult.Failed;
            }
        });
    }

    private static int CopyDirectoryBestEffort(string sourceDir, string destDir, List<string> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var dirInfo = new DirectoryInfo(sourceDir);
        if (!dirInfo.Exists)
        {
            return 0;
        }

        try
        {
            Directory.CreateDirectory(destDir);
        }
        catch (Exception ex)
        {
            if (errors.Count < 12)
            {
                errors.Add(FormatPathError("Could not create folder", destDir, ex));
            }
            return 0;
        }

        int copied = 0;

        FileInfo[] files;
        try
        {
            files = dirInfo.GetFiles();
        }
        catch (Exception ex)
        {
            if (errors.Count < 12)
            {
                errors.Add(FormatPathError("Could not enumerate files", sourceDir, ex));
            }
            return 0;
        }

        foreach (var file in files)
        {
            var targetFilePath = Path.Combine(destDir, file.Name);
            try
            {
                file.CopyTo(targetFilePath, overwrite: true);
                copied++;
            }
            catch (Exception ex)
            {
                var gameLockers = GetLockingProcesses(file.FullName)
                    .Where(IsLikelyGameProcess)
                    .Select(FormatLockingProcessSummary)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (gameLockers.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Shader backup can't be created because a game is using shader cache files.");
                    sb.AppendLine("Locked file:");
                    sb.AppendLine(file.FullName);
                    sb.AppendLine();
                    sb.AppendLine("Blocking process(es):");
                    foreach (var locker in gameLockers)
                    {
                        sb.Append("- ").AppendLine(locker);
                    }
                    sb.AppendLine();
                    sb.AppendLine(ExplainException(ex));
                    SetLastErrorIfEmpty(sb.ToString().TrimEnd());
                    throw;
                }

                if (errors.Count < 12)
                {
                    errors.Add(FormatPathErrorWithLockers("Skipped file while backing up", file.FullName, ex));
                }
            }
        }

        DirectoryInfo[] subDirs;
        try
        {
            subDirs = dirInfo.GetDirectories();
        }
        catch (Exception ex)
        {
            if (errors.Count < 12)
            {
                errors.Add(FormatPathError("Could not enumerate folders", sourceDir, ex));
            }
            return copied;
        }

        foreach (var subDir in subDirs)
        {
            copied += CopyDirectoryBestEffort(subDir.FullName, Path.Combine(destDir, subDir.Name), errors);
        }

        return copied;
    }

    private static bool TryHasMeaningfulDirectoryContent(string path, out bool hasContent, out string? error)
    {
        hasContent = false;
        error = null;
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(entry);
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase)) continue;

                hasContent = true;
                return true;
            }

            hasContent = false;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            hasContent = false;
            error = FormatPathError("Could not enumerate directory", path, ex);
            return false;
        }
    }

    public static Task<bool> RestoreShadersAsync()
    {
        ClearLastError();
        return Task.Run(() =>
        {
            try
            {
                if (!EnsureFortniteNotRunning())
                {
                    return false;
                }
                if (!Directory.Exists(BackupRoot))
                {
                    SetLastErrorIfEmpty($"Backup not found: {BackupRoot}");
                    return false;
                }

                var destinationMap = BuildShaderBackupNameToDestinationMap();
                int restored = 0;

                foreach (var backupDir in Directory.EnumerateDirectories(BackupRoot, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(backupDir);
                    if (!destinationMap.TryGetValue(name, out var destination))
                    {
                        continue;
                    }

                    _ = TryDeleteDirectory(destination);
                    try
                    {
                        CopyDirectory(backupDir, destination);
                    }
                    catch (Exception ex)
                    {
                        SetLastErrorIfEmpty(FormatPathError("Failed to restore directory", destination, ex));
                        return false;
                    }

                    restored++;
                }

                if (restored <= 0)
                {
                    SetLastErrorIfEmpty("No shader/cache folders were restored. Backup may be invalid.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SetLastErrorIfEmpty(ExplainException(ex));
                return false;
            }
        });
    }

    public static bool HasShaderBackup()
    {
        try
        {
            return Directory.Exists(BackupRoot) &&
                   Directory.EnumerateDirectories(BackupRoot, "*", SearchOption.TopDirectoryOnly).Any();
        }
        catch
        {
            return false;
        }
    }

    public static SettingsSnapshot GetSettingsSnapshot()
    {
        var snapshot = new SettingsSnapshot { Exists = File.Exists(GusPath) };
        if (!snapshot.Exists)
        {
            return snapshot;
        }

        int width = 1920;
        int height = 1080;
        int fullscreen = 0;
        float frameLimit = 0f;
        int resQuality = 100;
        string rhi = "dx12";
        string feature = "es31";
        int latencyMode = 0;

        try
        {
            var lines = File.ReadAllLines(GusPath);
            string currentSection = string.Empty;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Trim('[', ']');
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (string.Equals(currentSection, "/Script/FortniteGame.FortGameUserSettings", StringComparison.OrdinalIgnoreCase))
                {
                    switch (key)
                    {
                        case "ResolutionSizeX":
                            _ = int.TryParse(value, out width);
                            break;
                        case "ResolutionSizeY":
                            _ = int.TryParse(value, out height);
                            break;
                        case "FullscreenMode":
                            _ = int.TryParse(value, out fullscreen);
                            break;
                        case "FrameRateLimit":
                            _ = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out frameLimit);
                            break;
                        case "LatencyTweak2":
                            _ = int.TryParse(value, out latencyMode);
                            break;
                    }
                }
                else if (string.Equals(currentSection, "ScalabilityGroups", StringComparison.OrdinalIgnoreCase))
                {
                    if (key == "sg.ResolutionQuality")
                    {
                        _ = int.TryParse(value.Split('.')[0], out resQuality);
                    }
                }
                else if (string.Equals(currentSection, "D3DRHIPreference", StringComparison.OrdinalIgnoreCase))
                {
                    if (key == "PreferredRHI") rhi = value;
                    if (key == "PreferredFeatureLevel") feature = value;
                }
            }
        }
        catch
        {
            // ignore parse failures, use defaults
        }

        return new SettingsSnapshot
        {
            Exists = true,
            Width = width,
            Height = height,
            FullscreenMode = fullscreen,
            FrameRateLimit = frameLimit,
            ResolutionQuality = resQuality,
            DxRhi = rhi,
            DxFeature = feature,
            LatencyMode = latencyMode
        };
    }

    public static async Task<bool> DownloadGameUserSettingsAsync()
    {
        ClearLastError();

        var tempFile = Path.Combine(Path.GetTempPath(), $"GameUserSettings_{Guid.NewGuid():N}.ini");

        try
        {
            Directory.CreateDirectory(GusDirectory);
            using var http = new HttpClient { Timeout = DownloadTimeout };
            var bytes = await http.GetByteArrayAsync(GusUrl).ConfigureAwait(false);
            await File.WriteAllBytesAsync(tempFile, bytes).ConfigureAwait(false);
            Directory.CreateDirectory(GusDirectory);
            File.Copy(tempFile, GusPath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            SetLastErrorIfEmpty($"URL: {GusUrl}{Environment.NewLine}{ExplainException(ex)}");
            return false;
        }
        finally
        {
            _ = TryDeleteFile(tempFile);
        }
    }

    public static async Task<bool> WriteGusValuesAsync(string rhi, string feature, bool requireBaseConfig)
    {
        ClearLastError();
        Directory.CreateDirectory(GusDirectory);

        if (requireBaseConfig && !File.Exists(GusPath))
        {
            var downloaded = await DownloadGameUserSettingsAsync().ConfigureAwait(false);
            if (!downloaded)
            {
                return false;
            }
        }

        var block = new[]
        {
            "[D3DRHIPreference]",
            $"PreferredRHI={rhi}",
            $"PreferredFeatureLevel={feature}"
        };

        try
        {
            if (File.Exists(GusPath))
            {
                var normalized = NormalizeNewLines(File.ReadAllText(GusPath));
                var lines = new List<string>(normalized.Split('\n'));
                int insertIndex = lines.Count;
                int start = lines.FindIndex(l => l.Trim().Equals("[D3DRHIPreference]", StringComparison.OrdinalIgnoreCase));
                if (start >= 0)
                {
                    int end = start + 1;
                    while (end < lines.Count && !lines[end].TrimStart().StartsWith("[", StringComparison.Ordinal))
                    {
                        end++;
                    }

                    lines.RemoveRange(start, end - start);
                    insertIndex = start;
                }

                while (insertIndex > 0 && string.IsNullOrWhiteSpace(lines[insertIndex - 1]))
                {
                    lines.RemoveAt(insertIndex - 1);
                    insertIndex--;
                }

                if (insertIndex > 0)
                {
                    lines.Insert(insertIndex, string.Empty);
                    insertIndex++;
                }

                foreach (var line in block)
                {
                    lines.Insert(insertIndex++, line);
                }

                while (insertIndex < lines.Count && string.IsNullOrWhiteSpace(lines[insertIndex]))
                {
                    lines.RemoveAt(insertIndex);
                }

                if (insertIndex < lines.Count)
                {
                    lines.Insert(insertIndex, string.Empty);
                }
                else
                {
                    lines.Add(string.Empty);
                }

                var finalText = string.Join("\r\n", lines);
                if (!finalText.EndsWith("\r\n", StringComparison.Ordinal))
                {
                    finalText += "\r\n";
                }

                File.WriteAllText(GusPath, finalText, Encoding.UTF8);
            }
            else
            {
                var body = string.Join("\r\n", block) + "\r\n\r\n";
                File.WriteAllText(GusPath, body, Encoding.UTF8);
            }

            return true;
        }
        catch (Exception ex)
        {
            SetLastErrorIfEmpty(FormatPathError("Could not modify GameUserSettings.ini", GusPath, ex));
            return false;
        }
    }

    public static async Task<bool> ApplyAdvancedSettingsAsync(AdvancedSettingsPayload payload)
    {
        ClearLastError();
        if (!await EnsureConfigExistsAsync().ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            var normalized = NormalizeNewLines(File.ReadAllText(GusPath));
            var lines = new List<string>(normalized.Split('\n'));

            SetSectionValues(lines, "/Script/FortniteGame.FortGameUserSettings", new Dictionary<string, string>
            {
                ["bLatencyTweak1"] = "False",
                ["LatencyTweak2"] = payload.LatencyMode.ToString(CultureInfo.InvariantCulture),
                ["bLatencyFlash"] = "False",
                ["ResolutionSizeX"] = payload.Width.ToString(CultureInfo.InvariantCulture),
                ["ResolutionSizeY"] = payload.Height.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedResolutionSizeX"] = payload.Width.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedResolutionSizeY"] = payload.Height.ToString(CultureInfo.InvariantCulture),
                ["DesiredScreenWidth"] = payload.Width.ToString(CultureInfo.InvariantCulture),
                ["DesiredScreenHeight"] = payload.Height.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedDesiredScreenWidth"] = payload.Width.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedDesiredScreenHeight"] = payload.Height.ToString(CultureInfo.InvariantCulture),
                ["FullscreenMode"] = payload.FullscreenMode.ToString(CultureInfo.InvariantCulture),
                ["FrameRateLimit"] = payload.FrameRateLimit.ToString("0.##", CultureInfo.InvariantCulture)
            });

            SetSectionValues(lines, "ScalabilityGroups", new Dictionary<string, string>
            {
                ["sg.ResolutionQuality"] = payload.ResolutionQuality.ToString(CultureInfo.InvariantCulture)
            });

            WriteLines(GusPath, lines);

            return await WriteGusValuesAsync(payload.DxRhi, payload.DxFeature, requireBaseConfig: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetLastErrorIfEmpty(FormatPathError("Could not modify GameUserSettings.ini", GusPath, ex));
            return false;
        }
    }

    public static async Task<bool> ApplyFrameRateLimitAsync(float frameRateLimit)
    {
        ClearLastError();
        if (!await EnsureConfigExistsAsync().ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            var normalized = NormalizeNewLines(File.ReadAllText(GusPath));
            var lines = new List<string>(normalized.Split('\n'));

            SetSectionValues(lines, "/Script/FortniteGame.FortGameUserSettings", new Dictionary<string, string>
            {
                ["FrameRateLimit"] = frameRateLimit.ToString("0.##", CultureInfo.InvariantCulture)
            });

            WriteLines(GusPath, lines);
            return true;
        }
        catch (Exception ex)
        {
            SetLastErrorIfEmpty(FormatPathError("Could not modify GameUserSettings.ini", GusPath, ex));
            return false;
        }
    }

    public static async Task<bool> ApplyResolutionAsync(int width, int height)
    {
        ClearLastError();
        if (!await EnsureConfigExistsAsync().ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            var normalized = NormalizeNewLines(File.ReadAllText(GusPath));
            var lines = new List<string>(normalized.Split('\n'));

            SetSectionValues(lines, "/Script/FortniteGame.FortGameUserSettings", new Dictionary<string, string>
            {
                ["ResolutionSizeX"] = width.ToString(CultureInfo.InvariantCulture),
                ["ResolutionSizeY"] = height.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedResolutionSizeX"] = width.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedResolutionSizeY"] = height.ToString(CultureInfo.InvariantCulture),
                ["DesiredScreenWidth"] = width.ToString(CultureInfo.InvariantCulture),
                ["DesiredScreenHeight"] = height.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedDesiredScreenWidth"] = width.ToString(CultureInfo.InvariantCulture),
                ["LastUserConfirmedDesiredScreenHeight"] = height.ToString(CultureInfo.InvariantCulture)
            });

            WriteLines(GusPath, lines);
            return true;
        }
        catch (Exception ex)
        {
            SetLastErrorIfEmpty(FormatPathError("Could not modify GameUserSettings.ini", GusPath, ex));
            return false;
        }
    }

    public static int GetPrimaryMonitorRefreshRateHz()
    {
        try
        {
            var mode = new DevMode();
            mode.dmSize = (ushort)Marshal.SizeOf(typeof(DevMode));

            if (EnumDisplaySettings(null, EnumCurrentSettings, ref mode) && mode.dmDisplayFrequency > 1)
            {
                int hz = (int)mode.dmDisplayFrequency;
                if (hz == 59) hz = 60;
                return hz;
            }
        }
        catch
        {
            // ignore
        }

        return 0;
    }

    public static (int Width, int Height) GetPrimaryMonitorResolution()
    {
        try
        {
            var mode = new DevMode();
            mode.dmSize = (ushort)Marshal.SizeOf(typeof(DevMode));

            if (EnumDisplaySettings(null, EnumCurrentSettings, ref mode) &&
                mode.dmPelsWidth > 0 &&
                mode.dmPelsHeight > 0)
            {
                return ((int)mode.dmPelsWidth, (int)mode.dmPelsHeight);
            }
        }
        catch
        {
            // ignore
        }

        return (0, 0);
    }

    private const int EnumCurrentSettings = -1;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DevMode devMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        private const int CchDeviceName = 32;
        private const int CchFormName = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceName)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchFormName)]
        public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    private static void RefreshGameUserSettingsLocation(List<string>? errors = null)
    {
        try
        {
            Directory.CreateDirectory(GusDirectory);
        }
        catch (Exception ex)
        {
            errors?.Add(FormatPathError("Could not ensure Fortnite config folder exists", GusDirectory, ex));
            return;
        }

        try
        {
            if (!File.Exists(GusPath) && File.Exists(GusDesktop))
            {
                File.Copy(GusDesktop, GusPath, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            errors?.Add(FormatPathError("Could not copy GameUserSettings.ini into Fortnite config folder", GusPath, ex));
        }
    }

    private static void PurgeCacheDirectories(List<string>? errors = null)
    {
        var pathsToDelete = new[]
        {
            Path.Combine(UserProfile, "AppData", "Local", "D3DSCache"),
            Path.Combine(UserProfile, "AppData", "Local", "NVIDIA", "DXCache"),
            Path.Combine(UserProfile, "AppData", "LocalLow", "NVIDIA", "PerDriverVersion", "DXCache")
        };

        foreach (var path in pathsToDelete)
        {
            _ = TryDeleteDirectoryBestEffort(path, errors);
        }

        var directories = new[]
        {
            Path.Combine(UserProfile, "AppData", "Local", "CrashReportClient"),
            Path.Combine(UserProfile, "AppData", "Local", "EpicOnlineServicesUIHelper"),
            Path.Combine(UserProfile, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "CrashReportClient"),
            Path.Combine(UserProfile, "AppData", "Local", "EpicGamesLauncher", "Saved", "Logs")
        };

        foreach (var dir in directories)
        {
            _ = TryDeleteDirectoryBestEffort(dir, errors);
        }

        var savedRoot = Path.Combine(UserProfile, "AppData", "Local", "EpicGamesLauncher", "Saved");
        if (Directory.Exists(savedRoot))
        {
            try
            {
                foreach (var subDir in Directory.EnumerateDirectories(savedRoot, "webcache*", SearchOption.TopDirectoryOnly))
                {
                    _ = TryDeleteDirectoryBestEffort(subDir, errors);
                }
            }
            catch (Exception ex)
            {
                errors?.Add(FormatPathError("Could not enumerate Epic Games Launcher caches", savedRoot, ex));
            }
        }

        var eacRoot = Path.Combine(UserProfile, "AppData", "Roaming", "EasyAntiCheat");
        if (Directory.Exists(eacRoot))
        {
            try
            {
                int errorLimit = 10;
                int errorCount = 0;

                foreach (var log in Directory.EnumerateFiles(eacRoot, "*.log", SearchOption.AllDirectories))
                {
                    if (TryDeleteFile(log, errors))
                    {
                        continue;
                    }

                    errorCount++;
                    if (errors != null && errorCount >= errorLimit)
                    {
                        errors.Add("Too many errors while deleting EasyAntiCheat logs. Stopping.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                errors?.Add(FormatPathError("Could not enumerate EasyAntiCheat logs", eacRoot, ex));
            }
        }
    }

    private static IEnumerable<string> ShaderCachePaths()
    {
        yield return Path.Combine(UserProfile, "AppData", "Local", "D3DSCache");
        yield return Path.Combine(UserProfile, "AppData", "Local", "NVIDIA", "DXCache");
        yield return Path.Combine(UserProfile, "AppData", "LocalLow", "NVIDIA", "PerDriverVersion", "DXCache");
    }

    private static string GetUniqueShaderDirectoryName(string path, HashSet<string> usedNames)
    {
        var baseName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var name = baseName;

        if (usedNames.Add(name))
        {
            return name;
        }

        var parent = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
        name = string.IsNullOrWhiteSpace(parent) ? $"{baseName}_2" : $"{parent}_{baseName}";
        int i = 2;
        while (!usedNames.Add(name))
        {
            name = string.IsNullOrWhiteSpace(parent) ? $"{baseName}_{i++}" : $"{parent}_{baseName}_{i++}";
        }

        return name;
    }

    private static Dictionary<string, string> BuildShaderBackupNameToDestinationMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in ShaderCachePaths())
        {
            var name = GetUniqueShaderDirectoryName(path, usedNames);
            map[name] = path;
        }

        return map;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        var dirInfo = new DirectoryInfo(sourceDir);
        if (!dirInfo.Exists) return;

        Directory.CreateDirectory(destDir);

        foreach (var file in dirInfo.GetFiles())
        {
            var targetFilePath = Path.Combine(destDir, file.Name);
            try
            {
                file.CopyTo(targetFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                SetLastErrorIfEmpty(FormatPathErrorWithLockers("Failed to copy file", file.FullName, ex));
                throw;
            }
        }

        foreach (var subDir in dirInfo.GetDirectories())
        {
            var targetSubDir = Path.Combine(destDir, subDir.Name);
            CopyDirectory(subDir.FullName, targetSubDir);
        }
    }

    private static async Task<bool> EnsureConfigExistsAsync()
    {
        if (File.Exists(GusPath))
        {
            return true;
        }

        return await DownloadGameUserSettingsAsync().ConfigureAwait(false);
    }

    private static int FindSectionStart(IList<string> lines, string section)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Equals($"[{section}]", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private static void SetSectionValues(List<string> lines, string section, IDictionary<string, string> values)
    {
        int start = FindSectionStart(lines, section);
        int insertAt;

        if (start == -1)
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.Add(string.Empty);
            }
            lines.Add($"[{section}]");
            start = lines.Count - 1;
            insertAt = start + 1;
        }
        else
        {
            insertAt = start + 1;
            int end = lines.Count;
            for (int i = start + 1; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("[", StringComparison.Ordinal))
                {
                    end = i;
                    break;
                }
            }

            for (int i = end - 1; i >= insertAt; i--)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                foreach (var key in values.Keys)
                {
                    if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        foreach (var kvp in values)
        {
            lines.Insert(insertAt++, $"{kvp.Key}={kvp.Value}");
        }
    }

    private static void WriteLines(string path, List<string> lines)
    {
        var text = string.Join("\r\n", lines);
        if (!text.EndsWith("\r\n", StringComparison.Ordinal))
        {
            text += "\r\n";
        }
        File.WriteAllText(path, text, Encoding.UTF8);
    }

    private static string NormalizeNewLines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    private static bool TryDeleteDirectoryBestEffort(string path, List<string>? errors = null)
    {
        if (!Directory.Exists(path))
        {
            return true;
        }

        try
        {
            Directory.Delete(path, recursive: true);
            return true;
        }
        catch
        {
        }

        var failed = false;
        var limitedErrors = errors ?? new List<string>();

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    if (TryScheduleDeleteAtReboot(file))
                    {
                        continue;
                    }

                    failed = true;
                    if (limitedErrors.Count < 12)
                    {
                        limitedErrors.Add(FormatPathErrorWithLockers("Could not delete file", file, ex));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            failed = true;
            if (limitedErrors.Count < 12)
            {
                limitedErrors.Add(FormatPathError("Could not enumerate folder for deletion", path, ex));
            }
        }

        try
        {
            var subDirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToArray();

            foreach (var dir in subDirs)
            {
                try
                {
                    Directory.Delete(dir, recursive: false);
                }
                catch
                {
                    _ = TryScheduleDeleteAtReboot(dir);
                }
            }
        }
        catch (Exception ex)
        {
            failed = true;
            if (limitedErrors.Count < 12)
            {
                limitedErrors.Add(FormatPathError("Could not enumerate folders for deletion", path, ex));
            }
        }

        try
        {
            Directory.Delete(path, recursive: false);
            return !failed;
        }
        catch (Exception ex)
        {
            if (TryScheduleDeleteAtReboot(path))
            {
                return !failed;
            }

            if (limitedErrors.Count < 12)
            {
                limitedErrors.Add(FormatPathError("Could not delete folder", path, ex));
            }

            return false;
        }
        finally
        {
            if (errors == null && limitedErrors.Count > 0)
            {
                SetLastErrorIfEmpty(string.Join(Environment.NewLine + Environment.NewLine, limitedErrors));
            }
        }
    }

    private static bool TryDeleteDirectory(string path, List<string>? errors = null)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            errors?.Add(FormatPathError("Could not delete folder", path, ex));
            SetLastErrorIfEmpty(FormatPathError("Could not delete folder", path, ex));
            return false;
        }
    }

    private static bool TryDeleteFile(string path, List<string>? errors = null)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
        catch (Exception ex)
        {
            var formatted = FormatPathErrorWithLockers("Could not delete file", path, ex);
            errors?.Add(formatted);
            SetLastErrorIfEmpty(formatted);
            return false;
        }
    }
}
