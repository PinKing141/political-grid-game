using Godot;
using PolisGrid.Core;
using System;
using System.Collections.Generic;

public partial class SimulationManager : Node
{
    [Signal]
    public delegate void TurnCompletedEventHandler();
    [Signal]
    public delegate void GameEndedEventHandler(string reason, string detail);

    [Export] public Vector2I GridSize = new Vector2I(30, 30);
    [Export] public int Seed = 1337;
    [Export] public int ElectionIntervalTicks = 48;
    [Export] public int HistoryWindowSize = 50;
    [Export] public int MinPolicyRampTurns = 3;
    [Export] public int MaxPolicyRampTurns = 6;
    [Export] public int ParliamentSeats = 120;
    [Export] public float EventChancePerTick = 0.33f;
    [Export] public float PartyDriftPerTick = 0.035f;
    [Export] public float PartySpawnDistanceThreshold = 0.58f;
    [Export(PropertyHint.Range, "0,1,0.001")] public float GlobalMediaPower = 0.05f;
    [Export(PropertyHint.Range, "0,1,0.001")] public float GovernmentMediaTargetBias = 0.35f;
    [Export(PropertyHint.Range, "0,1,0.001")] public float PropagandaMediaBoost = 0.18f;
    [Export] public int PropagandaDurationTurns = 6;
    [Export] public float PropagandaCapitalCost = 8f;
    [Export(PropertyHint.Range, "0.5,2.0,0.05")] public float FactionActionCostMultiplier = 1.0f;
    [Export(PropertyHint.Range, "0.5,2.0,0.05")] public float FactionActionApprovalMultiplier = 1.0f;
    [Export] public int FactionActionCooldownTurns = 6;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BudgetServices = 0.5f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BudgetMilitary = 0.5f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BudgetInfrastructure = 0.5f;
    [Export] public string PlayerPartyName = "SocDem";
    [Export] public bool AutoStart = true;
    [Export] public string DefaultCandidateName = "Alex Mercer";
    [Export] public string DefaultCandidateBackgroundId = "activist";
    [Export] public string DefaultCandidateTraitIds = "grassroots,policy_wonk";
    [Export] public bool EndGameOnElectionLoss = true;
    [Export] public bool LockPolicyWhenOutOfPower = true;
    [Export] public float RevolutionUnrestThreshold = 90f;
    [Export] public int RevolutionUnrestTurns = 4;
    [Export] public float BankruptcyTreasuryThreshold = -5000f;
    [Export(PropertyHint.Range, "0,1,0.001")] public float NeighborIdeologyDiffusionRate = 0.010f;
    [Export(PropertyHint.Range, "0,20,0.1")] public float NeighborStabilitySpilloverStrength = 3.0f;

    public DistrictTile[,] Grid { get; private set; } = new DistrictTile[0, 0];
    public PolicyManager PolicyManager { get; } = new PolicyManager();
    public PoliticalCompass GovernmentPolicy = new PoliticalCompass(0f, 0f, 0f, 0f);
    public int TurnCounter { get; private set; }
    public float NationalStability { get; private set; } = 50f;
    public float NationalUnrest { get; private set; } = 50f;
    public float NationalTurnout { get; private set; } = 0.5f;
    public float Treasury { get; private set; } = 0f;
    public float PoliticalCapital { get; private set; } = 25f;
    public float CurrentStabilityShock => _stabilityShock;
    public string LastEventTitle { get; private set; } = "None";
    public string LastEventDescription { get; private set; } = "No event this turn.";
    public string CurrentGovernmentParty { get; private set; } = "Caretaker Coalition";
    public bool IsGameOver { get; private set; }
    public string GameOverReason { get; private set; } = string.Empty;
    public string GameOverDetail { get; private set; } = string.Empty;
    public bool PlayerInPower { get; private set; } = true;
    public IReadOnlyDictionary<string, int> ActiveWorldTags => _worldTags.ActiveTags;
    public int PropagandaTurnsRemaining => _propagandaTurnsRemaining;
    public float EffectiveMediaPower => Mathf.Clamp(GlobalMediaPower + (_propagandaTurnsRemaining > 0 ? PropagandaMediaBoost : 0f), 0f, 1f);
    public string LastPersistenceStatus { get; private set; } = string.Empty;
    public bool LastPersistenceFailed { get; private set; }
    public string LastFactionActionStatus { get; private set; } = string.Empty;
    public string LastTurnDigest { get; private set; } = string.Empty;
    public CandidateProfile PlayerCandidate { get; private set; } = new CandidateProfile();
    public bool HasStarted { get; private set; }

    public int TicksUntilElection
    {
        get
        {
            if (ElectionIntervalTicks <= 0)
            {
                return 0;
            }

            int mod = TurnCounter % ElectionIntervalTicks;
            return mod == 0 ? ElectionIntervalTicks : ElectionIntervalTicks - mod;
        }
    }

    public IReadOnlyList<Party> Parties => _parties;
    public IReadOnlyDictionary<string, float> Polls => _lastPolls;
    public IReadOnlyDictionary<string, float> ProjectedPolls => _projectedPolls;
    public float PollMarginOfError { get; private set; } = 0.05f;
    public IReadOnlyDictionary<string, int> LastElectionSeats => _lastSeats;
    public IReadOnlyCollection<string> ActivePolicies => PolicyManager.ActivePolicyNames;
    public IReadOnlyList<FactionState> Factions => _factions;
    public IReadOnlyList<string> RecentActionHistory => _recentActionHistory;
    public string TopFactionDemand { get; private set; } = string.Empty;
    public IReadOnlyList<float> StabilityHistory => _stabilityHistory;
    public IReadOnlyList<float> TurnoutHistory => _turnoutHistory;
    public IReadOnlyList<float> TreasuryHistory => _treasuryHistory;
    public IReadOnlyList<float> CapitalHistory => _capitalHistory;

    private readonly List<Party> _parties = new List<Party>();
    private readonly List<SimulationEvent> _eventDeck = new List<SimulationEvent>();
    private readonly Dictionary<string, float> _lastPolls = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _projectedPolls = new Dictionary<string, float>();
    private readonly Dictionary<string, int> _lastSeats = new Dictionary<string, int>();
    private readonly List<float> _stabilityHistory = new List<float>();
    private readonly List<float> _turnoutHistory = new List<float>();
    private readonly List<float> _treasuryHistory = new List<float>();
    private readonly List<float> _capitalHistory = new List<float>();
    private readonly List<FactionState> _factions = new List<FactionState>();
    private readonly List<string> _recentActionHistory = new List<string>();
    private int _factionCrisisStreak;

    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private FastNoiseLite _densityNoise = new FastNoiseLite();
    private FastNoiseLite _industryNoise = new FastNoiseLite();
    private PoliticalCompass _nationalVoterCenter = new PoliticalCompass(0f, 0f, 0f, 0f);
    private float _stabilityShock;
    private int _highUnrestTurnStreak;
    private readonly WorldTagManager _worldTags = new WorldTagManager();
    private int _propagandaTurnsRemaining;
    private readonly Dictionary<string, CandidateTraitDefinition> _candidateTraits = new Dictionary<string, CandidateTraitDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CandidateBackgroundDefinition> _candidateBackgrounds = new Dictionary<string, CandidateBackgroundDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _factionActionCooldowns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public override void _Ready()
    {
        if (GridSize.X <= 0 || GridSize.Y <= 0)
        {
            GD.PushError("GridSize must be positive.");
            return;
        }

        AddToGroup("simulation_manager");

        InitializeRandom();
        BuildEventDeck();
        SeedInitialParties();
        SeedInitialFactions();
        SeedDefaultPolicies();
        InitializeCandidateData();
        InitializeGrid();

        if (SessionLaunchContext.TryConsumeLoadCampaignPath(out string pendingLoadPath))
        {
            LoadGame(string.IsNullOrWhiteSpace(pendingLoadPath) ? "user://savegame.json" : pendingLoadPath);
            return;
        }

        if (AutoStart)
        {
            BeginGame();
        }
    }

    public void BeginGame()
    {
        if (HasStarted)
        {
            return;
        }

        HasStarted = true;
        RunTurn();
    }

    public void RunTurn()
    {
        if (IsGameOver)
        {
            return;
        }

        if (!HasStarted)
        {
            return;
        }

        if (Grid.Length == 0)
        {
            GD.PushWarning("RunTurn called before grid initialization.");
            return;
        }

        float prevStability = NationalStability;
        float prevTurnout = NationalTurnout;
        float prevTreasury = Treasury;
        float prevCapital = PoliticalCapital;

        TurnCounter++;
        _worldTags.AdvanceTurn();
        SyncPolicyStateTags();
        AdvancePolicyRamps();
        ResolveEventCard();
        ApplyNeighborDiffusion();
        SimulateTiles();
        ApplyPolicyTickEffects();
        UpdateFactionApproval();
        UpdatePartyDrift();
        UpdatePolls();
        TrySpawnNewParty();

        if (ElectionIntervalTicks > 0 && TurnCounter % ElectionIntervalTicks == 0)
        {
            ResolveElection();
        }

        GD.Print(
            $"Turn {TurnCounter} | Stability {NationalStability:0.0} | Unrest {NationalUnrest:0.0} | Turnout {NationalTurnout:P1} | Event: {LastEventTitle}");

        RecordHistoryPoint();
        EvaluateLoseConditions();
        AdvanceTemporaryEffects();
        LastTurnDigest = BuildTurnDigest(prevStability, prevTurnout, prevTreasury, prevCapital);

        EmitSignal(SignalName.TurnCompleted);
    }

    public bool TryLaunchPropagandaCampaign()
    {
        if (IsGameOver)
        {
            return false;
        }

        if (LockPolicyWhenOutOfPower && !PlayerInPower)
        {
            GD.Print("Propaganda blocked (out of power).");
            return false;
        }

        float cost = Mathf.Max(0f, PropagandaCapitalCost);
        cost *= GetCandidatePropagandaCostMultiplier();
        if (PoliticalCapital < cost)
        {
            GD.Print("Propaganda blocked (insufficient capital).");
            return false;
        }

        PoliticalCapital -= cost;
        _propagandaTurnsRemaining = Mathf.Max(_propagandaTurnsRemaining, Mathf.Max(1, PropagandaDurationTurns));
        LastEventTitle = "Propaganda Campaign";
        LastEventDescription = $"State information campaign active for {_propagandaTurnsRemaining} turns.";
        AppendActionHistory($"Propaganda Campaign launched (-{cost:0.0} capital).");
        return true;
    }

