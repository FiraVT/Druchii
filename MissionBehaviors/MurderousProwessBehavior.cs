using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using XeraDruchii.Utilities;

namespace XeraDruchii.MissionBehaviors
{
    /// <summary>
    /// Battle behavior that tracks kills and triggers "Murderous Prowess" buff
    /// when enough blood has been shed on the battlefield.
    /// </summary>
    public class MurderousProwessBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private float _prowessPoints = 0f;
        private float _threshold = -1f;
        private bool _hasTriggered = false;
        private bool _halfwayTriggered = false;
        private bool _isDruchiiPresent = false;
        private readonly object _lock = new object();
        private SoundEvent _hornSound;

        protected override void OnEndMission()
        {
            base.OnEndMission();
            _hornSound?.Release();
            _hornSound = null;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            CheckDruchiiPresence();
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            try
            {
                if (!_isDruchiiPresent && agent?.Character != null)
                {
                    if (agent.HasAttribute("MurderousProwess"))
                    {
                        _isDruchiiPresent = true;
                    }
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Warn("Error in OnAgentCreated: " + ex.Message);
            }
        }

        private void CheckDruchiiPresence()
        {
            if (Mission == null || Mission.Agents == null || Mission.Agents.Count == 0)
            {
                return;
            }

            // Create a safe copy to avoid collection modified exceptions
            var agentsCopy = new List<Agent>(Mission.Agents.Count);
            for (int i = 0; i < Mission.Agents.Count; i++)
            {
                if (Mission.Agents[i] != null)
                    agentsCopy.Add(Mission.Agents[i]);
            }

            foreach (var agent in agentsCopy)
            {
                if (agent == null || agent.Character == null) continue;

                try
                {
                    if (agent.HasAttribute("MurderousProwess"))
                    {
                        _isDruchiiPresent = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    XeraLogger.Warn($"Error checking agent {agent.Character.StringId}: {ex.Message}");
                }
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == null || Mission == null || Mission.Agents == null || !_isDruchiiPresent)
                return;

            lock (_lock)
            {
                if (_hasTriggered) return;

                if (_threshold < 0) InitializeThreshold();

                float contribution = 0f;

                try
                {
                    if (affectedAgent.IsHuman)
                    {
                        contribution = 10f;

                        if (affectedAgent.Character != null)
                        {
                            if (affectedAgent.HasAttribute("MurderousProwess"))
                            {
                                contribution = 20f;
                            }

                            if (affectedAgent.IsHero)
                            {
                                contribution *= 5f;
                            }
                            else
                            {
                                contribution += affectedAgent.Character.Level * 0.2f;
                            }
                        }
                    }
                    else
                    {
                        contribution = 2f;
                    }

                    _prowessPoints += contribution;

                    if (!_halfwayTriggered && _prowessPoints >= _threshold * 0.5f)
                    {
                        _halfwayTriggered = true;
                        InformationManager.DisplayMessage(new InformationMessage("The field is soaked in blood. The Dark Elves grow restless...", Color.FromUint(0xFF800080)));
                    }

                    if (_prowessPoints >= _threshold)
                    {
                        TriggerMurderousProwess();
                    }
                }
                catch (Exception ex)
                {
                    XeraLogger.Error($"Error processing agent death: {ex.Message}");
                }
            }
        }

        private void InitializeThreshold()
        {
            // Count humans safely to set a more stable threshold
            int humanCount = 0;
            if (Mission?.Agents != null)
            {
                for (int i = 0; i < Mission.Agents.Count; i++)
                {
                    if (Mission.Agents[i]?.IsHuman == true) humanCount++;
                }
            }
            // Multiplier of 4.0 * humans is balanced for ~25% total deaths needed
            _threshold = Math.Max(200f, humanCount * 4.0f); 
        }

        private void TriggerMurderousProwess()
        {
            _hasTriggered = true;

            if (Mission == null || Mission.Agents == null)
            {
                XeraLogger.Warn("Cannot trigger Murderous Prowess - Mission or Agents is null");
                return;
            }

            var druchiiAgents = new List<Agent>();

            // Safely iterate with bounds checking
            int agentCount = Mission.Agents.Count;
            for (int i = 0; i < agentCount; i++)
            {
                if (i >= Mission.Agents.Count) break; // Safety check in case count changes

                var agent = Mission.Agents[i];
                if (agent == null || !agent.IsActive() || agent.Character == null)
                    continue;

                try
                {
                    if (agent.HasAttribute("MurderousProwess"))
                    {
                        druchiiAgents.Add(agent);
                    }
                }
                catch (Exception ex)
                {
                    XeraLogger.Warn($"Error checking agent for Murderous Prowess: {ex.Message}");
                }
            }

            if (druchiiAgents.Count == 0)
            {
                XeraLogger.Debug("No Druchii agents found to buff");
                return;
            }

            foreach (var agent in druchiiAgents)
            {
                try
                {
                    var affector = (Mission != null && Mission.MainAgent != null) ? Mission.MainAgent : agent;
                    TORMissionHelper.ApplyStatusEffectToAgents(new List<Agent> { agent }, "de_murderous_prowess_melee", affector, 90f);
                    TORMissionHelper.ApplyStatusEffectToAgents(new List<Agent> { agent }, "de_murderous_prowess_charge", affector, 90f);
                    TORMissionHelper.ApplyStatusEffectToAgents(new List<Agent> { agent }, "de_murderous_prowess_ap_missile", affector, 90f);

                    var moraleComp = agent.GetComponent<CommonAIComponent>();
                    if (moraleComp != null)
                    {
                        moraleComp.Morale = Math.Min(100f, moraleComp.Morale + 15f);
                    }
                }
                catch (Exception ex)
                {
                    XeraLogger.Error($"Error applying buffs to agent: {ex.Message}");
                }
            }

            InformationManager.DisplayMessage(new InformationMessage($"MURDEROUS PROWESS ACTIVATED! ({druchiiAgents.Count} units buffed)", Color.FromUint(0xFF800080)));

            try
            {
                int soundIndex = SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack");
                if (soundIndex != -1 && Mission != null && Mission.Scene != null)
                {
                    _hornSound?.Release();
                    _hornSound = SoundEvent.CreateEvent(soundIndex, Mission.Scene);
                    _hornSound?.PlayInPosition(Vec3.Zero);
                }
            }
            catch (Exception ex)
            {
                XeraLogger.Debug($"Error playing horn sound: {ex.Message}");
            }
        }
    }
}
