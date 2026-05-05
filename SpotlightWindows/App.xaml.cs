using System.Reflection;
using System.Windows;

namespace SpotWhy;

public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                Assembly.GetExecutingAssembly().Location),
            Text = "SpotWhy\nCtrl + Espacio para buscar",
            Visible = true
        };

        _trayIcon.Click += (s, args) =>
        {
            var window = Windows.OfType<MainWindow>().FirstOrDefault();
            window?.ToggleVisibility();
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Salir", null, (s, args) => Shutdown());
        _trayIcon.ContextMenuStrip = menu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
