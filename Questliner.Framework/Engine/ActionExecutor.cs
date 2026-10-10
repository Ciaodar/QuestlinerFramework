using System;
using System.Collections.Generic;
using Questliner.Shared.Models;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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
                        bool isQuest = !Questliner.Framework.Models.QuestlinerTradeItemPriceFactorModel.IsEquippableItem(giveItem) &&
                                       Questliner.Framework.Models.QuestlinerTradeItemPriceFactorModel.IsQuestItem(giveItem);
                        var element = new EquipmentElement(giveItem, null, null, isQuestItem: isQuest);
                        MobileParty.MainParty.ItemRoster.AddToCounts(element, action.Amount);
                    }
                    break;
                }
                case "TakeItem":
                {
                    ItemObject? takeItem = MBObjectManager.Instance.GetObject<ItemObject>(action.ObjectId);
                    if (takeItem != null && action.Amount > 0)
                    {
                        int remaining = action.Amount;
                        var roster = MobileParty.MainParty?.ItemRoster;
                        if (roster != null)
                        {
                            // First pass: deduct from IsQuestItem == true stacks, second pass: IsQuestItem == false stacks
                            for (int pass = 0; pass < 2 && remaining > 0; pass++)
                            {
                                bool wantQuestFlag = (pass == 0);
                                var matchingElements = new List<(EquipmentElement Element, int Amount)>();
                                for (int i = 0; i < roster.Count; i++)
                                {
                                    var copy = roster.GetElementCopyAtIndex(i);
                                    if (copy.EquipmentElement.Item == takeItem &&
                                        copy.EquipmentElement.IsQuestItem == wantQuestFlag &&
                                        copy.Amount > 0)
                                    {
                                        matchingElements.Add((copy.EquipmentElement, copy.Amount));
                                    }
                                }

                                foreach (var match in matchingElements)
                                {
                                    if (remaining <= 0) break;
                                    int toRemove = Math.Min(match.Amount, remaining);
                                    if (toRemove > 0)
                                    {
                                        roster.AddToCounts(match.Element, -toRemove);
                                        remaining -= toRemove;
                                    }
                                }
                            }
                        }

                        // If the player equipped an equippable quest weapon, also check MainHero equipment slots
                        if (remaining > 0 && Hero.MainHero != null)
                        {
                            Equipment?[] equipments = new Equipment?[]
                            {
                                Hero.MainHero.BattleEquipment,
                                Hero.MainHero.CivilianEquipment,
                                Hero.MainHero.StealthEquipment
                            };
                            foreach (var eq in equipments)
                            {
                                if (eq == null || remaining <= 0) continue;
                                for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumEquipmentSetSlots && remaining > 0; slot++)
                                {
                                    if (eq[slot].Item == takeItem)
                                    {
                                        eq[slot] = EquipmentElement.Invalid;
                                        remaining--;
                                    }
                                }
                            }
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
                    string rawTemplateId = string.IsNullOrWhiteSpace(action.PartyTemplateId) ? "looters_template" : action.PartyTemplateId.Trim();
                    string clanId = rawTemplateId.EndsWith("_template", StringComparison.OrdinalIgnoreCase)
                        ? rawTemplateId.Substring(0, rawTemplateId.Length - 9).Trim()
                        : rawTemplateId;
                    string locationRefId = !string.IsNullOrEmpty(action.ObjectId) ? action.ObjectId : action.SettlementAlias;

                    // 1. Determine Spawn Position & Navigation Mesh FIRST (so local culture can use actual spawn position)
                    CampaignVec2 spawnPosition = MobileParty.MainParty.Position;
                    if (Hero.MainHero.CurrentSettlement != null)
                    {
                        spawnPosition = Hero.MainHero.CurrentSettlement.GatePosition;
                    }

                    if (quest != null && !string.IsNullOrEmpty(locationRefId))
                    {
                        if (locationRefId.Equals("Player", StringComparison.OrdinalIgnoreCase))
                        {
                            spawnPosition = Hero.MainHero.CurrentSettlement != null 
                                ? Hero.MainHero.CurrentSettlement.GatePosition 
                                : MobileParty.MainParty.Position;
                        }
                        else
                        {
                            var targetSettlement = quest.GetTargetSettlement(locationRefId)
                                                ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(locationRefId, StringComparison.OrdinalIgnoreCase));
                            if (targetSettlement != null)
                            {
                                spawnPosition = targetSettlement.GatePosition;
                            }
                            else
                            {
                                var targetParty = quest.TargetParties.TryGetValue(locationRefId, out var p) ? p : null;
                                if (targetParty != null && targetParty.IsActive)
                                {
                                    spawnPosition = targetParty.Position;
                                }
                                else
                                {
                                    var targetHeroObj = quest.GetTargetHero(locationRefId);
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

                    // 2. Resolve Local Main Culture (nearest town/castle/village to spawnPosFinal, or current settlement, or empire fallback)
                    CultureObject? localCulture = ResolveLocalCulture(spawnPosFinal, quest, locationRefId);

                    // 3. Check for Dynamic Archetypes or Custom Inline Troop Composition
                    int targetAmount = action.Amount > 0 ? action.Amount : 30;
                    bool isDynamicRoster = TryBuildDynamicTroopRoster(rawTemplateId, targetAmount, localCulture, out var dynamicRoster);

                    // 4. Resolve Hostile Bandit Clan / Faction
                    Clan? FindClanById(string targetId)
                    {
                        return Clan.BanditFactions.FirstOrDefault(c => c.StringId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                            ?? Clan.All.FirstOrDefault(c => c.StringId.Equals(targetId, StringComparison.OrdinalIgnoreCase));
                    }

                    Clan? FindClanContains(string keyword)
                    {
                        return Clan.BanditFactions.FirstOrDefault(c => c.StringId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                            ?? Clan.All.FirstOrDefault(c => c.IsBanditFaction && c.StringId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                            ?? Clan.All.FirstOrDefault(c => c.StringId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    Clan? partyClan = FindClanById(clanId);

                    // A) Thematic military renegade / outlaw factions (assassins, mercenaries, deserters, garrison guards, caravan guards)
                    if (partyClan == null)
                    {
                        bool isMilitaryRenegade = clanId.IndexOf("deserter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  clanId.IndexOf("garrison", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  clanId.IndexOf("assassin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  clanId.IndexOf("mercenar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  clanId.IndexOf("guard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  clanId.IndexOf("caravan", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isMilitaryRenegade)
                        {
                            partyClan = FindClanContains("deserters");
                        }
                    }

                    // B) Specific regional outlaw keywords in clanId
                    if (partyClan == null)
                    {
                        if (clanId.IndexOf("steppe", StringComparison.OrdinalIgnoreCase) >= 0 || clanId.IndexOf("nomad", StringComparison.OrdinalIgnoreCase) >= 0)
                            partyClan = FindClanContains("steppe_bandits");
                        else if (clanId.IndexOf("desert", StringComparison.OrdinalIgnoreCase) >= 0 || clanId.IndexOf("bedouin", StringComparison.OrdinalIgnoreCase) >= 0)
                            partyClan = FindClanContains("desert_bandits");
                        else if (clanId.IndexOf("sea", StringComparison.OrdinalIgnoreCase) >= 0 || clanId.IndexOf("corsair", StringComparison.OrdinalIgnoreCase) >= 0 || clanId.IndexOf("raider", StringComparison.OrdinalIgnoreCase) >= 0)
                            partyClan = FindClanContains("sea_raiders");
                        else if (clanId.IndexOf("forest", StringComparison.OrdinalIgnoreCase) >= 0)
                            partyClan = FindClanContains("forest_bandits");
                        else if (clanId.IndexOf("mountain", StringComparison.OrdinalIgnoreCase) >= 0)
                            partyClan = FindClanContains("mountain_bandits");
                    }

                    // C) Regional culture mapping
                    if (partyClan == null && localCulture != null)
                    {
                        string cultId = localCulture.StringId.ToLowerInvariant();
                        string targetBanditKeyword = cultId switch
                        {
                            "aserai" => "desert_bandits",
                            "khuzait" => "steppe_bandits",
                            "battania" => "forest_bandits",
                            "sturgia" => "sea_raiders",
                            "vlandia" => "mountain_bandits",
                            "empire" => "mountain_bandits",
                            _ => ""
                        };

                        if (!string.IsNullOrEmpty(targetBanditKeyword))
                        {
                            partyClan = FindClanContains(targetBanditKeyword);
                        }
                    }

                    // D) Thematic fallback order (never default straight to looters)
                    if (partyClan == null)
                    {
                        partyClan = FindClanContains("deserters")
                                 ?? FindClanContains("mountain_bandits")
                                 ?? FindClanContains("forest_bandits")
                                 ?? FindClanContains("sea_raiders")
                                 ?? FindClanContains("steppe_bandits")
                                 ?? FindClanContains("desert_bandits")
                                 ?? Clan.All.FirstOrDefault(c => c.IsBanditFaction && c.StringId.IndexOf("looter", StringComparison.OrdinalIgnoreCase) < 0);
                    }

                    // E) Absolute final fallback if no other bandit faction exists
                    partyClan ??= FindClanContains("looters") ?? Clan.BanditFactions.FirstOrDefault() ?? Clan.All.FirstOrDefault();

                    // 5. Resolve Base PartyTemplateObject (required by CustomPartyComponent.CreateCustomPartyWithPartyTemplate)
                    PartyTemplateObject? partyTemplate = null;
                    if (!isDynamicRoster)
                    {
                        partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(rawTemplateId);
                        if (partyTemplate == null && !rawTemplateId.EndsWith("_template", StringComparison.OrdinalIgnoreCase))
                        {
                            partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(rawTemplateId + "_template");
                        }
                        if (partyTemplate == null && rawTemplateId.EndsWith("_template", StringComparison.OrdinalIgnoreCase))
                        {
                            partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(clanId);
                        }
                        if (partyTemplate == null)
                        {
                            var matchingClan = partyClan ?? Clan.BanditFactions.FirstOrDefault(c => c.StringId.Equals(clanId, StringComparison.OrdinalIgnoreCase));
                            if (matchingClan?.DefaultPartyTemplate != null)
                            {
                                partyTemplate = matchingClan.DefaultPartyTemplate;
                            }
                        }
                        if (partyTemplate == null &&
                            !rawTemplateId.Equals("looters_template", StringComparison.OrdinalIgnoreCase) &&
                            !rawTemplateId.Equals("looters", StringComparison.OrdinalIgnoreCase))
                        {
                            var singleTroop = FindCharacter(rawTemplateId, allowSubstring: false)
                                           ?? FindCharacter(clanId, allowSubstring: false);
                            if (singleTroop != null)
                            {
                                dynamicRoster = new List<(CharacterObject character, int count)> { (singleTroop, targetAmount) };
                                isDynamicRoster = true;
                            }
                            else
                            {
                                isDynamicRoster = TryBuildDynamicTroopRoster("deserters_template", targetAmount, localCulture, out dynamicRoster);
                            }
                        }
                    }

                    if (partyTemplate == null)
                    {
                        partyTemplate = partyClan?.DefaultPartyTemplate
                                     ?? Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template")
                                     ?? Clan.BanditFactions.FirstOrDefault(c => c.DefaultPartyTemplate != null)?.DefaultPartyTemplate
                                     ?? Campaign.Current.ObjectManager.GetObjectTypeList<PartyTemplateObject>().FirstOrDefault();
                    }

                    if (partyTemplate == null)
                    {
                        TaleWorlds.Library.Debug.Print($"[Questliner Error] Could not find any valid PartyTemplateObject for action '{action.TargetAlias}'.");
                        break;
                    }

                    // 6. Resolve Custom Name
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

                    // 7. Create and Initialize Party using CustomPartyComponent
                    var homeSettlement = (!string.IsNullOrEmpty(locationRefId) && quest != null
                                            ? (quest.GetTargetSettlement(locationRefId)
                                               ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(locationRefId, StringComparison.OrdinalIgnoreCase)))
                                            : null)
                                      ?? Hero.MainHero.CurrentSettlement
                                      ?? Hero.MainHero.HomeSettlement
                                      ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault();
                    string mountId = "";
                    string harnessId = "";
                    if (clanId.IndexOf("caravan", StringComparison.OrdinalIgnoreCase) >= 0 || clanId.IndexOf("smuggler", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bool useCamel = localCulture != null && (localCulture.StringId == "aserai" || localCulture.StringId == "khuzait");
                        mountId = useCamel ? "camel" : "mule";
                        harnessId = useCamel ? "camel_saddle_a" : "mule_load_a";
                    }

                    var party = TaleWorlds.CampaignSystem.Party.PartyComponents.CustomPartyComponent.CreateCustomPartyWithPartyTemplate(
                        spawnPosFinal,
                        0f,
                        homeSettlement,
                        customPartyNameObj,
                        partyClan,
                        partyTemplate,
                        null!,
                        mountId,
                        harnessId
                    );

                    // 8. Populate Troop Roster (Dynamic Archetype / Custom Roster vs Standard PartyTemplate)
                    if (isDynamicRoster && dynamicRoster.Count > 0)
                    {
                        party.MemberRoster.Clear();
                        foreach (var (character, count) in dynamicRoster)
                        {
                            if (character != null && count > 0)
                            {
                                party.MemberRoster.AddToCounts(character, count);
                            }
                        }
                    }
                    else
                    {
                        if (partyTemplate.Stacks != null && partyTemplate.Stacks.Count > 0)
                        {
                            party.MemberRoster.Clear();
                            int stackCount = partyTemplate.Stacks.Count;
                            int perStack = targetAmount / stackCount;
                            int remainder = targetAmount % stackCount;

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

                        if (party.MemberRoster.TotalManCount < targetAmount)
                        {
                            int deficit = targetAmount - party.MemberRoster.TotalManCount;
                            var fillChar = partyTemplate.Stacks?.FirstOrDefault().Character
                                        ?? localCulture?.BasicTroop
                                        ?? Campaign.Current.ObjectManager.GetObject<CharacterObject>("looter");
                            if (fillChar != null && deficit > 0)
                            {
                                party.MemberRoster.AddToCounts(fillChar, deficit);
                            }
                        }
                    }

                    if (party.MemberRoster.TotalManCount <= 0)
                    {
                        var emergencyTroop = localCulture?.BasicTroop
                                          ?? Campaign.Current.ObjectManager.GetObject<CharacterObject>("looter")
                                          ?? CharacterObject.All.FirstOrDefault(c => c.IsSoldier && !c.IsHero && !c.IsTemplate);
                        if (emergencyTroop != null)
                        {
                            party.MemberRoster.AddToCounts(emergencyTroop, Math.Max(1, targetAmount));
                        }
                    }

                    var grainItem = DefaultItems.Grain ?? MBObjectManager.Instance.GetObject<ItemObject>("grain");
                    if (grainItem != null)
                    {
                        party.ItemRoster.AddToCounts(grainItem, Math.Max(25, targetAmount));
                    }

                    int footmenCount = party.Party.NumberOfMenWithoutHorse;
                    if (footmenCount > 0)
                    {
                        var saddleHorse = MBObjectManager.Instance.GetObject<ItemObject>("saddle_horse")
                                       ?? Campaign.Current.ObjectManager.GetObject<ItemObject>("saddle_horse")
                                       ?? Campaign.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                                            .FirstOrDefault(i => i.HorseComponent != null && i.HorseComponent.IsRideable && !i.HorseComponent.IsPackAnimal);
                        if (saddleHorse != null)
                        {
                            party.ItemRoster.AddToCounts(saddleHorse, footmenCount);
                        }
                    }

                    party.RecentEventsMorale = 50f;

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

                    // 9. Protection and AI (Problem 9 fix: ignore other AI lords permanently so they don't attack quest targets)
                    party.IgnoreByOtherPartiesTill(CampaignTime.Never);
                    party.Ai.SetDoNotMakeNewDecisions(true);

                    string aiBehavior = action.StringValue ?? "";
                    if (aiBehavior.Equals("IgnoreAi", StringComparison.OrdinalIgnoreCase))
                    {
                        party.IgnoreByOtherPartiesTill(CampaignTime.Never);
                        party.SetMoveModeHold();
                    }
                    else if (aiBehavior.Equals("Hold", StringComparison.OrdinalIgnoreCase))
                    {
                        party.SetMoveModeHold();
                    }
                    else if (aiBehavior.Equals("PatrolAroundSettlement", StringComparison.OrdinalIgnoreCase) && quest != null && !string.IsNullOrEmpty(locationRefId))
                    {
                        var targetSettlement = quest.GetTargetSettlement(locationRefId)
                                            ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(locationRefId, StringComparison.OrdinalIgnoreCase));
                        if (targetSettlement != null)
                            party.SetMovePatrolAroundSettlement(targetSettlement, MobileParty.NavigationType.Default, false);
                        else
                            party.SetMoveModeHold();
                    }
                    else if (aiBehavior.Equals("GoToSettlement", StringComparison.OrdinalIgnoreCase) && quest != null && !string.IsNullOrEmpty(locationRefId))
                    {
                        var targetSettlement = quest.GetTargetSettlement(locationRefId)
                                            ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(locationRefId, StringComparison.OrdinalIgnoreCase));
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
                        string targetRefId = !string.IsNullOrEmpty(action.ObjectId) ? action.ObjectId : action.SettlementAlias;
                        if (action.StringValue == "IgnoreAi")
                        {
                            p.IgnoreByOtherPartiesTill(TaleWorlds.CampaignSystem.CampaignTime.Never);
                            p.SetMoveModeHold();
                        }
                        else if (action.StringValue == "UnignoreAi")
                        {
                            p.IgnoreByOtherPartiesTill(TaleWorlds.CampaignSystem.CampaignTime.Now);
                        }
                        else if (action.StringValue == "Hold")
                        {
                            p.SetMoveModeHold();
                        }
                        else if (action.StringValue == "EngagePlayer")
                        {
                            p.SetMoveEngageParty(TaleWorlds.CampaignSystem.Party.MobileParty.MainParty, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                        }
                        else if (!string.IsNullOrEmpty(targetRefId))
                        {
                            if (action.StringValue == "EngageParty")
                            {
                                if (targetRefId.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
                                    p.SetMoveEngageParty(TaleWorlds.CampaignSystem.Party.MobileParty.MainParty, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                                else if (quest.TargetParties.TryGetValue(targetRefId, out var targetP) && targetP != null)
                                    p.SetMoveEngageParty(targetP, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default);
                            }
                            else if (action.StringValue == "EscortParty" && quest.TargetParties.TryGetValue(targetRefId, out var escortP) && escortP != null)
                            {
                                p.SetMoveEscortParty(escortP, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);
                            }
                            else
                            {
                                var targetSettlement = quest.GetTargetSettlement(targetRefId)
                                                    ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(targetRefId, System.StringComparison.OrdinalIgnoreCase));
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
                        if (action.StringValue.Equals("Execution", StringComparison.OrdinalIgnoreCase) ||
                            action.ObjectId.Equals("Execution", StringComparison.OrdinalIgnoreCase))
                        {
                            TaleWorlds.CampaignSystem.Actions.KillCharacterAction.ApplyByExecution(target, null, true, true);
                        }
                        else
                        {
                            // ApplyByMurder natively handles removing them from towns, resolving their workshops, etc.
                            TaleWorlds.CampaignSystem.Actions.KillCharacterAction.ApplyByMurder(target, null, true);
                        }
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
                case "ShowInquiry":
                {
                    string rawTitle = !string.IsNullOrEmpty(action.Title) ? action.Title : action.ObjectId;
                    string rawText = !string.IsNullOrEmpty(action.Text) ? action.Text : action.StringValue;
                    string rawAffirmative = !string.IsNullOrEmpty(action.AffirmativeText)
                        ? action.AffirmativeText
                        : (!string.IsNullOrEmpty(action.ButtonText) ? action.ButtonText : "{=yS1OVOgk}OK");
                    string rawNegative = action.NegativeText;

                    string titleText = quest != null ? quest.FormatText(rawTitle).ToString() : new TextObject(rawTitle).ToString();
                    string bodyText = quest != null ? quest.FormatText(rawText).ToString() : new TextObject(rawText).ToString();
                    string affText = quest != null ? quest.FormatText(rawAffirmative).ToString() : new TextObject(rawAffirmative).ToString();
                    string negText = !string.IsNullOrEmpty(rawNegative)
                        ? (quest != null ? quest.FormatText(rawNegative).ToString() : new TextObject(rawNegative).ToString())
                        : string.Empty;

                    bool isNegativeShown = action.IsNegativeOptionShown || !string.IsNullOrWhiteSpace(negText);

                    var inquiryData = new InquiryData(
                        titleText: titleText,
                        text: bodyText,
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: isNegativeShown,
                        affirmativeText: affText,
                        negativeText: negText,
                        affirmativeAction: () =>
                        {
                            if (quest != null && !string.IsNullOrEmpty(action.AffirmativeStageId))
                            {
                                quest.TransitionToStage(action.AffirmativeStageId);
                            }
                        },
                        negativeAction: () =>
                        {
                            if (quest != null && !string.IsNullOrEmpty(action.NegativeStageId))
                            {
                                quest.TransitionToStage(action.NegativeStageId);
                            }
                        },
                        soundEventPath: !string.IsNullOrEmpty(action.SoundEventPath) ? action.SoundEventPath : "",
                        expireTime: action.ExpireTime
                    );

                    InformationManager.ShowInquiry(inquiryData, pauseGameActiveState: action.PauseGame, prioritize: true);
                    break;
                }
                default:
                {
                    TaleWorlds.Library.Debug.Print($"[Questliner] Unknown Action Type executed: {action.ActionType}");
                    break;
                }
            }
        }

        private static bool IsValidMainCulture(CultureObject? culture)
        {
            return culture != null && !culture.IsBandit && (culture.IsMainCulture || culture.BasicTroop != null);
        }

        private static CultureObject? ResolveLocalCulture(CampaignVec2 spawnPosition, DynamicQuest? quest, string? locationRefId)
        {
            string refId = locationRefId ?? string.Empty;
            bool hasExplicitRemoteTarget = !string.IsNullOrEmpty(refId) && !refId.Equals("Player", StringComparison.OrdinalIgnoreCase);

            if (quest != null && hasExplicitRemoteTarget)
            {
                var targetSettlement = quest.GetTargetSettlement(refId)
                                    ?? TaleWorlds.CampaignSystem.Settlements.Settlement.All.FirstOrDefault(s => s.StringId.Equals(refId, StringComparison.OrdinalIgnoreCase));
                if (targetSettlement != null && IsValidMainCulture(targetSettlement.Culture))
                {
                    return targetSettlement.Culture;
                }

                var targetHero = quest.GetTargetHero(refId);
                if (targetHero != null)
                {
                    if (IsValidMainCulture(targetHero.CurrentSettlement?.Culture))
                        return targetHero.CurrentSettlement!.Culture;
                    if (IsValidMainCulture(targetHero.Culture))
                        return targetHero.Culture;
                }
            }
            else
            {
                var currentSettlementCulture = TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement?.Culture
                                            ?? Hero.MainHero.CurrentSettlement?.Culture;
                if (IsValidMainCulture(currentSettlementCulture))
                {
                    return currentSettlementCulture;
                }
            }

            var pos2D = spawnPosition.ToVec2();
            var nearestSettlement = TaleWorlds.CampaignSystem.Settlements.Settlement.All
                .Where(s => (s.IsTown || s.IsCastle || s.IsVillage) && IsValidMainCulture(s.Culture))
                .OrderBy(s => s.GetPosition2D.DistanceSquared(pos2D))
                .FirstOrDefault();

            if (nearestSettlement?.Culture != null)
            {
                return nearestSettlement.Culture;
            }

            var fallbackCurrentCulture = TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement?.Culture
                                      ?? Hero.MainHero.CurrentSettlement?.Culture;
            if (IsValidMainCulture(fallbackCurrentCulture))
            {
                return fallbackCurrentCulture;
            }

            var empireCulture = MBObjectManager.Instance.GetObject<CultureObject>("empire")
                             ?? Campaign.Current?.ObjectManager?.GetObject<CultureObject>("empire");
            if (empireCulture != null)
            {
                return empireCulture;
            }

            if (IsValidMainCulture(Hero.MainHero.Culture))
            {
                return Hero.MainHero.Culture;
            }

            return CharacterObject.All.FirstOrDefault(c => c.IsSoldier && !c.IsHero && IsValidMainCulture(c.Culture))?.Culture;
        }

        private static CharacterObject? FindCharacter(string troopId, bool allowSubstring = true)
        {
            if (string.IsNullOrWhiteSpace(troopId))
                return null;

            string trimmed = troopId.Trim();
            var direct = MBObjectManager.Instance.GetObject<CharacterObject>(trimmed)
                      ?? Campaign.Current?.ObjectManager?.GetObject<CharacterObject>(trimmed);
            if (direct != null && !direct.IsHero && !direct.IsTemplate && !direct.IsChildTemplate)
                return direct;

            var caseInsensitive = CharacterObject.All.FirstOrDefault(c =>
                !c.IsHero && !c.IsTemplate && !c.IsChildTemplate &&
                c.StringId.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (caseInsensitive != null)
                return caseInsensitive;

            if (!allowSubstring)
                return null;

            return CharacterObject.All.FirstOrDefault(c =>
                !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                (c.IsSoldier || c.Occupation == Occupation.Mercenary || c.Occupation == Occupation.Bandit || c.Occupation == Occupation.CaravanGuard) &&
                c.StringId.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? CharacterObject.All.FirstOrDefault(c =>
                !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                c.StringId.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static List<CharacterObject> CollectUpgradeTree(CharacterObject? root, int minTier, int maxTier)
        {
            var result = new List<CharacterObject>();
            if (root == null)
                return result;

            var visited = new HashSet<CharacterObject>();
            var queue = new Queue<CharacterObject>();
            queue.Enqueue(root);
            visited.Add(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!current.IsHero && !current.IsTemplate && !current.IsChildTemplate && !current.IsObsolete &&
                    current.Tier >= minTier && current.Tier <= maxTier)
                {
                    result.Add(current);
                }

                if (current.UpgradeTargets != null)
                {
                    foreach (var next in current.UpgradeTargets)
                    {
                        if (next != null && visited.Add(next))
                        {
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return result;
        }

        private static List<CharacterObject> SpreadByTier(List<CharacterObject> units, bool descendingFirstPass)
        {
            if (units.Count <= 2)
                return units;

            var tierGroups = descendingFirstPass
                ? units.GroupBy(u => u.Tier).OrderByDescending(g => g.Key).Select(g => g.ToList()).ToList()
                : units.GroupBy(u => u.Tier).OrderBy(g => g.Key).Select(g => g.ToList()).ToList();

            if (tierGroups.Count <= 1)
                return units;

            var spread = new List<CharacterObject>(units.Count);
            int pass = 0;
            while (spread.Count < units.Count)
            {
                bool addedInPass = false;
                foreach (var group in tierGroups)
                {
                    if (pass < group.Count)
                    {
                        spread.Add(group[pass]);
                        addedInPass = true;
                    }
                }
                if (!addedInPass)
                    break;
                pass++;
            }

            return spread;
        }

        private static List<(CharacterObject character, int count)> DistributeTroops(
            IEnumerable<CharacterObject?> candidatePool,
            int totalTroops,
            int minTypes = 2,
            int maxTypes = 4)
        {
            var roster = new List<(CharacterObject character, int count)>();
            var distinctPool = candidatePool
                .Where(c => c != null && !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete)
                .Select(c => c!)
                .Distinct()
                .ToList();

            if (distinctPool.Count == 0)
            {
                var fallback = FindCharacter("imperial_infantryman")
                            ?? FindCharacter("mercenary_swordsman")
                            ?? FindCharacter("looter");
                if (fallback != null)
                {
                    roster.Add((fallback, Math.Max(1, totalTroops)));
                }
                return roster;
            }

            int desiredTypes = Math.Min(distinctPool.Count, Math.Min(maxTypes, Math.Max(1, totalTroops)));
            if (desiredTypes < minTypes && distinctPool.Count >= minTypes && totalTroops >= minTypes)
            {
                desiredTypes = minTypes;
            }

            // Pick a balanced mix of infantry and ranged/other units spread across tiers
            var selected = new List<CharacterObject>();
            var infantry = SpreadByTier(distinctPool.Where(c => c.IsInfantry).ToList(), descendingFirstPass: false);
            var ranged = SpreadByTier(distinctPool.Where(c => c.IsRanged).ToList(), descendingFirstPass: true);
            var others = SpreadByTier(distinctPool.Where(c => !c.IsInfantry && !c.IsRanged).ToList(), descendingFirstPass: false);

            int infIdx = 0, rngIdx = 0, othIdx = 0;
            while (selected.Count < desiredTypes)
            {
                bool addedAny = false;
                if (infIdx < infantry.Count && selected.Count < desiredTypes)
                {
                    selected.Add(infantry[infIdx++]);
                    addedAny = true;
                }
                if (rngIdx < ranged.Count && selected.Count < desiredTypes)
                {
                    selected.Add(ranged[rngIdx++]);
                    addedAny = true;
                }
                if (othIdx < others.Count && selected.Count < desiredTypes)
                {
                    selected.Add(others[othIdx++]);
                    addedAny = true;
                }
                if (!addedAny)
                    break;
            }

            if (selected.Count == 0)
            {
                selected.AddRange(distinctPool.Take(desiredTypes));
            }

            int typeCount = selected.Count;
            int perType = totalTroops / typeCount;
            int remainder = totalTroops % typeCount;

            for (int i = 0; i < typeCount; i++)
            {
                int count = perType + (i < remainder ? 1 : 0);
                if (count > 0)
                {
                    roster.Add((selected[i], count));
                }
            }

            return roster;
        }

        private static bool TryBuildDynamicTroopRoster(
            string rawTemplateId,
            int totalTroops,
            CultureObject? localCulture,
            out List<(CharacterObject character, int count)> roster)
        {
            roster = new List<(CharacterObject character, int count)>();
            if (string.IsNullOrWhiteSpace(rawTemplateId))
                return false;

            string trimmed = rawTemplateId.Trim();

            // 1. Custom Inline Troop Composition ("custom:troop1:count1,troop2:count2" or "troop1:count1,troop2:count2")
            if (trimmed.StartsWith("custom:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains(":") ||
                trimmed.Contains(","))
            {
                string body = trimmed.StartsWith("custom:", StringComparison.OrdinalIgnoreCase)
                    ? trimmed.Substring(7).Trim()
                    : trimmed;

                var entries = body.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var uncountedCharacters = new List<CharacterObject>();

                foreach (var rawEntry in entries)
                {
                    string entry = rawEntry.Trim();
                    if (string.IsNullOrEmpty(entry))
                        continue;

                    int colonIdx = entry.IndexOf(':');
                    if (colonIdx >= 0)
                    {
                        string troopId = entry.Substring(0, colonIdx).Trim();
                        string countStr = entry.Substring(colonIdx + 1).Trim();
                        var character = FindCharacter(troopId) ?? localCulture?.BasicTroop ?? FindCharacter("mercenary_swordsman");
                        if (character != null)
                        {
                            if (int.TryParse(countStr, out int parsedCount) && parsedCount > 0)
                            {
                                roster.Add((character, parsedCount));
                            }
                            else
                            {
                                uncountedCharacters.Add(character);
                            }
                        }
                    }
                    else
                    {
                        var character = FindCharacter(entry) ?? localCulture?.BasicTroop ?? FindCharacter("mercenary_swordsman");
                        if (character != null)
                        {
                            uncountedCharacters.Add(character);
                        }
                    }
                }

                if (uncountedCharacters.Count > 0)
                {
                    int alreadyAssigned = roster.Sum(r => r.count);
                    int remainingTroops = Math.Max(uncountedCharacters.Count, totalTroops - alreadyAssigned);
                    int perTroop = remainingTroops / uncountedCharacters.Count;
                    int rem = remainingTroops % uncountedCharacters.Count;
                    for (int i = 0; i < uncountedCharacters.Count; i++)
                    {
                        int count = Math.Max(1, perTroop + (i < rem ? 1 : 0));
                        roster.Add((uncountedCharacters[i], count));
                    }
                }

                return roster.Count > 0;
            }

            // 2. Built-in Culture-Aware & Archetype Templates (match case-insensitively, with or without "_template" suffix)
            string archetype = trimmed.EndsWith("_template", StringComparison.OrdinalIgnoreCase)
                ? trimmed.Substring(0, trimmed.Length - 9).Trim()
                : trimmed;
            archetype = archetype.ToLowerInvariant();

            // A) Deserters ("deserters", "deserters_template")
            if (archetype == "deserters" || archetype == "deserter" || archetype.Contains("deserter"))
            {
                var pool = new List<CharacterObject>();
                if (localCulture?.BasicTroop != null)
                {
                    var treeTroops = CollectUpgradeTree(localCulture.BasicTroop, 2, 4);
                    pool.AddRange(treeTroops.Where(c => !c.IsMounted));
                    if (pool.Count < 2)
                    {
                        pool.AddRange(treeTroops);
                    }
                }

                if (localCulture != null)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.Culture == localCulture &&
                        c.IsSoldier &&
                        !c.IsHero &&
                        !c.IsTemplate &&
                        !c.IsChildTemplate &&
                        !c.IsObsolete &&
                        !c.IsMounted &&
                        c.Tier >= 2 && c.Tier <= 4));

                    if (pool.Count < 2)
                    {
                        pool.AddRange(CharacterObject.All.Where(c =>
                            c.Culture == localCulture &&
                            c.IsSoldier &&
                            !c.IsHero &&
                            !c.IsTemplate &&
                            !c.IsChildTemplate &&
                            !c.IsObsolete &&
                            c.Tier >= 2 && c.Tier <= 4));
                    }
                }

                if (pool.Count == 0)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.IsSoldier && !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        c.Culture != null && !c.Culture.IsBandit &&
                        c.Tier >= 2 && c.Tier <= 4).Take(6));
                }

                roster = DistributeTroops(pool, totalTroops, 2, 4);
                return roster.Count > 0;
            }

            // B) Mercenaries ("mercenary", "mercenaries", "mercenary_template")
            if (archetype == "mercenary" || archetype == "mercenaries" || archetype.Contains("mercenar"))
            {
                string[] mercenaryIds =
                {
                    "mercenary_swordsman",
                    "hired_blade",
                    "mercenary_crossbowman",
                    "hired_crossbow",
                    "mercenary_scout",
                    "veteran_borrowed_troop"
                };

                var pool = new List<CharacterObject>();
                foreach (var id in mercenaryIds)
                {
                    var c = FindCharacter(id);
                    if (c != null)
                        pool.Add(c);
                }

                if (pool.Count < 2)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        c.Occupation == Occupation.Mercenary &&
                        c.Tier >= 3 && c.Tier <= 5));
                }

                if (pool.Count < 2 && localCulture != null)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.Culture == localCulture &&
                        c.IsSoldier && !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        c.Tier >= 3 && c.Tier <= 4));
                }

                roster = DistributeTroops(pool, totalTroops, 2, 4);
                return roster.Count > 0;
            }

            // C) Assassins ("assassins", "assassins_template", "shadow_assassins_template")
            if (archetype == "assassins" || archetype == "assassin" || archetype == "shadow_assassins" ||
                archetype.Contains("assassin") || archetype.Contains("hitman") || archetype.Contains("hitmen"))
            {
                string[] assassinCoreIds =
                {
                    "hired_blade",
                    "mercenary_swordsman",
                    "hired_crossbow"
                };

                var pool = new List<CharacterObject>();
                foreach (var id in assassinCoreIds)
                {
                    var c = FindCharacter(id);
                    if (c != null)
                        pool.Add(c);
                }

                if (localCulture != null)
                {
                    var agileLocal = CharacterObject.All.Where(c =>
                        c.Culture == localCulture &&
                        c.IsSoldier &&
                        !c.IsHero &&
                        !c.IsTemplate &&
                        !c.IsChildTemplate &&
                        !c.IsObsolete &&
                        !c.IsMounted &&
                        c.Tier >= 3 && c.Tier <= 4).Take(2);
                    pool.AddRange(agileLocal);
                }

                if (pool.Count < 2)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.IsSoldier && !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        !c.IsMounted && c.Tier >= 3 && c.Tier <= 4).Take(4));
                }

                roster = DistributeTroops(pool, totalTroops, 2, 4);
                return roster.Count > 0;
            }

            // D) Caravan Guards ("caravan_guards", "caravan_guards_template", "smuggler_caravan_template")
            if (archetype == "caravan_guards" || archetype == "caravan_guard" || archetype == "smuggler_caravan" ||
                archetype.Contains("caravan") || archetype.Contains("smuggler") || archetype.Contains("armed_trader"))
            {
                var pool = new List<CharacterObject>();

                if (localCulture?.CaravanGuard != null)
                {
                    pool.AddRange(CollectUpgradeTree(localCulture.CaravanGuard, 1, 6));
                }

                if (localCulture != null)
                {
                    string cultureId = localCulture.StringId;
                    string[] cultureCaravanIds =
                    {
                        $"caravan_guard_{cultureId}",
                        $"veteran_caravan_guard_{cultureId}",
                        $"armed_trader_{cultureId}"
                    };
                    foreach (var id in cultureCaravanIds)
                    {
                        var c = FindCharacter(id, allowSubstring: false);
                        if (c != null)
                            pool.Add(c);
                    }

                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.Culture == localCulture &&
                        !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        (c.StringId.IndexOf("caravan_guard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         c.StringId.IndexOf("armed_trader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         c.Occupation == Occupation.CaravanGuard)));
                }

                string[] directCaravanIds =
                {
                    "caravan_guard",
                    "veteran_caravan_guard",
                    "armed_trader"
                };

                foreach (var id in directCaravanIds)
                {
                    var c = FindCharacter(id, allowSubstring: false);
                    if (c != null)
                        pool.Add(c);
                }

                if (pool.Count < 2)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        (c.StringId.IndexOf("caravan_guard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         c.StringId.IndexOf("armed_trader", StringComparison.OrdinalIgnoreCase) >= 0)).Take(4));
                }

                if (pool.Count < 2)
                {
                    var fallbackMerc1 = FindCharacter("mercenary_swordsman");
                    var fallbackMerc2 = FindCharacter("mercenary_scout");
                    if (fallbackMerc1 != null) pool.Add(fallbackMerc1);
                    if (fallbackMerc2 != null) pool.Add(fallbackMerc2);
                }

                roster = DistributeTroops(pool, totalTroops, 2, 4);
                return roster.Count > 0;
            }

            // E) Garrison Guards / Elite Guards ("garrison_guards", "garrison_guards_template", "elite_guards_template")
            if (archetype == "garrison_guards" || archetype == "garrison_guard" ||
                archetype == "elite_guards" || archetype == "elite_guard" ||
                archetype.Contains("garrison") || archetype.Contains("guard"))
            {
                var pool = new List<CharacterObject>();
                if (localCulture?.BasicTroop != null)
                {
                    var treeTroops = CollectUpgradeTree(localCulture.BasicTroop, 3, 5);
                    pool.AddRange(treeTroops.Where(c => !c.IsMounted));
                    if (pool.Count < 2)
                    {
                        pool.AddRange(treeTroops);
                    }
                }

                if (localCulture != null)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.Culture == localCulture &&
                        c.IsSoldier &&
                        !c.IsHero &&
                        !c.IsTemplate &&
                        !c.IsChildTemplate &&
                        !c.IsObsolete &&
                        !c.IsMounted &&
                        c.Tier >= 3 && c.Tier <= 5));

                    if (pool.Count < 2)
                    {
                        pool.AddRange(CharacterObject.All.Where(c =>
                            c.Culture == localCulture &&
                            c.IsSoldier &&
                            !c.IsHero &&
                            !c.IsTemplate &&
                            !c.IsChildTemplate &&
                            !c.IsObsolete &&
                            c.Tier >= 3 && c.Tier <= 5));
                    }
                }

                if (pool.Count == 0)
                {
                    pool.AddRange(CharacterObject.All.Where(c =>
                        c.IsSoldier && !c.IsHero && !c.IsTemplate && !c.IsChildTemplate && !c.IsObsolete &&
                        c.Culture != null && !c.Culture.IsBandit &&
                        !c.IsMounted && c.Tier >= 3 && c.Tier <= 5).Take(6));
                }

                roster = DistributeTroops(pool, totalTroops, 2, 4);
                return roster.Count > 0;
            }

            return false;
        }
    }
}
