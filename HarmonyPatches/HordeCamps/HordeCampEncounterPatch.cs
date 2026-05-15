using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TOR_Core.Models;
using XeraDruchii.Behaviors;
using Helpers;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.HordeCamps
{
    /// <summary>
    /// Custom encounter menu for the Horde Camp when deployed.
    /// Redirects visitors and player interactions to the camp menus.
    /// </summary>
    [HarmonyPatchCategory("LatePatches")]
    [HarmonyPatch(typeof(TOREncounterGameMenuModel), nameof(TOREncounterGameMenuModel.GetEncounterMenu))]
    public static class HordeCampEncounterPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty, ref bool startBattle, ref bool joinBattle, ref string __result)
        {
            try
            {
                if (Campaign.Current == null || attackerParty == null || defenderParty == null) return true;

                PartyBase encounteredParty = null;
                try
                {
                    encounteredParty = MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty);
                }
                catch (Exception ex)
                {
                    XeraLogger.Error("HordeCampEncounterPatch: MapEventHelper.GetEncounteredPartyBase failed", ex);
                    return true;
                }

                if (encounteredParty == null)
                {
                    // To prevent a crash in the base TOR_Core TOREncounterGameMenuModel which doesn't check for null
                    __result = string.Empty;
                    startBattle = false;
                    joinBattle = false;
                    return false;
                }

                if (encounteredParty.IsMobile && encounteredParty.MobileParty == MobileParty.MainParty)
                {
                    var behavior = HordeCampBehavior.Instance;
                    if (behavior != null && behavior.IsDeployed)
                    {
                        if (attackerParty != encounteredParty && attackerParty.MapFaction != null && encounteredParty.MapFaction != null && attackerParty.MapFaction.IsAtWarWith(encounteredParty.MapFaction))
                        {
                            // Allow hostile encounter to proceed normally
                            return true;
                        }

                        startBattle = false;
                        joinBattle = false;
                        
                        if (attackerParty != encounteredParty)
                        {
                            // It's a visitor
                            __result = "horde_camp_visitor_menu";
                        }
                        else
                        {
                            // It's the player clicking themselves or similar
                            __result = "horde_camp_menu";
                        }
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                XeraLogger.Error("HordeCampEncounterPatch: Critical failure in prefix", ex);
                return true;
            }
        }
    }
}
