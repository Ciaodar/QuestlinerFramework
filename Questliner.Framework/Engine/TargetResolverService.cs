using System;
using System.Collections.Generic;
using System.Linq;
using Questliner.Shared.Models;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

using TaleWorlds.CampaignSystem.Party;

namespace Questliner.Framework.Engine
{
    public static class TargetResolverService
    {
        public static Hero? ResolveHero(TargetResolverModel model, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (model.TargetType != "Hero") return null;

            if (!string.IsNullOrEmpty(model.SpecificId))
            {
                if (model.SpecificId.StartsWith("GlobalHero(") && model.SpecificId.EndsWith(")"))
                {
                    var gKey = model.SpecificId.Substring(11, model.SpecificId.Length - 12).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    if (gh != null && gh.IsAlive) return gh;
                }
                return Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == model.SpecificId);
            }

            // Check if Filters explicitly requires an existing GlobalHero
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var globalFilter = model.Filters.Split(',').Select(f => f.Trim())
                    .FirstOrDefault(f => !f.StartsWith("!") && f.StartsWith("GlobalHero(") && f.EndsWith(")"));
                if (globalFilter != null)
                {
                    var gKey = globalFilter.Substring(11, globalFilter.Length - 12).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    if (gh != null && gh.IsAlive)
                    {
                        return gh;
                    }
                }
            }

            IEnumerable<Hero> candidates = Hero.AllAliveHeroes;
            
            // DYNAMIC QUESTS MUST ALWAYS EXCLUDE THE MAIN HERO FROM RANDOM ASSIGNMENTS
            candidates = candidates.Where(h => h != Hero.MainHero);

            if (!string.IsNullOrEmpty(model.RequiredSettlementAlias))
            {
                if (quest != null && quest.TargetSettlements.TryGetValue(model.RequiredSettlementAlias, out var reqSettlement))
                {
                    candidates = candidates.Where(h => h.CurrentSettlement == reqSettlement || h.HomeSettlement == reqSettlement);
                }
                else
                {
                    return null; // Required settlement not resolved
                }
            }

