using Godot;
using PolisGrid.Core;
using System;
using System.Collections.Generic;

public partial class CandidateCreatorController : Control
{
    [Export] public NodePath SimulationManagerPath;
    [Export] public NodePath CandidateNamePath;
    [Export] public NodePath BackgroundOptionPath;
    [Export] public NodePath TraitPrimaryOptionPath;
    [Export] public NodePath TraitSecondaryOptionPath;
    [Export] public NodePath BackgroundDescriptionPath;
    [Export] public NodePath TraitPrimaryDescriptionPath;
    [Export] public NodePath TraitSecondaryDescriptionPath;
    [Export] public NodePath SummaryPath;
    [Export] public NodePath HudRootPath;
    [Export] public NodePath WorldViewPath;

    private SimulationManager _simulationManager;
    private LineEdit _candidateName;
    private OptionButton _backgroundOption;
    private OptionButton _traitPrimaryOption;
    private OptionButton _traitSecondaryOption;
    private Label _backgroundDescription;
    private Label _traitPrimaryDescription;
    private Label _traitSecondaryDescription;
    private RichTextLabel _summary;
    private CanvasItem _hudRoot;
    private CanvasItem _worldView;

    private readonly List<CandidateBackgroundDefinition> _backgrounds = new List<CandidateBackgroundDefinition>();
    private readonly List<CandidateTraitDefinition> _traits = new List<CandidateTraitDefinition>();
    private readonly Dictionary<string, CandidateBackgroundDefinition> _backgroundById = new Dictionary<string, CandidateBackgroundDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CandidateTraitDefinition> _traitById = new Dictionary<string, CandidateTraitDefinition>(StringComparer.OrdinalIgnoreCase);

    public override void _Ready()
    {
        _simulationManager = ResolveNodeOrWarn<SimulationManager>(SimulationManagerPath, nameof(SimulationManagerPath));
        _candidateName = ResolveNodeOrWarn<LineEdit>(CandidateNamePath, nameof(CandidateNamePath));
        _backgroundOption = ResolveNodeOrWarn<OptionButton>(BackgroundOptionPath, nameof(BackgroundOptionPath));
        _traitPrimaryOption = ResolveNodeOrWarn<OptionButton>(TraitPrimaryOptionPath, nameof(TraitPrimaryOptionPath));
        _traitSecondaryOption = ResolveNodeOrWarn<OptionButton>(TraitSecondaryOptionPath, nameof(TraitSecondaryOptionPath));
        _backgroundDescription = ResolveNodeOrWarn<Label>(BackgroundDescriptionPath, nameof(BackgroundDescriptionPath));
        _traitPrimaryDescription = ResolveNodeOrWarn<Label>(TraitPrimaryDescriptionPath, nameof(TraitPrimaryDescriptionPath));
        _traitSecondaryDescription = ResolveNodeOrWarn<Label>(TraitSecondaryDescriptionPath, nameof(TraitSecondaryDescriptionPath));
        _summary = ResolveNodeOrWarn<RichTextLabel>(SummaryPath, nameof(SummaryPath));
        _hudRoot = ResolveNodeOrWarn<CanvasItem>(HudRootPath, nameof(HudRootPath));
        _worldView = ResolveNodeOrWarn<CanvasItem>(WorldViewPath, nameof(WorldViewPath));

        if (_hudRoot != null)
        {
            _hudRoot.Visible = false;
        }

        PopulateOptions();
        RefreshDescriptionsAndSummary();
    }

    public void OnBackgroundSelected(long index)
    {
        RefreshDescriptionsAndSummary();
    }

    public void OnPrimaryTraitSelected(long index)
    {
        RefreshDescriptionsAndSummary();
    }

    public void OnSecondaryTraitSelected(long index)
    {
        RefreshDescriptionsAndSummary();
    }

    public void OnNameChanged(string value)
    {
        RefreshDescriptionsAndSummary();
    }

    public void OnStartPressed()
    {
        if (_simulationManager == null)
        {
            return;
        }

        string name = _candidateName?.Text ?? string.Empty;
        string backgroundId = GetSelectedBackgroundId();

        List<string> selectedTraits = new List<string>();
        string primaryTraitId = GetSelectedTraitId(_traitPrimaryOption);
        string secondaryTraitId = GetSelectedTraitId(_traitSecondaryOption);

        if (!string.IsNullOrWhiteSpace(primaryTraitId))
        {
            selectedTraits.Add(primaryTraitId);
        }

        if (!string.IsNullOrWhiteSpace(secondaryTraitId) &&
            !string.Equals(primaryTraitId, secondaryTraitId, StringComparison.OrdinalIgnoreCase))
        {
            selectedTraits.Add(secondaryTraitId);
        }

        _simulationManager.SetPlayerCandidate(name, backgroundId, selectedTraits);

        if (!_simulationManager.HasStarted)
        {
            _simulationManager.BeginGame();
        }

        if (_hudRoot != null)
        {
            _hudRoot.Visible = true;
        }

        QueueFree();
    }

