using System.Windows.Media;

namespace SpotWhy.Models;

public enum SearchResultType
{
    Application,
    File,
    Folder
}

public class SearchResult
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public SearchResultType Type { get; set; }
    public ImageSource? Icon { get; set; }
    public string DisplayPath
    {
        get
        {
            try { return System.IO.Path.GetDirectoryName(Path) ?? ""; }
            catch { return ""; }
        }
    }
}
