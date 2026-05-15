using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;

namespace XeraDruchii.CampaignSystems
{
    /// <summary>
    /// Career choice definitions for Druchii Sorceress career
    /// </summary>
    public class DruchiiSorceressCareerChoices : TORCareerChoicesBase
    {
        public DruchiiSorceressCareerChoices(CareerObject careerId) : base(careerId) { }
        protected override void RegisterAll()
        {
            var root = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DruchiiSorceressRoot"));
            root.Initialize(CareerID, "Harness the dark power of the Convent. Your magic is a weapon of absolute destruction, leaving nothing but husks in your wake.", null, true, ChoiceType.Keystone);
        }
        protected override void InitializeKeyStones() { }
        protected override void InitializePassives() { }
    }

    /// <summary>
    /// Career choice definitions for Druchii Mercenary career
    /// </summary>
    public class DruchiiMercenaryCareerChoices : TORCareerChoicesBase
    {
        public DruchiiMercenaryCareerChoices(CareerObject careerId) : base(careerId) { }
        protected override void RegisterAll()
        {
            var root = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DruchiiMercenaryRoot"));
            root.Initialize(CareerID, "A life of calculated violence and profit. You lead your company with cold efficiency, selling your blade to the highest bidder.", null, true, ChoiceType.Keystone);
        }
        protected override void InitializeKeyStones() { }
        protected override void InitializePassives() { }
    }
}
