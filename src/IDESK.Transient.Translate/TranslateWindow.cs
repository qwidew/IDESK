using IDESK.Core;
using IDESK.Widgets.Translate;

namespace IDESK.Transient.Translate;

/// <summary>
/// 翻译输入临时窗口。默认紧凑输入框大小，翻译后结果展示在独立子窗口中。
/// 复用 IDESK.Widgets.Translate 的 TranslateViewModel / TranslateDisplayItem / 解析逻辑。
/// </summary>
public sealed class TranslateWindow : TransientWidget
{
    public TranslateWindow()
    {
        Title = "Translate";
        Width = 420;
        Height = 68;  // 单行输入高度

        var vm = new TranslateViewModel();
        NormalContent = new TranslateTransientView(vm);
    }
}