    public void SetBudgetAllocations(float services, float military, float infrastructure)
    {
        BudgetServices = Mathf.Clamp(services, 0f, 1f);
        BudgetMilitary = Mathf.Clamp(military, 0f, 1f);
        BudgetInfrastructure = Mathf.Clamp(infrastructure, 0f, 1f);
    }

    public bool TryEnactFactionAction(string actionId)
    {
        if (IsGameOver)
        {
            LastFactionActionStatus = "Action blocked: game is already over.";
            return false;
        }

        if (LockPolicyWhenOutOfPower && !PlayerInPower)
        {
            LastFactionActionStatus = "Action blocked: you are currently out of power.";
            return false;
        }

        string normalized = actionId?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized != "union_strike_deal" && normalized != "military_appeasement" && normalized != "youth_reform_package")
        {
            LastFactionActionStatus = "Action blocked: unknown faction action.";
            return false;
        }

        if (IsFactionActionOnCooldown(normalized, out int turnsRemaining))
        {
            LastFactionActionStatus = $"{GetFactionActionName(normalized)} is on cooldown ({turnsRemaining} turns left).";
            return false;
        }

        float treasuryCost;
        float capitalCost;
        float stabilityGain;
        float turnoutGain;
        string description;

        switch (normalized)
        {
            case "union_strike_deal":
                treasuryCost = 260f * FactionActionCostMultiplier;
                capitalCost = 5f * FactionActionCostMultiplier;
                stabilityGain = 2.5f;
                turnoutGain = 0.01f;
                description = "Government brokered a strike deal, easing labor tensions while unsettling hardline business groups.";
                break;
            case "military_appeasement":
                treasuryCost = 220f * FactionActionCostMultiplier;
                capitalCost = 6f * FactionActionCostMultiplier;
                stabilityGain = 2.0f;
                turnoutGain = 0f;
                description = "Additional security procurement and command concessions strengthened military loyalty.";
                break;
            default:
                treasuryCost = 240f * FactionActionCostMultiplier;
                capitalCost = 7f * FactionActionCostMultiplier;
                stabilityGain = 1.5f;
                turnoutGain = 0.02f;
                description = "A reform package for youth expanded civic programs and restored confidence among younger blocs.";
                break;
        }

        if (Treasury < treasuryCost)
        {
            LastFactionActionStatus = $"{GetFactionActionName(normalized)} blocked: insufficient treasury ({treasuryCost:0} required).";
            return false;
        }

        if (PoliticalCapital < capitalCost)
        {
            LastFactionActionStatus = $"{GetFactionActionName(normalized)} blocked: insufficient political capital ({capitalCost:0} required).";
            return false;
        }

        Treasury -= treasuryCost;
        PoliticalCapital = Mathf.Max(0f, PoliticalCapital - capitalCost);

