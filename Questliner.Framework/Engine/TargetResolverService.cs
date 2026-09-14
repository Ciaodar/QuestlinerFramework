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
                return Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == model.SpecificId);
            }

            IEnumerable<Hero> candidates = Hero.AllAliveHeroes;
            
            // DYNAMIC QUESTS MUST ALWAYS EXCLUDE THE MAIN HERO FROM RANDOM ASSIGNMENTS
            candidates = candidates.Where(h => h != Hero.MainHero);

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

            // 2. SELECTOR
            var playerPos = GetPlayerPosition();
            return model.Selector switch
            {
                "Random" => candidates.GetRandomElementInefficiently(),
                "Nearest" => candidates.OrderBy(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(playerPos)).FirstOrDefault(),
                "SecondNearest" => candidates.OrderBy(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(playerPos)).Skip(1).FirstOrDefault(),
                "Furthest" => candidates.OrderByDescending(h => (h.CurrentSettlement?.GetPosition2D ?? h.HomeSettlement?.GetPosition2D ?? TaleWorlds.Library.Vec2.Zero).DistanceSquared(playerPos)).FirstOrDefault(),
                _ => candidates.GetRandomElementInefficiently()
            };
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
                }
            }

            if (!candidates.Any()) return null;

            var playerPos = GetPlayerPosition();
            return model.Selector switch
            {
                "Random" => candidates.GetRandomElementInefficiently(),
                "Nearest" => candidates.OrderBy(s => s.GetPosition2D.DistanceSquared(playerPos)).FirstOrDefault(),
                "SecondNearest" => candidates.OrderBy(s => s.GetPosition2D.DistanceSquared(playerPos)).Skip(1).FirstOrDefault(),
                "Furthest" => candidates.OrderByDescending(s => s.GetPosition2D.DistanceSquared(playerPos)).FirstOrDefault(),
                _ => candidates.GetRandomElementInefficiently()
            };
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

        private static IEnumerable<Hero> ApplyFilter(IEnumerable<Hero> source, string filter, DynamicQuest? quest, Hero? questGiver = null)
        {
            var effectiveGiver = questGiver ?? quest?.QuestGiver;
            IFaction? giverFaction = GetEffectiveFaction(effectiveGiver);
            IFaction playerFaction = Clan.PlayerClan.MapFaction;

            if (filter.StartsWith("SameFactionAs("))
            {
                var alias = filter.Substring(14, filter.Length - 15);
                if (quest != null && quest.TargetHeroes.TryGetValue(alias, out var aliasHero))
                {
                    var faction = GetEffectiveFaction(aliasHero);
                    return source.Where(h => GetEffectiveFaction(h) == faction);
                }
                return source;
            }

            if (filter.StartsWith("InSettlement("))
            {
                var alias = filter.Substring(13, filter.Length - 14);
                if (quest != null && quest.TargetSettlements.TryGetValue(alias, out var aliasSettlement))
                {
                    return source.Where(h => h.CurrentSettlement == aliasSettlement || h.HomeSettlement == aliasSettlement);
                }
                return source;
            }

            return filter switch
            {
                "PlayerFaction" => source.Where(h => GetEffectiveFaction(h) == playerFaction),
                "SameCultureAsPlayer" => source.Where(h => h.Culture == Hero.MainHero.Culture),
                "SameAsGiverFaction" => source.Where(h => giverFaction != null && GetEffectiveFaction(h) == giverFaction),
                "EnemyFaction" => source.Where(h => {
                    var f = GetEffectiveFaction(h);
                    return f != null && f.IsAtWarWith(playerFaction);
                }),
                "!PlayerClan" => source.Where(h => h.Clan != Clan.PlayerClan),
                "!Leader" => source.Where(h => h.Clan == null || h.Clan.Leader != h),
                "Lord" => source.Where(h => h.IsLord),
                "Artisan" => source.Where(h => h.IsNotable && h.CharacterObject.Occupation == Occupation.Artisan),
                "Merchant" => source.Where(h => h.IsNotable && h.CharacterObject.Occupation == Occupation.Merchant),
                "Headman" => source.Where(h => h.IsNotable && h.CharacterObject.Occupation == Occupation.Headman),
                "GangLeader" => source.Where(h => h.IsNotable && h.CharacterObject.Occupation == Occupation.GangLeader),
                "Wanderer" => source.Where(h => h.IsWanderer),
                "IsFemale" => source.Where(h => h.IsFemale),
                "IsMale" => source.Where(h => !h.IsFemale),
                _ => source // Ignore unknown filters gracefully
            };
        }

        public static bool IsHeroMatch(TargetResolverModel model, Hero hero, DynamicQuest? quest, Hero? questGiver = null)
        {
            if (model.TargetType != "Hero") return false;
            if (hero == Hero.MainHero) return false;
            if (!string.IsNullOrEmpty(model.SpecificId) && hero.StringId != model.SpecificId) return false;

            // To check if a single hero matches the triggers, pass them through the filter pipeline!
            if (!string.IsNullOrEmpty(model.Filters))
            {
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
    }
}
