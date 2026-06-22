using IDESK.Core;

namespace IDESK.Widgets.Translate;

public class TranslateWindow : DeskWidget
{
    public TranslateWindow()
    {
        var vm = new TranslateViewModel();
        NormalContent = new TranslateView(vm);
        Title = "翻译";
    }
}
