using System;

using TaleWorlds.CampaignSystem;


using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using Helpers;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Models;
using TOR_Core.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using XeraDruchii.Utilities;
namespace XeraDruchii.Behaviors
{
    public class HordeCampBehavior : CampaignBehaviorBase
    {
        public static HordeCampBehavior Instance;

        private HordeCampComponent _hordeCampData;
        private bool _isDeployed = false;
        private Vec2 _lastKnownPos = Vec2.Invalid;
        private bool? _isDruchiiCached = null;

        public bool IsDeployed => _isDeployed;

        public HordeCampComponent CampData
        {
            get
            {
                if (_hordeCampData == null)
                {
                    _hordeCampData = new HordeCampComponent();
                }
                return _hordeCampData;
            }
        }

        public bool IsDruchiiPlayer
        {
            get
            {
                if (_isDruchiiCached.HasValue) return _isDruchiiCached.Value;
                bool result = CalculateIsDruchii();
                // Only cache if the result is true OR if the hero is valid (meaning we've had a fair chance to check)
                var mainHero = XeraLogger.GetSafeMainHero();
                if (result || (Campaign.Current != null && mainHero != null && mainHero.Culture != null))
                {
                    _isDruchiiCached = result;
                }
                return result;
            }
        }

        private bool CalculateIsDruchii()
        {
            try
            {
                var hero = XeraLogger.GetSafeMainHero();
                if (hero != null && hero.Culture != null)
                {
                    string cultureId = hero.Culture.StringId;
                    if (!string.IsNullOrEmpty(cultureId))
                    {
                        if (cultureId.Equals(TORConstants.Cultures.DRUCHII, StringComparison.OrdinalIgnoreCase) ||
                            cultureId.Equals("druchii_unlocked", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                var mainParty = (Campaign.Current != null) ? MobileParty.MainParty : null;
                if (mainParty != null && mainParty.ActualClan != null && mainParty.ActualClan.Culture != null)
                {
                    string clanCultureId = mainParty.ActualClan.Culture.StringId;
                    if (!string.IsNullOrEmpty(clanCultureId))
                    {
                        if (clanCultureId.Equals(TORConstants.Cultures.DRUCHII, StringComparison.OrdinalIgnoreCase) ||
                            clanCultureId.Equals("druchii_unlocked", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                // Use Debug.Print as fallback if XeraLogger is part of the problem
                TaleWorlds.Library.Debug.Print("XeraDruchii: Error calculating IsDruchiiPlayer: " + ex.Message);
                XeraLogger.Error("Error calculating IsDruchiiPlayer", ex);
                return false;
            }
        }

        private bool IsDruchiiCareerCompleted(Hero hero)
        {
            if (hero == null) return false;
            var career = hero.GetCareer();
            if (career == null) return false;
            // Check if it's a Druchii career and if they have unlocked the high tiers
            if (career.StringId.Equals("DruchiiSorceress", StringComparison.OrdinalIgnoreCase) || 
                career.StringId.Equals("DruchiiMercenary", StringComparison.OrdinalIgnoreCase))
            {
                // In TOR, careers usually have 3 main tiers. 
                return hero.HasUnlockedCareerChoiceTier(3);
            }
            return false;
        }

        private void SpawnGrandSlaverParty()
        {
            try
            {
                var settlement = MBObjectManager.Instance.GetObject<Settlement>("darkelf_camp_01");
                if (settlement != null && settlement.SettlementComponent is TOR_Core.CampaignMechanics.TORCustomSettlement.SlaverCampComponent comp)
                {
                    comp.SpawnNewParty(out var party, null);
                    if (party != null)
                    {
                        party.Party.SetCustomName(new TextObject("Druchii Grand Slavers"));
                        
                        // 1. Purge basic troops and replace with Elite Roster
                        party.MemberRoster.Clear();
                        
                        // Use corrected IDs from tor_de_new_troops.xml
                        var blackGuard = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_black_guard");
                        var executioner = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_har_ganeth_executioner");
                        var sister = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_sister_of_slaughter");
                        var witchElf = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_witch_elves");
                        var darkshard = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_darkshard_shield");
                        var rider = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_darkrider_shield");
                        var warlock = MBObjectManager.Instance.GetObject<CharacterObject>("tor_de_doomfire_warlocks");

                        if (blackGuard != null) party.MemberRoster.AddToCounts(blackGuard, 30);
                        if (executioner != null) party.MemberRoster.AddToCounts(executioner, 30);
                        if (sister != null) party.MemberRoster.AddToCounts(sister, 20);
                        if (witchElf != null) party.MemberRoster.AddToCounts(witchElf, 20);
                        if (darkshard != null) party.MemberRoster.AddToCounts(darkshard, 40);
                        if (rider != null) party.MemberRoster.AddToCounts(rider, 20);
                        
                        // Limit to 2 Doomfire Warlocks as requested
                        if (warlock != null) party.MemberRoster.AddToCounts(warlock, 2);

                        // 2. Assign an Elite Dreadlord as the Party Leader
                        var lordTemplate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_druchii_lord_0");
                        if (lordTemplate != null)
                        {
                            party.MemberRoster.AddToCounts(lordTemplate, 1);
                        }

                        // 3. Logistics for long-range raiding
                        party.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, 100));
                        party.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, 200));
                        
                        XeraLogger.Info("Spawned Druchii Grand Slaver party with elite composition.");
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error spawning Grand Slaver party: " + ex.Message);
            }
        }

        public HordeCampBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            if (CampaignEvents.MapEventEnded != null)
            {
                CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            }
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public void OnHotkeyPressed()
        {
            if (!IsDruchiiPlayer) 
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=horde_camp_fail_druchii}Encampment deployment failed: Not a Druchii player.").ToString()));
                return;
            }
            
            if (_isDeployed)
            {
                GameMenu.ActivateGameMenu("horde_camp_menu");
                return;
            }

            if (MobileParty.MainParty.CurrentSettlement != null)
            {
                MBInformationManager.AddQuickInformation(new TextObject("You cannot deploy an encampment inside another settlement."));
                return;
            }

            if (MobileParty.MainParty.IsCurrentlyAtSea)
            {
                MBInformationManager.AddQuickInformation(new TextObject("You cannot deploy an encampment at sea."));
                return;
            }

            Settlement nearestSettlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 10f, s => true);
            if (nearestSettlement != null && nearestSettlement.GatePosition.Distance(MobileParty.MainParty.Position.ToVec2()) < 1.5f)
            {
                var text = new TextObject("You are too close to {SETTLEMENT_NAME} to deploy a camp.");
                text.SetTextVariable("SETTLEMENT_NAME", nearestSettlement.Name);
                MBInformationManager.AddQuickInformation(text);
                return;
            }

            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=horde_camp_deploy_open}Opening Druchii Encampment deployment menu...").ToString()));
            InformationManager.ShowInquiry(new InquiryData(
                "Establish Druchii Encampment",
                "Do you wish to establish a camp at this location?",
                true, true,
                "Yes", "No",
                () => DeployCamp(),
                null
            ), true);
        }

        private void OnDailyTick()
        {
            // Early exit if game state is invalid
            var mainHero = XeraLogger.GetSafeMainHero();
            if (Campaign.Current == null || MobileParty.MainParty == null || mainHero == null || mainHero.Culture == null)
            {
                return;
            }

            try
            {
                var data = CampData;
                if (data == null) return;

                // Clear cached Druchii status on each tick
                _isDruchiiCached = null;


                // Daily Slave Upkeep
                if (data.Slaves > 0)
                {
                    int slavePensLevel = data.GetBuildingLevel("slave_pens");
                    
                    // -80% attrition at level 3
                    float reductionFactor = 0f;
                    if (slavePensLevel == 1) reductionFactor = 0.25f;
                    else if (slavePensLevel == 2) reductionFactor = 0.50f;
                    else if (slavePensLevel >= 3) reductionFactor = 0.80f;

                    float lossRate = 0.02f * (1f - reductionFactor);
                    
                    // Calculate expected float loss (e.g., 10 slaves * 0.02 = 0.2 slaves)
                    float expectedLoss = data.Slaves * lossRate;
                    
                    // Probabilistic rounding
                    int finalLoss = (expectedLoss - (int)expectedLoss > MBRandom.RandomFloat) ? (int)expectedLoss + 1 : (int)expectedLoss;
                    
                    if (finalLoss > 0)
                    {
                        data.AddSlaves(-finalLoss);
                        
                        // Localization-friendly notification
                        var text = new TextObject("{LOSS_COUNT} slaves have died or escaped from the camp.");
                        text.SetTextVariable("LOSS_COUNT", finalLoss);
                        InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                    }
                }

                // Construction Progress
                if (_isDeployed && data.IsConstructing)
                {
                    data.ConstructionDaysRemaining--;
                    if (data.ConstructionDaysRemaining <= 0)
                    {
                        string constructionId = data.CurrentConstructionId;
                        string buildingName = constructionId != null ?
                            constructionId.Replace("_", " ").ToUpper() : "STRUCTURE";
                        data.CompleteConstruction();

                        if (constructionId != null && constructionId.Equals("encampment_main", StringComparison.OrdinalIgnoreCase))
                        {
                            data.RecruitmentCapacity += 2;
                            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=horde_camp_upgrade_msg}Encampment upgraded! Daily recruitment capacity increased by 2.").ToString()));
                        }
                        
                        var text = new TextObject("{=horde_camp_build_complete}Construction Complete: {BUILDING_NAME}!");
                        text.SetTextVariable("BUILDING_NAME", buildingName);
                        MBInformationManager.AddQuickInformation(text); 
                    }
                }

                // Recruitment Refresh
                if (_isDeployed && CampaignTime.Now > data.LastRecruitmentRefresh + CampaignTime.Days(1))
                {
                    int mainLevel = data.GetBuildingLevel("encampment_main");
                    data.RecruitmentCapacity = 2 + (mainLevel * 2);
                    data.LastRecruitmentRefresh = CampaignTime.Now;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in OnDailyTick: " + ex.Message, ex);
            }
        }

        private void OnHourlyTick()
        {
            // Early exit if game state is invalid
            if (Campaign.Current == null || MobileParty.MainParty == null)
            {
                return;
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                if (Campaign.Current == null) return;
                
                _isDruchiiCached = null;
                AddGameMenus(starter);

                // Inject careers and choices safely during session launch
                XeraDruchii.Data.DataManager.InjectCareersAndChoices();
                // Inject custom resources for Druchii
                XeraDruchii.Data.DataManager.InjectCustomResources();
                
                XeraLogger.Info("Session launched, Horde Camp initialized.");
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in OnSessionLaunched: " + ex.Message, ex);
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent == null || !IsDruchiiPlayer) return;

            // Player Victory
            if (mapEvent.IsPlayerMapEvent && mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                var side = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
                if (side == null) return;
                int captives = side.TroopCasualties;
                var data = CampData;
                if (captives > 0 && data != null)
                {
                    int slavePensLevel = data.GetBuildingLevel("slave_pens");
                    float slaveRatio = 0.10f; // Base 10%
                    if (slavePensLevel == 1) slaveRatio = 0.20f;
                    else if (slavePensLevel == 2) slaveRatio = 0.40f;
                    else if (slavePensLevel >= 3) slaveRatio = 0.60f;

                    int slavesGained = (int)(captives * slaveRatio);
                    
                    // Camp bonus: 10% increase per Encampment level
                    int mainLevel = data.GetBuildingLevel("encampment_main");
                    if (mainLevel > 0)
                    {
                        float bonus = 1f + (mainLevel * 0.1f);
                        int originalSlaves = slavesGained;
                        slavesGained = (int)(slavesGained * bonus);
                        
                        var bonusText = new TextObject("Encampment Level {LVL} grants a {BONUS_PCT}% bonus to captives taken. (+{BONUS_VAL} slaves)");
                        bonusText.SetTextVariable("LVL", mainLevel);
                        bonusText.SetTextVariable("BONUS_PCT", mainLevel * 10);
                        bonusText.SetTextVariable("BONUS_VAL", slavesGained - originalSlaves);
                        InformationManager.DisplayMessage(new InformationMessage(bonusText.ToString()));
                    }

                    data.AddSlaves(slavesGained);
                    
                    var text = new TextObject("Gained {SLAVE_GAIN} slaves from the battle ({CAPTURE_RATE}% capture rate). Total slaves: {TOTAL_SLAVES}");
                    text.SetTextVariable("SLAVE_GAIN", slavesGained);
                    text.SetTextVariable("CAPTURE_RATE", (int)(slaveRatio * 100f));
                    text.SetTextVariable("TOTAL_SLAVES", data.Slaves);
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                }
            }
            // Tithe logic: AI raider successful raid
            else if (!mapEvent.IsPlayerMapEvent && mapEvent.IsRaid && mapEvent.WinningSide == BattleSideEnum.Attacker)
            {
                var mainHero = XeraLogger.GetSafeMainHero();
                if ((mainHero != null && mainHero.Clan != null && mainHero.Clan.Tier >= 4) || IsDruchiiCareerCompleted(mainHero))
                {
                    // Check if winner was a Druchii Slaver party
                    var winnerSide = mapEvent.AttackerSide;
                    bool isSlaverRaid = false;
                    foreach (var party in winnerSide.Parties)
                    {
                        if (party.Party.MobileParty != null && party.Party.MobileParty.StringId != null && party.Party.MobileParty.StringId.IndexOf("druchii_clan_1_party", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isSlaverRaid = true;
                            break;
                        }
                    }

                    if (isSlaverRaid)
                    {
                        var defeatedSide = mapEvent.DefenderSide;
                        int captives = defeatedSide.TroopCasualties; 
                        if (captives > 0)
                        {
                            int tithe = (int)(captives * 0.25f);
                            if (tithe > 0)
                            {
                                CampData.AddSlaves(tithe);
                                var text = new TextObject("The Dreadlord's Tithe: Received {TITHE} slaves from a successful Druchii raid.");
                                text.SetTextVariable("TITHE", tithe);
                                InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Color.FromUint(0xFF9933FF)));
                            }
                        }
                    }
                }
            }
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            try
            {
                // Initial state for new game
                var camp = MBObjectManager.Instance.GetObject<Settlement>("darkelf_camp_01");
                if (camp != null)
                {
                    // Move settlement off-map initially to prevent AI confusion
                    // Using reflection if Position2D or similar is not directly accessible/writable
                    try 
                    {
                        var field = typeof(Settlement).GetField("_position", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (field != null)
                        {
                            field.SetValue(camp, new CampaignVec2(new Vec2(-1000f, -1000f), false));
                        }
                    }
                    catch { }
                    camp.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in OnNewGameCreated: " + ex.Message, ex);
            }
        }

        private void AddGameMenus(CampaignGameStarter starter)
        {
            try
            {
                if (starter == null)
                {
                    XeraLogger.Error("AddGameMenus called with null starter");
                    return;
                }
                
                XeraLogger.Info("XeraDruchii: Adding horde camp game menus...");
                
                // Map menu option to deploy camp
                string[] parentMenus = { "map_back_to_menu", "wait_menu", "town", "castle", "village" };
                foreach (var menu in parentMenus)
                {
                    starter.AddGameMenuOption(menu, "deploy_horde_camp", "{=horde_camp_deploy}Deploy Druchii Encampment",
                        args =>
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                            return !_isDeployed && MobileParty.MainParty != null && MobileParty.MainParty.CurrentSettlement == null && IsDruchiiPlayer;
                        },
                        args => DeployCamp());
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in AddGameMenus menu deployment options:", ex);
            }
            
            try
            {
                XeraLogger.Info("XeraDruchii: Initializing horde camp menu text variables...");
                
                string factionName = "Druchii";
                string resourceName = "Slaves";
                
                MBTextManager.SetTextVariable("FACTION_NAME_UPPER", factionName.ToUpper());
                MBTextManager.SetTextVariable("RESOURCE_NAME", resourceName);
                MBTextManager.SetTextVariable("RESOURCE_NAME_UPPER", resourceName.ToUpper());

                starter.AddGameMenu("horde_camp_menu", "{=horde_camp_desc}Your {FACTION_NAME_UPPER} forces have established a Camp.\n{RESOURCE_NAME}: {SLAVE_COUNT}\nUpkeep Reduction: {UPKEEP_BONUS}% | Party Size Bonus: {PARTY_SIZE_BONUS}\nRecruitment: {RECRUIT_CAP} / {RECRUIT_MAX} per day | {RESOURCE_NAME} Capture Rate: {CAPTURE_RATE}%", 
                    OnHordeCampMenuInit);

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_wait", "{=horde_camp_wait_btn}Wait and Oversee Encampment",
                args => true,
                args => GameMenu.SwitchToMenu("horde_camp_wait"));

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_build", "{=horde_camp_build_btn}Manage Construction",
                args => true,
                args => GameMenu.SwitchToMenu("horde_camp_build_menu"));

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_sacrifice", "{=horde_camp_sacrifice_btn}Perform Sacrifice to the Gods",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    int altarLevel = data.GetBuildingLevel("sacrificial_altar");
                    args.IsEnabled = altarLevel > 0 && data.Slaves >= 50;
                    if (altarLevel == 0) args.Tooltip = new TextObject("You need to build a Sacrificial Altar first.");
                    else if (data.Slaves < 50) args.Tooltip = new TextObject("You need more resources.");
                    return true;
                },
                args => 
                {
                    var data = CampData;
                    if (data != null && data.TrySpendSlaves(50))
                    {
                        MobileParty.MainParty.RecentEventsMorale += 10f;
                        string msg = "Sacrificed 50 slaves. Khaine is pleased!";
                        InformationManager.DisplayMessage(new InformationMessage(msg));
                        
                        if (Campaign.Current.Models.GetFaithModel() is TORFaithModel model)
                        {
                            model.AddBlessingToParty(MobileParty.MainParty, "cult_of_khaine");
                        }
                        else
                        {
                            // Fallback if model not found for some reason
                            XeraLogger.GetSafeMainHero()?.AddSkillXp(TORSkills.Faith, 50);
                        }
                        
                        GameMenu.ActivateGameMenu("horde_camp_menu");
                    }
                });

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_grand_raid", "{=horde_camp_grand_raid_btn}Ritual of the Grand Raid",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    int altarLevel = data.GetBuildingLevel("sacrificial_altar");
                    args.IsEnabled = altarLevel >= 3 && data.Slaves >= 1000;
                    if (altarLevel < 3) args.Tooltip = new TextObject("Requires Sacrificial Altar Level 3.");
                    else if (data.Slaves < 1000) args.Tooltip = new TextObject("Requires 1,000 Slaves.");
                    return true;
                },
                args => 
                {
                    if (CampData.TrySpendSlaves(1000))
                    {
                        SpawnGrandSlaverParty();
                        InformationManager.DisplayMessage(new InformationMessage("A Grand Slaver party has set sail from the Druchii Slaver Camp!", Color.FromUint(0xFFCC0000)));
                        GameMenu.ActivateGameMenu("horde_camp_menu");
                    }
                });


            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_process_prisoners", "{=horde_camp_process_prisoners_btn}Incorporate Prisoners",
                args => 
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    int prisonerCount = MobileParty.MainParty.PrisonRoster.TotalManCount;
                    args.IsEnabled = prisonerCount > 0;
                    if (prisonerCount == 0) args.Tooltip = new TextObject("You have no prisoners to process.");
                    return true;
                },
                args => 
                {
                    int prisonerCount = MobileParty.MainParty.PrisonRoster.TotalManCount;
                    if (prisonerCount > 0)
                    {
                        var data = CampData;
                        if (data != null)
                        {
                            data.AddSlaves(prisonerCount);
                            MobileParty.MainParty.PrisonRoster.Clear();
                            
                            string resName = "slaves";
                            var text = new TextObject("Processed {PRISONER_COUNT} prisoners into {RESOURCE_NAME}.");
                            text.SetTextVariable("PRISONER_COUNT", prisonerCount);
                            text.SetTextVariable("RESOURCE_NAME", resName);
                            InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                            GameMenu.ActivateGameMenu("horde_camp_menu");
                        }
                    }
                });

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_recruit", "{=horde_camp_recruit_btn}Recruit (Capacity: {RECRUIT_CAP} / {RECRUIT_MAX})",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    int mainLevel = data.GetBuildingLevel("encampment_main");
                    int maxRecruitCap = 2 + (mainLevel * 2);
                    MBTextManager.SetTextVariable("RECRUIT_MAX", maxRecruitCap);

                    int tentLevel = data.GetBuildingLevel("slave_pens");
                    int warriorHallLevel = data.GetBuildingLevel("warrior_hall");
                    
                    args.IsEnabled = data.RecruitmentCapacity > 0;
                    
                    if (data.RecruitmentCapacity <= 0) 
                        args.Tooltip = new TextObject("Recruitment capacity exhausted for today.");
                    else if (tentLevel == 0 && warriorHallLevel == 0) 
                        args.Tooltip = new TextObject("Only basic conscripts can be recruited without Slave Pens.");
                    
                    return true;
                },
                args => GameMenu.SwitchToMenu("horde_camp_recruit_menu"));

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_recruit_hero", "{=horde_camp_recruit_hero_btn}Recruit Hero",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    var fireTemplate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_wanderer_druchii_sorceress_fire_0");
                    var darkTemplate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_wanderer_druchii_sorceress_dark_0");
                    if (fireTemplate == null && darkTemplate == null) return false;
                    
                    int pavilionLevel = data.GetBuildingLevel("druchii_pavilion");
                    args.IsEnabled = pavilionLevel >= 1;
                    if (pavilionLevel == 0) args.Tooltip = new TextObject("You need to build a Druchii Pavilion first.");
                    
                    return true;
                },
                args => GameMenu.SwitchToMenu("horde_camp_recruit_hero_menu"));

            // Recruitment Sub-menu
            starter.AddGameMenu("horde_camp_recruit_menu", "{=horde_camp_recruit_desc}Select troops to recruit. Each unit consumes 1 capacity point. Gold: {PLAYER_GOLD} | Capacity: {RECRUIT_CAP} / {RECRUIT_MAX}", 
                args => {
                    var data = CampData;
                    if (data != null)
                    {
                        int mainLevel = data.GetBuildingLevel("encampment_main");
                        MBTextManager.SetTextVariable("RECRUIT_CAP", data.RecruitmentCapacity);
                        MBTextManager.SetTextVariable("RECRUIT_MAX", 2 + (mainLevel * 2));
                        var mainHero = XeraLogger.GetSafeMainHero();
                        if (mainHero != null)
                        {
                            MBTextManager.SetTextVariable("PLAYER_GOLD", mainHero.Gold);
                        }
                    }
                });

            AddRecruitOption(starter, "tor_de_conscript", "Druchii Conscript", "encampment_main", 0);
            AddRecruitOption(starter, "tor_de_dreadspear", "Druchii Dreadspear", "encampment_main", 1);
            AddRecruitOption(starter, "tor_de_bleaksword", "Druchii Bleaksword", "warrior_hall", 1);
            AddRecruitOption(starter, "tor_de_darkshard", "Druchii Darkshard", "warrior_hall", 1);
            AddRecruitOption(starter, "tor_de_darkshard_shield", "Druchii Darkshard (Shielded)", "warrior_hall", 2);
            AddRecruitOption(starter, "tor_de_darkrider", "Druchii Dark Rider", "reaver_stables", 1);
            AddRecruitOption(starter, "tor_de_darkrider_shield", "Druchii Dark Rider (Shielded)", "reaver_stables", 2);
            AddRecruitOption(starter, "tor_de_darkrider_crossbow", "Druchii Dark Rider (Crossbow)", "reaver_stables", 2);
            AddRecruitOption(starter, "tor_de_doomfire_warlocks", "Doomfire Warlocks", "dread_manse", 2);
            AddRecruitOption(starter, "tor_de_witch_elves", "Witch Elves", "sacrificial_altar", 2);
            AddRecruitOption(starter, "tor_de_sister_of_slaughter", "Sisters of Slaughter", "sacrificial_altar", 3);
            AddRecruitOption(starter, "tor_de_har_ganeth_executioner", "Har Ganeth Executioner", "dread_manse", 1);
            AddRecruitOption(starter, "tor_de_black_guard", "Black Guard of Naggarond", "dread_manse", 2);

            // Hero Recruitment Sub-menu
            starter.AddGameMenu("horde_camp_recruit_hero_menu", "{=horde_camp_recruit_hero_desc}Recruit a powerful Druchii hero to lead your forces. Gold: {PLAYER_GOLD}", 
                OnHordeCampMenuInit);

            AddHeroRecruitOption(starter, "tor_wanderer_druchii_sorceress_fire_0", "Sorceress (Fire)", 30000, 0);
            AddHeroRecruitOption(starter, "tor_wanderer_druchii_sorceress_dark_0", "Sorceress (Dark)", 30000, 0);

            starter.AddGameMenuOption("horde_camp_recruit_hero_menu", "recruit_hero_back", "{=horde_camp_back_btn}Back",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.SwitchToMenu("horde_camp_menu"));

            starter.AddGameMenuOption("horde_camp_recruit_menu", "recruit_back", "{=horde_camp_back_btn}Back",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.SwitchToMenu("horde_camp_menu"));

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_pack", "{=horde_camp_pack_btn}Break Camp",
                args => true,
                args => PackUpCamp());

            starter.AddGameMenuOption("horde_camp_menu", "horde_camp_leave", "{=horde_camp_leave_btn}Leave Camp",
                args => true,
                args => GameMenu.ExitToLast());

            // Wait Menu
            starter.AddWaitGameMenu("horde_camp_wait", "{WAIT_TEXT}",
                args => { args.MenuContext.GameMenu.StartWait(); },
                null,
                args => { GameMenu.SwitchToMenu("horde_camp_menu"); },
                (args, dt) => 
                {
                    var data = CampData;
                    string waitText = "Overseeing the development of your encampment...";
                    if (data != null)
                    {
                        if (data.IsConstructing)
                        {
                            var textObj = new TextObject("Construction of {BUILDING_NAME} is in progress... ({DAYS_REMAINING} days remaining)\n");
                            textObj.SetTextVariable("BUILDING_NAME", data.CurrentConstructionId.Replace("_", " ").ToUpper());
                            textObj.SetTextVariable("DAYS_REMAINING", data.ConstructionDaysRemaining);
                            waitText = textObj.ToString();
                        }
                        
                        var statsText = new TextObject("Slaves: {SLAVE_COUNT}");
                        statsText.SetTextVariable("SLAVE_COUNT", data.Slaves);
                        waitText += statsText.ToString();
                    }
                    MBTextManager.SetTextVariable("WAIT_TEXT", waitText);
                },
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption("horde_camp_wait", "horde_camp_wait_back", "{=horde_camp_back_btn}Stop Waiting",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => { GameMenu.SwitchToMenu("horde_camp_menu"); });

            // Building Menu
            starter.AddGameMenu("horde_camp_build_menu", "{=horde_camp_build_desc}Select a structure to construct. Slaves: {SLAVE_COUNT} | Gold: {PLAYER_GOLD}{BUILD_STATUS}", 
                OnHordeCampMenuInit);

            // Encampment Main
            AddEncampmentOption(starter);

            // Warrior Hall
            AddBuildingOption(starter, "warrior_hall", "Warrior Hall", 500, 2, 3, 1, 1);
            AddBuildingOption(starter, "warrior_hall", "Warrior Hall", 5000, 5, 3, 2, 2);
            AddBuildingOption(starter, "warrior_hall", "Warrior Hall", 10000, 10, 3, 3, 3);

            // Reaver Stables
            AddBuildingOption(starter, "reaver_stables", "Reaver Stables", 1000, 3, 3, 1, 1);
            AddBuildingOption(starter, "reaver_stables", "Reaver Stables", 5000, 7, 3, 2, 2);
            AddBuildingOption(starter, "reaver_stables", "Reaver Stables", 10000, 15, 3, 3, 3);

            // Altar of Khaine
            AddBuildingOption(starter, "sacrificial_altar", "Altar of Khaine", 5000, 5, 3, 1, 2);
            AddBuildingOption(starter, "sacrificial_altar", "Altar of Khaine", 6000, 10, 3, 2, 3);
            AddBuildingOption(starter, "sacrificial_altar", "Altar of Khaine", 10000, 20, 3, 3, 4);

            // Slave Pens
            AddBuildingOption(starter, "slave_pens", "Slave Pens", 500, 1, 3, 1, 1);
            AddBuildingOption(starter, "slave_pens", "Slave Pens", 1000, 3, 3, 2, 2);
            AddBuildingOption(starter, "slave_pens", "Slave Pens", 2500, 5, 3, 3, 3);

            // Druchii Pavilion
            AddBuildingOption(starter, "druchii_pavilion", "Druchii Pavilion", 1000, 2, 3, 1, 1);
            AddBuildingOption(starter, "druchii_pavilion", "Druchii Pavilion", 2500, 5, 3, 2, 2);
            AddBuildingOption(starter, "druchii_pavilion", "Druchii Pavilion", 5000, 10, 3, 3, 3);

            // Dread Manse
            AddBuildingOption(starter, "dread_manse", "Dread Manse", 15000, 15, 3, 1, 4);
            AddBuildingOption(starter, "dread_manse", "Dread Manse", 30000, 30, 3, 2, 5);

            starter.AddGameMenuOption("horde_camp_build_menu", "build_back", "{=horde_camp_back_btn}Back",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.SwitchToMenu("horde_camp_menu"));

            // Visitor Menu
            starter.AddGameMenu("horde_camp_visitor_menu", "{=horde_camp_visitor_desc}You have encountered a Druchii encampment. The air is thick with the scent of blood and sea salt.", 
                null);

            starter.AddGameMenuOption("horde_camp_visitor_menu", "horde_camp_visitor_talk", "{=horde_camp_visitor_talk_btn}Request an audience",
                args => true,
                args => {
                    InformationManager.DisplayMessage(new InformationMessage("You exchange brief words with the camp's commander."));
                });

            starter.AddGameMenuOption("horde_camp_visitor_menu", "horde_camp_visitor_leave", "{=horde_camp_leave_btn}Leave",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.ExitToLast());
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error adding game menus:", ex);
            }
        }

        private int GetRequiredRenownForMainLevel(int level)
        {
            if (level <= 1) return 0;
            if (level == 2) return 1;
            if (level == 3) return 2;
            if (level == 4) return 3;
            if (level == 5) return 4;
            return 0;
        }

        private void AddEncampmentOption(CampaignGameStarter starter)
        {
            // Level 1: Tier 0, 100 Slaves, 1,200 Gold
            AddEncampmentLevel(starter, 1, 1200, 1, 0);
            // Level 2: Tier 1, 200 Slaves, 1,600 Gold
            AddEncampmentLevel(starter, 2, 1600, 2, 1);
            // Level 3: Tier 2, 400 Slaves, 5,000 Gold
            AddEncampmentLevel(starter, 3, 5000, 4, 2);
            // Level 4: Tier 3, 800 Slaves, 10,000 Gold
            AddEncampmentLevel(starter, 4, 10000, 8, 3);
            // Level 5: Tier 4, 1,600 Slaves, 20,000 Gold
            AddEncampmentLevel(starter, 5, 20000, 16, 4);
        }

        private void AddEncampmentLevel(CampaignGameStarter starter, int toLevel, int goldCost, int slaveCost, int renownReq)
        {
            slaveCost *= 100; // Convert previous GP cost to Slaves
            string renownStr = renownReq > 0 ? ", Clan Tier " + renownReq : "";
            starter.AddGameMenuOption("horde_camp_build_menu", "build_encampment_" + toLevel, 
                "Druchii Encampment [Lvl " + (toLevel - 1) + " -> " + toLevel + "] (" + goldCost + " Gold, " + slaveCost + " Slaves" + renownStr + ")",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    int currentLevel = data.GetBuildingLevel("encampment_main");
                    if (currentLevel != toLevel - 1) return false;

                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var mainHero = XeraLogger.GetSafeMainHero();
                    bool canAfford = mainHero != null && mainHero.Gold >= goldCost && data.Slaves >= slaveCost;
                    bool hasRenown = mainHero != null && mainHero.Clan != null && mainHero.Clan.Tier >= renownReq;
                    args.IsEnabled = canAfford && !data.IsConstructing && hasRenown;
                    
                    if (data.IsConstructing) args.Tooltip = new TextObject("Already constructing something.");
                    else if (!hasRenown) args.Tooltip = new TextObject("Requires Clan Tier " + renownReq + ".");
                    else if (!canAfford) args.Tooltip = new TextObject("Not enough resources.");
                    
                    return true;
                },
                args => 
                {
                    var data = CampData;
                    var mainHero = XeraLogger.GetSafeMainHero();
                    if (data != null && mainHero != null)
                    {
                        mainHero.Gold -= goldCost;
                        data.TrySpendSlaves(slaveCost);
                        data.StartConstruction("encampment_main", 3);
                        InformationManager.DisplayMessage(new InformationMessage("Started upgrading Druchii Encampment."));
                    }
                    GameMenu.SwitchToMenu("horde_camp_build_menu");
                });
        }

        private void AddBuildingOption(CampaignGameStarter starter, string id, string name, int goldCost, int slaveCost, int buildTimeDays, int toLevel, int reqEncLevel)
        {
            slaveCost *= 100; // Convert previous GP cost to Slaves
            starter.AddGameMenuOption("horde_camp_build_menu", "build_" + id + "_" + toLevel, 
                name + " [Lvl " + (toLevel - 1) + " -> " + toLevel + "] (" + goldCost + " Gold, " + slaveCost + " Slaves)",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    int currentLevel = data.GetBuildingLevel(id);
                    if (currentLevel != toLevel - 1) return false;

                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    
                    int mainLevel = data.GetBuildingLevel("encampment_main");
                    bool mainRequirementMet = mainLevel >= reqEncLevel;
                    
                    int renownReq = GetRequiredRenownForMainLevel(reqEncLevel);
                    var mainHero = XeraLogger.GetSafeMainHero();
                    bool hasRenown = mainHero != null && mainHero.Clan != null && mainHero.Clan.Tier >= renownReq;

                    bool canAfford = mainHero != null && mainHero.Gold >= goldCost && data.Slaves >= slaveCost;
                    args.IsEnabled = canAfford && !data.IsConstructing && mainRequirementMet && hasRenown;
                    
                    if (data.IsConstructing) args.Tooltip = new TextObject("Already constructing something.");
                    else if (!mainRequirementMet) args.Tooltip = new TextObject("Requires Druchii Encampment Level " + reqEncLevel + ".");
                    else if (!hasRenown) args.Tooltip = new TextObject("Requires Clan Tier " + renownReq + ".");
                    else if (!canAfford) args.Tooltip = new TextObject("Not enough resources.");
                    
                    return true;
                },
                args => 
                {
                    var data = CampData;
                    var mainHero = XeraLogger.GetSafeMainHero();
                    if (data != null && mainHero != null)
                    {
                        mainHero.Gold -= goldCost;
                        data.TrySpendSlaves(slaveCost);
                        data.StartConstruction(id, buildTimeDays);
                        InformationManager.DisplayMessage(new InformationMessage("Started construction of " + name + "."));
                    }
                    GameMenu.SwitchToMenu("horde_camp_build_menu");
                });
        }

        private void AddRecruitOption(CampaignGameStarter starter, string troopId, string name, string reqBuilding, int reqLevel)
        {
            var troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);
            int goldCost = 0;
            if (troop != null)
            {
                int tier = troop.Tier;
                if (tier <= 1) goldCost = 100;
                else goldCost = 500 + (tier - 2) * 500;
            }

            starter.AddGameMenuOption("horde_camp_recruit_menu", "recruit_" + troopId, name + " (" + goldCost + " Gold)",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    
                    if (troop == null) return false;

                    bool buildingMet = data.GetBuildingLevel(reqBuilding) >= reqLevel;
                    var mainHero = XeraLogger.GetSafeMainHero();
                    bool canAfford = mainHero != null && mainHero.Gold >= goldCost;
                    args.IsEnabled = buildingMet && data.RecruitmentCapacity > 0 && canAfford;
                    
                    if (!buildingMet)
                    {
                        args.Tooltip = new TextObject("Requires " + reqBuilding.Replace("_", " ") + " Level " + reqLevel);
                    }
                    else if (data.RecruitmentCapacity <= 0) 
                    {
                        args.Tooltip = new TextObject("No capacity remaining.");
                    }
                    else if (!canAfford)
                    {
                        args.Tooltip = new TextObject("Not enough gold (" + goldCost + " Gold required).");
                    }
                    
                    return true;
                },
                args => 
                {
                    var data = CampData;
                    var mainHero = XeraLogger.GetSafeMainHero();
                    if (troop != null && data != null && mainHero != null && mainHero.Gold >= goldCost)
                    {
                        mainHero.Gold -= goldCost;
                        PartyBase.MainParty.MemberRoster.AddToCounts(troop, 1);
                        data.RecruitmentCapacity--;
                        InformationManager.DisplayMessage(new InformationMessage("Recruited 1 " + name + " for " + goldCost + " gold."));
                    }
                    GameMenu.SwitchToMenu("horde_camp_recruit_menu");
                });
        }

        private void AddHeroRecruitOption(CampaignGameStarter starter, string templateId, string name, int goldCost, int slaveCost)
        {
            starter.AddGameMenuOption("horde_camp_recruit_hero_menu", "recruit_hero_" + templateId, name + " (" + goldCost + " Gold)",
                args => 
                {
                    var data = CampData;
                    if (data == null) return false;
                    
                    var template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);
                    if (template == null) return false;

                    var mainHero = XeraLogger.GetSafeMainHero();
                    bool canAfford = mainHero != null && mainHero.Gold >= goldCost && data.Slaves >= slaveCost;
                    args.IsEnabled = canAfford;
                    
                    if (!canAfford) args.Tooltip = new TextObject("Not enough resources.");
                    
                    return true;
                },
                args => 
                {
                    var template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);
                    var data = CampData;
                    var mainHeroInner = XeraLogger.GetSafeMainHero();
                    if (template != null && data != null && mainHeroInner != null)
                    {
                        mainHeroInner.Gold -= goldCost;
                        data.TrySpendSlaves(slaveCost);
                        
                        Hero hero = HeroCreator.CreateSpecialHero(template, null, Clan.PlayerClan, null, 25 + MBRandom.RandomInt(10));
                        hero.SetNewOccupation(Occupation.Wanderer);
                        
                        // Add companion and party
                        AddCompanionAction.Apply(Clan.PlayerClan, hero);
                        AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, true);
                        
                        InformationManager.DisplayMessage(new InformationMessage("Recruited " + hero.Name + "."));
                    }
                    GameMenu.SwitchToMenu("horde_camp_menu");
                });
        }

        private void OnHordeCampMenuInit(MenuCallbackArgs args)
        {
            try
            {
                var data = CampData;
                if (data == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Horde Camp Menu Error: Data is null."));
                    return;
                }

                var mainHero = XeraLogger.GetSafeMainHero();
                if (mainHero != null && mainHero.Culture != null)
                {
                    var resource = mainHero.GetCultureSpecificCustomResource();
                    if (resource != null)
                    {
                        MBTextManager.SetTextVariable("SLAVE_COUNT", (int)mainHero.GetCustomResourceValue(resource.StringId));
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("SLAVE_COUNT", 0);
                        XeraLogger.Warn("Druchii custom resource not found during menu init.");
                    }
                }
                else
                {
                    // Fallback to avoid raw tags in UI during rare null windows
                    MBTextManager.SetTextVariable("SLAVE_COUNT", 0);
                }

                int mainLevel = data.GetBuildingLevel("encampment_main");
                int warriorHallLevel = data.GetBuildingLevel("warrior_hall");
                int slavePensLevel = data.GetBuildingLevel("slave_pens");
                int pavilionLevel = data.GetBuildingLevel("druchii_pavilion");
                int altarLevel = data.GetBuildingLevel("sacrificial_altar");

                MBTextManager.SetTextVariable("ENCAMPMENT_MAIN_LVL", mainLevel);
                MBTextManager.SetTextVariable("WARRIOR_HALL_LVL", warriorHallLevel);
                MBTextManager.SetTextVariable("SLAVE_PENS_LVL", slavePensLevel);
                MBTextManager.SetTextVariable("DRUCHII_PAVILION_LVL", pavilionLevel);
                MBTextManager.SetTextVariable("SACRIFICIAL_ALTAR_LVL", altarLevel);
            
                int reaverStablesLevel = data.GetBuildingLevel("reaver_stables");
                int dreadManseLevel = data.GetBuildingLevel("dread_manse");
                MBTextManager.SetTextVariable("REAVER_STABLES_LVL", reaverStablesLevel);
                MBTextManager.SetTextVariable("DREAD_MANSE_LVL", dreadManseLevel);

                int upkeepBonus = (mainLevel * 10) + (warriorHallLevel * 5);
                int partySizeBonus = pavilionLevel * 20;

                MBTextManager.SetTextVariable("UPKEEP_BONUS", upkeepBonus);
                MBTextManager.SetTextVariable("PARTY_SIZE_BONUS", partySizeBonus);
                MBTextManager.SetTextVariable("RECRUIT_CAP", data.RecruitmentCapacity);
                MBTextManager.SetTextVariable("RECRUIT_MAX", 2 + (mainLevel * 2));
                
                if (mainHero != null)
                {
                    MBTextManager.SetTextVariable("PLAYER_GOLD", mainHero.Gold);
                }
                else
                {
                    MBTextManager.SetTextVariable("PLAYER_GOLD", 0);
                }

                float captureRate = 0.10f;
                if (slavePensLevel == 1) captureRate = 0.20f;
                else if (slavePensLevel == 2) captureRate = 0.40f;
                else if (slavePensLevel >= 3) captureRate = 0.60f;
                MBTextManager.SetTextVariable("CAPTURE_RATE", (int)(captureRate * 100f));

                if (args != null && args.MenuContext != null && args.MenuContext.GameMenu != null)
                {
                    if (args.MenuContext.GameMenu.StringId.Equals("horde_camp_build_menu", StringComparison.OrdinalIgnoreCase))
                    {
                        string buildStatus = "";
                        if (data.IsConstructing)
                        {
                            string projName = data.CurrentConstructionId != null ? data.CurrentConstructionId.Replace("_", " ").ToUpper() : "NONE";
                            buildStatus = "\n\nCURRENT PROJECT: " + projName + " (" + data.ConstructionDaysRemaining + " days remaining)";
                        }
                        MBTextManager.SetTextVariable("BUILD_STATUS", buildStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("Horde Camp Menu Init Exception: " + ex.Message));
            }
        }

        public void DeployCamp()
        {
            try
            {
                if (Campaign.Current == null || MobileParty.MainParty == null) return;
                if (_isDeployed) return;

                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=horde_camp_deploying}Establishing Druchii Encampment...").ToString()));
                
                _isDeployed = true;
                _lastKnownPos = MobileParty.MainParty.Position.ToVec2();
                
                // Make party stationary
                MobileParty.MainParty.SetMoveModeHold();
                
                // Initialize recruitment if first time
                var data = CampData;
                if (data.LastRecruitmentRefresh == CampaignTime.Never)
                {
                    data.RecruitmentCapacity = 2; 
                    data.LastRecruitmentRefresh = CampaignTime.Now;
                }
                data.IsActive = true;

                // Visual change handled by Harmony patch or direct visual update
                if (MobileParty.MainParty.Party != null)
                {
                    MobileParty.MainParty.Party.SetVisualAsDirty();
                }

                GameMenu.ActivateGameMenu("horde_camp_menu");
                XeraLogger.Info("Camp deployed at " + _lastKnownPos);
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in DeployCamp: " + ex.Message, ex);
            }
        }

        public void PackUpCamp()
        {
            if (!_isDeployed) return;

            try
            {
                _isDeployed = false;
                _lastKnownPos = Vec2.Invalid;
                
                if (MobileParty.MainParty != null)
                {
                    MobileParty.MainParty.SetMoveModeHold(); // or similar to reset
                    // There isn't a direct 'SetMoveModeResume', it usually happens on next move order
                }

                var data = CampData;
                data.IsActive = false;

                if (MobileParty.MainParty?.Party != null)
                {
                    MobileParty.MainParty.Party.SetVisualAsDirty();
                }

                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=horde_camp_packed}Encampment packed up.").ToString()));
                XeraLogger.Info("Camp packed up");
                
                GameMenu.ExitToLast();
            }
            catch (Exception ex)
            {
                XeraLogger.Error("Error in PackUpCamp: " + ex.Message, ex);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData("_isDeployed", ref _isDeployed);
                dataStore.SyncData("_lastKnownPos", ref _lastKnownPos);
                dataStore.SyncData("_hordeCampData", ref _hordeCampData);
            }
            catch (Exception ex)
            {
                XeraLogger.Error("XeraDruchii: Error during horde camp save data sync:", ex);
            }
        }
    }
}
