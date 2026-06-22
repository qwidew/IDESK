using System.Windows;

namespace IDESK.Core;

/// <summary>
/// Window 位置/尺寸持久化辅助，与 IWidgetPosition 配合使用。
/// 复用方式与 WidgetWindowHelper 对 DeskWidget 的 Save/Restore 一致。
/// </summary>
public static class WindowPositionHelper
{
    public static void SavePosition(Window window, IWidgetPosition pos)
    {
        pos.PositionX = window.Left;
        pos.PositionY = window.Top;
        pos.Width = window.Width;
        pos.Height = window.Height;
    }

    public static void RestorePosition(Window window, IWidgetPosition pos)
    {
        window.Width = pos.Width;
        window.Height = pos.Height;
        if (pos.PositionX != 0 || pos.PositionY != 0)
        {
            window.Left = pos.PositionX;
            window.Top = pos.PositionY;
        }
    }
}
