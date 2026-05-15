using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using XeraDruchii.Utilities;

namespace XeraDruchii.CampaignSystems
{
    public class DruchiiSorceressCareerButtonBehavior : TOR_Core.CharacterDevelopment.CareerSystem.CareerButton.CareerButtonBehaviorBase
    {
        public DruchiiSorceressCareerButtonBehavior(CareerObject career) : base(career) { }

        public override string CareerButtonIcon => "CareerSystem\\spell_icon";

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
        {
            if (isPrisoner && characterObject != null)
            {
                // Simple implementation: Execute prisoner for Slaves
                int count = characterObject.Level;
                XeraLogger.Info("Sacrificed prisoner for slaves.");
                var mainHero = XeraLogger.GetSafeMainHero();
                if (mainHero != null)
                {
                    mainHero.AddCustomResource("Slaves", count);
                    MobileParty.MainParty.PrisonRoster.AddToCountsAtIndex(MobileParty.MainParty.PrisonRoster.FindIndexOfTroop(characterObject), -1);
                    InformationManager.DisplayMessage(new InformationMessage(characterObject.Name.ToString() + " has been sacrificed. You gained " + count.ToString() + " Slaves."));
                }
            }
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            var mainHero = XeraLogger.GetSafeMainHero();
            return mainHero != null && mainHero.GetCareer()?.StringId == "DruchiiSorceress" && isPrisoner && characterObject != null && !characterObject.IsHero;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText = new TextObject("{=str_druchii_sacrifice_desc}Sacrifice this prisoner to gain Slaves.");
            return true;
        }
    }

    public class DruchiiMercenaryCareerButtonBehavior : TOR_Core.CharacterDevelopment.CareerSystem.CareerButton.CareerButtonBehaviorBase
    {
        private CharacterObject _currentTemplate;
        private int _price = 5000;

        public DruchiiMercenaryCareerButtonBehavior(CareerObject career) : base(career) { }

        public override string CareerButtonIcon => "CareerSystem\\ghal_maraz";

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
        {
            if (characterObject != null)
            {
                InitiateDialog(characterObject.StringId);
            }
        }

        private void InitiateDialog(string troopID)
        {
            isDialogStart = true;
            var characterTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            Game.Current.GameStateManager.PopState(0);

            if (characterTemplate == null) return;

            _price = 500 * characterTemplate.Level;
            GameTexts.SetVariable("MERCCOMPANIONPRICE", _price.ToString());
            _currentTemplate = characterTemplate;
            
            var mainHero = XeraLogger.GetSafeMainHero();
            if (mainHero == null) return;

            ConversationCharacterData characterData = new ConversationCharacterData(_currentTemplate, null);
            ConversationCharacterData playerData = new ConversationCharacterData(mainHero.CharacterObject, mainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData, characterData);
        }

        public void MakeDruchiiCompanion()
        {
            var mainHero = XeraLogger.GetSafeMainHero();
            if (mainHero == null || _currentTemplate == null) return;

            var hero = HeroCreator.CreateSpecialHero(_currentTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);
            hero.SetNewOccupation(Occupation.Special);
            GiveGoldAction.ApplyBetweenCharacters(mainHero, null, _price);
            AddCompanionAction.Apply(mainHero.Clan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentTemplate), -1);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            var mainHero = XeraLogger.GetSafeMainHero();
            return mainHero != null && mainHero.GetCareer()?.StringId == "DruchiiMercenary" && !isPrisoner && characterObject != null && !characterObject.IsHero && characterObject.Culture?.StringId == "druchii";
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText = new TextObject("{=str_druchii_mercenary_button_desc}Convert this veteran Druchii into a loyal companion.");
            
            var mainHero = XeraLogger.GetSafeMainHero();
            if (mainHero == null) return false;

            if (mainHero.Clan.Companions.Count >= Campaign.Current.Models.ClanTierModel.GetCompanionLimit(mainHero.Clan))
            {
                displayText = new TextObject("{=str_druchii_companion_limit}Clan companion limit reached.");
                return false;
            }

            if (characterObject != null && characterObject.Level < 20)
            {
                displayText = new TextObject("{=str_druchii_mercenary_low_level}Troop must be at least level 20.");
                return false;
            }

            if (characterObject != null && mainHero.Gold < 500 * characterObject.Level)
            {
                displayText = new TextObject("{=str_druchii_mercenary_no_gold}Not enough gold.");
                return false;
            }

            return true;
        }
    }
}
