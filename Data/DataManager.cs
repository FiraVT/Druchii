using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using HarmonyLib;
using NLog;
using TaleWorlds.ModuleManager;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.CharacterCreation;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using XeraDruchii.CampaignSystems;
using XeraDruchii.Utilities;

namespace XeraDruchii.Data
{
    /// <summary>
    /// Manages loading and registration of all custom data (abilities, status effects, items, etc.)
    /// </summary>
    public static class DataManager
    {
        private static string _modulePath;
        public static string ModulePath
        {
            get
            {
                if (string.IsNullOrEmpty(_modulePath))
                    _modulePath = ModuleHelper.GetModuleFullPath("XeraDruchii");
                return _modulePath;
            }
        }

        public static void LoadStatusEffects(string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_de_statuseffects.xml");
                if (!File.Exists(path)) return;

                var ser = new XmlSerializer(typeof(List<StatusEffectTemplate>), new XmlRootAttribute("StatusEffects"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<StatusEffectTemplate>;
                    if (list == null) return;

                    var field = AccessTools.Field(typeof(StatusEffectManager), "_idToStatusEffect");
                    if (field == null)
                    {
                        XeraLogger.Error("CRITICAL - Cannot find StatusEffectManager._idToStatusEffect field.");
                        return;
                    }

                    var dict = field.GetValue(null) as Dictionary<string, StatusEffectTemplate>;
                    if (dict == null)
                    {
                        XeraLogger.Error("CRITICAL - StatusEffectManager dictionary is null.");
                        return;
                    }

                    foreach (var item in list)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.StringID))
                        {
                            dict[item.StringID] = item;
                            XeraLogger.Debug($"Registered status effect: {item.StringID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading status effects: " + ex.Message);
            }
        }

        public static void LoadTriggeredEffects(string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_de_triggeredeffects.xml");
                if (!File.Exists(path)) return;

                var ser = new XmlSerializer(typeof(List<TriggeredEffectTemplate>), new XmlRootAttribute("TriggeredEffectTemplates"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<TriggeredEffectTemplate>;
                    if (list == null) return;

                    var field = AccessTools.Field(typeof(TriggeredEffectManager), "_dictionary");
                    if (field == null)
                    {
                        XeraLogger.Error("CRITICAL - Cannot find TriggeredEffectManager._dictionary field.");
                        return;
                    }

                    var dict = field.GetValue(null) as Dictionary<string, TriggeredEffectTemplate>;
                    if (dict == null)
                    {
                        XeraLogger.Error("CRITICAL - TriggeredEffectManager dictionary is null.");
                        return;
                    }

                    foreach (var item in list)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.StringID))
                        {
                            dict[item.StringID] = item;
                            XeraLogger.Debug($"Registered triggered effect: {item.StringID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading triggered effects: " + ex.Message);
            }
        }

        public static void LoadAbilityTemplates(string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_de_abilitytemplates.xml");
                if (!File.Exists(path)) return;

                var ser = new XmlSerializer(typeof(List<AbilityTemplate>), new XmlRootAttribute("AbilityTemplates"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<AbilityTemplate>;
                    if (list == null) return;

                    var field = AccessTools.Field(typeof(AbilityFactory), "_templates");
                    if (field == null)
                    {
                        XeraLogger.Error("CRITICAL - Cannot find AbilityFactory._templates field.");
                        return;
                    }

                    var dict = field.GetValue(null) as Dictionary<string, AbilityTemplate>;
                    if (dict == null)
                    {
                        XeraLogger.Error("CRITICAL - AbilityFactory dictionary is null.");
                        return;
                    }

                    foreach (var item in list)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.StringID))
                        {
                            dict[item.StringID] = item;
                            XeraLogger.Debug($"Registered ability template: {item.StringID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading ability templates: " + ex.Message);
            }
        }

