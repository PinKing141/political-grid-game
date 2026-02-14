using Godot;
using PolisGrid.Core;
using System;
using System.Collections.Generic;
using System.Text;

public partial class HudController : CanvasLayer
{
    [Export] public NodePath SimulationManagerPath;
    [Export] public NodePath HeatmapPath;

    [Export] public NodePath TurnLabelPath;
    [Export] public NodePath StabilityLabelPath;
    [Export] public NodePath TurnoutLabelPath;
    [Export] public NodePath EventLabelPath;
    [Export] public NodePath GovernmentLabelPath;
    [Export] public NodePath TreasuryLabelPath;
    [Export] public NodePath CapitalLabelPath;
    [Export] public NodePath ProjectedPollsLabelPath;
    [Export] public NodePath FactionSummaryPath;
    [Export] public NodePath TurnDigestLabelPath;
    [Export] public NodePath ActionHistoryLabelPath;
    [Export] public NodePath UnionStrikeDealButtonPath;
    [Export] public NodePath MilitaryAppeasementButtonPath;
    [Export] public NodePath YouthReformButtonPath;

    [Export] public NodePath HealthcareTogglePath;
    [Export] public NodePath PolicingTogglePath;
    [Export] public NodePath BordersTogglePath;
    [Export] public NodePath ServicesBudgetSliderPath;
    [Export] public NodePath MilitaryBudgetSliderPath;
    [Export] public NodePath InfrastructureBudgetSliderPath;
    [Export] public NodePath CandidateNameInputPath;
    [Export] public NodePath CandidateBackgroundOptionPath;
    [Export] public NodePath CandidateTraitPrimaryOptionPath;
    [Export] public NodePath CandidateTraitSecondaryOptionPath;
    [Export] public NodePath CandidateSummaryLabelPath;
    [Export] public NodePath PolicyRampLabelPath;
    [Export] public NodePath InspectorTitlePath;
    [Export] public NodePath InspectorSummaryPath;
    [Export] public NodePath InspectorBlocsPath;
    [Export] public NodePath DashboardChartPath;
    [Export] public NodePath DashboardHoverLabelPath;
    [Export] public NodePath ModeOptionPath;
    [Export] public NodePath TileSizeModeTogglePath;
    [Export] public NodePath EventPopupPanelPath;
    [Export] public NodePath EventPopupTitlePath;
    [Export] public NodePath EventPopupBodyPath;
    [Export] public NodePath GameOverPanelPath;
    [Export] public NodePath GameOverTitlePath;
    [Export] public NodePath GameOverBodyPath;

    [Export] public float PlayIntervalSeconds = 0.9f;
    [Export] public float FastIntervalSeconds = 0.2f;

    private SimulationManager _simulationManager;
    private HeatmapView _heatmapView;
    private Label _turnLabel;
    private Label _stabilityLabel;
    private Label _turnoutLabel;
    private Label _eventLabel;
    private Label _governmentLabel;
    private Label _treasuryLabel;
    private Label _capitalLabel;
    private Label _projectedPollsLabel;
    private RichTextLabel _factionSummaryLabel;
    private Label _turnDigestLabel;
    private RichTextLabel _actionHistoryLabel;
    private Button _unionStrikeDealButton;
    private Button _militaryAppeasementButton;
    private Button _youthReformButton;
    private CheckButton _healthcareToggle;
    private CheckButton _policingToggle;
    private CheckButton _bordersToggle;
    private HSlider _servicesBudgetSlider;
    private HSlider _militaryBudgetSlider;
    private HSlider _infrastructureBudgetSlider;
    private LineEdit _candidateNameInput;
    private OptionButton _candidateBackgroundOption;
    private OptionButton _candidateTraitPrimaryOption;
    private OptionButton _candidateTraitSecondaryOption;
    private RichTextLabel _candidateSummaryLabel;
    private readonly List<string> _candidateBackgroundIds = new List<string>();
    private readonly List<string> _candidateTraitIds = new List<string>();
    private readonly Dictionary<string, CandidateBackgroundDefinition> _candidateBackgroundById = new Dictionary<string, CandidateBackgroundDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CandidateTraitDefinition> _candidateTraitById = new Dictionary<string, CandidateTraitDefinition>(StringComparer.OrdinalIgnoreCase);
    private RichTextLabel _policyRampLabel;
    private Label _inspectorTitle;
    private Label _inspectorSummary;
    private RichTextLabel _inspectorBlocs;
    private HistoryChart _dashboardChart;
    private Label _dashboardHoverLabel;
    private OptionButton _modeOption;
    private CheckButton _tileSizeModeToggle;
    private Control _eventPopupPanel;
    private Label _eventPopupTitle;
    private Label _eventPopupBody;
    private Control _gameOverPanel;
    private Label _gameOverTitle;
    private Label _gameOverBody;
    private Timer _tickTimer;
    private Timer _eventPopupTimer;
    private bool _hasSelectedTile;
    private Vector2I _selectedTile;
    private int _lastShownEventTurn = -1;

