using System;
using HarmonyLib;
using SandBox;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using XeraDruchii.Behaviors;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.HordeCamps
{
    /// <summary>
    /// Forces a specific scene when a battle occurs while the Horde Camp is deployed.
    /// </summary>
    [HarmonyPatch(typeof(SandBoxMissions), "OpenBattleMission", new Type[] { typeof(string), typeof(bool) })]
    public static class HordeCampMissionPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref string scene)
        {
            try
            {
                if (Campaign.Current == null) return;
                var behavior = HordeCampBehavior.Instance;
                if (behavior != null && behavior.IsDeployed)
                {
                    XeraLogger.Info("HordeCampMissionPatch: Forcing camp scene TOR_slaver_bay_001");
                    scene = "TOR_slaver_bay_001";
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("HordeCampMissionPatch error", ex);
            }
        }
    }
}
