using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Models;
using TOR_Core.Extensions;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.Gameplay
{
    [HarmonyPatch(typeof(TORCustomResourceModel), "GetCultureSpecificCustomResourceChange")]
    public static class DruchiiCustomResourcePatches
    {
        public static void Postfix(Hero hero, string resourceid, ref ExplainedNumber __result)
        {
            if (resourceid == "Slaves" && hero.Culture?.StringId == "druchii")
            {
                // 1. Passive Attrition (Decay)
                float current = hero.GetCustomResourceValue("Slaves");
                if (current > 50)
                {
                    // 2% daily loss due to "accidents", escapes, or exhaustion
                    float decay = current * 0.02f;
                    __result.Add(-decay, new TextObject("{=str_druchii_slaves_attrition}Slave Attrition"));
                }

                // 2. Settlement Tribute
                if (hero.Clan != null)
                {
                    float settlementBonus = 0;
                    foreach (var settlement in hero.Clan.Settlements)
                    {
                        if (settlement.IsTown) settlementBonus += 10;
                        else if (settlement.IsCastle) settlementBonus += 4;
                    }
                    
                    if (settlementBonus > 0)
                    {
                        __result.Add(settlementBonus, new TextObject("{=str_druchii_slaves_tribute}Slave Tribute"));
                    }
                }

                // 3. Career Bonus
                if (hero.GetCareer()?.StringId == "DruchiiMercenary")
                {
                    __result.Add(5, new TextObject("{=str_druchii_mercenary_slave_bonus}Mercenary Captives"));
                }
            }
        }
    }
}
