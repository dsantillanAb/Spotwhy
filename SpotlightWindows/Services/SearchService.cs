using SpotWhy.Models;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace SpotWhy.Services;

public class SearchService : IDisposable
{
    private List<SearchResult> _appCache = new();
    private CancellationTokenSource? _currentCts;
    private readonly EverythingService? _everything;
    private readonly UsageTracker _usage = new();

    public bool UsingEverything => _everything?.IsAvailable ?? false;

    public SearchService()
    {
        BuildAppCache();
        _everything = new EverythingService();
    }

    private void BuildAppCache()
    {
        var apps = new List<SearchResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Start Menu .lnk files
        var startMenuPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
        };

        foreach (var path in startMenuPaths)
        {
            if (!Directory.Exists(path)) continue;
            try
            {
                foreach (var shortcut in Directory.EnumerateFiles(path, "*.lnk", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(shortcut);
                    if (seen.Add(name))
                    {
                        apps.Add(new SearchResult
                        {
                            Name = name,
                            Path = shortcut,
                            Type = SearchResultType.Application,
                            Icon = GetIconForPath(shortcut)
                        });
                    }
                }
            }
            catch { }
        }

        // 2. Registry App Paths (traditional Win32 apps)
        try
        {
            using var appPaths = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
            if (appPaths != null)
            {
                foreach (var keyName in appPaths.GetSubKeyNames())
                {
                    var name = Path.GetFileNameWithoutExtension(keyName);
                    if (string.IsNullOrEmpty(name) || !seen.Add(name)) continue;

                    using var key = appPaths.OpenSubKey(keyName);
                    var exePath = key?.GetValue("") as string;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        apps.Add(new SearchResult
                        {
                            Name = name,
                            Path = exePath,
                            Type = SearchResultType.Application,
                            Icon = GetIconForPath(exePath)
                        });
                    }
                }
            }
        }
        catch { }

        // 3. AppX packages (UWP/Store apps like Calculator)
        try
        {
            var packagesKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
            if (packagesKey != null)
            {
                foreach (var packageFullName in packagesKey.GetSubKeyNames())
                {
                    try
                    {
                        using var pkgKey = packagesKey.OpenSubKey(packageFullName);
                        if (pkgKey == null) continue;

                        var installPath = pkgKey.GetValue("PackageRootFolder") as string;
                        if (string.IsNullOrEmpty(installPath)) continue;

                        var appIds = new List<string>();
                        foreach (var subKey in pkgKey.GetSubKeyNames())
                        {
                            // Skip well-known metadata keys; treat unknown keys as app IDs
                            if (subKey is "Capabilities" or "FileAssociations" or "URLAssociations" or "AppData" or "AppXManifest") 
                                continue;
                            appIds.Add(subKey);
                        }
                        if (appIds.Count == 0) continue;

                        var displayName = ResolveAppxDisplayName(installPath, packageFullName);
                        if (string.IsNullOrEmpty(displayName) || !seen.Add(displayName)) continue;

                        var familyName = GetPackageFamilyName(packageFullName);
                        foreach (var appId in appIds)
                        {
                            var aumid = $"{familyName}!{appId}";
                            apps.Add(new SearchResult
                            {
                                Name = displayName,
                                Path = aumid,
                                Type = SearchResultType.Application,
                                Icon = GetAppxIcon(pkgKey, packageFullName)
                            });
                            break;
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        _appCache = apps;
    }

    /// <summary>
    /// Extracts the PackageFamilyName from a PackageFullName.
    /// PackageFullName format: &lt;Name&gt;_&lt;Version&gt;_&lt;Arch&gt;_&lt;PublisherId&gt;
    /// PackageFamilyName format: &lt;Name&gt;_&lt;PublisherId&gt;
    /// </summary>
    private static string GetPackageFamilyName(string packageFullName)
    {
        var segments = packageFullName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4) return packageFullName;

        // Find version segment (has 3 dots, like 11.2508.4.0)
        int versionIdx = -1;
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].Count(c => c == '.') == 3)
            {
                versionIdx = i;
                break;
            }
        }
        if (versionIdx < 0) return packageFullName;

        var name = string.Join("_", segments.Take(versionIdx));
        var publisherId = segments[^1];
        return $"{name}_{publisherId}";
    }

