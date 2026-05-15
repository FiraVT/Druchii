using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace XeraDruchii.Utilities
{
    /// <summary>
    /// Logging utility for XeraDruchii mod using NLog and ButterLib
    /// </summary>
    public static class XeraLogger
    {
        public static bool DebugMode = true;
        private static NLog.Logger _logger;
        private static Microsoft.Extensions.Logging.ILogger _butterLogger;
        private static readonly Dictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();

        public static void SetButterLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _butterLogger = logger;
        }

        public static void Configure()
        {
            try
            {
                string modulePath = null;
                try 
                {
                    modulePath = ModuleHelper.GetModuleFullPath("XeraDruchii");
                }
                catch (Exception)
                {
                    TaleWorlds.Library.Debug.Print("XeraDruchii: Could not get module path via ModuleHelper.");
                }

                if (string.IsNullOrEmpty(modulePath))
                {
                    // Fallback to relative path if ModuleHelper fails
                    modulePath = Path.Combine("..", "..", "Modules", "XeraDruchii");
                }

                var logDir = Path.Combine(modulePath, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logPath = Path.Combine(logDir, "xeradruchii.log");
                var config = LogManager.Configuration ?? new LoggingConfiguration();

                var fileTarget = new FileTarget("XeraDruchiiTarget")
                {
                    FileName = logPath,
                    Layout = "${longdate} [${level:uppercase=true}] ${message} ${exception:format=tostring}",
                    KeepFileOpen = false,
                    ArchiveOldFileOnStartup = true,
                    MaxArchiveFiles = 3
                };

                config.AddTarget(fileTarget);
                // Filter to only our namespace to avoid drowning in TOR_Core/Native logs
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget, "XeraDruchii*");
                
                LogManager.Configuration = config;
                _logger = LogManager.GetLogger("XeraDruchii");
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("XeraDruchii: Failed to configure logging: " + ex.Message);
            }
        }

        public static void Info(string message, [CallerMemberName] string caller = "")
        {
            string formatted = $"[{caller}] {message}";
            if (!formatted.StartsWith("XeraDruchii")) formatted = "XeraDruchii: " + formatted;
            _butterLogger?.LogInformation(formatted);
            _logger?.Info(formatted);
        }

        public static void Error(string message, Exception ex = null, [CallerMemberName] string caller = "")
        {
            string formatted = $"[{caller}] {message}";
            if (!formatted.StartsWith("XeraDruchii")) formatted = "XeraDruchii: " + formatted;
            
            if (ex != null)
            {
                _butterLogger?.LogError(ex, formatted);
                _logger?.Error(ex, formatted);
            }
            else
            {
                _butterLogger?.LogError(formatted);
                _logger?.Error(formatted);
            }

            LogCampaignSnapshot();
            
            // Mirror to in-game message for visibility if possible
            try
            {
                InformationManager.DisplayMessage(new InformationMessage(formatted, Color.FromUint(0xFFFF0000)));
            }
            catch { /* Ignore if UI not ready */ }
        }

        public static void Warn(string message, [CallerMemberName] string caller = "")
        {
            string formatted = $"[{caller}] {message}";
            if (!formatted.StartsWith("XeraDruchii")) formatted = "XeraDruchii: " + formatted;
            _butterLogger?.LogWarning(formatted);
            _logger?.Warn(formatted);

            // Mirror to in-game message for visibility
            try
            {
                InformationManager.DisplayMessage(new InformationMessage(formatted, Color.FromUint(0xFFFFFF00)));
            }
            catch { /* Ignore if UI not ready */ }
        }

        public static void Debug(string message, [CallerMemberName] string caller = "")
        {
            if (!DebugMode) return;
            string formatted = $"[{caller}] {message}";
            if (!formatted.StartsWith("XeraDruchii")) formatted = "XeraDruchii: " + formatted;
            _butterLogger?.LogDebug(formatted);
            _logger?.Debug(formatted);
        }

        public static void Trace(string message, [CallerMemberName] string caller = "")
        {
            if (!DebugMode) return;
            string formatted = $"[{caller}] {message}";
            if (!formatted.StartsWith("XeraDruchii")) formatted = "XeraDruchii: " + formatted;
            _butterLogger?.LogTrace(formatted);
            _logger?.Trace(formatted);
        }

        public static void StartTimer(string name)
        {
            _timers[name] = Stopwatch.StartNew();
        }

        public static void StopAndLogTimer(string name, [CallerMemberName] string caller = "")
        {
            if (_timers.TryGetValue(name, out var sw))
            {
                sw.Stop();
                Info($"Timer '{name}' finished in {sw.ElapsedMilliseconds}ms", caller);
                _timers.Remove(name);
            }
        }

        public static TaleWorlds.CampaignSystem.Hero GetSafeMainHero()
        {
            try
            {
                var player = GetSafePlayerCharacter();
                return player?.HeroObject;
            }
            catch { }
            return null;
        }

        public static TaleWorlds.CampaignSystem.CharacterObject GetSafePlayerCharacter()
        {
            try
            {
                if (TaleWorlds.Core.Game.Current == null) return null;
                return TaleWorlds.Core.Game.Current.PlayerTroop as TaleWorlds.CampaignSystem.CharacterObject;
            }
            catch { }
            return null;
        }

        private static void LogCampaignSnapshot()
        {
            try
            {
                if (TaleWorlds.CampaignSystem.Campaign.Current != null)
                {
                    var mainHero = GetSafeMainHero();
                    string heroInfo = mainHero != null ? $"{mainHero.Name} (Culture: {mainHero.Culture?.StringId ?? "null"})" : "null";
                    
                    string snapshot = $"XeraDruchii: [Snapshot] Hero: {heroInfo}";
                    
                    if (TaleWorlds.MountAndBlade.Mission.Current != null)
                    {
                        snapshot += $" | Mission: {TaleWorlds.MountAndBlade.Mission.Current.Mode}";
                    }
                    
                    _butterLogger?.LogInformation(snapshot);
                    _logger?.Info(snapshot);
                }
            }
            catch { /* Ignore snapshot errors */ }
        }
    }
}
