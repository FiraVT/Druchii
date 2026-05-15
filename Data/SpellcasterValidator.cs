using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using XeraDruchii.CampaignSystems;
using XeraDruchii.Utilities;

namespace XeraDruchii.Data
{
    /// <summary>
    /// Validates that all TOR_Core systems required for XeraDruchii units are ready.
    /// If data is missing (e.g. due to Harmony hook timing issues), it attempts to manually load it.
    /// </summary>
    public static class SpellcasterValidator
    {
        private static bool _staticDataValidated = false;
        private static bool _instanceDataValidated = false;

        public static void EnsureSystemsReady()
        {
            ValidateSystemsReady(null);
        }

        public static void ValidateSystemsReady(Mission mission)
        {
            try
            {
                if (!_staticDataValidated)
                {
                    ValidateStaticData();
                }

                if (!_instanceDataValidated || mission != null)
                {
                    ValidateInstanceData();
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error($"Error during system verification: {ex.Message}");
            }
        }

        private static void ValidateStaticData()
        {
            XeraLogger.Info("Validating TOR_Core static data for XeraDruchii...");

            // Check 1: Status Effect Manager
            var statusEffectField = AccessTools.Field(typeof(StatusEffectManager), "_idToStatusEffect");
            var statusEffectDict = statusEffectField?.GetValue(null) as Dictionary<string, StatusEffectTemplate>;

            if (statusEffectField == null || statusEffectDict == null)
            {
                XeraLogger.Warn("StatusEffectManager not initialized.");
            }
            else if (!statusEffectDict.ContainsKey("de_murderous_prowess_melee"))
            {
                XeraLogger.Info("Druchii status effects missing. Manually triggering load...");
                DataManager.LoadStatusEffects();
            }

            // Check 2: Triggered Effect Manager
            var triggeredEffectField = AccessTools.Field(typeof(TriggeredEffectManager), "_dictionary");
            var triggeredEffectDict = triggeredEffectField?.GetValue(null) as Dictionary<string, TriggeredEffectTemplate>;

            if (triggeredEffectField == null || triggeredEffectDict == null)
            {
                XeraLogger.Warn("TriggeredEffectManager not initialized.");
            }
            else if (!triggeredEffectDict.ContainsKey("de_doombolt_trigger"))
            {
                XeraLogger.Info("Druchii triggered effects missing. Manually triggering load...");
                DataManager.LoadTriggeredEffects();
                DataManager.InjectCustomResources();
            }

            // Check 3: Ability Factory
            var abilityField = AccessTools.Field(typeof(AbilityFactory), "_templates");
            var abilityDict = abilityField?.GetValue(null) as Dictionary<string, AbilityTemplate>;

            if (abilityField == null || abilityDict == null)
            {
                XeraLogger.Error("AbilityFactory not initialized!");
            }
            else if (!abilityDict.ContainsKey("LesserDoomBolt"))
            {
                XeraLogger.Info("Druchii ability templates missing. Manually triggering load...");
                DataManager.LoadAbilityTemplates();
            }

            // Check 4: Extended Info Manager (Static part)
            var infoField = AccessTools.Field(typeof(ExtendedInfoManager), "_characterInfos");
            var infoDict = infoField?.GetValue(null) as Dictionary<string, CharacterExtendedInfo>;
            if (infoField == null || infoDict == null)
            {
                XeraLogger.Warn("ExtendedInfoManager character info missing.");
            }
            else if (!infoDict.ContainsKey("tor_de_doomfire_warlocks"))
            {
                XeraLogger.Info("Druchii extended info missing. Manually triggering injection...");
                DataManager.InjectExtendedUnitProperties(infoDict);
            }

            // Check 4.5: Druchii Culture and Faction in TORConstants
            try 
            {
                if (!TORConstants.Cultures.All.Contains("druchii"))
                {
                    XeraLogger.Info("Druchii culture missing from TORConstants. Adding...");
                    TORConstants.Cultures.All.Add("druchii");
                }
                if (!TORConstants.Factions.All.Contains("druchii_kingdom"))
                {
                    XeraLogger.Info("Druchii kingdom missing from TORConstants. Adding...");
                    TORConstants.Factions.All.Add("druchii_kingdom");
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error injecting TORConstants: " + ex.Message);
            }

            _staticDataValidated = VerifyCriticalDataLoaded();
        }

        private static void ValidateInstanceData()
        {
            if (Game.Current == null) return;

            XeraLogger.Info("Validating TOR_Core instance data for XeraDruchii...");
            bool allInstanceOk = true;

            // Check 1.5: Religion Object Cache
            try
            {
                if (MBObjectManager.Instance != null)
                {
                    var druchiiReligion = MBObjectManager.Instance.GetObject<ReligionObject>("cult_of_khaine");
                    if (druchiiReligion != null)
                    {
                        if (!ReligionObject.All.Contains(druchiiReligion))
                        {
                            XeraLogger.Info("Druchii religion missing from cache. Refreshing ReligionObject.All...");
                            ReligionObject.FillAll();
                        }
                    }
                    else
                    {
                        allInstanceOk = false;
                    }
                }
                else
                {
                    allInstanceOk = false;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Warn("Failed to verify/refresh ReligionObject cache: " + ex.Message);
                allInstanceOk = false;
            }

            // Check 3.5: Career Cache and Buttons
            try
            {
                if (MBObjectManager.Instance != null)
                {
                    var sorceress = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiSorceress");
                    var mercenary = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiMercenary");
                    
                    if (sorceress != null)
                    {
                        // 3.5.2: Career Buttons Injection
                        if (CareerButtons.Instance != null)
                        {
                            var buttonsField = AccessTools.Field(typeof(CareerButtons), "_careerButtons");
                            var buttonsDict = buttonsField?.GetValue(CareerButtons.Instance) as Dictionary<string, CareerButtonBehaviorBase>;
                            if (buttonsDict != null)
                            {
                                if (!buttonsDict.ContainsKey(sorceress.StringId))
                                {
                                    XeraLogger.Info("Druchii Sorceress button missing. Injecting...");
                                    buttonsDict[sorceress.StringId] = new DruchiiSorceressCareerButtonBehavior(sorceress);
                                }
                                if (mercenary != null && !buttonsDict.ContainsKey(mercenary.StringId))
                                {
                                    XeraLogger.Info("Druchii Mercenary button missing. Injecting...");
                                    buttonsDict[mercenary.StringId] = new DruchiiMercenaryCareerButtonBehavior(mercenary);
                                }
                            }
                        }
                    }
                    else
                    {
                        allInstanceOk = false;
                    }
                }
                else
                {
                    allInstanceOk = false;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Warn("Failed to verify/refresh Career cache or buttons: " + ex.Message);
                allInstanceOk = false;
            }

            // Check 5: MainHero Extended Info
            try
            {
                var mainHero = XeraLogger.GetSafeMainHero();
                if (Campaign.Current != null && mainHero != null && ExtendedInfoManager.Instance != null)
                {
                    if (mainHero.CharacterObject == null) return;
                    
                    var heroInfoField = AccessTools.Field(typeof(ExtendedInfoManager), "_heroInfos");
                    var heroInfoDict = heroInfoField?.GetValue(ExtendedInfoManager.Instance) as Dictionary<string, HeroExtendedInfo>;
                    if (heroInfoDict != null)
                    {
                        var key = mainHero.GetInfoKey();
                        if (key != null && !heroInfoDict.ContainsKey(key))
                        {
                            XeraLogger.Info("MainHero missing from ExtendedInfoManager. Adding entry...");
                            heroInfoDict[key] = new HeroExtendedInfo(mainHero.CharacterObject);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Warn("Failed to ensure MainHero extended info: " + ex.Message);
            }

            _instanceDataValidated = allInstanceOk;
        }

        private static bool VerifyCriticalDataLoaded()
        {
            try
            {
                var statusEffectDict = AccessTools.Field(typeof(StatusEffectManager), "_idToStatusEffect")?.GetValue(null) as Dictionary<string, StatusEffectTemplate>;
                var triggeredEffectDict = AccessTools.Field(typeof(TriggeredEffectManager), "_dictionary")?.GetValue(null) as Dictionary<string, TriggeredEffectTemplate>;
                var abilityDict = AccessTools.Field(typeof(AbilityFactory), "_templates")?.GetValue(null) as Dictionary<string, AbilityTemplate>;

                bool ok = true;
                if (statusEffectDict == null || !statusEffectDict.ContainsKey("de_murderous_prowess_melee")) ok = false;
                if (triggeredEffectDict == null || !triggeredEffectDict.ContainsKey("de_doombolt_trigger")) ok = false;
                if (abilityDict == null || !abilityDict.ContainsKey("LesserDoomBolt")) ok = false;
                
                return ok;
            }
            catch { return false; }
        }

        /// <summary>
        /// Checks if a specific ability is safe to use on a unit.
        /// </summary>
        public static bool IsAbilitySafe(string abilityId)
        {
            try
            {
                var field = AccessTools.Field(typeof(AbilityFactory), "_templates");
                var dict = field?.GetValue(null) as Dictionary<string, AbilityTemplate>;
                return dict != null && dict.Count > 0 && dict.ContainsKey(abilityId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation status.
        /// </summary>
        public static bool AreSystemsReady()
        {
            return _staticDataValidated && _instanceDataValidated;
        }
    }
}
