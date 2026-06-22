using System.IO;
using System.Text.Json;
using System.Windows;

namespace IDESK.Core;

public static class ThemeManager
{
    private const string BaseUri = "pack://application:,,,/IDESK.Core;component/Style/";

    private static bool _isDark;

    public static bool IsDark => _isDark;

    public static void Load()
    {
        string path = GetPath();
        try
        {
            string json = File.ReadAllText(path);
            _isDark = JsonSerializer.Deserialize<bool>(json);
        }
        catch
        {
            _isDark = false;
        }

        Apply(_isDark);
    }

    public static void Toggle()
    {
        _isDark = !_isDark;
        Apply(_isDark);
        Save();
    }

    private static void Apply(bool dark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;

        // Colors.xaml is always loaded via App.xaml — only swap theme styles
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var uri = dicts[i].Source?.ToString() ?? "";
            if (uri.Contains("DefaultLightTheme.xaml") || uri.Contains("DefaultDarkTheme.xaml"))
            {
                dicts.RemoveAt(i);
            }
        }

        string themeFile = dark ? "DefaultDarkTheme.xaml" : "DefaultLightTheme.xaml";
        dicts.Add(new ResourceDictionary { Source = new Uri($"{BaseUri}{themeFile}") });
    }

    private static string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "theme.json");

    private static void Save()
    {
        string path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(_isDark));
    }
}
