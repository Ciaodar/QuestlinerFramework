using System.Linq;
using Questliner.Shared.Models;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Questliner.Framework.Engine
{
    public static class ConditionEvaluator
    {
        /// <summary>
        /// Evaluates a ConditionModel to determine if a quest stage should progress.
        /// </summary>
        public static bool IsConditionMet(ConditionModel condition, DynamicQuest? quest)
        {
            if (condition.ConditionType.StartsWith("!"))
            {
                var copy = new ConditionModel { ConditionType = condition.ConditionType.Substring(1), TargetAlias = condition.TargetAlias, ObjectId = condition.ObjectId, Amount = condition.Amount, StringValue = condition.StringValue };
                return !IsConditionMet(copy, quest);
            }
            switch (condition.ConditionType)
            {
                case "HasItem":
                {
                    ItemObject? item = Campaign.Current.ObjectManager.GetObject<ItemObject>(condition.ObjectId);
                    if (item == null) return false;
                    return MobileParty.MainParty.ItemRoster.GetItemNumber(item) >= condition.Amount;
                }
                case "StageTimeout":
                {
                    return quest != null && TaleWorlds.CampaignSystem.CampaignTime.Now.ToDays - quest.StageStartTime.ToDays >= condition.Amount;
                }
                case "HasTroopCount":
                {
                    return TaleWorlds.CampaignSystem.Party.MobileParty.MainParty.MemberRoster.TotalManCount >= condition.Amount;
                }
                case "HasGold":
                {
                    return Hero.MainHero.Gold >= condition.Amount;
                }

                case "TalkedTo":
                {
                    // Relies on EventDispatcher setting this flag when a conversation ends
                    return quest != null && quest.GetFlag($"TalkedTo_{condition.TargetAlias}");
                }
                case "HasPledgedAllegiance":
                {
                    Hero? target = quest?.GetTargetHero(condition.TargetAlias);
                    return target != null && Hero.MainHero.Clan.Kingdom != null && Hero.MainHero.Clan.Kingdom == target.Clan?.Kingdom;
                }
                case "TargetIsDead":
                {
                    Hero? target = quest?.GetTargetHero(condition.TargetAlias);
                    bool isDead = target != null && (!target.IsAlive || (quest != null && quest.GetFlag($"Killed_{condition.TargetAlias}")));
                    return isDead;
                }
                case "TargetIsAlive":
                {
                    Hero? target = quest?.GetTargetHero(condition.TargetAlias);
                    return target != null && target.IsAlive;
                }
                case "BattleResolvedAs":
                {
                    if (quest == null) return false;
                    string result = quest.GetString($"BattleResult_{condition.TargetAlias}");
                    return result.Equals(condition.StringValue, System.StringComparison.OrdinalIgnoreCase);
                }
                case "TargetIsPrisoner":
                {
                    Hero? target = quest?.GetTargetHero(condition.TargetAlias);
                    return target != null && target.IsPrisoner;
                }
                case "TargetRelation":
                {
                    Hero? target = quest?.GetTargetHero(condition.TargetAlias);
                    return target != null && target.GetRelation(Hero.MainHero) >= condition.Amount;
                }
                case "PlayerHasFief":
                {
                    return Clan.PlayerClan.Fiefs.Count >= condition.Amount;
                }
                case "PlayerClanTier":
                case "ClanTier":
                {
                    return Clan.PlayerClan.Tier >= condition.Amount;
                }
                case "PartySize":
                {
                    return MobileParty.MainParty.MemberRoster.TotalManCount >= condition.Amount;
                }
                case "CheckFlag":
                {
                    return quest != null && quest.GetFlag(condition.ObjectId) == (condition.Amount > 0);
                }
                case "IsStageActive":
                {
                    return quest != null && quest.CurrentStageId == condition.ObjectId;
                }
                case "IsQuestActive":
                {
                    bool active = TaleWorlds.CampaignSystem.Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Any(q => q.IsOngoing && q.Model?.Id == condition.ObjectId);
                    return (condition.Amount > 0) ? active : !active;
                }
                                case "IsQuestCompleted":
                {
                    bool completed = Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance != null && 
                                     Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance.CompletedQuestIds.Contains(condition.ObjectId);
                    
                    if (!completed)
                    {
                        var qm = TaleWorlds.CampaignSystem.Campaign.Current.QuestManager;
                        var q = qm.Quests.FirstOrDefault(x => x.StringId.Contains(condition.ObjectId));
                        if (q != null && !q.IsOngoing) completed = true;
                    }
                    
                    return (condition.Amount > 0) ? completed : !completed;
                }
                case "IsVassalOfTargetFaction":
                {
                    var targetHero = quest?.GetTargetHero(condition.TargetAlias);
                    if (targetHero == null) return false;
                    var playerClan = TaleWorlds.CampaignSystem.Clan.PlayerClan;
                    bool isVassal = playerClan.Kingdom != null && playerClan.Kingdom == targetHero.Clan?.Kingdom && playerClan.IsUnderMercenaryService == false;
                    return condition.Amount > 0 ? isVassal : !isVassal;
                }
                case "DefeatedParties":
                {
                    return quest != null && quest.GetCounter($"Defeated_{condition.TargetAlias}_Parties") >= condition.Amount;
                }
                case "HasPrisoner":
                {
                    var roster = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty.PrisonRoster;
                    int count = 0;
                    foreach (var element in roster.GetTroopRoster())
                    {
                        if (element.Character.StringId == condition.ObjectId)
                            count += element.Number;
                    }
                    return count >= condition.Amount;
                }
                case "GlobalFlag":
                {
                    bool flagValue = GlobalStateManager.Instance.GetFlag(condition.ObjectId);
                    // If Amount > 0 it expects true, else it expects false. But we also support Operator in future, for now Amount > 0 -> true.
                    // Wait, the XML says <Condition Type="GlobalFlag" Key="..." Operator="==" Value="true" />
                    // But our engine currently parses condition.Amount. If we want it simple, Amount=1 means true, Amount=0 means false. 
                    // Let's use Amount:
                    bool expected = condition.Amount > 0;
                    return flagValue == expected;
                }
                case "ItemCountInInventory":
                {
                    ItemObject? item = Campaign.Current.ObjectManager.GetObject<ItemObject>(condition.ObjectId);
                    if (item == null) return false;
                    return MobileParty.MainParty.ItemRoster.GetItemNumber(item) >= condition.Amount;
                }
                case "SkillCheck":
                {
                    // ObjectId should be the skill string id (e.g., "Charm", "Roguery")
                    var skill = Campaign.Current.ObjectManager.GetObject<TaleWorlds.Core.SkillObject>(condition.ObjectId);
                    if (skill == null) return false;
                    return Hero.MainHero.GetSkillValue(skill) >= condition.Amount;
                }
                // Future expansions: HasTroop, SettlementProsperity...
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the numerical current progress of a condition, used for Discrete/Progress Logs.
        /// </summary>
        public static int GetCurrentProgress(ConditionModel condition, DynamicQuest quest)
        {
            switch (condition.ConditionType)
            {
                case "HasItem":
                {
                    ItemObject? item = MBObjectManager.Instance.GetObject<ItemObject>(condition.ObjectId);
                    return item != null ? MobileParty.MainParty.ItemRoster.GetItemNumber(item) : 0;
                }
                case "HasGold":
                    return Hero.MainHero.Gold;
                case "PlayerHasFief":
                    return Clan.PlayerClan.Fiefs.Count;
                case "PartySize":
                    return MobileParty.MainParty.MemberRoster.TotalManCount;
                case "DefeatedParties":
                    return quest != null ? quest.GetCounter($"Defeated_{condition.TargetAlias}_Parties") : 0;
                default:
                    // If it's a boolean check (TalkedTo, CheckFlag, TargetIsDead), just return 1 if true, 0 if false.
                    return IsConditionMet(condition, quest) ? condition.Amount : 0;
            }
        }
    }
}
