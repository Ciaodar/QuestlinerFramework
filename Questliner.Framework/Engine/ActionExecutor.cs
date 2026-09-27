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
                case "DestroyEncounteredParty":
                    if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null && TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.EncounteredMobileParty != null)
                    {
                        var mobileParty = TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.EncounteredMobileParty;
                        TaleWorlds.CampaignSystem.Campaign.Current.ConversationManager.ConversationEndOneShot += () =>
                        {
                            if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null)
                            {
                                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Finish(true);
                            }
                            if (TaleWorlds.CampaignSystem.Campaign.Current.CurrentMenuContext != null)
                            {
                                TaleWorlds.CampaignSystem.GameMenus.GameMenu.ExitToLast();
                            }
                            TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(null, mobileParty);
                        };
                    }
                    break;

                case "DestroyParty":
                {
                    if (quest != null && quest.TargetParties.TryGetValue(action.TargetAlias, out var partyToDestroy))
                    {
                        if (partyToDestroy != null && partyToDestroy.IsActive)
                        {
                            TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(null, partyToDestroy);
                        }
                    }
                    break;
                }

                case "SpawnBanditParty":
                    string templateId = string.IsNullOrEmpty(action.PartyTemplateId) ? "looters_template" : action.PartyTemplateId;
                    string clanId = templateId.Replace("_template", ""); // e.g. looters, sea_raiders
                    
                    var looterTemplate = TaleWorlds.CampaignSystem.Campaign.Current.ObjectManager.GetObject<TaleWorlds.CampaignSystem.Party.PartyTemplateObject>(templateId);
                    
                    if (looterTemplate != null)
                    {
                        var party = TaleWorlds.CampaignSystem.Party.MobileParty.CreateParty("ql_spawned_" + clanId + "_" + TaleWorlds.Core.MBRandom.RandomInt(10000), null);
                        
                        party.ActualClan = TaleWorlds.CampaignSystem.Clan.BanditFactions.FirstOrDefault(c => c.StringId == clanId) ?? TaleWorlds.CampaignSystem.Clan.BanditFactions.FirstOrDefault(c => c.StringId == "looters");
                        
                        TaleWorlds.CampaignSystem.CampaignVec2 spawnPosition = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty.Position;
                        if (quest != null && !string.IsNullOrEmpty(action.ObjectId))
                        {
                            var targetSettlement = quest.GetTargetSettlement(action.ObjectId);
                            if (targetSettlement != null)
                            {
                                spawnPosition = targetSettlement.GatePosition;
                            }
                            else
                            {
                                var targetParty = quest.TargetParties.TryGetValue(action.ObjectId, out var p) ? p : null;
                                if (targetParty != null && targetParty.IsActive)
                                {
                                    spawnPosition = targetParty.Position;
                                }
                            }
                        }

                        party.InitializeMobilePartyAroundPosition(looterTemplate, spawnPosition, action.SpawnRadius > 0 ? action.SpawnRadius : 2f, action.SpawnDistance > 0 ? action.SpawnDistance : 1f);
                        
                        if (quest != null && !string.IsNullOrEmpty(action.TargetAlias))
                        {
                            quest.TargetParties[action.TargetAlias] = party;
                        }
                        
                        string partyName = "{=!}Bandits";
                        if (!string.IsNullOrEmpty(action.StringValue) && 
                            !action.StringValue.Equals("Hold", System.StringComparison.OrdinalIgnoreCase) &&
                            !action.StringValue.Equals("PatrolAroundSettlement", System.StringComparison.OrdinalIgnoreCase) &&
                            !action.StringValue.Equals("GoToSettlement", System.StringComparison.OrdinalIgnoreCase) &&
                            !action.StringValue.Equals("EngagePlayer", System.StringComparison.OrdinalIgnoreCase))
                        {
                            partyName = action.StringValue;
                        }
                        else if (!string.IsNullOrEmpty(action.TargetAlias))
                        {
                            partyName = action.TargetAlias;
                        }
                        party.Party.SetCustomName(new TaleWorlds.Localization.TextObject(partyName));

                        // Set troop count
                        int targetAmount = action.Amount > 0 ? action.Amount : 15;
                        TaleWorlds.CampaignSystem.CharacterObject? fillCharacter = null;
                        if (looterTemplate.Stacks != null && looterTemplate.Stacks.Count > 0)
                        {
                            fillCharacter = looterTemplate.Stacks[0].Character;
                        }
                        if (fillCharacter == null)
                        {
                            fillCharacter = TaleWorlds.CampaignSystem.Campaign.Current.ObjectManager.GetObject<TaleWorlds.CampaignSystem.CharacterObject>("looter");
                        }

                        if (fillCharacter != null && party.MemberRoster.TotalManCount < targetAmount)
                        {
                            party.MemberRoster.AddToCounts(fillCharacter, targetAmount - party.MemberRoster.TotalManCount);
                        }
                        
                        // Prevent AI from attacking them for 24 hours
                        party.IgnoreByOtherPartiesTill(TaleWorlds.CampaignSystem.CampaignTime.HoursFromNow(24));
                        
                        party.Ai.SetDoNotMakeNewDecisions(true);

                        // AI Movement
                        string aiBehavior = action.StringValue ?? "";
                        if (aiBehavior.Equals("Hold", System.StringComparison.OrdinalIgnoreCase))
                        {
                            party.SetMoveModeHold();
                        }
                        else if (aiBehavior.Equals("PatrolAroundSettlement", System.StringComparison.OrdinalIgnoreCase) && quest != null && !string.IsNullOrEmpty(action.ObjectId))
                        {
                            var targetSettlement = quest.GetTargetSettlement(action.ObjectId);
                            if (targetSettlement != null)
                                party.SetMovePatrolAroundSettlement(targetSettlement, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);
                            else
                                party.SetMoveModeHold();
                        }
                        else if (aiBehavior.Equals("GoToSettlement", System.StringComparison.OrdinalIgnoreCase) && quest != null && !string.IsNullOrEmpty(action.ObjectId))
                        {
                            var targetSettlement = quest.GetTargetSettlement(action.ObjectId);
                            if (targetSettlement != null)
                                party.SetMoveGoToSettlement(targetSettlement, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);
                            else
                                party.SetMoveModeHold();
                        }
                        else
                        {
                            // Default: engage player
                            party.SetMoveEngageParty(TaleWorlds.CampaignSystem.Party.MobileParty.MainParty, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                        }
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
                case "ChangeCrimeRating":
                {
                    IFaction? targetFaction = null;
                    if (!string.IsNullOrEmpty(action.TargetAlias) && quest != null)
                    {
                        var hero = quest.GetTargetHero(action.TargetAlias);
                        var settlement = quest.GetTargetSettlement(action.TargetAlias);
                        var party = quest.GetTargetParty(action.TargetAlias);
                        targetFaction = hero?.MapFaction ?? settlement?.MapFaction ?? party?.MapFaction;
                    }
                    if (targetFaction == null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        targetFaction = Campaign.Current.Factions.FirstOrDefault(f => f.StringId == action.ObjectId);
                    }

                    if (targetFaction != null)
                    {
                        ChangeCrimeRatingAction.Apply(targetFaction, action.Amount, true);
                    }

                    var currentFaction = TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement?.MapFaction ?? Hero.MainHero.CurrentSettlement?.MapFaction;
                    if (currentFaction != null && currentFaction != targetFaction)
                    {
                        ChangeCrimeRatingAction.Apply(currentFaction, action.Amount, true);
                    }
                    break;
                }
                case "DeclareWar":
                {
                    IFaction? targetFaction = null;
                    if (!string.IsNullOrEmpty(action.TargetAlias) && quest != null)
                    {
                        var hero = quest.GetTargetHero(action.TargetAlias);
                        var settlement = quest.GetTargetSettlement(action.TargetAlias);
                        var party = quest.GetTargetParty(action.TargetAlias);
                        targetFaction = hero?.MapFaction ?? settlement?.MapFaction ?? party?.MapFaction;
                    }
                    if (targetFaction == null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        targetFaction = Campaign.Current.Factions.FirstOrDefault(f => f.StringId == action.ObjectId);
                    }
                    if (targetFaction != null && Clan.PlayerClan.MapFaction != null && targetFaction != Clan.PlayerClan.MapFaction)
                    {
                        DeclareWarAction.ApplyByDefault(Clan.PlayerClan.MapFaction, targetFaction);
                    }
                    break;
                }
                case "MakePeace":
                {
                    IFaction? targetFaction = null;
                    if (!string.IsNullOrEmpty(action.TargetAlias) && quest != null)
                    {
                        var hero = quest.GetTargetHero(action.TargetAlias);
                        var settlement = quest.GetTargetSettlement(action.TargetAlias);
                        var party = quest.GetTargetParty(action.TargetAlias);
                        targetFaction = hero?.MapFaction ?? settlement?.MapFaction ?? party?.MapFaction;
                    }
                    if (targetFaction == null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        targetFaction = Campaign.Current.Factions.FirstOrDefault(f => f.StringId == action.ObjectId);
                    }
                    if (targetFaction != null && Clan.PlayerClan.MapFaction != null && targetFaction != Clan.PlayerClan.MapFaction)
                    {
                        MakePeaceAction.Apply(Clan.PlayerClan.MapFaction, targetFaction);
                    }
                    break;
                }
                case "SetPrisoner":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null) TakePrisonerAction.Apply(PartyBase.MainParty, target);
                    break;
                }
                case "DamageHero":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null) target.HitPoints -= action.Amount;
                    break;
                }
                case "SetFlag":
                {
                    quest?.SetFlag(action.ObjectId, action.Amount > 0);
                    break;
                }
                case "SetVariable":
                {
                    if (quest != null)
                    {
                        if (!string.IsNullOrEmpty(action.StringValue))
                            quest.SetStringVariable(action.TargetAlias, action.StringValue);
                        else
                            quest.SetCounter(action.TargetAlias, action.Amount);
                    }
                    break;
                }
                case "IncrementVariable":
                {
                    quest?.IncrementCounter(action.TargetAlias, action.Amount);
                    break;
                }
                case "SetPartyAi":
                {
                    if (quest != null && quest.TargetParties.TryGetValue(action.TargetAlias, out var p) && p != null && p.IsActive)
                    {
                        if (action.StringValue == "IgnoreAi") p.IgnoreByOtherPartiesTill(TaleWorlds.CampaignSystem.CampaignTime.Never);
                        else if (action.StringValue == "UnignoreAi") p.IgnoreByOtherPartiesTill(TaleWorlds.CampaignSystem.CampaignTime.Now);
                        else if (action.StringValue == "Hold") p.SetMoveModeHold();
                        else if (!string.IsNullOrEmpty(action.ObjectId))
                        {
                            if (action.StringValue == "EngageParty")
                            {
                                if (action.ObjectId.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
                                    p.SetMoveEngageParty(TaleWorlds.CampaignSystem.Party.MobileParty.MainParty, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                                else if (quest.TargetParties.TryGetValue(action.ObjectId, out var targetP) && targetP != null)
                                    p.SetMoveEngageParty(targetP, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                            }
                            else if (action.StringValue == "EscortParty" && quest.TargetParties.TryGetValue(action.ObjectId, out var escortP) && escortP != null)
                                p.SetMoveEscortParty(escortP, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);
                            else
                            {
                                var targetSettlement = quest.GetTargetSettlement(action.ObjectId);
                                if (targetSettlement != null)
                                {
                                    if (action.StringValue == "GoToSettlement") p.SetMoveGoToSettlement(targetSettlement, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);
                                    else if (action.StringValue == "PatrolAroundSettlement") p.SetMovePatrolAroundSettlement(targetSettlement, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);
                                }
                            }
                        }
                    }
                    break;
                }
                case "StartQuest":
                {
                    var model = Questliner.Framework.Services.QuestModuleScanner.LoadedQuests.FirstOrDefault(q => q.Id == action.ObjectId);
                    if (model != null && !TaleWorlds.CampaignSystem.Campaign.Current.QuestManager.Quests.OfType<Questliner.Framework.Core.DynamicQuest>().Any(q => q.Model?.Id == model.Id))
                    {
                        Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.TryAutoStartQuest(model);
                    }
                    break;
                }

                case "SetGlobalFlag":
                {
                    Questliner.Framework.Core.GlobalStateManager.Instance.SetFlag(action.ObjectId, action.Amount > 0);
                    break;
                }
                case "SaveHeroToGlobal":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null && !string.IsNullOrEmpty(action.StringValue))
                    {
                        Questliner.Framework.Core.GlobalStateManager.Instance.SetGlobalHero(action.StringValue, target);
                    }
                    break;
                }
                case "ForceConversation":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null && target.CharacterObject != null)
                    {
                        TaleWorlds.CampaignSystem.Conversation.CampaignMapConversation.OpenConversation(
                            new TaleWorlds.CampaignSystem.Conversation.ConversationCharacterData(Hero.MainHero.CharacterObject), 
                            new TaleWorlds.CampaignSystem.Conversation.ConversationCharacterData(target.CharacterObject)
                        );
                    }
                    break;
                }
                case "KillHero":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null && target.IsAlive)
                    {
                        // ApplyByMurder natively handles removing them from towns, resolving their workshops, etc.
                        TaleWorlds.CampaignSystem.Actions.KillCharacterAction.ApplyByMurder(target, null, true);
                    }
                    break;
                }
                case "MakeFugitive":
                {
                    Hero? target = quest?.GetTargetHero(action.TargetAlias);
                    if (target != null && target.IsAlive && target.IsActive)
                    {
                        // Safely removes the hero from the active map (Lost/Fugitive status)
                        TaleWorlds.CampaignSystem.Actions.DisableHeroAction.Apply(target);
                    }
                    break;
                }
                default:
                {
                    TaleWorlds.Library.Debug.Print($"[Questliner] Unknown Action Type executed: {action.ActionType}");
                    break;
                }
            }
        }
    }
}
