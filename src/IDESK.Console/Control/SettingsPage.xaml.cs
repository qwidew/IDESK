using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IDESK.Console.Models;
using IDESK.Core;
using IDESK.Core.Agent;
using Microsoft.Win32;

namespace IDESK.Console.Control;

public partial class SettingsPage : UserControl
{
    public event Action<bool>? DebugPageVisibilityChanged;

    private readonly LlmConfig _llmConfig;
    private readonly TransientConfig _transientConfig;

    public SettingsPage()
    {
        InitializeComponent();
        _llmConfig = LlmConfig.Load();
        ApiUrlBox.Text = _llmConfig.Url;
        ApiKeyBox.Password = _llmConfig.Key;
        ModelBox.Text = _llmConfig.Model;
        MaxContextBox.Text = _llmConfig.MaxContext.ToString();
        ProviderToggle.IsChecked = _llmConfig.Provider == "anthropic";

        _transientConfig = TransientConfig.Load();
        MaxLoopsBox.Text = _transientConfig.MaxAgentLoops.ToString();
        AutoCloseToggle.IsChecked = _transientConfig.AutoCloseOnDeactivated;
        DangerConfirmToggle.IsChecked = _transientConfig.DangerousConfirm;
        DebugPageToggle.IsChecked = _transientConfig.DebugPageVisible;
        StartMinimizedToggle.IsChecked = _transientConfig.StartMinimized;
    }

    private void OnProviderToggle(object sender, RoutedEventArgs e)
    {
        _llmConfig.Provider = ProviderToggle.IsChecked == true ? "anthropic" : "openai";
    }

    private void OnOpenDataFolder(object sender, RoutedEventArgs e)
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IDESK");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        Process.Start("explorer.exe", dir);
    }

    private void OnEnableAutoStart(object sender, RoutedEventArgs e)
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue("IDESK",
                Process.GetCurrentProcess().MainModule?.FileName ?? "");
            MessageBox.Show("开机自启已开启", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"设置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnDisableAutoStart(object sender, RoutedEventArgs e)
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("IDESK", false);
            MessageBox.Show("开机自启已关闭", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"关闭失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnOpenKeyBindings(object sender, RoutedEventArgs e)
    {
        var console = (ConsoleWindow)Window.GetWindow(this);
        var page = new KeyBindingsPage();
        page.BackRequested += console.NavigateBack;
        console.NavigateTo(page);
    }

    private void OnSaveLlmConfig(object sender, RoutedEventArgs e)
    {
        _llmConfig.Url = ApiUrlBox.Text.Trim();
        _llmConfig.Key = ApiKeyBox.Password;
        _llmConfig.Model = ModelBox.Text.Trim();
        if (int.TryParse(MaxContextBox.Text.Trim(), out int mc) && mc > 0)
            _llmConfig.MaxContext = mc;
        _llmConfig.Save();

        if (int.TryParse(MaxLoopsBox.Text.Trim(), out int ml) && ml > 0 && ml <= 20)
            _transientConfig.MaxAgentLoops = ml;
        _transientConfig.Save();

        MessageBox.Show("LLM 配置已保存", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnAutoCloseToggle(object sender, RoutedEventArgs e)
    {
        _transientConfig.AutoCloseOnDeactivated = AutoCloseToggle.IsChecked == true;
        _transientConfig.Save();
    }

    private void OnDangerConfirmToggle(object sender, RoutedEventArgs e)
    {
        _transientConfig.DangerousConfirm = DangerConfirmToggle.IsChecked == true;
        _transientConfig.Save();
    }

    private void OnDebugPageToggle(object sender, RoutedEventArgs e)
    {
        _transientConfig.DebugPageVisible = DebugPageToggle.IsChecked == true;
        _transientConfig.Save();
        DebugPageVisibilityChanged?.Invoke(DebugPageToggle.IsChecked == true);
    }

    private void OnUninstallInfo(object sender, RoutedEventArgs e)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dir = System.IO.Path.Combine(localAppData, "IDESK");
        string msg = $"要完全卸载，请手动删除以下目录：\n\n{dir}\n\n该目录包含所有用户数据（待办、笔记、日程等）。";
        MessageBox.Show(msg, "卸载帮助", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnStartMinimizedToggle(object sender, RoutedEventArgs e)
    {
        _transientConfig.StartMinimized = StartMinimizedToggle.IsChecked == true;
        _transientConfig.Save();
    }
}
