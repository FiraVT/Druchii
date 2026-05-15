using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.RaidingParties;
using XeraDruchii.Behaviors;
using XeraDruchii.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace XeraDruchii.HarmonyPatches.Gameplay
{
    [HarmonyPatch(typeof(RaidingPartyComponent), "FindNewTarget")]
    public static class CoordinatedCrueltyPatch
    {
        public static bool Prefix(RaidingPartyComponent __instance)
        {
            if (__instance.MobileParty != null && __instance.MobileParty.StringId.Contains("druchii_clan_1_party"))
            {
                // Try to find villages belonging to factions the player is at war with
                var mainHero = XeraLogger.GetSafeMainHero();
                var playerFaction = mainHero?.MapFaction;
                if (playerFaction != null)
                {
                    var target = Settlement.All.Where(s => s != null && s.IsVillage && !s.IsRaided && !s.IsUnderRaid && 
                    s.MapFaction != null && s.MapFaction.IsAtWarWith(playerFaction) && 
                    s.Position.ToVec2().DistanceSquared(__instance.MobileParty.Position.ToVec2()) < 10000f) // 100 distance squared
                    .OrderBy(s => s.Position.ToVec2().DistanceSquared(__instance.MobileParty.Position.ToVec2()))
                    .FirstOrDefault();

                    if (target != null)
                    {
                        __instance.Target = target;
                        return false; // Skip original logic
                    }
                }
            }
            return true;
        }
    }
}
