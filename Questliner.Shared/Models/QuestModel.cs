using System.Collections.Generic;
using System.Xml.Serialization;

namespace Questliner.Shared.Models
{
    [XmlRoot("Questline")]
    public class QuestModel
    {
        [XmlAttribute("Id")]
        public string Id { get; set; } = string.Empty;

        [XmlAttribute("Title")]
        public string Title { get; set; } = string.Empty;

        [XmlAttribute("TimeLimitInDays")]
        public int TimeLimitInDays { get; set; } = -1;

        [System.Xml.Serialization.XmlIgnore]
        public string SourceModuleId { get; set; } = string.Empty;

        [XmlArray("TargetResolvers")]
        [XmlArrayItem("Resolver")]
        public List<TargetResolverModel> TargetResolvers { get; set; } = new List<TargetResolverModel>();

        [XmlArray("Prerequisites")]
        [XmlArrayItem("Condition")]
        public List<ConditionModel> Prerequisites { get; set; } = new List<ConditionModel>();

        [XmlArray("Triggers")]
        [XmlArrayItem("Trigger")]
        public List<TriggerModel> Triggers { get; set; } = new List<TriggerModel>();

        [XmlArray("Stages")]
        [XmlArrayItem("Stage")]
        public List<StageModel> Stages { get; set; } = new List<StageModel>();

        [XmlArray("CustomDialogs")]
        [XmlArrayItem("DialogNode")]
        public List<CustomDialogModel> CustomDialogs { get; set; } = new List<CustomDialogModel>();

        [XmlArray("GameMenuOptions")]
        [XmlArrayItem("GameMenuOption")]
        public List<GameMenuOptionModel> GameMenuOptions { get; set; } = new List<GameMenuOptionModel>();
    }

    public class GameMenuOptionModel
    {
        [XmlAttribute("Id")]
        public string Id { get; set; } = string.Empty;

        [XmlAttribute("TargetMenu")]
        public string TargetMenu { get; set; } = string.Empty;

        [XmlAttribute("Text")]
        public string Text { get; set; } = string.Empty;

        [XmlAttribute("IsLeave")]
        public bool IsLeave { get; set; } = false;

        [XmlArray("Conditions")]
        [XmlArrayItem("Condition")]
        public List<ConditionModel> Conditions { get; set; } = new List<ConditionModel>();

        [XmlArray("Consequences")]
        [XmlArrayItem("Action")]
        public List<ActionModel> Consequences { get; set; } = new List<ActionModel>();
    }

    public class TargetResolverModel
    {
        [XmlAttribute("Alias")]
        public string Alias { get; set; } = string.Empty; // e.g., "Giver", "TargetB"

        [XmlAttribute("TargetType")]
        public string TargetType { get; set; } = "Hero"; // Hero, Settlement, MobileParty

        [XmlAttribute("Filters")]
        public string Filters { get; set; } = string.Empty; // e.g., "Lord, SameAsGiverFaction, !PlayerClan"

        [XmlAttribute("RequiredSettlementAlias")]
        public string RequiredSettlementAlias { get; set; } = string.Empty;

        [XmlAttribute("Selector")]
        public string Selector { get; set; } = "Random"; // Random, NearestTown, etc.

        [XmlAttribute("SpecificId")]
        public string SpecificId { get; set; } = string.Empty; // Used if exact ID is needed (e.g. "poros")
    }

    public class TriggerModel
    {
        [XmlAttribute("EventType")]
        public string EventType { get; set; } = "OnConversation"; // OnConversation, OnSettlementEntered, OnHeroPrisonerTaken

        [XmlAttribute("TargetAlias")]
        public string TargetAlias { get; set; } = string.Empty;
    }

    public class StageModel
    {
        [XmlAttribute("Id")]
        public string Id { get; set; } = string.Empty;

        [XmlElement("LogText")]
        public string LogText { get; set; } = string.Empty;

        [XmlAttribute("LogType")]
        public string LogType { get; set; } = "Simple"; // Simple, Discrete