    private static string? ResolveAppxDisplayName(string installPath, string packageFullName)
    {
        // Try PackageManager (resolves ms-resource: to localized names)
        try
        {
            var pkgManager = new PackageManager();
            var pkg = pkgManager.FindPackages().FirstOrDefault(p =>
                p.Id.FullName.Equals(packageFullName, StringComparison.OrdinalIgnoreCase));
            if (pkg != null)
            {
                var dn = pkg.DisplayName;
                if (!string.IsNullOrEmpty(dn) && !dn.StartsWith("ms-resource:"))
                    return dn;
            }
        }
        catch { }

        // Try parsing AppxManifest.xml
        try
        {
            var manifestPath = System.IO.Path.Combine(installPath, "AppxManifest.xml");
            if (System.IO.File.Exists(manifestPath))
            {
                var doc = XDocument.Load(manifestPath);
                var root = doc.Root;
                if (root == null) return null;

                var ns = root.GetDefaultNamespace();

                var displayName = root
                    .Element(ns + "Properties")?
                    .Element(ns + "DisplayName")?.Value;
                if (!string.IsNullOrEmpty(displayName) && !displayName.StartsWith("ms-resource:"))
                    return displayName;

                foreach (var ve in root.Descendants())
                {
                    if (ve.Name.LocalName == "VisualElements")
                    {
                        var veName = ve.Attribute("DisplayName")?.Value;
                        if (!string.IsNullOrEmpty(veName) && !veName.StartsWith("ms-resource:"))
                            return veName;
                    }
                }
            }
        }
        catch { }

        // Fallback: clean up package name
        var name = packageFullName;
        var idx = name.IndexOf('_');
        if (idx > 0) name = name[..idx];
        idx = name.LastIndexOf('.');
        if (idx > 0 && idx < name.Length - 1)
            name = name[(idx + 1)..];
        return name;
    }

    private static ImageSource? GetAppxIcon(RegistryKey pkgKey, string packageId)
    {
        try
        {
            // AppX icons are stored in the package install directory
            var installLocation = pkgKey.GetValue("PackageInstallLocation") as string;
            if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                return null;

            // Look for common icon paths in the package
            var iconCandidates = new[]
            {
                @"Assets\AppList.scale-100.png",
                @"Assets\AppList.png",
                @"Assets\Square44x44Logo.png",
                @"Assets\Square44x44Logo.scale-100.png",
                @"Assets\StoreLogo.scale-100.png",
                @"Assets\StoreLogo.png",
            };

            foreach (var relPath in iconCandidates)
            {
                var iconPath = Path.Combine(installLocation, relPath);
                if (File.Exists(iconPath))
                    return GetIconForPath(iconPath);
            }
        }
        catch { }
        return null;
    }

public List<SearchResult> Search(string query)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        var frequent = GetFrequentApps();
        return frequent;
    }

    _currentCts?.Cancel();
    _currentCts = new CancellationTokenSource();
    var token = _currentCts.Token;

    var lowerQuery = query.ToLowerInvariant();
    var results = new ConcurrentBag<SearchResult>();

    // Apps from cache
    SearchApps(lowerQuery, results);

    // Files & folders via Everything or fallback
    if (_everything?.IsAvailable == true)
    {
        var everythingResults = _everything.Search(query);
        foreach (var r in everythingResults)
            results.Add(r);
    }
    else
    {
        var sw = Stopwatch.StartNew();
        var folders = GetSearchFolders();

        Parallel.ForEach(folders, folder =>
        {
            if (token.IsCancellationRequested) return;
            if (!Directory.Exists(folder)) return;

            try
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(folder, "*" + query + "*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = 2,
                    ReturnSpecialDirectories = false
                }))
                {
                    if (token.IsCancellationRequested || sw.ElapsedMilliseconds > 1500) return;

                    var name = Path.GetFileName(entry);
                    var isDir = (File.GetAttributes(entry) & FileAttributes.Directory) == FileAttributes.Directory;

                    results.Add(new SearchResult
                    {
                        Name = name,
                        Path = entry,
                        Type = isDir ? SearchResultType.Folder : SearchResultType.File,
                        Icon = GetIconForPath(entry),
                        Query = query
                    });
                }
            }
            catch { }
        });
    }

    return results
        .Select(r => { r.Query = query; return r; })
        .OrderByDescending(r => r.Type == SearchResultType.Application ? 1 : 0)
        .ThenByDescending(r => _usage.GetUsage(r.Path))
        .ThenBy(r => r.Name)
        .Take(24)
        .ToList();
}

