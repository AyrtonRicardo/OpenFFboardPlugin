using Hid.Net;
using OpenFFBoardPlugin.DTO;
using SimHub.Plugins;
using SimHub.Plugins.Styles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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

        private void UpdateConnectedTo(string connectedTo)
        {
            if (Plugin.Settings.SelectedHidDeviceId != connectedTo)
            {
                Plugin.Settings.SelectedHidDeviceId = connectedTo;
            }
            ViewConnectedTo.Text = connectedTo;
        }

        private void ViewBoards_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            List<BoardText> boards = new List<BoardText>();
            for (int i = 0; i < Plugin.BoardsHid?.Length; i++)
            {
                IHidDevice device = Plugin.BoardsHid[i];
                bool isSelected = Plugin.Settings.SelectedHidDeviceId != null && Plugin.Settings.SelectedHidDeviceId.Equals(device.DeviceId);
                boards.Add(new BoardText() { Name = device.DeviceId, DeviceIndex = i, IsEnabled = isSelected });
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

            UpdateConnectedTo(SelectedBoard()?.Name);
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
                    var res = await SHMessageBox.Show("Please select a COM", "No COM selected", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
                    await SHMessageBox.Show(res.ToString());
                    return;
                }

                Plugin.ConnectToBoard(SelectedBoard().DeviceIndex);
                UpdateConnectedTo(SelectedBoard().Name);
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

        private async void SetProfileToCurrentGame_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Plugin == null || Plugin.PluginManager == null || Plugin.PluginManager.GameManager == null)
            {
                await SHMessageBox.Show("No data identified, cannot set profile.");
                return;
            }

            if (Plugin.PluginManager.GameManager.GameName() == "")
            {
                await SHMessageBox.Show("No game detected, cannot set profile");
                return;
            }

            if (!Plugin.IsConnected())
            {
                await SHMessageBox.Show("No board connected, cannot set profile");
                return;
            }

            var gameName = Plugin.PluginManager.GameManager.GameName();

            ViewCurrentActiveProfile.Text = gameName;
            Plugin.UpdateProfileDataIfConnected();
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