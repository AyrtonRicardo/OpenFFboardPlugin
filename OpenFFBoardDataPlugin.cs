using GameReaderCommon;
using Hid.Net;
using OpenFFBoardPlugin.DTO;
using OpenFFBoardPlugin.Utils;
using SimHub.Plugins;
using System;
using System.Threading.Tasks;
using System.Windows.Media;
using WoteverCommon.Extensions;

namespace OpenFFBoardPlugin
{
    [PluginDescription("OpenFFBoard plugin to communicate with openffboard firmware")]
    [PluginAuthor("Ayrton Ricardo")]
    [PluginName("OpenFFBoard companion plugin")]
    public class OpenFFBoardDataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private enum AcceptableMainClassEnum
        {
            FFBMain = 1
        }

        public OpenFFBoardDataPluginSettings Settings;
        public OpenFFBoard.Board OpenFFBoard;
        public IHidDevice[] BoardsHid = null;
        public string ActiveProfile = null;
        public string settingsName = "GeneralSettings";

        private string _lastAutoAppliedGame = null;
        private bool _isApplyingProfile = false;

        // ── Corner minimum speed tracking ──────────────────────────────────────
        // Phase machine: 0 = accelerating/steady (tracking peak), 1 = decelerating (tracking min).
        // Published as InputDisplay.CornerMinSpeedKmh: falls live with speed while decelerating
        // through a corner, freezes at the corner minimum for CornerMinDisplaySeconds once the car
        // accelerates away, then clears so the dashboard falls back to showing live speed.
        // InputDisplay.LastCornerMinSpeedKmh keeps the permanent (non-expiring) record.
        private const double CornerEnterDropKmh = 5.0;   // drop below peak that starts "decelerating" phase
        private const double CornerExitRiseKmh = 5.0;    // rise above min that ends the corner
        private const double CornerMinValidDropKmh = 10.0; // total drop required for a real corner (filters small lifts)
        private const double CornerMinDisplaySeconds = 4.0; // how long the frozen corner min stays on screen after exit

        private int _cornerPhase = 0;
        private double _cornerPeakSpeed = 0;
        private double _cornerCurrentMin = 0;
        private double _lastCornerMinSpeed = -1;
        private double _cornerDisplayMin = -1;
        private DateTime _cornerMinFrozenAt = DateTime.MinValue;

        internal void UpdateCornerMinSpeed(double speedKmh, bool gameRunning)
        {
            if (!gameRunning)
            {
                _cornerPhase = 0;
                _cornerPeakSpeed = 0;
                _cornerDisplayMin = -1; // don't leak a stale value into the next session
                return;
            }

            if (_cornerPhase == 0)
            {
                if (speedKmh > _cornerPeakSpeed)
                {
                    _cornerPeakSpeed = speedKmh;
                }

                if (_cornerPeakSpeed - speedKmh >= CornerEnterDropKmh)
                {
                    _cornerPhase = 1;
                    _cornerCurrentMin = speedKmh;
                }
                else if (_cornerDisplayMin >= 0 && (DateTime.UtcNow - _cornerMinFrozenAt).TotalSeconds >= CornerMinDisplaySeconds)
                {
                    _cornerDisplayMin = -1; // expired: dashboard falls back to live speed
                }
            }
            else
            {
                if (speedKmh < _cornerCurrentMin)
                {
                    _cornerCurrentMin = speedKmh;
                }

                // live value only once the drop is big enough to be a corner (avoids flicker on small lifts)
                if (_cornerPeakSpeed - _cornerCurrentMin >= CornerMinValidDropKmh)
                {
                    _cornerDisplayMin = _cornerCurrentMin;
                }

                if (speedKmh - _cornerCurrentMin >= CornerExitRiseKmh)
                {
                    if (_cornerPeakSpeed - _cornerCurrentMin >= CornerMinValidDropKmh)
                    {
                        _lastCornerMinSpeed = _cornerCurrentMin;
                        _cornerDisplayMin = _lastCornerMinSpeed;
                        _cornerMinFrozenAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _cornerDisplayMin = -1; // lift was too small to count as a corner
                    }

                    _cornerPhase = 0;
                    _cornerPeakSpeed = speedKmh;
                }
            }
        }

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "OpenFFBoard companion";

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            if (data?.NewData != null)
                UpdateCornerMinSpeed(data.NewData.SpeedKmh, data.GameRunning);

