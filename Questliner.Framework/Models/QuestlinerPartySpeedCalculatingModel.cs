using System;
using System.Linq;
using Questliner.Framework.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace Questliner.Framework.Models
{
    public class QuestlinerPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        private static readonly TextObject _questPartySpeedModifierText = new TextObject("{=qlf_quest_party_speed}Quest movement modifier");
        private static readonly TextObject _questPursuitBonusText = new TextObject("{=qlf_quest_party_pursuit}Quest pursuit");
        private static readonly TextObject _questDetachmentBonusText = new TextObject("{=qlf_quest_party_mobilization}Quest party mobilization");

        public override ExplainedNumber CalculateBaseSpeed(
            MobileParty mobileParty,
            bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0,
            int additionalTroopOnHorseCount = 0)
        {
            ExplainedNumber baseSpeed = base.CalculateBaseSpeed(mobileParty, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);

            if (mobileParty != null && IsQuestParty(mobileParty))
            {
                int totalMen = mobileParty.MemberRoster.TotalManCount + additionalTroopOnFootCount + additionalTroopOnHorseCount;
                int limit = mobileParty.Party.PartySizeLimit;

                if (mobileParty.AttachedParties != null && mobileParty.AttachedParties.Count > 0)
                {
                    foreach (var attached in mobileParty.AttachedParties)
                    {
                        totalMen += attached.MemberRoster.TotalManCount;
                        limit += attached.Party.PartySizeLimit;
                    }
                }

                if (totalMen > limit && limit > 0)
                {
                    // In DefaultPartySpeedCalculatingModel, non-hero parties have an innate size limit of 20,
                    // which causes severe speed penalties ((1.0 / (totalMen / limit)) - 1.0) when quest detachments
                    // have 30 to 75 men. We cancel out this artificial penalty for quest-related parties.
                    float overRatio = (float)totalMen / (float)limit;
                    float rawPenalty = (1f / overRatio) - 1f;
                    float appliedPenalty = (mobileParty.ActualClan?.StringId == "deserters") ? rawPenalty * 0.5f : rawPenalty;
                    baseSpeed.AddFactor(-appliedPenalty, _questDetachmentBonusText);
                }
            }

            return baseSpeed;
        }

        public override ExplainedNumber CalculateFinalSpeed(
            MobileParty mobileParty,
            ExplainedNumber finalSpeed)
        {
            finalSpeed = base.CalculateFinalSpeed(mobileParty, finalSpeed);

            if (mobileParty != null && IsQuestParty(mobileParty))
            {
                // Clean pursuit / movement bonus (+20%) so pursuing bandits and fleeing targets can move dynamically
                finalSpeed.AddFactor(0.20f, _questPartySpeedModifierText);

                // If this quest party is actively pursuing / engaging the player, give an additional pursuit boost (+15%)
                // so it can dynamically chase down the player as requested ("beni yeterince hızlı kovalayamıyorlar")
                bool isEngagingPlayer = (mobileParty.DefaultBehavior == AiBehavior.EngageParty || mobileParty.ShortTermBehavior == AiBehavior.EngageParty) &&
                                        (mobileParty.TargetParty == MobileParty.MainParty || mobileParty.ShortTermTargetParty == MobileParty.MainParty ||
                                         mobileParty.Ai?.AiBehaviorPartyBase?.MobileParty == MobileParty.MainParty);
                if (isEngagingPlayer)
                {
                    finalSpeed.AddFactor(0.15f, _questPursuitBonusText);
                }
            }

            return finalSpeed;
        }

        public static bool IsQuestParty(MobileParty? party)
        {
            if (party == null || party == MobileParty.MainParty)
                return false;

            if (party.IsCurrentlyUsedByAQuest)
                return true;

            try
            {
                var questManager = Campaign.Current?.QuestManager;
                if (questManager != null)
                {
                    foreach (var quest in questManager.Quests)
                    {
                        if (quest is DynamicQuest dq && dq.IsOngoing)
                        {
                            if (dq.TargetParties.ContainsValue(party))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Safe ignore
            }

            return false;
        }
    }
}