        public static void InjectExtendedUnitProperties(Dictionary<string, CharacterExtendedInfo> infos, string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;

                // Load unit extended properties
                string unitPath = Path.Combine(modulePath, "ModuleData", "tor_de_extendedunitproperties.xml");
                if (File.Exists(unitPath))
                {
                    LoadExtendedInfoFile(unitPath, infos);
                }

                // Load companion extended properties
                string companionPath = Path.Combine(modulePath, "ModuleData", "tor_de_companion_extendedinfo.xml");
                if (File.Exists(companionPath))
                {
                    LoadExtendedInfoFile(companionPath, infos);
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error injecting extended unit properties: " + ex.Message);
            }
        }

        private static void LoadExtendedInfoFile(string filePath, Dictionary<string, CharacterExtendedInfo> infos)
        {
            var ser = new XmlSerializer(typeof(List<CharacterExtendedInfo>), new XmlRootAttribute("ArrayOfCharacterExtendedInfo"));
            using (var reader = File.OpenRead(filePath))
            {
                var list = ser.Deserialize(reader) as List<CharacterExtendedInfo>;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (item == null || string.IsNullOrEmpty(item.CharacterStringId)) continue;
                        MergeExtendedInfo(infos, item);
                    }
                }
            }
        }

        private static void MergeExtendedInfo(Dictionary<string, CharacterExtendedInfo> infos, CharacterExtendedInfo item)
        {
            if (infos.TryGetValue(item.CharacterStringId, out var existing))
            {
                if (existing.Abilities == null) existing.Abilities = new List<string>();
                if (existing.CharacterAttributes == null) existing.CharacterAttributes = new List<string>();

                if (item.Abilities != null)
                {
                    foreach (var ab in item.Abilities)
                    {
                        if (ab != null && !existing.Abilities.Contains(ab))
                            existing.Abilities.Add(ab);
                    }
                }
                if (item.CharacterAttributes != null)
                {
                    foreach (var attr in item.CharacterAttributes)
                    {
                        if (attr != null && !existing.CharacterAttributes.Contains(attr))
                            existing.CharacterAttributes.Add(attr);
                    }
                }
            }
            else
            {
                infos[item.CharacterStringId] = item;
            }
        }

        public static void LoadItemTraits(string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_de_itemtraits.xml");
                if (!File.Exists(path)) return;

                var ser = new XmlSerializer(typeof(List<ItemTrait>), new XmlRootAttribute("ItemTraits"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<ItemTrait>;
                    if (list == null) return;

                    if (ItemTraitManager.Instance == null)
                    {
                        XeraLogger.Warn("ItemTraitManager not initialized yet...");
                        return;
                    }

                    var field = AccessTools.Field(typeof(ItemTraitManager), "_itemTraits");
                    if (field == null)
                    {
                        XeraLogger.Error("CRITICAL - Cannot find ItemTraitManager._itemTraits field.");
                        return;
                    }

                    var existingList = field.GetValue(ItemTraitManager.Instance) as List<ItemTrait>;
                    if (existingList == null)
                    {
                        XeraLogger.Error("CRITICAL - ItemTraitManager list is null.");
                        return;
                    }

                    foreach (var trait in list)
                    {
                        if (trait != null && !existingList.Any(t => t.ItemTraitStringId == trait.ItemTraitStringId))
                        {
                            existingList.Add(trait);
                            XeraLogger.Debug($"Registered item trait: {trait.ItemTraitStringId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading item traits: " + ex.Message);
            }
        }

        public static void LoadExtendedItemProperties(string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_de_extendeditemproperties.xml");
                if (!File.Exists(path)) return;

                var ser = new XmlSerializer(typeof(List<ExtendedItemObjectProperties>), new XmlRootAttribute("ArrayOfExtendedItemObjectProperties"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<ExtendedItemObjectProperties>;
                    if (list == null) return;

                    var field = AccessTools.Field(typeof(ExtendedItemObjectManager), "_itemToInfoMap");
                    if (field == null)
                    {
                        XeraLogger.Error("CRITICAL - Cannot find ExtendedItemObjectManager._itemToInfoMap field.");
                        return;
                    }

                    var dict = field.GetValue(null) as Dictionary<string, ExtendedItemObjectProperties>;
                    if (dict == null)
                    {
                        XeraLogger.Error("CRITICAL - ExtendedItemObjectManager dictionary is null.");
                        return;
                    }

                    foreach (var item in list)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.ItemStringId))
                        {
                            dict[item.ItemStringId] = item;
                            XeraLogger.Debug($"Registered extended item property: {item.ItemStringId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading extended item properties: " + ex.Message);
            }
        }

        public static void LoadCharacterCreationOptions(List<CharacterCreationOption> options, string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_cc_options.xml");
                if (!File.Exists(path)) return;

                if (options.Any(o => o.Id != null && o.Id.Contains("_de_"))) return;

                var ser = new XmlSerializer(typeof(List<CharacterCreationOption>), new XmlRootAttribute("ArrayOfCharacterCreationOption"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<CharacterCreationOption>;
                    if (list != null)
                    {
                        options.AddRange(list);
                        XeraLogger.Info($"XeraDruchii: Loaded {list.Count} character creation options.");
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading character creation options: " + ex.Message);
            }
        }

        public static void LoadSpecializationOptions(List<SpecializationOption> options, string modulePath = null)
        {
            try
            {
                if (modulePath == null) modulePath = ModulePath;
                if (string.IsNullOrEmpty(modulePath)) return;
                string path = Path.Combine(modulePath, "ModuleData", "tor_specialization_options.xml");
                if (!File.Exists(path)) return;

                if (options.Any(o => o.Id != null && o.Id.Contains("_de_"))) return;

                var ser = new XmlSerializer(typeof(List<SpecializationOption>), new XmlRootAttribute("ArrayOfSpecializationOption"));
                using (var reader = File.OpenRead(path))
                {
                    var list = ser.Deserialize(reader) as List<SpecializationOption>;
                    if (list != null)
                    {
                        options.AddRange(list);
                        XeraLogger.Info($"XeraDruchii: Loaded {list.Count} specialization options.");
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error loading specialization options: " + ex.Message);
            }
        }

        public static void InjectCareersAndChoices()
        {
            try
            {
                if (MBObjectManager.Instance == null) return;
                
                var sorceress = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiSorceress");
                var mercenary = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiMercenary");

                if (sorceress == null || mercenary == null)
                {
                    XeraLogger.Warn("Druchii careers not found in MBObjectManager during injection.");
                    return;
                }

                // 1. Inject into TORCareers.All
                var careersField = AccessTools.Field(typeof(TORCareers), "_allCareers");
                if (careersField != null && TORCareers.Instance != null)
                {
                    var all = careersField.GetValue(TORCareers.Instance) as IEnumerable<CareerObject>;
                    if (all != null)
                    {
                        var list = all.ToList();
                        bool added = false;
                        if (!list.Contains(sorceress)) { list.Add(sorceress); added = true; }
                        if (!list.Contains(mercenary)) { list.Add(mercenary); added = true; }
                        if (added)
                        {
                            careersField.SetValue(TORCareers.Instance, new MBReadOnlyList<CareerObject>(list));
                            XeraLogger.Info("Druchii careers injected into TORCareers.All");
                        }
                    }
                }

                // 2. Inject into TORCareerChoices.Instance
                if (TORCareerChoices.Instance != null)
                {
                    var choicesField = AccessTools.Field(typeof(TORCareerChoices), "_allCareerChoices");
                    if (choicesField?.GetValue(TORCareerChoices.Instance) is List<TORCareerChoicesBase> choices)
                    {
                        bool added = false;
                        if (!choices.Any(x => x.GetID() == sorceress))
                        {
                            choices.Add(new DruchiiSorceressCareerChoices(sorceress));
                            added = true;
                        }
                        if (!choices.Any(x => x.GetID() == mercenary))
                        {
                            choices.Add(new DruchiiMercenaryCareerChoices(mercenary));
                            added = true;
                        }
                        if (added) XeraLogger.Info("Druchii career choices injected into TORCareerChoices");
                    }
                }

                // 3. Inject into CareerButtons.Instance
                var cbInstance = TOR_Core.CharacterDevelopment.CareerSystem.CareerButton.CareerButtons.Instance;
                if (cbInstance != null)
                {
                    var buttonsField = AccessTools.Field(typeof(TOR_Core.CharacterDevelopment.CareerSystem.CareerButton.CareerButtons), "_careerButtons");
                    if (buttonsField?.GetValue(cbInstance) is Dictionary<string, TOR_Core.CharacterDevelopment.CareerSystem.CareerButton.CareerButtonBehaviorBase> buttons)
                    {
                        if (!buttons.ContainsKey("DruchiiSorceress"))
                        {
                            buttons.Add("DruchiiSorceress", new DruchiiSorceressCareerButtonBehavior(sorceress));
                        }
                        if (!buttons.ContainsKey("DruchiiMercenary"))
                        {
                            buttons.Add("DruchiiMercenary", new DruchiiMercenaryCareerButtonBehavior(mercenary));
                        }
                        XeraLogger.Info("Druchii career buttons injected into CareerButtons.Instance");
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in InjectCareersAndChoices: " + ex.Message);
            }
        }

        public static void InjectHirelingActivities(object activitiesInstance)
        {
            try
            {
                if (activitiesInstance == null) return;
                
                var sorceress = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiSorceress");
                var mercenary = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiMercenary");

                if (sorceress == null || mercenary == null) return;

                var field = AccessTools.Field(activitiesInstance.GetType(), "_activitySets");
                if (field?.GetValue(activitiesInstance) is Dictionary<CareerObject, List<SkillObject>> dict)
                {
                    if (!dict.ContainsKey(sorceress))
                    {
                        dict[sorceress] = new List<SkillObject>
                        {
                            TORSkills.Spellcraft,
                            DefaultSkills.Charm,
                            DefaultSkills.Riding,
                            DefaultSkills.Roguery,
                            DefaultSkills.Medicine
                        };
                        XeraLogger.Info("Druchii Sorceress hireling activities injected.");
                    }

                    if (!dict.ContainsKey(mercenary))
                    {
                        dict[mercenary] = new List<SkillObject>
                        {
                            DefaultSkills.OneHanded,
                            DefaultSkills.TwoHanded,
                            DefaultSkills.Crossbow,
                            DefaultSkills.Athletics,
                            DefaultSkills.Roguery
                        };
                        XeraLogger.Info("Druchii Mercenary hireling activities injected.");
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in InjectHirelingActivities: " + ex.Message);
            }
        }

        public static void InjectCustomResources()
        {
            try
            {
                var type = AccessTools.TypeByName("TOR_Core.CampaignMechanics.CustomResources.CustomResourceManager");
                if (type == null) return;

                var instanceField = AccessTools.Property(type, "Instance");
                var instance = instanceField?.GetValue(null);
                if (instance == null) return;

                var resourcesField = AccessTools.Field(type, "_resources");
                var dict = resourcesField?.GetValue(instance) as System.Collections.IDictionary;
                if (dict != null)
                {
                    if (!dict.Contains("Slaves"))
                    {
                        var resourceType = AccessTools.TypeByName("TOR_Core.CampaignMechanics.CustomResources.CustomResource");
                        if (resourceType != null)
                        {
                            // public CustomResource(string id, string iconName, string cultureId, GetResourceInfoDelegate getInfoDelegate = null)
                            var delegateType = resourceType.GetNestedType("TextToolTipFunction");
                            Delegate tooltipDelegate = null;
                            if (delegateType != null)
                            {
                                tooltipDelegate = Delegate.CreateDelegate(delegateType, typeof(DruchiiCustomResourceHelpers).GetMethod("GetSlavesResourceTooltip"));
                            }
                            
                            var resource = Activator.CreateInstance(resourceType, new object[] { "Slaves", "slave_icon_45", "druchii", tooltipDelegate });
                            if (resource != null)
                            {
                                dict["Slaves"] = resource;
                                XeraLogger.Info("Injected Slaves custom resource with tooltip for Druchii.");
                            }
                        }
                    }
                    if (dict.Contains("Slaves"))
                    {
                        // Update existing Slaves resource if it doesn't have druchii
                        var resource = dict["Slaves"];
                        if (resource != null)
                        {
                            var culturesProp = AccessTools.Property(resource.GetType(), "Cultures");
                            var culturesList = culturesProp?.GetValue(resource) as List<string>;
                            if (culturesList != null && !culturesList.Contains("druchii"))
                            {
                                culturesList.Add("druchii");
                                XeraLogger.Info("Added Druchii to existing Slaves custom resource.");
                            }
                            
                            // Also ensure tooltip is set if it's null
                            var functionField = AccessTools.Field(resource.GetType(), "_getInfoDelegate");
                            if (functionField != null && functionField.GetValue(resource) == null)
                            {
                                var delegateType = resource.GetType().GetNestedType("TextToolTipFunction");
                                if (delegateType != null)
                                {
                                    var tooltipDelegate = Delegate.CreateDelegate(delegateType, typeof(DruchiiCustomResourceHelpers).GetMethod("GetSlavesResourceTooltip"));
                                    functionField.SetValue(resource, tooltipDelegate);
                                    XeraLogger.Info("Applied Druchii tooltip to existing Slaves custom resource.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in InjectCustomResources: " + ex.Message);
            }
        }
        public static void InjectAllHirelingActivities(object activitiesInstance)
        {
            try
            {
                if (activitiesInstance == null) return;
                var field = AccessTools.Field(activitiesInstance.GetType(), "_activitySets");
                var dict = field?.GetValue(activitiesInstance) as Dictionary<CareerObject, List<SkillObject>>;
                if (dict == null) return;

                // We need to replicate the original TOR_Core logic but safely
                // Or just fill it for all careers in TORCareers.All
                foreach (var career in TORCareers.All)
                {
                    if (dict.ContainsKey(career)) continue;

                    var skills = new List<SkillObject>(5);
                    // Use some sensible defaults based on career if possible, or just native ones
                    if (career.StringId.Contains("Wizard") || career.StringId.Contains("Magister") || career.StringId.Contains("Sorceress"))
                    {
                        skills.Add(TORSkills.Spellcraft);
                        skills.Add(DefaultSkills.Charm);
                        skills.Add(DefaultSkills.Riding);
                        skills.Add(DefaultSkills.Roguery);
                        skills.Add(DefaultSkills.Medicine);
                    }
                    else if (career.StringId.Contains("Knight") || career.StringId.Contains("Warrior"))
                    {
                        skills.Add(DefaultSkills.OneHanded);
                        skills.Add(DefaultSkills.TwoHanded);
                        skills.Add(DefaultSkills.Riding);
                        skills.Add(DefaultSkills.Polearm);
                        skills.Add(DefaultSkills.Leadership);
                    }
                    else
                    {
                        skills.Add(DefaultSkills.OneHanded);
                        skills.Add(DefaultSkills.Athletics);
                        skills.Add(DefaultSkills.Roguery);
                        skills.Add(DefaultSkills.Scouting);
                        skills.Add(DefaultSkills.Leadership);
                    }
                    dict[career] = skills;
                }
                XeraLogger.Info("Injected all missing hireling activities.");
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in InjectAllHirelingActivities: " + ex.Message);
            }
        }
    }
}