private List<SearchResult> GetFrequentApps()
{
    var top = _usage.GetTopUsed(12);
    var results = new List<SearchResult>();

    foreach (var (key, count) in top)
    {
        var app = _appCache.FirstOrDefault(a =>
            a.Path.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (app != null)
        {
            var appCopy = new SearchResult
            {
                Name = app.Name,
                Path = app.Path,
                Type = app.Type,
                Icon = app.Icon,
                Query = ""
            };
            results.Add(appCopy);
        }
    }

    // Fill with remaining apps alphabetically if not enough
    if (results.Count < 8)
    {
        var existing = new HashSet<string>(results.Select(r => r.Path), StringComparer.OrdinalIgnoreCase);
        foreach (var app in _appCache.OrderBy(a => a.Name))
        {
            if (!existing.Contains(app.Path))
            {
                var appCopy = new SearchResult
                {
                    Name = app.Name,
                    Path = app.Path,
                    Type = app.Type,
                    Icon = app.Icon,
                    Query = ""
                };
                results.Add(appCopy);
                existing.Add(app.Path);
                if (results.Count >= 12) break;
            }
        }
    }

    return results.Take(12).ToList();
}

    public void TrackOpen(SearchResult result)
    {
        _usage.TrackOpen(result.Path);
    }

private void SearchApps(string query, ConcurrentBag<SearchResult> results)
{
    var lowerQuery = query.ToLowerInvariant().Replace(" ", "");

    foreach (var app in _appCache)
    {
        if (MatchesApp(app.Name, lowerQuery))
        {
            results.Add(new SearchResult
            {
                Name = app.Name,
                Path = app.Path,
                Type = app.Type,
                Icon = app.Icon,
                Query = query
            });
        }
    }
}

    private static bool MatchesApp(string appName, string lowerQuery)
    {
        var lowerName = appName.ToLowerInvariant();

        // 1. Direct substring match (current behavior)
        if (lowerName.Contains(lowerQuery)) return true;

        // 2. No-spaces comparison (e.g., "visualstudiocode")
        var noSpace = string.Concat(lowerName.Where(c => !char.IsWhiteSpace(c)));
        if (noSpace.Contains(lowerQuery)) return true;

        var words = lowerName.Split([' ', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return false;

        // 3. Any word starts with the query
        if (words.Any(w => w.StartsWith(lowerQuery))) return true;

        // 4. Any word contains the query
        if (words.Any(w => w.Contains(lowerQuery))) return true;

        // 5. Acronym: first letter of each word (e.g., "vsc" from "visual studio code")
        if (words.Length >= 2)
        {
            var acronym = string.Concat(words.Select(w => w[0]));
            if (acronym.Length >= 2 && (acronym.Contains(lowerQuery) || lowerQuery.Contains(acronym)))
                return true;

            // 6. Extended acronym: first letters + last word (e.g., "vscode" from VS + Code)
            if (words.Length >= 2)
            {
                var extended = string.Concat(words.Take(words.Length - 1).Select(w => w[0].ToString())) + words[^1];
                var lowerExtended = extended.ToLowerInvariant();
                if (lowerExtended.Contains(lowerQuery) || lowerQuery.Contains(lowerExtended))
                    return true;
            }
        }

        return false;
    }

    private static string[] GetSearchFolders()
    {
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return
        [
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            user + @"\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu",
            user + @"\AppData\Local\Microsoft\WindowsApps",
        ];
    }

    public static ImageSource? GetIconForPath(string path)
    {
        try
        {
            var shfi = new SHFILEINFO();
            var ret = SHGetFileInfo(path, 0, out shfi, Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_SMALLICON);
            if (ret != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                var icon = Imaging.CreateBitmapSourceFromHIcon(
                    shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                icon.Freeze();
                DestroyIcon(shfi.hIcon);
                return icon;
            }
        }
        catch { }
        return null;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, int cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;

    public void Dispose()
    {
        _everything?.Dispose();
        _currentCts?.Cancel();
        _currentCts?.Dispose();
    }
}
