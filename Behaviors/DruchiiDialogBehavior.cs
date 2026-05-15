using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using XeraDruchii.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace XeraDruchii.Behaviors
{
    public class DruchiiDialogBehavior : CampaignBehaviorBase
    {
        public static DruchiiDialogBehavior Instance;
        private Dictionary<string, CampaignTime> _partiesWithSpoils = new Dictionary<string, CampaignTime>();

        public DruchiiDialogBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddDialogs(starter);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent != null && mapEvent.IsRaid && mapEvent.WinningSide == BattleSideEnum.Attacker)
            {
                foreach (var party in mapEvent.AttackerSide.Parties)
                {
                    if (party.Party.MobileParty != null && party.Party.MobileParty.StringId != null && party.Party.MobileParty.StringId.Contains("druchii_clan_1_party"))
                    {
                        _partiesWithSpoils[party.Party.MobileParty.StringId] = CampaignTime.Now;
                    }
                }
            }
        }

        private void AddDialogs(CampaignGameStarter starter)
        {
            // "Hand Over the Spoils"
            // Greeting from the slaver party if they have spoils and player is high renown or high level
            starter.AddDialogLine("druchii_slaver_handover_start", "start", "druchii_slaver_handover_answer",
                "{=druchii_dialog_01}My Lord! We have taken many captives from the nearby villages. They are yours to command.",
                () => IsDruchiiSlaverWithSpoils(), null, 200);

            starter.AddPlayerLine("druchii_slaver_handover_demand", "druchii_slaver_handover_answer", "close_window",
                "{=druchii_dialog_02}I have need of your captives. Hand over the spoils.",
                () => {
                    var mainHero = XeraLogger.GetSafeMainHero();
                    return mainHero != null && mainHero.Clan != null && mainHero.Clan.Tier >= 3;
                },
                () => HandOverSpoils());

            starter.AddPlayerLine("druchii_slaver_handover_ignore", "druchii_slaver_handover_answer", "close_window",
                "{=druchii_dialog_03}Carry on with your work.",
                null, null);
        }

        private bool IsDruchiiSlaverWithSpoils()
        {
            var party = PlayerEncounter.EncounteredMobileParty;
            if (party == null || party.StringId == null || !party.StringId.Contains("druchii_clan_1_party")) return false;
            
            if (_partiesWithSpoils.TryGetValue(party.StringId, out var time))
            {
                // Spoils are valid for 48 hours
                return time.ElapsedHoursUntilNow <= 48;
            }
            return false;
        }

        private void HandOverSpoils()
        {
            var party = PlayerEncounter.EncounteredMobileParty;
            if (party != null && HordeCampBehavior.Instance != null)
            {
                int slaves = MBRandom.RandomInt(50, 101);
                HordeCampBehavior.Instance.CampData.AddSlaves(slaves);
                
                // Reduce party size
                int reduction = (int)(party.MemberRoster.TotalManCount * 0.2f);
                if (reduction > 0)
                {
                    party.MemberRoster.RemoveNumberOfNonHeroTroopsRandomly(reduction);
                }

                // Order to return to camp
                var camp = MBObjectManager.Instance.GetObject<Settlement>("darkelf_camp_01");
                if (camp != null)
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(party, camp, MobileParty.NavigationType.Default, true, false);
                }

                _partiesWithSpoils.Remove(party.StringId);
                
                InformationManager.DisplayMessage(new InformationMessage($"Received {slaves} slaves from the raiders.", Color.FromUint(0xFF9933FF)));
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_partiesWithSpoils", ref _partiesWithSpoils);
        }
    }
}
