using Hid.Net;
using OpenFFBoardPlugin.DTO;
using SimHub.Plugins;
using SimHub.Plugins.Styles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace OpenFFBoardPlugin
{
    /// <summary>
    /// SettingsControl.xaml interaction class.
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public DataPlugin Plugin { get; }

        internal ProfileHolder ProfileData { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(DataPlugin plugin) : this()
        {
            this.Plugin = plugin;

            ViewAutoConnectOnStartup.IsChecked = Plugin.Settings.AutoConnectOnStartup;
            ViewLastError.Text = "";
            ViewProfileJsonPath.Text = Plugin.Settings.ProfileJsonPath;
            ViewPluginConfigJsonPath.Text = Plugin.GetCommonStoragePath();

            if (!string.IsNullOrEmpty(Plugin.Settings.ProfileJsonPath))
            {
                ProfileData = ProfileHolder.LoadFromJson(Plugin.Settings.ProfileJsonPath + "\\profiles.json");
            }

            SetViewProfileData();
        }

        public string ConnectionString()
        {
            if (Plugin == null) return null;

            if (Plugin.OpenFFBoard.IsConnected) return "CONNECTED";

            return "NOT CONNECTED";
        }

        private void UpdateConnectedTo(BoardText board)
        {
            var deviceId = board?.DeviceId;
            if (Plugin.Settings.SelectedHidDeviceId != deviceId)
            {
                Plugin.Settings.SelectedHidDeviceId = deviceId;
            }
            ViewConnectedTo.Text = board?.Name ?? "";
        }

        private static string FormatDeviceName(IHidDevice device, int index)
        {
            var def = device.ConnectedDeviceDefinition;
            if (def != null && !string.IsNullOrWhiteSpace(def.ProductName))
            {
                var name = def.ProductName.Trim();
                return name;
            }

            if (def != null && def.VendorId.HasValue && def.ProductId.HasValue)
                return $"OpenFFBoard VID:0x{def.VendorId.Value:X4} PID:0x{def.ProductId.Value:X4}";

            var match = Regex.Match(device.DeviceId ?? "", @"VID_([0-9A-Fa-f]{4}).*?PID_([0-9A-Fa-f]{4})");
            if (match.Success)
                return $"OpenFFBoard VID:0x{match.Groups[1].Value.ToUpper()} PID:0x{match.Groups[2].Value.ToUpper()}";

            return $"OpenFFBoard #{index + 1}";
        }

        private async void ViewBoards_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            await Plugin.RefreshBoardsAsync();

            List<BoardText> boards = new List<BoardText>();
            for (int i = 0; i < Plugin.BoardsHid?.Length; i++)
            {
                IHidDevice device = Plugin.BoardsHid[i];
                bool isSelected = Plugin.Settings.SelectedHidDeviceId != null && Plugin.Settings.SelectedHidDeviceId.Equals(device.DeviceId);
                boards.Add(new BoardText() { Name = FormatDeviceName(device, i), DeviceId = device.DeviceId, DeviceIndex = i, IsEnabled = isSelected });
            }

            viewBoards.ItemsSource = boards;

            foreach (object o in viewBoards.Items)
            {
                if ((o is BoardText) && ((o as BoardText).IsEnabled))
                {
                    viewBoards.SelectedItem = o;
                    break;
                }
            }

            UpdateConnectedTo(SelectedBoard());
        }

        private void ViewBoards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (BoardText bt in e.RemovedItems)
            {
                bt.IsEnabled = false;
            }

            if (viewBoards.SelectedItem != null)
            {
                (viewBoards.SelectedItem as BoardText).IsEnabled = true;
            }
        }

        private BoardText SelectedBoard()
        {
            if (viewBoards.SelectedItem != null && viewBoards.SelectedItem is BoardText)
            {
                return viewBoards.SelectedItem as BoardText;
            }
            return null;
        }

        private async void ViewSelectedCom_Connect(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            try
            {
                var selected = SelectedBoard();
                if (selected == null)
                {
                    var res = await SHMessageBox.Show("Please select a HID", "No HID selected", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
                    await SHMessageBox.Show(res.ToString());
                    return;
                }

                await Plugin.ConnectToBoardAsync(SelectedBoard().DeviceIndex);
                UpdateConnectedTo(SelectedBoard());

                var profileName = Plugin.FindProfileForCurrentGame();
                if (!string.IsNullOrEmpty(profileName))
                {
                    await Plugin.ApplyProfileAsync(profileName);
                    ViewCurrentActiveProfile.Text = Plugin.ActiveProfile ?? "";
                }
            }
            catch (Exception ex)
            {
                await SHMessageBox.Show(ex.ToString());
            }
        }

        private async void ViewSelectedCom_Disconnect(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            try
            {
                if (Plugin.OpenFFBoard == null)
                {
                    await SHMessageBox.Show("OpenFFBoard is not connected");
                    return;
                }

                Plugin.Disconnect();
                UpdateConnectedTo(null);
            }
            catch (Exception ex)
            {
                await SHMessageBox.Show(ex.ToString());
            }
        }

        private void SaveConfiguration_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            try 
            {
                Plugin.SaveConfig();
            }
            catch (Exception ex)
            {
                ErrorHasHappened($"failed to save configuration: {ex.Message}");
            }
            
        }

        private async void CreateProfileForCurrentGame_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null || Plugin.PluginManager == null || Plugin.PluginManager.GameManager == null)
            {
                await SHMessageBox.Show("No data identified, cannot create profile.");
                return;
            }

            if (string.IsNullOrEmpty(Plugin.PluginManager.GameManager.GameName()))
            {
                await SHMessageBox.Show("No game detected, cannot create profile");
                return;
            }

            var created = Plugin.CreateProfileForCurrentGame();
            if (created != null)
                ViewCurrentActiveProfile.Text = created;
        }

        private async void ApplyCurrentProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
                return;

            if (!Plugin.IsConnected())
            {
                await SHMessageBox.Show("No board connected, cannot apply profile");
                return;
            }

            var profileName = Plugin.ActiveProfile;
            if (string.IsNullOrEmpty(profileName))
            {
                await SHMessageBox.Show("No profile selected. Create a profile first.");
                return;
            }

            await Plugin.ApplyProfileAsync(profileName);
            ViewCurrentActiveProfile.Text = Plugin.ActiveProfile;
        }

        private void ErrorHasHappened(string error)
        {
            ViewLastError.Text = error;
        }

        private void ViewAutoConnectOnStartup_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            Plugin.Settings.AutoConnectOnStartup = (bool)ViewAutoConnectOnStartup.IsChecked;
        }

        private void ViewAutoConnectOnStartup_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            Plugin.Settings.AutoConnectOnStartup = (bool)ViewAutoConnectOnStartup.IsChecked;
        }
        
        private void SelectProfileJsonPath_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Plugin.Settings.ProfileJsonPath = dialog.SelectedPath;
                ViewProfileJsonPath.Text = Plugin.Settings.ProfileJsonPath;
            }
        }

        private void SetViewProfileData()
        {
            if (ProfileData == null)
            {
                return;
            }

            ViewProfileData.Text = $"{ProfileData?.Profiles?.Count} profiles found";
            ViewCurrentActiveProfile.Text = Plugin.ActiveProfile;
        }
    }

    public class BoardText
    {
        public string Name { get; set; }
        public string DeviceId { get; set; }
        public int DeviceIndex { get; set; }
        private bool Enabled { get; set; }

        public bool IsEnabled
        {
            get { return Enabled; }
            set
            {
                Enabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}