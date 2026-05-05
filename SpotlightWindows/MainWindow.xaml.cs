using SpotWhy.Models;
using SpotWhy.Services;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using Timer = System.Timers.Timer;

namespace SpotWhy;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService;
    private readonly SearchService _searchService;
    private readonly Timer _memoryTimer;
    private bool _isVisible;
    private AppTheme _currentTheme;
    private const int BarHeight = 52;
    private const int ItemHeight = 44;
    private const int MaxListItems = 10;

    public MainWindow()
    {
        InitializeComponent();

        _hotkeyService = new HotkeyService(this);
        _searchService = new SearchService();

        _memoryTimer = new Timer(3000);
        _memoryTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateMemoryDisplay);
        _memoryTimer.Start();

        Loaded += OnLoaded;
    }

    private void UpdateMemoryDisplay()
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            var mb = proc.PrivateMemorySize64 / (1024.0 * 1024.0);
            MemoryText.Text = $"SpotWhy  ·  {mb:F1} MB";
        }
        catch
        {
            MemoryText.Text = "SpotWhy";
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AcrylicService.Apply(this);

        _currentTheme = ThemeService.GetCurrentTheme();
        ApplyTheme(_currentTheme);

        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.Register();

        Height = BarHeight;
        Hide();
        _isVisible = false;

        CenterWindow();
    }

    private void CenterWindow()
    {
        var desktop = SystemParameters.WorkArea;
        Left = (desktop.Width - Width) / 2;
        Top = (desktop.Height - BarHeight) / 2;
    }

    private void ApplyTheme(AppTheme theme)
    {
        var c = ThemeService.GetColors(theme);

        Resources["ThemeBg"] = new SolidColorBrush(c.Bg);
        Resources["ThemeBorder"] = new SolidColorBrush(c.Border);
        Resources["ThemeTextPrimary"] = new SolidColorBrush(c.TextPrimary);
        Resources["ThemeTextSecondary"] = new SolidColorBrush(c.TextSecondary);
        Resources["ThemeTextPlaceholder"] = new SolidColorBrush(c.TextPlaceholder);
        Resources["ThemeTextEmpty"] = new SolidColorBrush(c.TextEmpty);
        Resources["ThemeItemHover"] = new SolidColorBrush(c.ItemHover);
        Resources["ThemeItemSelected"] = new SolidColorBrush(c.ItemSelected);
        Resources["ThemeBadgeBg"] = new SolidColorBrush(c.BadgeBg);
        Resources["ThemeBadgeText"] = new SolidColorBrush(c.BadgeText);
        Resources["ThemeSeparator"] = new SolidColorBrush(c.Separator);
        Resources["ThemeIconBg"] = new SolidColorBrush(c.IconBg);
        Resources["ThemeIconPrimary"] = new SolidColorBrush(c.IconPrimary);
        Resources["ThemeIconSecondary"] = new SolidColorBrush(c.IconSecondary);
        Resources["ThemeIconStroke"] = new SolidColorBrush(c.IconStroke);
        Resources["ThemeCaret"] = new SolidColorBrush(c.TextPrimary);
    }

    public void ToggleVisibility()
    {
        if (_isVisible)
            HideWindow();
        else
            ShowWindow();
    }

    private void OnHotkeyPressed()
    {
        Dispatcher.Invoke(() =>
        {
            if (_isVisible)
                HideWindow();
            else
                ShowWindow();
        });
    }

    private void ShowWindow()
    {
        var currentTheme = ThemeService.GetCurrentTheme();
        if (currentTheme != _currentTheme)
        {
            _currentTheme = currentTheme;
            ApplyTheme(_currentTheme);
            AcrylicService.Apply(this);
        }

        CenterWindow();
        Show();
        _isVisible = true;
        Activate();
        UpdateMemoryDisplay();

        SearchBox.Text = "";
        SearchBox.Focus();
        EmptyStateText.Visibility = Visibility.Collapsed;

        // Show frequent apps
        var frequent = _searchService.Search("");
        ResultsList.ItemsSource = frequent;
        if (frequent.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
            var listHeight = Math.Min(frequent.Count, MaxListItems) * ItemHeight;
            Height = BarHeight + (frequent.Count > 0 ? listHeight + 12 : 0);
        }
        else
        {
            Height = BarHeight;
            EmptyStateText.Visibility = Visibility.Visible;
        }

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void HideWindow()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(80));
        fadeOut.Completed += (s, e) =>
        {
            Hide();
            _isVisible = false;
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        PlaceholderText.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(query))
        {
            ResultsList.ItemsSource = null;
            EmptyStateText.Visibility = Visibility.Visible;
            AnimateHeight(BarHeight);
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;

        var results = _searchService.Search(query);
        ResultsList.ItemsSource = results;

        if (results.Count > 0)
            ResultsList.SelectedIndex = 0;

        var listHeight = Math.Min(results.Count, MaxListItems) * ItemHeight;
        var totalHeight = BarHeight + (results.Count > 0 ? listHeight + 12 : 0);
        AnimateHeight(Math.Min(totalHeight, MaxHeight));
    }

    private void AnimateHeight(double targetHeight)
    {
        if (Math.Abs(Height - targetHeight) < 1) return;

        var anim = new DoubleAnimation(Height, targetHeight, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(HeightProperty, anim);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideWindow();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (ResultsList.Items.Count > 0)
            {
                ResultsList.Focus();
                ResultsList.SelectedIndex = 0;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            OpenSelectedItem();
            e.Handled = true;
        }
    }

    private void ResultsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideWindow();
            e.Handled = true;
        }
        else if (e.Key == Key.Up && ResultsList.SelectedIndex <= 0)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            OpenSelectedItem();
            e.Handled = true;
        }
        else if (e.Key == Key.Back && SearchBox.Text.Length > 0)
        {
            SearchBox.Focus();
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedItem();
    }

    private void OpenSelectedItem()
    {
        if (ResultsList.SelectedItem is not SearchResult result) return;

        try
        {
            _searchService.TrackOpen(result);

            var path = result.Path;

            if (result.Type == SearchResultType.Application)
            {
                if (path.Contains('!') && !path.EndsWith(".lnk"))
                {
                    // AppX / UWP app
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"shell:AppsFolder\\{path}",
                        UseShellExecute = true
                    });
                }
                else if (path.EndsWith(".lnk"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }

            HideWindow();
        }
        catch { }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (_isVisible)
            HideWindow();
    }

    protected override void OnClosed(EventArgs e)
    {
        _hotkeyService.Unregister();
        _memoryTimer?.Stop();
        _memoryTimer?.Dispose();
        (_searchService as IDisposable)?.Dispose();
        base.OnClosed(e);
    }
}
