using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DockCatWin.Platform;
using WpfImage = System.Windows.Controls.Image;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

namespace DockCatWin.UI.CatWindow;

public sealed class CatWindowController
{
    private readonly Window window;
    private readonly WpfImage image;
    private readonly ScaleTransform mirrorTransform;
    private WpfSize sourceSize;
    private double scale = 0.1;
    private double extraTopWidth;
    private double extraTopHeight;

    public CatWindowController(Window window, WpfImage image, ScaleTransform mirrorTransform, WpfSize sourceSize)
    {
        this.window = window;
        this.image = image;
        this.mirrorTransform = mirrorTransform;
        this.sourceSize = sourceSize;
        ApplySize();
    }

    public WpfSize CatSize => new(image.Width, image.Height);

    public void SetImage(BitmapImage? bitmap)
    {
        image.Source = bitmap;
    }

    public void SetImageScale(double percent)
    {
        scale = Math.Clamp(percent, 4, 100) / 100;
        ApplySize();
    }

    public void SetSourceSize(WpfSize size)
    {
        if (size.Width <= 0 || size.Height <= 0 || size == sourceSize)
        {
            return;
        }

        sourceSize = size;
        ApplySize();
    }

    public void SetExtraTopContent(double width, double height)
    {
        extraTopWidth = Math.Max(0, width);
        extraTopHeight = Math.Max(0, height);
        ApplySize();
    }

    public void SetMirrored(bool mirrored)
    {
        mirrorTransform.ScaleX = mirrored ? -1 : 1;
    }

    public void SetAnchor(WpfPoint anchor, TaskbarEdge edge)
    {
        if (edge is TaskbarEdge.Left or TaskbarEdge.Right)
        {
            window.Left = anchor.X - CatSize.Width - CatOffsetX;
            window.Top = anchor.Y - window.Height;
            return;
        }

        window.Left = anchor.X - CatOffsetX;
        window.Top = anchor.Y - window.Height;
    }

    public WpfPoint CurrentAnchor(TaskbarEdge edge)
    {
        if (edge is TaskbarEdge.Left or TaskbarEdge.Right)
        {
            return new WpfPoint(window.Left + CatOffsetX + CatSize.Width, window.Top + window.Height);
        }

        return new WpfPoint(window.Left + CatOffsetX, window.Top + window.Height);
    }

    private void ApplySize()
    {
        var width = Math.Max(48, sourceSize.Width * scale);
        var height = Math.Max(48, sourceSize.Height * scale);
        image.Width = width;
        image.Height = height;
        window.Width = Math.Max(width, extraTopWidth);
        window.Height = height + extraTopHeight;
    }

    private double CatOffsetX => Math.Max(0, (window.Width - CatSize.Width) / 2);
}
