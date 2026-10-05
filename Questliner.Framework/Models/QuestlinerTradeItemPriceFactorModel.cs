using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Questliner.Framework.Models
{
    public class QuestlinerTradeItemPriceFactorModel : DefaultTradeItemPriceFactorModel
    {
        public override int GetPrice(
            EquipmentElement itemRosterElement,
            MobileParty clientParty,
            PartyBase merchant,
            bool isSelling,
            float inStoreValue,
            float supply,
            float demand)
        {
            if (itemRosterElement.Item != null && isSelling)
            {
                if (IsQuestItem(itemRosterElement.Item))
                {
                    // Non-merchandise quest items cannot be sold for denars
                    return 0;
                }
            }

            return base.GetPrice(itemRosterElement, clientParty, merchant, isSelling, inStoreValue, supply, demand);
        }

        public static bool IsQuestItem(ItemObject? item)
        {
            if (item == null) return false;

            if (item.NotMerchandise || item.Value <= 0)
            {
                return true;
            }

            string id = item.StringId.ToLower();
            if (id == "raven_bone_cipher" || id == "black_ledger" || id == "shadow_master_ring" ||
                id.Contains("cipher") || id.Contains("ledger") || id.Contains("ring") || id.Contains("sealed_order"))
            {
                return true;
            }

            return false;
        }
    }
}
