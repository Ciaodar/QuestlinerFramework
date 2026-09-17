using Questliner.Shared.Models;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Questliner.Framework.Engine
{
    public static class ActionExecutor
    {
        /// <summary>
        /// Executes a specific ActionModel parsed from the Questliner XML.
        /// </summary>
        public static void Execute(ActionModel action, DynamicQuest? quest)
        {
            switch (action.ActionType)
            {
                case "GiveItem":
                {
                    ItemObject? giveItem = MBObjectManager.Instance.GetObject<ItemObject>(action.ObjectId);
                    if (giveItem != null)
                    {
                        MobileParty.MainParty.ItemRoster.AddToCounts(giveItem, action.Amount);
                    }
                    break;
                }
                case "TakeItem":
                {
                    ItemObject? takeItem = MBObjectManager.Instance.GetObject<ItemObject>(action.ObjectId);
                    if (takeItem != null)
                    {
                        MobileParty.MainParty.ItemRoster.AddToCounts(takeItem, -action.Amount);
                    }
                    break;
                }
                case "AddGold":
                {
                    int goldToAdd = action.Amount;
                    if (action.ObjectId == "PigeonGoldMath")
                    {
                        int tenPct = (int)(Hero.MainHero.Gold * 0.10f);
                        goldToAdd = tenPct > 1000 ? tenPct : 1000;
                    }
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goldToAdd);
                    break;
                }
                case "TakeGold":
                {
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, action.Amount);
                    break;
                }
                case "LoseGoldPercentage":
                {
                    int amountToLose = (int)(Hero.MainHero.Gold * (action.Amount / 100f));
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amountToLose);
                    break;
                }
                case "RemovePrisoner":
                {
                    var troop = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<TaleWorlds.CampaignSystem.CharacterObject>(action.ObjectId);
                    if (troop != null)
                    {
                        TaleWorlds.CampaignSystem.Party.MobileParty.MainParty.PrisonRoster.AddToCounts(troop, -action.Amount);
                    }
                    break;
                }
                case "ChangeRelation":
                    var targetHero = quest?.GetTargetHero(action.TargetAlias);
                    if (targetHero != null)
                    {
                        TaleWorlds.CampaignSystem.Actions.ChangeRelationAction.ApplyPlayerRelation(targetHero, action.Amount);
                    }
                    break;
                case "ChangeStage":
                    quest?.TransitionToStage(action.ObjectId);
                    break;
                case "ChangeOtherQuestStage":
                {
                    var otherQuest = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(q => q.Model?.Id == action.ObjectId && q.IsOngoing);
                    if (otherQuest != null)
                    {
                        otherQuest.TransitionToStage(action.TargetAlias);
                    }
                    break;
                }
                case "FailQuest":
                    quest?.ForceFail();
                    break;
                case "CompleteQuest":
                    quest?.ForceSuccess();
                    break;
                case "LeaveEncounter":
                    if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null)
                    {
                        TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.LeaveEncounter = true;
                    }
                    break;
                    case "StartQuest":
                    var newQuestModel = Questliner.Framework.Services.QuestModuleScanner.LoadedQuests.FirstOrDefault(q => q.Id == action.ObjectId);
                    if (newQuestModel != null)
                    {
                        var duration = TaleWorlds.CampaignSystem.CampaignTime.DaysFromNow(newQuestModel.TimeLimitInDays > 0 ? newQuestModel.TimeLimitInDays : 9999);
                        var newQ = new DynamicQuest(newQuestModel, null, duration);
                        
                        foreach(var res in newQuestModel.TargetResolvers)
                        {
                            if (res.TargetType == "Settlement")
                            {
                                var s = Questliner.Framework.Engine.TargetResolverService.ResolveSettlement(res, newQ);
                                if (s != null) newQ.TargetSettlements[res.Alias] = s;
                            }
                            else
                            {
                                var h = Questliner.Framework.Engine.TargetResolverService.ResolveHero(res, newQ);
                                if (h != null) newQ.TargetHeroes[res.Alias] = h;
                            }
                        }
                        newQ.StartQuest();
                    }
                    break;
                case "SpawnBanditParty":
                    var looterTemplate = TaleWorlds.CampaignSystem.Campaign.Current.ObjectManager.GetObject<TaleWorlds.CampaignSystem.Party.PartyTemplateObject>("looters_template");
                    
                    if (looterTemplate != null)
                    {
                        var party = TaleWorlds.CampaignSystem.Party.MobileParty.CreateParty("ql_spawned_looters_" + TaleWorlds.Core.MBRandom.RandomInt(10000), null);
                        
                        party.ActualClan = TaleWorlds.CampaignSystem.Clan.BanditFactions.FirstOrDefault(c => c.StringId == "looters");
                        party.InitializeMobilePartyAroundPosition(looterTemplate, TaleWorlds.CampaignSystem.Party.MobileParty.MainParty.Position, 1f, 0.5f);
                        
                        if (quest != null && !string.IsNullOrEmpty(action.TargetAlias))
                        {
                            quest.TargetParties[action.TargetAlias] = party;
                        }
                        
                        party.Party.SetCustomName(new TaleWorlds.Localization.TextObject("{=!}Looters"));

                        // Set troop count
                        int targetAmount = action.Amount > 0 ? action.Amount : 15;
                        var looterCharacter = TaleWorlds.CampaignSystem.Campaign.Current.ObjectManager.GetObject<TaleWorlds.CampaignSystem.CharacterObject>("looter");
                        if (looterCharacter != null && party.MemberRoster.TotalManCount < targetAmount)
                        {
                            party.MemberRoster.AddToCounts(looterCharacter, targetAmount - party.MemberRoster.TotalManCount);
                        }
                        
                        // Prevent AI from attacking them for 24 hours
                        party.IgnoreByOtherPartiesTill(TaleWorlds.CampaignSystem.CampaignTime.HoursFromNow(24));
                        
                        // Force them to engage the player
                        
                        party.Ai.SetDoNotMakeNewDecisions(true);
                        
                        // Force attack player
                        party.Ai.SetDoNotMakeNewDecisions(true);
                        party.SetMoveEngageParty(TaleWorlds.CampaignSystem.Party.MobileParty.MainParty, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                    }
                    break;
                case "MakeMet":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null)
                    {
                        target.SetHasMet();
                    }
                    break;
                }
                case "ChangeRenown":
                {
                    Clan.PlayerClan.AddRenown(action.Amount);
                    break;
                }
                case "ChangeInfluence":
                {
                    ChangeClanInfluenceAction.Apply(Clan.PlayerClan, action.Amount);
                    break;
                }
                case "DeclareWar":
                {
                    Hero? target = quest.GetTargetHero(action.TargetAlias);
                    if (target?.Clan?.MapFaction != null)
                        DeclareWarAction.ApplyByDefault(Clan.PlayerClan.MapFaction, target.Clan.MapFaction);
                    break;
                }
                case "MakePeace":
                {
                    Hero? target = quest.GetTargetHero(action.TargetAlias);
                    if (target?.Clan?.MapFaction != null)
                        MakePeaceAction.Apply(Clan.PlayerClan.MapFaction, target.Clan.MapFaction);
                    break;
                }
                case "SetPrisoner":
                {
                    Hero? target = quest.GetTargetHero(action.TargetAlias);
                    if (target != null) TakePrisonerAction.Apply(PartyBase.MainParty, target);
                    break;
                }
                case "DamageHero":
                {
                    Hero? target = quest.GetTargetHero(action.TargetAlias);
                    if (target != null) target.HitPoints -= action.Amount;
                    break;
                }
                case "SetFlag":
                {
                    quest.SetFlag(action.ObjectId, action.Amount > 0);
                    break;
                }
                // Future expansions: SpawnParty, DeclareWar, TriggerSequelQuest, etc.
            }
        }
    }
}
