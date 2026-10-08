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
                if (itemRosterElement.IsQuestItem || IsQuestItem(itemRosterElement.Item))
                {
                    // Non-merchandise quest items cannot be sold for denars
                    return 0;
                }
            }

            return base.GetPrice(itemRosterElement, clientParty, merchant, isSelling, inStoreValue, supply, demand);
        }

        public static bool IsEquippableItem(ItemObject? item)
        {
            if (item == null) return false;

            if (item.IsCraftedWeapon || item.HasWeaponComponent || item.HasArmorComponent || item.IsMountable || item.HasSaddleComponent || item.HasBannerComponent)
            {
                return true;
            }

            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                case ItemObject.ItemTypeEnum.SlingStones:
                case ItemObject.ItemTypeEnum.Shield:
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Sling:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.HeadArmor:
                case ItemObject.ItemTypeEnum.BodyArmor:
                case ItemObject.ItemTypeEnum.LegArmor:
                case ItemObject.ItemTypeEnum.HandArmor:
                case ItemObject.ItemTypeEnum.ChestArmor:
                case ItemObject.ItemTypeEnum.Cape:
                case ItemObject.ItemTypeEnum.Horse:
                case ItemObject.ItemTypeEnum.HorseHarness:
                case ItemObject.ItemTypeEnum.Banner:
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Musket:
                case ItemObject.ItemTypeEnum.Bullets:
                    return true;
            }

            string id = item.StringId?.ToLowerInvariant() ?? string.Empty;
            if (id == "geciciahitkilici" ||
                id == "ahitkilici_efsanevi" ||
                id == "pigeon_secret_dagger" ||
                id == "shadow_bone_dagger" ||
                id == "pigeon_blood_claymore")
            {
                return true;
            }

            return false;
        }

        public static bool IsQuestItem(ItemObject? item)
        {
            if (item == null) return false;

            // Equippable weapons/armor/mounts must NOT be marked as IsQuestItem on EquipmentElement,
            // because Native Bannerlord's InventoryLogic.TransferIsMovementValid blocks equipping any non-banner IsQuestItem.
            if (IsEquippableItem(item))
            {
                return false;
            }

            string id = item.StringId?.ToLowerInvariant() ?? string.Empty;
            if (id == "raven_bone_cipher" || 
                id == "black_ledger" || 
                id == "shadow_master_ring" ||
                id == "sifreli_defter" ||
                id == "lordun_muhurlu_emri" ||
                id == "kalfamuhru" ||
                id == "kalbinatesi" ||
                id == "yildizdemiri" ||
                id.StartsWith("ql_quest_") || 
                id.StartsWith("qlf_quest_") ||
                id.Contains("cipher") || 
                id.Contains("sealed_order"))
            {
                return true;
            }

            if (item.ItemType == ItemObject.ItemTypeEnum.Goods && (item.NotMerchandise || item.Value <= 0))
            {
                return true;
            }

            return false;
        }
    }
}
