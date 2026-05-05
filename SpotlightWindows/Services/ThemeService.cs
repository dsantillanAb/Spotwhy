using Microsoft.Win32;

namespace SpotWhy.Services;

public enum AppTheme { Dark, Light }

public static class ThemeService
{
    public static AppTheme GetCurrentTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
                return value == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch { }
        return AppTheme.Dark;
    }

    public static (System.Windows.Media.Color Bg, System.Windows.Media.Color Border,
                   System.Windows.Media.Color TextPrimary, System.Windows.Media.Color TextSecondary,
                   System.Windows.Media.Color TextPlaceholder, System.Windows.Media.Color TextEmpty,
                   System.Windows.Media.Color ItemHover, System.Windows.Media.Color ItemSelected,
                   System.Windows.Media.Color BadgeBg, System.Windows.Media.Color BadgeText,
                   System.Windows.Media.Color Separator, System.Windows.Media.Color IconBg,
                   System.Windows.Media.Color IconPrimary, System.Windows.Media.Color IconSecondary,
                   System.Windows.Media.Color IconStroke)
        GetColors(AppTheme theme)
    {
        if (theme == AppTheme.Light)
        {
            return (
                Bg: System.Windows.Media.Color.FromArgb(235, 252, 252, 253),
                Border: System.Windows.Media.Color.FromArgb(235, 252, 252, 253),
                TextPrimary: System.Windows.Media.Color.FromArgb(235, 25, 25, 30),
                TextSecondary: System.Windows.Media.Color.FromArgb(150, 80, 80, 90),
                TextPlaceholder: System.Windows.Media.Color.FromArgb(80, 140, 140, 150),
                TextEmpty: System.Windows.Media.Color.FromArgb(60, 140, 140, 150),
                ItemHover: System.Windows.Media.Color.FromArgb(14, 0, 0, 0),
                ItemSelected: System.Windows.Media.Color.FromArgb(28, 0, 0, 0),
                BadgeBg: System.Windows.Media.Color.FromArgb(12, 0, 0, 0),
                BadgeText: System.Windows.Media.Color.FromArgb(80, 100, 100, 110),
                Separator: System.Windows.Media.Color.FromArgb(18, 0, 0, 0),
                IconBg: System.Windows.Media.Color.FromArgb(18, 0, 0, 0),
                IconPrimary: System.Windows.Media.Color.FromArgb(255, 30, 122, 68),
                IconSecondary: System.Windows.Media.Color.FromArgb(255, 107, 191, 138),
                IconStroke: System.Windows.Media.Color.FromArgb(255, 15, 77, 40)
            );
        }
        else
        {
            return (
                Bg: System.Windows.Media.Color.FromArgb(190, 16, 16, 20),
                Border: System.Windows.Media.Color.FromArgb(190, 16, 16, 20),
                TextPrimary: System.Windows.Media.Color.FromArgb(255, 255, 255, 255),
                TextSecondary: System.Windows.Media.Color.FromArgb(140, 200, 200, 210),
                TextPlaceholder: System.Windows.Media.Color.FromArgb(85, 255, 255, 255),
                TextEmpty: System.Windows.Media.Color.FromArgb(60, 255, 255, 255),
                ItemHover: System.Windows.Media.Color.FromArgb(20, 255, 255, 255),
                ItemSelected: System.Windows.Media.Color.FromArgb(40, 255, 255, 255),
                BadgeBg: System.Windows.Media.Color.FromArgb(16, 255, 255, 255),
                BadgeText: System.Windows.Media.Color.FromArgb(80, 200, 200, 210),
                Separator: System.Windows.Media.Color.FromArgb(24, 255, 255, 255),
                IconBg: System.Windows.Media.Color.FromArgb(24, 255, 255, 255),
                IconPrimary: System.Windows.Media.Color.FromArgb(255, 60, 190, 110),
                IconSecondary: System.Windows.Media.Color.FromArgb(255, 140, 220, 170),
                IconStroke: System.Windows.Media.Color.FromArgb(255, 30, 140, 70)
            );
        }
    }
}