    public override void _Ready()
    {
        _simulationManager = ResolveSimulationManager();
        _heatmapView = ResolveNodeOrWarn<HeatmapView>(HeatmapPath, nameof(HeatmapPath));

        _turnLabel = ResolveNodeOrWarn<Label>(TurnLabelPath, nameof(TurnLabelPath));
        _stabilityLabel = ResolveNodeOrWarn<Label>(StabilityLabelPath, nameof(StabilityLabelPath));
        _turnoutLabel = ResolveNodeOrWarn<Label>(TurnoutLabelPath, nameof(TurnoutLabelPath));
        _eventLabel = ResolveNodeOrWarn<Label>(EventLabelPath, nameof(EventLabelPath));
        _governmentLabel = ResolveNodeOrWarn<Label>(GovernmentLabelPath, nameof(GovernmentLabelPath));
        _treasuryLabel = ResolveNodeOrWarn<Label>(TreasuryLabelPath, nameof(TreasuryLabelPath));
        _capitalLabel = ResolveNodeOrWarn<Label>(CapitalLabelPath, nameof(CapitalLabelPath));
        _projectedPollsLabel = ResolveNodeOrWarn<Label>(ProjectedPollsLabelPath, nameof(ProjectedPollsLabelPath));
        _factionSummaryLabel = ResolveNodeOrWarn<RichTextLabel>(FactionSummaryPath, nameof(FactionSummaryPath));
        _turnDigestLabel = ResolveNodeOrWarn<Label>(TurnDigestLabelPath, nameof(TurnDigestLabelPath));
        _actionHistoryLabel = ResolveNodeOrWarn<RichTextLabel>(ActionHistoryLabelPath, nameof(ActionHistoryLabelPath));
        _unionStrikeDealButton = ResolveNodeOrWarn<Button>(UnionStrikeDealButtonPath, nameof(UnionStrikeDealButtonPath));
        _militaryAppeasementButton = ResolveNodeOrWarn<Button>(MilitaryAppeasementButtonPath, nameof(MilitaryAppeasementButtonPath));
        _youthReformButton = ResolveNodeOrWarn<Button>(YouthReformButtonPath, nameof(YouthReformButtonPath));
        _healthcareToggle = ResolveNodeOrWarn<CheckButton>(HealthcareTogglePath, nameof(HealthcareTogglePath));
        _policingToggle = ResolveNodeOrWarn<CheckButton>(PolicingTogglePath, nameof(PolicingTogglePath));
        _bordersToggle = ResolveNodeOrWarn<CheckButton>(BordersTogglePath, nameof(BordersTogglePath));
        _servicesBudgetSlider = ResolveNodeOrWarn<HSlider>(ServicesBudgetSliderPath, nameof(ServicesBudgetSliderPath));
        _militaryBudgetSlider = ResolveNodeOrWarn<HSlider>(MilitaryBudgetSliderPath, nameof(MilitaryBudgetSliderPath));
        _infrastructureBudgetSlider = ResolveNodeOrWarn<HSlider>(InfrastructureBudgetSliderPath, nameof(InfrastructureBudgetSliderPath));
        _candidateNameInput = ResolveNodeOrWarn<LineEdit>(CandidateNameInputPath, nameof(CandidateNameInputPath));
        _candidateBackgroundOption = ResolveNodeOrWarn<OptionButton>(CandidateBackgroundOptionPath, nameof(CandidateBackgroundOptionPath));
        _candidateTraitPrimaryOption = ResolveNodeOrWarn<OptionButton>(CandidateTraitPrimaryOptionPath, nameof(CandidateTraitPrimaryOptionPath));
        _candidateTraitSecondaryOption = ResolveNodeOrWarn<OptionButton>(CandidateTraitSecondaryOptionPath, nameof(CandidateTraitSecondaryOptionPath));
        _candidateSummaryLabel = ResolveNodeOrWarn<RichTextLabel>(CandidateSummaryLabelPath, nameof(CandidateSummaryLabelPath));
        _policyRampLabel = ResolveNodeOrWarn<RichTextLabel>(PolicyRampLabelPath, nameof(PolicyRampLabelPath));
        _inspectorTitle = ResolveNodeOrWarn<Label>(InspectorTitlePath, nameof(InspectorTitlePath));
        _inspectorSummary = ResolveNodeOrWarn<Label>(InspectorSummaryPath, nameof(InspectorSummaryPath));
        _inspectorBlocs = ResolveNodeOrWarn<RichTextLabel>(InspectorBlocsPath, nameof(InspectorBlocsPath));
        _dashboardChart = ResolveNodeOrWarn<HistoryChart>(DashboardChartPath, nameof(DashboardChartPath));
        _dashboardHoverLabel = ResolveNodeOrWarn<Label>(DashboardHoverLabelPath, nameof(DashboardHoverLabelPath));
        _modeOption = ResolveNodeOrWarn<OptionButton>(ModeOptionPath, nameof(ModeOptionPath));
        _tileSizeModeToggle = ResolveNodeOrWarn<CheckButton>(TileSizeModeTogglePath, nameof(TileSizeModeTogglePath));
        _eventPopupPanel = ResolveNodeOrWarn<Control>(EventPopupPanelPath, nameof(EventPopupPanelPath));
        _eventPopupTitle = ResolveNodeOrWarn<Label>(EventPopupTitlePath, nameof(EventPopupTitlePath));
        _eventPopupBody = ResolveNodeOrWarn<Label>(EventPopupBodyPath, nameof(EventPopupBodyPath));
        _gameOverPanel = ResolveNodeOrWarn<Control>(GameOverPanelPath, nameof(GameOverPanelPath));
        _gameOverTitle = ResolveNodeOrWarn<Label>(GameOverTitlePath, nameof(GameOverTitlePath));
        _gameOverBody = ResolveNodeOrWarn<Label>(GameOverBodyPath, nameof(GameOverBodyPath));

        _tickTimer = new Timer();
        _tickTimer.OneShot = false;
        _tickTimer.WaitTime = PlayIntervalSeconds;
        _tickTimer.Timeout += OnTickTimerTimeout;
        AddChild(_tickTimer);

        _eventPopupTimer = new Timer();
        _eventPopupTimer.OneShot = true;
        _eventPopupTimer.WaitTime = 4.0f;
        _eventPopupTimer.Timeout += OnEventPopupTimeout;
        AddChild(_eventPopupTimer);

        if (_eventPopupPanel != null)
        {
            _eventPopupPanel.Visible = false;
        }

        if (_gameOverPanel != null)
        {
            _gameOverPanel.Visible = false;
        }

        if (_modeOption != null)
        {
            if (_modeOption.ItemCount == 0)
            {
                _modeOption.AddItem("Stability", 0);
                _modeOption.AddItem("Turnout", 1);
                _modeOption.AddItem("Party", 2);
                _modeOption.AddItem("Wealth", 3);
                _modeOption.AddItem("Population", 4);
                _modeOption.AddItem("Ideology", 5);
            }

            _modeOption.ItemSelected += OnModeSelected;
            _modeOption.Select(0);
        }

        if (_simulationManager != null)
        {
            _simulationManager.TurnCompleted += OnTurnCompleted;
            _simulationManager.GameEnded += OnGameEnded;

            if (_simulationManager.IsGameOver)
            {
                OnGameEnded(_simulationManager.GameOverReason, _simulationManager.GameOverDetail);
            }
        }

        if (_heatmapView != null)
        {
            _heatmapView.TileSelected += OnTileSelected;
        }

        if (_tileSizeModeToggle != null && _heatmapView != null)
        {
            _tileSizeModeToggle.SetPressedNoSignal(_heatmapView.IsTileSizeLocked());
        }

        if (_dashboardChart != null)
        {
            _dashboardChart.HoverInfoChanged += OnDashboardHoverInfoChanged;
        }

        SyncPolicyToggleStates();
        SyncBudgetSliders();
        InitializeCandidateCreatorUi();
        RefreshCandidateSummary();
        ApplyHudTooltips();
        RefreshLabels();
        RefreshInspector();
        RefreshDashboard();
    }

