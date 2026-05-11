using System.Windows;
using Forms = System.Windows.Forms;

namespace DockCatWin.Platform;

internal enum TaskbarEdge
{
    Bottom,
    Top,
    Left,
    Right,
    Unknown
}

internal readonly record struct TaskbarActivityArea(
    Rect Screen,
    Rect WorkingArea,
    TaskbarEdge Edge,
    double BaselineY,
    double MinX,
    double MaxX);

internal static class TaskbarGeometry
{
    public static TaskbarActivityArea Current(double dpiScaleX, double dpiScaleY)
    {
        var screen = Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens.First();
        var bounds = ToDip(screen.Bounds, dpiScaleX, dpiScaleY);
        var working = ToDip(screen.WorkingArea, dpiScaleX, dpiScaleY);
        var edge = InferEdge(bounds, working);

        var baselineY = edge switch
        {
            TaskbarEdge.Top => working.Top,
            TaskbarEdge.Bottom => working.Bottom,
            _ => working.Bottom
        };

        return new TaskbarActivityArea(
            Screen: bounds,
            WorkingArea: working,
            Edge: edge,
            BaselineY: baselineY,
            MinX: working.Left,
            MaxX: working.Right);
    }

    private static Rect ToDip(System.Drawing.Rectangle rectangle, double dpiScaleX, double dpiScaleY)
    {
        return new Rect(
            rectangle.Left / dpiScaleX,
            rectangle.Top / dpiScaleY,
            rectangle.Width / dpiScaleX,
            rectangle.Height / dpiScaleY);
    }

    private static TaskbarEdge InferEdge(Rect screen, Rect working)
    {
        const double threshold = 8;

        if (working.Bottom < screen.Bottom - threshold)
        {
            return TaskbarEdge.Bottom;
        }

        if (working.Top > screen.Top + threshold)
        {
            return TaskbarEdge.Top;
        }

        if (working.Left > screen.Left + threshold)
        {
            return TaskbarEdge.Left;
        }

        if (working.Right < screen.Right - threshold)
        {
            return TaskbarEdge.Right;
        }

        return TaskbarEdge.Unknown;
    }
}
