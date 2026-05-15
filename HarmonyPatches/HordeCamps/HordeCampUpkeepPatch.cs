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
    /// Reduces party upkeep based on Horde Camp buildings.
    /// </summary>
    [HarmonyPatch(typeof(TORPartyWageModel), nameof(TORPartyWageModel.GetTotalWage))]
    public static class HordeCampUpkeepPatch
    {
        [HarmonyPostfix]
        public static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            try
            {
                if (Campaign.Current == null) return;
                var mainHero = XeraLogger.GetSafeMainHero();
                if (mobileParty == null || !mobileParty.IsMainParty || mainHero == null) return;

                var behavior = HordeCampBehavior.Instance;
                if (behavior == null || !behavior.IsDruchiiPlayer) return;

                var data = behavior.CampData;
                if (data != null && behavior.IsDeployed)
                {
                    int warriorHallLevel = data.GetBuildingLevel("warrior_hall");
                    if (warriorHallLevel >= 2)
                    {
                        float warriorBonus = (warriorHallLevel == 2) ? 0.10f : 0.15f; 
                        __result.AddFactor(-warriorBonus, new TextObject("{=horde_camp_upkeep}Warrior Hall Upkeep Reduction"));
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("HordeCampUpkeepPatch error", ex);
            }
        }
    }
}
