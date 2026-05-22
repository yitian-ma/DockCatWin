using System.IO;
using Forms = System.Windows.Forms;

namespace DockCatWin.UI.Tray;

public sealed class TrayIconController : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;
    private System.Drawing.Icon? currentIcon;

    public TrayIconController(string? iconImagePath = null)
    {
        notifyIcon = new Forms.NotifyIcon
        {
            Icon = CreateIcon(iconImagePath) ?? System.Drawing.SystemIcons.Application,
            Text = "DockCatWin",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
    }

    public event Action? PetRequested;
    public event Action? ToggleStateRequested;
    public event Action? OutingRequested;
    public event Action? RecallRequested;
    public event Action? SettingsRequested;
    public event Action? RestoreDataRequested;
    public event Action? ToggleVisibilityRequested;
    public event Action? ExitRequested;

    public void Update(
        bool isVisible,
        bool isWalking,
        bool isOutingAway = false,
        string? statusText = null,
        string? remainingText = null)
    {
        var menu = notifyIcon.ContextMenuStrip!;
        menu.Items.Clear();
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            menu.Items.Add(statusText).Enabled = false;
        }

        if (!string.IsNullOrWhiteSpace(remainingText))
        {
            menu.Items.Add(remainingText).Enabled = false;
        }

        if (menu.Items.Count > 0)
        {
            menu.Items.Add(new Forms.ToolStripSeparator());
        }

        if (isOutingAway)
        {
            menu.Items.Add("召回小猫", null, (_, _) => RecallRequested?.Invoke());
        }
        else
        {
            menu.Items.Add("摸摸", null, (_, _) => PetRequested?.Invoke());
            menu.Items.Add(isWalking ? "休息一下" : "散步", null, (_, _) => ToggleStateRequested?.Invoke());
            menu.Items.Add("出门玩吧", null, (_, _) => OutingRequested?.Invoke());
        }
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("设置...", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add("恢复备份...", null, (_, _) => RestoreDataRequested?.Invoke());
        menu.Items.Add(isVisible ? "隐藏小猫" : "显示小猫", null, (_, _) => ToggleVisibilityRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke());
    }

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        currentIcon?.Dispose();
    }

    private System.Drawing.Icon? CreateIcon(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        try
        {
            using var bitmap = new System.Drawing.Bitmap(imagePath!);
            using var square = new System.Drawing.Bitmap(32, 32);
            using (var graphics = System.Drawing.Graphics.FromImage(square))
            {
                graphics.Clear(System.Drawing.Color.Transparent);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                var scale = Math.Min(32.0 / bitmap.Width, 32.0 / bitmap.Height);
                var width = (int)(bitmap.Width * scale);
                var height = (int)(bitmap.Height * scale);
                graphics.DrawImage(bitmap, (32 - width) / 2, 32 - height, width, height);
            }

            currentIcon = System.Drawing.Icon.FromHandle(square.GetHicon());
            return currentIcon;
        }
        catch
        {
            return null;
        }
    }
}