        if (normalized == "union_strike_deal")
        {
            ApplyFactionApprovalDelta("labor", 18f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("youth", 6f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("industrial", -9f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("clergy", -2f * FactionActionApprovalMultiplier);
        }
        else if (normalized == "military_appeasement")
        {
            ApplyFactionApprovalDelta("military", 20f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("clergy", 8f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("youth", -10f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("labor", -5f * FactionActionApprovalMultiplier);
            BudgetMilitary = Mathf.Clamp(BudgetMilitary + 0.08f, 0f, 1f);
        }
        else
        {
            ApplyFactionApprovalDelta("youth", 20f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("labor", 6f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("clergy", -7f * FactionActionApprovalMultiplier);
            ApplyFactionApprovalDelta("industrial", -5f * FactionActionApprovalMultiplier);
            BudgetServices = Mathf.Clamp(BudgetServices + 0.06f, 0f, 1f);
        }

        _stabilityShock += stabilityGain;
        NationalTurnout = Mathf.Clamp(NationalTurnout + turnoutGain, 0f, 1f);
        _factionCrisisStreak = 0;
        SetFactionActionCooldown(normalized, Mathf.Max(1, FactionActionCooldownTurns));
        UpdateFactionDemands();

        LastEventTitle = GetFactionActionName(normalized);
        LastEventDescription = description;
        LastFactionActionStatus = $"{GetFactionActionName(normalized)} executed. Treasury -{treasuryCost:0}, Capital -{capitalCost:0}.";
        AppendActionHistory(LastFactionActionStatus);
        return true;
    }

    public int GetFactionActionCooldownRemaining(string actionId)
    {
        string normalized = actionId?.Trim().ToLowerInvariant() ?? string.Empty;
        return _factionActionCooldowns.TryGetValue(normalized, out int turns) ? Mathf.Max(0, turns) : 0;
    }

    public bool SaveGame(string userPath = "user://savegame.json")
    {
        SaveGameData save = BuildSaveGameData();
        if (GameSaver.TrySave(userPath, save, out string error))
        {
            LastPersistenceFailed = false;
            LastPersistenceStatus = $"Saved to {userPath}";
            GD.Print($"Game saved to {userPath}");
            return true;
        }

        LastPersistenceFailed = true;
        LastPersistenceStatus = $"Save failed: {error}";
        GD.PushWarning($"Save failed: {error}");
        return false;
    }

    public bool LoadGame(string userPath = "user://savegame.json")
    {
        if (!GameSaver.TryLoad(userPath, out SaveGameData save, out string error))
        {
            LastPersistenceFailed = true;
            LastPersistenceStatus = $"Load failed: {error}";
            GD.PushWarning($"Load failed: {error}");
            return false;
        }

        ApplySaveGameData(save);
        LastPersistenceFailed = false;
        LastPersistenceStatus = $"Loaded from {userPath}";
        GD.Print($"Game loaded from {userPath}");
        HasStarted = true;
        EmitSignal(SignalName.TurnCompleted);
        return true;
    }

    private void RecordHistoryPoint()
    {
        int window = Mathf.Max(10, HistoryWindowSize);
        AppendHistory(_stabilityHistory, NationalStability, window);
        AppendHistory(_turnoutHistory, NationalTurnout, window);
        AppendHistory(_treasuryHistory, Treasury, window);
        AppendHistory(_capitalHistory, PoliticalCapital, window);
    }

    private static void AppendHistory(List<float> history, float value, int maxWindow)
    {
        history.Add(value);
        if (history.Count <= maxWindow)
        {
            return;
        }

        int removeCount = history.Count - maxWindow;
        history.RemoveRange(0, removeCount);
    }

    private string BuildTurnDigest(float prevStability, float prevTurnout, float prevTreasury, float prevCapital)
    {
        float stabilityDelta = NationalStability - prevStability;
        float turnoutDelta = NationalTurnout - prevTurnout;
        float treasuryDelta = Treasury - prevTreasury;
        float capitalDelta = PoliticalCapital - prevCapital;

        return
            $"Stability {stabilityDelta:+0.0;-0.0;0.0}, " +
            $"Turnout {turnoutDelta:+0.0%;-0.0%;0.0%}, " +
            $"Treasury {treasuryDelta:+0;-0;0}, " +
            $"Capital {capitalDelta:+0.0;-0.0;0.0}. " +
            $"Top demand: {(string.IsNullOrWhiteSpace(TopFactionDemand) ? "None" : TopFactionDemand)}";
    }

    private void AppendActionHistory(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return;
        }

        _recentActionHistory.Add($"T{TurnCounter}: {entry}");
        if (_recentActionHistory.Count > 12)
        {
            _recentActionHistory.RemoveAt(0);
        }
    }

    private void InitializeRandom()
    {
        _rng = new RandomNumberGenerator();
        _rng.Seed = (ulong)Mathf.Max(1, Seed);

        _densityNoise = new FastNoiseLite();
        _densityNoise.Seed = Seed;
        _densityNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _densityNoise.Frequency = 0.07f;

        _industryNoise = new FastNoiseLite();
        _industryNoise.Seed = Seed * 31 + 17;
        _industryNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _industryNoise.Frequency = 0.09f;
    }

    private void BuildEventDeck()
    {
        _eventDeck.Clear();

        if (DataLoader.TryLoadEvents(out List<SimulationEvent> loadedEvents) && loadedEvents.Count > 0)
        {
            _eventDeck.AddRange(loadedEvents);
            GD.Print($"Loaded {_eventDeck.Count} events from res://Data/events.json");
            return;
        }

        _eventDeck.Add(new SimulationEvent(
            "Market Crash",
            "A sudden downturn increases demand for intervention and political certainty.",
            new PoliticalCompass(-0.20f, -0.03f, 0.08f, -0.02f),
            -12f,
            1f));

        _eventDeck.Add(new SimulationEvent(
            "Corruption Scandal",
            "Leaked documents trigger broad anger and distrust in institutions.",
            new PoliticalCompass(-0.05f, 0.06f, -0.12f, 0.00f),
            -10f,
            0.8f));

        _eventDeck.Add(new SimulationEvent(
            "Tech Boom",
            "High-growth sectors expand and urban optimism rises.",
            new PoliticalCompass(0.14f, 0.07f, -0.05f, 0.10f),
            7f,
            0.7f));

        _eventDeck.Add(new SimulationEvent(
            "Border Crisis",
            "Tensions around migration and security dominate public debate.",
            new PoliticalCompass(-0.02f, -0.06f, 0.14f, -0.18f),
            -8f,
            0.9f));

        _eventDeck.Add(new SimulationEvent(
            "Civil Rights Wave",
            "Grassroots organizing boosts social mobilization and progressive demands.",
            new PoliticalCompass(-0.04f, 0.18f, -0.10f, 0.06f),
            4f,
            0.6f));

        _eventDeck.Add(new SimulationEvent(
            "Energy Shock",
            "Rising prices trigger concern about sovereignty and cost of living.",
            new PoliticalCompass(-0.10f, -0.02f, 0.06f, -0.12f),
            -6f,
            0.75f));
    }

    private void SeedInitialParties()
    {
        _parties.Clear();

        if (DataLoader.TryLoadDefaultParties(out List<Party> loadedParties) && loadedParties.Count > 0)
        {
            _parties.AddRange(loadedParties);
            GD.Print($"Loaded {_parties.Count} default parties from res://Data/parties_defaults.json");
            return;
        }

        _parties.Add(new Party(
            "SocDem",
            new Color(0.85f, 0.28f, 0.28f),
            new PoliticalCompass(-0.60f, 0.35f, -0.25f, 0.35f),
            0.60f));

        _parties.Add(new Party(
            "Conservative",
            new Color(0.22f, 0.38f, 0.80f),
            new PoliticalCompass(0.40f, -0.55f, 0.40f, -0.30f),
            0.45f));

        _parties.Add(new Party(
            "Liberal",
            new Color(0.95f, 0.80f, 0.22f),
            new PoliticalCompass(0.25f, 0.55f, -0.35f, 0.55f),
            0.70f));
    }

    private void SeedInitialFactions()
    {
        _factions.Clear();
        _factionActionCooldowns.Clear();
        LastFactionActionStatus = string.Empty;
        _factions.Add(new FactionState { Id = "military", Name = "Militarists", Approval = 55f });
        _factions.Add(new FactionState { Id = "labor", Name = "Trade Unions", Approval = 55f });
        _factions.Add(new FactionState { Id = "clergy", Name = "Clergy", Approval = 55f });
        _factions.Add(new FactionState { Id = "industrial", Name = "Industrialists", Approval = 55f });
        _factions.Add(new FactionState { Id = "youth", Name = "Youth Movement", Approval = 55f });
        UpdateFactionDemands();
    }

    private void UpdateFactionApproval()
    {
        if (_factions.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _factions.Count; i++)
        {
            FactionState faction = _factions[i];
            float drift = 0f;

            switch (faction.Id)
            {
                case "military":
                    drift += Mathf.Lerp(-1.1f, 1.1f, BudgetMilitary);
                    drift += PolicyManager.IsActive("Strict Policing") ? 0.35f : -0.35f;
                    break;
                case "labor":
                    drift += Mathf.Lerp(-1.0f, 1.0f, BudgetServices);
                    drift += PolicyManager.IsActive("Universal Healthcare") ? 0.45f : -0.25f;
                    break;
                case "clergy":
                    drift += PolicyManager.IsActive("Strict Policing") ? 0.25f : -0.15f;
                    drift += PolicyManager.IsActive("Open Borders") ? -0.20f : 0.10f;
                    break;
                case "industrial":
                    drift += Mathf.Lerp(-0.9f, 1.2f, BudgetInfrastructure);
                    drift += Treasury >= 0f ? 0.20f : -0.25f;
                    break;
                case "youth":
                    drift += PolicyManager.IsActive("Open Borders") ? 0.40f : -0.15f;
                    drift += NationalUnrest > 65f ? -0.40f : 0.15f;
                    break;
            }

            drift += (NationalStability - 50f) * 0.008f;
            faction.Approval = Mathf.Clamp(faction.Approval + drift, 0f, 100f);
        }

        float avgApproval = 0f;
        float minApproval = 100f;
        for (int i = 0; i < _factions.Count; i++)
        {
            avgApproval += _factions[i].Approval;
            minApproval = Mathf.Min(minApproval, _factions[i].Approval);
        }

        avgApproval /= _factions.Count;

        if (avgApproval < 40f)
        {
            _stabilityShock -= 1.4f;
            PoliticalCapital = Mathf.Max(0f, PoliticalCapital - 0.35f);
        }

        if (minApproval < 20f)
        {
            Treasury -= 40f;
            _factionCrisisStreak++;
            LastEventTitle = "Faction Protests";
            LastEventDescription = "Disaffected groups organized disruptions, reducing confidence and productivity.";
        }
        else
        {
            _factionCrisisStreak = 0;
        }

        UpdateFactionDemands();
    }

    private void UpdateFactionDemands()
    {
        TopFactionDemand = string.Empty;

        for (int i = 0; i < _factions.Count; i++)
        {
            FactionState faction = _factions[i];
            faction.CurrentDemand = BuildFactionDemand(faction.Id);
        }

        FactionState lowest = null;
        for (int i = 0; i < _factions.Count; i++)
        {
            if (lowest == null || _factions[i].Approval < lowest.Approval)
            {
                lowest = _factions[i];
            }
        }

        if (lowest != null)
        {
            TopFactionDemand = $"{lowest.Name}: {lowest.CurrentDemand}";
        }
    }

    private string BuildFactionDemand(string factionId)
    {
        switch (factionId)
        {
            case "military":
                return BudgetMilitary < 0.50f
                    ? "Increase military spending and strengthen policing posture."
                    : "Maintain security funding and command authority.";
            case "labor":
                return BudgetServices < 0.50f
                    ? "Boost services funding and worker protections."
                    : "Preserve social programs and wage confidence.";
            case "clergy":
                return PolicyManager.IsActive("Open Borders")
                    ? "Prioritize social cohesion and tighter migration controls."
                    : "Support community order and moral institutions.";
            case "industrial":
                return BudgetInfrastructure < 0.50f
                    ? "Increase infrastructure and investor confidence measures."
                    : "Keep business climate predictable and growth-focused.";
            case "youth":
                return !PolicyManager.IsActive("Open Borders")
                    ? "Expand civic freedoms and openness reforms."
                    : "Deliver jobs, housing, and anti-corruption reforms.";
            default:
                return "Demand clearer governance direction.";
        }
    }

    private static string GetFactionActionName(string actionId)
    {
        switch (actionId)
        {
            case "union_strike_deal":
                return "Union Strike Deal";
            case "military_appeasement":
                return "Military Appeasement";
            case "youth_reform_package":
                return "Youth Reform Package";
            default:
                return "Faction Action";
        }
    }

    private bool IsFactionActionOnCooldown(string actionId, out int turns)
    {
        turns = 0;
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return false;
        }

        if (!_factionActionCooldowns.TryGetValue(actionId, out int remaining) || remaining <= 0)
        {
            return false;
        }

        turns = remaining;
        return true;
    }

    private void SetFactionActionCooldown(string actionId, int turns)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return;
        }

        _factionActionCooldowns[actionId] = Mathf.Max(0, turns);
    }

    private void ReduceFactionActionCooldowns()
    {
        if (_factionActionCooldowns.Count == 0)
        {
            return;
        }

        List<string> keys = new List<string>(_factionActionCooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            int next = Mathf.Max(0, _factionActionCooldowns[key] - 1);
            if (next == 0)
            {
                _factionActionCooldowns.Remove(key);
            }
            else
            {
                _factionActionCooldowns[key] = next;
            }
        }
    }

    private void ApplyFactionApprovalDelta(string factionId, float delta)
    {
        if (string.IsNullOrWhiteSpace(factionId))
        {
            return;
        }

        for (int i = 0; i < _factions.Count; i++)
        {
            FactionState faction = _factions[i];
            if (!string.Equals(faction.Id, factionId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            faction.Approval = Mathf.Clamp(faction.Approval + delta, 0f, 100f);
            return;
        }
    }

    private void SeedDefaultPolicies()
    {
        PolicyManager.ClearAll();

        if (DataLoader.TryLoadPolicies(out List<Policy> loadedPolicies) && loadedPolicies.Count > 0)
        {
            foreach (Policy policy in loadedPolicies)
            {
                PolicyManager.RegisterPolicy(policy);
            }

            GD.Print($"Loaded {loadedPolicies.Count} policies from res://Data/policies.json");
            return;
        }

        PolicyManager.RegisterPolicy(new Policy(
            "Universal Healthcare",
            6f,
            new Dictionary<string, float>
            {
                { Policy.ShiftEconomic, -0.08f },
                { Policy.StabilityOffset, 2.0f },
                { Policy.TreasuryPerTick, -55f },
                { Policy.HappinessKey(BlocCategory.Workers), 0.08f },
                { Policy.HappinessKey(BlocCategory.Retirees), 0.10f },
                { Policy.HappinessKey(BlocCategory.Students), 0.03f },
                { Policy.HappinessKey(BlocCategory.Business), -0.02f },
                { Policy.TurnoutKey(BlocCategory.Workers), 0.02f },
                { Policy.TurnoutKey(BlocCategory.Retirees), 0.01f }
            }));

        PolicyManager.RegisterPolicy(new Policy(
            "Strict Policing",
            5f,
            new Dictionary<string, float>
            {
                { Policy.ShiftAuthority, 0.10f },
                { Policy.StabilityOffset, 2.5f },
                { Policy.TurnoutOffset, -0.02f },
                { Policy.TreasuryPerTick, -35f },
                { Policy.HappinessKey(BlocCategory.Retirees), 0.05f },
                { Policy.HappinessKey(BlocCategory.Business), 0.03f },
                { Policy.HappinessKey(BlocCategory.Students), -0.10f },
                { Policy.HappinessKey(BlocCategory.Workers), -0.03f },
                { Policy.TurnoutKey(BlocCategory.Students), -0.05f }
            }));

        PolicyManager.RegisterPolicy(new Policy(
            "Open Borders",
            4f,
            new Dictionary<string, float>
            {
                { Policy.ShiftDiplomatic, 0.12f },
                { Policy.ShiftSocietal, 0.04f },
                { Policy.StabilityOffset, -1.5f },
                { Policy.TreasuryPerTick, 22f },
                { Policy.HappinessKey(BlocCategory.Professionals), 0.08f },
                { Policy.HappinessKey(BlocCategory.Students), 0.06f },
                { Policy.HappinessKey(BlocCategory.Business), 0.03f },
                { Policy.HappinessKey(BlocCategory.Farmers), -0.04f },
                { Policy.HappinessKey(BlocCategory.Retirees), -0.05f },
                { Policy.TurnoutKey(BlocCategory.Professionals), 0.02f },
                { Policy.TurnoutKey(BlocCategory.Farmers), -0.01f }
            }));
    }

    public bool TrySetPolicyActive(string policyName, bool active)
    {
        if (IsGameOver)
        {
            return false;
        }

        if (LockPolicyWhenOutOfPower && !PlayerInPower)
        {
            GD.Print($"Policy blocked (out of power): {policyName}");
            return false;
        }

        if (!PolicyManager.TryGetPolicy(policyName, out Policy policy))
        {
            return false;
        }

        int minRamp = Mathf.Max(1, MinPolicyRampTurns);
        int maxRamp = Mathf.Max(minRamp, MaxPolicyRampTurns);
        int rampTurns = _rng.RandiRange(minRamp, maxRamp);

        if (!active)
        {
            return PolicyManager.SetPolicyActive(policyName, false, rampTurns);
        }

        if (PolicyManager.IsActive(policyName))
        {
            return true;
        }

        float adjustedCost = policy.PoliticalCapitalCost * GetCandidatePolicyCostMultiplier();
        if (PoliticalCapital < adjustedCost)
        {
            GD.Print($"Policy blocked (candidate-adjusted capital cost): {policyName}");
            return false;
        }

        PoliticalCapital -= adjustedCost;
        return PolicyManager.SetPolicyActive(policyName, true, rampTurns);
    }

    private void InitializeGrid()
    {
        Grid = new DistrictTile[GridSize.X, GridSize.Y];

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                float densitySample = _densityNoise.GetNoise2D(x, y);
                float industrySample = _industryNoise.GetNoise2D(x, y);
                PopulationDensity density = ClassifyDensity(densitySample);
                IndustryType industry = ClassifyIndustry(industrySample);

                DistrictTile tile = new DistrictTile(x, y, density, industry);
                SeedBlocsForTile(tile);
                Grid[x, y] = tile;
            }
        }
    }

    private PopulationDensity ClassifyDensity(float noiseSample)
    {
        if (noiseSample >= 0.35f)
        {
            return PopulationDensity.Metro;
        }
        if (noiseSample >= -0.15f)
        {
            return PopulationDensity.Urban;
        }

        return PopulationDensity.Rural;
    }

    private IndustryType ClassifyIndustry(float noiseSample)
    {
        if (noiseSample < -0.35f)
        {
            return IndustryType.Agriculture;
        }
        if (noiseSample < -0.05f)
        {
            return IndustryType.HeavyIndustry;
        }
        if (noiseSample < 0.35f)
        {
            return IndustryType.Services;
        }

        return IndustryType.Tech;
    }

    private void SeedBlocsForTile(DistrictTile tile)
    {
        switch (tile.Density)
        {
            case PopulationDensity.Rural:
                tile.AddBloc("Farmers", JitterPopulation(2200), Ideology(0.15f, -0.70f, 0.35f, -0.60f, 0.14f));
                tile.AddBloc("Small Business", JitterPopulation(1700), Ideology(0.55f, -0.30f, 0.10f, -0.25f, 0.15f));
                tile.AddBloc("Retirees", JitterPopulation(1800), Ideology(-0.15f, -0.75f, 0.45f, -0.35f, 0.10f));
                break;

            case PopulationDensity.Urban:
                tile.AddBloc("Industrial Workers", JitterPopulation(2600), Ideology(-0.55f, -0.10f, 0.10f, -0.10f, 0.16f));
                tile.AddBloc("Service Workers", JitterPopulation(2400), Ideology(-0.20f, 0.10f, -0.10f, 0.05f, 0.15f));
                tile.AddBloc("Managers", JitterPopulation(1800), Ideology(0.45f, 0.25f, 0.00f, 0.25f, 0.15f));
                break;

            case PopulationDensity.Metro:
                tile.AddBloc("Students", JitterPopulation(2500), Ideology(-0.25f, 0.85f, -0.55f, 0.35f, 0.14f));
                tile.AddBloc("Knowledge Class", JitterPopulation(2300), Ideology(0.40f, 0.65f, -0.30f, 0.65f, 0.14f));
                tile.AddBloc("Service Precariat", JitterPopulation(2900), Ideology(-0.35f, 0.45f, -0.15f, 0.30f, 0.16f));
                break;
        }

        switch (tile.DominantIndustry)
        {
            case IndustryType.Agriculture:
                tile.AddBloc("Agrarian Labour", JitterPopulation(1200), Ideology(-0.45f, -0.40f, 0.20f, -0.50f, 0.12f));
                break;
            case IndustryType.HeavyIndustry:
                tile.AddBloc("Union Trades", JitterPopulation(1300), Ideology(-0.70f, -0.20f, 0.25f, -0.15f, 0.12f));
                break;
            case IndustryType.Services:
                tile.AddBloc("Retail Workers", JitterPopulation(1250), Ideology(-0.20f, 0.20f, -0.10f, 0.15f, 0.12f));
                break;
            case IndustryType.Tech:
                tile.AddBloc("Tech Professionals", JitterPopulation(1400), Ideology(0.55f, 0.65f, -0.40f, 0.70f, 0.12f));
                break;
        }
    }

    private int JitterPopulation(int baseline, float variance = 0.25f)
    {
        float factor = 1f + _rng.RandfRange(-variance, variance);
        return Mathf.Max(300, Mathf.RoundToInt(baseline * factor));
    }

    private PoliticalCompass Ideology(float econ, float soc, float auth, float dip, float jitter)
    {
        return new PoliticalCompass(
            econ + _rng.RandfRange(-jitter, jitter),
            soc + _rng.RandfRange(-jitter, jitter),
            auth + _rng.RandfRange(-jitter, jitter),
            dip + _rng.RandfRange(-jitter, jitter));
    }

    private void ResolveEventCard()
    {
        LastEventTitle = "None";
        LastEventDescription = "No event this turn.";

        if (_eventDeck.Count == 0 || _rng.Randf() > EventChancePerTick)
        {
            _stabilityShock = Mathf.Lerp(_stabilityShock, 0f, 0.50f);
            return;
        }

        SimulationEvent pickedEvent = DrawWeightedEvent();
        if (pickedEvent == null)
        {
            return;
        }

        LastEventTitle = pickedEvent.Title;
        LastEventDescription = pickedEvent.Description;
        _stabilityShock += pickedEvent.StabilityImpact * GetCandidateEventShockMultiplier();
        ApplyGlobalVoterShift(pickedEvent.VoterShift);

        if (pickedEvent.TagEffects != null)
        {
            foreach (EventTagEffect tagEffect in pickedEvent.TagEffects)
            {
                if (tagEffect == null || string.IsNullOrWhiteSpace(tagEffect.Tag))
                {
                    continue;
                }

                _worldTags.SetTag(tagEffect.Tag, tagEffect.DurationTurns);
            }
        }
    }

    private SimulationEvent DrawWeightedEvent()
    {
        float totalWeight = 0f;
        foreach (SimulationEvent simEvent in _eventDeck)
        {
            totalWeight += ComputeEventWeight(simEvent);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = _rng.RandfRange(0f, totalWeight);
        float cumulative = 0f;

        foreach (SimulationEvent simEvent in _eventDeck)
        {
            cumulative += ComputeEventWeight(simEvent);
            if (roll <= cumulative)
            {
                return simEvent;
            }
        }

        return _eventDeck[_eventDeck.Count - 1];
    }

    private void ApplyGlobalVoterShift(PoliticalCompass shift)
    {
        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                foreach (VoterBloc bloc in Grid[x, y].Blocs)
                {
                    float strength = _rng.RandfRange(0.45f, 0.90f);
                    bloc.ApplyGlobalShift(shift, strength);
                }
            }
        }
    }

    private void SimulateTiles()
    {
        PoliticalCompass effectivePolicy = GovernmentPolicy + GetBudgetScaledPolicyShift();
        PolicyTurnEffects turnEffects = BuildPolicyTurnEffects();
        float weightedStability = 0f;
        float weightedTurnout = 0f;
        int totalPopulation = 0;

        float econ = 0f;
        float soc = 0f;
        float auth = 0f;
        float dip = 0f;

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                DistrictTile tile = Grid[x, y];
                tile.ProcessTurn(effectivePolicy, turnEffects);

                int tilePop = Mathf.Max(1, tile.TotalPopulation);
                totalPopulation += tilePop;
                weightedStability += tile.Stability * tilePop;
                weightedTurnout += tile.Turnout * tilePop;

                econ += tile.AverageIdeology.Economic * tilePop;
                soc += tile.AverageIdeology.Societal * tilePop;
                auth += tile.AverageIdeology.Authority * tilePop;
                dip += tile.AverageIdeology.Diplomatic * tilePop;
            }
        }

        if (totalPopulation <= 0)
        {
            NationalStability = 50f;
            NationalTurnout = 0.5f;
            _nationalVoterCenter = new PoliticalCompass(0f, 0f, 0f, 0f);
            return;
        }

        float stabilityOffset = GetBudgetScaledCombinedEffect(Policy.StabilityOffset);
        float turnoutOffset = GetBudgetScaledCombinedEffect(Policy.TurnoutOffset);

        NationalStability = Mathf.Clamp(weightedStability / totalPopulation + _stabilityShock + stabilityOffset, 0f, 100f);
        NationalTurnout = Mathf.Clamp(weightedTurnout / totalPopulation + turnoutOffset, 0f, 1f);
        NationalUnrest = Mathf.Clamp(100f - NationalStability, 0f, 100f);
        _nationalVoterCenter = new PoliticalCompass(
            econ / totalPopulation,
            soc / totalPopulation,
            auth / totalPopulation,
            dip / totalPopulation);

        _stabilityShock = Mathf.Lerp(_stabilityShock, 0f, 0.30f);
    }

