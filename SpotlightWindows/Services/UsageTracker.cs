using System.Text.Json;

namespace SpotWhy.Services;

public class UsageTracker
{
    private readonly string _filePath;
    private Dictionary<string, int> _usage = new();
    private readonly object _lock = new();

    public UsageTracker()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpotWhy");
        System.IO.Directory.CreateDirectory(dir);
        _filePath = System.IO.Path.Combine(dir, "usage.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (System.IO.File.Exists(_filePath))
            {
                var json = System.IO.File.ReadAllText(_filePath);
                _usage = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
            }
        }
        catch { _usage = new(); }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_usage);
            System.IO.File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    public void TrackOpen(string key)
    {
        lock (_lock)
        {
            _usage.TryGetValue(key, out var count);
            _usage[key] = count + 1;
            Save();
        }
    }

    public int GetUsage(string key)
    {
        lock (_lock)
        {
            return _usage.GetValueOrDefault(key, 0);
        }
    }

    public List<(string Key, int Count)> GetTopUsed(int count = 10)
    {
        lock (_lock)
        {
            return _usage
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .Select(kv => (kv.Key, kv.Value))
                .ToList();
        }
    }
}