            if (!Settings.AutoApplyProfileOnGameChange || !IsConnected() || _isApplyingProfile)
                return;

            var currentGame = pluginManager?.GameManager?.GameName();
            if (string.IsNullOrEmpty(currentGame) || currentGame == _lastAutoAppliedGame)
                return;

            _lastAutoAppliedGame = currentGame;

            var profileName = FindProfileForCurrentGame();
            if (!string.IsNullOrEmpty(profileName))
            {
                _isApplyingProfile = true;
                _ = ApplyProfileAsync(profileName).ContinueWith(_ => _isApplyingProfile = false);
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            this.SaveConfig();

            this.Disconnect();
        }

        public void SaveConfig()
        {
            this.SaveCommonSettings(settingsName, Settings, 5);
        }

        internal string GetCommonStoragePath()
        {
            return PluginManager.GetCommonStoragePath(GetType().Name + "." + settingsName + ".json");
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        public async Task RefreshBoardsAsync()
        {
            var devices = await global::OpenFFBoard.Hid.GetBoardsAsync().ConfigureAwait(false);

            if (devices != null)
            {
                foreach (var device in devices)
                {
                    try
                    {
                        if (!device.IsInitialized)
                        {
                            await device.InitializeAsync().ConfigureAwait(false);
                            device.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        SimHub.Logging.Current.Warn($"Could not read descriptor for {device.DeviceId}: {ex.Message}");
                    }
                }
            }

            BoardsHid = devices;
        }

        public async Task ConnectToBoardAsync(int hidDeviceIndex = 0)
        {
            await Task.Run(() =>
            {
                OpenFFBoard = new OpenFFBoard.Hid(BoardsHid[hidDeviceIndex]);
                OpenFFBoard.Connect();

                byte mainClass = OpenFFBoard.System.GetMain();

                if (!Enum.IsDefined(typeof(AcceptableMainClassEnum), (int)mainClass))
                {
                    SimHub.Logging.Current.Error($"Incompatible board main class {mainClass}");
                    throw new Exception($"Incompatible board main class {mainClass}");
                }
            });
        }

        public bool IsConnected()
        {
            return OpenFFBoard != null && OpenFFBoard.IsConnected;
        }

        public void Disconnect()
        {
            if (OpenFFBoard == null || !OpenFFBoard.IsConnected)
            {
                return;
            }

            OpenFFBoard.Disconnect();
            OpenFFBoard = null;
        }

        private ProfileHolder LoadProfileData()
        {
            if (string.IsNullOrEmpty(Settings.ProfileJsonPath))
            {
                return null;
            }

            try
            {
                return ProfileHolder.LoadFromJson(Settings.ProfileJsonPath + "\\profiles.json");
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Failed to load profile data from {Settings.ProfileJsonPath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Returns the profile name for the current game if one exists, otherwise null.
        /// Never creates anything.
        /// </summary>
        internal string FindProfileForCurrentGame()
        {
            var gameName = PluginManager?.GameManager?.GameName();
            if (string.IsNullOrEmpty(gameName))
                return null;

            var profileHolder = LoadProfileData();
            return profileHolder?.Profiles?
                .Find(p => p.Name.Equals(gameName, StringComparison.InvariantCultureIgnoreCase))
                ?.Name;
        }

        /// <summary>
        /// Creates a profile entry for the current game if one doesn't exist yet,
        /// cloning the "default" profile as the starting point. Saves profiles.json.
        /// Does NOT send any commands to the board.
        /// </summary>
        internal string CreateProfileForCurrentGame()
        {
            var gameName = PluginManager?.GameManager?.GameName();
            if (string.IsNullOrEmpty(gameName))
                return null;

            var profilePath = Settings.ProfileJsonPath + "\\profiles.json";

            ProfileHolder profileHolder = LoadProfileData();
            if (profileHolder == null)
            {
                SimHub.Logging.Current.Warn("No profile data loaded — check profile JSON path in settings");
                return null;
            }

            bool alreadyExists = profileHolder.Profiles != null &&
                profileHolder.Profiles.Exists(p => p.Name.Equals(gameName, StringComparison.InvariantCultureIgnoreCase));

            var profile = profileHolder.GetOrCreateProfileForGame(gameName);

            if (!alreadyExists)
            {
                profileHolder.SaveToJson(profilePath);
                SimHub.Logging.Current.Info($"Created new profile for '{gameName}' based on default");
            }
            else
            {
                SimHub.Logging.Current.Info($"Profile for '{gameName}' already exists, nothing created");
            }

            return profile.Name;
        }

        /// <summary>
        /// Sends the commands from the named profile to the board.
        /// </summary>
        internal async Task ApplyProfileAsync(string profileName)
        {
            if (!IsConnected())
                return;

            if (string.IsNullOrEmpty(profileName))
                return;

            ProfileHolder profileHolder = LoadProfileData();
            if (profileHolder == null)
            {
                SimHub.Logging.Current.Warn("No profile data loaded — check profile JSON path in settings");
                return;
            }

            var profile = profileHolder.Profiles?.Find(p => p.Name.Equals(profileName, StringComparison.InvariantCultureIgnoreCase));
            if (profile == null)
            {
                SimHub.Logging.Current.Warn($"Profile '{profileName}' not found");
                return;
            }

            ActiveProfile = profile.Name;

            var commands = ProfileToCommandConverter.ConvertProfileToCommands(profile, OpenFFBoard);
            await Task.Run(() =>
            {
                commands.ForEach(cmd =>
                {
                    if (!cmd())
                        SimHub.Logging.Current.Error("Failed to execute command");
                });
            });
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings<OpenFFBoardDataPluginSettings>(settingsName, () => new OpenFFBoardDataPluginSettings());

            // Dashboard extras: published as properties so the
            // "OpenFFBoard Companion - Input Display" dashboard (and any other dash)
            // can bind to them instead of relying on manually edited dashboard variables.
            this.AttachDelegate(name: "InputDisplay.SteeringRotationDegrees", valueProvider: () => Settings.SteeringRotationDegrees);
            this.AttachDelegate(name: "InputDisplay.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
            this.AttachDelegate(name: "InputDisplay.ShiftLightThresholdPercent", valueProvider: () => Settings.ShiftLightThresholdPercent);
            this.AttachDelegate(name: "InputDisplay.ShowTraces", valueProvider: () => Settings.ShowTraces);
            this.AttachDelegate(name: "InputDisplay.ShowPedals", valueProvider: () => Settings.ShowPedals);
            this.AttachDelegate(name: "InputDisplay.ShowGearAndSpeed", valueProvider: () => Settings.ShowGearAndSpeed);
            this.AttachDelegate(name: "InputDisplay.ShowExtras", valueProvider: () => Settings.ShowExtras);
            this.AttachDelegate(name: "InputDisplay.ShowSteering", valueProvider: () => Settings.ShowSteering);
            this.AttachDelegate(name: "InputDisplay.ShowFFBClipping", valueProvider: () => Settings.ShowFFBClipping);
            this.AttachDelegate(name: "InputDisplay.WheelImage", valueProvider: () => Settings.WheelImage);
            this.AttachDelegate(name: "InputDisplay.CornerMinSpeedKmh", valueProvider: () => _cornerDisplayMin);
            this.AttachDelegate(name: "InputDisplay.LastCornerMinSpeedKmh", valueProvider: () => _lastCornerMinSpeed);

            // Race info extras: published as properties so the
            // "OpenFFBoard Companion - Race Info" dashboard can bind to them.
            this.AttachDelegate(name: "RaceInfo.BackgroundOpacity", valueProvider: () => Settings.RaceInfoBackgroundOpacity);
            this.AttachDelegate(name: "RaceInfo.ShowPosition", valueProvider: () => Settings.RaceInfoShowPosition);
            this.AttachDelegate(name: "RaceInfo.ShowSession", valueProvider: () => Settings.RaceInfoShowSession);
            this.AttachDelegate(name: "RaceInfo.ShowLapTiming", valueProvider: () => Settings.RaceInfoShowLapTiming);
            this.AttachDelegate(name: "RaceInfo.ShowFuel", valueProvider: () => Settings.RaceInfoShowFuel);
            this.AttachDelegate(name: "RaceInfo.ShowTemps", valueProvider: () => Settings.RaceInfoShowTemps);
            this.AttachDelegate(name: "RaceInfo.ShowTyres", valueProvider: () => Settings.RaceInfoShowTyres);
        }
    }
}