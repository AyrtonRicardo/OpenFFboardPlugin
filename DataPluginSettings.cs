namespace OpenFFBoardPlugin
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DataPluginSettings
    {
        public bool AutoConnectOnStartup = false;
        public bool AutoApplyProfileOnGameChange = false;
        public string ProfileJsonPath = null;
        public string SelectedHidDeviceId = null;

        /// <summary>
        /// When true, the dashboard bundled in the plugin DLL is extracted into SimHub's
        /// DashTemplates folder on startup whenever its version differs from the installed one.
        /// </summary>
        public bool AutoUpdateDashboards = true;

        /// <summary>
        /// Dashboard extras: published as SimHub properties (InputDisplay.*) and consumed
        /// by the "OpenFFBoard Companion - Input Display" dashboard.
        /// </summary>
        public int SteeringRotationDegrees = 480;   // full lock-to-lock rotation used by the steering indicator
        public int BackgroundOpacity = 70;          // overlay background opacity, 0-100
        public int ShiftLightThresholdPercent = 85; // % of max RPM where the shift LED strip starts lighting
        public bool ShowTraces = true;
        public bool ShowPedals = true;
        public bool ShowGearAndSpeed = true;
        public bool ShowExtras = true;              // ABS / TC / BB block
        public bool ShowSteering = true;
    }
}