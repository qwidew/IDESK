using System.Windows;
using System.Windows.Controls;
using IDESK.Core;

namespace IDESK.Transient.Chat;

/// <summary>
/// 聊天临时窗口。按快捷键（默认 Alt+Space）创建关闭，或点击 ✕ 关闭销毁。
/// 内部嵌入完整的 ChatPage（含主题选择、聊天记录、上下文压缩）。
/// </summary>
public sealed class ChatWindow : TransientWidget
{
    public ChatWindow()
    {
        Title = "Chat";
        Width = 480;
        Height = 640;

        var body = new Border
        {
            CornerRadius = new CornerRadius(8),
            Child = new ChatPage(),
        };
        body.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
        NormalContent = body;
    }
}
