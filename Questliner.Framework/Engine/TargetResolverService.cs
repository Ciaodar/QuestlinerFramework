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

            if (!string.IsNullOrEmpty(model.Filters) && model.Filters.Contains("||"))
            {
                var branches = model.Filters.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var branch in branches)
                {
                    var cleanBranch = branch.Trim();
                    if (cleanBranch.StartsWith("(") && cleanBranch.EndsWith(")"))
                    {
                        cleanBranch = cleanBranch.Substring(1, cleanBranch.Length - 2).Trim();
                    }
                    var branchModel = new TargetResolverModel
                    {
                        Alias = model.Alias,
                        TargetType = model.TargetType,
                        Filters = cleanBranch,
                        RequiredSettlementAlias = model.RequiredSettlementAlias,
                        Selector = model.Selector,
                        SpecificId = model.SpecificId,
                        IsOptional = model.IsOptional
                    };
                    var resolved = ResolveHero(branchModel, quest, questGiver);
                    if (resolved != null)
                    {
                        return resolved;
                    }
                }
                return null;
            }

            if (!string.IsNullOrEmpty(model.SpecificId))
            {
                if (model.SpecificId.StartsWith("GlobalHero(") && model.SpecificId.EndsWith(")"))
                {
                    int closeIdx = model.SpecificId.LastIndexOf(')');
                    var gKey = (closeIdx > 11) ? model.SpecificId.Substring(11, closeIdx - 11).Trim() : model.SpecificId.Substring(11).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    if (gh != null && gh.IsAlive) return gh;
                    return null;
                }
                return Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == model.SpecificId && h.IsAlive);
            }

            // Check if Filters explicitly requires an existing GlobalHero
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var globalFilter = model.Filters.Split(',').Select(f => f.Trim())
                    .FirstOrDefault(f => !f.StartsWith("!") && f.StartsWith("GlobalHero(") && f.EndsWith(")"));
                if (globalFilter != null)
                {
                    int closeIdx = globalFilter.LastIndexOf(')');
                    var gKey = (closeIdx > 11) ? globalFilter.Substring(11, closeIdx - 11).Trim() : globalFilter.Substring(11).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    if (gh != null && gh.IsAlive)
                    {
                        return gh;
                    }
                    return null;
                }
            }

            bool allowChildren = !string.IsNullOrEmpty(model.Filters) && model.Filters.Split(',').Any(f => f.Trim().Equals("IsChild", StringComparison.OrdinalIgnoreCase));
            IEnumerable<Hero> candidates = Hero.AllAliveHeroes.Where(h => h.IsAlive && !h.IsDisabled && !h.IsNotSpawned && h != Hero.MainHero && (allowChildren || !h.IsChild));

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
                var filters = model.Filters.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f));
                foreach (var filter in filters)
                {
                    candidates = candidates.Where(s => CheckSettlementFilter(s, filter, quest, questGiver));
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
                var filters = model.Filters.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f));
                foreach (var filter in filters)
                {
                    if (!CheckSettlementFilter(settlement, filter, quest, questGiver)) return false;
                }
            }
            return true;
        }

        private static bool CheckSettlementFilter(Settlement settlement, string rawFilter, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (string.IsNullOrWhiteSpace(rawFilter)) return true;

            bool isNegated = false;
            string filter = rawFilter.Trim();
            if (filter.StartsWith("!"))
            {
                isNegated = true;
                filter = filter.Substring(1).Trim();
            }

            bool matches = false;

            if (filter == "Town") matches = settlement.IsTown;
            else if (filter == "Village") matches = settlement.IsVillage;
            else if (filter == "Castle") matches = settlement.IsCastle;
            else if (filter == "Hideout") matches = settlement.IsHideout;
            else if (filter == "SameCultureAsPlayer") matches = settlement.Culture == Hero.MainHero.Culture;
            else if (filter.StartsWith("SameCultureAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 14) ? filter.Substring(14, closeIdx - 14).Trim() : filter.Substring(14).Trim();
                CultureObject? culture = null;
                if (quest != null)
                {
                    culture = quest.GetTargetHero(alias)?.Culture
                           ?? quest.GetTargetSettlement(alias)?.Culture
                           ?? (quest.GetTargetParty(alias)?.ActualClan?.Culture ?? quest.GetTargetParty(alias)?.Party?.Culture);
                }
                if (culture == null)
                {
                    if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                    {
                        culture = Hero.MainHero?.Culture;
                    }
                    else
                    {
                        var gh = GlobalStateManager.Instance.GetGlobalHero(alias);
                        culture = gh?.Culture ?? Campaign.Current?.AliveHeroes.FirstOrDefault(h => h.StringId == alias)?.Culture;
                    }
                }
                matches = (culture != null && settlement.Culture == culture);
            }
            else if (filter.StartsWith("SameSettlementAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 17) ? filter.Substring(17, closeIdx - 17).Trim() : filter.Substring(17).Trim();
                Settlement? refSettlement = null;
                if (quest != null)
                {
                    var targetHero = quest.GetTargetHero(alias);
                    refSettlement = GetHeroSettlement(targetHero)
                                 ?? quest.GetTargetSettlement(alias)
                                 ?? quest.GetTargetParty(alias)?.CurrentSettlement;
                }
                if (refSettlement == null)
                {
                    if (alias.Equals("Giver", StringComparison.OrdinalIgnoreCase) || alias.Equals("QuestGiver", StringComparison.OrdinalIgnoreCase))
                    {
                        var giver = questGiver ?? quest?.QuestGiver;
                        refSettlement = GetHeroSettlement(giver);
                    }
                    else if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                    {
                        refSettlement = GetHeroSettlement(Hero.MainHero);
                    }
                    else
                    {
                        var gh = GlobalStateManager.Instance.GetGlobalHero(alias);
                        if (gh != null)
                        {
                            refSettlement = GetHeroSettlement(gh);
                        }
                        else
                        {
                            var hero = Campaign.Current?.AliveHeroes.FirstOrDefault(h => h.StringId == alias);
                            refSettlement = GetHeroSettlement(hero) ?? Settlement.All.FirstOrDefault(s => s.StringId == alias);
                        }
                    }
                }
                matches = (refSettlement != null && settlement == refSettlement);
            }
            else if (filter.StartsWith("SameFactionAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 14) ? filter.Substring(14, closeIdx - 14).Trim() : filter.Substring(14).Trim();
                IFaction? faction = null;
                if (quest != null)
                {
                    var aliasHero = quest.GetTargetHero(alias);
                    if (aliasHero != null) faction = GetEffectiveFaction(aliasHero);
                    else
                    {
                        var aliasSettlement = quest.GetTargetSettlement(alias);
                        if (aliasSettlement != null) faction = aliasSettlement.MapFaction;
                        else
                        {
                            var aliasParty = quest.GetTargetParty(alias);
                            if (aliasParty != null) faction = aliasParty.MapFaction;
                        }
                    }
                }
                if (faction == null)
                {
                    if (alias.Equals("Giver", StringComparison.OrdinalIgnoreCase) || alias.Equals("QuestGiver", StringComparison.OrdinalIgnoreCase))
                    {
                        faction = GetEffectiveFaction(questGiver ?? quest?.QuestGiver);
                    }
                    else if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                    {
                        faction = Clan.PlayerClan?.MapFaction;
                    }
                    else
                    {
                        var gh = GlobalStateManager.Instance.GetGlobalHero(alias);
                        if (gh != null) faction = GetEffectiveFaction(gh);
                        else
                        {
                            var hero = Campaign.Current?.AliveHeroes.FirstOrDefault(h => h.StringId == alias);
                            if (hero != null) faction = GetEffectiveFaction(hero);
                            else
                            {
                                var sett = Settlement.All.FirstOrDefault(s => s.StringId == alias);
                                if (sett != null) faction = sett.MapFaction;
                            }
                        }
                    }
                }
                matches = (faction != null && settlement.MapFaction == faction);
            }
            else if (filter.StartsWith("SameKingdomAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 14) ? filter.Substring(14, closeIdx - 14).Trim() : filter.Substring(14).Trim();
                Kingdom? kingdom = null;
                if (quest != null)
                {
                    var aliasHero = quest.GetTargetHero(alias);
                    if (aliasHero != null) kingdom = GetEffectiveKingdom(aliasHero);
                    else
                    {
                        var aliasSettlement = quest.GetTargetSettlement(alias);
                        if (aliasSettlement != null) kingdom = (aliasSettlement.MapFaction as Kingdom) ?? aliasSettlement.OwnerClan?.Kingdom;
                        else
                        {
                            var aliasParty = quest.GetTargetParty(alias);
                            if (aliasParty != null) kingdom = (aliasParty.MapFaction as Kingdom) ?? aliasParty.ActualClan?.Kingdom;
                        }
                    }
                }
                if (kingdom == null)
                {
                    if (alias.Equals("Giver", StringComparison.OrdinalIgnoreCase) || alias.Equals("QuestGiver", StringComparison.OrdinalIgnoreCase))
                    {
                        kingdom = GetEffectiveKingdom(questGiver ?? quest?.QuestGiver);
                    }
                    else if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                    {
                        kingdom = Clan.PlayerClan?.Kingdom;
                    }
                    else
                    {
                        var gh = GlobalStateManager.Instance.GetGlobalHero(alias);
                        if (gh != null) kingdom = GetEffectiveKingdom(gh);
                        else
                        {
                            var hero = Campaign.Current?.AliveHeroes.FirstOrDefault(h => h.StringId == alias);
                            if (hero != null) kingdom = GetEffectiveKingdom(hero);
                            else
                            {
                                kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == alias);
                            }
                        }
                    }
                }

                var settKingdom = (settlement.MapFaction as Kingdom) ?? settlement.OwnerClan?.Kingdom;
                matches = (kingdom != null && settKingdom != null && settKingdom == kingdom);
            }
            else
            {
                return true;
            }

            return isNegated ? !matches : matches;
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

        public static Settlement? GetHeroSettlement(Hero? hero)
        {
            if (hero == null) return null;
            return hero.CurrentSettlement 
                ?? hero.PartyBelongedToAsPrisoner?.Settlement 
                ?? hero.PartyBelongedTo?.CurrentSettlement 
                ?? hero.HomeSettlement;
        }

        private static IFaction? GetEffectiveFaction(Hero? h)
        {
            if (h == null) return null;
            return h.Clan?.MapFaction 
                ?? h.MapFaction 
                ?? h.CurrentSettlement?.MapFaction 
                ?? h.HomeSettlement?.MapFaction;
        }

        private static Kingdom? GetEffectiveKingdom(Hero? h)
        {
            if (h == null) return null;
            return h.Clan?.Kingdom 
                   ?? (h.Clan?.MapFaction as Kingdom) 
                   ?? (h.MapFaction as Kingdom)
                   ?? h.CurrentSettlement?.OwnerClan?.Kingdom
                   ?? (h.CurrentSettlement?.MapFaction as Kingdom) 
                   ?? h.HomeSettlement?.OwnerClan?.Kingdom
                   ?? (h.HomeSettlement?.MapFaction as Kingdom);
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
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 14) ? filter.Substring(14, closeIdx - 14).Trim() : filter.Substring(14).Trim();
                IFaction? faction = null;
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero) && aliasHero != null)
                {
                    faction = GetEffectiveFaction(aliasHero);
                }
                else if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                {
                    faction = Clan.PlayerClan?.MapFaction ?? Hero.MainHero?.MapFaction;
                }
                else if ((alias.Equals("Giver", StringComparison.OrdinalIgnoreCase) || alias.Equals("QuestGiver", StringComparison.OrdinalIgnoreCase)) && effectiveGiver != null)
                {
                    faction = GetEffectiveFaction(effectiveGiver);
                }
                else if (quest != null && quest.TargetSettlements.TryGetValue(alias, out var aliasSettlement) && aliasSettlement != null)
                {
                    faction = aliasSettlement.MapFaction;
                }
                else
                {
                    var gh = GlobalStateManager.Instance.GetGlobalHero(alias) ?? Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == alias);
                    if (gh != null) faction = GetEffectiveFaction(gh);
                }

                if (faction != null)
                {
                    return isNegated ? source.Where(h => GetEffectiveFaction(h) != faction) : source.Where(h => GetEffectiveFaction(h) == faction);
                }
                return source;
            }

            if (filter.StartsWith("SameKingdomAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 14) ? filter.Substring(14, closeIdx - 14).Trim() : filter.Substring(14).Trim();
                Kingdom? kingdom = null;
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero) && aliasHero != null)
                {
                    kingdom = GetEffectiveKingdom(aliasHero);
                }
                else if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                {
                    kingdom = Clan.PlayerClan?.Kingdom;
                }
                else if ((alias.Equals("Giver", StringComparison.OrdinalIgnoreCase) || alias.Equals("QuestGiver", StringComparison.OrdinalIgnoreCase)) && effectiveGiver != null)
                {
                    kingdom = GetEffectiveKingdom(effectiveGiver);
                }
                else if (quest != null && quest.TargetSettlements.TryGetValue(alias, out var aliasSettlement) && aliasSettlement != null)
                {
                    kingdom = aliasSettlement.OwnerClan?.Kingdom ?? (aliasSettlement.MapFaction as Kingdom);
                }
                else
                {
                    var gh = GlobalStateManager.Instance.GetGlobalHero(alias) ?? Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == alias);
                    if (gh != null) kingdom = GetEffectiveKingdom(gh);
                }

                if (kingdom != null)
                {
                    return isNegated ? source.Where(h => GetEffectiveKingdom(h) != kingdom) : source.Where(h => GetEffectiveKingdom(h) == kingdom);
                }
                return source;
            }

            if (filter.StartsWith("SameClanAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 11) ? filter.Substring(11, closeIdx - 11).Trim() : filter.Substring(11).Trim();
                Clan? clan = null;
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero) && aliasHero != null)
                {
                    clan = aliasHero.Clan;
                }
                else if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                {
                    clan = Clan.PlayerClan;
                }
                else if ((alias.Equals("Giver", StringComparison.OrdinalIgnoreCase) || alias.Equals("QuestGiver", StringComparison.OrdinalIgnoreCase)) && effectiveGiver != null)
                {
                    clan = effectiveGiver.Clan;
                }
                else if (quest != null && quest.TargetSettlements.TryGetValue(alias, out var aliasSettlement) && aliasSettlement != null)
                {
                    clan = aliasSettlement.OwnerClan;
                }
                else
                {
                    var gh = GlobalStateManager.Instance.GetGlobalHero(alias) ?? Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == alias);
                    if (gh != null) clan = gh.Clan;
                }

                if (clan != null)
                {
                    return isNegated ? source.Where(h => h.Clan != clan) : source.Where(h => h.Clan == clan);
                }
                return source;
            }

            if (filter.StartsWith("SameCultureAs("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 14) ? filter.Substring(14, closeIdx - 14).Trim() : filter.Substring(14).Trim();
                CultureObject? culture = null;
                if (quest != null)
                {
                    culture = quest.GetTargetHero(alias)?.Culture
                           ?? quest.GetTargetSettlement(alias)?.Culture
                           ?? (quest.GetTargetParty(alias)?.ActualClan?.Culture ?? quest.GetTargetParty(alias)?.Party?.Culture);
                }
                if (culture == null)
                {
                    if (alias.Equals("Player", StringComparison.OrdinalIgnoreCase) || alias.Equals("MainHero", StringComparison.OrdinalIgnoreCase))
                        culture = Hero.MainHero?.Culture;
                    else
                    {
                        var gh = GlobalStateManager.Instance.GetGlobalHero(alias);
                        culture = gh?.Culture ?? Campaign.Current?.AliveHeroes.FirstOrDefault(h => h.StringId == alias)?.Culture;
                    }
                }
                return isNegated ? source.Where(h => h.Culture != culture) : source.Where(h => h.Culture == culture);
            }

            if (filter.StartsWith("InSettlement("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 13) ? filter.Substring(13, closeIdx - 13).Trim() : filter.Substring(13).Trim();
                if (quest != null && quest.TargetSettlements.TryGetValue(alias, out var aliasSettlement))
                {
                    return isNegated 
                        ? source.Where(h => GetHeroSettlement(h) != aliasSettlement)
                        : source.Where(h => GetHeroSettlement(h) == aliasSettlement);
                }
                return source;
            }

            if (filter.StartsWith("Target("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string alias = (closeIdx > 7) ? filter.Substring(7, closeIdx - 7).Trim() : filter.Substring(7).Trim();
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                {
                    return isNegated ? source.Where(h => h != aliasHero) : source.Where(h => h == aliasHero);
                }
                return source;
            }

            if (filter.StartsWith("GlobalHero("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string key = (closeIdx > 11) ? filter.Substring(11, closeIdx - 11).Trim() : filter.Substring(11).Trim();
                var globalHero = Questliner.Framework.Core.GlobalStateManager.Instance.GetGlobalHero(key);
                if (globalHero == null || !globalHero.IsAlive)
                {
                    return isNegated ? source : Enumerable.Empty<Hero>();
                }
                return isNegated ? source.Where(h => h != globalHero) : source.Where(h => h == globalHero);
            }

            if (filter.StartsWith("Trait("))
            {
                int closeIdx = filter.LastIndexOf(')');
                string traitName = ((closeIdx > 6) ? filter.Substring(6, closeIdx - 6).Trim() : filter.Substring(6).Trim()).ToLowerInvariant();
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
                "IsChild" => h => h.IsChild,
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
                    int closeIdx = model.SpecificId.LastIndexOf(')');
                    var gKey = (closeIdx > 11) ? model.SpecificId.Substring(11, closeIdx - 11).Trim() : model.SpecificId.Substring(11).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    return gh == hero && gh.IsAlive;
                }
                if (hero.StringId != model.SpecificId) return false;
            }

            if (!string.IsNullOrEmpty(model.Filters) && model.Filters.Contains("||"))
            {
                var branches = model.Filters.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var branch in branches)
                {
                    var cleanBranch = branch.Trim();
                    if (cleanBranch.StartsWith("(") && cleanBranch.EndsWith(")"))
                    {
                        cleanBranch = cleanBranch.Substring(1, cleanBranch.Length - 2).Trim();
                    }
                    var branchModel = new TargetResolverModel
                    {
                        Alias = model.Alias,
                        TargetType = model.TargetType,
                        Filters = cleanBranch,
                        RequiredSettlementAlias = model.RequiredSettlementAlias,
                        Selector = model.Selector,
                        SpecificId = model.SpecificId,
                        IsOptional = model.IsOptional
                    };
                    if (IsHeroMatch(branchModel, hero, quest, questGiver)) return true;
                }
                return false;
            }

            bool allowChildren = !string.IsNullOrEmpty(model.Filters) && model.Filters.Split(',').Any(f => f.Trim().Equals("IsChild", StringComparison.OrdinalIgnoreCase));
            if (hero.IsChild && !allowChildren) return false;

            // To check if a single hero matches the triggers, pass them through the filter pipeline!
            if (!string.IsNullOrEmpty(model.Filters))
            {
                var globalFilter = model.Filters.Split(',').Select(f => f.Trim())
                    .FirstOrDefault(f => !f.StartsWith("!") && f.StartsWith("GlobalHero(") && f.EndsWith(")"));
                if (globalFilter != null)
                {
                    int closeIdx = globalFilter.LastIndexOf(')');
                    var gKey = (closeIdx > 11) ? globalFilter.Substring(11, closeIdx - 11).Trim() : globalFilter.Substring(11).Trim();
                    var gh = GlobalStateManager.Instance.GetGlobalHero(gKey);
                    if (gh == null || !gh.IsAlive || gh != hero)
                    {
                        return false;
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
