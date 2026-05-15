using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;
using TOR_Core.CharacterDevelopment;
using XeraDruchii.Utilities;

namespace XeraDruchii.HarmonyPatches.Abilities
{
    [HarmonyPatch(typeof(Ability), "DoCast")]
    public static class KindleflamePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Ability __instance, Agent casterAgent)
        {
            if (__instance == null || __instance.Template == null || casterAgent == null) return;

            if (__instance.Template.AbilityType == AbilityType.Spell && 
                __instance.Template.BelongsToLoreID != null && __instance.Template.BelongsToLoreID.Equals("LoreOfFire", StringComparison.OrdinalIgnoreCase) && 
                casterAgent.HasAttribute("Kindleflame"))
            {
                ApplyKindleflameMapWide(casterAgent);
            }
        }

        private static void ApplyKindleflameMapWide(Agent casterAgent)
        {
            if (Mission.Current == null || casterAgent == null || casterAgent.Team == null) return;

            var enemies = Mission.Current.Agents.Where(a => 
                a.IsActive() && 
                a.Team != null && 
                a.Team.IsEnemyOf(casterAgent.Team)
            );

            var enemyList = enemies.ToList();
            if (enemyList.Count == 0) return;

            TORMissionHelper.ApplyStatusEffectToAgents(
                enemyList, 
                "de_kindleflame_weakness", 
                casterAgent, 
                15f
            );

            InformationManager.DisplayMessage(new InformationMessage(
                "Kindleflame: Enemies are now more vulnerable to fire!", 
                Color.FromUint(0xFFFF4500)
            ));
        }
    }

    [HarmonyPatch(typeof(ExtendedInfoManager), "InitializeTemplatedHeroStats")]
    public static class InitializeTemplatedHeroStatsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hero hero)
        {
            try
            {
                if (hero == null || hero.CharacterObject == null) return;

                string templateId = hero.Template?.StringId;
                if (templateId != null && (templateId.Equals("tor_wanderer_druchii_sorceress_dark_0", StringComparison.OrdinalIgnoreCase) || 
                                          templateId.Equals("tor_wanderer_druchii_sorceress_fire_0", StringComparison.OrdinalIgnoreCase)))
                {
                    // Ensure they are recognized as spellcasters
                    hero.AddAttribute("SpellCaster");
                    hero.AddAttribute("AbilityUser");

                    // Initialize Magic stats
                    hero.AddKnownLore("MinorMagic");
                    if (templateId.Contains("fire"))
                    {
                        hero.AddKnownLore("LoreOfFire");
                    }
                    else
                    {
                        hero.AddKnownLore("DarkMagic");
                    }

                    // Set casting level based on their high Spellcraft (200)
                    hero.SetSpellCastingLevel(SpellCastingLevel.Adept);

                    // Initialize Winds of Magic resource using extension method
                    hero.AddWindsOfMagic(100f);
                    
                    XeraLogger.Info("Initialized magic stats for " + hero.Name.ToString());
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in InitializeTemplatedHeroStatsPatch: " + ex.Message);
            }
        }
    }
}
