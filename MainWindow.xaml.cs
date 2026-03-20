using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace MidiServiceRestarter
{
    /// <summary>
    /// Model to hold MIDI device information.
    /// </summary>
    public class MidiDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>Direction (IN / OUT / IN+OUT)</summary>
        public string Direction { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private readonly string targetServiceName = "MidiSrv";

        private DispatcherTimer _serviceTimer;
        private DispatcherTimer _deviceTimer;
        private bool _isRestarting = false;
        private bool _isRefreshing = false;

        private ObservableCollection<MidiDeviceInfo> _devices = new();

        public MainWindow()
        {
            InitializeComponent();

            DeviceListView.ItemsSource = _devices;

            // Check service status every second
            _serviceTimer = new DispatcherTimer();
            _serviceTimer.Interval = TimeSpan.FromSeconds(1);
            _serviceTimer.Tick += (s, e) => CheckServiceStatus();
            _serviceTimer.Start();

            // Refresh MIDI device list every 5 seconds
            _deviceTimer = new DispatcherTimer();
            _deviceTimer.Interval = TimeSpan.FromSeconds(5);
            _deviceTimer.Tick += async (s, e) => await RefreshMidiDevicesAsync();
            _deviceTimer.Start();

            // Initial check
            CheckServiceStatus();
            _ = RefreshMidiDevicesAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            _serviceTimer?.Stop();
            _deviceTimer?.Stop();
            base.OnClosed(e);
        }

        // --- Service Status Check ---

        private void CheckServiceStatus()
        {
            try
            {
                using (var sc = new ServiceController(targetServiceName))
                {
                    if (!_isRestarting)
                    {
                        StatusText.Text = $"Service Status: {sc.Status}";
                        RestartButton.IsEnabled = sc.Status == ServiceControllerStatus.Running
                                                || sc.Status == ServiceControllerStatus.Stopped;
                    }
                    else
                    {
                        StatusText.Text = $"Restarting... ({sc.Status})";
                    }
                }
            }
            catch
            {
                if (!_isRestarting)
                {
                    StatusText.Text = $"Service not found or access denied.\n({targetServiceName})";
                    RestartButton.IsEnabled = false;
                }
            }
        }

        // --- MIDI Device Enumeration ---

        /// <summary>
        /// Enumerates MIDI 1.0 / 2.0 devices and updates the ListView.
        /// </summary>
        private async Task RefreshMidiDevicesAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            try
            {
                // Dictionary to merge IN and OUT devices into a single entry
                var dict = new Dictionary<string, MidiDeviceInfo>(StringComparer.OrdinalIgnoreCase);

                // -- Get MIDI IN devices --
                string inSelector = MidiInPort.GetDeviceSelector();
                var inDevices = await DeviceInformation.FindAllAsync(inSelector);

                foreach (var d in inDevices)
                {
                    if (!dict.TryGetValue(d.Name, out var info))
                    {
                        info = new MidiDeviceInfo { Name = d.Name, Direction = "IN" };
                        dict[d.Name] = info;
                    }
                    else
                    {
                        info.Direction = "IN+OUT";
                    }
                }

                // -- Get MIDI OUT devices --
                string outSelector = MidiOutPort.GetDeviceSelector();
                var outDevices = await DeviceInformation.FindAllAsync(outSelector);

                foreach (var d in outDevices)
                {
                    if (!dict.TryGetValue(d.Name, out var info))
                    {
                        info = new MidiDeviceInfo { Name = d.Name, Direction = "OUT" };
                        dict[d.Name] = info;
                    }
                    else
                    {
                        if (info.Direction == "IN") info.Direction = "IN+OUT";
                    }
                }

                // -- Update UI --
                string now = DateTime.Now.ToString("HH:mm:ss");

                _devices.Clear();
                foreach (var kv in dict.Values)
                    _devices.Add(kv);

                LastUpdatedText.Text = $"Last Updated: {now}";
                DeviceCountText.Text = _devices.Count == 0
                    ? "No devices found"
                    : $"{_devices.Count} devices detected";
            }
            catch (Exception ex)
            {
                LastUpdatedText.Text = "Update Error";
                DeviceCountText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        // --- Event Handlers ---

        private async void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRestarting) return;

            _isRestarting = true;
            RestartButton.IsEnabled = false;
            StatusText.Text = "Restarting...";

            await Task.Run(() =>
            {
                try
                {
                    using (var sc = new ServiceController(targetServiceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Running
                         || sc.Status == ServiceControllerStatus.StartPending)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        }

                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"An error occurred:\n{ex.Message}", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });

            _isRestarting = false;
            CheckServiceStatus();

            // Refresh devices after service restart
            await RefreshMidiDevicesAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            await RefreshMidiDevicesAsync();
            RefreshButton.IsEnabled = true;
        }
        private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _deviceTimer?.Start();
        }

        private void AutoUpdateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _deviceTimer?.Stop();
        }
    }
}
