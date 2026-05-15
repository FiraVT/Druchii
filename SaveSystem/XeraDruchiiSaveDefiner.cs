using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using XeraDruchii.Behaviors;

namespace XeraDruchii.SaveSystem
{
    /// <summary>
    /// Saveable type definer for XeraDruchii classes.
    /// Uses base ID 772000.
    /// </summary>
    public class XeraDruchiiSaveDefiner : SaveableTypeDefiner
    {
        public XeraDruchiiSaveDefiner() : base(772000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(HordeCampComponent), 1);
            AddClassDefinition(typeof(HordeCampBehavior), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(Dictionary<string, CampaignTime>));
        }
    }
}
