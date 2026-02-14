using System;
using System.Collections.Generic;

namespace PolisGrid.Core
{
    [Serializable]
    public class SaveGameData
    {
        public int Version { get; set; } = 1;
        public GlobalStatsSaveData Global { get; set; } = new GlobalStatsSaveData();
        public List<TileSaveData> Grid { get; set; } = new List<TileSaveData>();
        public List<PartySaveData> Parties { get; set; } = new List<PartySaveData>();
        public List<PolicyStateSaveData> PolicyStates { get; set; } = new List<PolicyStateSaveData>();
        public List<FactionSaveData> Factions { get; set; } = new List<FactionSaveData>();
        public List<FactionActionCooldownSaveData> FactionActionCooldowns { get; set; } = new List<FactionActionCooldownSaveData>();
        public List<WorldTagSaveData> WorldTags { get; set; } = new List<WorldTagSaveData>();
        public List<float> StabilityHistory { get; set; } = new List<float>();
        public List<float> TurnoutHistory { get; set; } = new List<float>();
        public List<float> TreasuryHistory { get; set; } = new List<float>();
        public List<float> CapitalHistory { get; set; } = new List<float>();
        public List<string> ActionHistory { get; set; } = new List<string>();
        public Dictionary<string, int> LastElectionSeats { get; set; } = new Dictionary<string, int>();
    }

    [Serializable]
    public class GlobalStatsSaveData
    {
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int Seed { get; set; }
        public int TurnCounter { get; set; }
        public float NationalStability { get; set; }
        public float NationalUnrest { get; set; }
        public float NationalTurnout { get; set; }
        public float Treasury { get; set; }
        public float PoliticalCapital { get; set; }
        public float StabilityShock { get; set; }
        public int HighUnrestTurnStreak { get; set; }
        public string LastEventTitle { get; set; }
        public string LastEventDescription { get; set; }
        public string CurrentGovernmentParty { get; set; }
        public string PlayerPartyName { get; set; }
        public bool PlayerInPower { get; set; }
        public bool IsGameOver { get; set; }
        public string GameOverReason { get; set; }
        public string GameOverDetail { get; set; }
        public float PollMarginOfError { get; set; }
        public int PropagandaTurnsRemaining { get; set; }
        public float BudgetServices { get; set; }
        public float BudgetMilitary { get; set; }
        public float BudgetInfrastructure { get; set; }
        public string LastTurnDigest { get; set; }
        public CandidateSaveData Candidate { get; set; } = new CandidateSaveData();
        public CompassSaveData GovernmentPolicy { get; set; } = new CompassSaveData();
        public CompassSaveData NationalVoterCenter { get; set; } = new CompassSaveData();
    }

    [Serializable]
    public class CandidateSaveData
    {
        public string Name { get; set; }
        public string BackgroundId { get; set; }
        public List<string> TraitIds { get; set; } = new List<string>();
        public float Charisma { get; set; }
        public float Competence { get; set; }
        public float Integrity { get; set; }
        public float Populism { get; set; }
    }

    [Serializable]
    public class TileSaveData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Density { get; set; }
        public int DominantIndustry { get; set; }
        public float Stability { get; set; }
        public float Turnout { get; set; }
        public float ExternalStabilityPressure { get; set; }
        public float MediaAccess { get; set; }
        public int LocalIssue { get; set; }
        public CompassSaveData AverageIdeology { get; set; } = new CompassSaveData();
        public List<BlocSaveData> Blocs { get; set; } = new List<BlocSaveData>();
    }

    [Serializable]
    public class BlocSaveData
    {
        public string Name { get; set; }
        public int Population { get; set; }
        public int Category { get; set; }
        public CompassSaveData Ideology { get; set; } = new CompassSaveData();
        public float Happiness { get; set; }
        public float TurnoutChance { get; set; }
        public float Anger { get; set; }
        public float Hope { get; set; }
        public float LastIdeologyHappinessPenalty { get; set; }
        public float LastIssueHappinessPenalty { get; set; }
        public float LastPolicyHappinessModifier { get; set; }
        public float LastPolicyTurnoutModifier { get; set; }
    }

    [Serializable]
    public class PartySaveData
    {
        public string Name { get; set; }
        public string ColorHtml { get; set; }
        public CompassSaveData Platform { get; set; } = new CompassSaveData();
        public float Opportunism { get; set; }
    }

    [Serializable]
    public class PolicyStateSaveData
    {
        public string Name { get; set; }
        public bool TargetActive { get; set; }
        public float CurrentStrength { get; set; }
        public float StepPerTurn { get; set; }
    }

    [Serializable]
    public class FactionSaveData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Approval { get; set; }
        public string CurrentDemand { get; set; }
    }

    [Serializable]
    public class FactionActionCooldownSaveData
    {
        public string ActionId { get; set; }
        public int RemainingTurns { get; set; }
    }

    [Serializable]
    public class WorldTagSaveData
    {
        public string Tag { get; set; }
        public int RemainingTurns { get; set; }
    }

    [Serializable]
    public class CompassSaveData
    {
        public float Economic { get; set; }
        public float Societal { get; set; }
        public float Authority { get; set; }
        public float Diplomatic { get; set; }
    }
}
