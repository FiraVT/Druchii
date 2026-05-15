using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Models;
using TOR_Core.BattleMechanics.AI;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.Gameplay
{
    [HarmonyPatch(typeof(TORHiringCompatibilityModel))]
    public static class HiringPatch
    {
        [HarmonyPatch("CanPlayerHireWanderer")]
        [HarmonyPostfix]
        public static void CanPlayerHireWandererPostfix(Hero player, Hero wanderer, ref bool __result)
        {
            if (player?.Culture?.StringId != null && wanderer?.Culture?.StringId != null &&
                player.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase) && 
                wanderer.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase)) __result = true;
        }

        [HarmonyPatch("CanPlayerHireTroopFromSeller")]
        [HarmonyPostfix]
        public static void CanPlayerHireTroopFromSellerPostfix(Hero player, Hero seller, ref bool __result)
        {
            if (player?.Culture?.StringId != null && seller?.Culture?.StringId != null &&
                player.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase) && 
                seller.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase)) __result = true;
        }
    }

    [HarmonyPatch(typeof(TORDiplomacyModel))]
    public static class DiplomacyPatch
    {
        [HarmonyPatch("GetScoreOfMercenaryToJoinKingdom")]
        [HarmonyPrefix]
        public static bool GetScoreOfMercenaryToJoinKingdomPrefix(Clan mercenaryClan, Kingdom kingdom, ref float __result)
        {
            if (mercenaryClan?.Culture?.StringId != null && kingdom?.Culture?.StringId != null &&
                mercenaryClan.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase) && 
                kingdom.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase))
            {
                __result = 100f;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TOR_Core.Models.TORSmithingModel))]
    public static class SmithingPatch
    {
        [HarmonyPatch("IsCultureAppropriateOrder")]
        [HarmonyPostfix]
        public static void IsCultureAppropriateOrderPostfix(string templateId, string cultureId, ref bool __result)
        {
            if (cultureId != null && cultureId.Equals("druchii", StringComparison.OrdinalIgnoreCase) && 
                templateId != null && (templateId.Contains("sword") || templateId.Contains("polearm") || templateId.Contains("rapier")))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(TOR_Core.BattleMechanics.AI.TORCultureBattleSettings))]
    public static class BattleSettingsPatch
    {
        [HarmonyPatch("GetPersonality")]
        [HarmonyPostfix]
        public static void GetPersonalityPostfix(string cultureId, ref TOR_Core.BattleMechanics.AI.TORCultureBattleSettings.BattlePersonality __result)
        {
            if (cultureId != null && cultureId.Equals("druchii", StringComparison.OrdinalIgnoreCase))
            {
                __result = new TOR_Core.BattleMechanics.AI.TORCultureBattleSettings.BattlePersonality
                {
                    ChargeWeightMultiplier = 1.4f,
                    DefendWeightMultiplier = 0.9f,
                    SkirmishWeightMultiplier = 1.1f,
                    EngagementDistanceMultiplier = 0.8f,
                    RetreatResistance = 1.1f,
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0.2f
                };
            }
        }
    }

    [HarmonyPatch(typeof(DiplomacyHelpers))]
    public static class DiplomacyHelpersPatch
    {
        [HarmonyPatch("GetLoreRivalryLevel")]
        [HarmonyPostfix]
        public static void GetLoreRivalryLevelPostfix(Kingdom kingdom1, Kingdom kingdom2, ref float __result)
        {
            if (kingdom1?.Culture == null || kingdom2?.Culture == null) return;
            string c1 = kingdom1.Culture.StringId;
            string c2 = kingdom2.Culture.StringId;
            if ((c1.Equals("druchii", StringComparison.OrdinalIgnoreCase) && (c2.Equals("battania", StringComparison.OrdinalIgnoreCase) || c2.Equals("vlandia", StringComparison.OrdinalIgnoreCase) || c2.Equals("empire", StringComparison.OrdinalIgnoreCase))) ||
                     ((c1.Equals("battania", StringComparison.OrdinalIgnoreCase) || c1.Equals("vlandia", StringComparison.OrdinalIgnoreCase) || c1.Equals("empire", StringComparison.OrdinalIgnoreCase)) && c2.Equals("druchii", StringComparison.OrdinalIgnoreCase))) __result = 1.0f;
        }
    }
}