            // 1. FILTER PIPELINE
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var filters = model.Filters.Split(',').Select(f => f.Trim());
                foreach (var filter in filters)
                {
                    candidates = ApplyFilter(candidates, filter, quest, questGiver);
                }
            }

            if (!candidates.Any()) return null;

            // 1.5 EXCLUDE ALREADY USED HEROES IF POSSIBLE
            if (TaleWorlds.CampaignSystem.Campaign.Current != null)
            {
                var usedHeroes = new System.Collections.Generic.HashSet<Hero>();
                var dynamicQuests = TaleWorlds.CampaignSystem.Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing);
                foreach (var dq in dynamicQuests)
                {
                    foreach (var th in dq.TargetHeroes.Values)
                    {
                        if (th != null) usedHeroes.Add(th);
                    }
                }

                var unusedCandidates = candidates.Where(h => !usedHeroes.Contains(h)).ToList();
                if (unusedCandidates.Any())
                {
                    candidates = unusedCandidates;
                }
            }

            // 2. SELECTOR
            var playerPos = GetPlayerPosition();
            var selector = model.Selector ?? "Random";

            if (selector.StartsWith("NearestTo(") && selector.EndsWith(")"))
            {
                var targetAlias = selector.Substring(10, selector.Length - 11).Trim();
                var refPos = GetAliasPosition(targetAlias, quest) ?? playerPos;
                return candidates.OrderBy(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(refPos)).FirstOrDefault();
            }
            if (selector.StartsWith("WithinDistance(") && selector.EndsWith(")"))
            {
                var parts = selector.Substring(15, selector.Length - 16).Split(',');
                var targetAlias = parts[0].Trim();
                float maxDist = parts.Length > 1 && float.TryParse(parts[1].Trim(), out var d) ? d : 50f;
                var refPos = GetAliasPosition(targetAlias, quest) ?? playerPos;
                var filtered = candidates.Where(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).Distance(refPos) <= maxDist).ToList();
                if (filtered.Any()) return filtered.GetRandomElementInefficiently();
                return candidates.OrderBy(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(refPos)).FirstOrDefault();
            }

            return selector switch
            {
                "Random" => candidates.GetRandomElementInefficiently(),
                "Nearest" => candidates.OrderBy(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(playerPos)).FirstOrDefault(),
                "SecondNearest" => candidates.OrderBy(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(playerPos)).Skip(1).FirstOrDefault(),
                "Furthest" => candidates.OrderByDescending(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(playerPos)).FirstOrDefault(),
                _ => candidates.GetRandomElementInefficiently()
            };
        }

        public static TaleWorlds.Library.Vec2? GetAliasPosition(string alias, DynamicQuest? quest)
        {
            if (quest != null)
            {
                if (quest.TargetSettlements.TryGetValue(alias, out var settlement) && settlement != null)
                    return settlement.GetPosition2D;
                if (quest.TargetHeroes.TryGetValue(alias, out var hero) && hero != null)
                    return hero.CurrentSettlement?.GetPosition2D ?? hero.HomeSettlement?.GetPosition2D ?? hero.PartyBelongedTo?.GetPosition2D;
                if (quest.TargetParties.TryGetValue(alias, out var party) && party != null)
                    return party.GetPosition2D;
            }

            var globalSettlement = Settlement.All.FirstOrDefault(s => s.StringId == alias);
            if (globalSettlement != null) return globalSettlement.GetPosition2D;

            var globalHero = Campaign.Current.AliveHeroes.FirstOrDefault(h => h.StringId == alias);
            if (globalHero != null) return globalHero.CurrentSettlement?.GetPosition2D ?? globalHero.HomeSettlement?.GetPosition2D;

            return null;
        }

        public static Settlement? ResolveSettlement(TargetResolverModel model, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (model.TargetType != "Settlement") return null;
            if (!string.IsNullOrEmpty(model.SpecificId)) return Settlement.All.FirstOrDefault(s => s.StringId == model.SpecificId);

            IEnumerable<Settlement> candidates = Settlement.All;
            
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var filters = model.Filters.Split(',').Select(f => f.Trim());
                foreach (var filter in filters)
                {
                    if (filter == "Town") candidates = candidates.Where(s => s.IsTown);
                    else if (filter == "Village") candidates = candidates.Where(s => s.IsVillage);
                    else if (filter == "Castle") candidates = candidates.Where(s => s.IsCastle);
                    else if (filter == "Hideout") candidates = candidates.Where(s => s.IsHideout);
                    // Same culture as player
                    else if (filter == "SameCultureAsPlayer") candidates = candidates.Where(s => s.Culture == Hero.MainHero.Culture);
                    else if (filter.StartsWith("SameFactionAs("))
                    {
                        var alias = filter.Substring(14, filter.Length - 15);
                        if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                        {
                            var faction = GetEffectiveFaction(aliasHero);
                            candidates = candidates.Where(s => s.MapFaction == faction);
                        }
                    }
                    else if (filter.StartsWith("SameKingdomAs("))
                    {
                        var alias = filter.Substring(14, filter.Length - 15);
                        if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                        {
                            var kingdom = aliasHero.Clan?.Kingdom;
                            candidates = candidates.Where(s => s.MapFaction != null && s.MapFaction.IsKingdomFaction && s.MapFaction == kingdom);
                        }
                    }
                }
            }

            if (!candidates.Any()) return null;

            var playerPos = GetPlayerPosition();
            var selector = model.Selector ?? "Random";

            if (selector.StartsWith("NearestTo(") && selector.EndsWith(")"))
            {
                var targetAlias = selector.Substring(10, selector.Length - 11).Trim();
                var refPos = GetAliasPosition(targetAlias, quest) ?? playerPos;
                return candidates.OrderBy(s => s.GetPosition2D.DistanceSquared(refPos)).FirstOrDefault();
            }
            if (selector.StartsWith("WithinDistance(") && selector.EndsWith(")"))
            {
                var parts = selector.Substring(15, selector.Length - 16).Split(',');
                var targetAlias = parts[0].Trim();
                float maxDist = parts.Length > 1 && float.TryParse(parts[1].Trim(), out var d) ? d : 50f;
                var refPos = GetAliasPosition(targetAlias, quest) ?? playerPos;
                var filtered = candidates.Where(s => s.GetPosition2D.Distance(refPos) <= maxDist).ToList();
                if (filtered.Any()) return filtered.GetRandomElementInefficiently();
                return candidates.OrderBy(s => s.GetPosition2D.DistanceSquared(refPos)).FirstOrDefault();
            }

            return selector switch
            {
                "Random" => candidates.GetRandomElementInefficiently(),
                "Nearest" => candidates.OrderBy(s => s.GetPosition2D.DistanceSquared(playerPos)).FirstOrDefault(),
                "SecondNearest" => candidates.OrderBy(s => s.GetPosition2D.DistanceSquared(playerPos)).Skip(1).FirstOrDefault(),
                "Furthest" => candidates.OrderByDescending(s => s.GetPosition2D.DistanceSquared(playerPos)).FirstOrDefault(),
                _ => candidates.GetRandomElementInefficiently()
            };
        }

        public static bool IsSettlementMatch(TargetResolverModel model, Settlement settlement, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (model.TargetType != "Settlement") return false;
            if (!string.IsNullOrEmpty(model.SpecificId) && settlement.StringId != model.SpecificId) return false;

            if (!string.IsNullOrEmpty(model.Filters))
            {
                var filters = model.Filters.Split(',').Select(f => f.Trim());
                foreach (var filter in filters)
                {
                    if (filter == "Town" && !settlement.IsTown) return false;
                    else if (filter == "Village" && !settlement.IsVillage) return false;
                    else if (filter == "Castle" && !settlement.IsCastle) return false;
                    else if (filter == "Hideout" && !settlement.IsHideout) return false;
                    else if (filter == "SameCultureAsPlayer" && settlement.Culture != Hero.MainHero.Culture) return false;
                    else if (filter.StartsWith("SameFactionAs("))
                    {
                        var alias = filter.Substring(14, filter.Length - 15);
                        if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                        {
                            if (settlement.MapFaction != GetEffectiveFaction(aliasHero)) return false;
                        }
                    }
                    else if (filter.StartsWith("SameKingdomAs("))
                    {
                        var alias = filter.Substring(14, filter.Length - 15);
                        if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                        {
                            if (settlement.MapFaction == null || !settlement.MapFaction.IsKingdomFaction || settlement.MapFaction != aliasHero.Clan?.Kingdom) return false;
                        }
                    }
                }
            }
            return true;
        }
        
        private static int ParseSelectorSkip(string selector)
        {
            var match = System.Text.RegularExpressions.Regex.Match(selector, @"\((\d+)\)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
            {
                return result > 0 ? result - 1 : 0; // Nearest(1) -> skip 0, Nearest(3) -> skip 2
            }
            if (selector == "SecondNearest") return 1; // Backwards compatibility
            return 0;
        }

        private static TaleWorlds.Library.Vec2 GetPlayerPosition()
        {
            return TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.GetPosition2D 
                   ?? Hero.MainHero.CurrentSettlement?.GetPosition2D 
                   ?? Hero.MainHero.HomeSettlement?.GetPosition2D 
                   ?? TaleWorlds.Library.Vec2.Zero;
        }

        private static IFaction? GetEffectiveFaction(Hero? h)
        {
            if (h == null) return null;
            return h.Clan?.MapFaction ?? h.CurrentSettlement?.MapFaction ?? h.HomeSettlement?.MapFaction;
        }

        private static Kingdom? GetEffectiveKingdom(Hero? h)
        {
            if (h == null) return null;
            return h.Clan?.Kingdom 
                   ?? (h.Clan?.MapFaction as Kingdom) 
                   ?? (h.CurrentSettlement?.MapFaction as Kingdom) 
                   ?? (h.HomeSettlement?.MapFaction as Kingdom)
                   ?? (h.MapFaction as Kingdom);
        }

        private static IEnumerable<Hero> ApplyFilter(IEnumerable<Hero> source, string filter, DynamicQuest? quest, Hero? questGiver = null)
        {
            var effectiveGiver = questGiver ?? quest?.QuestGiver;
            IFaction? giverFaction = GetEffectiveFaction(effectiveGiver);
            IFaction playerFaction = Clan.PlayerClan.MapFaction;

            bool isNegated = false;
            if (filter.StartsWith("!"))
            {
                isNegated = true;
                filter = filter.Substring(1);
            }

            if (filter.StartsWith("SameFactionAs("))
            {
                var alias = filter.Substring(14, filter.Length - 15);
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                {
                    var faction = GetEffectiveFaction(aliasHero);
                    return isNegated ? source.Where(h => GetEffectiveFaction(h) != faction) : source.Where(h => GetEffectiveFaction(h) == faction);
                }
                return source;
            }

            if (filter.StartsWith("SameKingdomAs("))
            {
                var alias = filter.Substring(14, filter.Length - 15);
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                {
                    var kingdom = GetEffectiveKingdom(aliasHero);
                    return isNegated ? source.Where(h => GetEffectiveKingdom(h) != kingdom) : source.Where(h => GetEffectiveKingdom(h) == kingdom);
                }
                return source;
            }

            if (filter.StartsWith("SameClanAs("))
            {
                var alias = filter.Substring(11, filter.Length - 12);
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                {
                    var clan = aliasHero.Clan;
                    return isNegated ? source.Where(h => h.Clan != clan) : source.Where(h => h.Clan == clan);
                }
                return source;
            }

            if (filter.StartsWith("InSettlement("))
            {
                var alias = filter.Substring(13, filter.Length - 14);
                if (quest != null && quest.TargetSettlements.TryGetValue(alias, out var aliasSettlement))
                {
                    return isNegated 
                        ? source.Where(h => h.CurrentSettlement != aliasSettlement && h.HomeSettlement != aliasSettlement)
                        : source.Where(h => h.CurrentSettlement == aliasSettlement || h.HomeSettlement == aliasSettlement);
                }
                return source;
            }

            if (filter.StartsWith("Target("))
            {
                var alias = filter.Substring(7, filter.Length - 8);
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                {
                    return isNegated ? source.Where(h => h != aliasHero) : source.Where(h => h == aliasHero);
                }
                return source;
            }

            if (filter.StartsWith("GlobalHero("))
            {
                var key = filter.Substring(11, filter.Length - 12);
                var globalHero = Questliner.Framework.Core.GlobalStateManager.Instance.GetGlobalHero(key);
                if (globalHero == null && !isNegated)
                {
                    // Fallback to prevent softlocks when debug testing quests directly via console
                    globalHero = source.FirstOrDefault(h => h.IsAlive && h != Hero.MainHero);
                    if (globalHero != null)
                    {
                        Questliner.Framework.Core.GlobalStateManager.Instance.SetGlobalHero(key, globalHero);
                        TaleWorlds.Library.Debug.Print($"[Questliner] GlobalHero '{key}' was not found in storage. Fallback assigned: '{globalHero.Name}' ({globalHero.StringId}).");
                    }
                }
                return isNegated ? source.Where(h => h != globalHero) : source.Where(h => h == globalHero);
            }

            if (filter.StartsWith("Trait("))
            {
                var traitName = filter.Substring(6, filter.Length - 7).ToLower();
                Func<Hero, bool> traitCondition = traitName switch
                {
                    "cruel" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Mercy) < 0,
                    "devious" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor) < 0,
                    "calculating" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating) > 0,
                    "impulsive" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating) < 0,
                    "mercy" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Mercy) > 0,
                    "honor" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor) > 0,
                    "generosity" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Generosity) > 0,
                    "closefisted" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Generosity) < 0,
                    "valor" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Valor) > 0,
                    "cautious" => h => h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Valor) < 0,
                    _ => h => true
                };

                return isNegated ? source.Where(h => !traitCondition(h)) : source.Where(traitCondition);
            }

            Func<Hero, bool>? condition = filter switch
            {
                "PlayerFaction" => h => GetEffectiveFaction(h) == playerFaction,
                "SameCultureAsPlayer" => h => h.Culture == Hero.MainHero.Culture,
                "SameAsGiverFaction" => h => giverFaction != null && GetEffectiveFaction(h) == giverFaction,
                "EnemyFaction" => h => {
                    var f = GetEffectiveFaction(h);
                    return f != null && f.IsAtWarWith(playerFaction);
                },
                "PlayerClan" => h => h.Clan == Clan.PlayerClan,
                "ClanLeader" => h => h.Clan != null && h.Clan.Leader == h,
                "KingdomLeader" => h => h.Clan != null && h.Clan.Kingdom != null && h.Clan.Kingdom.Leader == h,
                "Lord" => h => h.IsLord,
                "Artisan" => h => h.IsNotable && h.CharacterObject.Occupation == Occupation.Artisan,
                "RuralNotable" => h => h.IsRuralNotable,
                "HasMobileParty" => h => h.PartyBelongedTo != null,
                "HasKingdom" => h => h.Clan != null && h.Clan.Kingdom != null,
                "Merchant" => h => h.IsNotable && h.CharacterObject.Occupation == Occupation.Merchant,
                "Headman" => h => h.IsNotable && h.CharacterObject.Occupation == Occupation.Headman,
                "GangLeader" => h => h.IsNotable && h.CharacterObject.Occupation == Occupation.GangLeader,
                "Wanderer" => h => h.IsWanderer,
                "Female" => h => h.IsFemale,
                "Male" => h => !h.IsFemale,
                "IsPrisoner" => h => h.IsPrisoner,
                "IsPrisonerInSettlement" => h => h.IsPrisoner && h.PartyBelongedToAsPrisoner != null && h.PartyBelongedToAsPrisoner.IsSettlement,
                _ => null
            };

            if (condition != null)
            {
                return isNegated ? source.Where(h => !condition(h)) : source.Where(condition);
            }
            
            return source;
        }

        public static bool IsHeroMatch(TargetResolverModel model, Hero hero, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (model.TargetType != "Hero") return false;
            if (hero == Hero.MainHero) return false;
            if (!string.IsNullOrEmpty(model.SpecificId))
            {
                if (model.SpecificId.StartsWith("GlobalHero(") && model.SpecificId.EndsWith(")"))
                {
                    var gKey = model.SpecificId.Substring(11, model.SpecificId.Length - 12).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    return gh == hero;
                }
                if (hero.StringId != model.SpecificId) return false;
            }

            // To check if a single hero matches the triggers, pass them through the filter pipeline!
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var globalFilter = model.Filters.Split(',').Select(f => f.Trim())
                    .FirstOrDefault(f => !f.StartsWith("!") && f.StartsWith("GlobalHero(") && f.EndsWith(")"));
                if (globalFilter != null)
                {
                    var gKey = globalFilter.Substring(11, globalFilter.Length - 12).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    if (gh != null)
                    {
                        return gh == hero;
                    }
                }

                var filters = model.Filters.Split(',').Select(f => f.Trim());
                foreach (var filter in filters)
                {
                    if (!ApplyFilter(new[] { hero }, filter, quest, questGiver).Any())
                    {
                        return false; // The hero failed at least one filter tag
                    }
                }
            }
            return true; 
        }
        public static MobileParty? ResolveMobileParty(TargetResolverModel model, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (model.TargetType != "MobileParty") return null;
            if (!string.IsNullOrEmpty(model.SpecificId)) return MobileParty.All.FirstOrDefault(p => p.StringId == model.SpecificId);

            IEnumerable<MobileParty> candidates = MobileParty.All.Where(p => p.IsActive);
            
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var filters = model.Filters.Split(',').Select(f => f.Trim());
                foreach (var rawFilter in filters)
                {
                    bool invert = rawFilter.StartsWith("!");
                    string filter = invert ? rawFilter.Substring(1) : rawFilter;
                    
                    IEnumerable<MobileParty> step = candidates;

                    if (filter == "BanditParty") step = candidates.Where(p => p.IsBandit);
                    else if (filter == "Caravan") step = candidates.Where(p => p.IsCaravan);
                    else if (filter == "LordParty") step = candidates.Where(p => p.IsLordParty);
                    else if (filter.StartsWith("PartyOf(") && filter.EndsWith(")"))
                    {
                        var heroAlias = filter.Substring(8, filter.Length - 9);
                        var hero = quest?.GetTargetHero(heroAlias);
                        var heroParties = candidates.Where(p => hero != null && (p.LeaderHero == hero || p == hero.PartyBelongedTo));
                        if (!heroParties.Any() && hero?.Clan != null)
                        {
                            heroParties = candidates.Where(p => p.ActualClan == hero.Clan);
                        }
                        step = heroParties;
                    }
                    else if (filter.StartsWith("Faction("))
                    {
                        var factionStr = filter.Substring(8, filter.Length - 9);
                        step = candidates.Where(p => p.MapFaction != null && p.MapFaction.StringId == factionStr);
                    }

                    candidates = invert ? candidates.Except(step) : step;
                }
            }

            var finalList = candidates.ToList();
            if (!finalList.Any()) return null;

            if (model.Selector.StartsWith("PartyOf(") && model.Selector.EndsWith(")"))
            {
                var heroAlias = model.Selector.Substring(8, model.Selector.Length - 9);
                var hero = quest?.GetTargetHero(heroAlias);
                if (hero != null && hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsActive)
                    return hero.PartyBelongedTo;
                var fallbackParty = finalList.FirstOrDefault(p => p.LeaderHero == hero) 
                                 ?? finalList.FirstOrDefault(p => p.ActualClan == hero?.Clan);
                if (fallbackParty != null)
                    return fallbackParty;
            }
            if (model.Selector.StartsWith("NearestTo(") && model.Selector.EndsWith(")"))
            {
                var refAlias = model.Selector.Substring(10, model.Selector.Length - 11);
                TaleWorlds.Library.Vec2 refPos = MobileParty.MainParty.GetPosition2D;
                if (quest != null)
                {
                    var targetHero = quest.GetTargetHero(refAlias);
                    var targetSettlement = quest.GetTargetSettlement(refAlias);
                    var targetParty = quest.GetTargetParty(refAlias);
                    if (targetParty != null && targetParty.IsActive) refPos = targetParty.GetPosition2D;
                    else if (targetHero?.PartyBelongedTo != null && targetHero.PartyBelongedTo.IsActive) refPos = targetHero.PartyBelongedTo.GetPosition2D;
                    else if (targetHero?.CurrentSettlement != null) refPos = targetHero.CurrentSettlement.GetPosition2D;
                    else if (targetSettlement != null) refPos = targetSettlement.GetPosition2D;
                }
                return finalList.OrderBy(p => p.GetPosition2D.DistanceSquared(refPos)).FirstOrDefault();
            }

            if (model.Selector == "Random")
                return finalList[TaleWorlds.Core.MBRandom.RandomInt(finalList.Count)];
            else if (model.Selector == "Nearest")
                return finalList.OrderBy(p => p.GetPosition2D.DistanceSquared(MobileParty.MainParty.GetPosition2D)).First();
            else if (model.Selector == "Furthest")
                return finalList.OrderByDescending(p => p.GetPosition2D.DistanceSquared(MobileParty.MainParty.GetPosition2D)).First();

            return finalList[TaleWorlds.Core.MBRandom.RandomInt(finalList.Count)];
        }
    }
}
