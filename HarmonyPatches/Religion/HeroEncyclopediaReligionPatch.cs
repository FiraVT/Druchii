using HarmonyLib;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using XeraDruchii.Utilities;
using System;
using System.Linq;

namespace XeraDruchii.HarmonyPatches.Religion
{
    [HarmonyPatch(typeof(HeroEncyclopediaVMExtension), "RefreshValues")]
    public static class HeroEncyclopediaReligionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(HeroEncyclopediaVMExtension __instance, ref Hero ____hero, ViewModel ____vm)
        {
            var heroVM = ____vm as EncyclopediaHeroPageVM;
            if (heroVM == null) return true;

            try
            {
                if (____hero == null)
                {
                    ____hero = heroVM.Obj as Hero;
                }

                if (____hero != null)
                {
                    var religionText = TORTextHelper.GetText("tor_religion_follower_none", "Not a follower of any religion");
                    
                    var dominantReligion = ____hero.GetDominantReligion();
                    if (____hero.HasAnyReligion() && dominantReligion != null)
                    {
                        var devotionLevel = ____hero.GetDevotionLevelForReligion(dominantReligion);
                        var devotionLevelText = GameTexts.FindText("tor_religion_devotionlevel", devotionLevel.ToString());
                        var religionNameText = GameTexts.FindText("tor_religion_name_of_god", dominantReligion.StringId);
                        
                        var link = HyperlinkTexts.GetSettlementHyperlinkText(dominantReligion.EncyclopediaLink, religionNameText);
                        
                        MBTextManager.SetTextVariable("TOR_DEVOTION_LEVEL", devotionLevelText);
                        MBTextManager.SetTextVariable("TOR_RELIGION", link);
                        
                        if (GameTexts.TryGetText("tor_religion_text_frame", out var frameText))
                        {
                            religionText = frameText.ToString();
                        }
                    }

                    var label = TORTextHelper.GetTextObject("tor_religion_label", "Religion") + ": ";

                    // Remove existing religion entries to avoid duplicates
                    var existingStats = heroVM.Stats.ToList();
                    foreach (var item in existingStats)
                    {
                        if (item.Definition == label.ToString())
                        {
                            heroVM.Stats.Remove(item);
                        }
                    }
                    
                    heroVM.Stats.Add(new StringPairItemVM(label.ToString(), religionText));
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("XeraDruchii: Error in HeroEncyclopediaVMExtension.RefreshValues for " + (____hero?.StringId ?? "unknown"), ex);
            }

            return false; // Skip original method
        }
    }
}
