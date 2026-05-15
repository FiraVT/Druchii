using HarmonyLib;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using XeraDruchii.Utilities;
using System;

namespace XeraDruchii.HarmonyPatches.Religion
{
    [HarmonyPatch(typeof(EncyclopediaReligionObjectVM), "RefreshValues")]
    public static class EncyclopediaReligionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(EncyclopediaReligionObjectVM __instance, ReligionObject ____religionObject)
        {
            if (____religionObject == null) return false;

            try
            {
                __instance.TitleText = ____religionObject.Name?.ToString() ?? "Unknown Religion";
                __instance.DescriptionText = ____religionObject.LoreText?.ToString() ?? "No description available.";

                if (__instance.Followers != null)
                {
                    __instance.Followers.Clear();
                    if (____religionObject.CurrentFollowers != null)
                    {
                        foreach (var follower in ____religionObject.CurrentFollowers)
                        {
                            if (follower != null)
                            {
                                __instance.Followers.Add(new HeroVM(follower));
                            }
                        }
                    }
                }

                if (__instance.ReligiousTroops != null)
                {
                    __instance.ReligiousTroops.Clear();
                    if (____religionObject.ReligiousTroops != null)
                    {
                        foreach (var troop in ____religionObject.ReligiousTroops)
                        {
                            if (troop != null)
                            {
                                __instance.ReligiousTroops.Add(new EncyclopediaUnitVM(troop, false));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("XeraDruchii: Error in EncyclopediaReligionObjectVM.RefreshValues for " + (____religionObject?.StringId ?? "unknown"), ex);
            }

            return false; // Skip original method
        }
    }

    [HarmonyPatch(typeof(ReligionCampaignBehavior), "OnDevotionLevelChanged")]
    public static class ReligionBehaviorPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(object sender, DevotionLevelChangedEventArgs e)
        {
            // Safety check for MainHero and Religion which can be null during early initialization
            var mainHero = XeraLogger.GetSafeMainHero();
            if (e == null || e.Hero == null || e.Religion == null || mainHero == null || mainHero.Name == null)
            {
                return false; 
            }
            return true;
        }
    }
}
