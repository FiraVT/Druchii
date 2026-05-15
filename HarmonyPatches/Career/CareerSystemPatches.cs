using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using XeraDruchii.CampaignSystems;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.Career
{
    [HarmonyPatch(typeof(TOR_Core.CharacterDevelopment.TORCareers))]
    public static class TORCareersPatch
    {
        [HarmonyPatch("RegisterAll")]
        [HarmonyPostfix]
        public static void RegisterAllPostfix(TORCareers __instance)
        {
            try 
            {
                MBObjectManager.Instance.RegisterPresumedObject(new CareerObject("DruchiiSorceress"));
                MBObjectManager.Instance.RegisterPresumedObject(new CareerObject("DruchiiMercenary"));
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Failed to register careers. " + ex.ToString());
            }
        }

        [HarmonyPatch("InitializeAll")]
        [HarmonyPostfix]
        public static void InitializeAllPostfix()
        {
            try 
            {
                var sorceress = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiSorceress");
                var mercenary = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiMercenary");

                sorceress?.Initialize("Druchii Sorceress", null, "MindControl", CareerAbilityChargeSupplier.GreyLordCareerCharge, 1000, typeof(MindControlScript));
                mercenary?.Initialize("Druchii Mercenary", null, "LetThemHaveIt");
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in InitializeAllPostfix: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(TORCareers), "All", MethodType.Getter)]
    public static class TORCareersAllPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref MBReadOnlyList<CareerObject> __result)
        {
            // If we are initializing career choices or hireling activities, filter them out 
            // to avoid TOR_Core's validation loop crash.
            if (__result != null)
            {
                bool needsFiltering = false;
                foreach (var career in __result)
                {
                    if (career != null && (career.StringId == "DruchiiSorceress" || career.StringId == "DruchiiMercenary"))
                    {
                        if (XeraDruchii.SubModule.IsInitializingCareerChoices || 
                            XeraDruchii.SubModule.IsInitializingHirelingActivities || 
                            career.RootNode == null)
                        {
                            needsFiltering = true;
                            break;
                        }
                    }
                }

                if (needsFiltering)
                {
                    var filtered = __result.Where(c => c != null && 
                        !((c.StringId == "DruchiiSorceress" || c.StringId == "DruchiiMercenary") && 
                          (XeraDruchii.SubModule.IsInitializingCareerChoices || 
                           XeraDruchii.SubModule.IsInitializingHirelingActivities || 
                           c.RootNode == null))).ToList();
                    __result = new MBReadOnlyList<CareerObject>(filtered);
                }
            }
        }
    }

    [HarmonyPatch(typeof(TORCareerChoices), MethodType.Constructor)]
    public static class TORCareerChoicesPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            XeraDruchii.SubModule.IsInitializingCareerChoices = true;
        }

        [HarmonyPostfix]
        public static void Postfix(TORCareerChoices __instance)
        {
            XeraDruchii.SubModule.IsInitializingCareerChoices = false;
            
            // Inject careers and choices immediately after TORCareerChoices is constructed
            XeraDruchii.Data.DataManager.InjectCareersAndChoices();
        }
    }

    [HarmonyPatch]
    public static class ServeAsAHirelingActivitiesPatch
    {
        [HarmonyTargetMethod]
        public static System.Reflection.MethodBase TargetMethod()
        {
            try
            {
                var type = AccessTools.TypeByName("TOR_Core.CampaignMechanics.ServeAsAHireling.ServeAsAHirelingActivities");
                if (type == null) return null;
                return AccessTools.Constructor(type);
            }
            catch { return null; }
        }

        [HarmonyPrefix]
        public static bool Prefix(object __instance)
        {
            try
            {
                XeraDruchii.SubModule.IsInitializingHirelingActivities = true;
                // Skip original constructor to avoid "Zerca" exception and validation loop
                var field = AccessTools.Field(__instance.GetType(), "_activitySets");
                if (field != null)
                {
                    // Manually initialize the dictionary to satisfy TOR_Core internal logic later
                    field.SetValue(__instance, new Dictionary<CareerObject, List<SkillObject>>());
                }
                return false; 
            }
            catch { return true; } // Fallback to original if something goes wrong here
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            XeraDruchii.SubModule.IsInitializingHirelingActivities = false;
            if (__instance != null)
            {
                XeraDruchii.Data.DataManager.InjectHirelingActivities(__instance);
                // Also need to fill other careers since we skipped the original constructor
                XeraDruchii.Data.DataManager.InjectAllHirelingActivities(__instance);
            }
        }
    }
}
