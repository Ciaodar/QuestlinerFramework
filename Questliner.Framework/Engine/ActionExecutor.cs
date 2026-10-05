using System;
using Questliner.Shared.Models;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using Helpers;
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
                        bool isQuest = Questliner.Framework.Models.QuestlinerTradeItemPriceFactorModel.IsQuestItem(giveItem);
                        var element = new EquipmentElement(giveItem, null, null, isQuestItem: isQuest);
                        MobileParty.MainParty.ItemRoster.AddToCounts(element, action.Amount);
                    }
                    break;
                }
                case "TakeItem":
                {
                    ItemObject? takeItem = MBObjectManager.Instance.GetObject<ItemObject>(action.ObjectId);
                    if (takeItem != null)
                    {
                        int questIndex = MobileParty.MainParty.ItemRoster.FindIndexOfElement(new EquipmentElement(takeItem, null, null, isQuestItem: true));
                        if (questIndex >= 0)
                        {
                            MobileParty.MainParty.ItemRoster.AddToCounts(new EquipmentElement(takeItem, null, null, isQuestItem: true), -action.Amount);
                        }
                        else
                        {
                            MobileParty.MainParty.ItemRoster.AddToCounts(takeItem, -action.Amount);
                        }
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
                        };
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
                {
                    string templateId = string.IsNullOrEmpty(action.PartyTemplateId) ? "looters_template" : action.PartyTemplateId;
                    string clanId = templateId.Replace("_template", ""); // e.g. looters, sea_raiders, deserters
                    
                    CultureObject? contextCulture = null;
                    if (quest != null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        contextCulture = quest.GetTargetSettlement(action.ObjectId)?.Culture;
                    }
                    contextCulture ??= TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement?.Culture 
                                    ?? Hero.MainHero.CurrentSettlement?.Culture 
                                    ?? Hero.MainHero.Culture;

                    // 1. Resolve Party Template with robust fallback chain
                    PartyTemplateObject? partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(templateId);
                    if (partyTemplate == null && !templateId.EndsWith("_template"))
                    {
                        partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(templateId + "_template");
                    }
                    if (partyTemplate == null && templateId.EndsWith("_template"))
                    {
                        partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(clanId);
                    }
                    if (partyTemplate == null)
                    {
                        var matchingClan = Clan.BanditFactions.FirstOrDefault(c => c.StringId.Equals(clanId, StringComparison.OrdinalIgnoreCase));
                        if (matchingClan?.DefaultPartyTemplate != null)
                        {
                            partyTemplate = matchingClan.DefaultPartyTemplate;
                        }
                    }
                    if (partyTemplate == null)
                    {
                        if (contextCulture != null && (clanId.Contains("deserter") || clanId.Contains("assassin") || clanId.Contains("mercenary") || clanId.Contains("caravan")))
                        {
                            partyTemplate = contextCulture.DefaultPartyTemplate ?? contextCulture.BanditBossPartyTemplate;
                        }
                    }
                    if (partyTemplate == null)
                    {
                        partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
                        if (!string.IsNullOrEmpty(action.PartyTemplateId))
                        {
                            TaleWorlds.Library.Debug.Print($"[Questliner Warning] PartyTemplate '{action.PartyTemplateId}' not found! Falling back to 'looters_template'.");
                        }
                    }
                    if (partyTemplate == null)
                    {
                        partyTemplate = Campaign.Current.ObjectManager.GetObjectTypeList<PartyTemplateObject>().FirstOrDefault();
                    }

                    if (partyTemplate == null)
                    {
                        TaleWorlds.Library.Debug.Print($"[Questliner Error] Could not find any valid PartyTemplateObject for action '{action.TargetAlias}'.");
                        break;
                    }

                    // 2. Resolve Bandit Clan / Faction
                    CultureObject? partyCulture = contextCulture ?? partyTemplate.Stacks?.FirstOrDefault().Character?.Culture;
                    Clan? partyClan = Clan.BanditFactions.FirstOrDefault(c => c.StringId.Equals(clanId, StringComparison.OrdinalIgnoreCase));
                    if (partyClan == null && partyCulture != null)
                    {
                        partyClan = Clan.BanditFactions.FirstOrDefault(c => c.Culture == partyCulture);
                    }
                    partyClan ??= Clan.BanditFactions.FirstOrDefault(c => c.StringId == "looters") ?? Clan.BanditFactions.FirstOrDefault();

                    // 3. Determine Spawn Position & Navigation Mesh
                    CampaignVec2 spawnPosition = MobileParty.MainParty.Position;
                    if (Hero.MainHero.CurrentSettlement != null)
                    {
                        spawnPosition = Hero.MainHero.CurrentSettlement.GatePosition;
                    }

                    if (quest != null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        if (action.ObjectId.Equals("Player", StringComparison.OrdinalIgnoreCase))
                        {
                            spawnPosition = Hero.MainHero.CurrentSettlement != null 
                                ? Hero.MainHero.CurrentSettlement.GatePosition 
                                : MobileParty.MainParty.Position;
                        }
                        else
                        {
                            var targetSettlement = quest.GetTargetSettlement(action.ObjectId)
                                                ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(action.ObjectId, StringComparison.OrdinalIgnoreCase));
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
                                else
                                {
                                    var targetHeroObj = quest.GetTargetHero(action.ObjectId);
                                    if (targetHeroObj != null)
                                    {
                                        spawnPosition = targetHeroObj.CurrentSettlement?.GatePosition 
                                                     ?? targetHeroObj.HomeSettlement?.GatePosition 
                                                     ?? targetHeroObj.PartyBelongedTo?.Position 
                                                     ?? spawnPosition;
                                    }
                                }
                            }
                        }
                    }

                    float spawnDist = action.SpawnDistance > 0 ? action.SpawnDistance : 2f;
                    float spawnRad = action.SpawnRadius > 0 ? action.SpawnRadius : 3f;
                    float maxDist = Math.Max(spawnDist, spawnRad);
                    float minDist = Math.Min(spawnDist, spawnRad);

                    CampaignVec2 spawnPosFinal = spawnPosition;
                    CampaignVec2 reachablePosition = NavigationHelper.FindReachablePointAroundPosition(
                        spawnPosition, 
                        MobileParty.NavigationType.Default, 
                        maxDist, 
                        minDist
                    );
                    if (reachablePosition.Face.IsValid())
                    {
                        spawnPosFinal = reachablePosition;
                    }

                    // 4. Resolve Custom Name
                    string partyName = "{=!}Bandits";
                    if (!string.IsNullOrEmpty(action.StringValue) && 
                        !action.StringValue.Equals("Hold", StringComparison.OrdinalIgnoreCase) &&
                        !action.StringValue.Equals("PatrolAroundSettlement", StringComparison.OrdinalIgnoreCase) &&
                        !action.StringValue.Equals("GoToSettlement", StringComparison.OrdinalIgnoreCase) &&
                        !action.StringValue.Equals("EngagePlayer", StringComparison.OrdinalIgnoreCase) &&
                        !action.StringValue.Equals("EngageParty", StringComparison.OrdinalIgnoreCase) &&
                        !action.StringValue.Equals("IgnoreAi", StringComparison.OrdinalIgnoreCase) &&
                        !action.StringValue.Equals("UnignoreAi", StringComparison.OrdinalIgnoreCase))
                    {
                        partyName = action.StringValue;
                    }
                    else if (!string.IsNullOrEmpty(action.TargetAlias))
                    {
                        partyName = action.TargetAlias;
                    }
                    var customPartyNameObj = new TaleWorlds.Localization.TextObject(partyName);

                    // 5. Create and Initialize Party using CustomPartyComponent
                    var homeSettlement = Hero.MainHero.CurrentSettlement ?? Hero.MainHero.HomeSettlement ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault();
                    var party = TaleWorlds.CampaignSystem.Party.PartyComponents.CustomPartyComponent.CreateCustomPartyWithPartyTemplate(
                        spawnPosFinal,
                        0f,
                        homeSettlement,
                        customPartyNameObj,
                        partyClan,
                        partyTemplate,
                        null!
                    );

                    party.Party.SetCustomName(customPartyNameObj);
                    party.SetPartyUsedByQuest(true);
                    party.IsVisible = true;
                    party.Party.SetVisualAsDirty();

                    if (quest != null && !string.IsNullOrEmpty(action.TargetAlias))
                    {
                        quest.TargetParties[action.TargetAlias] = party;
                        quest.AddTrackedObject(party);
                        DynamicQuest.BindPartyToMBTextManager(action.TargetAlias, party);
                    }

                    // 6. Set Troop Count & Diverse Troop Distribution
                    int targetAmount = action.Amount > 0 ? action.Amount : 15;
                    if (party.MemberRoster.TotalManCount < targetAmount)
                    {
                        int deficit = targetAmount - party.MemberRoster.TotalManCount;
                        if (partyTemplate.Stacks != null && partyTemplate.Stacks.Count > 0)
                        {
                            int stackCount = partyTemplate.Stacks.Count;
                            int perStack = deficit / stackCount;
                            int remainder = deficit % stackCount;

                            for (int i = 0; i < stackCount; i++)
                            {
                                var stackChar = partyTemplate.Stacks[i].Character;
                                if (stackChar != null)
                                {
                                    int countToAdd = perStack + (i < remainder ? 1 : 0);
                                    if (countToAdd > 0)
                                    {
                                        party.MemberRoster.AddToCounts(stackChar, countToAdd);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var fillChar = Campaign.Current.ObjectManager.GetObject<CharacterObject>("looter");
                            if (fillChar != null)
                            {
                                party.MemberRoster.AddToCounts(fillChar, deficit);
                            }
                        }
                    }

                    // 7. Protection and AI
                    party.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(24));
                    party.Ai.SetDoNotMakeNewDecisions(true);

                    string aiBehavior = action.StringValue ?? "";
                    if (aiBehavior.Equals("Hold", StringComparison.OrdinalIgnoreCase) || aiBehavior.Equals("IgnoreAi", StringComparison.OrdinalIgnoreCase))
                    {
                        party.SetMoveModeHold();
                    }
                    else if (aiBehavior.Equals("PatrolAroundSettlement", StringComparison.OrdinalIgnoreCase) && quest != null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        var targetSettlement = quest.GetTargetSettlement(action.ObjectId);
                        if (targetSettlement != null)
                            party.SetMovePatrolAroundSettlement(targetSettlement, MobileParty.NavigationType.Default, false);
                        else
                            party.SetMoveModeHold();
                    }
                    else if (aiBehavior.Equals("GoToSettlement", StringComparison.OrdinalIgnoreCase) && quest != null && !string.IsNullOrEmpty(action.ObjectId))
                    {
                        var targetSettlement = quest.GetTargetSettlement(action.ObjectId);
                        if (targetSettlement != null)
                            party.SetMoveGoToSettlement(targetSettlement, MobileParty.NavigationType.Default, false);
                        else
                            party.SetMoveModeHold();
                    }
                    else
                    {
                        // Default: engage player
                        party.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.Default);
                    }
                    break;
                }
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
                    if (targetFaction == null)
                    {
                        targetFaction = TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement?.MapFaction ?? 
                                        Hero.MainHero.CurrentSettlement?.MapFaction ?? 
                                        Hero.MainHero.MapFaction;
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
                    IFaction? sourceFaction = null;

                    if (!string.IsNullOrEmpty(action.TargetAlias) && quest != null)
                    {
                        var hero = quest.GetTargetHero(action.TargetAlias);
                        var settlement = quest.GetTargetSettlement(action.TargetAlias);
                        var party = quest.GetTargetParty(action.TargetAlias);
                        targetFaction = hero?.MapFaction ?? settlement?.MapFaction ?? party?.MapFaction;
                    }
                    if (!string.IsNullOrEmpty(action.ObjectId))
                    {
                        var objFaction = Campaign.Current.Factions.FirstOrDefault(f => f.StringId.Equals(action.ObjectId, System.StringComparison.OrdinalIgnoreCase));
                        if (targetFaction == null)
                        {
                            targetFaction = objFaction;
                        }
                        else
                        {
                            sourceFaction = objFaction;
                        }
                    }

                    if (sourceFaction == null)
                    {
                        sourceFaction = Clan.PlayerClan.MapFaction;
                    }

                    if (targetFaction != null && sourceFaction != null)
                    {
                        if (targetFaction == sourceFaction)
                        {
                            sourceFaction = Campaign.Current.Kingdoms.FirstOrDefault(k => k != targetFaction && !k.IsEliminated) ?? sourceFaction;
                        }

                        if (targetFaction != sourceFaction)
                        {
                            DeclareWarAction.ApplyByDefault(sourceFaction, targetFaction);
                        }
                    }
                    break;
                }
                case "MakePeace":
                {
                    IFaction? targetFaction = null;
                    IFaction? sourceFaction = null;

                    if (!string.IsNullOrEmpty(action.TargetAlias) && quest != null)
                    {
                        var hero = quest.GetTargetHero(action.TargetAlias);
                        var settlement = quest.GetTargetSettlement(action.TargetAlias);
                        var party = quest.GetTargetParty(action.TargetAlias);
                        targetFaction = hero?.MapFaction ?? settlement?.MapFaction ?? party?.MapFaction;
                    }
                    if (!string.IsNullOrEmpty(action.ObjectId))
                    {
                        var objFaction = Campaign.Current.Factions.FirstOrDefault(f => f.StringId.Equals(action.ObjectId, System.StringComparison.OrdinalIgnoreCase));
                        if (targetFaction == null)
                        {
                            targetFaction = objFaction;
                        }
                        else
                        {
                            sourceFaction = objFaction;
                        }
                    }

                    if (sourceFaction == null)
                    {
                        sourceFaction = Clan.PlayerClan.MapFaction;
                    }

                    if (targetFaction != null && sourceFaction != null && targetFaction != sourceFaction)
                    {
                        MakePeaceAction.Apply(sourceFaction, targetFaction);
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
                    if (model != null)
                    {
                        if (Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance != null &&
                            Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance.CompletedQuestIds.Contains(model.Id))
                        {
                            TaleWorlds.Library.Debug.Print($"[Questliner] Quest '{model.Id}' is already marked as completed. Skipping StartQuest.");
                            break;
                        }

                        if (!TaleWorlds.CampaignSystem.Campaign.Current.QuestManager.Quests.OfType<Questliner.Framework.Core.DynamicQuest>().Any(q => q.Model?.Id == model.Id))
                        {
                            Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.TryAutoStartQuest(model);
                        }
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
                    if (target == null && (action.TargetAlias.Equals("Speaker", System.StringComparison.OrdinalIgnoreCase) || 
                                           action.TargetAlias.Equals("ConversationHero", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        target = Hero.OneToOneConversationHero;
                    }
                    if (target == null)
                    {
                        target = GlobalStateManager.Instance.GetGlobalHero(action.TargetAlias)
                              ?? Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == action.TargetAlias);
                    }
                    if (target != null && !string.IsNullOrEmpty(action.StringValue))
                    {
                        Questliner.Framework.Core.GlobalStateManager.Instance.SetGlobalHero(action.StringValue, target);
                        DynamicQuest.BindHeroToMBTextManager(action.StringValue, target);
                        TaleWorlds.Library.Debug.Print($"[Questliner] Saved GlobalHero '{action.StringValue}' -> {target.Name} ({target.StringId})");
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
