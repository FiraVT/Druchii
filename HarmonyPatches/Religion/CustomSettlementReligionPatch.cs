using HarmonyLib;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using XeraDruchii.Utilities;
using System;
using System.Linq;

namespace XeraDruchii.HarmonyPatches.Religion
{
    [HarmonyPatch(typeof(ShrineMenuLogic), "ShrineMenuInit")]
    public static class ShrineMenuInitPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;
            
            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null) return true;

            // Original code: if (component.Religion != null) MBTextManager.SetTextVariable("RELIGION_LINK", component.Religion.EncyclopediaLinkWithName);
            // We ensure component.Religion is not null before accessing its properties.
            if (component.Religion == null)
            {
                XeraLogger.Warn("XeraDruchii: Shrine at " + (settlement.StringId ?? "unknown") + " has no assigned religion. Preventing potential crash.");
                return true;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShrineMenuLogic), "PrayCondition")]
    public static class ShrinePrayConditionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null || component.Religion == null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShrineMenuLogic), "DonationCondition")]
    public static class ShrineDonationConditionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null || component.Religion == null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CursedSiteMenuLogic), "PurifyCondition")]
    public static class CursedSitePurifyConditionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as CursedSiteComponent;
            if (component == null || component.Religion == null)
            {
                // If religion is null, we can't check HostileReligions, which would crash.
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShrineMenuLogic), "PrayingTick")]
    public static class ShrinePrayingTickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(MenuCallbackArgs args, CampaignTime dt)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null || component.Religion == null)
            {
                // Stop the wait early if religion is missing to prevent crashes in original method
                args.MenuContext.GameMenu.EndWait();
                PlayerEncounter.Current.IsPlayerWaiting = false;
                GameMenu.SwitchToMenu("shrine_menu");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShrineMenuLogic), "PrayResultInit")]
    public static class ShrinePrayResultInitPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null || component.Religion == null)
            {
                MBTextManager.SetTextVariable("PRAY_RESULT", "The shrine is silent.");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShrineMenuLogic), "DefileResultConsequence")]
    public static class ShrineDefileResultConsequencePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null || component.Religion == null)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShrineMenuLogic), "LootResultConsequence")]
    public static class ShrineLootResultConsequencePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return true;

            var component = settlement.SettlementComponent as ShrineComponent;
            if (component == null || component.Religion == null)
            {
                return false;
            }
            return true;
        }
    }
}