    public override void _ExitTree()
    {
        if (_simulationManager != null)
        {
            _simulationManager.TurnCompleted -= OnTurnCompleted;
            _simulationManager.GameEnded -= OnGameEnded;
        }

        if (_tickTimer != null)
        {
            _tickTimer.Timeout -= OnTickTimerTimeout;
        }

        if (_eventPopupTimer != null)
        {
            _eventPopupTimer.Timeout -= OnEventPopupTimeout;
        }

        if (_modeOption != null)
        {
            _modeOption.ItemSelected -= OnModeSelected;
        }

        if (_heatmapView != null)
        {
            _heatmapView.TileSelected -= OnTileSelected;
        }

        if (_dashboardChart != null)
        {
            _dashboardChart.HoverInfoChanged -= OnDashboardHoverInfoChanged;
        }
    }

    public void OnNextTurnPressed()
    {
        if (_simulationManager?.IsGameOver == true)
        {
            return;
        }

        _simulationManager?.RunTurn();
    }

    public void OnPausePressed()
    {
        _tickTimer?.Stop();
    }

    public void OnPlayPressed()
    {
        if (_simulationManager?.IsGameOver == true)
        {
            return;
        }

        if (_tickTimer == null)
        {
            return;
        }

        _tickTimer.WaitTime = Mathf.Max(0.05f, PlayIntervalSeconds);
        if (_tickTimer.IsStopped())
        {
            _tickTimer.Start();
        }
    }

    public void OnFastPressed()
    {
        if (_simulationManager?.IsGameOver == true)
        {
            return;
        }

        if (_tickTimer == null)
        {
            return;
        }

        _tickTimer.WaitTime = Mathf.Max(0.05f, FastIntervalSeconds);
        if (_tickTimer.IsStopped())
        {
            _tickTimer.Start();
        }
    }

    public void OnResetLayoutPressed()
    {
        const string layoutRelativePath = "user://ui_layout.cfg";

        if (FileAccess.FileExists(layoutRelativePath))
        {
            string absolutePath = ProjectSettings.GlobalizePath(layoutRelativePath);
            Error removeResult = DirAccess.RemoveAbsolute(absolutePath);
            GD.Print(removeResult == Error.Ok
                ? "UI layout reset: saved layout file removed."
                : $"UI layout reset: failed to remove saved layout ({removeResult}).");
        }
        else
        {
            GD.Print("UI layout reset: no saved layout file found.");
        }

        GetTree().ReloadCurrentScene();
    }

    public void OnSavePressed()
    {
        if (_simulationManager == null)
        {
            return;
        }

        bool ok = _simulationManager.SaveGame();
        ShowPersistenceStatus(ok, _simulationManager.LastPersistenceStatus);
    }

    public void OnLoadPressed()
    {
        if (_simulationManager == null)
        {
            return;
        }

        bool ok = _simulationManager.LoadGame();
        if (!ok)
        {
            ShowPersistenceStatus(false, _simulationManager.LastPersistenceStatus);
            return;
        }

        if (_tileSizeModeToggle != null && _heatmapView != null)
        {
            _tileSizeModeToggle.SetPressedNoSignal(_heatmapView.IsTileSizeLocked());
        }

        RefreshLabels();
        SyncPolicyToggleStates();
        SyncBudgetSliders();
        RefreshCandidateSummary();
        RefreshInspector();
        RefreshDashboard();
        _heatmapView?.Refresh();
        ShowPersistenceStatus(true, _simulationManager.LastPersistenceStatus);
    }

    private void OnTickTimerTimeout()
    {
        _simulationManager?.RunTurn();
    }

    private void OnModeSelected(long index)
    {
        _heatmapView?.SetMode((int)index);
    }

    public void OnHealthcareToggled(bool pressed)
    {
        TryTogglePolicy("Universal Healthcare", pressed, _healthcareToggle);
    }

    public void OnPolicingToggled(bool pressed)
    {
        TryTogglePolicy("Strict Policing", pressed, _policingToggle);
    }

    public void OnBordersToggled(bool pressed)
    {
        TryTogglePolicy("Open Borders", pressed, _bordersToggle);
    }

    public void OnPropagandaPressed()
    {
        if (_simulationManager == null)
        {
            return;
        }

        bool applied = _simulationManager.TryLaunchPropagandaCampaign();
        if (!applied)
        {
            return;
        }

        RefreshLabels();
    }

    public void OnUnionStrikeDealPressed()
    {
        TryRunFactionAction("union_strike_deal");
    }

    public void OnMilitaryAppeasementPressed()
    {
        TryRunFactionAction("military_appeasement");
    }

    public void OnYouthReformPackagePressed()
    {
        TryRunFactionAction("youth_reform_package");
    }

    public void OnServicesBudgetChanged(double value)
    {
        ApplyBudgetSliders((float)value, null, null);
    }

    public void OnMilitaryBudgetChanged(double value)
    {
        ApplyBudgetSliders(null, (float)value, null);
    }

    public void OnInfrastructureBudgetChanged(double value)
    {
        ApplyBudgetSliders(null, null, (float)value);
    }

    public void OnTileSizeModeToggled(bool pressed)
    {
        _heatmapView?.SetTileSizeLock(pressed);
    }

    private void TryRunFactionAction(string actionId)
    {
        if (_simulationManager == null)
        {
            return;
        }

        bool ok = _simulationManager.TryEnactFactionAction(actionId);

        RefreshLabels();
        RefreshFactionSummary();
        RefreshCandidateSummary();
        RefreshInspector();
        RefreshDashboard();
        _heatmapView?.Refresh();

        if (_eventPopupPanel == null || _eventPopupTitle == null || _eventPopupBody == null)
        {
            return;
        }

        _eventPopupTitle.Text = ok ? "Action Executed" : "Action Blocked";
        _eventPopupBody.Text = string.IsNullOrWhiteSpace(_simulationManager.LastFactionActionStatus)
            ? (ok ? "Faction action executed." : "Faction action could not be executed.")
            : _simulationManager.LastFactionActionStatus;
        _eventPopupPanel.Visible = true;
        _eventPopupTimer?.Start();
    }

