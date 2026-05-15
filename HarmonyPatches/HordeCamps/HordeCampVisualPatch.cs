using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using XeraDruchii.Behaviors;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.HordeCamps
{
    /// <summary>
    /// Visual patch for horde camp icon.
    /// Manually registered because GetMapIconId is difficult to patch with attributes in some versions.
    /// </summary>
    public static class HordeCampVisualPatch
    {
        public static void Register(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(typeof(PartyComponent), "GetMapIconId");
                if (method == null)
                {
                    // In some versions it's a property
                    method = AccessTools.Property(typeof(PartyComponent), "MapIconId")?.GetMethod;
                }

                if (method == null)
                {
                    // Try MobileParty as fallback for some 1.2.x beta/subversions
                    method = AccessTools.Property(typeof(MobileParty), "MapIconId")?.GetMethod;
                }

                if (method != null)
                {
                    var postfix = typeof(HordeCampVisualPatch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                    XeraLogger.Info("HordeCampVisualPatch: Successfully registered.");
                }
                else
                {
                    XeraLogger.Warn("HordeCampVisualPatch: GetMapIconId method/property not found in PartyComponent. Visual patch disabled.");
                }
            }
            catch (System.Exception ex)
            {
                XeraLogger.Error("HordeCampVisualPatch: Failed to register: " + ex.Message);
            }
        }

        public static void Postfix(object __instance, ref string __result)
        {
            if (Campaign.Current == null) return;
            MobileParty mobileParty = null;
            if (__instance is PartyComponent component) mobileParty = component.MobileParty;
            else if (__instance is MobileParty party) mobileParty = party;

            if (mobileParty != null && mobileParty.IsMainParty && HordeCampBehavior.Instance != null && HordeCampBehavior.Instance.IsDeployed)
            {
                __result = "darkelf_camp_01";
            }
        }
    }
}