    private void ApplyNeighborDiffusion()
    {
        if (Grid.Length == 0)
        {
            return;
        }

        float clampedIdeologyRate = Mathf.Clamp(NeighborIdeologyDiffusionRate, 0f, 1f);
        float clampedStabilityStrength = Mathf.Max(0f, NeighborStabilitySpilloverStrength);
        float clampedMediaPower = Mathf.Clamp(EffectiveMediaPower, 0f, 1f);
        PoliticalCompass mediaTarget = BlendCompass(_nationalVoterCenter, GovernmentPolicy, Mathf.Clamp(GovernmentMediaTargetBias, 0f, 1f));

        if (clampedIdeologyRate <= 0f && clampedStabilityStrength <= 0f && clampedMediaPower <= 0f)
        {
            return;
        }

        int width = GridSize.X;
        int height = GridSize.Y;

        PoliticalCompass[,] ideologyField = new PoliticalCompass[width, height];
        float[,] stabilityField = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                ideologyField[x, y] = Grid[x, y].AverageIdeology;
                stabilityField[x, y] = Grid[x, y].Stability;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                DistrictTile tile = Grid[x, y];
                PoliticalCompass neighborIdeology = GetAverageNeighborIdeology(ideologyField, x, y, width, height);
                float neighborStability = GetAverageNeighborStability(stabilityField, x, y, width, height);

                if (clampedIdeologyRate > 0f)
                {
                    foreach (VoterBloc bloc in tile.Blocs)
                    {
                        bloc.NudgeIdeologyTowards(neighborIdeology, clampedIdeologyRate);

                        if (clampedMediaPower > 0f)
                        {
                            float mediaStrength = Mathf.Clamp(tile.MediaAccess * clampedMediaPower, 0f, 1f);
                            bloc.NudgeIdeologyTowards(mediaTarget, mediaStrength);
                        }
                    }
                }
                else if (clampedMediaPower > 0f)
                {
                    foreach (VoterBloc bloc in tile.Blocs)
                    {
                        float mediaStrength = Mathf.Clamp(tile.MediaAccess * clampedMediaPower, 0f, 1f);
                        bloc.NudgeIdeologyTowards(mediaTarget, mediaStrength);
                    }
                }

                float stabilityDelta = neighborStability - tile.Stability;
                tile.ExternalStabilityPressure = stabilityDelta * 0.01f * clampedStabilityStrength;
            }
        }
    }

    private static PoliticalCompass GetAverageNeighborIdeology(PoliticalCompass[,] ideologyField, int x, int y, int width, int height)
    {
        float econ = 0f;
        float soc = 0f;
        float auth = 0f;
        float dip = 0f;
        int count = 0;

        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                int nx = x + ox;
                int ny = y + oy;

                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    continue;
                }

                PoliticalCompass ide = ideologyField[nx, ny];
                econ += ide.Economic;
                soc += ide.Societal;
                auth += ide.Authority;
                dip += ide.Diplomatic;
                count++;
            }
        }

        if (count == 0)
        {
            return ideologyField[x, y];
        }

        return new PoliticalCompass(econ / count, soc / count, auth / count, dip / count);
    }

    private static float GetAverageNeighborStability(float[,] stabilityField, int x, int y, int width, int height)
    {
        float total = 0f;
        int count = 0;

        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                int nx = x + ox;
                int ny = y + oy;

                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    continue;
                }

                total += stabilityField[nx, ny];
                count++;
            }
        }

        if (count == 0)
        {
            return stabilityField[x, y];
        }

        return total / count;
    }

    private static PoliticalCompass BlendCompass(PoliticalCompass from, PoliticalCompass to, float t)
    {
        return new PoliticalCompass(
            Mathf.Lerp(from.Economic, to.Economic, t),
            Mathf.Lerp(from.Societal, to.Societal, t),
            Mathf.Lerp(from.Authority, to.Authority, t),
            Mathf.Lerp(from.Diplomatic, to.Diplomatic, t));
    }

    private PolicyTurnEffects BuildPolicyTurnEffects()
    {
        PolicyTurnEffects effects = new PolicyTurnEffects();
        Dictionary<string, float> combined = PolicyManager.GetCombinedEffects();

        foreach (KeyValuePair<string, float> kvp in combined)
        {
            float scaledValue = kvp.Value * GetBudgetMultiplierForEffect(kvp.Key);

            if (kvp.Key.StartsWith(Policy.BlocHappinessPrefix, StringComparison.Ordinal))
            {
                string categoryToken = kvp.Key.Substring(Policy.BlocHappinessPrefix.Length);
                if (TryParseBlocCategory(categoryToken, out BlocCategory category))
                {
                    effects.AddHappinessOffset(category, scaledValue);
                }

                continue;
            }

            if (kvp.Key.StartsWith(Policy.BlocTurnoutPrefix, StringComparison.Ordinal))
            {
                string categoryToken = kvp.Key.Substring(Policy.BlocTurnoutPrefix.Length);
                if (TryParseBlocCategory(categoryToken, out BlocCategory category))
                {
                    effects.AddTurnoutOffset(category, scaledValue);
                }
            }
        }

        effects.GlobalHappinessOffset = effects.GetHappinessOffset(BlocCategory.General);
        effects.GlobalTurnoutOffset = effects.GetTurnoutOffset(BlocCategory.General);
        return effects;
    }

    private static bool TryParseBlocCategory(string token, out BlocCategory category)
    {
        if (Enum.TryParse(token, true, out category))
        {
            return true;
        }

        category = BlocCategory.General;
        return false;
    }

    private void ApplyPolicyTickEffects()
    {
        Treasury += GetBudgetScaledCombinedEffect(Policy.TreasuryPerTick);

        if (Treasury < 0f)
        {
            float debtStress = Mathf.Clamp(-Treasury / 1500f, 0f, 2.5f);
            float mitigatedStress = debtStress * (1f - GetCandidateDebtStressMitigation());
            _stabilityShock -= 1.5f + mitigatedStress * 4f;
            PoliticalCapital = Mathf.Max(0f, PoliticalCapital - (0.25f + mitigatedStress * 0.75f));
        }

        float candidateCapitalGain = GetCandidateCapitalGainMultiplier();
        PoliticalCapital = Mathf.Clamp(
            PoliticalCapital + GetBudgetScaledCombinedEffect(Policy.PoliticalCapitalPerTick) * candidateCapitalGain,
            0f,
            100f);
    }

    private PoliticalCompass GetBudgetScaledPolicyShift()
    {
        return new PoliticalCompass(
            GetBudgetScaledCombinedEffect(Policy.ShiftEconomic),
            GetBudgetScaledCombinedEffect(Policy.ShiftSocietal),
            GetBudgetScaledCombinedEffect(Policy.ShiftAuthority),
            GetBudgetScaledCombinedEffect(Policy.ShiftDiplomatic));
    }

    private float GetBudgetScaledCombinedEffect(string key)
    {
        return PolicyManager.GetCombinedEffect(key) * GetBudgetMultiplierForEffect(key);
    }

    private float GetBudgetMultiplierForEffect(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return 1f;
        }

        if (key == Policy.ShiftAuthority)
        {
            return Mathf.Lerp(0.3f, 1.7f, BudgetMilitary);
        }

        if (key == Policy.StabilityOffset || key == Policy.TurnoutOffset ||
            key.StartsWith(Policy.BlocHappinessPrefix, StringComparison.Ordinal) ||
            key.StartsWith(Policy.BlocTurnoutPrefix, StringComparison.Ordinal))
        {
            return Mathf.Lerp(0.3f, 1.7f, BudgetServices);
        }

        if (key == Policy.TreasuryPerTick || key == Policy.PoliticalCapitalPerTick || key == Policy.ShiftDiplomatic)
        {
            return Mathf.Lerp(0.3f, 1.7f, BudgetInfrastructure);
        }

        if (key == Policy.ShiftEconomic || key == Policy.ShiftSocietal)
        {
            float blend = (BudgetServices + BudgetInfrastructure) * 0.5f;
            return Mathf.Lerp(0.3f, 1.7f, blend);
        }

        return 1f;
    }

    private void AdvancePolicyRamps()
    {
        PolicyManager.AdvancePolicyRamps();
    }

    private void UpdatePartyDrift()
    {
        foreach (Party party in _parties)
        {
            party.DriftTowards(_nationalVoterCenter, PartyDriftPerTick);
        }
    }

    private void UpdatePolls()
    {
        _lastPolls.Clear();
        _projectedPolls.Clear();
        if (_parties.Count == 0)
        {
            return;
        }

        Dictionary<string, float> scoreByParty = new Dictionary<string, float>();
        foreach (Party party in _parties)
        {
            scoreByParty[party.Name] = 0f;
        }

        float totalScore = 0f;

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                DistrictTile tile = Grid[x, y];
                foreach (VoterBloc bloc in tile.Blocs)
                {
                    float turnoutWeight = bloc.Population * bloc.TurnoutChance;
                    float totalPreference = 0f;

                    Dictionary<string, float> localPreference = new Dictionary<string, float>();
                    foreach (Party party in _parties)
                    {
                        float alignment = party.AlignmentScore(bloc.Ideology);
                        float issueMatch = 1f - Mathf.Abs(
                            bloc.Ideology.GetAxis(tile.LocalIssue) - party.Platform.GetAxis(tile.LocalIssue)) * 0.5f;
                        float preference = Mathf.Clamp(alignment * 0.80f + issueMatch * 0.20f, 0f, 1f);
                        localPreference[party.Name] = preference;
                        totalPreference += preference;
                    }

                    if (totalPreference <= 0f)
                    {
                        float equalShare = turnoutWeight / _parties.Count;
                        foreach (Party party in _parties)
                        {
                            scoreByParty[party.Name] += equalShare;
                            totalScore += equalShare;
                        }
                    }
                    else
                    {
                        foreach (Party party in _parties)
                        {
                            float contribution = turnoutWeight * (localPreference[party.Name] / totalPreference);
                            scoreByParty[party.Name] += contribution;
                            totalScore += contribution;
                        }
                    }
                }
            }
        }

        if (totalScore <= 0f)
        {
            float equal = 1f / _parties.Count;
            foreach (Party party in _parties)
            {
                _lastPolls[party.Name] = equal;
            }

            BuildProjectedPolls();
            return;
        }

        foreach (Party party in _parties)
        {
            _lastPolls[party.Name] = scoreByParty[party.Name] / totalScore;
        }

        BuildProjectedPolls();
    }

    private void BuildProjectedPolls()
    {
        _projectedPolls.Clear();
        if (_lastPolls.Count == 0)
        {
            PollMarginOfError = 0.05f;
            return;
        }

        float baseError = 0.03f;
        float volatilityError = Mathf.Clamp((100f - NationalStability) / 700f, 0f, 0.09f);
        PollMarginOfError = Mathf.Clamp(baseError + volatilityError, 0.03f, 0.12f);

        float total = 0f;
        foreach (KeyValuePair<string, float> kvp in _lastPolls)
        {
            float projected = Mathf.Clamp(kvp.Value + _rng.RandfRange(-PollMarginOfError, PollMarginOfError), 0f, 1f);
            if (!string.IsNullOrWhiteSpace(PlayerPartyName) && kvp.Key == PlayerPartyName)
            {
                projected = Mathf.Clamp(projected + GetCandidatePollingBonus(), 0f, 1f);
            }

            _projectedPolls[kvp.Key] = projected;
            total += projected;
        }

        if (total <= 0.0001f)
        {
            float equal = 1f / _projectedPolls.Count;
            List<string> keys = new List<string>(_projectedPolls.Keys);
            foreach (string key in keys)
            {
                _projectedPolls[key] = equal;
            }

            return;
        }

        List<string> normalizeKeys = new List<string>(_projectedPolls.Keys);
        foreach (string key in normalizeKeys)
        {
            _projectedPolls[key] /= total;
        }
    }

    private void TrySpawnNewParty()
    {
        if (_parties.Count >= 8)
        {
            return;
        }

        int totalPopulation = 0;
        int disaffectedPopulation = 0;
        float econ = 0f;
        float soc = 0f;
        float auth = 0f;
        float dip = 0f;

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                foreach (VoterBloc bloc in Grid[x, y].Blocs)
                {
                    totalPopulation += bloc.Population;
                    float closestDistance = DistanceToClosestParty(bloc.Ideology) / PoliticalCompass.MaxDistance;
                    if (closestDistance < PartySpawnDistanceThreshold)
                    {
                        continue;
                    }

                    disaffectedPopulation += bloc.Population;
                    econ += bloc.Ideology.Economic * bloc.Population;
                    soc += bloc.Ideology.Societal * bloc.Population;
                    auth += bloc.Ideology.Authority * bloc.Population;
                    dip += bloc.Ideology.Diplomatic * bloc.Population;
                }
            }
        }

        if (totalPopulation <= 0 || disaffectedPopulation < totalPopulation * 0.08f)
        {
            return;
        }

        PoliticalCompass platform = new PoliticalCompass(
            econ / disaffectedPopulation,
            soc / disaffectedPopulation,
            auth / disaffectedPopulation,
            dip / disaffectedPopulation);

        string spawnedName = BuildSpawnedPartyName(platform);
        Color color = Color.FromHsv(_rng.Randf(), 0.65f, 0.95f);
        float opportunism = _rng.RandfRange(0.35f, 0.85f);
        _parties.Add(new Party(spawnedName, color, platform, opportunism));

        GD.Print($"New party spawned: {spawnedName}.");
    }

    private float DistanceToClosestParty(PoliticalCompass ideology)
    {
        if (_parties.Count == 0)
        {
            return PoliticalCompass.MaxDistance;
        }

        float minDistance = PoliticalCompass.MaxDistance;
        foreach (Party party in _parties)
        {
            minDistance = Mathf.Min(minDistance, ideology.DistanceTo(party.Platform));
        }

        return minDistance;
    }

    private string BuildSpawnedPartyName(PoliticalCompass platform)
    {
        string econ = platform.Economic < -0.20f ? "Labor" : (platform.Economic > 0.20f ? "Market" : "Civic");
        string social = platform.Societal < -0.20f ? "Tradition" : (platform.Societal > 0.20f ? "Progress" : "Center");
        string authority = platform.Authority > 0.25f ? "Order" : (platform.Authority < -0.25f ? "Liberty" : "Forum");
        string baseName = $"{social} {econ} {authority}";

        if (!PartyNameExists(baseName))
        {
            return baseName;
        }

        int suffix = 2;
        while (PartyNameExists($"{baseName} {suffix}"))
        {
            suffix++;
        }

        return $"{baseName} {suffix}";
    }

    private bool PartyNameExists(string name)
    {
        foreach (Party party in _parties)
        {
            if (party.Name == name)
            {
                return true;
            }
        }
        return false;
    }

    private void ResolveElection()
    {
        if (_parties.Count == 0)
        {
            return;
        }

        Dictionary<string, float> votes = new Dictionary<string, float>();
        foreach (Party party in _parties)
        {
            votes[party.Name] = 0f;
        }

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                DistrictTile tile = Grid[x, y];
                foreach (VoterBloc bloc in tile.Blocs)
                {
                    float totalWeight = 0f;
                    float[] partyWeights = new float[_parties.Count];

                    for (int p = 0; p < _parties.Count; p++)
                    {
                        Party party = _parties[p];
                        float alignment = party.AlignmentScore(bloc.Ideology);
                        float campaignBuff = CampaignBuff(party, tile);
                        float weight = Mathf.Pow(Mathf.Max(0.01f, alignment), 2f) * campaignBuff;
                        partyWeights[p] = weight;
                        totalWeight += weight;
                    }

                    float voters = bloc.Population * bloc.TurnoutChance;
                    if (totalWeight <= 0f)
                    {
                        float equalVotes = voters / _parties.Count;
                        foreach (Party party in _parties)
                        {
                            votes[party.Name] += equalVotes;
                        }
                    }
                    else
                    {
                        for (int p = 0; p < _parties.Count; p++)
                        {
                            Party party = _parties[p];
                            float share = partyWeights[p] / totalWeight;
                            votes[party.Name] += voters * share;
                        }
                    }
                }
            }
        }

        _lastSeats.Clear();
        Dictionary<string, float> remainders = new Dictionary<string, float>();
        float totalVotes = 0f;
        foreach (KeyValuePair<string, float> kvp in votes)
        {
            totalVotes += kvp.Value;
        }

        int allocated = 0;
        foreach (Party party in _parties)
        {
            float voteShare = totalVotes <= 0f ? 0f : votes[party.Name] / totalVotes;
            float exactSeats = voteShare * ParliamentSeats;
            int floorSeats = Mathf.FloorToInt(exactSeats);
            _lastSeats[party.Name] = floorSeats;
            remainders[party.Name] = exactSeats - floorSeats;
            allocated += floorSeats;
        }

        while (allocated < ParliamentSeats)
        {
            string bestParty = FindLargestRemainder(remainders);
            if (bestParty == string.Empty)
            {
                break;
            }

            _lastSeats[bestParty] += 1;
            remainders[bestParty] = -1f;
            allocated++;
        }

        string winner = FindTopSeatParty();
        CurrentGovernmentParty = winner;
        PlayerInPower = string.IsNullOrWhiteSpace(PlayerPartyName) || winner == PlayerPartyName;
        Party winnerParty = FindPartyByName(winner);
        if (winnerParty != null)
        {
            GovernmentPolicy = GovernmentPolicy.NudgeTowards(winnerParty.Platform, 0.12f);
        }

        if (LockPolicyWhenOutOfPower && !PlayerInPower)
        {
            PoliticalCapital = Mathf.Min(PoliticalCapital, 2f);
        }

        if (EndGameOnElectionLoss && !PlayerInPower)
        {
            EndGame(
                "Election Lost",
                $"Your party ({PlayerPartyName}) lost control to {winner}. The mandate has shifted.");
        }

        GD.Print($"Election resolved on turn {TurnCounter}. Winner: {winner}.");
    }

    private void EvaluateLoseConditions()
    {
        if (IsGameOver)
        {
            return;
        }

        if (NationalUnrest >= RevolutionUnrestThreshold)
        {
            _highUnrestTurnStreak++;
        }
        else
        {
            _highUnrestTurnStreak = 0;
        }

        if (_highUnrestTurnStreak >= Mathf.Max(1, RevolutionUnrestTurns))
        {
            EndGame(
                "Revolution",
                $"Unrest remained above {RevolutionUnrestThreshold:0} for {_highUnrestTurnStreak} turns.");
            return;
        }

        if (Treasury <= BankruptcyTreasuryThreshold)
        {
            EndGame(
                "Bankruptcy",
                $"Treasury collapsed to {Treasury:0} (threshold {BankruptcyTreasuryThreshold:0}). IMF intervention triggered.");
            return;
        }

        if (_factionCrisisStreak >= 4)
        {
            EndGame(
                "Factional Collapse",
                "One or more factions remained in extreme disapproval long enough to trigger nationwide paralysis.");
        }
    }

    private void EndGame(string reason, string detail)
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        GameOverReason = reason;
        GameOverDetail = detail;
        EmitSignal(SignalName.GameEnded, reason, detail);
    }

    private void AdvanceTemporaryEffects()
    {
        if (_propagandaTurnsRemaining > 0)
        {
            _propagandaTurnsRemaining--;
        }

        ReduceFactionActionCooldowns();
    }

    private SaveGameData BuildSaveGameData()
    {
        SaveGameData save = new SaveGameData();

        save.Global.GridWidth = GridSize.X;
        save.Global.GridHeight = GridSize.Y;
        save.Global.Seed = Seed;
        save.Global.TurnCounter = TurnCounter;
        save.Global.NationalStability = NationalStability;
        save.Global.NationalUnrest = NationalUnrest;
        save.Global.NationalTurnout = NationalTurnout;
        save.Global.Treasury = Treasury;
        save.Global.PoliticalCapital = PoliticalCapital;
        save.Global.StabilityShock = _stabilityShock;
        save.Global.HighUnrestTurnStreak = _highUnrestTurnStreak;
        save.Global.LastEventTitle = LastEventTitle;
        save.Global.LastEventDescription = LastEventDescription;
        save.Global.CurrentGovernmentParty = CurrentGovernmentParty;
        save.Global.PlayerPartyName = PlayerPartyName;
        save.Global.PlayerInPower = PlayerInPower;
        save.Global.IsGameOver = IsGameOver;
        save.Global.GameOverReason = GameOverReason;
        save.Global.GameOverDetail = GameOverDetail;
        save.Global.PollMarginOfError = PollMarginOfError;
        save.Global.PropagandaTurnsRemaining = _propagandaTurnsRemaining;
        save.Global.BudgetServices = BudgetServices;
        save.Global.BudgetMilitary = BudgetMilitary;
        save.Global.BudgetInfrastructure = BudgetInfrastructure;
        save.Global.LastTurnDigest = LastTurnDigest;
        save.Global.Candidate = ToCandidateSave(PlayerCandidate);
        save.Global.GovernmentPolicy = ToCompassSave(GovernmentPolicy);
        save.Global.NationalVoterCenter = ToCompassSave(_nationalVoterCenter);

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                DistrictTile tile = Grid[x, y];
                TileSaveData tileSave = new TileSaveData
                {
                    X = x,
                    Y = y,
                    Density = (int)tile.Density,
                    DominantIndustry = (int)tile.DominantIndustry,
                    Stability = tile.Stability,
                    Turnout = tile.Turnout,
                    ExternalStabilityPressure = tile.ExternalStabilityPressure,
                    MediaAccess = tile.MediaAccess,
                    LocalIssue = (int)tile.LocalIssue,
                    AverageIdeology = ToCompassSave(tile.AverageIdeology)
                };

                for (int i = 0; i < tile.Blocs.Count; i++)
                {
                    VoterBloc bloc = tile.Blocs[i];
                    tileSave.Blocs.Add(new BlocSaveData
                    {
                        Name = bloc.Name,
                        Population = bloc.Population,
                        Category = (int)bloc.Category,
                        Ideology = ToCompassSave(bloc.Ideology),
                        Happiness = bloc.Happiness,
                        TurnoutChance = bloc.TurnoutChance,
                        Anger = bloc.Anger,
                        Hope = bloc.Hope,
                        LastIdeologyHappinessPenalty = bloc.LastIdeologyHappinessPenalty,
                        LastIssueHappinessPenalty = bloc.LastIssueHappinessPenalty,
                        LastPolicyHappinessModifier = bloc.LastPolicyHappinessModifier,
                        LastPolicyTurnoutModifier = bloc.LastPolicyTurnoutModifier
                    });
                }

                save.Grid.Add(tileSave);
            }
        }

        for (int i = 0; i < _parties.Count; i++)
        {
            Party party = _parties[i];
            save.Parties.Add(new PartySaveData
            {
                Name = party.Name,
                ColorHtml = party.Color.ToHtml(),
                Platform = ToCompassSave(party.Platform),
                Opportunism = party.Opportunism
            });
        }

        List<PolicyStateSnapshot> policyStates = PolicyManager.ExportPolicyStates();
        foreach (PolicyStateSnapshot state in policyStates)
        {
            save.PolicyStates.Add(new PolicyStateSaveData
            {
                Name = state.Name,
                TargetActive = state.TargetActive,
                CurrentStrength = state.CurrentStrength,
                StepPerTurn = state.StepPerTurn
            });
        }

        for (int i = 0; i < _factions.Count; i++)
        {
            FactionState faction = _factions[i];
            save.Factions.Add(new FactionSaveData
            {
                Id = faction.Id,
                Name = faction.Name,
                Approval = faction.Approval,
                CurrentDemand = faction.CurrentDemand
            });
        }

        foreach (KeyValuePair<string, int> cooldown in _factionActionCooldowns)
        {
            if (string.IsNullOrWhiteSpace(cooldown.Key) || cooldown.Value <= 0)
            {
                continue;
            }

            save.FactionActionCooldowns.Add(new FactionActionCooldownSaveData
            {
                ActionId = cooldown.Key,
                RemainingTurns = cooldown.Value
            });
        }

        foreach (KeyValuePair<string, int> kvp in _worldTags.ActiveTags)
        {
            save.WorldTags.Add(new WorldTagSaveData
            {
                Tag = kvp.Key,
                RemainingTurns = kvp.Value
            });
        }

        save.StabilityHistory.AddRange(_stabilityHistory);
        save.TurnoutHistory.AddRange(_turnoutHistory);
        save.TreasuryHistory.AddRange(_treasuryHistory);
        save.CapitalHistory.AddRange(_capitalHistory);
        save.ActionHistory.AddRange(_recentActionHistory);

        foreach (KeyValuePair<string, int> seat in _lastSeats)
        {
            save.LastElectionSeats[seat.Key] = seat.Value;
        }

        return save;
    }

    private void ApplySaveGameData(SaveGameData save)
    {
        if (save == null || save.Global == null)
        {
            return;
        }

        GridSize = new Vector2I(Mathf.Max(1, save.Global.GridWidth), Mathf.Max(1, save.Global.GridHeight));
        Seed = save.Global.Seed;
        TurnCounter = save.Global.TurnCounter;
        NationalStability = save.Global.NationalStability;
        NationalUnrest = save.Global.NationalUnrest;
        NationalTurnout = save.Global.NationalTurnout;
        Treasury = save.Global.Treasury;
        PoliticalCapital = save.Global.PoliticalCapital;
        _stabilityShock = save.Global.StabilityShock;
        _highUnrestTurnStreak = save.Global.HighUnrestTurnStreak;
        LastEventTitle = save.Global.LastEventTitle ?? "None";
        LastEventDescription = save.Global.LastEventDescription ?? "No event this turn.";
        CurrentGovernmentParty = save.Global.CurrentGovernmentParty ?? "Caretaker Coalition";
        PlayerPartyName = string.IsNullOrWhiteSpace(save.Global.PlayerPartyName) ? PlayerPartyName : save.Global.PlayerPartyName;
        PlayerInPower = save.Global.PlayerInPower;
        IsGameOver = save.Global.IsGameOver;
        GameOverReason = save.Global.GameOverReason ?? string.Empty;
        GameOverDetail = save.Global.GameOverDetail ?? string.Empty;
        PollMarginOfError = save.Global.PollMarginOfError;
        _propagandaTurnsRemaining = save.Global.PropagandaTurnsRemaining;
        BudgetServices = Mathf.Clamp(save.Global.BudgetServices, 0f, 1f);
        BudgetMilitary = Mathf.Clamp(save.Global.BudgetMilitary, 0f, 1f);
        BudgetInfrastructure = Mathf.Clamp(save.Global.BudgetInfrastructure, 0f, 1f);
        LastTurnDigest = save.Global.LastTurnDigest ?? string.Empty;
        GovernmentPolicy = FromCompassSave(save.Global.GovernmentPolicy);
        _nationalVoterCenter = FromCompassSave(save.Global.NationalVoterCenter);

        InitializeRandom();
        BuildEventDeck();
        SeedDefaultPolicies();
        InitializeCandidateData();

        if (save.Global.Candidate != null)
        {
            PlayerCandidate = CandidateProfile.Build(
                save.Global.Candidate.Name,
                save.Global.Candidate.BackgroundId,
                save.Global.Candidate.TraitIds,
                _candidateBackgrounds,
                _candidateTraits);
            PlayerCandidate.Charisma = Mathf.Clamp(save.Global.Candidate.Charisma, 0f, 1f);
            PlayerCandidate.Competence = Mathf.Clamp(save.Global.Candidate.Competence, 0f, 1f);
            PlayerCandidate.Integrity = Mathf.Clamp(save.Global.Candidate.Integrity, 0f, 1f);
            PlayerCandidate.Populism = Mathf.Clamp(save.Global.Candidate.Populism, 0f, 1f);
        }
        else
        {
            EnsureDefaultCandidateProfile(true);
        }

        _factions.Clear();
        if (save.Factions != null)
        {
            for (int i = 0; i < save.Factions.Count; i++)
            {
                FactionSaveData faction = save.Factions[i];
                if (faction == null || string.IsNullOrWhiteSpace(faction.Id))
                {
                    continue;
                }

                _factions.Add(new FactionState
                {
                    Id = faction.Id,
                    Name = string.IsNullOrWhiteSpace(faction.Name) ? faction.Id : faction.Name,
                    Approval = Mathf.Clamp(faction.Approval, 0f, 100f),
                    CurrentDemand = faction.CurrentDemand ?? string.Empty
                });
            }
        }

        if (_factions.Count == 0)
        {
            SeedInitialFactions();
        }
        else
        {
            UpdateFactionDemands();
        }

        _factionActionCooldowns.Clear();
        if (save.FactionActionCooldowns != null)
        {
            for (int i = 0; i < save.FactionActionCooldowns.Count; i++)
            {
                FactionActionCooldownSaveData cooldown = save.FactionActionCooldowns[i];
                if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.ActionId) || cooldown.RemainingTurns <= 0)
                {
                    continue;
                }

                _factionActionCooldowns[cooldown.ActionId] = cooldown.RemainingTurns;
            }
        }

        _parties.Clear();
        if (save.Parties != null)
        {
            foreach (PartySaveData p in save.Parties)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Name))
                {
                    continue;
                }

                Color color = Color.FromString(p.ColorHtml ?? "#ffffff", Colors.White);
                _parties.Add(new Party(p.Name, color, FromCompassSave(p.Platform), p.Opportunism));
            }
        }

        if (_parties.Count == 0)
        {
            SeedInitialParties();
        }

        List<PolicyStateSnapshot> policySnapshots = new List<PolicyStateSnapshot>();
        if (save.PolicyStates != null)
        {
            foreach (PolicyStateSaveData ps in save.PolicyStates)
            {
                if (ps == null)
                {
                    continue;
                }

                policySnapshots.Add(new PolicyStateSnapshot
                {
                    Name = ps.Name,
                    TargetActive = ps.TargetActive,
                    CurrentStrength = ps.CurrentStrength,
                    StepPerTurn = ps.StepPerTurn
                });
            }
        }

        PolicyManager.ImportPolicyStates(policySnapshots);

        List<KeyValuePair<string, int>> tags = new List<KeyValuePair<string, int>>();
        if (save.WorldTags != null)
        {
            foreach (WorldTagSaveData tag in save.WorldTags)
            {
                if (tag == null || string.IsNullOrWhiteSpace(tag.Tag))
                {
                    continue;
                }

                tags.Add(new KeyValuePair<string, int>(tag.Tag, tag.RemainingTurns));
            }
        }

        _worldTags.ImportTags(tags);
        SyncPolicyStateTags();

        Grid = new DistrictTile[GridSize.X, GridSize.Y];
        if (save.Grid != null)
        {
            foreach (TileSaveData ts in save.Grid)
            {
                if (ts == null)
                {
                    continue;
                }

                if (ts.X < 0 || ts.Y < 0 || ts.X >= GridSize.X || ts.Y >= GridSize.Y)
                {
                    continue;
                }

                PopulationDensity density = Enum.IsDefined(typeof(PopulationDensity), ts.Density)
                    ? (PopulationDensity)ts.Density
                    : PopulationDensity.Urban;
                IndustryType industry = Enum.IsDefined(typeof(IndustryType), ts.DominantIndustry)
                    ? (IndustryType)ts.DominantIndustry
                    : IndustryType.Services;

                DistrictTile tile = new DistrictTile(ts.X, ts.Y, density, industry)
                {
                    Stability = ts.Stability,
                    Turnout = ts.Turnout,
                    ExternalStabilityPressure = ts.ExternalStabilityPressure,
                    MediaAccess = Mathf.Clamp(ts.MediaAccess, 0f, 1f),
                    LocalIssue = Enum.IsDefined(typeof(CompassAxis), ts.LocalIssue)
                        ? (CompassAxis)ts.LocalIssue
                        : CompassAxis.Economic,
                    AverageIdeology = FromCompassSave(ts.AverageIdeology)
                };

                if (ts.Blocs != null)
                {
                    for (int i = 0; i < ts.Blocs.Count; i++)
                    {
                        BlocSaveData bs = ts.Blocs[i];
                        if (bs == null)
                        {
                            continue;
                        }

                        VoterBloc bloc = new VoterBloc(bs.Name ?? "Bloc", bs.Population, FromCompassSave(bs.Ideology))
                        {
                            Category = Enum.IsDefined(typeof(BlocCategory), bs.Category)
                                ? (BlocCategory)bs.Category
                                : BlocCategory.General,
                            Happiness = bs.Happiness,
                            TurnoutChance = bs.TurnoutChance,
                            Anger = bs.Anger,
                            Hope = bs.Hope,
                            LastIdeologyHappinessPenalty = bs.LastIdeologyHappinessPenalty,
                            LastIssueHappinessPenalty = bs.LastIssueHappinessPenalty,
                            LastPolicyHappinessModifier = bs.LastPolicyHappinessModifier,
                            LastPolicyTurnoutModifier = bs.LastPolicyTurnoutModifier
                        };
                        tile.Blocs.Add(bloc);
                    }
                }

                Grid[ts.X, ts.Y] = tile;
            }
        }

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                if (Grid[x, y] != null)
                {
                    continue;
                }

                DistrictTile tile = new DistrictTile(x, y, PopulationDensity.Urban, IndustryType.Services);
                tile.AddBloc("Residents", 1000, new PoliticalCompass(0f, 0f, 0f, 0f));
                Grid[x, y] = tile;
            }
        }

        _stabilityHistory.Clear();
        _turnoutHistory.Clear();
        _treasuryHistory.Clear();
        _capitalHistory.Clear();
        _recentActionHistory.Clear();
        if (save.StabilityHistory != null) _stabilityHistory.AddRange(save.StabilityHistory);
        if (save.TurnoutHistory != null) _turnoutHistory.AddRange(save.TurnoutHistory);
        if (save.TreasuryHistory != null) _treasuryHistory.AddRange(save.TreasuryHistory);
        if (save.CapitalHistory != null) _capitalHistory.AddRange(save.CapitalHistory);
        if (save.ActionHistory != null)
        {
            for (int i = 0; i < save.ActionHistory.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(save.ActionHistory[i]))
                {
                    _recentActionHistory.Add(save.ActionHistory[i]);
                }
            }

            while (_recentActionHistory.Count > 12)
            {
                _recentActionHistory.RemoveAt(0);
            }
        }

        _lastSeats.Clear();
        if (save.LastElectionSeats != null)
        {
            foreach (KeyValuePair<string, int> seat in save.LastElectionSeats)
            {
                _lastSeats[seat.Key] = seat.Value;
            }
        }

        UpdatePolls();
    }

    private static CompassSaveData ToCompassSave(PoliticalCompass compass)
    {
        return new CompassSaveData
        {
            Economic = compass.Economic,
            Societal = compass.Societal,
            Authority = compass.Authority,
            Diplomatic = compass.Diplomatic
        };
    }

    private static PoliticalCompass FromCompassSave(CompassSaveData data)
    {
        if (data == null)
        {
            return new PoliticalCompass(0f, 0f, 0f, 0f);
        }

        return new PoliticalCompass(data.Economic, data.Societal, data.Authority, data.Diplomatic);
    }

    private void SyncPolicyStateTags()
    {
        IReadOnlyCollection<string> policyNames = PolicyManager.GetAllPolicyNames();
        foreach (string policyName in policyNames)
        {
            string tag = BuildPolicyActiveTag(policyName);
            if (PolicyManager.IsActive(policyName))
            {
                _worldTags.SetTag(tag, 0);
            }
            else
            {
                _worldTags.RemoveTag(tag);
            }
        }
    }

    private static string BuildPolicyActiveTag(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return "policy_unknown_active";
        }

        string normalized = policyName.Trim().ToLowerInvariant().Replace(' ', '_');
        return $"policy_{normalized}_active";
    }

    private float ComputeEventWeight(SimulationEvent simEvent)
    {
        if (simEvent == null)
        {
            return 0f;
        }

        float weight = Mathf.Max(0f, simEvent.Weight);
        if (simEvent.WeightRules == null || simEvent.WeightRules.Count == 0)
        {
            return weight;
        }

        for (int i = 0; i < simEvent.WeightRules.Count; i++)
        {
            EventWeightRule rule = simEvent.WeightRules[i];
            if (rule == null || !IsWeightRuleSatisfied(rule))
            {
                continue;
            }

            float multiplier = rule.WeightMultiplier <= 0f ? 1f : rule.WeightMultiplier;
            weight = Mathf.Max(0f, weight * multiplier + rule.WeightAdd);
        }

        return Mathf.Max(0f, weight);
    }

    private bool IsWeightRuleSatisfied(EventWeightRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.RequireTag) && !_worldTags.HasTag(rule.RequireTag))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.ForbidTag) && _worldTags.HasTag(rule.ForbidTag))
        {
            return false;
        }

        if (!float.IsNaN(rule.MinUnrest) && NationalUnrest < rule.MinUnrest)
        {
            return false;
        }

        if (!float.IsNaN(rule.MaxUnrest) && NationalUnrest > rule.MaxUnrest)
        {
            return false;
        }

        if (!float.IsNaN(rule.MinStability) && NationalStability < rule.MinStability)
        {
            return false;
        }

        if (!float.IsNaN(rule.MaxStability) && NationalStability > rule.MaxStability)
        {
            return false;
        }

        return true;
    }

    private float CampaignBuff(Party party, DistrictTile tile)
    {
        float buff = 1f;

        if (tile.Density == PopulationDensity.Metro && party.Platform.Societal > 0.25f)
        {
            buff += 0.08f;
        }
        if (tile.Density == PopulationDensity.Rural && party.Platform.Societal < -0.25f)
        {
            buff += 0.08f;
        }
        if (tile.DominantIndustry == IndustryType.HeavyIndustry && party.Platform.Economic < -0.25f)
        {
            buff += 0.07f;
        }
        if (tile.DominantIndustry == IndustryType.Tech && party.Platform.Economic > 0.25f)
        {
            buff += 0.07f;
        }

        if (!string.IsNullOrWhiteSpace(PlayerPartyName) && party.Name == PlayerPartyName)
        {
            buff *= GetCandidateCampaignMultiplier();
        }

        return buff;
    }

    private void InitializeCandidateData()
    {
        _candidateTraits.Clear();
        _candidateBackgrounds.Clear();

        if (DataLoader.TryLoadCandidateTraits(out List<CandidateTraitDefinition> loadedTraits))
        {
            foreach (CandidateTraitDefinition trait in loadedTraits)
            {
                if (trait == null || string.IsNullOrWhiteSpace(trait.Id))
                {
                    continue;
                }

                _candidateTraits[trait.Id] = trait;
            }
        }

        if (_candidateTraits.Count == 0)
        {
            SeedFallbackCandidateTraits();
        }

        if (DataLoader.TryLoadCandidateBackgrounds(out List<CandidateBackgroundDefinition> loadedBackgrounds))
        {
            foreach (CandidateBackgroundDefinition background in loadedBackgrounds)
            {
                if (background == null || string.IsNullOrWhiteSpace(background.Id))
                {
                    continue;
                }

                _candidateBackgrounds[background.Id] = background;
            }
        }

        if (_candidateBackgrounds.Count == 0)
        {
            SeedFallbackCandidateBackgrounds();
        }

        EnsureDefaultCandidateProfile(true);
    }

    private void EnsureDefaultCandidateProfile(bool force = false)
    {
        if (!force && PlayerCandidate != null && !string.IsNullOrWhiteSpace(PlayerCandidate.Name))
        {
            return;
        }

        List<string> traitIds = ParseTraitIds(DefaultCandidateTraitIds);
        if (traitIds.Count == 0)
        {
            foreach (string key in _candidateTraits.Keys)
            {
                traitIds.Add(key);
                if (traitIds.Count >= 2)
                {
                    break;
                }
            }
        }

        PlayerCandidate = CandidateProfile.Build(
            DefaultCandidateName,
            DefaultCandidateBackgroundId,
            traitIds,
            _candidateBackgrounds,
            _candidateTraits);
    }

    public void SetPlayerCandidate(string name, string backgroundId, IList<string> traitIds)
    {
        PlayerCandidate = CandidateProfile.Build(
            name,
            backgroundId,
            traitIds,
            _candidateBackgrounds,
            _candidateTraits);
    }

    public IReadOnlyList<CandidateTraitDefinition> GetAvailableCandidateTraits()
    {
        List<CandidateTraitDefinition> items = new List<CandidateTraitDefinition>(_candidateTraits.Values);
        items.Sort((a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
        return items;
    }

    public IReadOnlyList<CandidateBackgroundDefinition> GetAvailableCandidateBackgrounds()
    {
        List<CandidateBackgroundDefinition> items = new List<CandidateBackgroundDefinition>(_candidateBackgrounds.Values);
        items.Sort((a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
        return items;
    }

    private static List<string> ParseTraitIds(string csv)
    {
        List<string> values = new List<string>();
        if (string.IsNullOrWhiteSpace(csv))
        {
            return values;
        }

        string[] parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                values.Add(part);
            }
        }

        return values;
    }

    private float GetCandidatePolicyCostMultiplier()
    {
        return PlayerCandidate?.PolicyCostMultiplier ?? 1f;
    }

    private float GetCandidatePropagandaCostMultiplier()
    {
        return PlayerCandidate?.PropagandaCostMultiplier ?? 1f;
    }

    private float GetCandidateCampaignMultiplier()
    {
        return PlayerCandidate?.CampaignStrengthMultiplier ?? 1f;
    }

    private float GetCandidatePollingBonus()
    {
        return PlayerCandidate?.PollingBonus ?? 0f;
    }

    private float GetCandidateCapitalGainMultiplier()
    {
        return PlayerCandidate?.CapitalGainMultiplier ?? 1f;
    }

    private float GetCandidateDebtStressMitigation()
    {
        return PlayerCandidate?.DebtStressMitigation ?? 0f;
    }

    private float GetCandidateEventShockMultiplier()
    {
        return PlayerCandidate?.EventShockMultiplier ?? 1f;
    }

    private static CandidateSaveData ToCandidateSave(CandidateProfile profile)
    {
        CandidateProfile p = profile ?? new CandidateProfile();
        return new CandidateSaveData
        {
            Name = p.Name,
            BackgroundId = p.BackgroundId,
            TraitIds = new List<string>(p.TraitIds ?? new List<string>()),
            Charisma = p.Charisma,
            Competence = p.Competence,
            Integrity = p.Integrity,
            Populism = p.Populism
        };
    }

    private void SeedFallbackCandidateTraits()
    {
        _candidateTraits["grassroots"] = new CandidateTraitDefinition
        {
            Id = "grassroots",
            Name = "Grassroots Organizer",
            CampaignStrengthMultiplierDelta = 0.10f,
            PollingBonusDelta = 0.01f,
            CharismaDelta = 0.08f
        };

        _candidateTraits["policy_wonk"] = new CandidateTraitDefinition
        {
            Id = "policy_wonk",
            Name = "Policy Wonk",
            CompetenceDelta = 0.12f,
            PolicyCostMultiplierDelta = -0.07f,
            CapitalGainMultiplierDelta = 0.07f,
            DebtStressMitigationDelta = 0.05f
        };
    }

    private void SeedFallbackCandidateBackgrounds()
    {
        _candidateBackgrounds["activist"] = new CandidateBackgroundDefinition
        {
            Id = "activist",
            Name = "Street Activist",
            CharismaDelta = 0.10f,
            PopulismDelta = 0.08f,
            CampaignStrengthMultiplierDelta = 0.10f
        };

        _candidateBackgrounds["business_exec"] = new CandidateBackgroundDefinition
        {
            Id = "business_exec",
            Name = "Business Executive",
            CompetenceDelta = 0.10f,
            DebtStressMitigationDelta = 0.10f,
            EventShockMultiplierDelta = -0.03f
        };
    }

    private string FindLargestRemainder(Dictionary<string, float> remainders)
    {
        string bestName = string.Empty;
        float bestValue = -999f;

        foreach (KeyValuePair<string, float> kvp in remainders)
        {
            if (kvp.Value > bestValue)
            {
                bestName = kvp.Key;
                bestValue = kvp.Value;
            }
        }

        return bestName;
    }

    private string FindTopSeatParty()
    {
        string topParty = string.Empty;
        int topSeats = -1;

        foreach (KeyValuePair<string, int> kvp in _lastSeats)
        {
            if (kvp.Value > topSeats)
            {
                topParty = kvp.Key;
                topSeats = kvp.Value;
            }
        }

        return topParty == string.Empty ? "Hung Parliament" : topParty;
    }

    private Party FindPartyByName(string name)
    {
        foreach (Party party in _parties)
        {
            if (party.Name == name)
            {
                return party;
            }
        }
        return null;
    }
}
