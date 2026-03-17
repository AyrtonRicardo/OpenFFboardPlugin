namespace OpenFFBoardPlugin
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DataPluginSettings
    {
        public bool AutoConnectOnStartup = false;
        public string ProfileJsonPath = null;
        public string SelectedHidDeviceId = null;
    }
}