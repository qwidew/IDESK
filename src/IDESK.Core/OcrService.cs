using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Windows.Graphics.Imaging;

namespace IDESK.Core;

public static class OcrService
{
    /// <param name="region">屏幕绝对坐标区域（物理像素）</param>
    public static async Task<string?> CaptureAndOcrAsync(Rect region)
    {
        int x = (int)region.X, y = (int)region.Y;
        int w = (int)region.Width, h = (int)region.Height;
        if (w <= 0 || h <= 0) return null;

        var softwareBitmap = await Task.Run(() => CaptureRegion(x, y, w, h));
        if (softwareBitmap == null) return null;

        return await RecognizeAsync(softwareBitmap);
    }

    private static SoftwareBitmap? CaptureRegion(int x, int y, int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var ras = stream.AsRandomAccessStream();
        var decoder = Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras).GetAwaiter().GetResult();
        return decoder.GetSoftwareBitmapAsync().GetAwaiter().GetResult();
    }

    private static async Task<string?> RecognizeAsync(SoftwareBitmap bitmap)
    {
        var ocr = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(
                     new Windows.Globalization.Language("zh-CN"))
                 ?? Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();

        if (ocr == null) return null;

        var result = await ocr.RecognizeAsync(bitmap);
        return result?.Text?.Trim();
    }
}
