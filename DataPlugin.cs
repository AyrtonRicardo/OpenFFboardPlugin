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
    public class DataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private enum AcceptableMainClassEnum
        {
            FFBMain = 1
        }

        public DataPluginSettings Settings;
        public OpenFFBoard.Board OpenFFBoard;
        public IHidDevice[] BoardsHid = null;
        public string ActiveProfile = null;
        public string settingsName = "GeneralSettings";

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
            // Define the value of our property (declared in init)
            if (data.GameRunning)
            {
                SimHub.Logging.Current.Info("Game is running cool");
                if (data.OldData != null && data.NewData != null)
                {
                   /* if (data.OldData.SpeedKmh < Settings.SpeedWarningLevel && data.OldData.SpeedKmh >= Settings.SpeedWarningLevel)
                   {
                        //  Trigger an event
                       //this.TriggerEvent("SpeedWarning");
                   } */
                }
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
            Settings = this.ReadCommonSettings<DataPluginSettings>(settingsName, () => new DataPluginSettings());

            /*
            // Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
            //this.AttachDelegate(name: "CurrentDateTime", valueProvider: () => DateTime.Now);

            // Declare an event
            this.AddEvent(eventName: "SpeedWarning");

            // Declare an action which can be called
            this.AddAction(
                actionName: "IncrementSpeedWarning",
                actionStart: (a, b) =>
                {
                    Settings.SpeedWarningLevel++;
                    SimHub.Logging.Current.Info("Speed warning changed");
                });

            // Declare an action which can be called, actions are meant to be "triggered" and does not reflect an input status (pressed/released ...)
            this.AddAction(
                actionName: "DecrementSpeedWarning",
                actionStart: (a, b) =>
                {
                    Settings.SpeedWarningLevel--;
                });

            // Declare an input which can be mapped, inputs are meant to be keeping state of the source inputs,
            // they won't trigger on inputs not capable of "holding" their state.
            // Internally they work similarly to AddAction, but are restricted to a "during" behavior
            this.AddInputMapping(
                inputName: "InputPressed",
                inputPressed: (a, b) => {/* One of the mapped input has been pressed   * /},
                inputReleased: (a, b) => {/* One of the mapped input has been released * /}
            );
            */
        }
    }
}