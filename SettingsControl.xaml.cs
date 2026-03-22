using Hid.Net;
using OpenFFBoardPlugin.DTO;
using SimHub.Plugins;
using SimHub.Plugins.Styles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenFFBoardPlugin
{
    /// <summary>
    /// SettingsControl.xaml interaction class.
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public DataPlugin Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(DataPlugin plugin) : this()
        {
            Plugin = plugin;

            ViewAutoConnectOnStartup.IsChecked = Plugin.Settings.AutoConnectOnStartup;
            ViewPluginConfigJsonPath.Text = Plugin.GetCommonStoragePath();

            var profilePath = Plugin.Settings.ProfileJsonPath;
            ViewProfileJsonPath.Text = !string.IsNullOrEmpty(profilePath) ? profilePath + "\\profiles.json" : "Not configured";

            RefreshProfileCount();
            RefreshCurrentGame();
            RefreshActiveProfile();
            RefreshConnectionStatus(null);
        }

        // ── Connection state ───────────────────────────────────────────────────

        private void UpdateConnectedTo(BoardText board)
        {
            var deviceId = board?.DeviceId;
            if (Plugin.Settings.SelectedHidDeviceId != deviceId)
                Plugin.Settings.SelectedHidDeviceId = deviceId;

            RefreshConnectionStatus(board);
        }

        private void RefreshConnectionStatus(BoardText board)
        {
            if (board != null)
            {
                ViewConnectedTo.Text = board.Name;
                ViewConnectedTo.FontStyle = FontStyles.Normal;
                ViewConnectedTo.Opacity = 1.0;
                ViewConnectionDot.Fill = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // green
            }
            else
            {
                ViewConnectedTo.Text = "Not connected";
                ViewConnectedTo.FontStyle = FontStyles.Italic;
                ViewConnectedTo.Opacity = 0.6;
                ViewConnectionDot.Fill = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)); // gray
            }
        }

        private void RefreshCurrentGame()
        {
            var gameName = Plugin?.PluginManager?.GameManager?.GameName();
            ViewCurrentGame.Text = !string.IsNullOrEmpty(gameName) ? gameName : "—";
        }

        private void RefreshActiveProfile()
        {
            ViewCurrentActiveProfile.Text = !string.IsNullOrEmpty(Plugin?.ActiveProfile)
                ? Plugin.ActiveProfile
                : "—";
        }

        private void RefreshProfileCount()
        {
            var path = Plugin?.Settings?.ProfileJsonPath;
            if (string.IsNullOrEmpty(path))
            {
                ViewProfileCount.Text = string.Empty;
                return;
            }

            try
            {
                var holder = ProfileHolder.LoadFromJson(path + "\\profiles.json");
                ViewProfileCount.Text = $"{holder?.Profiles?.Count ?? 0} profile(s) loaded";
            }
            catch
            {
                ViewProfileCount.Text = "Could not read profiles file";
            }
        }

        private void ShowError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ViewLastError.Visibility = Visibility.Collapsed;
                ViewLastError.Text = string.Empty;
            }
            else
            {
                ViewLastError.Text = message;
                ViewLastError.Visibility = Visibility.Visible;
            }
        }

        // ── HID device list ────────────────────────────────────────────────────

        private static string FormatDeviceName(IHidDevice device, int index)
        {
            var def = device.ConnectedDeviceDefinition;

            if (def != null && !string.IsNullOrWhiteSpace(def.ProductName))
                return def.ProductName.Trim();

            if (def != null && def.VendorId.HasValue && def.ProductId.HasValue)
                return $"OpenFFBoard VID:0x{def.VendorId.Value:X4} PID:0x{def.ProductId.Value:X4}";

            var match = Regex.Match(device.DeviceId ?? "", @"VID_([0-9A-Fa-f]{4}).*?PID_([0-9A-Fa-f]{4})");
            if (match.Success)
                return $"OpenFFBoard VID:0x{match.Groups[1].Value.ToUpper()} PID:0x{match.Groups[2].Value.ToUpper()}";

            return $"OpenFFBoard #{index + 1}";
        }

        private async void ViewBoards_Loaded(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            await Plugin.RefreshBoardsAsync();

            var boards = new List<BoardText>();
            for (int i = 0; i < Plugin.BoardsHid?.Length; i++)
            {
                IHidDevice device = Plugin.BoardsHid[i];
                bool isSelected = Plugin.Settings.SelectedHidDeviceId != null
                    && Plugin.Settings.SelectedHidDeviceId.Equals(device.DeviceId);
                boards.Add(new BoardText
                {
                    Name = FormatDeviceName(device, i),
                    DeviceId = device.DeviceId,
                    DeviceIndex = i,
                    IsEnabled = isSelected
                });
            }

            viewBoards.ItemsSource = boards;

            foreach (object item in viewBoards.Items)
            {
                if (item is BoardText bt && bt.IsEnabled)
                {
                    viewBoards.SelectedItem = item;
                    break;
                }
            }

            UpdateConnectedTo(SelectedBoard());
        }

        private void ViewBoards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (BoardText bt in e.RemovedItems)
                bt.IsEnabled = false;

            if (viewBoards.SelectedItem is BoardText selected)
                selected.IsEnabled = true;
        }

        private BoardText SelectedBoard()
        {
            return viewBoards.SelectedItem as BoardText;
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private async void ViewSelectedCom_Connect(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            ShowError(null);

            try
            {
                var selected = SelectedBoard();
                if (selected == null)
                {
                    await SHMessageBox.Show("Please select a device from the list.", "No device selected",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                await Plugin.ConnectToBoardAsync(selected.DeviceIndex);
                UpdateConnectedTo(selected);

                var profileName = Plugin.FindProfileForCurrentGame();
                if (!string.IsNullOrEmpty(profileName))
                {
                    await Plugin.ApplyProfileAsync(profileName);
                    RefreshActiveProfile();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void ViewSelectedCom_Disconnect(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            ShowError(null);

            try
            {
                if (!Plugin.IsConnected())
                {
                    await SHMessageBox.Show("No board is currently connected.");
                    return;
                }

                Plugin.Disconnect();
                UpdateConnectedTo(null);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            ShowError(null);
            try
            {
                Plugin.SaveConfig();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save settings: {ex.Message}");
            }
        }

        private async void CreateProfileForCurrentGame_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            ShowError(null);
            RefreshCurrentGame();

            if (Plugin.PluginManager?.GameManager == null || string.IsNullOrEmpty(Plugin.PluginManager.GameManager.GameName()))
            {
                await SHMessageBox.Show("No game is currently detected. Launch a game first.", "No game detected",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var created = Plugin.CreateProfileForCurrentGame();
            if (created != null)
            {
                Plugin.ActiveProfile = created;
                RefreshActiveProfile();
                RefreshProfileCount();
            }
        }

        private async void ApplyCurrentProfile_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            ShowError(null);

            if (!Plugin.IsConnected())
            {
                await SHMessageBox.Show("No board is connected. Connect to a board first.", "Not connected",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var profileName = Plugin.ActiveProfile;
            if (string.IsNullOrEmpty(profileName))
            {
                await SHMessageBox.Show("No active profile. Create a profile for the current game first.", "No profile",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            await Plugin.ApplyProfileAsync(profileName);
            RefreshActiveProfile();
        }

        private void SelectProfileJsonPath_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Plugin.Settings.ProfileJsonPath = dialog.SelectedPath;
                ViewProfileJsonPath.Text = dialog.SelectedPath + "\\profiles.json";
                RefreshProfileCount();
            }
        }

        private void ViewAutoConnectOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            if (Plugin != null)
                Plugin.Settings.AutoConnectOnStartup = ViewAutoConnectOnStartup.IsChecked == true;
        }

        private void ViewAutoConnectOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Plugin != null)
                Plugin.Settings.AutoConnectOnStartup = ViewAutoConnectOnStartup.IsChecked == true;
        }
    }

    public class BoardText : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string DeviceId { get; set; }
        public int DeviceIndex { get; set; }

        private bool _enabled;
        public bool IsEnabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
