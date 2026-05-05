using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SpotWhy.Models;

public enum SearchResultType
{
    Application,
    File,
    Folder
}

public class SearchResult : INotifyPropertyChanged
{
    private string _name = "";
    private string _query = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Query
    {
        get => _query;
        set { _query = value; OnPropertyChanged(); }
    }

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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