    private void OnTurnCompleted()
    {
        RefreshLabels();
        SyncPolicyToggleStates();
        SyncBudgetSliders();
        RefreshCandidateSummary();
        RefreshInspector();
        RefreshDashboard();
        ShowEventPopupIfNeeded();
        _heatmapView?.Refresh();
    }

    private void OnTileSelected(int x, int y)
    {
        _selectedTile = new Vector2I(x, y);
        _hasSelectedTile = true;
        RefreshInspector();
    }

    private void RefreshLabels()
    {
        if (_simulationManager == null)
        {
            return;
        }

        if (_turnLabel != null)
        {
            _turnLabel.Text = $"Turn: {_simulationManager.TurnCounter}";
        }

        if (_stabilityLabel != null)
        {
            _stabilityLabel.Text = $"Stability: {_simulationManager.NationalStability:0.0}";
        }

        if (_turnoutLabel != null)
        {
            _turnoutLabel.Text = $"Turnout: {_simulationManager.NationalTurnout:P0}";
        }

        if (_eventLabel != null)
        {
            _eventLabel.Text = $"Event: {_simulationManager.LastEventTitle}";
        }

        if (_governmentLabel != null)
        {
            _governmentLabel.Text = $"Government: {_simulationManager.CurrentGovernmentParty}";
        }

        if (_treasuryLabel != null)
        {
            _treasuryLabel.Text = $"Treasury: {_simulationManager.Treasury:0}";
        }

        if (_capitalLabel != null)
        {
            string propagandaSuffix = _simulationManager.PropagandaTurnsRemaining > 0
                ? $" | Media+ {_simulationManager.PropagandaTurnsRemaining}t"
                : string.Empty;
            _capitalLabel.Text = $"Capital: {_simulationManager.PoliticalCapital:0.0}{propagandaSuffix}";
        }

        if (_projectedPollsLabel != null)
        {
            _projectedPollsLabel.Text = BuildProjectedPollsText();
        }

        RefreshFactionSummary();
        RefreshFactionActionButtons();
        RefreshTurnDigestAndHistory();

        RefreshPolicyRampReadout();
    }

