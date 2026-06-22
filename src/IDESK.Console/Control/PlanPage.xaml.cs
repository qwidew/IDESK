using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDESK.Widgets.Plan.Service;

namespace IDESK.Console.Control;

public partial class PlanPage : UserControl
{
    private readonly IPlanService _planService;
    private readonly IPlanLiteService _planLiteService;
    private bool _isPlanCreated;
    private bool _isLiteCreated;

    public event Action? PlanCreated;
    public event Action? PlanDeleteRequested;
    public event Action? LiteCreated;
    public event Action? LiteDeleteRequested;

    public PlanPage(IPlanService planService, IPlanLiteService planLiteService)
    {
        _planService = planService;
        _planLiteService = planLiteService;
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            _isPlanCreated = await _planService.GetCreatedAsync();
            _isLiteCreated = await _planLiteService.GetCreatedAsync();
            UpdateCards();
        };
    }

    // ── Plan ──

    private void OnPlanCreateClick(object sender, MouseButtonEventArgs e)
    {
        _isPlanCreated = true;
        _planService.SetCreatedAsync();
        UpdateCards();
        PlanCreated?.Invoke();
    }

    private async void OnPlanDeleteClick(object sender, RoutedEventArgs e)
    {
        _isPlanCreated = false;
        var cfg = await _planService.GetConfigAsync();
        if (cfg != null)
        {
            cfg.Created = false;
            await _planService.SaveConfigAsync(cfg);
        }
        UpdateCards();
        PlanDeleteRequested?.Invoke();
    }

    // ── Plan Lite ──

    private void OnLiteCreateClick(object sender, MouseButtonEventArgs e)
    {
        _isLiteCreated = true;
        _planLiteService.SetCreatedAsync();
        UpdateCards();
        LiteCreated?.Invoke();
    }

    private async void OnLiteDeleteClick(object sender, RoutedEventArgs e)
    {
        _isLiteCreated = false;
        var cfg = await _planLiteService.GetConfigAsync();
        if (cfg != null)
        {
            cfg.Created = false;
            await _planLiteService.SaveConfigAsync(cfg);
        }
        UpdateCards();
        LiteDeleteRequested?.Invoke();
    }

    private void UpdateCards()
    {
        PlanCreateBtn.Visibility = _isPlanCreated ? Visibility.Collapsed : Visibility.Visible;
        PlanCard.Visibility = _isPlanCreated ? Visibility.Visible : Visibility.Collapsed;
        LiteCreateBtn.Visibility = _isLiteCreated ? Visibility.Collapsed : Visibility.Visible;
        LiteCard.Visibility = _isLiteCreated ? Visibility.Visible : Visibility.Collapsed;
    }
}