    private void PopulateOptions()
    {
        if (_simulationManager == null)
        {
            return;
        }

        _backgrounds.Clear();
        _traits.Clear();
        _backgroundById.Clear();
        _traitById.Clear();

        IReadOnlyList<CandidateBackgroundDefinition> backgrounds = _simulationManager.GetAvailableCandidateBackgrounds();
        IReadOnlyList<CandidateTraitDefinition> traits = _simulationManager.GetAvailableCandidateTraits();

        for (int i = 0; i < backgrounds.Count; i++)
        {
            CandidateBackgroundDefinition background = backgrounds[i];
            _backgrounds.Add(background);
            _backgroundById[background.Id] = background;
        }

        for (int i = 0; i < traits.Count; i++)
        {
            CandidateTraitDefinition trait = traits[i];
            _traits.Add(trait);
            _traitById[trait.Id] = trait;
        }

        if (_backgroundOption != null)
        {
            _backgroundOption.Clear();
            for (int i = 0; i < _backgrounds.Count; i++)
            {
                CandidateBackgroundDefinition background = _backgrounds[i];
                _backgroundOption.AddItem(background.Name, i);
                _backgroundOption.SetItemTooltip(i, BuildBackgroundDetail(background));
            }
        }

        if (_traitPrimaryOption != null)
        {
            _traitPrimaryOption.Clear();
            _traitPrimaryOption.AddItem("None", 0);
            _traitPrimaryOption.SetItemTooltip(0, "No primary trait selected.");
        }

        if (_traitSecondaryOption != null)
        {
            _traitSecondaryOption.Clear();
            _traitSecondaryOption.AddItem("None", 0);
            _traitSecondaryOption.SetItemTooltip(0, "No secondary trait selected.");
        }

        for (int i = 0; i < _traits.Count; i++)
        {
            CandidateTraitDefinition trait = _traits[i];
            int index = i + 1;
            _traitPrimaryOption?.AddItem(trait.Name, index);
            _traitSecondaryOption?.AddItem(trait.Name, index);
            _traitPrimaryOption?.SetItemTooltip(index, BuildTraitDetail(trait));
            _traitSecondaryOption?.SetItemTooltip(index, BuildTraitDetail(trait));
        }

        CandidateProfile profile = _simulationManager.PlayerCandidate;
        if (profile == null)
        {
            return;
        }

        if (_candidateName != null)
        {
            _candidateName.Text = profile.Name;
        }

        if (_backgroundOption != null)
        {
            int bgIndex = FindBackgroundIndex(profile.BackgroundId);
            _backgroundOption.Select(bgIndex < 0 ? 0 : bgIndex);
        }

        if (_traitPrimaryOption != null)
        {
            int traitIndex = profile.TraitIds.Count > 0 ? FindTraitIndex(profile.TraitIds[0]) : -1;
            _traitPrimaryOption.Select(traitIndex < 0 ? 0 : traitIndex + 1);
        }

        if (_traitSecondaryOption != null)
        {
            int traitIndex = profile.TraitIds.Count > 1 ? FindTraitIndex(profile.TraitIds[1]) : -1;
            _traitSecondaryOption.Select(traitIndex < 0 ? 0 : traitIndex + 1);
        }
    }

