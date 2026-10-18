using System;
using System.Linq;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace Questliner.Framework.Models
{
    public class QuestlinerHeroDeathProbabilityCalculationModel : DefaultHeroDeathProbabilityCalculationModel
    {
        public override float CalculateHeroDeathProbability(Hero hero)
        {
            if (hero != null && IsProtectedQuestHero(hero))
            {
                return 0f;
            }

            return base.CalculateHeroDeathProbability(hero);
        }

        public static bool IsProtectedQuestHero(Hero hero)
        {
            if (hero == null) return false;

            if (GlobalStateManager.Instance != null && GlobalStateManager.Instance.GlobalHeroes != null)
            {
                if (GlobalStateManager.Instance.GlobalHeroes.ContainsValue(hero) ||
                    GlobalStateManager.Instance.GlobalHeroes.Values.Any(gh => gh != null && gh.StringId == hero.StringId))
                {
                    return true;
                }
            }

            if (Campaign.Current?.QuestManager?.Quests != null)
            {
                foreach (var quest in Campaign.Current.QuestManager.Quests)
                {
                    if (quest is DynamicQuest dq && dq.IsOngoing)
                    {
                        if (dq.TargetHeroes.ContainsValue(hero) ||
                            dq.TargetHeroes.Values.Any(th => th != null && th.StringId == hero.StringId))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
