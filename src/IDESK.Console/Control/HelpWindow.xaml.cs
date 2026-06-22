using System.IO;
using System.Windows;
using Markdig;

namespace IDESK.Console.Control;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadMd();
    }

    private void LoadMd()
    {
        try
        {
            string mdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help.md");
            if (!File.Exists(mdPath))
            {
                ShowHtml("<p style='color:#999;font-size:18px;font-family:sans-serif;text-align:center;margin-top:100px;'>暂无帮助内容</p>");
                return;
            }
            string md = File.ReadAllText(mdPath);
            ShowHtml(Wrap(Markdown.ToHtml(md)));
        }
        catch
        {
            ShowHtml("<p style='color:red;font-size:18px;font-family:sans-serif;'>加载失败</p>");
        }
    }

    private void ShowHtml(string html) => Browser.NavigateToString(html);

    private static string Wrap(string body) => $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
body {{ font-family: -apple-system,'Segoe UI',sans-serif; padding: 32px 36px; color: #1a1a1a; font-size: 18px; line-height: 2.2; background: #fff; }}
h1 {{ font-size: 32px; margin: 0 0 10px 0; }}
h2 {{ font-size: 26px; margin: 28px 0 12px 0; }}
blockquote {{ margin: 0 0 16px 0; padding: 0 0 0 16px; border-left: 4px solid #2196F3; color: #666; font-size: 17px; }}
ul {{ margin: 0; padding-left: 26px; }}
li {{ margin: 8px 0; }}
table {{ border-collapse: collapse; margin: 12px 0; }}
td, th {{ border: 1px solid #ddd; padding: 8px 14px; text-align: left; font-size: 16px; }}
th {{ background: #f5f5f5; }}
code {{ background: #f0f0f0; padding: 2px 8px; border-radius: 3px; font-size: 16px; }}
</style>
</head>
<body>
{body}
</body>
</html>";
}
