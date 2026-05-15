using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using XeraDruchii.Behaviors;
using TOR_Core.Models;
using XeraDruchii.Utilities;
using TaleWorlds.Localization;

namespace XeraDruchii.HarmonyPatches.HordeCamps
{
    /// <summary>
    /// Buffs party healing when the Horde Camp is deployed.
    /// </summary>
    [HarmonyPatch(typeof(TORPartyHealingModel))]
    public static class HordeCampHealingPatch
    {
        [HarmonyPatch(nameof(TORPartyHealingModel.GetDailyHealingForRegulars))]
        [HarmonyPostfix]
        public static void PostfixRegulars(PartyBase party, ref ExplainedNumber __result)
        {
            try
            {
                if (Campaign.Current == null) return;
                if (party != null && party.IsMobile && party.MobileParty.IsMainParty && HordeCampBehavior.Instance != null && HordeCampBehavior.Instance.IsDeployed)
                {
                    int mainLevel = HordeCampBehavior.Instance.CampData.GetBuildingLevel("encampment_main");
                    if (mainLevel > 0)
                    {
                        float bonus = mainLevel * 0.2f; // 20% per level
                        __result.AddFactor(bonus, new TextObject("{=horde_camp_medical}Encampment Medical Facilities"));
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("HordeCampHealingPatch (Regulars) error", ex);
            }
        }

        [HarmonyPatch(nameof(TORPartyHealingModel.GetDailyHealingHpForHeroes))]
        [HarmonyPostfix]
        public static void PostfixHeroes(PartyBase party, ref ExplainedNumber __result)
        {
            try
            {
                if (Campaign.Current == null) return;
                if (party != null && party.IsMobile && party.MobileParty.IsMainParty && HordeCampBehavior.Instance != null && HordeCampBehavior.Instance.IsDeployed)
                {
                    int mainLevel = HordeCampBehavior.Instance.CampData.GetBuildingLevel("encampment_main");
                    if (mainLevel > 0)
                    {
                        float bonus = mainLevel * 0.2f; // 20% per level
                        __result.AddFactor(bonus, new TextObject("{=horde_camp_rest}Encampment Secure Rest"));
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("HordeCampHealingPatch (Heroes) error", ex);
            }
        }
    }
}
