using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using XeraDruchii.Behaviors;
using XeraDruchii.Data;
using XeraDruchii.MissionBehaviors;
using XeraDruchii.Utilities;
using XeraDruchii.HarmonyPatches.HordeCamps;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.Logger.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace XeraDruchii
{
    /// <summary>
    /// Main SubModule for the Xera Druchii complete Dark Elf mod
    /// Combines: Horde Camps, Spellcaster Units, Faction Unlock, Character Creation, and Hero Management
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        public static bool IsInitializingCareerChoices = false;
        public static bool IsInitializingHirelingActivities = false;
        public static Harmony HarmonyInstance { get; private set; }
        private static bool _harmonyPatched = false;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            XeraLogger.Configure();
            XeraLogger.Info("SubModule loading...");

            // Dependency Audit
            try
            {
                XeraLogger.Info($"Game Version: {typeof(MBSubModuleBase).Assembly.GetName().Version}");
                XeraLogger.Info($"Harmony Version: {typeof(Harmony).Assembly.GetName().Version}");
                
                var butterLibVersion = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Bannerlord.ButterLib")?.GetName().Version;
                XeraLogger.Info($"ButterLib Version: {butterLibVersion}");
            }
            catch (Exception ex)
            {
                XeraLogger.Warn($"Failed to audit dependency versions: {ex.Message}");
            }

            try
            {
                // Add ButterLib Serilog provider for additional logging
                this.AddSerilogLoggerProvider($"XeraDruchii_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log", new[] { "XeraDruchii.*" });
                XeraLogger.Info("ButterLib Serilog provider initialized. Detailed logs will be in Documents/Mount and Blade II Bannerlord/Configs/ModLogs/");
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Failed to initialize Serilog provider", ex);
            }

            try
            {
                if (!_harmonyPatched)
                {
                    XeraLogger.StartTimer("HarmonyPatching");
                    HarmonyInstance = new Harmony("com.xeradruchii.complete");
                    HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    HordeCampVisualPatch.Register(HarmonyInstance);
                    _harmonyPatched = true;
                    XeraLogger.StopAndLogTimer("HarmonyPatching");

                    XeraLogger.Info("Harmony patches applied successfully. Data will load via TOR_Core hooks.");

                    // Verify patches
                    var patchedMethods = HarmonyInstance.GetPatchedMethods().ToList();
                    XeraLogger.Info($"Total patched methods: {patchedMethods.Count}");
                    foreach (var method in patchedMethods)
                    {
                        XeraLogger.Trace($"Patched method: {method.DeclaringType?.FullName}.{method.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error during initialization:", ex);
            }
        }

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            
            // Activate LatePatches category (contains HordeCampEncounterPatch)
            try 
            {
                HarmonyInstance?.PatchCategory("LatePatches");
                XeraLogger.Info("XeraDruchii: LatePatches category applied.");
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error applying LatePatches", ex);
            }
        }

        public override void BeginGameStart(Game game)
        {
            try
            {
                XeraLogger.Info($"XeraDruchii: BeginGameStart for {game.GameType.GetType().Name}");
                base.BeginGameStart(game);
            }
            catch (Exception ex)
            {
                XeraLogger.Error("XeraDruchii: Error in BeginGameStart:", ex);
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            // Ensure TOR_Core systems are ready and our data is loaded
            try
            {
                SpellcasterValidator.EnsureSystemsReady();
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Failed to ensure systems ready in OnBeforeInitialModuleScreenSetAsRoot", ex);
            }

            // Try to get temporary logger from ButterLib
            try
            {
                var logger = this.GetTempServiceProvider()?.GetService<Microsoft.Extensions.Logging.ILogger<SubModule>>();
                if (logger != null)
                {
                    XeraLogger.SetButterLogger(logger);
                    XeraLogger.Info("ButterLib temporary logger initialized.");
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Failed to initialize ButterLib temporary logger", ex);
            }

            // Check if TOR_Core is available
            try
            {
                var torCoreAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "TOR_Core");

                if (torCoreAssembly == null)
                {
                    XeraLogger.Warn("TOR_Core not found. Some features may not work.");
                }
                else
                {
                    XeraLogger.Info($"TOR_Core found: {torCoreAssembly.GetName().Version}");
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error checking TOR_Core dependency", ex);
            }
        }

        // ButterLib uses this name via reflection to finalize service provider setup
        protected void OnAfterInitialModuleScreenSetAsRoot()
        {
            try
            {
                var logger = this.GetServiceProvider()?.GetService<Microsoft.Extensions.Logging.ILogger<SubModule>>();
                if (logger != null)
                {
                    XeraLogger.SetButterLogger(logger);
                    XeraLogger.Info("ButterLib permanent logger initialized.");
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Failed to initialize ButterLib permanent logger", ex);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new HordeCampBehavior());
                starter.AddBehavior(new DruchiiDialogBehavior());
            }
        }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            HordeCampBehavior.Instance = null;
            DruchiiDialogBehavior.Instance = null;
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            // Validate TOR_Core systems before battle starts
            SpellcasterValidator.ValidateSystemsReady(mission);

            mission.AddMissionBehavior(new MurderousProwessBehavior());
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (Game.Current == null || !(Game.Current.GameType is Campaign) || Campaign.Current == null) return;
            var mainHero = XeraLogger.GetSafeMainHero();
            if (mainHero == null || mainHero.MapFaction == null) return;

            // Check for Home key press for Horde Camp hotkey
            if (Input.IsKeyReleased(InputKey.Home) || Input.IsKeyReleased(InputKey.PageUp))
            {
                if (HordeCampBehavior.Instance != null)
                {
                    HordeCampBehavior.Instance.OnHotkeyPressed();
                }
            }
        }
    }
}
