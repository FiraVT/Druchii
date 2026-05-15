using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using XeraDruchii.Utilities;


namespace XeraDruchii.Behaviors
{
    public class HordeCampComponent
    {
        [SaveableField(103)] public Dictionary<string, int> Buildings = new Dictionary<string, int>();
        [SaveableField(104)] public string CurrentConstructionId = null;
        [SaveableField(105)] public int ConstructionDaysRemaining = 0;
        [SaveableField(106)] public int RecruitmentCapacity = 0;
        [SaveableField(107)] public CampaignTime LastRecruitmentRefresh = CampaignTime.Never;
        [SaveableField(108)] public bool IsActive = false;

        public int GetBuildingLevel(string id) => Buildings.TryGetValue(id, out var level) ? level : 0;
        public void UpgradeBuilding(string id) { if (Buildings.ContainsKey(id)) Buildings[id]++; else Buildings[id] = 1; }
        public int Slaves 
        {
            get
            {
                var mainHero = XeraLogger.GetSafeMainHero();
                if (Campaign.Current == null || mainHero == null || mainHero.Culture == null) return 0;
                try
                {
                    // Check if CustomResourceManager is initialized to avoid crash in extension method
                    if (TOR_Core.CampaignMechanics.CustomResources.CustomResourceManager.Instance == null) return 0;
                    var resource = mainHero.GetCultureSpecificCustomResource();
                    return resource != null ? (int)mainHero.GetCustomResourceValue(resource.StringId) : 0;
                }
                catch { return 0; }
            }
        }
        public void AddSlaves(int count)
        {
            var mainHero = XeraLogger.GetSafeMainHero();
            if (Campaign.Current != null && mainHero != null && mainHero.Culture != null)
            {
                try
                {
                    if (TOR_Core.CampaignMechanics.CustomResources.CustomResourceManager.Instance != null)
                    {
                        mainHero.AddCultureSpecificCustomResource(count);
                    }
                }
                catch { }
            }
        }
        public bool TrySpendSlaves(int count) 
        { 
            var mainHero = XeraLogger.GetSafeMainHero();
            if (Campaign.Current == null || mainHero == null || mainHero.Culture == null) return false;
            try
            {
                if (TOR_Core.CampaignMechanics.CustomResources.CustomResourceManager.Instance == null) return false;
                var resource = mainHero.GetCultureSpecificCustomResource();
                if (resource == null) return false;
                float currentSlaves = mainHero.GetCustomResourceValue(resource.StringId);
                if (currentSlaves >= count) 
                { 
                    mainHero.AddCustomResource(resource.StringId, -count); 
                    return true; 
                } 
            }
            catch { }
            return false; 
        }
        public bool IsConstructing => !string.IsNullOrEmpty(CurrentConstructionId);
        public void StartConstruction(string id, int days) { CurrentConstructionId = id; ConstructionDaysRemaining = days; }
        public void CompleteConstruction() { if (IsConstructing) { UpgradeBuilding(CurrentConstructionId); CurrentConstructionId = null; ConstructionDaysRemaining = 0; } }
    }
}
