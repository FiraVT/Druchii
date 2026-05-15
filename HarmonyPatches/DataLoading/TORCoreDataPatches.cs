using System.Collections.Generic;
using HarmonyLib;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.CampaignMechanics.CharacterCreation;
using XeraDruchii.Data;

namespace XeraDruchii.HarmonyPatches.DataLoading
{
    [HarmonyPatch(typeof(StatusEffectManager), nameof(StatusEffectManager.LoadStatusEffects))]
    public static class StatusEffectManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => DataManager.LoadStatusEffects();
    }

    [HarmonyPatch(typeof(TriggeredEffectManager), nameof(TriggeredEffectManager.LoadTemplates))]
    public static class TriggeredEffectManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => DataManager.LoadTriggeredEffects();
    }

    [HarmonyPatch(typeof(AbilityFactory), nameof(AbilityFactory.LoadTemplates))]
    public static class AbilityFactoryPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => DataManager.LoadAbilityTemplates();
    }

    [HarmonyPatch(typeof(ExtendedInfoManager), "TryLoadCharacters")]
    public static class ExtendedInfoManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Dictionary<string, CharacterExtendedInfo> infos)
        {
            if (infos == null) return;
            DataManager.InjectExtendedUnitProperties(infos);
        }
    }

    [HarmonyPatch(typeof(ItemTraitManager), nameof(ItemTraitManager.LoadItemTraits))]
    public static class ItemTraitManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => DataManager.LoadItemTraits();
    }

    [HarmonyPatch(typeof(ExtendedItemObjectManager), nameof(ExtendedItemObjectManager.LoadXML))]
    public static class ExtendedItemObjectManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => DataManager.LoadExtendedItemProperties();
    }

    [HarmonyPatch(typeof(TORCharacterCreationContentHandler), "InitializeContent")]
    public static class CharacterCreationContentPatch
    {
        [HarmonyPrefix]
        public static void Prefix(TORCharacterCreationContentHandler __instance)
        {
            var optionsField = AccessTools.Field(typeof(TORCharacterCreationContentHandler), "_options");
            if (optionsField?.GetValue(__instance) is List<CharacterCreationOption> options)
            {
                DataManager.LoadCharacterCreationOptions(options);
            }
            var specField = AccessTools.Field(typeof(TORCharacterCreationContentHandler), "_specializationOptions");
            if (specField?.GetValue(__instance) is List<SpecializationOption> specOptions)
            {
                DataManager.LoadSpecializationOptions(specOptions);
            }
        }
    }
}