        [XmlAttribute("TargetAmount")]
        public int TargetAmount { get; set; } = 1;

        [XmlAttribute("IsSuccessNode")]
        public bool IsSuccessNode { get; set; } = false;

        [XmlAttribute("IsFailNode")]
        public bool IsFailNode { get; set; } = false;

        [XmlAttribute("NextStageId")]
        public string NextStageId { get; set; } = string.Empty;

        [XmlAttribute("TimeoutInDays")]
        public int TimeoutInDays { get; set; } = 0; // 0 means no timeout
        
        [XmlAttribute("TimeoutInHours")]
        public int TimeoutInHours { get; set; } = 0; // 0 means no timeout

        [XmlArray("OnEnterActions")]
        [XmlArrayItem("Action")]
        public List<ActionModel> OnEnterActions { get; set; } = new List<ActionModel>();

        [XmlArray("Conditions")]
        [XmlArrayItem("Condition")]
        public List<ConditionModel> Conditions { get; set; } = new List<ConditionModel>();

        [XmlArray("OnExitActions")]
        [XmlArrayItem("Action")]
        public List<ActionModel> OnExitActions { get; set; } = new List<ActionModel>();

        [XmlArray("OnTimeoutActions")]
        [XmlArrayItem("Action")]
        public List<ActionModel> OnTimeoutActions { get; set; } = new List<ActionModel>();
    }

    public class ActionModel
    {
        [XmlAttribute("Type")]
        public string ActionType { get; set; } = string.Empty; // GiveItem, TakeItem, AddGold, SpawnBanditParty, ChangeRelation

        [XmlAttribute("TargetAlias")]
        public string TargetAlias { get; set; } = string.Empty; // e.g., who gets the relation change

        [XmlAttribute("ObjectId")]
        public string ObjectId { get; set; } = string.Empty; // Item ID (e.g. "cow"), Troop ID

        [XmlAttribute("Amount")]
        public int Amount { get; set; } = 1;

        [XmlAttribute("StringValue")]
        public string StringValue { get; set; } = string.Empty;
    }

    public class ConditionModel
    {
        [XmlAttribute("Type")]
        public string ConditionType { get; set; } = string.Empty; // TalkedTo, HasItem, TargetIsDead, PlayerHasFief

        [XmlAttribute("TargetAlias")]
        public string TargetAlias { get; set; } = string.Empty;

        [XmlAttribute("ObjectId")]
        public string ObjectId { get; set; } = string.Empty;

        [XmlAttribute("Amount")]
        public int Amount { get; set; } = 1;
    }

    public class CustomDialogModel
    {
        [XmlAttribute("Type")]
        public string Type { get; set; } = "Npc"; // Npc, Player

        [XmlAttribute("Id")]
        public string Id { get; set; } = string.Empty; // Must be unique per quest

        [XmlAttribute("TargetAlias")]
        public string TargetAlias { get; set; } = string.Empty; // Who is this dialogue linked to? e.g., "Giver", "NobleTarget"

        [XmlAttribute("SpeakerStringId")]
        public string SpeakerStringId { get; set; } = string.Empty; // e.g. "barber" for generic generic NPCs

        [XmlAttribute("RequiredSettlementAlias")]
        public string RequiredSettlementAlias { get; set; } = string.Empty; // Requires the player to be in this specific tracked settlement

        [XmlAttribute("InputToken")]
        public string InputToken { get; set; } = string.Empty; // e.g. "ql_quest_discuss_finish" or a custom state

        [XmlAttribute("OutputToken")]
        public string OutputToken { get; set; } = string.Empty; // e.g. "close_window" or a custom state

        [XmlElement("Text")]
        public string Text { get; set; } = string.Empty;

        [XmlArray("Conditions")]
        [XmlArrayItem("Condition")]
        public List<ConditionModel> Conditions { get; set; } = new List<ConditionModel>();

        [XmlArray("Consequences")]
        [XmlArrayItem("Action")]
        public List<ActionModel> Consequences { get; set; } = new List<ActionModel>();
    }
}
