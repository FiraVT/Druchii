using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using XeraDruchii.Behaviors;
using XeraDruchii.Utilities;
using TOR_Core.Models;

namespace XeraDruchii.HarmonyPatches.HordeCamps
{
    /// <summary>
    /// Buffs party size based on Horde Camp buildings.
    /// </summary>
    [HarmonyPatch(typeof(TORPartySizeModel), nameof(TORPartySizeModel.GetPartyMemberSizeLimit))]
    public static class HordeCampPartySizePatch
    {
        [HarmonyPostfix]
        public static void Postfix(PartyBase party, ref ExplainedNumber __result)
        {
            try
            {
                if (Campaign.Current == null) return;
                var mainHero = XeraLogger.GetSafeMainHero();
                if (party == null || party.LeaderHero == null || mainHero == null || party.LeaderHero != mainHero) return;

                var behavior = HordeCampBehavior.Instance;
                if (behavior != null && behavior.IsDruchiiPlayer)
                {
                    var data = behavior.CampData;
                    if (data != null)
                    {
                        int pavilionLevel = data.GetBuildingLevel("druchii_pavilion");
                        if (pavilionLevel > 0)
                        {
                            float bonus = pavilionLevel * 20f;
                            __result.Add(bonus, new TextObject("{=horde_camp_pavilion}Druchii Pavilion Bonus"));
                        }
                    }
                }
            }
            catch
            {
                // Silently fail to avoid crashing the whole party screen
            }
        }
    }
}