    private void RefreshTurnDigestAndHistory()
    {
        if (_simulationManager == null)
        {
            return;
        }

        if (_turnDigestLabel != null)
        {
            _turnDigestLabel.Text = string.IsNullOrWhiteSpace(_simulationManager.LastTurnDigest)
                ? "Turn digest: awaiting first completed turn."
                : _simulationManager.LastTurnDigest;
        }

        if (_actionHistoryLabel != null)
        {
            IReadOnlyList<string> history = _simulationManager.RecentActionHistory;
            if (history == null || history.Count == 0)
            {
                _actionHistoryLabel.Text = "No recent actions.";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = history.Count - 1; i >= 0; i--)
                {
                    sb.Append("• ");
                    sb.AppendLine(history[i]);
                }

                _actionHistoryLabel.Text = sb.ToString();
            }
        }
    }

    private void RefreshFactionActionButtons()
    {
        if (_simulationManager == null)
        {
            return;
        }

        ApplyFactionActionButtonState(_unionStrikeDealButton, "Union Strike Deal", "union_strike_deal");
        ApplyFactionActionButtonState(_militaryAppeasementButton, "Military Appeasement", "military_appeasement");
        ApplyFactionActionButtonState(_youthReformButton, "Youth Reform Package", "youth_reform_package");
    }

    private void ApplyFactionActionButtonState(Button button, string baseLabel, string actionId)
    {
        if (button == null)
        {
            return;
        }

        int turns = _simulationManager.GetFactionActionCooldownRemaining(actionId);
        button.Text = turns > 0 ? $"{baseLabel} ({turns}t)" : baseLabel;
        button.Disabled = turns > 0;
        button.TooltipText = BuildFactionActionTooltip(actionId, turns);
    }

    private string BuildFactionActionTooltip(string actionId, int cooldownTurns)
    {
        string details;
        switch (actionId)
        {
            case "union_strike_deal":
                details =
                    "Cost: Treasury 260, Capital 5\n" +
                    "Effects: Labor +18, Youth +6, Industrial -9, Clergy -2\n" +
                    "National: Stability +2.5, Turnout +1%";
                break;
            case "military_appeasement":
                details =
                    "Cost: Treasury 220, Capital 6\n" +
                    "Effects: Military +20, Clergy +8, Youth -10, Labor -5\n" +
                    "National: Stability +2.0, Military Budget +8%";
                break;
            case "youth_reform_package":
                details =
                    "Cost: Treasury 240, Capital 7\n" +
                    "Effects: Youth +20, Labor +6, Clergy -7, Industrial -5\n" +
                    "National: Stability +1.5, Turnout +2%, Services Budget +6%";
                break;
            default:
                details = "Faction action details unavailable.";
                break;
        }

        if (_simulationManager == null)
        {
            return details;
        }

        if (cooldownTurns > 0)
        {
            return details + $"\nStatus: Blocked (cooldown {cooldownTurns} turns remaining).";
        }

        if (_simulationManager.IsGameOver)
        {
            return details + "\nStatus: Blocked (game over).";
        }

        if (_simulationManager.LockPolicyWhenOutOfPower && !_simulationManager.PlayerInPower)
        {
            return details + "\nStatus: Blocked (you are currently out of power).";
        }

        return details + "\nStatus: Ready.";
    }

    private void RefreshFactionSummary()
    {
        if (_factionSummaryLabel == null || _simulationManager == null)
        {
            return;
        }

        IReadOnlyList<FactionState> factions = _simulationManager.Factions;
        if (factions == null || factions.Count == 0)
        {
            _factionSummaryLabel.Text = "Factions: N/A";
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < factions.Count; i++)
        {
            FactionState faction = factions[i];
            sb.AppendLine($"• {faction.Name} {FormatFactionApproval(faction.Approval)}");
        }

        if (!string.IsNullOrWhiteSpace(_simulationManager.TopFactionDemand))
        {
            sb.AppendLine();
            sb.Append($"[color=#E3B341]Top Demand:[/color] {_simulationManager.TopFactionDemand}");
        }

        _factionSummaryLabel.Text = sb.ToString();
    }

    private static string FormatFactionApproval(float approval)
    {
        string color;
        if (approval >= 65f)
        {
            color = "#6EE787";
        }
        else if (approval >= 40f)
        {
            color = "#E3B341";
        }
        else
        {
            color = "#F85149";
        }

        return $"[color={color}]{approval:0}%[/color]";
    }

    private void SyncPolicyToggleStates()
    {
        if (_simulationManager == null)
        {
            return;
        }

        SetToggleSilently(_healthcareToggle, _simulationManager.PolicyManager.IsActive("Universal Healthcare"));
        SetToggleSilently(_policingToggle, _simulationManager.PolicyManager.IsActive("Strict Policing"));
        SetToggleSilently(_bordersToggle, _simulationManager.PolicyManager.IsActive("Open Borders"));
    }

    private void SyncBudgetSliders()
    {
        if (_simulationManager == null)
        {
            return;
        }

        SetSliderSilently(_servicesBudgetSlider, _simulationManager.BudgetServices * 100f);
        SetSliderSilently(_militaryBudgetSlider, _simulationManager.BudgetMilitary * 100f);
        SetSliderSilently(_infrastructureBudgetSlider, _simulationManager.BudgetInfrastructure * 100f);
    }

    private static void SetToggleSilently(CheckButton toggle, bool pressed)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.SetPressedNoSignal(pressed);
    }

    private static void SetSliderSilently(HSlider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueNoSignal(value);
    }

    private void ApplyBudgetSliders(float? services, float? military, float? infrastructure)
    {
        if (_simulationManager == null)
        {
            return;
        }

        float nextServices = (services ?? (float)(_servicesBudgetSlider?.Value ?? (_simulationManager.BudgetServices * 100f))) / 100f;
        float nextMilitary = (military ?? (float)(_militaryBudgetSlider?.Value ?? (_simulationManager.BudgetMilitary * 100f))) / 100f;
        float nextInfrastructure = (infrastructure ?? (float)(_infrastructureBudgetSlider?.Value ?? (_simulationManager.BudgetInfrastructure * 100f))) / 100f;

        _simulationManager.SetBudgetAllocations(nextServices, nextMilitary, nextInfrastructure);
        RefreshLabels();
    }

    private void InitializeCandidateCreatorUi()
    {
        if (_simulationManager == null)
        {
            return;
        }

        IReadOnlyList<CandidateBackgroundDefinition> backgrounds = _simulationManager.GetAvailableCandidateBackgrounds();
        IReadOnlyList<CandidateTraitDefinition> traits = _simulationManager.GetAvailableCandidateTraits();

        _candidateBackgroundIds.Clear();
        _candidateTraitIds.Clear();
        _candidateBackgroundById.Clear();
        _candidateTraitById.Clear();

        if (_candidateBackgroundOption != null)
        {
            _candidateBackgroundOption.Clear();
            for (int i = 0; i < backgrounds.Count; i++)
            {
                CandidateBackgroundDefinition background = backgrounds[i];
                _candidateBackgroundIds.Add(background.Id);
                _candidateBackgroundById[background.Id] = background;
                _candidateBackgroundOption.AddItem(BuildBackgroundOptionLabel(background), i);
                _candidateBackgroundOption.SetItemTooltip(i, BuildBackgroundOptionTooltip(background));
            }
        }

        if (_candidateTraitPrimaryOption != null)
        {
            _candidateTraitPrimaryOption.Clear();
            _candidateTraitPrimaryOption.AddItem("None", 0);
        }

        if (_candidateTraitSecondaryOption != null)
        {
            _candidateTraitSecondaryOption.Clear();
            _candidateTraitSecondaryOption.AddItem("None", 0);
        }

        for (int i = 0; i < traits.Count; i++)
        {
            CandidateTraitDefinition trait = traits[i];
            _candidateTraitIds.Add(trait.Id);
            _candidateTraitById[trait.Id] = trait;
            string label = BuildTraitOptionLabel(trait);
            _candidateTraitPrimaryOption?.AddItem(label, i + 1);
            _candidateTraitSecondaryOption?.AddItem(label, i + 1);
            string tooltip = BuildTraitOptionTooltip(trait);
            _candidateTraitPrimaryOption?.SetItemTooltip(i + 1, tooltip);
            _candidateTraitSecondaryOption?.SetItemTooltip(i + 1, tooltip);
        }

        CandidateProfile profile = _simulationManager.PlayerCandidate;
        if (profile != null)
        {
            if (_candidateNameInput != null)
            {
                _candidateNameInput.Text = profile.Name;
            }

            SelectCandidateOptionById(_candidateBackgroundOption, _candidateBackgroundIds, profile.BackgroundId, false);

            string primary = profile.TraitIds.Count > 0 ? profile.TraitIds[0] : string.Empty;
            string secondary = profile.TraitIds.Count > 1 ? profile.TraitIds[1] : string.Empty;
            SelectCandidateOptionById(_candidateTraitPrimaryOption, _candidateTraitIds, primary, true);
            SelectCandidateOptionById(_candidateTraitSecondaryOption, _candidateTraitIds, secondary, true);
        }
    }

    public void OnCandidateApplyPressed()
    {
        if (_simulationManager == null)
        {
            return;
        }

        string name = _candidateNameInput?.Text ?? string.Empty;
        string backgroundId = GetSelectedCandidateId(_candidateBackgroundOption, _candidateBackgroundIds, false);
        string traitA = GetSelectedCandidateId(_candidateTraitPrimaryOption, _candidateTraitIds, true);
        string traitB = GetSelectedCandidateId(_candidateTraitSecondaryOption, _candidateTraitIds, true);

        List<string> traits = new List<string>();
        if (!string.IsNullOrWhiteSpace(traitA))
        {
            traits.Add(traitA);
        }

        if (!string.IsNullOrWhiteSpace(traitB) && !string.Equals(traitA, traitB, StringComparison.OrdinalIgnoreCase))
        {
            traits.Add(traitB);
        }

        _simulationManager.SetPlayerCandidate(name, backgroundId, traits);
        RefreshCandidateSummary();
    }

    public void OnCandidateNameChanged(string value)
    {
        RefreshCandidateSummary();
    }

    public void OnCandidateBackgroundSelected(long index)
    {
        RefreshCandidateSummary();
    }

    public void OnCandidateTraitPrimarySelected(long index)
    {
        RefreshCandidateSummary();
    }

    public void OnCandidateTraitSecondarySelected(long index)
    {
        RefreshCandidateSummary();
    }

    private void RefreshCandidateSummary()
    {
        if (_candidateSummaryLabel == null || _simulationManager == null)
        {
            return;
        }

        CandidateProfile profile = BuildCandidatePreviewProfile() ?? _simulationManager.PlayerCandidate;
        if (profile == null)
        {
            _candidateSummaryLabel.Text = "Candidate: N/A";
            return;
        }

        _candidateSummaryLabel.Text =
            $"[b]Candidate: {profile.Name}[/b]\n" +
            $"Stats C{profile.Charisma * 100f:0} Co{profile.Competence * 100f:0} I{profile.Integrity * 100f:0} P{profile.Populism * 100f:0}\n" +
            BuildColoredModifierLine("Policy Cost", profile.PolicyCostMultiplier, invertSign: true) + "\n" +
            BuildColoredModifierLine("Campaign", profile.CampaignStrengthMultiplier) + "\n" +
            BuildColoredModifierLine("Capital", profile.CapitalGainMultiplier);
    }

    private CandidateProfile BuildCandidatePreviewProfile()
    {
        if (_simulationManager == null)
        {
            return null;
        }

        string name = _candidateNameInput?.Text ?? string.Empty;
        string backgroundId = GetSelectedCandidateId(_candidateBackgroundOption, _candidateBackgroundIds, false);
        string traitA = GetSelectedCandidateId(_candidateTraitPrimaryOption, _candidateTraitIds, true);
        string traitB = GetSelectedCandidateId(_candidateTraitSecondaryOption, _candidateTraitIds, true);

        List<string> traits = new List<string>();
        if (!string.IsNullOrWhiteSpace(traitA))
        {
            traits.Add(traitA);
        }

        if (!string.IsNullOrWhiteSpace(traitB) && !string.Equals(traitA, traitB, StringComparison.OrdinalIgnoreCase))
        {
            traits.Add(traitB);
        }

        return CandidateProfile.Build(name, backgroundId, traits, _candidateBackgroundById, _candidateTraitById);
    }

    private static void SelectCandidateOptionById(OptionButton option, List<string> ids, string id, bool hasNoneItem)
    {
        if (option == null)
        {
            return;
        }

        int index = ids.FindIndex(item => string.Equals(item, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            option.Select(0);
            return;
        }

        option.Select(hasNoneItem ? index + 1 : index);
    }

    private static string GetSelectedCandidateId(OptionButton option, List<string> ids, bool hasNoneItem)
    {
        if (option == null)
        {
            return string.Empty;
        }

        int selected = option.Selected;
        if (selected < 0)
        {
            return string.Empty;
        }

        if (hasNoneItem)
        {
            if (selected == 0)
            {
                return string.Empty;
            }

            selected -= 1;
        }

        if (selected < 0 || selected >= ids.Count)
        {
            return string.Empty;
        }

        return ids[selected];
    }

    private static string BuildBackgroundOptionLabel(CandidateBackgroundDefinition background)
    {
        if (background == null)
        {
            return "Unknown";
        }

        string shortDesc = background.Description ?? string.Empty;
        if (shortDesc.Length > 48)
        {
            shortDesc = shortDesc.Substring(0, 48).TrimEnd() + "...";
        }

        return string.IsNullOrWhiteSpace(shortDesc)
            ? background.Name
            : $"{background.Name} — {shortDesc}";
    }

    private static string BuildTraitOptionLabel(CandidateTraitDefinition trait)
    {
        if (trait == null)
        {
            return "Unknown";
        }

        string shortDesc = trait.Description ?? string.Empty;
        if (shortDesc.Length > 44)
        {
            shortDesc = shortDesc.Substring(0, 44).TrimEnd() + "...";
        }

        return string.IsNullOrWhiteSpace(shortDesc)
            ? trait.Name
            : $"{trait.Name} — {shortDesc}";
    }

    private static string BuildBackgroundOptionTooltip(CandidateBackgroundDefinition background)
    {
        if (background == null)
        {
            return string.Empty;
        }

        return $"{background.Name}\n{background.Description}\n" +
            $"Policy Cost {FormatDeltaFromMultiplier(background.PolicyCostMultiplierDelta, invertSign: true)} | " +
            $"Campaign {FormatDeltaFromMultiplier(background.CampaignStrengthMultiplierDelta)} | " +
            $"Capital {FormatDeltaFromMultiplier(background.CapitalGainMultiplierDelta)}";
    }

    private static string BuildTraitOptionTooltip(CandidateTraitDefinition trait)
    {
        if (trait == null)
        {
            return string.Empty;
        }

        return $"{trait.Name}\n{trait.Description}\n" +
            $"Policy Cost {FormatDeltaFromMultiplier(trait.PolicyCostMultiplierDelta, invertSign: true)} | " +
            $"Campaign {FormatDeltaFromMultiplier(trait.CampaignStrengthMultiplierDelta)} | " +
            $"Capital {FormatDeltaFromMultiplier(trait.CapitalGainMultiplierDelta)}";
    }

    private static string FormatMultiplierPercent(float multiplier, bool invertSign = false)
    {
        float delta = (multiplier - 1f) * 100f;
        if (invertSign)
        {
            delta *= -1f;
        }

        return $"{delta:+0;-0;0}%";
    }

    private static string BuildColoredModifierLine(string label, float multiplier, bool invertSign = false)
    {
        float delta = (multiplier - 1f) * 100f;
        if (invertSign)
        {
            delta *= -1f;
        }

        string color;
        if (delta > 0.001f)
        {
            color = "#6EE787";
        }
        else if (delta < -0.001f)
        {
            color = "#F85149";
        }
        else
        {
            color = "#9DA7B3";
        }

        return $"[color={color}]{label} {delta:+0;-0;0}%[/color]";
    }

    private static string FormatDeltaFromMultiplier(float multiplierDelta, bool invertSign = false)
    {
        float delta = multiplierDelta * 100f;
        if (invertSign)
        {
            delta *= -1f;
        }

        return $"{delta:+0;-0;0}%";
    }

    private void TryTogglePolicy(string policyName, bool active, CheckButton source)
    {
        if (_simulationManager == null)
        {
            return;
        }

        bool applied = _simulationManager.TrySetPolicyActive(policyName, active);
        if (!applied)
        {
            SetToggleSilently(source, _simulationManager.PolicyManager.IsActive(policyName));
        }

        RefreshLabels();
    }

    private void RefreshPolicyRampReadout()
    {
        if (_policyRampLabel == null || _simulationManager == null)
        {
            return;
        }

        _policyRampLabel.Text =
            BuildRampLine("Healthcare", "Universal Healthcare") + "\n" +
            BuildRampLine("Policing", "Strict Policing") + "\n" +
            BuildRampLine("Borders", "Open Borders");
    }

    private string BuildRampLine(string shortName, string policyName)
    {
        float strength = _simulationManager.PolicyManager.GetPolicyStrength(policyName);
        bool targetActive = _simulationManager.PolicyManager.IsActive(policyName);
        float pct = strength * 100f;

        string colorHex;
        if (targetActive)
        {
            colorHex = "#6EE787";
        }
        else if (strength > 0.001f)
        {
            colorHex = "#E3B341";
        }
        else
        {
            colorHex = "#9DA7B3";
        }

        string trend = GetTrendGlyph(targetActive, strength);
        return $"[color={colorHex}]{shortName} {trend} {pct:0}%[/color]";
    }

    private static string GetTrendGlyph(bool targetActive, float strength)
    {
        if (targetActive)
        {
            return strength >= 0.995f ? "•" : "↑";
        }

        return strength <= 0.005f ? "•" : "↓";
    }

    private void RefreshInspector()
    {
        if (_inspectorTitle == null || _inspectorSummary == null || _inspectorBlocs == null)
        {
            return;
        }

        if (_simulationManager == null || !_hasSelectedTile)
        {
            _inspectorTitle.Text = "Tile Inspector";
            _inspectorSummary.Text = "Click a tile on the grid to inspect local demographics and sentiment.";
            _inspectorBlocs.Text = string.Empty;
            return;
        }

        if (_selectedTile.X < 0 || _selectedTile.Y < 0 ||
            _selectedTile.X >= _simulationManager.GridSize.X || _selectedTile.Y >= _simulationManager.GridSize.Y)
        {
            _inspectorTitle.Text = "Tile Inspector";
            _inspectorSummary.Text = "Selected tile is out of bounds.";
            _inspectorBlocs.Text = string.Empty;
            return;
        }

        DistrictTile tile = _simulationManager.Grid[_selectedTile.X, _selectedTile.Y];

        _inspectorTitle.Text = $"Tile [{_selectedTile.X},{_selectedTile.Y}]";
        _inspectorSummary.Text =
            $"Population: {tile.TotalPopulation}  •  {tile.Density}  •  {tile.DominantIndustry}\n" +
            $"Stability: {tile.Stability:0.0}   Turnout: {tile.Turnout:P0}   Issue: {tile.LocalIssue}\n" +
            $"Shocks → Event {_simulationManager.CurrentStabilityShock:+0.0;-0.0;0.0}, Neighbor {_selectedTilePressure(tile):+0.0;-0.0;0.0}";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < tile.Blocs.Count; i++)
        {
            VoterBloc bloc = tile.Blocs[i];
            sb.Append($"• {bloc.Name} ({bloc.Category})\n");
            sb.Append($"  Pop {bloc.Population}   Happy {bloc.Happiness:P0}   Turnout {bloc.TurnoutChance:P0}\n");
            sb.AppendLine();
            sb.Append("  Why: ");
            sb.Append($"Ideology {FormatPenaltyToken(-bloc.LastIdeologyHappinessPenalty)}  ");
            sb.Append($"Issue {FormatPenaltyToken(-bloc.LastIssueHappinessPenalty)}  ");
            sb.Append($"PolicyH {FormatSignedToken(bloc.LastPolicyHappinessModifier)}  ");
            sb.Append($"PolicyT {FormatSignedToken(bloc.LastPolicyTurnoutModifier)}");
            sb.AppendLine();
            string happinessBreakdown = BuildPolicyContributionBreakdown(bloc.Category, false);
            string turnoutBreakdown = BuildPolicyContributionBreakdown(bloc.Category, true);
            if (!string.IsNullOrWhiteSpace(happinessBreakdown))
            {
                sb.Append($"  PolicyH by law: {happinessBreakdown}\n");
            }

            if (!string.IsNullOrWhiteSpace(turnoutBreakdown))
            {
                sb.Append($"  PolicyT by law: {turnoutBreakdown}\n");
            }

            sb.AppendLine();
        }

        _inspectorBlocs.Text = sb.ToString();
    }

    private static string FormatPenaltyToken(float value)
    {
        return FormatSignedToken(value);
    }

    private static string FormatSignedToken(float value)
    {
        string color;
        if (value > 0.0001f)
        {
            color = "#6EE787";
        }
        else if (value < -0.0001f)
        {
            color = "#F85149";
        }
        else
        {
            color = "#9DA7B3";
        }

        return $"[color={color}]{value * 100f:+0;-0;0}%[/color]";
    }

    private static float _selectedTilePressure(DistrictTile tile)
    {
        return tile.ExternalStabilityPressure;
    }

    private void RefreshDashboard()
    {
        if (_dashboardChart == null || _simulationManager == null)
        {
            return;
        }

        _dashboardChart.SetSeries(
            _simulationManager.StabilityHistory,
            _simulationManager.TurnoutHistory,
            _simulationManager.TreasuryHistory,
            _simulationManager.CapitalHistory,
            _simulationManager.TurnCounter);

        if (_dashboardHoverLabel != null && string.IsNullOrWhiteSpace(_dashboardHoverLabel.Text))
        {
            _dashboardHoverLabel.Text = "Hover chart for turn values";
        }
    }

    private string BuildProjectedPollsText()
    {
        if (_simulationManager == null || _simulationManager.ProjectedPolls.Count == 0)
        {
            return "Projected Polls: N/A";
        }

        List<KeyValuePair<string, float>> items = new List<KeyValuePair<string, float>>(_simulationManager.ProjectedPolls);
        items.Sort((a, b) => b.Value.CompareTo(a.Value));

        int take = Mathf.Min(3, items.Count);
        StringBuilder sb = new StringBuilder();
        sb.Append($"Projected Polls (±{_simulationManager.PollMarginOfError:P0}): ");

        for (int i = 0; i < take; i++)
        {
            if (i > 0)
            {
                sb.Append(" | ");
            }

            sb.Append($"{items[i].Key} {items[i].Value:P0}");
        }

        return sb.ToString();
    }

    private void ApplyHudTooltips()
    {
        if (_stabilityLabel != null)
        {
            _stabilityLabel.TooltipText = "National stability (0-100). Lower values increase unrest and crisis risk.";
        }

        if (_turnoutLabel != null)
        {
            _turnoutLabel.TooltipText = "Expected turnout this turn. Higher values amplify election swings.";
        }

        if (_capitalLabel != null)
        {
            _capitalLabel.TooltipText = "Political capital used to activate major policies.";
        }

        if (_treasuryLabel != null)
        {
            _treasuryLabel.TooltipText = "National treasury balance. Sustained negative values cause debt stress penalties.";
        }

        if (_projectedPollsLabel != null)
        {
            _projectedPollsLabel.TooltipText = "Fog-of-war polling estimate with margin of error. Actual election result may differ.";
        }

        if (_factionSummaryLabel != null)
        {
            _factionSummaryLabel.TooltipText = "Faction approval and current pressure point. Low approval can trigger unrest penalties.";
        }

        if (_turnDigestLabel != null)
        {
            _turnDigestLabel.TooltipText = "End-turn summary of key metric changes and dominant political pressure.";
        }

        if (_actionHistoryLabel != null)
        {
            _actionHistoryLabel.TooltipText = "Recent strategic actions and interventions, newest first.";
        }
    }

    private void OnDashboardHoverInfoChanged(string text)
    {
        if (_dashboardHoverLabel == null)
        {
            return;
        }

        _dashboardHoverLabel.Text = text;
    }

    private void ShowEventPopupIfNeeded()
    {
        if (_simulationManager == null || _eventPopupPanel == null || _eventPopupTitle == null || _eventPopupBody == null)
        {
            return;
        }

        if (_simulationManager.LastEventTitle == "None")
        {
            return;
        }

        if (_lastShownEventTurn == _simulationManager.TurnCounter)
        {
            return;
        }

        _lastShownEventTurn = _simulationManager.TurnCounter;
        _eventPopupTitle.Text = _simulationManager.LastEventTitle;
        _eventPopupBody.Text = _simulationManager.LastEventDescription;
        _eventPopupPanel.Visible = true;
        _eventPopupTimer?.Start();
    }

    private void OnEventPopupTimeout()
    {
        if (_eventPopupPanel != null)
        {
            _eventPopupPanel.Visible = false;
        }
    }

    private void ShowPersistenceStatus(bool success, string detail)
    {
        if (_eventPopupPanel == null || _eventPopupTitle == null || _eventPopupBody == null)
        {
            return;
        }

        _eventPopupTitle.Text = success ? "Saved" : "Persistence Error";
        _eventPopupBody.Text = string.IsNullOrWhiteSpace(detail)
            ? (success ? "Operation completed." : "Operation failed.")
            : detail;
        _eventPopupPanel.Visible = true;
        _eventPopupTimer?.Start();
    }

    private void OnGameEnded(string reason, string detail)
    {
        _tickTimer?.Stop();

        if (_gameOverPanel == null)
        {
            return;
        }

        if (_gameOverTitle != null)
        {
            _gameOverTitle.Text = reason;
        }

        if (_gameOverBody != null)
        {
            _gameOverBody.Text = detail;
        }

        _gameOverPanel.Visible = true;
    }

    private string BuildPolicyContributionBreakdown(BlocCategory category, bool turnout)
    {
        if (_simulationManager == null)
        {
            return string.Empty;
        }

        Dictionary<string, float> contributions = _simulationManager.PolicyManager.GetPolicyContributionsForBloc(category, turnout);
        if (contributions.Count == 0)
        {
            return string.Empty;
        }

        List<KeyValuePair<string, float>> items = new List<KeyValuePair<string, float>>(contributions);
        items.Sort((a, b) => Mathf.Abs(b.Value).CompareTo(Mathf.Abs(a.Value)));

        StringBuilder sb = new StringBuilder();
        int shown = 0;
        for (int i = 0; i < items.Count && shown < 3; i++)
        {
            if (shown > 0)
            {
                sb.Append(" | ");
            }

            sb.Append(items[i].Key);
            sb.Append(' ');
            sb.Append(FormatSignedToken(items[i].Value));
            shown++;
        }

        return sb.ToString();
    }

    private SimulationManager ResolveSimulationManager()
    {
        if (SimulationManagerPath != null && !SimulationManagerPath.IsEmpty)
        {
            SimulationManager fromPath = GetNodeOrNull<SimulationManager>(SimulationManagerPath);
            if (fromPath == null)
            {
                GD.PushWarning($"HudController wiring: '{nameof(SimulationManagerPath)}' path not found ({SimulationManagerPath}).");
            }

            return fromPath;
        }

        var managers = GetTree().GetNodesInGroup("simulation_manager");
        if (managers.Count > 0)
        {
            return managers[0] as SimulationManager;
        }

        GD.PushWarning($"HudController wiring: '{nameof(SimulationManagerPath)}' unresolved (no export path and no simulation_manager group node).");
        return null;
    }

    private T ResolveNodeOrWarn<T>(NodePath path, string fieldName) where T : Node
    {
        if (path == null || path.IsEmpty)
        {
            GD.PushWarning($"HudController wiring: '{fieldName}' is empty.");
            return null;
        }

        T node = GetNodeOrNull<T>(path);
        if (node == null)
        {
            GD.PushWarning($"HudController wiring: '{fieldName}' path not found ({path}).");
        }

        return node;
    }
}
