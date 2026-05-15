using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using TOR_Core.CampaignMechanics.CharacterCreation;
using TORCCOption = TOR_Core.CampaignMechanics.CharacterCreation.CharacterCreationOption;
using TORSpecOption = TOR_Core.CampaignMechanics.CharacterCreation.SpecializationOption;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions.ExtendedInfoSystem;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.CharacterCreation
{
    [HarmonyPatch(typeof(TORCharacterCreationContentHandler))]
    public static class CharacterCreationPatches
    {
        [HarmonyPatch("OnOptionSelected")]
        [HarmonyPrefix]
        public static void OnOptionSelectedPrefix(TORCharacterCreationContentHandler __instance, CharacterCreationManager manager, string optionId)
        {
            if (optionId != null && optionId.Equals("option_3_de_sorceress", StringComparison.OrdinalIgnoreCase))
            {
                AccessTools.Field(typeof(TORCharacterCreationContentHandler), "_isFemale")?.SetValue(__instance, true);
            }
        }

        [HarmonyPatch("ApplyProfessionBonuses")]
        [HarmonyPostfix]
        public static void ApplyProfessionBonusesPostfix(TORCharacterCreationContentHandler __instance)
        {
            var hero = XeraLogger.GetSafeMainHero();
            if (hero == null || hero.Culture == null) return;

            bool isDruchii = hero.Culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase);

            // All Druchii get Murderous Prowess and a spawn location override
            if (isDruchii)
            {
                hero.AddAttribute("MurderousProwess");
                var khaine = MBObjectManager.Instance.GetObject<ReligionObject>("cult_of_khaine");
                if (khaine != null)
                {
                    hero.AddReligiousInfluence(khaine, 100, false);
                }
                AccessTools.Field(typeof(TORCharacterCreationContentHandler), "_storedSpawnPosition")?.SetValue(__instance, new CampaignVec2(new TaleWorlds.Library.Vec2(918.8679f, 1025.561f), true));
            }

            string selectedProfessionId = AccessTools.Field(typeof(TORCharacterCreationContentHandler), "_selectedProfessionId")?.GetValue(__instance) as string;
            if (selectedProfessionId == null) return;

            if (selectedProfessionId.Equals("option_3_de_sorceress", StringComparison.OrdinalIgnoreCase))
            {
                hero.AddAttribute("SpellCaster");
                // Lore and Abilities moved to ApplyStoredSpecializations
                hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                var sorceress = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiSorceress");
                if (sorceress != null) hero.AddCareer(sorceress);
            }
            else if (selectedProfessionId.Equals("option_3_de_mercenary", StringComparison.OrdinalIgnoreCase))
            {
                var mercenary = MBObjectManager.Instance.GetObject<CareerObject>("DruchiiMercenary");
                if (mercenary != null) hero.AddCareer(mercenary);
            }
        }

        [HarmonyPatch("ApplyStoredSpecializations")]
        [HarmonyPostfix]
        public static void ApplyStoredSpecializationsPostfix(TORCharacterCreationContentHandler __instance)
        {
            string specId = AccessTools.Field(typeof(TORCharacterCreationContentHandler), "_selectedSpecializationOptionId")?.GetValue(__instance) as string;
            if (string.IsNullOrEmpty(specId)) return;
            
            var hero = XeraLogger.GetSafeMainHero();
            if (hero == null || hero.Culture == null) return;

            switch (specId)
            {
                case "lore_of_dark_magic":
                    hero.AddKnownLore("DarkMagic");
                    hero.AddAbility("LesserDoomBolt");
                    break;
                case "lore_of_fire_de":
                    hero.AddKnownLore("LoreOfFire");
                    hero.AddAbility("BoltOfAqshy");
                    break;
            }
        }

        [HarmonyPatch("OnCultureSelected")]
        [HarmonyPrefix]
        public static bool OnCultureSelectedPrefix(TORCharacterCreationContentHandler __instance)
        {
            try
            {
                var player = XeraLogger.GetSafePlayerCharacter();
                var culture = player?.Culture;
                
                if (culture != null && culture.StringId.Equals("druchii", StringComparison.OrdinalIgnoreCase))
                {
                    if (player != null)
                    {
                        player.Race = TaleWorlds.Core.FaceGen.GetRaceOrDefault("elf");
                        string default_elf = "<BodyProperties version='4' age='25.84' weight='0.0015' build='0.4228'  key='000AAC0800001007B97634CE6774B835537D86629511323BDCB177278A84020300A606030A48B49500000000000000000000000000000000000000003F4C1000'/>";
                        if (BodyProperties.FromString(default_elf, out BodyProperties properties))
                            player.UpdatePlayerCharacterBodyProperties(properties, player.Race, player.IsFemale);
                        
                        if (culture.DefaultBattleEquipmentRoster != null)
                            player.Equipment.FillFrom(culture.DefaultBattleEquipmentRoster.DefaultEquipment);
                        
                        if (culture.DefaultCivilianEquipmentRoster != null)
                            player.FirstCivilianEquipment.FillFrom(culture.DefaultCivilianEquipmentRoster.DefaultEquipment);
                        
                        var emptyEquipment = new Equipment();
                        player.FirstStealthEquipment.FillFrom(emptyEquipment, false);

                        // Clear Extended Info for clean start
                        if (Hero.MainHero != null)
                        {
                            ExtendedInfoManager.Instance.ClearInfo(Hero.MainHero);
                        }

                        // Call visual update via reflection
                        var updateVisualsMethod = AccessTools.Method(typeof(TORCharacterCreationContentHandler), "UpdateVisuals");
                        updateVisualsMethod?.Invoke(__instance, new object[] { player.Race });
                    }
                    return false; // Skip original method for Druchii to avoid Switch crash
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in OnCultureSelectedPrefix: " + ex.Message);
            }
            return true;
        }

        [HarmonyPatch("OnCultureSelected")]
        [HarmonyPostfix]
        public static void OnCultureSelectedPostfix(TORCharacterCreationContentHandler __instance)
        {
            // Postfix can remain for any additional logic if needed, but Prefix handled most of it for Druchii
        }
    }
}
