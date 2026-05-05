using SpotWhy.Models;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpotWhy.Services;

public class EverythingService : IDisposable
{
    private IntPtr _hModule = IntPtr.Zero;
    private bool _available;
    private string? _dllPath;

    // Delegates for Everything SDK functions
    private delegate void SetSearchWDelegate(string lpSearchString);
    private delegate void SetRequestFlagsDelegate(uint dwRequestFlags);
    private delegate void SetMaxResultsDelegate(uint dwMaxResults);
    private delegate bool QueryWDelegate(bool bWait);
    private delegate uint GetNumResultsDelegate();
    private delegate bool IsFolderDelegate(uint dwIndex);
    private delegate void GetResultFullPathNameWDelegate(uint dwIndex, IntPtr lpString, uint dwMaxCount);

    private SetSearchWDelegate? _setSearch;
    private SetRequestFlagsDelegate? _setRequestFlags;
    private SetMaxResultsDelegate? _setMaxResults;
    private QueryWDelegate? _query;
    private GetNumResultsDelegate? _getNumResults;
    private IsFolderDelegate? _isFolder;
    private GetResultFullPathNameWDelegate? _getFullPath;

    private const uint EVERYTHING_REQUEST_FILE_NAME = 0x00000004;
    private const uint EVERYTHING_REQUEST_PATH = 0x00000008;
    private const uint EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000010;
    private const uint EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;

    public bool IsAvailable => _available;

    public EverythingService()
    {
        Initialize();
    }

    private void Initialize()
    {
        _dllPath = FindEverythingDll();
        if (_dllPath == null) return;

        _hModule = LoadLibrary(_dllPath);
        if (_hModule == IntPtr.Zero) return;

        try
        {
            _setSearch = GetDelegate<SetSearchWDelegate>("Everything_SetSearchW");
            _setRequestFlags = GetDelegate<SetRequestFlagsDelegate>("Everything_SetRequestFlags");
            _setMaxResults = GetDelegate<SetMaxResultsDelegate>("Everything_SetMaxResults");
            _query = GetDelegate<QueryWDelegate>("Everything_QueryW");
            _getNumResults = GetDelegate<GetNumResultsDelegate>("Everything_GetNumResults");
            _isFolder = GetDelegate<IsFolderDelegate>("Everything_IsFolder");
            _getFullPath = GetDelegate<GetResultFullPathNameWDelegate>("Everything_GetResultFullPathNameW");

            // Quick test query to check if Everything is running
            _setSearch!("test");
            _setRequestFlags!(EVERYTHING_REQUEST_FILE_NAME);
            _setMaxResults!(1);
            _available = _query!(true);
        }
        catch
        {
            _available = false;
        }
    }

    private T GetDelegate<T>(string name) where T : Delegate
    {
        var ptr = GetProcAddress(_hModule, name);
        if (ptr == IntPtr.Zero)
            throw new EntryPointNotFoundException(name);
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    public List<SearchResult> Search(string query)
    {
        if (!_available || string.IsNullOrWhiteSpace(query))
            return new();

        var results = new List<SearchResult>();

        try
        {
            _setSearch!(query);
            _setRequestFlags!(EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME);
            _setMaxResults!(50);

            if (!_query!(true)) return results;

            var count = _getNumResults!();
            var sbPtr = Marshal.AllocHGlobal(520);

            for (uint i = 0; i < count && i < 50; i++)
            {
                _getFullPath!(i, sbPtr, 260);
                var path = Marshal.PtrToStringUni(sbPtr);
                if (string.IsNullOrEmpty(path)) continue;

                var isFolder = _isFolder!(i);
                var name = System.IO.Path.GetFileName(path);

                results.Add(new SearchResult
                {
                    Name = name,
                    Path = path,
                    Type = isFolder ? SearchResultType.Folder : SearchResultType.File,
                    Icon = GetIconForPath(path)
                });
            }

            Marshal.FreeHGlobal(sbPtr);
        }
        catch { }

        return results;
    }

    private static string? FindEverythingDll()
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", "Everything.dll"),
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", "Everything.dll"),
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything", "Everything.dll"),
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
                return path;
        }

        // Try PATH
        try
        {
            foreach (var dir in Environment.GetEnvironmentVariable("PATH")!.Split(';'))
            {
                var path = System.IO.Path.Combine(dir.Trim(), "Everything.dll");
                if (System.IO.File.Exists(path))
                    return path;
            }
        }
        catch { }

        return null;
    }

    private static ImageSource? GetIconForPath(string path)
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

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

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
        if (_available && _hModule != IntPtr.Zero)
        {
            FreeLibrary(_hModule);
            _hModule = IntPtr.Zero;
            _available = false;
        }
    }
}
