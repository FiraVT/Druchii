using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.SpellTrainers
{
    // Patch to allow Druchii to access Empire trainers (for Lore of Fire)
    [HarmonyPatch(typeof(SpellTrainerInTownBehavior), "HasEmpireTrainerAccess")]
    public static class EmpireTrainerAccessPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            try
            {
                if (__result) return;
                if (MobileParty.MainParty != null && MobileParty.MainParty.GetSpellCasterMemberHeroes().Any(x => x.Culture != null && x.Culture.StringId != null && x.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase)))
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in EmpireTrainerAccessPatch: " + ex.Message);
            }
        }
    }

    // Patch to allow Druchii to access Wood Elf/Spellsinger trainers
    [HarmonyPatch(typeof(SpellTrainerInTownBehavior), "HasSpellsingerTrainerAccess")]
    public static class WoodElfTrainerAccessPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            try
            {
                if (__result) return;
                if (MobileParty.MainParty != null && MobileParty.MainParty.GetSpellCasterMemberHeroes().Any(x => x.Culture != null && x.Culture.StringId != null && x.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase)))
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in WoodElfTrainerAccessPatch: " + ex.Message);
            }
        }
    }

    // Patch to allow Druchii to access Vampire trainers (for Dark Magic)
    [HarmonyPatch(typeof(SpellTrainerInTownBehavior), "HasVampireTrainerAccess")]
    public static class VampireTrainerAccessPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            try
            {
                if (__result) return;
                if (MobileParty.MainParty != null && MobileParty.MainParty.GetSpellCasterMemberHeroes().Any(x => x.Culture != null && x.Culture.StringId != null && x.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase)))
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in VampireTrainerAccessPatch: " + ex.Message);
            }
        }
    }
}
