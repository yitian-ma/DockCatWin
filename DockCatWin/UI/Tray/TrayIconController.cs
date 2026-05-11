using Forms = System.Windows.Forms;

namespace DockCatWin.UI.Tray;

public sealed class TrayIconController : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;

    public TrayIconController()
    {
        notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "DockCatWin",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
    }

    public event Action? PetRequested;
    public event Action? ToggleStateRequested;
    public event Action? SettingsRequested;
    public event Action? ToggleVisibilityRequested;
    public event Action? ExitRequested;

    public void Update(bool isVisible, bool isWalking)
    {
        var menu = notifyIcon.ContextMenuStrip!;
        menu.Items.Clear();
        menu.Items.Add("摸摸", null, (_, _) => PetRequested?.Invoke());
        menu.Items.Add(isWalking ? "休息一下" : "散步", null, (_, _) => ToggleStateRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("设置...", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add(isVisible ? "隐藏小猫" : "显示小猫", null, (_, _) => ToggleVisibilityRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke());
    }

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }
}
