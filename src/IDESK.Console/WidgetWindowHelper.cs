using IDESK.Core;

namespace IDESK.Console;

internal static class WidgetWindowHelper
{
    public static void SavePosition(DeskWidget window, IWidgetPosition pos)
    {
        pos.PositionY = window.Top;
        pos.BookmarkPresetId = window.BookmarkPresetId;
        pos.IsBookmarkMode = window.IsBookmarkMode;
        if (window.IsBookmarkMode)
            pos.BookmarkPositionX = window.Left;
        else
        {
            pos.PositionX = window.Left;
            pos.Width = window.Width;
            pos.Height = window.Height;
        }
    }

    public static void RestorePosition(DeskWidget window, IWidgetPosition pos)
    {
        window.Width = pos.Width;
        window.Height = pos.Height;
        window.BookmarkPresetId = pos.BookmarkPresetId;

        if (pos.IsBookmarkMode)
        {
            window.Left = pos.PositionX;
            window.Top = pos.PositionY;
            window.RestoreBookmarkState(pos.BookmarkPositionX);
        }
        else if (pos.PositionX != 0 || pos.PositionY != 0)
        {
            window.Left = pos.PositionX;
            window.Top = pos.PositionY;
        }
    }

    public static void SetupAutoSave(DeskWidget window, IWidgetPosition pos, Func<Task> persistAsync)
    {
        window.BookmarkEntering += async () =>
        {
            SavePosition(window, pos);
            await persistAsync();
        };

        window.Deactivated += async (_, _) =>
        {
            SavePosition(window, pos);
            await persistAsync();
        };
    }
}