    private void RefreshDescriptionsAndSummary()
    {
        CandidateBackgroundDefinition background = GetSelectedBackground();
        CandidateTraitDefinition primary = GetSelectedTrait(_traitPrimaryOption);
        CandidateTraitDefinition secondary = GetSelectedTrait(_traitSecondaryOption);

        if (_backgroundDescription != null)
        {
            _backgroundDescription.Text = background == null
                ? "Background: none"
                : $"Background: {BuildBackgroundDetail(background)}";
        }

        if (_traitPrimaryDescription != null)
        {
            _traitPrimaryDescription.Text = primary == null
                ? "Primary Trait: none"
                : $"Primary Trait: {BuildTraitDetail(primary)}";
        }

        if (_traitSecondaryDescription != null)
        {
            _traitSecondaryDescription.Text = secondary == null
                ? "Secondary Trait: none"
                : $"Secondary Trait: {BuildTraitDetail(secondary)}";
        }

        if (_summary != null)
        {
            string name = string.IsNullOrWhiteSpace(_candidateName?.Text) ? "Candidate" : _candidateName.Text.Trim();
            string bgName = background?.Name ?? "None";
            string t1 = primary?.Name ?? "None";
            string t2 = secondary?.Name ?? "None";
            CandidateProfile preview = BuildPreviewProfile();
            _summary.Text =
                $"[b]{name}[/b]\n" +
                $"Background: {bgName}\n" +
                $"Traits: {t1}, {t2}\n" +
                BuildColoredModifierLine("Policy Cost", preview?.PolicyCostMultiplier ?? 1f, invertSign: true) + "\n" +
                BuildColoredModifierLine("Campaign", preview?.CampaignStrengthMultiplier ?? 1f) + "\n" +
                BuildColoredModifierLine("Capital Gain", preview?.CapitalGainMultiplier ?? 1f);
        }
    }

    private CandidateProfile BuildPreviewProfile()
    {
        string name = _candidateName?.Text ?? string.Empty;
        string backgroundId = GetSelectedBackgroundId();

        List<string> selectedTraits = new List<string>();
        string primaryTraitId = GetSelectedTraitId(_traitPrimaryOption);
        string secondaryTraitId = GetSelectedTraitId(_traitSecondaryOption);

        if (!string.IsNullOrWhiteSpace(primaryTraitId))
        {
            selectedTraits.Add(primaryTraitId);
        }

        if (!string.IsNullOrWhiteSpace(secondaryTraitId) &&
            !string.Equals(primaryTraitId, secondaryTraitId, StringComparison.OrdinalIgnoreCase))
        {
            selectedTraits.Add(secondaryTraitId);
        }

        return CandidateProfile.Build(name, backgroundId, selectedTraits, _backgroundById, _traitById);
    }

    private CandidateBackgroundDefinition GetSelectedBackground()
    {
        if (_backgroundOption == null)
        {
            return null;
        }

        int index = _backgroundOption.Selected;
        if (index < 0 || index >= _backgrounds.Count)
        {
            return null;
        }

        return _backgrounds[index];
    }

    private string GetSelectedBackgroundId()
    {
        CandidateBackgroundDefinition selected = GetSelectedBackground();
        return selected?.Id ?? string.Empty;
    }

    private CandidateTraitDefinition GetSelectedTrait(OptionButton option)
    {
        if (option == null)
        {
            return null;
        }

        int selected = option.Selected;
        if (selected <= 0)
        {
            return null;
        }

        int index = selected - 1;
        if (index < 0 || index >= _traits.Count)
        {
            return null;
        }

        return _traits[index];
    }

    private string GetSelectedTraitId(OptionButton option)
    {
        return GetSelectedTrait(option)?.Id ?? string.Empty;
    }

    private int FindBackgroundIndex(string id)
    {
        for (int i = 0; i < _backgrounds.Count; i++)
        {
            if (string.Equals(_backgrounds[i].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindTraitIndex(string id)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (string.Equals(_traits[i].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string BuildBackgroundDetail(CandidateBackgroundDefinition background)
    {
        if (background == null)
        {
            return string.Empty;
        }

        return $"{background.Name}: {background.Description}\n" +
            $"Policy Cost {FormatDeltaFromMultiplier(background.PolicyCostMultiplierDelta, invertSign: true)} | " +
            $"Campaign {FormatDeltaFromMultiplier(background.CampaignStrengthMultiplierDelta)} | " +
            $"Capital {FormatDeltaFromMultiplier(background.CapitalGainMultiplierDelta)}";
    }

    private static string BuildTraitDetail(CandidateTraitDefinition trait)
    {
        if (trait == null)
        {
            return string.Empty;
        }

        return $"{trait.Name}: {trait.Description}\n" +
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

        return $"[color={color}]{label}: {delta:+0;-0;0}%[/color]";
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

    private T ResolveNodeOrWarn<T>(NodePath path, string fieldName) where T : Node
    {
        if (path == null || path.IsEmpty)
        {
            GD.PushWarning($"CandidateCreator wiring: '{fieldName}' is empty.");
            return null;
        }

        T node = GetNodeOrNull<T>(path);
        if (node == null)
        {
            GD.PushWarning($"CandidateCreator wiring: '{fieldName}' path not found ({path}).");
        }

        return node;
    }
}
