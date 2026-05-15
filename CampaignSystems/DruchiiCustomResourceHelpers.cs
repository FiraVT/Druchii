using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TOR_Core.Models;
using TOR_Core.Extensions;
using XeraDruchii.Utilities;

namespace XeraDruchii.CampaignSystems
{
    public static class DruchiiCustomResourceHelpers
    {
        public static List<TooltipProperty> GetSlavesResourceTooltip()
        {
            List<TooltipProperty> list = new List<TooltipProperty>();
            var mainHero = XeraLogger.GetSafeMainHero();
            if (mainHero == null) return list;

            list.Add(new TooltipProperty(new TextObject("{=str_tor_custom_resource_name_Slaves}Slaves").ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
            list.Add(new TooltipProperty("", new TextObject("{=str_tor_custom_resource_description_Slaves}Captives from raids and conquests, used to fuel the Druchii war machine and dark rituals.").ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
            
            var model = Campaign.Current.Models.GetCustomResourceModel();
            if (model != null)
            {
                ExplainedNumber change = model.GetCultureSpecificCustomResourceChange(mainHero, "Slaves");
                list.Add(new TooltipProperty(new TextObject("{=str_druchii_daily_change}Daily Change").ToString(), change.ResultNumber.ToString("0.##"), 0));
                
                // Add breakdown if there is any change
                foreach (var line in change.GetLines())
                {
                    list.Add(new TooltipProperty(line.Item1, line.Item2.ToString("0.##"), 0));
                }
            }

            return list;
        }
    }
}
