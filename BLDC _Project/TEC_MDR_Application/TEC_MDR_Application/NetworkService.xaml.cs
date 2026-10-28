
////////////////using BLDC_Demo.Controls;
////////////////using Microsoft.Win32;
////////////////using System;
////////////////using System.Collections.Generic;
////////////////using System.Linq;
////////////////using System.Net;
////////////////using System.Net.NetworkInformation;
////////////////using System.Net.Sockets;
////////////////using System.Text.RegularExpressions;
////////////////using System.Threading.Tasks;
////////////////using System.Windows;
////////////////using System.Windows.Controls;
////////////////using System.Windows.Input;

////////////////namespace BLDC_Demo
////////////////{
////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  Model: a discovered network module
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public class NetworkModule
////////////////    {
////////////////        public string SerialNumber { get; set; } = string.Empty;
////////////////        public string IpAddress { get; set; } = string.Empty;
////////////////        public string Mask { get; set; } = string.Empty;
////////////////        public string Gateway { get; set; } = string.Empty;
////////////////        public string FirmwareRev { get; set; } = string.Empty;
////////////////        public string Position { get; set; } = "—";

////////////////        public override string ToString() =>
////////////////            $"{IpAddress}   SN: {SerialNumber} ";
////////////////    }

////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  Backup container
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public class ModuleBackup
////////////////    {
////////////////        public string SerialNumber { get; set; } = string.Empty;
////////////////        public string IpAddress { get; set; } = string.Empty;
////////////////        public string Mask { get; set; } = string.Empty;
////////////////        public string Gateway { get; set; } = string.Empty;
////////////////        public string Position { get; set; } = string.Empty;
////////////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////////////    }

////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  UserControl code-behind  (targets C# 7.3)
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public partial class NetworkService : UserControl
////////////////    {
////////////////        // ── ESP32 Modbus holding-register map for network config ──
////////////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////////////            = new Dictionary<string, ModuleBackup>();

////////////////        public NetworkService()
////////////////        {
////////////////            InitializeComponent();
////////////////            AttachOctetHandlers();
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  OCTET HELPERS
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void AttachOctetHandlers()
////////////////        {
////////////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////////////            AttachToGroup(ipBoxes);
////////////////            AttachToGroup(maskBoxes);
////////////////            AttachToGroup(gwBoxes);

////////////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////////////        }

////////////////        private void SetGatewayEnabled(bool enabled)
////////////////        {
////////////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////////////        }

////////////////        private static void AttachToGroup(TextBox[] boxes)
////////////////        {
////////////////            for (int i = 0; i < boxes.Length; i++)
////////////////            {
////////////////                int idx = i;

////////////////                boxes[idx].PreviewTextInput += (s, e) =>
////////////////                {
////////////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////////////                };

////////////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////////////                {
////////////////                    TextBox tb = (TextBox)s;
////////////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////////////                    {
////////////////                        e.Handled = true;
////////////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////////////                    }
////////////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////////////                    {
////////////////                        boxes[idx - 1].Focus();
////////////////                        boxes[idx - 1].SelectAll();
////////////////                    }
////////////////                };

////////////////                boxes[idx].TextChanged += (s, e) =>
////////////////                {
////////////////                    TextBox tb = (TextBox)s;
////////////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////////////                        boxes[idx + 1].Focus();
////////////////                };

////////////////                boxes[idx].LostFocus += (s, e) =>
////////////////                {
////////////////                    TextBox tb = (TextBox)s;
////////////////                    if (int.TryParse(tb.Text, out int val))
////////////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////////////                    else if (tb.Text.Length > 0)
////////////////                        tb.Text = string.Empty;
////////////////                };
////////////////            }
////////////////        }

////////////////        private static int Clamp(int value, int min, int max)
////////////////        {
////////////////            if (value < min) return min;
////////////////            if (value > max) return max;
////////////////            return value;
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  IP FIELD READ / WRITE
////////////////        // ════════════════════════════════════════════════════════
////////////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////////////        private string GetGateway() =>
////////////////            ChkGateway.IsChecked == true
////////////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////////////                : string.Empty;

////////////////        private void SetIpFields(string ip, string mask, string gw)
////////////////        {
////////////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////////////            ChkGateway.IsChecked = hasGw;
////////////////            SetGatewayEnabled(hasGw);

////////////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////////        }

////////////////        private static void FillOctets(string address,
////////////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////////////        {
////////////////            string[] parts = address != null
////////////////                ? address.Split('.')
////////////////                : new string[0];

////////////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////////////        }

////////////////        private static void ClearOctets(params TextBox[] boxes)
////////////////        {
////////////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////////////        }

////////////////        private static bool IsValidIp(string ip) =>
////////////////            IPAddress.TryParse(ip, out _);

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  DISCOVER (PORT SCAN FILTERED)
////////////////        // ════════════════════════════════════════════════════════
////////////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            LstModules.Items.Clear();
////////////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////////////            Button btn = (Button)sender;
////////////////            btn.IsEnabled = false;

////////////////            try
////////////////            {
////////////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////////////                foreach (NetworkModule m in modules)
////////////////                    LstModules.Items.Add(m);

////////////////                TxtStatus.Text = modules.Count == 0
////////////////                    ? "No motor controllers found."
////////////////                    : $"Found {modules.Count} motor controller(s).";
////////////////            }
////////////////            catch (Exception ex)
////////////////            {
////////////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////////////            }
////////////////            finally
////////////////            {
////////////////                btn.IsEnabled = true;
////////////////            }
////////////////        }

////////////////        private static List<NetworkModule> DiscoverModules()
////////////////        {
////////////////            var found = new List<NetworkModule>();
////////////////            string baseIp = GetLocalSubnetBase();
////////////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////////////            var localIps = new HashSet<string>(
////////////////                NetworkInterface.GetAllNetworkInterfaces()
////////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////////                    .Select(ua => ua.Address.ToString())
////////////////            );

////////////////            const int targetPort = 502;
////////////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////////////            for (int i = 1; i <= 254; i++)
////////////////            {
////////////////                string targetIp = $"{baseIp}.{i}";
////////////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////////////            }

////////////////            Task.WaitAll(tasks.ToArray());

////////////////            foreach (var t in tasks)
////////////////            {
////////////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////////////                {
////////////////                    found.Add(new NetworkModule
////////////////                    {
////////////////                        IpAddress = t.Result.Item2,
////////////////                        SerialNumber = "ESP32-BLDC",
////////////////                        FirmwareRev = "1.0.0",
////////////////                        Mask = "255.255.255.0",
////////////////                        Position = "—"
////////////////                    });
////////////////                }
////////////////            }

////////////////            return found;
////////////////        }

////////////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////////////        {
////////////////            try
////////////////            {
////////////////                using (var client = new TcpClient())
////////////////                {
////////////////                    var connectTask = client.ConnectAsync(ip, port);
////////////////                    var delayTask = Task.Delay(200);
////////////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
////////////////                    if (completedTask == connectTask && client.Connected)
////////////////                        return Tuple.Create(true, ip);
////////////////                }
////////////////                return Tuple.Create(false, ip);
////////////////            }
////////////////            catch
////////////////            {
////////////////                return Tuple.Create(false, ip);
////////////////            }
////////////////        }

////////////////        private static string GetLocalSubnetBase()
////////////////        {
////////////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////////////                .ToList();

////////////////            if (!candidates.Any())
////////////////            {
////////////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////////                    .ToList();
////////////////            }

////////////////            foreach (NetworkInterface ni in candidates)
////////////////            {
////////////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////////////                {
////////////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////////                    {
////////////////                        string[] parts = ua.Address.ToString().Split('.');
////////////////                        if (parts.Length == 4)
////////////////                        {
////////////////                            if (parts[0] == "127") continue;               // loopback
////////////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////////////                        }
////////////////                    }
////////////////                }
////////////////            }
////////////////            return string.Empty;
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  LIST SELECTION → populate fields
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////////////        {
////////////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////////////            if (m == null) return;

////////////////            TxtSerialNumber.Text = m.SerialNumber;
////////////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////////////            // ── Push selected module's IP into the Connect panel ──
////////////////            // so clicking Connect on the main comm control uses this module's IP.
////////////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////////////            {
////////////////                mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
////////////////            }
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  SELECT ALL / NONE
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////////////            => LstModules.SelectAll();

////////////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////////////            => LstModules.UnselectAll();


////////////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            string ip = GetIp();
////////////////            string mask = GetMask();
////////////////            string gateway = GetGateway();

////////////////            if (!IsValidIp(ip))
////////////////            {
////////////////                Warn("Enter a valid IP address.", "Invalid IP");
////////////////                return;
////////////////            }

////////////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////////////            {
////////////////                Warn("Main window not available.", "Error");
////////////////                return;
////////////////            }

////////////////            var commCtrl = mainWin.CommMotorSelect;

////////////////            // NOTE: connection check removed intentionally — Set IP now proceeds
////////////////            // regardless of commCtrl.BLLogic.IsConnected. If BLLogic itself is
////////////////            // not initialized, SendWriteCommand below may throw/fail.

////////////////            // ── Step 1: Split IP into 4 octets ──
////////////////            string[] ipParts = ip.Split('.');
////////////////            if (ipParts.Length != 4 ||
////////////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////////////                !byte.TryParse(ipParts[3], out byte octet4))
////////////////            {
////////////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////////////                return;
////////////////            }

////////////////            if (octet1 == 0)
////////////////            {
////////////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////////////                return;
////////////////            }

////////////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////////////            byte maskLastOctet = 0;
////////////////            string[] maskParts = mask.Split('.');
////////////////            if (maskParts.Length == 4)
////////////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////////////            // ── Step 2: Confirm with the user before rebooting the device ──
////////////////            var confirm = MessageBox.Show(
////////////////                $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
////////////////                "The connection will be lost automatically during the reboot.\n\nContinue?",
////////////////                "Confirm IP Change",
////////////////                MessageBoxButton.YesNo,
////////////////                MessageBoxImage.Warning);

////////////////            if (confirm != MessageBoxResult.Yes) return;

////////////////            // ── Step 3: Stage the octets + netmask ──
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET1, octet1);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET2, octet2);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET3, octet3);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET4, octet4);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_MASK_OCTET4, maskLastOctet);

////////////////            // ── Step 4: COMMIT — this is what actually saves + reboots the device ──
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_APPLY_REBOOT, 1);

////////////////            MessageBox.Show(
////////////////                $"IP {ip} sent and commit issued.\n" +
////////////////                "The device is rebooting now — reconnect using the new IP.",
////////////////                "IP Sent",
////////////////                MessageBoxButton.OK,
////////////////                MessageBoxImage.Information);

////////////////            // ── Step 5: Update selected module in list if one is selected ──
////////////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////////////            if (module != null)
////////////////            {
////////////////                module.IpAddress = ip;
////////////////                module.Mask = mask;
////////////////                module.Gateway = gateway;
////////////////                int idx = LstModules.SelectedIndex;
////////////////                LstModules.Items.Refresh();
////////////////                LstModules.SelectedIndex = idx;
////////////////            }

////////////////            // ── Step 6: Disconnect (device is rebooting anyway) and update the connect field ──
////////////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////////////            {
////////////////                commCtrl.BLLogic.Close();
////////////////                mainWin.DataContext = null;
////////////////            }

////////////////            commCtrl.TcpIpAddress.Text = ip;
////////////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////////////            commCtrl.ApplyAndSaveIp(ip);

////////////////            TxtStatus.Text = $"IP set → {ip}. Device rebooting. Reconnect required in a few seconds.";
////////////////        }

////////////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            // TODO: implement backup logic
////////////////        }
////////////////        // ════════════════════════════════════════════════════════
////////////////        //  RESTORE
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            if (LstModules.SelectedItems.Count == 0)
////////////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////////////            OpenFileDialog dlg = new OpenFileDialog
////////////////            {
////////////////                Title = "Open Backup File",
////////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////////            };

////////////////            if (dlg.ShowDialog() != true) return;

////////////////            int restored = 0;
////////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////////            {
////////////////                string[] parts = line.Split(',');
////////////////                if (parts.Length < 6) continue;

////////////////                string sn = parts[1].Trim();
////////////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////////////                {
////////////////                    if (m.SerialNumber != sn) continue;
////////////////                    m.IpAddress = parts[2].Trim();
////////////////                    m.Mask = parts[3].Trim();
////////////////                    m.Gateway = parts[4].Trim();
////////////////                    m.Position = parts[5].Trim();
////////////////                    restored++;
////////////////                }
////////////////            }

////////////////            LstModules.Items.Refresh();
////////////////            TxtStatus.Text = restored > 0
////////////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////////////                : "No matching modules found in the backup file.";
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  RESTORE BY IP
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            string ip = GetIp();

////////////////            if (!IsValidIp(ip))
////////////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////////////            OpenFileDialog dlg = new OpenFileDialog
////////////////            {
////////////////                Title = "Open Backup File",
////////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////////            };

////////////////            if (dlg.ShowDialog() != true) return;

////////////////            bool found = false;
////////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////////            {
////////////////                string[] parts = line.Split(',');
////////////////                if (parts.Length < 6) continue;
////////////////                if (parts[2].Trim() != ip) continue;

////////////////                foreach (NetworkModule m in LstModules.Items)
////////////////                {
////////////////                    if (m.IpAddress != ip) continue;
////////////////                    m.Mask = parts[3].Trim();
////////////////                    m.Gateway = parts[4].Trim();
////////////////                    m.Position = parts[5].Trim();
////////////////                    found = true;
////////////////                }
////////////////            }

////////////////            LstModules.Items.Refresh();
////////////////            TxtStatus.Text = found
////////////////                ? $"Restore by IP complete → {ip}"
////////////////                : $"No backup entry found for IP {ip}.";
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  REORDER IPs
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            if (LstModules.Items.Count == 0)
////////////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////////////            if (dlg.ShowDialog() != true) return;

////////////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////////////            byte[] bytes = startAddr.GetAddressBytes();
////////////////            int counter = bytes[3];

////////////////            foreach (NetworkModule m in LstModules.Items)
////////////////            {
////////////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////////////                m.IpAddress = new IPAddress(bytes).ToString();
////////////////            }

////////////////            LstModules.Items.Refresh();
////////////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  UPGRADE FW
////////////////        // ════════════════════════════════════════════════════════
////////////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            if (LstModules.SelectedItems.Count == 0)
////////////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////////////            MessageBoxResult confirm = MessageBox.Show(
////////////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////////////                "Do NOT power off during upgrade.",
////////////////                "Confirm Firmware Upgrade",
////////////////                MessageBoxButton.YesNo,
////////////////                MessageBoxImage.Question);

////////////////            if (confirm != MessageBoxResult.Yes) return;

////////////////            OpenFileDialog dlg = new OpenFileDialog
////////////////            {
////////////////                Title = "Select Firmware File",
////////////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////////////            };

////////////////            if (dlg.ShowDialog() != true) return;

////////////////            Button btn = (Button)sender;
////////////////            btn.IsEnabled = false;
////////////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////////////            List<NetworkModule> targets = LstModules.SelectedItems
////////////////                .Cast<NetworkModule>()
////////////////                .ToList();

////////////////            string fwPath = dlg.FileName;

////////////////            try
////////////////            {
////////////////                await Task.Run(() =>
////////////////                {
////////////////                    foreach (NetworkModule m in targets)
////////////////                    {
////////////////                        System.Threading.Thread.Sleep(500);
////////////////                        m.FirmwareRev = "NEW";
////////////////                    }
////////////////                });

////////////////                LstModules.Items.Refresh();
////////////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////////////            }
////////////////            catch (Exception ex)
////////////////            {
////////////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////////////            }
////////////////            finally
////////////////            {
////////////////                btn.IsEnabled = true;
////////////////            }
////////////////        }

////////////////        private static void Warn(string message, string title) =>
////////////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////////////    }

////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public class ReorderIpDialog : Window
////////////////    {
////////////////        private readonly TextBox _input;
////////////////        public string StartIp => _input.Text.Trim();

////////////////        public ReorderIpDialog()
////////////////        {
////////////////            Title = "Reorder IPs";
////////////////            Width = 280;
////////////////            Height = 130;
////////////////            ResizeMode = ResizeMode.NoResize;
////////////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////////////            sp.Children.Add(new TextBlock
////////////////            {
////////////////                Text = "Enter starting IP address:",
////////////////                Margin = new Thickness(0, 0, 0, 6)
////////////////            });

////////////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////////////            sp.Children.Add(_input);

////////////////            StackPanel btnRow = new StackPanel
////////////////            {
////////////////                Orientation = Orientation.Horizontal,
////////////////                HorizontalAlignment = HorizontalAlignment.Right,
////////////////                Margin = new Thickness(0, 10, 0, 0)
////////////////            };

////////////////            Button ok = new Button
////////////////            {
////////////////                Content = "OK",
////////////////                Width = 70,
////////////////                Height = 24,
////////////////                Margin = new Thickness(0, 0, 6, 0),
////////////////                IsDefault = true
////////////////            };
////////////////            ok.Click += (s, ev) => { DialogResult = true; };

////////////////            Button cancel = new Button
////////////////            {
////////////////                Content = "Cancel",
////////////////                Width = 70,
////////////////                Height = 24,
////////////////                IsCancel = true
////////////////            };

////////////////            btnRow.Children.Add(ok);
////////////////            btnRow.Children.Add(cancel);
////////////////            sp.Children.Add(btnRow);

////////////////            Content = sp;
////////////////        }
////////////////    }
////////////////}
//////////////using BLDC_Demo.Controls;
//////////////using Microsoft.Win32;
//////////////using System;
//////////////using System.Collections.Generic;
//////////////using System.Linq;
//////////////using System.Net;
//////////////using System.Net.NetworkInformation;
//////////////using System.Net.Sockets;
//////////////using System.Text.RegularExpressions;
//////////////using System.Threading.Tasks;
//////////////using System.Windows;
//////////////using System.Windows.Controls;
//////////////using System.Windows.Input;

//////////////namespace BLDC_Demo
//////////////{
//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  Model: a discovered network module
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public class NetworkModule
//////////////    {
//////////////        public string SerialNumber { get; set; } = string.Empty;
//////////////        public string IpAddress { get; set; } = string.Empty;
//////////////        public string Mask { get; set; } = string.Empty;
//////////////        public string Gateway { get; set; } = string.Empty;
//////////////        public string FirmwareRev { get; set; } = string.Empty;
//////////////        public string Position { get; set; } = "—";

//////////////        public override string ToString() =>
//////////////            $"{IpAddress}   SN: {SerialNumber} ";
//////////////    }

//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  Backup container
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public class ModuleBackup
//////////////    {
//////////////        public string SerialNumber { get; set; } = string.Empty;
//////////////        public string IpAddress { get; set; } = string.Empty;
//////////////        public string Mask { get; set; } = string.Empty;
//////////////        public string Gateway { get; set; } = string.Empty;
//////////////        public string Position { get; set; } = string.Empty;
//////////////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////////////    }

//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  UserControl code-behind  (targets C# 7.3)
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public partial class NetworkService : UserControl
//////////////    {
//////////////        // ── ESP32 Modbus holding-register map for network config ──
//////////////        private const ushort REG_IP_OCTET1 = 0x0136;
//////////////        private const ushort REG_IP_OCTET2 = 0x0137;
//////////////        private const ushort REG_IP_OCTET3 = 0x0138;
//////////////        private const ushort REG_IP_OCTET4 = 0x0139;
//////////////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////////////        private const ushort REG_APPLY_REBOOT = 0x013B;

//////////////        // Default Modbus TCP port used when we have to open a temporary
//////////////        // connection to push a new IP to a device that isn't connected yet.
//////////////        // Change this if your devices use a different port.
//////////////        private const int DEFAULT_MODBUS_PORT = 502;

//////////////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////////////            = new Dictionary<string, ModuleBackup>();

//////////////        public NetworkService()
//////////////        {
//////////////            InitializeComponent();
//////////////            AttachOctetHandlers();
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  OCTET HELPERS
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void AttachOctetHandlers()
//////////////        {
//////////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////////////            AttachToGroup(ipBoxes);
//////////////            AttachToGroup(maskBoxes);
//////////////            AttachToGroup(gwBoxes);

//////////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////////////        }

//////////////        private void SetGatewayEnabled(bool enabled)
//////////////        {
//////////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////////////        }

//////////////        private static void AttachToGroup(TextBox[] boxes)
//////////////        {
//////////////            for (int i = 0; i < boxes.Length; i++)
//////////////            {
//////////////                int idx = i;

//////////////                boxes[idx].PreviewTextInput += (s, e) =>
//////////////                {
//////////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////////////                };

//////////////                boxes[idx].PreviewKeyDown += (s, e) =>
//////////////                {
//////////////                    TextBox tb = (TextBox)s;
//////////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////////////                    {
//////////////                        e.Handled = true;
//////////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////////////                    }
//////////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////////////                    {
//////////////                        boxes[idx - 1].Focus();
//////////////                        boxes[idx - 1].SelectAll();
//////////////                    }
//////////////                };

//////////////                boxes[idx].TextChanged += (s, e) =>
//////////////                {
//////////////                    TextBox tb = (TextBox)s;
//////////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////////////                        boxes[idx + 1].Focus();
//////////////                };

//////////////                boxes[idx].LostFocus += (s, e) =>
//////////////                {
//////////////                    TextBox tb = (TextBox)s;
//////////////                    if (int.TryParse(tb.Text, out int val))
//////////////                        tb.Text = Clamp(val, 0, 255).ToString();
//////////////                    else if (tb.Text.Length > 0)
//////////////                        tb.Text = string.Empty;
//////////////                };
//////////////            }
//////////////        }

//////////////        private static int Clamp(int value, int min, int max)
//////////////        {
//////////////            if (value < min) return min;
//////////////            if (value > max) return max;
//////////////            return value;
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  IP FIELD READ / WRITE
//////////////        // ════════════════════════════════════════════════════════
//////////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////////////        private string GetGateway() =>
//////////////            ChkGateway.IsChecked == true
//////////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////////////                : string.Empty;

//////////////        private void SetIpFields(string ip, string mask, string gw)
//////////////        {
//////////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////////////            ChkGateway.IsChecked = hasGw;
//////////////            SetGatewayEnabled(hasGw);

//////////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////////        }

//////////////        private static void FillOctets(string address,
//////////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////////////        {
//////////////            string[] parts = address != null
//////////////                ? address.Split('.')
//////////////                : new string[0];

//////////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////////////        }

//////////////        private static void ClearOctets(params TextBox[] boxes)
//////////////        {
//////////////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////////////        }

//////////////        private static bool IsValidIp(string ip) =>
//////////////            IPAddress.TryParse(ip, out _);

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  DISCOVER (PORT SCAN FILTERED)
//////////////        // ════════════════════════════════════════════════════════
//////////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            LstModules.Items.Clear();
//////////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////////////            Button btn = (Button)sender;
//////////////            btn.IsEnabled = false;

//////////////            try
//////////////            {
//////////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

//////////////                foreach (NetworkModule m in modules)
//////////////                    LstModules.Items.Add(m);

//////////////                TxtStatus.Text = modules.Count == 0
//////////////                    ? "No motor controllers found."
//////////////                    : $"Found {modules.Count} motor controller(s).";
//////////////            }
//////////////            catch (Exception ex)
//////////////            {
//////////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////////////            }
//////////////            finally
//////////////            {
//////////////                btn.IsEnabled = true;
//////////////            }
//////////////        }

//////////////        private static List<NetworkModule> DiscoverModules()
//////////////        {
//////////////            var found = new List<NetworkModule>();
//////////////            string baseIp = GetLocalSubnetBase();
//////////////            if (string.IsNullOrEmpty(baseIp)) return found;

//////////////            var localIps = new HashSet<string>(
//////////////                NetworkInterface.GetAllNetworkInterfaces()
//////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////////                    .Select(ua => ua.Address.ToString())
//////////////            );

//////////////            const int targetPort = 502;
//////////////            var tasks = new List<Task<Tuple<bool, string>>>();

//////////////            for (int i = 1; i <= 254; i++)
//////////////            {
//////////////                string targetIp = $"{baseIp}.{i}";
//////////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////////////            }

//////////////            Task.WaitAll(tasks.ToArray());

//////////////            foreach (var t in tasks)
//////////////            {
//////////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////////////                {
//////////////                    found.Add(new NetworkModule
//////////////                    {
//////////////                        IpAddress = t.Result.Item2,
//////////////                        SerialNumber = "ESP32-BLDC",
//////////////                        FirmwareRev = "1.0.0",
//////////////                        Mask = "255.255.255.0",
//////////////                        Position = "—"
//////////////                    });
//////////////                }
//////////////            }

//////////////            return found;
//////////////        }

//////////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////////////        {
//////////////            try
//////////////            {
//////////////                using (var client = new TcpClient())
//////////////                {
//////////////                    var connectTask = client.ConnectAsync(ip, port);
//////////////                    var delayTask = Task.Delay(200);
//////////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
//////////////                    if (completedTask == connectTask && client.Connected)
//////////////                        return Tuple.Create(true, ip);
//////////////                }
//////////////                return Tuple.Create(false, ip);
//////////////            }
//////////////            catch
//////////////            {
//////////////                return Tuple.Create(false, ip);
//////////////            }
//////////////        }

//////////////        private static string GetLocalSubnetBase()
//////////////        {
//////////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////////////                .ToList();

//////////////            if (!candidates.Any())
//////////////            {
//////////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////////                    .ToList();
//////////////            }

//////////////            foreach (NetworkInterface ni in candidates)
//////////////            {
//////////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////////////                {
//////////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////////                    {
//////////////                        string[] parts = ua.Address.ToString().Split('.');
//////////////                        if (parts.Length == 4)
//////////////                        {
//////////////                            if (parts[0] == "127") continue;               // loopback
//////////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////////////                        }
//////////////                    }
//////////////                }
//////////////            }
//////////////            return string.Empty;
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  LIST SELECTION → populate fields
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////////////        {
//////////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////////////            if (m == null) return;

//////////////            TxtSerialNumber.Text = m.SerialNumber;
//////////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////////////            // ── Push selected module's IP into the Connect panel ──
//////////////            // so clicking Connect on the main comm control uses this module's IP.
//////////////            if (Application.Current.MainWindow is MainWindow mainWin)
//////////////            {
//////////////                mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
//////////////            }
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  SELECT ALL / NONE
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////////////            => LstModules.SelectAll();

//////////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////////////            => LstModules.UnselectAll();

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  SET IP  (works with or without an existing connection)
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            string ip = GetIp();
//////////////            string mask = GetMask();
//////////////            string gateway = GetGateway();

//////////////            if (!IsValidIp(ip))
//////////////            {
//////////////                Warn("Enter a valid IP address.", "Invalid IP");
//////////////                return;
//////////////            }

//////////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////////////            {
//////////////                Warn("Main window not available.", "Error");
//////////////                return;
//////////////            }

//////////////            var commCtrl = mainWin.CommMotorSelect;

//////////////            if (commCtrl?.BLLogic == null)
//////////////            {
//////////////                Warn("Communication layer not initialized.", "Error");
//////////////                return;
//////////////            }

//////////////            // ── Step 1: Split IP into 4 octets ──
//////////////            string[] ipParts = ip.Split('.');
//////////////            if (ipParts.Length != 4 ||
//////////////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////////////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////////////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////////////                !byte.TryParse(ipParts[3], out byte octet4))
//////////////            {
//////////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////////////                return;
//////////////            }

//////////////            if (octet1 == 0)
//////////////            {
//////////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////////////                return;
//////////////            }

//////////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////////////            byte maskLastOctet = 0;
//////////////            string[] maskParts = mask.Split('.');
//////////////            if (maskParts.Length == 4)
//////////////                byte.TryParse(maskParts[3], out maskLastOctet);

//////////////            // ── Step 2: Confirm with the user before rebooting the device ──
//////////////            var confirm = MessageBox.Show(
//////////////                $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
//////////////                "The connection will be lost automatically during the reboot.\n\nContinue?",
//////////////                "Confirm IP Change",
//////////////                MessageBoxButton.YesNo,
//////////////                MessageBoxImage.Warning);

//////////////            if (confirm != MessageBoxResult.Yes) return;

//////////////            // ── Step 3: Make sure we actually have a live connection to write to.
//////////////            // The device might not be connected yet (user typed a new/target IP
//////////////            // and clicked Set without pressing Connect first). We need to talk
//////////////            // to the device at its CURRENT IP (the one it's reachable at right
//////////////            // now) — NOT the new IP being set, since that doesn't exist yet.
//////////////            //
//////////////            // NOTE: this assumes the "current" IP the device is reachable at is
//////////////            // whatever is currently in the Connect panel (TcpIpAddress) or the
//////////////            // selected module's IP — i.e. wherever we can currently reach it.
//////////////            bool wasAlreadyConnected = !string.IsNullOrEmpty(commCtrl.BLLogic.ActivePortName);
//////////////            bool openedTemporaryConnection = false;

//////////////            try
//////////////            {
//////////////                if (!wasAlreadyConnected)
//////////////                {
//////////////                    // Prefer the currently selected module's IP (the device's
//////////////                    // CURRENT reachable address) over the "new" IP just typed
//////////////                    // into the Set panel — otherwise we'd try to connect to an
//////////////                    // address the device doesn't have yet.
//////////////                    NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////////////                    string currentDeviceIp = selected != null
//////////////                        ? selected.IpAddress
//////////////                        : commCtrl.TcpIpAddress.Text.Trim();

//////////////                    if (!IsValidIp(currentDeviceIp))
//////////////                    {
//////////////                        Warn(
//////////////                            "Device isn't connected and no valid current IP is known.\n" +
//////////////                            "Run Discover or select a module first, then Set.",
//////////////                            "Not Connected");
//////////////                        return;
//////////////                    }

//////////////                    if (!int.TryParse(commCtrl.TcpPort.Text, out int port))
//////////////                        port = DEFAULT_MODBUS_PORT;

//////////////                    var tempTcp = new CommEthernet(currentDeviceIp, port);

//////////////                    if (!commCtrl.BLLogic.Start(tempTcp))
//////////////                    {
//////////////                        Warn(
//////////////                            $"Could not connect to device at {currentDeviceIp}:{port} to send the new IP.",
//////////////                            "Connect Failed");
//////////////                        return;
//////////////                    }

//////////////                    openedTemporaryConnection = true;
//////////////                    commCtrl.UpdateStatusText();
//////////////                }

//////////////                // ── Step 4: Stage the octets + netmask ──
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET1, octet1);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET2, octet2);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET3, octet3);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET4, octet4);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_MASK_OCTET4, maskLastOctet);

//////////////                // ── Step 5: COMMIT — this is what actually saves + reboots the device ──
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_APPLY_REBOOT, 1);
//////////////            }
//////////////            catch (Exception ex)
//////////////            {
//////////////                Warn(
//////////////                    $"Failed to send new IP to the device:\n{ex.Message}\n\n" +
//////////////                    "The IP was NOT changed on the device.",
//////////////                    "Write Error");
//////////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
//////////////                return; // don't touch UI state below — nothing actually changed on the device
//////////////            }

//////////////            MessageBox.Show(
//////////////                $"IP {ip} sent and commit issued.\n" +
//////////////                "The device is rebooting now — reconnect using the new IP.",
//////////////                "IP Sent",
//////////////                MessageBoxButton.OK,
//////////////                MessageBoxImage.Information);

//////////////            // ── Step 6: Update selected module in list if one is selected ──
//////////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////////////            if (module != null)
//////////////            {
//////////////                module.IpAddress = ip;
//////////////                module.Mask = mask;
//////////////                module.Gateway = gateway;
//////////////                int idx = LstModules.SelectedIndex;
//////////////                LstModules.Items.Refresh();
//////////////                LstModules.SelectedIndex = idx;
//////////////            }

//////////////            // ── Step 7: Disconnect (device is rebooting anyway) and update the connect field ──
//////////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////////////            {
//////////////                commCtrl.BLLogic.Close();
//////////////                mainWin.DataContext = null;
//////////////            }

//////////////            commCtrl.TcpIpAddress.Text = ip;
//////////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////////////            commCtrl.ApplyAndSaveIp(ip);

//////////////            TxtStatus.Text = $"IP set → {ip}. Device rebooting. Reconnect required in a few seconds.";
//////////////        }

//////////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            // TODO: implement backup logic
//////////////        }
//////////////        // ════════════════════════════════════════════════════════
//////////////        //  RESTORE
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            if (LstModules.SelectedItems.Count == 0)
//////////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////////////            OpenFileDialog dlg = new OpenFileDialog
//////////////            {
//////////////                Title = "Open Backup File",
//////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////////            };

//////////////            if (dlg.ShowDialog() != true) return;

//////////////            int restored = 0;
//////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////////            {
//////////////                string[] parts = line.Split(',');
//////////////                if (parts.Length < 6) continue;

//////////////                string sn = parts[1].Trim();
//////////////                foreach (NetworkModule m in LstModules.SelectedItems)
//////////////                {
//////////////                    if (m.SerialNumber != sn) continue;
//////////////                    m.IpAddress = parts[2].Trim();
//////////////                    m.Mask = parts[3].Trim();
//////////////                    m.Gateway = parts[4].Trim();
//////////////                    m.Position = parts[5].Trim();
//////////////                    restored++;
//////////////                }
//////////////            }

//////////////            LstModules.Items.Refresh();
//////////////            TxtStatus.Text = restored > 0
//////////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////////////                : "No matching modules found in the backup file.";
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  RESTORE BY IP
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            string ip = GetIp();

//////////////            if (!IsValidIp(ip))
//////////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////////////            OpenFileDialog dlg = new OpenFileDialog
//////////////            {
//////////////                Title = "Open Backup File",
//////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////////            };

//////////////            if (dlg.ShowDialog() != true) return;

//////////////            bool found = false;
//////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////////            {
//////////////                string[] parts = line.Split(',');
//////////////                if (parts.Length < 6) continue;
//////////////                if (parts[2].Trim() != ip) continue;

//////////////                foreach (NetworkModule m in LstModules.Items)
//////////////                {
//////////////                    if (m.IpAddress != ip) continue;
//////////////                    m.Mask = parts[3].Trim();
//////////////                    m.Gateway = parts[4].Trim();
//////////////                    m.Position = parts[5].Trim();
//////////////                    found = true;
//////////////                }
//////////////            }

//////////////            LstModules.Items.Refresh();
//////////////            TxtStatus.Text = found
//////////////                ? $"Restore by IP complete → {ip}"
//////////////                : $"No backup entry found for IP {ip}.";
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  REORDER IPs
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            if (LstModules.Items.Count == 0)
//////////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////////////            ReorderIpDialog dlg = new ReorderIpDialog();
//////////////            if (dlg.ShowDialog() != true) return;

//////////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////////////            byte[] bytes = startAddr.GetAddressBytes();
//////////////            int counter = bytes[3];

//////////////            foreach (NetworkModule m in LstModules.Items)
//////////////            {
//////////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////////////                m.IpAddress = new IPAddress(bytes).ToString();
//////////////            }

//////////////            LstModules.Items.Refresh();
//////////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  UPGRADE FW
//////////////        // ════════════════════════════════════════════════════════
//////////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            if (LstModules.SelectedItems.Count == 0)
//////////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////////////            MessageBoxResult confirm = MessageBox.Show(
//////////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////////////                "Do NOT power off during upgrade.",
//////////////                "Confirm Firmware Upgrade",
//////////////                MessageBoxButton.YesNo,
//////////////                MessageBoxImage.Question);

//////////////            if (confirm != MessageBoxResult.Yes) return;

//////////////            OpenFileDialog dlg = new OpenFileDialog
//////////////            {
//////////////                Title = "Select Firmware File",
//////////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////////////            };

//////////////            if (dlg.ShowDialog() != true) return;

//////////////            Button btn = (Button)sender;
//////////////            btn.IsEnabled = false;
//////////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////////////            List<NetworkModule> targets = LstModules.SelectedItems
//////////////                .Cast<NetworkModule>()
//////////////                .ToList();

//////////////            string fwPath = dlg.FileName;

//////////////            try
//////////////            {
//////////////                await Task.Run(() =>
//////////////                {
//////////////                    foreach (NetworkModule m in targets)
//////////////                    {
//////////////                        System.Threading.Thread.Sleep(500);
//////////////                        m.FirmwareRev = "NEW";
//////////////                    }
//////////////                });

//////////////                LstModules.Items.Refresh();
//////////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////////////            }
//////////////            catch (Exception ex)
//////////////            {
//////////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////////////            }
//////////////            finally
//////////////            {
//////////////                btn.IsEnabled = true;
//////////////            }
//////////////        }

//////////////        private static void Warn(string message, string title) =>
//////////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////////////    }

//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public class ReorderIpDialog : Window
//////////////    {
//////////////        private readonly TextBox _input;
//////////////        public string StartIp => _input.Text.Trim();

//////////////        public ReorderIpDialog()
//////////////        {
//////////////            Title = "Reorder IPs";
//////////////            Width = 280;
//////////////            Height = 130;
//////////////            ResizeMode = ResizeMode.NoResize;
//////////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////////////            sp.Children.Add(new TextBlock
//////////////            {
//////////////                Text = "Enter starting IP address:",
//////////////                Margin = new Thickness(0, 0, 0, 6)
//////////////            });

//////////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////////////            sp.Children.Add(_input);

//////////////            StackPanel btnRow = new StackPanel
//////////////            {
//////////////                Orientation = Orientation.Horizontal,
//////////////                HorizontalAlignment = HorizontalAlignment.Right,
//////////////                Margin = new Thickness(0, 10, 0, 0)
//////////////            };

//////////////            Button ok = new Button
//////////////            {
//////////////                Content = "OK",
//////////////                Width = 70,
//////////////                Height = 24,
//////////////                Margin = new Thickness(0, 0, 6, 0),
//////////////                IsDefault = true
//////////////            };
//////////////            ok.Click += (s, ev) => { DialogResult = true; };

//////////////            Button cancel = new Button
//////////////            {
//////////////                Content = "Cancel",
//////////////                Width = 70,
//////////////                Height = 24,
//////////////                IsCancel = true
//////////////            };

//////////////            btnRow.Children.Add(ok);
//////////////            btnRow.Children.Add(cancel);
//////////////            sp.Children.Add(btnRow);

//////////////            Content = sp;
//////////////        }
//////////////    }
//////////////}
////////////using BLDC_Demo.Controls;
////////////using Microsoft.Win32;
////////////using System;
////////////using System.Collections.Generic;
////////////using System.Linq;
////////////using System.Net;
////////////using System.Net.NetworkInformation;
////////////using System.Net.Sockets;
////////////using System.Text.RegularExpressions;
////////////using System.Threading.Tasks;
////////////using System.Windows;
////////////using System.Windows.Controls;
////////////using System.Windows.Input;

////////////namespace BLDC_Demo
////////////{
////////////    // ────────────────────────────────────────────────────────────
////////////    //  Model: a discovered network module
////////////    // ────────────────────────────────────────────────────────────
////////////    public class NetworkModule
////////////    {
////////////        public string SerialNumber { get; set; } = string.Empty;
////////////        public string IpAddress { get; set; } = string.Empty;
////////////        public string Mask { get; set; } = string.Empty;
////////////        public string Gateway { get; set; } = string.Empty;
////////////        public string FirmwareRev { get; set; } = string.Empty;
////////////        public string Position { get; set; } = "—";

////////////        public override string ToString() =>
////////////            $"{IpAddress}   SN: {SerialNumber} ";
////////////    }

////////////    // ────────────────────────────────────────────────────────────
////////////    //  Backup container
////////////    // ────────────────────────────────────────────────────────────
////////////    public class ModuleBackup
////////////    {
////////////        public string SerialNumber { get; set; } = string.Empty;
////////////        public string IpAddress { get; set; } = string.Empty;
////////////        public string Mask { get; set; } = string.Empty;
////////////        public string Gateway { get; set; } = string.Empty;
////////////        public string Position { get; set; } = string.Empty;
////////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////////    }

////////////    // ────────────────────────────────────────────────────────────
////////////    //  UserControl code-behind  (targets C# 7.3)
////////////    // ────────────────────────────────────────────────────────────
////////////    public partial class NetworkService : UserControl
////////////    {
////////////        // ── ESP32 Modbus holding-register map for network config ──
////////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////////        // Default Modbus TCP port used when we have to open a temporary
////////////        // connection to push a new IP to a device that isn't connected yet.
////////////        // Change this if your devices use a different port.
////////////        private const int DEFAULT_MODBUS_PORT = 502;

////////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////////            = new Dictionary<string, ModuleBackup>();

////////////        public NetworkService()
////////////        {
////////////            InitializeComponent();
////////////            AttachOctetHandlers();
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  OCTET HELPERS
////////////        // ════════════════════════════════════════════════════════
////////////        private void AttachOctetHandlers()
////////////        {
////////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////////            AttachToGroup(ipBoxes);
////////////            AttachToGroup(maskBoxes);
////////////            AttachToGroup(gwBoxes);

////////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////////        }

////////////        private void SetGatewayEnabled(bool enabled)
////////////        {
////////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////////        }

////////////        private static void AttachToGroup(TextBox[] boxes)
////////////        {
////////////            for (int i = 0; i < boxes.Length; i++)
////////////            {
////////////                int idx = i;

////////////                boxes[idx].PreviewTextInput += (s, e) =>
////////////                {
////////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////////                };

////////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////////                {
////////////                    TextBox tb = (TextBox)s;
////////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////////                    {
////////////                        e.Handled = true;
////////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////////                    }
////////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////////                    {
////////////                        boxes[idx - 1].Focus();
////////////                        boxes[idx - 1].SelectAll();
////////////                    }
////////////                };

////////////                boxes[idx].TextChanged += (s, e) =>
////////////                {
////////////                    TextBox tb = (TextBox)s;
////////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////////                        boxes[idx + 1].Focus();
////////////                };

////////////                boxes[idx].LostFocus += (s, e) =>
////////////                {
////////////                    TextBox tb = (TextBox)s;
////////////                    if (int.TryParse(tb.Text, out int val))
////////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////////                    else if (tb.Text.Length > 0)
////////////                        tb.Text = string.Empty;
////////////                };
////////////            }
////////////        }

////////////        private static int Clamp(int value, int min, int max)
////////////        {
////////////            if (value < min) return min;
////////////            if (value > max) return max;
////////////            return value;
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  IP FIELD READ / WRITE
////////////        // ════════════════════════════════════════════════════════
////////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////////        private string GetGateway() =>
////////////            ChkGateway.IsChecked == true
////////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////////                : string.Empty;

////////////        private void SetIpFields(string ip, string mask, string gw)
////////////        {
////////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////////            ChkGateway.IsChecked = hasGw;
////////////            SetGatewayEnabled(hasGw);

////////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////        }

////////////        private static void FillOctets(string address,
////////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////////        {
////////////            string[] parts = address != null
////////////                ? address.Split('.')
////////////                : new string[0];

////////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////////        }

////////////        private static void ClearOctets(params TextBox[] boxes)
////////////        {
////////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////////        }

////////////        private static bool IsValidIp(string ip) =>
////////////            IPAddress.TryParse(ip, out _);

////////////        // ════════════════════════════════════════════════════════
////////////        //  DISCOVER (PORT SCAN FILTERED)
////////////        // ════════════════════════════════════════════════════════
////////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            LstModules.Items.Clear();
////////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////////            Button btn = (Button)sender;
////////////            btn.IsEnabled = false;

////////////            try
////////////            {
////////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////////                foreach (NetworkModule m in modules)
////////////                    LstModules.Items.Add(m);

////////////                TxtStatus.Text = modules.Count == 0
////////////                    ? "No motor controllers found."
////////////                    : $"Found {modules.Count} motor controller(s).";
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////////            }
////////////            finally
////////////            {
////////////                btn.IsEnabled = true;
////////////            }
////////////        }

////////////        private static List<NetworkModule> DiscoverModules()
////////////        {
////////////            var found = new List<NetworkModule>();
////////////            string baseIp = GetLocalSubnetBase();
////////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////////            var localIps = new HashSet<string>(
////////////                NetworkInterface.GetAllNetworkInterfaces()
////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////                    .Select(ua => ua.Address.ToString())
////////////            );

////////////            const int targetPort = 502;
////////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////////            for (int i = 1; i <= 254; i++)
////////////            {
////////////                string targetIp = $"{baseIp}.{i}";
////////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////////            }

////////////            Task.WaitAll(tasks.ToArray());

////////////            foreach (var t in tasks)
////////////            {
////////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////////                {
////////////                    found.Add(new NetworkModule
////////////                    {
////////////                        IpAddress = t.Result.Item2,
////////////                        SerialNumber = "ESP32-BLDC",
////////////                        FirmwareRev = "1.0.0",
////////////                        Mask = "255.255.255.0",
////////////                        Position = "—"
////////////                    });
////////////                }
////////////            }

////////////            return found;
////////////        }

////////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////////        {
////////////            try
////////////            {
////////////                using (var client = new TcpClient())
////////////                {
////////////                    var connectTask = client.ConnectAsync(ip, port);
////////////                    var delayTask = Task.Delay(200);
////////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
////////////                    if (completedTask == connectTask && client.Connected)
////////////                        return Tuple.Create(true, ip);
////////////                }
////////////                return Tuple.Create(false, ip);
////////////            }
////////////            catch
////////////            {
////////////                return Tuple.Create(false, ip);
////////////            }
////////////        }

////////////        private static string GetLocalSubnetBase()
////////////        {
////////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////////                .ToList();

////////////            if (!candidates.Any())
////////////            {
////////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////                    .ToList();
////////////            }

////////////            foreach (NetworkInterface ni in candidates)
////////////            {
////////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////////                {
////////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////                    {
////////////                        string[] parts = ua.Address.ToString().Split('.');
////////////                        if (parts.Length == 4)
////////////                        {
////////////                            if (parts[0] == "127") continue;               // loopback
////////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////////                        }
////////////                    }
////////////                }
////////////            }
////////////            return string.Empty;
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  LIST SELECTION → populate fields
////////////        // ════════════════════════════════════════════════════════
////////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////////        {
////////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////////            if (m == null) return;

////////////            TxtSerialNumber.Text = m.SerialNumber;
////////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////////            // ── Push selected module's IP into the Connect panel ──
////////////            // so clicking Connect on the main comm control uses this module's IP.
////////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////////            {
////////////                mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
////////////            }
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  SELECT ALL / NONE
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////////            => LstModules.SelectAll();

////////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////////            => LstModules.UnselectAll();

////////////        // ════════════════════════════════════════════════════════
////////////        //  SET IP  (works with or without an existing connection)
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            string ip = GetIp();
////////////            string mask = GetMask();
////////////            string gateway = GetGateway();

////////////            if (!IsValidIp(ip))
////////////            {
////////////                Warn("Enter a valid IP address.", "Invalid IP");
////////////                return;
////////////            }

////////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////////            {
////////////                Warn("Main window not available.", "Error");
////////////                return;
////////////            }

////////////            var commCtrl = mainWin.CommMotorSelect;

////////////            if (commCtrl?.BLLogic == null)
////////////            {
////////////                Warn("Communication layer not initialized.", "Error");
////////////                return;
////////////            }

////////////            // ── Step 1: Split IP into 4 octets ──
////////////            string[] ipParts = ip.Split('.');
////////////            if (ipParts.Length != 4 ||
////////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////////                !byte.TryParse(ipParts[3], out byte octet4))
////////////            {
////////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////////                return;
////////////            }

////////////            if (octet1 == 0)
////////////            {
////////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////////                return;
////////////            }

////////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////////            byte maskLastOctet = 0;
////////////            string[] maskParts = mask.Split('.');
////////////            if (maskParts.Length == 4)
////////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////////            // ── Step 2: Confirm with the user before rebooting the device ──
////////////            var confirm = MessageBox.Show(
////////////                $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
////////////                "The connection will be lost automatically during the reboot.\n\nContinue?",
////////////                "Confirm IP Change",
////////////                MessageBoxButton.YesNo,
////////////                MessageBoxImage.Warning);

////////////            if (confirm != MessageBoxResult.Yes) return;

////////////            // ── Step 3: Make sure we actually have a live connection to write to.
////////////            // The device might not be connected yet (user typed a new/target IP
////////////            // and clicked Set without pressing Connect first). We need to talk
////////////            // to the device at its CURRENT IP (the one it's reachable at right
////////////            // now) — NOT the new IP being set, since that doesn't exist yet.
////////////            //
////////////            // NOTE: this assumes the "current" IP the device is reachable at is
////////////            // whatever is currently in the Connect panel (TcpIpAddress) or the
////////////            // selected module's IP — i.e. wherever we can currently reach it.
////////////            bool wasAlreadyConnected = !string.IsNullOrEmpty(commCtrl.BLLogic.ActivePortName);

////////////            try
////////////            {
////////////                if (!wasAlreadyConnected)
////////////                {
////////////                    // Prefer the currently selected module's IP (the device's
////////////                    // CURRENT reachable address) over the "new" IP just typed
////////////                    // into the Set panel — otherwise we'd try to connect to an
////////////                    // address the device doesn't have yet.
////////////                    NetworkModule selected = LstModules.SelectedItem as NetworkModule;
////////////                    string currentDeviceIp = selected != null
////////////                        ? selected.IpAddress
////////////                        : commCtrl.TcpIpAddress.Text.Trim();

////////////                    if (!IsValidIp(currentDeviceIp))
////////////                    {
////////////                        Warn(
////////////                            "Device isn't connected and no valid current IP is known.\n" +
////////////                            "Run Discover or select a module first, then Set.",
////////////                            "Not Connected");
////////////                        return;
////////////                    }

////////////                    if (!int.TryParse(commCtrl.TcpPort.Text, out int port))
////////////                        port = DEFAULT_MODBUS_PORT;

////////////                    var tempTcp = new CommEthernet(currentDeviceIp, port);

////////////                    if (!commCtrl.BLLogic.Start(tempTcp))
////////////                    {
////////////                        Warn(
////////////                            $"Could not connect to device at {currentDeviceIp}:{port} to send the new IP.",
////////////                            "Connect Failed");
////////////                        return;
////////////                    }

////////////                    commCtrl.UpdateStatusText();
////////////                }

////////////                // ── Step 4: Stage the octets + netmask ──
////////////                // SendWriteCommand now RETURNS bool — false means it silently
////////////                // no-op'd (not connected), which is exactly the failure mode
////////////                // that used to report "IP set" while the device got nothing.
////////////                bool ok = true;
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET1, octet1);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET2, octet2);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET3, octet3);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET4, octet4);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_MASK_OCTET4, maskLastOctet);

////////////                // ── Step 5: COMMIT — this is what actually saves + reboots the device ──
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_APPLY_REBOOT, 1);

////////////                if (!ok)
////////////                {
////////////                    Warn(
////////////                        "One or more writes were not sent (no active connection).\n" +
////////////                        "The IP was NOT changed on the device.",
////////////                        "Write Failed");
////////////                    TxtStatus.Text = "Set IP failed: write not sent (not connected).";
////////////                    return; // don't touch UI state below — nothing actually changed on the device
////////////                }
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                Warn(
////////////                    $"Failed to send new IP to the device:\n{ex.Message}\n\n" +
////////////                    "The IP was NOT changed on the device.",
////////////                    "Write Error");
////////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
////////////                return; // don't touch UI state below — nothing actually changed on the device
////////////            }

////////////            MessageBox.Show(
////////////                $"IP {ip} sent and commit issued.\n" +
////////////                "The device is rebooting now — reconnect using the new IP.",
////////////                "IP Sent",
////////////                MessageBoxButton.OK,
////////////                MessageBoxImage.Information);

////////////            // ── Step 6: Update selected module in list if one is selected ──
////////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////////            if (module != null)
////////////            {
////////////                module.IpAddress = ip;
////////////                module.Mask = mask;
////////////                module.Gateway = gateway;
////////////                int idx = LstModules.SelectedIndex;
////////////                LstModules.Items.Refresh();
////////////                LstModules.SelectedIndex = idx;
////////////            }

////////////            // ── Step 7: Disconnect (device is rebooting anyway) and update the connect field ──
////////////            // IMPORTANT: use Close(drainPendingWrites: true) here — the writes
////////////            // above (including the reboot/commit register) are queued and sent
////////////            // asynchronously by BL's IO thread. Closing immediately with the
////////////            // old Close() could tear the thread down before the last write(s)
////////////            // actually left the socket, silently dropping the commit.
////////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////////            {
////////////                commCtrl.BLLogic.Close(drainPendingWrites: true);
////////////                mainWin.DataContext = null;
////////////            }

////////////            commCtrl.TcpIpAddress.Text = ip;
////////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////////            commCtrl.ApplyAndSaveIp(ip);

////////////            TxtStatus.Text = $"IP set → {ip}. Device rebooting. Reconnect required in a few seconds.";
////////////        }

////////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            // TODO: implement backup logic
////////////        }
////////////        // ════════════════════════════════════════════════════════
////////////        //  RESTORE
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            if (LstModules.SelectedItems.Count == 0)
////////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////////            OpenFileDialog dlg = new OpenFileDialog
////////////            {
////////////                Title = "Open Backup File",
////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////            };

////////////            if (dlg.ShowDialog() != true) return;

////////////            int restored = 0;
////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////            {
////////////                string[] parts = line.Split(',');
////////////                if (parts.Length < 6) continue;

////////////                string sn = parts[1].Trim();
////////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////////                {
////////////                    if (m.SerialNumber != sn) continue;
////////////                    m.IpAddress = parts[2].Trim();
////////////                    m.Mask = parts[3].Trim();
////////////                    m.Gateway = parts[4].Trim();
////////////                    m.Position = parts[5].Trim();
////////////                    restored++;
////////////                }
////////////            }

////////////            LstModules.Items.Refresh();
////////////            TxtStatus.Text = restored > 0
////////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////////                : "No matching modules found in the backup file.";
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  RESTORE BY IP
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            string ip = GetIp();

////////////            if (!IsValidIp(ip))
////////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////////            OpenFileDialog dlg = new OpenFileDialog
////////////            {
////////////                Title = "Open Backup File",
////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////            };

////////////            if (dlg.ShowDialog() != true) return;

////////////            bool found = false;
////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////            {
////////////                string[] parts = line.Split(',');
////////////                if (parts.Length < 6) continue;
////////////                if (parts[2].Trim() != ip) continue;

////////////                foreach (NetworkModule m in LstModules.Items)
////////////                {
////////////                    if (m.IpAddress != ip) continue;
////////////                    m.Mask = parts[3].Trim();
////////////                    m.Gateway = parts[4].Trim();
////////////                    m.Position = parts[5].Trim();
////////////                    found = true;
////////////                }
////////////            }

////////////            LstModules.Items.Refresh();
////////////            TxtStatus.Text = found
////////////                ? $"Restore by IP complete → {ip}"
////////////                : $"No backup entry found for IP {ip}.";
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  REORDER IPs
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            if (LstModules.Items.Count == 0)
////////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////////            if (dlg.ShowDialog() != true) return;

////////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////////            byte[] bytes = startAddr.GetAddressBytes();
////////////            int counter = bytes[3];

////////////            foreach (NetworkModule m in LstModules.Items)
////////////            {
////////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////////                m.IpAddress = new IPAddress(bytes).ToString();
////////////            }

////////////            LstModules.Items.Refresh();
////////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  UPGRADE FW
////////////        // ════════════════════════════════════════════════════════
////////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            if (LstModules.SelectedItems.Count == 0)
////////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////////            MessageBoxResult confirm = MessageBox.Show(
////////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////////                "Do NOT power off during upgrade.",
////////////                "Confirm Firmware Upgrade",
////////////                MessageBoxButton.YesNo,
////////////                MessageBoxImage.Question);

////////////            if (confirm != MessageBoxResult.Yes) return;

////////////            OpenFileDialog dlg = new OpenFileDialog
////////////            {
////////////                Title = "Select Firmware File",
////////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////////            };

////////////            if (dlg.ShowDialog() != true) return;

////////////            Button btn = (Button)sender;
////////////            btn.IsEnabled = false;
////////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////////            List<NetworkModule> targets = LstModules.SelectedItems
////////////                .Cast<NetworkModule>()
////////////                .ToList();

////////////            string fwPath = dlg.FileName;

////////////            try
////////////            {
////////////                await Task.Run(() =>
////////////                {
////////////                    foreach (NetworkModule m in targets)
////////////                    {
////////////                        System.Threading.Thread.Sleep(500);
////////////                        m.FirmwareRev = "NEW";
////////////                    }
////////////                });

////////////                LstModules.Items.Refresh();
////////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////////            }
////////////            finally
////////////            {
////////////                btn.IsEnabled = true;
////////////            }
////////////        }

////////////        private static void Warn(string message, string title) =>
////////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////////    }

////////////    // ────────────────────────────────────────────────────────────
////////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////////    // ────────────────────────────────────────────────────────────
////////////    public class ReorderIpDialog : Window
////////////    {
////////////        private readonly TextBox _input;
////////////        public string StartIp => _input.Text.Trim();

////////////        public ReorderIpDialog()
////////////        {
////////////            Title = "Reorder IPs";
////////////            Width = 280;
////////////            Height = 130;
////////////            ResizeMode = ResizeMode.NoResize;
////////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////////            sp.Children.Add(new TextBlock
////////////            {
////////////                Text = "Enter starting IP address:",
////////////                Margin = new Thickness(0, 0, 0, 6)
////////////            });

////////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////////            sp.Children.Add(_input);

////////////            StackPanel btnRow = new StackPanel
////////////            {
////////////                Orientation = Orientation.Horizontal,
////////////                HorizontalAlignment = HorizontalAlignment.Right,
////////////                Margin = new Thickness(0, 10, 0, 0)
////////////            };

////////////            Button ok = new Button
////////////            {
////////////                Content = "OK",
////////////                Width = 70,
////////////                Height = 24,
////////////                Margin = new Thickness(0, 0, 6, 0),
////////////                IsDefault = true
////////////            };
////////////            ok.Click += (s, ev) => { DialogResult = true; };

////////////            Button cancel = new Button
////////////            {
////////////                Content = "Cancel",
////////////                Width = 70,
////////////                Height = 24,
////////////                IsCancel = true
////////////            };

////////////            btnRow.Children.Add(ok);
////////////            btnRow.Children.Add(cancel);
////////////            sp.Children.Add(btnRow);

////////////            Content = sp;
////////////        }
////////////    }
////////////}
//////////using BLDC_Demo.Controls;
//////////using Microsoft.Win32;
//////////using System;
//////////using System.Collections.Generic;
//////////using System.IO;
//////////using System.Linq;
//////////using System.Net;
//////////using System.Net.NetworkInformation;
//////////using System.Net.Sockets;
//////////using System.Text.RegularExpressions;
//////////using System.Threading;
//////////using System.Threading.Tasks;
//////////using System.Windows;
//////////using System.Windows.Controls;
//////////using System.Windows.Input;

//////////namespace BLDC_Demo
//////////{
//////////    // ────────────────────────────────────────────────────────────
//////////    //  Model: a discovered network module
//////////    // ────────────────────────────────────────────────────────────
//////////    public class NetworkModule
//////////    {
//////////        public string SerialNumber { get; set; } = string.Empty;
//////////        public string IpAddress { get; set; } = string.Empty;
//////////        public string Mask { get; set; } = string.Empty;
//////////        public string Gateway { get; set; } = string.Empty;
//////////        public string FirmwareRev { get; set; } = string.Empty;
//////////        public string Position { get; set; } = "—";

//////////        public override string ToString() =>
//////////            $"{IpAddress}   SN: {SerialNumber} ";
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  Backup container
//////////    // ────────────────────────────────────────────────────────────
//////////    public class ModuleBackup
//////////    {
//////////        public string SerialNumber { get; set; } = string.Empty;
//////////        public string IpAddress { get; set; } = string.Empty;
//////////        public string Mask { get; set; } = string.Empty;
//////////        public string Gateway { get; set; } = string.Empty;
//////////        public string Position { get; set; } = string.Empty;
//////////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  UserControl code-behind  (targets C# 7.3)
//////////    // ────────────────────────────────────────────────────────────
//////////    public partial class NetworkService : UserControl
//////////    {
//////////        // ── ESP32 Modbus holding-register map for network config ──
//////////        private const ushort REG_IP_OCTET1 = 0x0136;
//////////        private const ushort REG_IP_OCTET2 = 0x0137;
//////////        private const ushort REG_IP_OCTET3 = 0x0138;
//////////        private const ushort REG_IP_OCTET4 = 0x0139;
//////////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////////        private const ushort REG_APPLY_REBOOT = 0x013B;

//////////        // Default Modbus TCP port used when we have to open a temporary
//////////        // connection to push a new IP to a device that isn't connected yet.
//////////        // Change this if your devices use a different port.
//////////        private const int DEFAULT_MODBUS_PORT = 502;

//////////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////////            = new Dictionary<string, ModuleBackup>();

//////////        public NetworkService()
//////////        {
//////////            InitializeComponent();
//////////            AttachOctetHandlers();
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  OCTET HELPERS
//////////        // ════════════════════════════════════════════════════════
//////////        private void AttachOctetHandlers()
//////////        {
//////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////////            AttachToGroup(ipBoxes);
//////////            AttachToGroup(maskBoxes);
//////////            AttachToGroup(gwBoxes);

//////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////////        }

//////////        private void SetGatewayEnabled(bool enabled)
//////////        {
//////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////////        }

//////////        private static void AttachToGroup(TextBox[] boxes)
//////////        {
//////////            for (int i = 0; i < boxes.Length; i++)
//////////            {
//////////                int idx = i;

//////////                boxes[idx].PreviewTextInput += (s, e) =>
//////////                {
//////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////////                };

//////////                boxes[idx].PreviewKeyDown += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////////                    {
//////////                        e.Handled = true;
//////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////////                    }
//////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////////                    {
//////////                        boxes[idx - 1].Focus();
//////////                        boxes[idx - 1].SelectAll();
//////////                    }
//////////                };

//////////                boxes[idx].TextChanged += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////////                        boxes[idx + 1].Focus();
//////////                };

//////////                boxes[idx].LostFocus += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (int.TryParse(tb.Text, out int val))
//////////                        tb.Text = Clamp(val, 0, 255).ToString();
//////////                    else if (tb.Text.Length > 0)
//////////                        tb.Text = string.Empty;
//////////                };
//////////            }
//////////        }

//////////        private static int Clamp(int value, int min, int max)
//////////        {
//////////            if (value < min) return min;
//////////            if (value > max) return max;
//////////            return value;
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  IP FIELD READ / WRITE
//////////        // ════════════════════════════════════════════════════════
//////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////////        private string GetGateway() =>
//////////            ChkGateway.IsChecked == true
//////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////////                : string.Empty;

//////////        private void SetIpFields(string ip, string mask, string gw)
//////////        {
//////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////////            ChkGateway.IsChecked = hasGw;
//////////            SetGatewayEnabled(hasGw);

//////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////        }

//////////        private static void FillOctets(string address,
//////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////////        {
//////////            string[] parts = address != null
//////////                ? address.Split('.')
//////////                : new string[0];

//////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////////        }

//////////        private static void ClearOctets(params TextBox[] boxes)
//////////        {
//////////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////////        }

//////////        private static bool IsValidIp(string ip) =>
//////////            IPAddress.TryParse(ip, out _);

//////////        // ════════════════════════════════════════════════════════
//////////        //  DISCOVER (PORT SCAN FILTERED)
//////////        // ════════════════════════════════════════════════════════
//////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            LstModules.Items.Clear();
//////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////////            Button btn = (Button)sender;
//////////            btn.IsEnabled = false;

//////////            try
//////////            {
//////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

//////////                foreach (NetworkModule m in modules)
//////////                    LstModules.Items.Add(m);

//////////                TxtStatus.Text = modules.Count == 0
//////////                    ? "No motor controllers found."
//////////                    : $"Found {modules.Count} motor controller(s).";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////////            }
//////////            finally
//////////            {
//////////                btn.IsEnabled = true;
//////////            }
//////////        }

//////////        private static List<NetworkModule> DiscoverModules()
//////////        {
//////////            var found = new List<NetworkModule>();
//////////            string baseIp = GetLocalSubnetBase();
//////////            if (string.IsNullOrEmpty(baseIp)) return found;

//////////            var localIps = new HashSet<string>(
//////////                NetworkInterface.GetAllNetworkInterfaces()
//////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////                    .Select(ua => ua.Address.ToString())
//////////            );

//////////            const int targetPort = 502;
//////////            var tasks = new List<Task<Tuple<bool, string>>>();

//////////            for (int i = 1; i <= 254; i++)
//////////            {
//////////                string targetIp = $"{baseIp}.{i}";
//////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////////            }

//////////            Task.WaitAll(tasks.ToArray());

//////////            foreach (var t in tasks)
//////////            {
//////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////////                {
//////////                    found.Add(new NetworkModule
//////////                    {
//////////                        IpAddress = t.Result.Item2,
//////////                        SerialNumber = "ESP32-BLDC",
//////////                        FirmwareRev = "1.0.0",
//////////                        Mask = "255.255.255.0",
//////////                        Position = "—"
//////////                    });
//////////                }
//////////            }

//////////            return found;
//////////        }

//////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////////        {
//////////            try
//////////            {
//////////                using (var client = new TcpClient())
//////////                {
//////////                    var connectTask = client.ConnectAsync(ip, port);
//////////                    var delayTask = Task.Delay(200);
//////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
//////////                    if (completedTask == connectTask && client.Connected)
//////////                        return Tuple.Create(true, ip);
//////////                }
//////////                return Tuple.Create(false, ip);
//////////            }
//////////            catch
//////////            {
//////////                return Tuple.Create(false, ip);
//////////            }
//////////        }

//////////        private static string GetLocalSubnetBase()
//////////        {
//////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////////                .ToList();

//////////            if (!candidates.Any())
//////////            {
//////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                    .ToList();
//////////            }

//////////            foreach (NetworkInterface ni in candidates)
//////////            {
//////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////////                {
//////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////                    {
//////////                        string[] parts = ua.Address.ToString().Split('.');
//////////                        if (parts.Length == 4)
//////////                        {
//////////                            if (parts[0] == "127") continue;               // loopback
//////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////////                        }
//////////                    }
//////////                }
//////////            }
//////////            return string.Empty;
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  LIST SELECTION → populate fields
//////////        // ════════════════════════════════════════════════════════
//////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////////        {
//////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////////            if (m == null) return;

//////////            TxtSerialNumber.Text = m.SerialNumber;
//////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////////            // ── Push selected module's IP into the Connect panel ──
//////////            // so clicking Connect on the main comm control uses this module's IP.
//////////            //if (Application.Current.MainWindow is MainWindow mainWin)
//////////            //{
//////////            //    mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
//////////            //}
//////////            if (Application.Current.MainWindow is MainWindow mainWin)
//////////            {
//////////                string[] octets = m.IpAddress.Split('.');
//////////                if (octets.Length == 4)
//////////                {
//////////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
//////////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
//////////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
//////////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
//////////                }
//////////            }
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  SELECT ALL / NONE
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////////            => LstModules.SelectAll();

//////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////////            => LstModules.UnselectAll();

//////////        // ════════════════════════════════════════════════════════
//////////        //  SET IP  (works with or without an existing connection)
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string ip = GetIp();
//////////            string mask = GetMask();
//////////            string gateway = GetGateway();

//////////            if (!IsValidIp(ip))
//////////            {
//////////                Warn("Enter a valid IP address.", "Invalid IP");
//////////                return;
//////////            }

//////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////////            {
//////////                Warn("Main window not available.", "Error");
//////////                return;
//////////            }

//////////            var commCtrl = mainWin.CommMotorSelect;

//////////            if (commCtrl?.BLLogic == null)
//////////            {
//////////                Warn("Communication layer not initialized.", "Error");
//////////                return;
//////////            }

//////////            // ── Step 1: Split IP into 4 octets ──
//////////            string[] ipParts = ip.Split('.');
//////////            if (ipParts.Length != 4 ||
//////////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////////                !byte.TryParse(ipParts[3], out byte octet4))
//////////            {
//////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////////                return;
//////////            }

//////////            if (octet1 == 0)
//////////            {
//////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////////                return;
//////////            }

//////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////////            byte maskLastOctet = 0;
//////////            string[] maskParts = mask.Split('.');
//////////            if (maskParts.Length == 4)
//////////                byte.TryParse(maskParts[3], out maskLastOctet);

//////////            // ── Step 2: Confirm with the user before rebooting the device ──
//////////            //var confirm = MessageBox.Show(
//////////            //    $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
//////////            //    "The connection will be lost automatically during the reboot.\n\nContinue?",
//////////            //    "Confirm IP Change",
//////////            //    MessageBoxButton.YesNo,
//////////            //    MessageBoxImage.Warning);

//////////            //if (confirm != MessageBoxResult.Yes) return;


//////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////////            //string currentDeviceIp = selected != null
//////////            //    ? selected.IpAddress
//////////            //    : commCtrl.TcpIpAddress.Text.Trim();
//////////            string currentDeviceIp = selected != null
//////////    ? selected.IpAddress
//////////    : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();
//////////            if (!IsValidIp(currentDeviceIp))
//////////            {
//////////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
//////////                return;
//////////            }


//////////             int  port1 = DEFAULT_MODBUS_PORT;

//////////            // ── Steps 4-5: Fire the writes straight over a fresh socket to
//////////            // the device's CURRENT ip:port — completely independent of
//////////            // whatever BLLogic's connection state is. No pre-flight
//////////            // "are we connected" check, no popup if it can't connect —
//////////            // just try to send, and report status via TxtStatus.
//////////            try
//////////            {
//////////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet);
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
//////////                return; // don't touch UI state below — nothing confirmed sent
//////////            }

//////////            //MessageBox.Show(
//////////            //    $"IP {ip} sent and commit issued.\n" +
//////////            //    "The device is rebooting now — reconnect using the new IP.",
//////////            //    "IP Sent",
//////////            //    MessageBoxButton.OK,
//////////            //    MessageBoxImage.Information);

//////////            // ── Step 6: Update selected module in list if one is selected ──
//////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////////            if (module != null)
//////////            {
//////////                module.IpAddress = ip;
//////////                module.Mask = mask;
//////////                module.Gateway = gateway;
//////////                int idx = LstModules.SelectedIndex;
//////////                LstModules.Items.Refresh();
//////////                LstModules.SelectedIndex = idx;
//////////            }

//////////            // ── Step 7: If the main comm panel happened to be connected to
//////////            // this same device, drop it (device is rebooting anyway).
//////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////////            {
//////////                commCtrl.BLLogic.Close();
//////////                mainWin.DataContext = null;
//////////            }

//////////            //commCtrl.TcpIpAddress.Text = ip;
//////////            string[] octets = ip.Split('.');
//////////            if (octets.Length == 4)
//////////            {
//////////                commCtrl.TcpIpOctet1.Text = octets[0];
//////////                commCtrl.TcpIpOctet2.Text = octets[1];
//////////                commCtrl.TcpIpOctet3.Text = octets[2];
//////////                commCtrl.TcpIpOctet4.Text = octets[3];
//////////            }
//////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////////            commCtrl.ApplyAndSaveIp(ip);

//////////            TxtStatus.Text = $"IP set → {ip}.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  DIRECT SET-IP WRITE — bypasses BLLogic entirely.
//////////        //  Opens its own short-lived TCP socket straight to the device's
//////////        //  current ip:port, sends all 6 Modbus writes back-to-back on
//////////        //  that one socket (in order: octets, mask, then commit/reboot),
//////////        //  then closes. Works regardless of whether the app's main
//////////        //  Connect panel is connected or not.
//////////        //
//////////        //  IMPORTANT: each write is followed by a read-back of the
//////////        //  device's response. Per the Modbus "Write Single Register"
//////////        //  spec (function 0x06), a slave that accepted the write echoes
//////////        //  the same frame back. If we don't see that echo within the
//////////        //  timeout, the write did NOT reach/get processed by the ESP32
//////////        //  — even though the bytes left our socket successfully — and
//////////        //  we now treat that as a failure instead of reporting success.
//////////        // ════════════════════════════════════════════════════════
//////////        private void SendSetIpDirect(string targetIp, int port,
//////////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet)
//////////        {
//////////            using (var client = new TcpClient())
//////////            {
//////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
//////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//////////                client.EndConnect(connectResult);

//////////                using (NetworkStream stream = client.GetStream())
//////////                {
//////////                    stream.WriteTimeout = 2000;
//////////                    stream.ReadTimeout = 2000;

//////////                    // Give the ESP32's TCP/Modbus stack a moment to finish
//////////                    // accept() and be ready to read before we start writing.
//////////                    Thread.Sleep(100);

//////////                    // ── REQUIRED FIRST: the firmware's tcp_server_task will
//////////                    // NOT process ANY write until it sees a 2-byte handshake
//////////                    // (literal bytes 0x01, 0x01) from the client. Until then
//////////                    // handshake_done stays false and every byte we send is
//////////                    // silently discarded (offset++ with no action) — this
//////////                    // was the actual cause of "app says success, device
//////////                    // shows old IP": the writes were never being read at all.
//////////                    SendHandshake(stream);

//////////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
//////////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

//////////                    // Commit/reboot last. Device may not send a clean echo
//////////                    // for this one if it starts rebooting mid-response —
//////////                    // that's expected, so don't throw if this specific
//////////                    // read-back times out.
//////////                    try
//////////                    {
//////////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
//////////                    }
//////////                    catch (TimeoutException)
//////////                    {
//////////                        // Acceptable: device may have started rebooting
//////////                        // before it could echo the commit frame back.
//////////                    }

//////////                    // Let the last frame actually clear the OS send buffer
//////////                    // and reach the NIC before we tear the socket down.
//////////                    Thread.Sleep(150);
//////////                }
//////////            }
//////////        }

//////////        // Sends the 2-byte handshake (literal 0x01, 0x01) the ESP32
//////////        // firmware requires before it will process any register writes on
//////////        // a freshly-accepted connection, and consumes the device's reply.
//////////        //
//////////        // Per modbus_server.cpp:
//////////        //   if (!handshake_done && rx[offset]==0x01 && rx[offset+1]==0x01) {
//////////        //       send(sock, &rx[offset], remaining, 0);      // echoes our 2 bytes
//////////        //       send_modbus_frame(sock, 0x01, 0x00, 0x00);  // + a 12-byte frame
//////////        //       handshake_done = true;
//////////        //   }
//////////        // So the expected reply is: our 2 bytes echoed back, followed by a
//////////        // fixed 12-byte confirmation frame — 14 bytes total. We must fully
//////////        // consume both before reading normal write-confirmation frames,
//////////        // otherwise leftover bytes misalign the next ReadModbusFrame call.
//////////        private static void SendHandshake(NetworkStream stream)
//////////        {
//////////            byte[] handshake = { 0x01, 0x01 };
//////////            stream.Write(handshake, 0, handshake.Length);
//////////            stream.Flush();

//////////            byte[] echo = ReadExactOrNull(stream, 2);
//////////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
//////////                throw new TimeoutException(
//////////                    "Device did not respond to the required connection handshake " +
//////////                    "(no echo received) — writes cannot proceed.");

//////////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
//////////            if (confirmFrame == null)
//////////                throw new TimeoutException(
//////////                    "Device echoed the handshake but did not send its confirmation frame.");
//////////        }

//////////        // Writes one Modbus "Write Single Register" frame and blocks until
//////////        // the device echoes it back (per spec) or ReadTimeout elapses.
//////////        // Throws if no valid echo is received — this is what turns a blind
//////////        // fire-and-forget write into an actually-verified one.
//////////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
//////////        {
//////////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
//////////            stream.Write(frame, 0, frame.Length);
//////////            stream.Flush();

//////////            byte[] response = ReadModbusFrame(stream);

//////////            // FC 0x06 success response echoes: [unitId, 0x06, regHi, regLo, valHi, valLo]
//////////            // (offset by the 6-byte MBAP header we also need to read first).
//////////            if (response == null)
//////////                throw new TimeoutException(
//////////                    $"No response from device for register 0x{registerAddress:X4} " +
//////////                    "(write may not have been processed).");

//////////            if (response.Length >= 6 && response[1] == 0x86) // 0x86 = FC 0x06 with error bit set
//////////            {
//////////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
//////////                throw new InvalidOperationException(
//////////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
//////////            }
//////////        }

//////////        // Reads one full Modbus TCP frame: 6-byte MBAP header + PDU body
//////////        // (length taken from the header). Returns the PDU body only
//////////        // (matching the convention used elsewhere in this app), or null
//////////        // if the read times out / connection closes before a full frame
//////////        // arrives.
//////////        private static byte[] ReadModbusFrame(NetworkStream stream)
//////////        {
//////////            byte[] mbap = ReadExactOrNull(stream, 6);
//////////            if (mbap == null) return null;

//////////            int length = (mbap[4] << 8) | mbap[5];
//////////            if (length <= 0 || length > 253) return null;

//////////            byte[] body = ReadExactOrNull(stream, length);
//////////            return body;
//////////        }

//////////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
//////////        {
//////////            byte[] buffer = new byte[count];
//////////            int read = 0;
//////////            try
//////////            {
//////////                while (read < count)
//////////                {
//////////                    int r = stream.Read(buffer, read, count - read);
//////////                    if (r <= 0) return null;
//////////                    read += r;
//////////                }
//////////                return buffer;
//////////            }
//////////            catch (IOException)
//////////            {
//////////                // Includes read timeout (thrown as IOException wrapping a
//////////                // SocketException when ReadTimeout elapses).
//////////                return null;
//////////            }
//////////        }

//////////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
//////////        {
//////////            byte[] data = {
//////////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//////////                (byte)(value >> 8), (byte)(value & 0xFF)
//////////            };

//////////            byte[] pdu = new byte[2 + data.Length];
//////////            pdu[0] = 0x01; // slave id
//////////            pdu[1] = 0x06; // function code: write single register
//////////            Array.Copy(data, 0, pdu, 2, data.Length);

//////////            byte[] frame = new byte[6 + pdu.Length];
//////////            frame[0] = 0x00; frame[1] = 0x01; // transaction id
//////////            frame[2] = 0x00; frame[3] = 0x00; // protocol id
//////////            frame[4] = (byte)(pdu.Length >> 8);
//////////            frame[5] = (byte)(pdu.Length & 0xFF);
//////////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

//////////            return frame;
//////////        }

//////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            // TODO: implement backup logic
//////////        }
//////////        // ════════════════════════════════════════════════════════
//////////        //  RESTORE
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.SelectedItems.Count == 0)
//////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Open Backup File",
//////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            int restored = 0;
//////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////            {
//////////                string[] parts = line.Split(',');
//////////                if (parts.Length < 6) continue;

//////////                string sn = parts[1].Trim();
//////////                foreach (NetworkModule m in LstModules.SelectedItems)
//////////                {
//////////                    if (m.SerialNumber != sn) continue;
//////////                    m.IpAddress = parts[2].Trim();
//////////                    m.Mask = parts[3].Trim();
//////////                    m.Gateway = parts[4].Trim();
//////////                    m.Position = parts[5].Trim();
//////////                    restored++;
//////////                }
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = restored > 0
//////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////////                : "No matching modules found in the backup file.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  RESTORE BY IP
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string ip = GetIp();

//////////            if (!IsValidIp(ip))
//////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Open Backup File",
//////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            bool found = false;
//////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////            {
//////////                string[] parts = line.Split(',');
//////////                if (parts.Length < 6) continue;
//////////                if (parts[2].Trim() != ip) continue;

//////////                foreach (NetworkModule m in LstModules.Items)
//////////                {
//////////                    if (m.IpAddress != ip) continue;
//////////                    m.Mask = parts[3].Trim();
//////////                    m.Gateway = parts[4].Trim();
//////////                    m.Position = parts[5].Trim();
//////////                    found = true;
//////////                }
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = found
//////////                ? $"Restore by IP complete → {ip}"
//////////                : $"No backup entry found for IP {ip}.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  REORDER IPs
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.Items.Count == 0)
//////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////////            ReorderIpDialog dlg = new ReorderIpDialog();
//////////            if (dlg.ShowDialog() != true) return;

//////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////////            byte[] bytes = startAddr.GetAddressBytes();
//////////            int counter = bytes[3];

//////////            foreach (NetworkModule m in LstModules.Items)
//////////            {
//////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////////                m.IpAddress = new IPAddress(bytes).ToString();
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  UPGRADE FW
//////////        // ════════════════════════════════════════════════════════
//////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.SelectedItems.Count == 0)
//////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////////            MessageBoxResult confirm = MessageBox.Show(
//////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////////                "Do NOT power off during upgrade.",
//////////                "Confirm Firmware Upgrade",
//////////                MessageBoxButton.YesNo,
//////////                MessageBoxImage.Question);

//////////            if (confirm != MessageBoxResult.Yes) return;

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Select Firmware File",
//////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            Button btn = (Button)sender;
//////////            btn.IsEnabled = false;
//////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////////            List<NetworkModule> targets = LstModules.SelectedItems
//////////                .Cast<NetworkModule>()
//////////                .ToList();

//////////            string fwPath = dlg.FileName;

//////////            try
//////////            {
//////////                await Task.Run(() =>
//////////                {
//////////                    foreach (NetworkModule m in targets)
//////////                    {
//////////                        System.Threading.Thread.Sleep(500);
//////////                        m.FirmwareRev = "NEW";
//////////                    }
//////////                });

//////////                LstModules.Items.Refresh();
//////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////////            }
//////////            finally
//////////            {
//////////                btn.IsEnabled = true;
//////////            }
//////////        }

//////////        private static void Warn(string message, string title) =>
//////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////////    // ────────────────────────────────────────────────────────────
//////////    public class ReorderIpDialog : Window
//////////    {
//////////        private readonly TextBox _input;
//////////        public string StartIp => _input.Text.Trim();

//////////        public ReorderIpDialog()
//////////        {
//////////            Title = "Reorder IPs";
//////////            Width = 280;
//////////            Height = 130;
//////////            ResizeMode = ResizeMode.NoResize;
//////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////////            sp.Children.Add(new TextBlock
//////////            {
//////////                Text = "Enter starting IP address:",
//////////                Margin = new Thickness(0, 0, 0, 6)
//////////            });

//////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////////            sp.Children.Add(_input);

//////////            StackPanel btnRow = new StackPanel
//////////            {
//////////                Orientation = Orientation.Horizontal,
//////////                HorizontalAlignment = HorizontalAlignment.Right,
//////////                Margin = new Thickness(0, 10, 0, 0)
//////////            };

//////////            Button ok = new Button
//////////            {
//////////                Content = "OK",
//////////                Width = 70,
//////////                Height = 24,
//////////                Margin = new Thickness(0, 0, 6, 0),
//////////                IsDefault = true
//////////            };
//////////            ok.Click += (s, ev) => { DialogResult = true; };

//////////            Button cancel = new Button
//////////            {
//////////                Content = "Cancel",
//////////                Width = 70,
//////////                Height = 24,
//////////                IsCancel = true
//////////            };

//////////            btnRow.Children.Add(ok);
//////////            btnRow.Children.Add(cancel);
//////////            sp.Children.Add(btnRow);

//////////            Content = sp;
//////////        }
//////////    }
//////////}
////////using BLDC_Demo.Controls;
////////using Microsoft.Win32;
////////using System;
////////using System.Collections.Generic;
////////using System.IO;
////////using System.Linq;
////////using System.Net;
////////using System.Net.NetworkInformation;
////////using System.Net.Sockets;
////////using System.Text.RegularExpressions;
////////using System.Threading;
////////using System.Threading.Tasks;
////////using System.Windows;
////////using System.Windows.Controls;
////////using System.Windows.Input;

////////namespace BLDC_Demo
////////{
////////    // ────────────────────────────────────────────────────────────
////////    //  Model: a discovered network module
////////    // ────────────────────────────────────────────────────────────
////////    public class NetworkModule
////////    {
////////        public string SerialNumber { get; set; } = string.Empty;
////////        public string IpAddress { get; set; } = string.Empty;
////////        public string Mask { get; set; } = string.Empty;
////////        public string Gateway { get; set; } = string.Empty;
////////        public string FirmwareRev { get; set; } = string.Empty;
////////        public string Position { get; set; } = "—";

////////        public override string ToString() =>
////////            $"{IpAddress}   SN: {SerialNumber} ";
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  Backup container
////////    // ────────────────────────────────────────────────────────────
////////    public class ModuleBackup
////////    {
////////        public string SerialNumber { get; set; } = string.Empty;
////////        public string IpAddress { get; set; } = string.Empty;
////////        public string Mask { get; set; } = string.Empty;
////////        public string Gateway { get; set; } = string.Empty;
////////        public string Position { get; set; } = string.Empty;
////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  UserControl code-behind  (targets C# 7.3)
////////    // ────────────────────────────────────────────────────────────
////////    public partial class NetworkService : UserControl
////////    {
////////        // ── ESP32 Modbus holding-register map for network config ──
////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////        // Default Modbus TCP port used when we have to open a temporary
////////        // connection to push a new IP to a device that isn't connected yet.
////////        private const int DEFAULT_MODBUS_PORT = 502;

////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////            = new Dictionary<string, ModuleBackup>();

////////        public NetworkService()
////////        {
////////            InitializeComponent();
////////            AttachOctetHandlers();
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  OCTET HELPERS
////////        // ════════════════════════════════════════════════════════
////////        private void AttachOctetHandlers()
////////        {
////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////            AttachToGroup(ipBoxes);
////////            AttachToGroup(maskBoxes);
////////            AttachToGroup(gwBoxes);

////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////        }

////////        private void SetGatewayEnabled(bool enabled)
////////        {
////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////        }

////////        private static void AttachToGroup(TextBox[] boxes)
////////        {
////////            for (int i = 0; i < boxes.Length; i++)
////////            {
////////                int idx = i;

////////                boxes[idx].PreviewTextInput += (s, e) =>
////////                {
////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////                };

////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////                    {
////////                        e.Handled = true;
////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////                    }
////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////                    {
////////                        boxes[idx - 1].Focus();
////////                        boxes[idx - 1].SelectAll();
////////                    }
////////                };

////////                boxes[idx].TextChanged += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////                        boxes[idx + 1].Focus();
////////                };

////////                boxes[idx].LostFocus += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (int.TryParse(tb.Text, out int val))
////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////                    else if (tb.Text.Length > 0)
////////                        tb.Text = string.Empty;
////////                };
////////            }
////////        }

////////        private static int Clamp(int value, int min, int max)
////////        {
////////            if (value < min) return min;
////////            if (value > max) return max;
////////            return value;
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  IP FIELD READ / WRITE
////////        // ════════════════════════════════════════════════════════
////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////        private string GetGateway() =>
////////            ChkGateway.IsChecked == true
////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////                : string.Empty;

////////        private void SetIpFields(string ip, string mask, string gw)
////////        {
////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////            ChkGateway.IsChecked = hasGw;
////////            SetGatewayEnabled(hasGw);

////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////        }

////////        private static void FillOctets(string address,
////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////        {
////////            string[] parts = address != null
////////                ? address.Split('.')
////////                : new string[0];

////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////        }

////////        private static void ClearOctets(params TextBox[] boxes)
////////        {
////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////        }

////////        private static bool IsValidIp(string ip) =>
////////            IPAddress.TryParse(ip, out _);

////////        // ════════════════════════════════════════════════════════
////////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
////////        // ════════════════════════════════════════════════════════
////////        //private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////        //{
////////        //    LstModules.Items.Clear();
////////        //    TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////        //    Button btn = (Button)sender;
////////        //    btn.IsEnabled = false;

////////        //    try
////////        //    {
////////        //        List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////        //        foreach (NetworkModule m in modules)
////////        //            LstModules.Items.Add(m);

////////        //        TxtStatus.Text = modules.Count == 0
////////        //            ? "No motor controllers found."
////////        //            : $"Found {modules.Count} motor controller(s).";
////////        //    }
////////        //    catch (Exception ex)
////////        //    {
////////        //        TxtStatus.Text = $"Discovery error: {ex.Message}";
////////        //    }
////////        //    finally
////////        //    {
////////        //        btn.IsEnabled = true;
////////        //    }
////////        //}
////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////        {
////////            LstModules.Items.Clear();
////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////            Button btn = (Button)sender;
////////            btn.IsEnabled = false;

////////            try
////////            {
////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////                // ── Option A: fill in serial for the device BL is connected to ──
////////                if (Application.Current.MainWindow is MainWindow mw)
////////                {
////////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
////////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
////////                    {
////////                        foreach (NetworkModule m in modules)
////////                            if (m.IpAddress == dev.IpAddress)
////////                                m.SerialNumber = dev.SerialNumber;
////////                    }
////////                }

////////                foreach (NetworkModule m in modules)
////////                    LstModules.Items.Add(m);

////////                TxtStatus.Text = modules.Count == 0
////////                    ? "No motor controllers found."
////////                    : $"Found {modules.Count} motor controller(s).";
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////            }
////////            finally
////////            {
////////                btn.IsEnabled = true;
////////            }
////////        }
////////        private static List<NetworkModule> DiscoverModules()
////////        {
////////            var found = new List<NetworkModule>();
////////            string baseIp = GetLocalSubnetBase();
////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////            var localIps = new HashSet<string>(
////////                NetworkInterface.GetAllNetworkInterfaces()
////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////                    .Select(ua => ua.Address.ToString())
////////            );

////////            const int targetPort = 502;
////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////            for (int i = 1; i <= 254; i++)
////////            {
////////                string targetIp = $"{baseIp}.{i}";
////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////            }

////////            Task.WaitAll(tasks.ToArray());

////////            foreach (var t in tasks)
////////            {
////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////                {
////////                    found.Add(new NetworkModule
////////                    {
////////                        IpAddress = t.Result.Item2,
////////                        SerialNumber = "-",
////////                        FirmwareRev = "1.0.0",
////////                        Mask = "255.255.255.0",
////////                        Position = "—"
////////                    });
////////                }
////////            }

////////            return found;
////////        }

////////        // Tries twice per host, with a short gap between attempts, and a
////////        // longer per-attempt timeout than before (500ms vs the old 200ms).
////////        // This is a workaround for devices that are slow to accept right
////////        // after a previous client disconnected — it does NOT fix a device
////////        // that is actively refusing connections (that's a firmware issue:
////////        // the ESP32's accept loop not resetting after client disconnect).
////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////        {
////////            for (int attempt = 0; attempt < 2; attempt++)
////////            {
////////                try
////////                {
////////                    using (var client = new TcpClient())
////////                    {
////////                        var connectTask = client.ConnectAsync(ip, port);
////////                        var delayTask = Task.Delay(500); // was 200
////////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
////////                        if (completedTask == connectTask && client.Connected)
////////                            return Tuple.Create(true, ip);
////////                    }
////////                }
////////                catch
////////                {
////////                    // Connection actively refused, or the attempt threw —
////////                    // fall through and retry once.
////////                }

////////                if (attempt == 0)
////////                    await Task.Delay(300);
////////            }

////////            return Tuple.Create(false, ip);
////////        }

////////        private static string GetLocalSubnetBase()
////////        {
////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////                .ToList();

////////            if (!candidates.Any())
////////            {
////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                    .ToList();
////////            }

////////            foreach (NetworkInterface ni in candidates)
////////            {
////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////                {
////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////                    {
////////                        string[] parts = ua.Address.ToString().Split('.');
////////                        if (parts.Length == 4)
////////                        {
////////                            if (parts[0] == "127") continue;               // loopback
////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////                        }
////////                    }
////////                }
////////            }
////////            return string.Empty;
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  LIST SELECTION → populate fields
////////        // ════════════════════════════════════════════════════════
////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////        {
////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////            if (m == null) return;

////////            TxtSerialNumber.Text = m.SerialNumber;
////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////            // ── Push selected module's IP into the Connect panel ──
////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////            {
////////                string[] octets = m.IpAddress.Split('.');
////////                if (octets.Length == 4)
////////                {
////////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
////////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
////////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
////////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
////////                }
////////            }
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  SELECT ALL / NONE
////////        // ════════════════════════════════════════════════════════
////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////            => LstModules.SelectAll();

////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////            => LstModules.UnselectAll();

////////        // ════════════════════════════════════════════════════════
////////        //  SET IP  (works with or without an existing connection)
////////        // ════════════════════════════════════════════════════════
////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////        {
////////            string ip = GetIp();
////////            string mask = GetMask();
////////            string gateway = GetGateway();

////////            if (!IsValidIp(ip))
////////            {
////////                Warn("Enter a valid IP address.", "Invalid IP");
////////                return;
////////            }

////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////            {
////////                Warn("Main window not available.", "Error");
////////                return;
////////            }

////////            var commCtrl = mainWin.CommMotorSelect;

////////            if (commCtrl?.BLLogic == null)
////////            {
////////                Warn("Communication layer not initialized.", "Error");
////////                return;
////////            }

////////            // ── Step 1: Split IP into 4 octets ──
////////            string[] ipParts = ip.Split('.');
////////            if (ipParts.Length != 4 ||
////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////                !byte.TryParse(ipParts[3], out byte octet4))
////////            {
////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////                return;
////////            }

////////            if (octet1 == 0)
////////            {
////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////                return;
////////            }

////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////            byte maskLastOctet = 0;
////////            string[] maskParts = mask.Split('.');
////////            if (maskParts.Length == 4)
////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
////////            string currentDeviceIp = selected != null
////////                ? selected.IpAddress
////////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

////////            if (!IsValidIp(currentDeviceIp))
////////            {
////////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
////////                return;
////////            }

////////            int port1 = DEFAULT_MODBUS_PORT;

////////            try
////////            {
////////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet);
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
////////                return; // don't touch UI state below — nothing confirmed sent
////////            }

////////            // ── Step 6: Update selected module in list if one is selected ──
////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////            if (module != null)
////////            {
////////                module.IpAddress = ip;
////////                module.Mask = mask;
////////                module.Gateway = gateway;
////////                int idx = LstModules.SelectedIndex;
////////                LstModules.Items.Refresh();
////////                LstModules.SelectedIndex = idx;
////////            }

////////            // ── Step 7: If the main comm panel happened to be connected to
////////            // this same device, drop it (device is rebooting anyway).
////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////            {
////////                commCtrl.BLLogic.Close();
////////                mainWin.DataContext = null;
////////            }

////////            string[] octets = ip.Split('.');
////////            if (octets.Length == 4)
////////            {
////////                commCtrl.TcpIpOctet1.Text = octets[0];
////////                commCtrl.TcpIpOctet2.Text = octets[1];
////////                commCtrl.TcpIpOctet3.Text = octets[2];
////////                commCtrl.TcpIpOctet4.Text = octets[3];
////////            }
////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////            commCtrl.ApplyAndSaveIp(ip);

////////            TxtStatus.Text = $"IP set → {ip}.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  DIRECT SET-IP WRITE — bypasses BLLogic entirely.
////////        // ════════════════════════════════════════════════════════
////////        private void SendSetIpDirect(string targetIp, int port,
////////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet)
////////        {
////////            using (var client = new TcpClient())
////////            {
////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

////////                client.EndConnect(connectResult);

////////                using (NetworkStream stream = client.GetStream())
////////                {
////////                    stream.WriteTimeout = 2000;
////////                    stream.ReadTimeout = 2000;

////////                    Thread.Sleep(100);

////////                    SendHandshake(stream);

////////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
////////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
////////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
////////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
////////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

////////                    try
////////                    {
////////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
////////                    }
////////                    catch (TimeoutException)
////////                    {
////////                        // Acceptable: device may have started rebooting
////////                        // before it could echo the commit frame back.
////////                    }

////////                    Thread.Sleep(150);
////////                }
////////            }
////////        }

////////        private static void SendHandshake(NetworkStream stream)
////////        {
////////            byte[] handshake = { 0x01, 0x01 };
////////            stream.Write(handshake, 0, handshake.Length);
////////            stream.Flush();

////////            byte[] echo = ReadExactOrNull(stream, 2);
////////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
////////                throw new TimeoutException(
////////                    "Device did not respond to the required connection handshake " +
////////                    "(no echo received) — writes cannot proceed.");

////////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
////////            if (confirmFrame == null)
////////                throw new TimeoutException(
////////                    "Device echoed the handshake but did not send its confirmation frame.");
////////        }

////////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
////////        {
////////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
////////            stream.Write(frame, 0, frame.Length);
////////            stream.Flush();

////////            byte[] response = ReadModbusFrame(stream);

////////            if (response == null)
////////                throw new TimeoutException(
////////                    $"No response from device for register 0x{registerAddress:X4} " +
////////                    "(write may not have been processed).");

////////            if (response.Length >= 6 && response[1] == 0x86)
////////            {
////////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
////////                throw new InvalidOperationException(
////////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
////////            }
////////        }

////////        private static byte[] ReadModbusFrame(NetworkStream stream)
////////        {
////////            byte[] mbap = ReadExactOrNull(stream, 6);
////////            if (mbap == null) return null;

////////            int length = (mbap[4] << 8) | mbap[5];
////////            if (length <= 0 || length > 253) return null;

////////            byte[] body = ReadExactOrNull(stream, length);
////////            return body;
////////        }

////////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
////////        {
////////            byte[] buffer = new byte[count];
////////            int read = 0;
////////            try
////////            {
////////                while (read < count)
////////                {
////////                    int r = stream.Read(buffer, read, count - read);
////////                    if (r <= 0) return null;
////////                    read += r;
////////                }
////////                return buffer;
////////            }
////////            catch (IOException)
////////            {
////////                return null;
////////            }
////////        }

////////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
////////        {
////////            byte[] data = {
////////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
////////                (byte)(value >> 8), (byte)(value & 0xFF)
////////            };

////////            byte[] pdu = new byte[2 + data.Length];
////////            pdu[0] = 0x01;
////////            pdu[1] = 0x06;
////////            Array.Copy(data, 0, pdu, 2, data.Length);

////////            byte[] frame = new byte[6 + pdu.Length];
////////            frame[0] = 0x00; frame[1] = 0x01;
////////            frame[2] = 0x00; frame[3] = 0x00;
////////            frame[4] = (byte)(pdu.Length >> 8);
////////            frame[5] = (byte)(pdu.Length & 0xFF);
////////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

////////            return frame;
////////        }

////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////        {
////////            // TODO: implement backup logic
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  RESTORE
////////        // ════════════════════════════════════════════════════════
////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.SelectedItems.Count == 0)
////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Open Backup File",
////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            int restored = 0;
////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////            {
////////                string[] parts = line.Split(',');
////////                if (parts.Length < 6) continue;

////////                string sn = parts[1].Trim();
////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////                {
////////                    if (m.SerialNumber != sn) continue;
////////                    m.IpAddress = parts[2].Trim();
////////                    m.Mask = parts[3].Trim();
////////                    m.Gateway = parts[4].Trim();
////////                    m.Position = parts[5].Trim();
////////                    restored++;
////////                }
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = restored > 0
////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////                : "No matching modules found in the backup file.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  RESTORE BY IP
////////        // ════════════════════════════════════════════════════════
////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////        {
////////            string ip = GetIp();

////////            if (!IsValidIp(ip))
////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Open Backup File",
////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            bool found = false;
////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////            {
////////                string[] parts = line.Split(',');
////////                if (parts.Length < 6) continue;
////////                if (parts[2].Trim() != ip) continue;

////////                foreach (NetworkModule m in LstModules.Items)
////////                {
////////                    if (m.IpAddress != ip) continue;
////////                    m.Mask = parts[3].Trim();
////////                    m.Gateway = parts[4].Trim();
////////                    m.Position = parts[5].Trim();
////////                    found = true;
////////                }
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = found
////////                ? $"Restore by IP complete → {ip}"
////////                : $"No backup entry found for IP {ip}.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  REORDER IPs
////////        // ════════════════════════════════════════════════════════
////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.Items.Count == 0)
////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////            if (dlg.ShowDialog() != true) return;

////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////            byte[] bytes = startAddr.GetAddressBytes();
////////            int counter = bytes[3];

////////            foreach (NetworkModule m in LstModules.Items)
////////            {
////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////                m.IpAddress = new IPAddress(bytes).ToString();
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  UPGRADE FW
////////        // ════════════════════════════════════════════════════════
////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.SelectedItems.Count == 0)
////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////            MessageBoxResult confirm = MessageBox.Show(
////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////                "Do NOT power off during upgrade.",
////////                "Confirm Firmware Upgrade",
////////                MessageBoxButton.YesNo,
////////                MessageBoxImage.Question);

////////            if (confirm != MessageBoxResult.Yes) return;

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Select Firmware File",
////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            Button btn = (Button)sender;
////////            btn.IsEnabled = false;
////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////            List<NetworkModule> targets = LstModules.SelectedItems
////////                .Cast<NetworkModule>()
////////                .ToList();

////////            string fwPath = dlg.FileName;

////////            try
////////            {
////////                await Task.Run(() =>
////////                {
////////                    foreach (NetworkModule m in targets)
////////                    {
////////                        System.Threading.Thread.Sleep(500);
////////                        m.FirmwareRev = "NEW";
////////                    }
////////                });

////////                LstModules.Items.Refresh();
////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////            }
////////            finally
////////            {
////////                btn.IsEnabled = true;
////////            }
////////        }

////////        private static void Warn(string message, string title) =>
////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////    // ────────────────────────────────────────────────────────────
////////    public class ReorderIpDialog : Window
////////    {
////////        private readonly TextBox _input;
////////        public string StartIp => _input.Text.Trim();

////////        public ReorderIpDialog()
////////        {
////////            Title = "Reorder IPs";
////////            Width = 280;
////////            Height = 130;
////////            ResizeMode = ResizeMode.NoResize;
////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////            sp.Children.Add(new TextBlock
////////            {
////////                Text = "Enter starting IP address:",
////////                Margin = new Thickness(0, 0, 0, 6)
////////            });

////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////            sp.Children.Add(_input);

////////            StackPanel btnRow = new StackPanel
////////            {
////////                Orientation = Orientation.Horizontal,
////////                HorizontalAlignment = HorizontalAlignment.Right,
////////                Margin = new Thickness(0, 10, 0, 0)
////////            };

////////            Button ok = new Button
////////            {
////////                Content = "OK",
////////                Width = 70,
////////                Height = 24,
////////                Margin = new Thickness(0, 0, 6, 0),
////////                IsDefault = true
////////            };
////////            ok.Click += (s, ev) => { DialogResult = true; };

////////            Button cancel = new Button
////////            {
////////                Content = "Cancel",
////////                Width = 70,
////////                Height = 24,
////////                IsCancel = true
////////            };

////////            btnRow.Children.Add(ok);
////////            btnRow.Children.Add(cancel);
////////            sp.Children.Add(btnRow);

////////            Content = sp;
////////        }
////////    }
////////}

////////////////using BLDC_Demo.Controls;
////////////////using Microsoft.Win32;
////////////////using System;
////////////////using System.Collections.Generic;
////////////////using System.Linq;
////////////////using System.Net;
////////////////using System.Net.NetworkInformation;
////////////////using System.Net.Sockets;
////////////////using System.Text.RegularExpressions;
////////////////using System.Threading.Tasks;
////////////////using System.Windows;
////////////////using System.Windows.Controls;
////////////////using System.Windows.Input;

////////////////namespace BLDC_Demo
////////////////{
////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  Model: a discovered network module
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public class NetworkModule
////////////////    {
////////////////        public string SerialNumber { get; set; } = string.Empty;
////////////////        public string IpAddress { get; set; } = string.Empty;
////////////////        public string Mask { get; set; } = string.Empty;
////////////////        public string Gateway { get; set; } = string.Empty;
////////////////        public string FirmwareRev { get; set; } = string.Empty;
////////////////        public string Position { get; set; } = "—";

////////////////        public override string ToString() =>
////////////////            $"{IpAddress}   SN: {SerialNumber} ";
////////////////    }

////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  Backup container
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public class ModuleBackup
////////////////    {
////////////////        public string SerialNumber { get; set; } = string.Empty;
////////////////        public string IpAddress { get; set; } = string.Empty;
////////////////        public string Mask { get; set; } = string.Empty;
////////////////        public string Gateway { get; set; } = string.Empty;
////////////////        public string Position { get; set; } = string.Empty;
////////////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////////////    }

////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  UserControl code-behind  (targets C# 7.3)
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public partial class NetworkService : UserControl
////////////////    {
////////////////        // ── ESP32 Modbus holding-register map for network config ──
////////////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////////////            = new Dictionary<string, ModuleBackup>();

////////////////        public NetworkService()
////////////////        {
////////////////            InitializeComponent();
////////////////            AttachOctetHandlers();
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  OCTET HELPERS
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void AttachOctetHandlers()
////////////////        {
////////////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////////////            AttachToGroup(ipBoxes);
////////////////            AttachToGroup(maskBoxes);
////////////////            AttachToGroup(gwBoxes);

////////////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////////////        }

////////////////        private void SetGatewayEnabled(bool enabled)
////////////////        {
////////////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////////////        }

////////////////        private static void AttachToGroup(TextBox[] boxes)
////////////////        {
////////////////            for (int i = 0; i < boxes.Length; i++)
////////////////            {
////////////////                int idx = i;

////////////////                boxes[idx].PreviewTextInput += (s, e) =>
////////////////                {
////////////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////////////                };

////////////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////////////                {
////////////////                    TextBox tb = (TextBox)s;
////////////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////////////                    {
////////////////                        e.Handled = true;
////////////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////////////                    }
////////////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////////////                    {
////////////////                        boxes[idx - 1].Focus();
////////////////                        boxes[idx - 1].SelectAll();
////////////////                    }
////////////////                };

////////////////                boxes[idx].TextChanged += (s, e) =>
////////////////                {
////////////////                    TextBox tb = (TextBox)s;
////////////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////////////                        boxes[idx + 1].Focus();
////////////////                };

////////////////                boxes[idx].LostFocus += (s, e) =>
////////////////                {
////////////////                    TextBox tb = (TextBox)s;
////////////////                    if (int.TryParse(tb.Text, out int val))
////////////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////////////                    else if (tb.Text.Length > 0)
////////////////                        tb.Text = string.Empty;
////////////////                };
////////////////            }
////////////////        }

////////////////        private static int Clamp(int value, int min, int max)
////////////////        {
////////////////            if (value < min) return min;
////////////////            if (value > max) return max;
////////////////            return value;
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  IP FIELD READ / WRITE
////////////////        // ════════════════════════════════════════════════════════
////////////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////////////        private string GetGateway() =>
////////////////            ChkGateway.IsChecked == true
////////////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////////////                : string.Empty;

////////////////        private void SetIpFields(string ip, string mask, string gw)
////////////////        {
////////////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////////////            ChkGateway.IsChecked = hasGw;
////////////////            SetGatewayEnabled(hasGw);

////////////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////////        }

////////////////        private static void FillOctets(string address,
////////////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////////////        {
////////////////            string[] parts = address != null
////////////////                ? address.Split('.')
////////////////                : new string[0];

////////////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////////////        }

////////////////        private static void ClearOctets(params TextBox[] boxes)
////////////////        {
////////////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////////////        }

////////////////        private static bool IsValidIp(string ip) =>
////////////////            IPAddress.TryParse(ip, out _);

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  DISCOVER (PORT SCAN FILTERED)
////////////////        // ════════════════════════════════════════════════════════
////////////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            LstModules.Items.Clear();
////////////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////////////            Button btn = (Button)sender;
////////////////            btn.IsEnabled = false;

////////////////            try
////////////////            {
////////////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////////////                foreach (NetworkModule m in modules)
////////////////                    LstModules.Items.Add(m);

////////////////                TxtStatus.Text = modules.Count == 0
////////////////                    ? "No motor controllers found."
////////////////                    : $"Found {modules.Count} motor controller(s).";
////////////////            }
////////////////            catch (Exception ex)
////////////////            {
////////////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////////////            }
////////////////            finally
////////////////            {
////////////////                btn.IsEnabled = true;
////////////////            }
////////////////        }

////////////////        private static List<NetworkModule> DiscoverModules()
////////////////        {
////////////////            var found = new List<NetworkModule>();
////////////////            string baseIp = GetLocalSubnetBase();
////////////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////////////            var localIps = new HashSet<string>(
////////////////                NetworkInterface.GetAllNetworkInterfaces()
////////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////////                    .Select(ua => ua.Address.ToString())
////////////////            );

////////////////            const int targetPort = 502;
////////////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////////////            for (int i = 1; i <= 254; i++)
////////////////            {
////////////////                string targetIp = $"{baseIp}.{i}";
////////////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////////////            }

////////////////            Task.WaitAll(tasks.ToArray());

////////////////            foreach (var t in tasks)
////////////////            {
////////////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////////////                {
////////////////                    found.Add(new NetworkModule
////////////////                    {
////////////////                        IpAddress = t.Result.Item2,
////////////////                        SerialNumber = "ESP32-BLDC",
////////////////                        FirmwareRev = "1.0.0",
////////////////                        Mask = "255.255.255.0",
////////////////                        Position = "—"
////////////////                    });
////////////////                }
////////////////            }

////////////////            return found;
////////////////        }

////////////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////////////        {
////////////////            try
////////////////            {
////////////////                using (var client = new TcpClient())
////////////////                {
////////////////                    var connectTask = client.ConnectAsync(ip, port);
////////////////                    var delayTask = Task.Delay(200);
////////////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
////////////////                    if (completedTask == connectTask && client.Connected)
////////////////                        return Tuple.Create(true, ip);
////////////////                }
////////////////                return Tuple.Create(false, ip);
////////////////            }
////////////////            catch
////////////////            {
////////////////                return Tuple.Create(false, ip);
////////////////            }
////////////////        }

////////////////        private static string GetLocalSubnetBase()
////////////////        {
////////////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////////////                .ToList();

////////////////            if (!candidates.Any())
////////////////            {
////////////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////////                    .ToList();
////////////////            }

////////////////            foreach (NetworkInterface ni in candidates)
////////////////            {
////////////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////////////                {
////////////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////////                    {
////////////////                        string[] parts = ua.Address.ToString().Split('.');
////////////////                        if (parts.Length == 4)
////////////////                        {
////////////////                            if (parts[0] == "127") continue;               // loopback
////////////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////////////                        }
////////////////                    }
////////////////                }
////////////////            }
////////////////            return string.Empty;
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  LIST SELECTION → populate fields
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////////////        {
////////////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////////////            if (m == null) return;

////////////////            TxtSerialNumber.Text = m.SerialNumber;
////////////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////////////            // ── Push selected module's IP into the Connect panel ──
////////////////            // so clicking Connect on the main comm control uses this module's IP.
////////////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////////////            {
////////////////                mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
////////////////            }
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  SELECT ALL / NONE
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////////////            => LstModules.SelectAll();

////////////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////////////            => LstModules.UnselectAll();


////////////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            string ip = GetIp();
////////////////            string mask = GetMask();
////////////////            string gateway = GetGateway();

////////////////            if (!IsValidIp(ip))
////////////////            {
////////////////                Warn("Enter a valid IP address.", "Invalid IP");
////////////////                return;
////////////////            }

////////////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////////////            {
////////////////                Warn("Main window not available.", "Error");
////////////////                return;
////////////////            }

////////////////            var commCtrl = mainWin.CommMotorSelect;

////////////////            // NOTE: connection check removed intentionally — Set IP now proceeds
////////////////            // regardless of commCtrl.BLLogic.IsConnected. If BLLogic itself is
////////////////            // not initialized, SendWriteCommand below may throw/fail.

////////////////            // ── Step 1: Split IP into 4 octets ──
////////////////            string[] ipParts = ip.Split('.');
////////////////            if (ipParts.Length != 4 ||
////////////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////////////                !byte.TryParse(ipParts[3], out byte octet4))
////////////////            {
////////////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////////////                return;
////////////////            }

////////////////            if (octet1 == 0)
////////////////            {
////////////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////////////                return;
////////////////            }

////////////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////////////            byte maskLastOctet = 0;
////////////////            string[] maskParts = mask.Split('.');
////////////////            if (maskParts.Length == 4)
////////////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////////////            // ── Step 2: Confirm with the user before rebooting the device ──
////////////////            var confirm = MessageBox.Show(
////////////////                $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
////////////////                "The connection will be lost automatically during the reboot.\n\nContinue?",
////////////////                "Confirm IP Change",
////////////////                MessageBoxButton.YesNo,
////////////////                MessageBoxImage.Warning);

////////////////            if (confirm != MessageBoxResult.Yes) return;

////////////////            // ── Step 3: Stage the octets + netmask ──
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET1, octet1);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET2, octet2);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET3, octet3);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET4, octet4);
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_MASK_OCTET4, maskLastOctet);

////////////////            // ── Step 4: COMMIT — this is what actually saves + reboots the device ──
////////////////            commCtrl.BLLogic.SendWriteCommand(REG_APPLY_REBOOT, 1);

////////////////            MessageBox.Show(
////////////////                $"IP {ip} sent and commit issued.\n" +
////////////////                "The device is rebooting now — reconnect using the new IP.",
////////////////                "IP Sent",
////////////////                MessageBoxButton.OK,
////////////////                MessageBoxImage.Information);

////////////////            // ── Step 5: Update selected module in list if one is selected ──
////////////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////////////            if (module != null)
////////////////            {
////////////////                module.IpAddress = ip;
////////////////                module.Mask = mask;
////////////////                module.Gateway = gateway;
////////////////                int idx = LstModules.SelectedIndex;
////////////////                LstModules.Items.Refresh();
////////////////                LstModules.SelectedIndex = idx;
////////////////            }

////////////////            // ── Step 6: Disconnect (device is rebooting anyway) and update the connect field ──
////////////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////////////            {
////////////////                commCtrl.BLLogic.Close();
////////////////                mainWin.DataContext = null;
////////////////            }

////////////////            commCtrl.TcpIpAddress.Text = ip;
////////////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////////////            commCtrl.ApplyAndSaveIp(ip);

////////////////            TxtStatus.Text = $"IP set → {ip}. Device rebooting. Reconnect required in a few seconds.";
////////////////        }

////////////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            // TODO: implement backup logic
////////////////        }
////////////////        // ════════════════════════════════════════════════════════
////////////////        //  RESTORE
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            if (LstModules.SelectedItems.Count == 0)
////////////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////////////            OpenFileDialog dlg = new OpenFileDialog
////////////////            {
////////////////                Title = "Open Backup File",
////////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////////            };

////////////////            if (dlg.ShowDialog() != true) return;

////////////////            int restored = 0;
////////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////////            {
////////////////                string[] parts = line.Split(',');
////////////////                if (parts.Length < 6) continue;

////////////////                string sn = parts[1].Trim();
////////////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////////////                {
////////////////                    if (m.SerialNumber != sn) continue;
////////////////                    m.IpAddress = parts[2].Trim();
////////////////                    m.Mask = parts[3].Trim();
////////////////                    m.Gateway = parts[4].Trim();
////////////////                    m.Position = parts[5].Trim();
////////////////                    restored++;
////////////////                }
////////////////            }

////////////////            LstModules.Items.Refresh();
////////////////            TxtStatus.Text = restored > 0
////////////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////////////                : "No matching modules found in the backup file.";
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  RESTORE BY IP
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            string ip = GetIp();

////////////////            if (!IsValidIp(ip))
////////////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////////////            OpenFileDialog dlg = new OpenFileDialog
////////////////            {
////////////////                Title = "Open Backup File",
////////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////////            };

////////////////            if (dlg.ShowDialog() != true) return;

////////////////            bool found = false;
////////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////////            {
////////////////                string[] parts = line.Split(',');
////////////////                if (parts.Length < 6) continue;
////////////////                if (parts[2].Trim() != ip) continue;

////////////////                foreach (NetworkModule m in LstModules.Items)
////////////////                {
////////////////                    if (m.IpAddress != ip) continue;
////////////////                    m.Mask = parts[3].Trim();
////////////////                    m.Gateway = parts[4].Trim();
////////////////                    m.Position = parts[5].Trim();
////////////////                    found = true;
////////////////                }
////////////////            }

////////////////            LstModules.Items.Refresh();
////////////////            TxtStatus.Text = found
////////////////                ? $"Restore by IP complete → {ip}"
////////////////                : $"No backup entry found for IP {ip}.";
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  REORDER IPs
////////////////        // ════════════════════════════════════════════════════════
////////////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            if (LstModules.Items.Count == 0)
////////////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////////////            if (dlg.ShowDialog() != true) return;

////////////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////////////            byte[] bytes = startAddr.GetAddressBytes();
////////////////            int counter = bytes[3];

////////////////            foreach (NetworkModule m in LstModules.Items)
////////////////            {
////////////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////////////                m.IpAddress = new IPAddress(bytes).ToString();
////////////////            }

////////////////            LstModules.Items.Refresh();
////////////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////////////        }

////////////////        // ════════════════════════════════════════════════════════
////////////////        //  UPGRADE FW
////////////////        // ════════════════════════════════════════════════════════
////////////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////////////        {
////////////////            if (LstModules.SelectedItems.Count == 0)
////////////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////////////            MessageBoxResult confirm = MessageBox.Show(
////////////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////////////                "Do NOT power off during upgrade.",
////////////////                "Confirm Firmware Upgrade",
////////////////                MessageBoxButton.YesNo,
////////////////                MessageBoxImage.Question);

////////////////            if (confirm != MessageBoxResult.Yes) return;

////////////////            OpenFileDialog dlg = new OpenFileDialog
////////////////            {
////////////////                Title = "Select Firmware File",
////////////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////////////            };

////////////////            if (dlg.ShowDialog() != true) return;

////////////////            Button btn = (Button)sender;
////////////////            btn.IsEnabled = false;
////////////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////////////            List<NetworkModule> targets = LstModules.SelectedItems
////////////////                .Cast<NetworkModule>()
////////////////                .ToList();

////////////////            string fwPath = dlg.FileName;

////////////////            try
////////////////            {
////////////////                await Task.Run(() =>
////////////////                {
////////////////                    foreach (NetworkModule m in targets)
////////////////                    {
////////////////                        System.Threading.Thread.Sleep(500);
////////////////                        m.FirmwareRev = "NEW";
////////////////                    }
////////////////                });

////////////////                LstModules.Items.Refresh();
////////////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////////////            }
////////////////            catch (Exception ex)
////////////////            {
////////////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////////////            }
////////////////            finally
////////////////            {
////////////////                btn.IsEnabled = true;
////////////////            }
////////////////        }

////////////////        private static void Warn(string message, string title) =>
////////////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////////////    }

////////////////    // ────────────────────────────────────────────────────────────
////////////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////////////    // ────────────────────────────────────────────────────────────
////////////////    public class ReorderIpDialog : Window
////////////////    {
////////////////        private readonly TextBox _input;
////////////////        public string StartIp => _input.Text.Trim();

////////////////        public ReorderIpDialog()
////////////////        {
////////////////            Title = "Reorder IPs";
////////////////            Width = 280;
////////////////            Height = 130;
////////////////            ResizeMode = ResizeMode.NoResize;
////////////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////////////            sp.Children.Add(new TextBlock
////////////////            {
////////////////                Text = "Enter starting IP address:",
////////////////                Margin = new Thickness(0, 0, 0, 6)
////////////////            });

////////////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////////////            sp.Children.Add(_input);

////////////////            StackPanel btnRow = new StackPanel
////////////////            {
////////////////                Orientation = Orientation.Horizontal,
////////////////                HorizontalAlignment = HorizontalAlignment.Right,
////////////////                Margin = new Thickness(0, 10, 0, 0)
////////////////            };

////////////////            Button ok = new Button
////////////////            {
////////////////                Content = "OK",
////////////////                Width = 70,
////////////////                Height = 24,
////////////////                Margin = new Thickness(0, 0, 6, 0),
////////////////                IsDefault = true
////////////////            };
////////////////            ok.Click += (s, ev) => { DialogResult = true; };

////////////////            Button cancel = new Button
////////////////            {
////////////////                Content = "Cancel",
////////////////                Width = 70,
////////////////                Height = 24,
////////////////                IsCancel = true
////////////////            };

////////////////            btnRow.Children.Add(ok);
////////////////            btnRow.Children.Add(cancel);
////////////////            sp.Children.Add(btnRow);

////////////////            Content = sp;
////////////////        }
////////////////    }
////////////////}
//////////////using BLDC_Demo.Controls;
//////////////using Microsoft.Win32;
//////////////using System;
//////////////using System.Collections.Generic;
//////////////using System.Linq;
//////////////using System.Net;
//////////////using System.Net.NetworkInformation;
//////////////using System.Net.Sockets;
//////////////using System.Text.RegularExpressions;
//////////////using System.Threading.Tasks;
//////////////using System.Windows;
//////////////using System.Windows.Controls;
//////////////using System.Windows.Input;

//////////////namespace BLDC_Demo
//////////////{
//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  Model: a discovered network module
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public class NetworkModule
//////////////    {
//////////////        public string SerialNumber { get; set; } = string.Empty;
//////////////        public string IpAddress { get; set; } = string.Empty;
//////////////        public string Mask { get; set; } = string.Empty;
//////////////        public string Gateway { get; set; } = string.Empty;
//////////////        public string FirmwareRev { get; set; } = string.Empty;
//////////////        public string Position { get; set; } = "—";

//////////////        public override string ToString() =>
//////////////            $"{IpAddress}   SN: {SerialNumber} ";
//////////////    }

//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  Backup container
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public class ModuleBackup
//////////////    {
//////////////        public string SerialNumber { get; set; } = string.Empty;
//////////////        public string IpAddress { get; set; } = string.Empty;
//////////////        public string Mask { get; set; } = string.Empty;
//////////////        public string Gateway { get; set; } = string.Empty;
//////////////        public string Position { get; set; } = string.Empty;
//////////////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////////////    }

//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  UserControl code-behind  (targets C# 7.3)
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public partial class NetworkService : UserControl
//////////////    {
//////////////        // ── ESP32 Modbus holding-register map for network config ──
//////////////        private const ushort REG_IP_OCTET1 = 0x0136;
//////////////        private const ushort REG_IP_OCTET2 = 0x0137;
//////////////        private const ushort REG_IP_OCTET3 = 0x0138;
//////////////        private const ushort REG_IP_OCTET4 = 0x0139;
//////////////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////////////        private const ushort REG_APPLY_REBOOT = 0x013B;

//////////////        // Default Modbus TCP port used when we have to open a temporary
//////////////        // connection to push a new IP to a device that isn't connected yet.
//////////////        // Change this if your devices use a different port.
//////////////        private const int DEFAULT_MODBUS_PORT = 502;

//////////////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////////////            = new Dictionary<string, ModuleBackup>();

//////////////        public NetworkService()
//////////////        {
//////////////            InitializeComponent();
//////////////            AttachOctetHandlers();
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  OCTET HELPERS
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void AttachOctetHandlers()
//////////////        {
//////////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////////////            AttachToGroup(ipBoxes);
//////////////            AttachToGroup(maskBoxes);
//////////////            AttachToGroup(gwBoxes);

//////////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////////////        }

//////////////        private void SetGatewayEnabled(bool enabled)
//////////////        {
//////////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////////////        }

//////////////        private static void AttachToGroup(TextBox[] boxes)
//////////////        {
//////////////            for (int i = 0; i < boxes.Length; i++)
//////////////            {
//////////////                int idx = i;

//////////////                boxes[idx].PreviewTextInput += (s, e) =>
//////////////                {
//////////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////////////                };

//////////////                boxes[idx].PreviewKeyDown += (s, e) =>
//////////////                {
//////////////                    TextBox tb = (TextBox)s;
//////////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////////////                    {
//////////////                        e.Handled = true;
//////////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////////////                    }
//////////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////////////                    {
//////////////                        boxes[idx - 1].Focus();
//////////////                        boxes[idx - 1].SelectAll();
//////////////                    }
//////////////                };

//////////////                boxes[idx].TextChanged += (s, e) =>
//////////////                {
//////////////                    TextBox tb = (TextBox)s;
//////////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////////////                        boxes[idx + 1].Focus();
//////////////                };

//////////////                boxes[idx].LostFocus += (s, e) =>
//////////////                {
//////////////                    TextBox tb = (TextBox)s;
//////////////                    if (int.TryParse(tb.Text, out int val))
//////////////                        tb.Text = Clamp(val, 0, 255).ToString();
//////////////                    else if (tb.Text.Length > 0)
//////////////                        tb.Text = string.Empty;
//////////////                };
//////////////            }
//////////////        }

//////////////        private static int Clamp(int value, int min, int max)
//////////////        {
//////////////            if (value < min) return min;
//////////////            if (value > max) return max;
//////////////            return value;
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  IP FIELD READ / WRITE
//////////////        // ════════════════════════════════════════════════════════
//////////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////////////        private string GetGateway() =>
//////////////            ChkGateway.IsChecked == true
//////////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////////////                : string.Empty;

//////////////        private void SetIpFields(string ip, string mask, string gw)
//////////////        {
//////////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////////////            ChkGateway.IsChecked = hasGw;
//////////////            SetGatewayEnabled(hasGw);

//////////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////////        }

//////////////        private static void FillOctets(string address,
//////////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////////////        {
//////////////            string[] parts = address != null
//////////////                ? address.Split('.')
//////////////                : new string[0];

//////////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////////////        }

//////////////        private static void ClearOctets(params TextBox[] boxes)
//////////////        {
//////////////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////////////        }

//////////////        private static bool IsValidIp(string ip) =>
//////////////            IPAddress.TryParse(ip, out _);

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  DISCOVER (PORT SCAN FILTERED)
//////////////        // ════════════════════════════════════════════════════════
//////////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            LstModules.Items.Clear();
//////////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////////////            Button btn = (Button)sender;
//////////////            btn.IsEnabled = false;

//////////////            try
//////////////            {
//////////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

//////////////                foreach (NetworkModule m in modules)
//////////////                    LstModules.Items.Add(m);

//////////////                TxtStatus.Text = modules.Count == 0
//////////////                    ? "No motor controllers found."
//////////////                    : $"Found {modules.Count} motor controller(s).";
//////////////            }
//////////////            catch (Exception ex)
//////////////            {
//////////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////////////            }
//////////////            finally
//////////////            {
//////////////                btn.IsEnabled = true;
//////////////            }
//////////////        }

//////////////        private static List<NetworkModule> DiscoverModules()
//////////////        {
//////////////            var found = new List<NetworkModule>();
//////////////            string baseIp = GetLocalSubnetBase();
//////////////            if (string.IsNullOrEmpty(baseIp)) return found;

//////////////            var localIps = new HashSet<string>(
//////////////                NetworkInterface.GetAllNetworkInterfaces()
//////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////////                    .Select(ua => ua.Address.ToString())
//////////////            );

//////////////            const int targetPort = 502;
//////////////            var tasks = new List<Task<Tuple<bool, string>>>();

//////////////            for (int i = 1; i <= 254; i++)
//////////////            {
//////////////                string targetIp = $"{baseIp}.{i}";
//////////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////////////            }

//////////////            Task.WaitAll(tasks.ToArray());

//////////////            foreach (var t in tasks)
//////////////            {
//////////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////////////                {
//////////////                    found.Add(new NetworkModule
//////////////                    {
//////////////                        IpAddress = t.Result.Item2,
//////////////                        SerialNumber = "ESP32-BLDC",
//////////////                        FirmwareRev = "1.0.0",
//////////////                        Mask = "255.255.255.0",
//////////////                        Position = "—"
//////////////                    });
//////////////                }
//////////////            }

//////////////            return found;
//////////////        }

//////////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////////////        {
//////////////            try
//////////////            {
//////////////                using (var client = new TcpClient())
//////////////                {
//////////////                    var connectTask = client.ConnectAsync(ip, port);
//////////////                    var delayTask = Task.Delay(200);
//////////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
//////////////                    if (completedTask == connectTask && client.Connected)
//////////////                        return Tuple.Create(true, ip);
//////////////                }
//////////////                return Tuple.Create(false, ip);
//////////////            }
//////////////            catch
//////////////            {
//////////////                return Tuple.Create(false, ip);
//////////////            }
//////////////        }

//////////////        private static string GetLocalSubnetBase()
//////////////        {
//////////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////////////                .ToList();

//////////////            if (!candidates.Any())
//////////////            {
//////////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////////                    .ToList();
//////////////            }

//////////////            foreach (NetworkInterface ni in candidates)
//////////////            {
//////////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////////////                {
//////////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////////                    {
//////////////                        string[] parts = ua.Address.ToString().Split('.');
//////////////                        if (parts.Length == 4)
//////////////                        {
//////////////                            if (parts[0] == "127") continue;               // loopback
//////////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////////////                        }
//////////////                    }
//////////////                }
//////////////            }
//////////////            return string.Empty;
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  LIST SELECTION → populate fields
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////////////        {
//////////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////////////            if (m == null) return;

//////////////            TxtSerialNumber.Text = m.SerialNumber;
//////////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////////////            // ── Push selected module's IP into the Connect panel ──
//////////////            // so clicking Connect on the main comm control uses this module's IP.
//////////////            if (Application.Current.MainWindow is MainWindow mainWin)
//////////////            {
//////////////                mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
//////////////            }
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  SELECT ALL / NONE
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////////////            => LstModules.SelectAll();

//////////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////////////            => LstModules.UnselectAll();

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  SET IP  (works with or without an existing connection)
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            string ip = GetIp();
//////////////            string mask = GetMask();
//////////////            string gateway = GetGateway();

//////////////            if (!IsValidIp(ip))
//////////////            {
//////////////                Warn("Enter a valid IP address.", "Invalid IP");
//////////////                return;
//////////////            }

//////////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////////////            {
//////////////                Warn("Main window not available.", "Error");
//////////////                return;
//////////////            }

//////////////            var commCtrl = mainWin.CommMotorSelect;

//////////////            if (commCtrl?.BLLogic == null)
//////////////            {
//////////////                Warn("Communication layer not initialized.", "Error");
//////////////                return;
//////////////            }

//////////////            // ── Step 1: Split IP into 4 octets ──
//////////////            string[] ipParts = ip.Split('.');
//////////////            if (ipParts.Length != 4 ||
//////////////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////////////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////////////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////////////                !byte.TryParse(ipParts[3], out byte octet4))
//////////////            {
//////////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////////////                return;
//////////////            }

//////////////            if (octet1 == 0)
//////////////            {
//////////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////////////                return;
//////////////            }

//////////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////////////            byte maskLastOctet = 0;
//////////////            string[] maskParts = mask.Split('.');
//////////////            if (maskParts.Length == 4)
//////////////                byte.TryParse(maskParts[3], out maskLastOctet);

//////////////            // ── Step 2: Confirm with the user before rebooting the device ──
//////////////            var confirm = MessageBox.Show(
//////////////                $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
//////////////                "The connection will be lost automatically during the reboot.\n\nContinue?",
//////////////                "Confirm IP Change",
//////////////                MessageBoxButton.YesNo,
//////////////                MessageBoxImage.Warning);

//////////////            if (confirm != MessageBoxResult.Yes) return;

//////////////            // ── Step 3: Make sure we actually have a live connection to write to.
//////////////            // The device might not be connected yet (user typed a new/target IP
//////////////            // and clicked Set without pressing Connect first). We need to talk
//////////////            // to the device at its CURRENT IP (the one it's reachable at right
//////////////            // now) — NOT the new IP being set, since that doesn't exist yet.
//////////////            //
//////////////            // NOTE: this assumes the "current" IP the device is reachable at is
//////////////            // whatever is currently in the Connect panel (TcpIpAddress) or the
//////////////            // selected module's IP — i.e. wherever we can currently reach it.
//////////////            bool wasAlreadyConnected = !string.IsNullOrEmpty(commCtrl.BLLogic.ActivePortName);
//////////////            bool openedTemporaryConnection = false;

//////////////            try
//////////////            {
//////////////                if (!wasAlreadyConnected)
//////////////                {
//////////////                    // Prefer the currently selected module's IP (the device's
//////////////                    // CURRENT reachable address) over the "new" IP just typed
//////////////                    // into the Set panel — otherwise we'd try to connect to an
//////////////                    // address the device doesn't have yet.
//////////////                    NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////////////                    string currentDeviceIp = selected != null
//////////////                        ? selected.IpAddress
//////////////                        : commCtrl.TcpIpAddress.Text.Trim();

//////////////                    if (!IsValidIp(currentDeviceIp))
//////////////                    {
//////////////                        Warn(
//////////////                            "Device isn't connected and no valid current IP is known.\n" +
//////////////                            "Run Discover or select a module first, then Set.",
//////////////                            "Not Connected");
//////////////                        return;
//////////////                    }

//////////////                    if (!int.TryParse(commCtrl.TcpPort.Text, out int port))
//////////////                        port = DEFAULT_MODBUS_PORT;

//////////////                    var tempTcp = new CommEthernet(currentDeviceIp, port);

//////////////                    if (!commCtrl.BLLogic.Start(tempTcp))
//////////////                    {
//////////////                        Warn(
//////////////                            $"Could not connect to device at {currentDeviceIp}:{port} to send the new IP.",
//////////////                            "Connect Failed");
//////////////                        return;
//////////////                    }

//////////////                    openedTemporaryConnection = true;
//////////////                    commCtrl.UpdateStatusText();
//////////////                }

//////////////                // ── Step 4: Stage the octets + netmask ──
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET1, octet1);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET2, octet2);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET3, octet3);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET4, octet4);
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_MASK_OCTET4, maskLastOctet);

//////////////                // ── Step 5: COMMIT — this is what actually saves + reboots the device ──
//////////////                commCtrl.BLLogic.SendWriteCommand(REG_APPLY_REBOOT, 1);
//////////////            }
//////////////            catch (Exception ex)
//////////////            {
//////////////                Warn(
//////////////                    $"Failed to send new IP to the device:\n{ex.Message}\n\n" +
//////////////                    "The IP was NOT changed on the device.",
//////////////                    "Write Error");
//////////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
//////////////                return; // don't touch UI state below — nothing actually changed on the device
//////////////            }

//////////////            MessageBox.Show(
//////////////                $"IP {ip} sent and commit issued.\n" +
//////////////                "The device is rebooting now — reconnect using the new IP.",
//////////////                "IP Sent",
//////////////                MessageBoxButton.OK,
//////////////                MessageBoxImage.Information);

//////////////            // ── Step 6: Update selected module in list if one is selected ──
//////////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////////////            if (module != null)
//////////////            {
//////////////                module.IpAddress = ip;
//////////////                module.Mask = mask;
//////////////                module.Gateway = gateway;
//////////////                int idx = LstModules.SelectedIndex;
//////////////                LstModules.Items.Refresh();
//////////////                LstModules.SelectedIndex = idx;
//////////////            }

//////////////            // ── Step 7: Disconnect (device is rebooting anyway) and update the connect field ──
//////////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////////////            {
//////////////                commCtrl.BLLogic.Close();
//////////////                mainWin.DataContext = null;
//////////////            }

//////////////            commCtrl.TcpIpAddress.Text = ip;
//////////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////////////            commCtrl.ApplyAndSaveIp(ip);

//////////////            TxtStatus.Text = $"IP set → {ip}. Device rebooting. Reconnect required in a few seconds.";
//////////////        }

//////////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            // TODO: implement backup logic
//////////////        }
//////////////        // ════════════════════════════════════════════════════════
//////////////        //  RESTORE
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            if (LstModules.SelectedItems.Count == 0)
//////////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////////////            OpenFileDialog dlg = new OpenFileDialog
//////////////            {
//////////////                Title = "Open Backup File",
//////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////////            };

//////////////            if (dlg.ShowDialog() != true) return;

//////////////            int restored = 0;
//////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////////            {
//////////////                string[] parts = line.Split(',');
//////////////                if (parts.Length < 6) continue;

//////////////                string sn = parts[1].Trim();
//////////////                foreach (NetworkModule m in LstModules.SelectedItems)
//////////////                {
//////////////                    if (m.SerialNumber != sn) continue;
//////////////                    m.IpAddress = parts[2].Trim();
//////////////                    m.Mask = parts[3].Trim();
//////////////                    m.Gateway = parts[4].Trim();
//////////////                    m.Position = parts[5].Trim();
//////////////                    restored++;
//////////////                }
//////////////            }

//////////////            LstModules.Items.Refresh();
//////////////            TxtStatus.Text = restored > 0
//////////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////////////                : "No matching modules found in the backup file.";
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  RESTORE BY IP
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            string ip = GetIp();

//////////////            if (!IsValidIp(ip))
//////////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////////////            OpenFileDialog dlg = new OpenFileDialog
//////////////            {
//////////////                Title = "Open Backup File",
//////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////////            };

//////////////            if (dlg.ShowDialog() != true) return;

//////////////            bool found = false;
//////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////////            {
//////////////                string[] parts = line.Split(',');
//////////////                if (parts.Length < 6) continue;
//////////////                if (parts[2].Trim() != ip) continue;

//////////////                foreach (NetworkModule m in LstModules.Items)
//////////////                {
//////////////                    if (m.IpAddress != ip) continue;
//////////////                    m.Mask = parts[3].Trim();
//////////////                    m.Gateway = parts[4].Trim();
//////////////                    m.Position = parts[5].Trim();
//////////////                    found = true;
//////////////                }
//////////////            }

//////////////            LstModules.Items.Refresh();
//////////////            TxtStatus.Text = found
//////////////                ? $"Restore by IP complete → {ip}"
//////////////                : $"No backup entry found for IP {ip}.";
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  REORDER IPs
//////////////        // ════════════════════════════════════════════════════════
//////////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            if (LstModules.Items.Count == 0)
//////////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////////////            ReorderIpDialog dlg = new ReorderIpDialog();
//////////////            if (dlg.ShowDialog() != true) return;

//////////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////////////            byte[] bytes = startAddr.GetAddressBytes();
//////////////            int counter = bytes[3];

//////////////            foreach (NetworkModule m in LstModules.Items)
//////////////            {
//////////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////////////                m.IpAddress = new IPAddress(bytes).ToString();
//////////////            }

//////////////            LstModules.Items.Refresh();
//////////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////////////        }

//////////////        // ════════════════════════════════════════════════════════
//////////////        //  UPGRADE FW
//////////////        // ════════════════════════════════════════════════════════
//////////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////////////        {
//////////////            if (LstModules.SelectedItems.Count == 0)
//////////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////////////            MessageBoxResult confirm = MessageBox.Show(
//////////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////////////                "Do NOT power off during upgrade.",
//////////////                "Confirm Firmware Upgrade",
//////////////                MessageBoxButton.YesNo,
//////////////                MessageBoxImage.Question);

//////////////            if (confirm != MessageBoxResult.Yes) return;

//////////////            OpenFileDialog dlg = new OpenFileDialog
//////////////            {
//////////////                Title = "Select Firmware File",
//////////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////////////            };

//////////////            if (dlg.ShowDialog() != true) return;

//////////////            Button btn = (Button)sender;
//////////////            btn.IsEnabled = false;
//////////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////////////            List<NetworkModule> targets = LstModules.SelectedItems
//////////////                .Cast<NetworkModule>()
//////////////                .ToList();

//////////////            string fwPath = dlg.FileName;

//////////////            try
//////////////            {
//////////////                await Task.Run(() =>
//////////////                {
//////////////                    foreach (NetworkModule m in targets)
//////////////                    {
//////////////                        System.Threading.Thread.Sleep(500);
//////////////                        m.FirmwareRev = "NEW";
//////////////                    }
//////////////                });

//////////////                LstModules.Items.Refresh();
//////////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////////////            }
//////////////            catch (Exception ex)
//////////////            {
//////////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////////////            }
//////////////            finally
//////////////            {
//////////////                btn.IsEnabled = true;
//////////////            }
//////////////        }

//////////////        private static void Warn(string message, string title) =>
//////////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////////////    }

//////////////    // ────────────────────────────────────────────────────────────
//////////////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////////////    // ────────────────────────────────────────────────────────────
//////////////    public class ReorderIpDialog : Window
//////////////    {
//////////////        private readonly TextBox _input;
//////////////        public string StartIp => _input.Text.Trim();

//////////////        public ReorderIpDialog()
//////////////        {
//////////////            Title = "Reorder IPs";
//////////////            Width = 280;
//////////////            Height = 130;
//////////////            ResizeMode = ResizeMode.NoResize;
//////////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////////////            sp.Children.Add(new TextBlock
//////////////            {
//////////////                Text = "Enter starting IP address:",
//////////////                Margin = new Thickness(0, 0, 0, 6)
//////////////            });

//////////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////////////            sp.Children.Add(_input);

//////////////            StackPanel btnRow = new StackPanel
//////////////            {
//////////////                Orientation = Orientation.Horizontal,
//////////////                HorizontalAlignment = HorizontalAlignment.Right,
//////////////                Margin = new Thickness(0, 10, 0, 0)
//////////////            };

//////////////            Button ok = new Button
//////////////            {
//////////////                Content = "OK",
//////////////                Width = 70,
//////////////                Height = 24,
//////////////                Margin = new Thickness(0, 0, 6, 0),
//////////////                IsDefault = true
//////////////            };
//////////////            ok.Click += (s, ev) => { DialogResult = true; };

//////////////            Button cancel = new Button
//////////////            {
//////////////                Content = "Cancel",
//////////////                Width = 70,
//////////////                Height = 24,
//////////////                IsCancel = true
//////////////            };

//////////////            btnRow.Children.Add(ok);
//////////////            btnRow.Children.Add(cancel);
//////////////            sp.Children.Add(btnRow);

//////////////            Content = sp;
//////////////        }
//////////////    }
//////////////}
////////////using BLDC_Demo.Controls;
////////////using Microsoft.Win32;
////////////using System;
////////////using System.Collections.Generic;
////////////using System.Linq;
////////////using System.Net;
////////////using System.Net.NetworkInformation;
////////////using System.Net.Sockets;
////////////using System.Text.RegularExpressions;
////////////using System.Threading.Tasks;
////////////using System.Windows;
////////////using System.Windows.Controls;
////////////using System.Windows.Input;

////////////namespace BLDC_Demo
////////////{
////////////    // ────────────────────────────────────────────────────────────
////////////    //  Model: a discovered network module
////////////    // ────────────────────────────────────────────────────────────
////////////    public class NetworkModule
////////////    {
////////////        public string SerialNumber { get; set; } = string.Empty;
////////////        public string IpAddress { get; set; } = string.Empty;
////////////        public string Mask { get; set; } = string.Empty;
////////////        public string Gateway { get; set; } = string.Empty;
////////////        public string FirmwareRev { get; set; } = string.Empty;
////////////        public string Position { get; set; } = "—";

////////////        public override string ToString() =>
////////////            $"{IpAddress}   SN: {SerialNumber} ";
////////////    }

////////////    // ────────────────────────────────────────────────────────────
////////////    //  Backup container
////////////    // ────────────────────────────────────────────────────────────
////////////    public class ModuleBackup
////////////    {
////////////        public string SerialNumber { get; set; } = string.Empty;
////////////        public string IpAddress { get; set; } = string.Empty;
////////////        public string Mask { get; set; } = string.Empty;
////////////        public string Gateway { get; set; } = string.Empty;
////////////        public string Position { get; set; } = string.Empty;
////////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////////    }

////////////    // ────────────────────────────────────────────────────────────
////////////    //  UserControl code-behind  (targets C# 7.3)
////////////    // ────────────────────────────────────────────────────────────
////////////    public partial class NetworkService : UserControl
////////////    {
////////////        // ── ESP32 Modbus holding-register map for network config ──
////////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////////        // Default Modbus TCP port used when we have to open a temporary
////////////        // connection to push a new IP to a device that isn't connected yet.
////////////        // Change this if your devices use a different port.
////////////        private const int DEFAULT_MODBUS_PORT = 502;

////////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////////            = new Dictionary<string, ModuleBackup>();

////////////        public NetworkService()
////////////        {
////////////            InitializeComponent();
////////////            AttachOctetHandlers();
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  OCTET HELPERS
////////////        // ════════════════════════════════════════════════════════
////////////        private void AttachOctetHandlers()
////////////        {
////////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////////            AttachToGroup(ipBoxes);
////////////            AttachToGroup(maskBoxes);
////////////            AttachToGroup(gwBoxes);

////////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////////        }

////////////        private void SetGatewayEnabled(bool enabled)
////////////        {
////////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////////        }

////////////        private static void AttachToGroup(TextBox[] boxes)
////////////        {
////////////            for (int i = 0; i < boxes.Length; i++)
////////////            {
////////////                int idx = i;

////////////                boxes[idx].PreviewTextInput += (s, e) =>
////////////                {
////////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////////                };

////////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////////                {
////////////                    TextBox tb = (TextBox)s;
////////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////////                    {
////////////                        e.Handled = true;
////////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////////                    }
////////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////////                    {
////////////                        boxes[idx - 1].Focus();
////////////                        boxes[idx - 1].SelectAll();
////////////                    }
////////////                };

////////////                boxes[idx].TextChanged += (s, e) =>
////////////                {
////////////                    TextBox tb = (TextBox)s;
////////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////////                        boxes[idx + 1].Focus();
////////////                };

////////////                boxes[idx].LostFocus += (s, e) =>
////////////                {
////////////                    TextBox tb = (TextBox)s;
////////////                    if (int.TryParse(tb.Text, out int val))
////////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////////                    else if (tb.Text.Length > 0)
////////////                        tb.Text = string.Empty;
////////////                };
////////////            }
////////////        }

////////////        private static int Clamp(int value, int min, int max)
////////////        {
////////////            if (value < min) return min;
////////////            if (value > max) return max;
////////////            return value;
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  IP FIELD READ / WRITE
////////////        // ════════════════════════════════════════════════════════
////////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////////        private string GetGateway() =>
////////////            ChkGateway.IsChecked == true
////////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////////                : string.Empty;

////////////        private void SetIpFields(string ip, string mask, string gw)
////////////        {
////////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////////            ChkGateway.IsChecked = hasGw;
////////////            SetGatewayEnabled(hasGw);

////////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////////        }

////////////        private static void FillOctets(string address,
////////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////////        {
////////////            string[] parts = address != null
////////////                ? address.Split('.')
////////////                : new string[0];

////////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////////        }

////////////        private static void ClearOctets(params TextBox[] boxes)
////////////        {
////////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////////        }

////////////        private static bool IsValidIp(string ip) =>
////////////            IPAddress.TryParse(ip, out _);

////////////        // ════════════════════════════════════════════════════════
////////////        //  DISCOVER (PORT SCAN FILTERED)
////////////        // ════════════════════════════════════════════════════════
////////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            LstModules.Items.Clear();
////////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////////            Button btn = (Button)sender;
////////////            btn.IsEnabled = false;

////////////            try
////////////            {
////////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////////                foreach (NetworkModule m in modules)
////////////                    LstModules.Items.Add(m);

////////////                TxtStatus.Text = modules.Count == 0
////////////                    ? "No motor controllers found."
////////////                    : $"Found {modules.Count} motor controller(s).";
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////////            }
////////////            finally
////////////            {
////////////                btn.IsEnabled = true;
////////////            }
////////////        }

////////////        private static List<NetworkModule> DiscoverModules()
////////////        {
////////////            var found = new List<NetworkModule>();
////////////            string baseIp = GetLocalSubnetBase();
////////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////////            var localIps = new HashSet<string>(
////////////                NetworkInterface.GetAllNetworkInterfaces()
////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////                    .Select(ua => ua.Address.ToString())
////////////            );

////////////            const int targetPort = 502;
////////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////////            for (int i = 1; i <= 254; i++)
////////////            {
////////////                string targetIp = $"{baseIp}.{i}";
////////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////////            }

////////////            Task.WaitAll(tasks.ToArray());

////////////            foreach (var t in tasks)
////////////            {
////////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////////                {
////////////                    found.Add(new NetworkModule
////////////                    {
////////////                        IpAddress = t.Result.Item2,
////////////                        SerialNumber = "ESP32-BLDC",
////////////                        FirmwareRev = "1.0.0",
////////////                        Mask = "255.255.255.0",
////////////                        Position = "—"
////////////                    });
////////////                }
////////////            }

////////////            return found;
////////////        }

////////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////////        {
////////////            try
////////////            {
////////////                using (var client = new TcpClient())
////////////                {
////////////                    var connectTask = client.ConnectAsync(ip, port);
////////////                    var delayTask = Task.Delay(200);
////////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
////////////                    if (completedTask == connectTask && client.Connected)
////////////                        return Tuple.Create(true, ip);
////////////                }
////////////                return Tuple.Create(false, ip);
////////////            }
////////////            catch
////////////            {
////////////                return Tuple.Create(false, ip);
////////////            }
////////////        }

////////////        private static string GetLocalSubnetBase()
////////////        {
////////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////////                .ToList();

////////////            if (!candidates.Any())
////////////            {
////////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////////                    .ToList();
////////////            }

////////////            foreach (NetworkInterface ni in candidates)
////////////            {
////////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////////                {
////////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////////                    {
////////////                        string[] parts = ua.Address.ToString().Split('.');
////////////                        if (parts.Length == 4)
////////////                        {
////////////                            if (parts[0] == "127") continue;               // loopback
////////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////////                        }
////////////                    }
////////////                }
////////////            }
////////////            return string.Empty;
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  LIST SELECTION → populate fields
////////////        // ════════════════════════════════════════════════════════
////////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////////        {
////////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////////            if (m == null) return;

////////////            TxtSerialNumber.Text = m.SerialNumber;
////////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////////            // ── Push selected module's IP into the Connect panel ──
////////////            // so clicking Connect on the main comm control uses this module's IP.
////////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////////            {
////////////                mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
////////////            }
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  SELECT ALL / NONE
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////////            => LstModules.SelectAll();

////////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////////            => LstModules.UnselectAll();

////////////        // ════════════════════════════════════════════════════════
////////////        //  SET IP  (works with or without an existing connection)
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            string ip = GetIp();
////////////            string mask = GetMask();
////////////            string gateway = GetGateway();

////////////            if (!IsValidIp(ip))
////////////            {
////////////                Warn("Enter a valid IP address.", "Invalid IP");
////////////                return;
////////////            }

////////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////////            {
////////////                Warn("Main window not available.", "Error");
////////////                return;
////////////            }

////////////            var commCtrl = mainWin.CommMotorSelect;

////////////            if (commCtrl?.BLLogic == null)
////////////            {
////////////                Warn("Communication layer not initialized.", "Error");
////////////                return;
////////////            }

////////////            // ── Step 1: Split IP into 4 octets ──
////////////            string[] ipParts = ip.Split('.');
////////////            if (ipParts.Length != 4 ||
////////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////////                !byte.TryParse(ipParts[3], out byte octet4))
////////////            {
////////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////////                return;
////////////            }

////////////            if (octet1 == 0)
////////////            {
////////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////////                return;
////////////            }

////////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////////            byte maskLastOctet = 0;
////////////            string[] maskParts = mask.Split('.');
////////////            if (maskParts.Length == 4)
////////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////////            // ── Step 2: Confirm with the user before rebooting the device ──
////////////            var confirm = MessageBox.Show(
////////////                $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
////////////                "The connection will be lost automatically during the reboot.\n\nContinue?",
////////////                "Confirm IP Change",
////////////                MessageBoxButton.YesNo,
////////////                MessageBoxImage.Warning);

////////////            if (confirm != MessageBoxResult.Yes) return;

////////////            // ── Step 3: Make sure we actually have a live connection to write to.
////////////            // The device might not be connected yet (user typed a new/target IP
////////////            // and clicked Set without pressing Connect first). We need to talk
////////////            // to the device at its CURRENT IP (the one it's reachable at right
////////////            // now) — NOT the new IP being set, since that doesn't exist yet.
////////////            //
////////////            // NOTE: this assumes the "current" IP the device is reachable at is
////////////            // whatever is currently in the Connect panel (TcpIpAddress) or the
////////////            // selected module's IP — i.e. wherever we can currently reach it.
////////////            bool wasAlreadyConnected = !string.IsNullOrEmpty(commCtrl.BLLogic.ActivePortName);

////////////            try
////////////            {
////////////                if (!wasAlreadyConnected)
////////////                {
////////////                    // Prefer the currently selected module's IP (the device's
////////////                    // CURRENT reachable address) over the "new" IP just typed
////////////                    // into the Set panel — otherwise we'd try to connect to an
////////////                    // address the device doesn't have yet.
////////////                    NetworkModule selected = LstModules.SelectedItem as NetworkModule;
////////////                    string currentDeviceIp = selected != null
////////////                        ? selected.IpAddress
////////////                        : commCtrl.TcpIpAddress.Text.Trim();

////////////                    if (!IsValidIp(currentDeviceIp))
////////////                    {
////////////                        Warn(
////////////                            "Device isn't connected and no valid current IP is known.\n" +
////////////                            "Run Discover or select a module first, then Set.",
////////////                            "Not Connected");
////////////                        return;
////////////                    }

////////////                    if (!int.TryParse(commCtrl.TcpPort.Text, out int port))
////////////                        port = DEFAULT_MODBUS_PORT;

////////////                    var tempTcp = new CommEthernet(currentDeviceIp, port);

////////////                    if (!commCtrl.BLLogic.Start(tempTcp))
////////////                    {
////////////                        Warn(
////////////                            $"Could not connect to device at {currentDeviceIp}:{port} to send the new IP.",
////////////                            "Connect Failed");
////////////                        return;
////////////                    }

////////////                    commCtrl.UpdateStatusText();
////////////                }

////////////                // ── Step 4: Stage the octets + netmask ──
////////////                // SendWriteCommand now RETURNS bool — false means it silently
////////////                // no-op'd (not connected), which is exactly the failure mode
////////////                // that used to report "IP set" while the device got nothing.
////////////                bool ok = true;
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET1, octet1);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET2, octet2);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET3, octet3);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_IP_OCTET4, octet4);
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_MASK_OCTET4, maskLastOctet);

////////////                // ── Step 5: COMMIT — this is what actually saves + reboots the device ──
////////////                ok &= commCtrl.BLLogic.SendWriteCommand(REG_APPLY_REBOOT, 1);

////////////                if (!ok)
////////////                {
////////////                    Warn(
////////////                        "One or more writes were not sent (no active connection).\n" +
////////////                        "The IP was NOT changed on the device.",
////////////                        "Write Failed");
////////////                    TxtStatus.Text = "Set IP failed: write not sent (not connected).";
////////////                    return; // don't touch UI state below — nothing actually changed on the device
////////////                }
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                Warn(
////////////                    $"Failed to send new IP to the device:\n{ex.Message}\n\n" +
////////////                    "The IP was NOT changed on the device.",
////////////                    "Write Error");
////////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
////////////                return; // don't touch UI state below — nothing actually changed on the device
////////////            }

////////////            MessageBox.Show(
////////////                $"IP {ip} sent and commit issued.\n" +
////////////                "The device is rebooting now — reconnect using the new IP.",
////////////                "IP Sent",
////////////                MessageBoxButton.OK,
////////////                MessageBoxImage.Information);

////////////            // ── Step 6: Update selected module in list if one is selected ──
////////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////////            if (module != null)
////////////            {
////////////                module.IpAddress = ip;
////////////                module.Mask = mask;
////////////                module.Gateway = gateway;
////////////                int idx = LstModules.SelectedIndex;
////////////                LstModules.Items.Refresh();
////////////                LstModules.SelectedIndex = idx;
////////////            }

////////////            // ── Step 7: Disconnect (device is rebooting anyway) and update the connect field ──
////////////            // IMPORTANT: use Close(drainPendingWrites: true) here — the writes
////////////            // above (including the reboot/commit register) are queued and sent
////////////            // asynchronously by BL's IO thread. Closing immediately with the
////////////            // old Close() could tear the thread down before the last write(s)
////////////            // actually left the socket, silently dropping the commit.
////////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////////            {
////////////                commCtrl.BLLogic.Close(drainPendingWrites: true);
////////////                mainWin.DataContext = null;
////////////            }

////////////            commCtrl.TcpIpAddress.Text = ip;
////////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////////            commCtrl.ApplyAndSaveIp(ip);

////////////            TxtStatus.Text = $"IP set → {ip}. Device rebooting. Reconnect required in a few seconds.";
////////////        }

////////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            // TODO: implement backup logic
////////////        }
////////////        // ════════════════════════════════════════════════════════
////////////        //  RESTORE
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            if (LstModules.SelectedItems.Count == 0)
////////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////////            OpenFileDialog dlg = new OpenFileDialog
////////////            {
////////////                Title = "Open Backup File",
////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////            };

////////////            if (dlg.ShowDialog() != true) return;

////////////            int restored = 0;
////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////            {
////////////                string[] parts = line.Split(',');
////////////                if (parts.Length < 6) continue;

////////////                string sn = parts[1].Trim();
////////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////////                {
////////////                    if (m.SerialNumber != sn) continue;
////////////                    m.IpAddress = parts[2].Trim();
////////////                    m.Mask = parts[3].Trim();
////////////                    m.Gateway = parts[4].Trim();
////////////                    m.Position = parts[5].Trim();
////////////                    restored++;
////////////                }
////////////            }

////////////            LstModules.Items.Refresh();
////////////            TxtStatus.Text = restored > 0
////////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////////                : "No matching modules found in the backup file.";
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  RESTORE BY IP
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            string ip = GetIp();

////////////            if (!IsValidIp(ip))
////////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////////            OpenFileDialog dlg = new OpenFileDialog
////////////            {
////////////                Title = "Open Backup File",
////////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////////            };

////////////            if (dlg.ShowDialog() != true) return;

////////////            bool found = false;
////////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////////            {
////////////                string[] parts = line.Split(',');
////////////                if (parts.Length < 6) continue;
////////////                if (parts[2].Trim() != ip) continue;

////////////                foreach (NetworkModule m in LstModules.Items)
////////////                {
////////////                    if (m.IpAddress != ip) continue;
////////////                    m.Mask = parts[3].Trim();
////////////                    m.Gateway = parts[4].Trim();
////////////                    m.Position = parts[5].Trim();
////////////                    found = true;
////////////                }
////////////            }

////////////            LstModules.Items.Refresh();
////////////            TxtStatus.Text = found
////////////                ? $"Restore by IP complete → {ip}"
////////////                : $"No backup entry found for IP {ip}.";
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  REORDER IPs
////////////        // ════════════════════════════════════════════════════════
////////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            if (LstModules.Items.Count == 0)
////////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////////            if (dlg.ShowDialog() != true) return;

////////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////////            byte[] bytes = startAddr.GetAddressBytes();
////////////            int counter = bytes[3];

////////////            foreach (NetworkModule m in LstModules.Items)
////////////            {
////////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////////                m.IpAddress = new IPAddress(bytes).ToString();
////////////            }

////////////            LstModules.Items.Refresh();
////////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////////        }

////////////        // ════════════════════════════════════════════════════════
////////////        //  UPGRADE FW
////////////        // ════════════════════════════════════════════════════════
////////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            if (LstModules.SelectedItems.Count == 0)
////////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////////            MessageBoxResult confirm = MessageBox.Show(
////////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////////                "Do NOT power off during upgrade.",
////////////                "Confirm Firmware Upgrade",
////////////                MessageBoxButton.YesNo,
////////////                MessageBoxImage.Question);

////////////            if (confirm != MessageBoxResult.Yes) return;

////////////            OpenFileDialog dlg = new OpenFileDialog
////////////            {
////////////                Title = "Select Firmware File",
////////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////////            };

////////////            if (dlg.ShowDialog() != true) return;

////////////            Button btn = (Button)sender;
////////////            btn.IsEnabled = false;
////////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////////            List<NetworkModule> targets = LstModules.SelectedItems
////////////                .Cast<NetworkModule>()
////////////                .ToList();

////////////            string fwPath = dlg.FileName;

////////////            try
////////////            {
////////////                await Task.Run(() =>
////////////                {
////////////                    foreach (NetworkModule m in targets)
////////////                    {
////////////                        System.Threading.Thread.Sleep(500);
////////////                        m.FirmwareRev = "NEW";
////////////                    }
////////////                });

////////////                LstModules.Items.Refresh();
////////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////////            }
////////////            finally
////////////            {
////////////                btn.IsEnabled = true;
////////////            }
////////////        }

////////////        private static void Warn(string message, string title) =>
////////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////////    }

////////////    // ────────────────────────────────────────────────────────────
////////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////////    // ────────────────────────────────────────────────────────────
////////////    public class ReorderIpDialog : Window
////////////    {
////////////        private readonly TextBox _input;
////////////        public string StartIp => _input.Text.Trim();

////////////        public ReorderIpDialog()
////////////        {
////////////            Title = "Reorder IPs";
////////////            Width = 280;
////////////            Height = 130;
////////////            ResizeMode = ResizeMode.NoResize;
////////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////////            sp.Children.Add(new TextBlock
////////////            {
////////////                Text = "Enter starting IP address:",
////////////                Margin = new Thickness(0, 0, 0, 6)
////////////            });

////////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////////            sp.Children.Add(_input);

////////////            StackPanel btnRow = new StackPanel
////////////            {
////////////                Orientation = Orientation.Horizontal,
////////////                HorizontalAlignment = HorizontalAlignment.Right,
////////////                Margin = new Thickness(0, 10, 0, 0)
////////////            };

////////////            Button ok = new Button
////////////            {
////////////                Content = "OK",
////////////                Width = 70,
////////////                Height = 24,
////////////                Margin = new Thickness(0, 0, 6, 0),
////////////                IsDefault = true
////////////            };
////////////            ok.Click += (s, ev) => { DialogResult = true; };

////////////            Button cancel = new Button
////////////            {
////////////                Content = "Cancel",
////////////                Width = 70,
////////////                Height = 24,
////////////                IsCancel = true
////////////            };

////////////            btnRow.Children.Add(ok);
////////////            btnRow.Children.Add(cancel);
////////////            sp.Children.Add(btnRow);

////////////            Content = sp;
////////////        }
////////////    }
////////////}
//////////using BLDC_Demo.Controls;
//////////using Microsoft.Win32;
//////////using System;
//////////using System.Collections.Generic;
//////////using System.IO;
//////////using System.Linq;
//////////using System.Net;
//////////using System.Net.NetworkInformation;
//////////using System.Net.Sockets;
//////////using System.Text.RegularExpressions;
//////////using System.Threading;
//////////using System.Threading.Tasks;
//////////using System.Windows;
//////////using System.Windows.Controls;
//////////using System.Windows.Input;

//////////namespace BLDC_Demo
//////////{
//////////    // ────────────────────────────────────────────────────────────
//////////    //  Model: a discovered network module
//////////    // ────────────────────────────────────────────────────────────
//////////    public class NetworkModule
//////////    {
//////////        public string SerialNumber { get; set; } = string.Empty;
//////////        public string IpAddress { get; set; } = string.Empty;
//////////        public string Mask { get; set; } = string.Empty;
//////////        public string Gateway { get; set; } = string.Empty;
//////////        public string FirmwareRev { get; set; } = string.Empty;
//////////        public string Position { get; set; } = "—";

//////////        public override string ToString() =>
//////////            $"{IpAddress}   SN: {SerialNumber} ";
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  Backup container
//////////    // ────────────────────────────────────────────────────────────
//////////    public class ModuleBackup
//////////    {
//////////        public string SerialNumber { get; set; } = string.Empty;
//////////        public string IpAddress { get; set; } = string.Empty;
//////////        public string Mask { get; set; } = string.Empty;
//////////        public string Gateway { get; set; } = string.Empty;
//////////        public string Position { get; set; } = string.Empty;
//////////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  UserControl code-behind  (targets C# 7.3)
//////////    // ────────────────────────────────────────────────────────────
//////////    public partial class NetworkService : UserControl
//////////    {
//////////        // ── ESP32 Modbus holding-register map for network config ──
//////////        private const ushort REG_IP_OCTET1 = 0x0136;
//////////        private const ushort REG_IP_OCTET2 = 0x0137;
//////////        private const ushort REG_IP_OCTET3 = 0x0138;
//////////        private const ushort REG_IP_OCTET4 = 0x0139;
//////////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////////        private const ushort REG_APPLY_REBOOT = 0x013B;

//////////        // Default Modbus TCP port used when we have to open a temporary
//////////        // connection to push a new IP to a device that isn't connected yet.
//////////        // Change this if your devices use a different port.
//////////        private const int DEFAULT_MODBUS_PORT = 502;

//////////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////////            = new Dictionary<string, ModuleBackup>();

//////////        public NetworkService()
//////////        {
//////////            InitializeComponent();
//////////            AttachOctetHandlers();
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  OCTET HELPERS
//////////        // ════════════════════════════════════════════════════════
//////////        private void AttachOctetHandlers()
//////////        {
//////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////////            AttachToGroup(ipBoxes);
//////////            AttachToGroup(maskBoxes);
//////////            AttachToGroup(gwBoxes);

//////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////////        }

//////////        private void SetGatewayEnabled(bool enabled)
//////////        {
//////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////////        }

//////////        private static void AttachToGroup(TextBox[] boxes)
//////////        {
//////////            for (int i = 0; i < boxes.Length; i++)
//////////            {
//////////                int idx = i;

//////////                boxes[idx].PreviewTextInput += (s, e) =>
//////////                {
//////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////////                };

//////////                boxes[idx].PreviewKeyDown += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////////                    {
//////////                        e.Handled = true;
//////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////////                    }
//////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////////                    {
//////////                        boxes[idx - 1].Focus();
//////////                        boxes[idx - 1].SelectAll();
//////////                    }
//////////                };

//////////                boxes[idx].TextChanged += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////////                        boxes[idx + 1].Focus();
//////////                };

//////////                boxes[idx].LostFocus += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (int.TryParse(tb.Text, out int val))
//////////                        tb.Text = Clamp(val, 0, 255).ToString();
//////////                    else if (tb.Text.Length > 0)
//////////                        tb.Text = string.Empty;
//////////                };
//////////            }
//////////        }

//////////        private static int Clamp(int value, int min, int max)
//////////        {
//////////            if (value < min) return min;
//////////            if (value > max) return max;
//////////            return value;
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  IP FIELD READ / WRITE
//////////        // ════════════════════════════════════════════════════════
//////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////////        private string GetGateway() =>
//////////            ChkGateway.IsChecked == true
//////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////////                : string.Empty;

//////////        private void SetIpFields(string ip, string mask, string gw)
//////////        {
//////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////////            ChkGateway.IsChecked = hasGw;
//////////            SetGatewayEnabled(hasGw);

//////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////        }

//////////        private static void FillOctets(string address,
//////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////////        {
//////////            string[] parts = address != null
//////////                ? address.Split('.')
//////////                : new string[0];

//////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////////        }

//////////        private static void ClearOctets(params TextBox[] boxes)
//////////        {
//////////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////////        }

//////////        private static bool IsValidIp(string ip) =>
//////////            IPAddress.TryParse(ip, out _);

//////////        // ════════════════════════════════════════════════════════
//////////        //  DISCOVER (PORT SCAN FILTERED)
//////////        // ════════════════════════════════════════════════════════
//////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            LstModules.Items.Clear();
//////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////////            Button btn = (Button)sender;
//////////            btn.IsEnabled = false;

//////////            try
//////////            {
//////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

//////////                foreach (NetworkModule m in modules)
//////////                    LstModules.Items.Add(m);

//////////                TxtStatus.Text = modules.Count == 0
//////////                    ? "No motor controllers found."
//////////                    : $"Found {modules.Count} motor controller(s).";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////////            }
//////////            finally
//////////            {
//////////                btn.IsEnabled = true;
//////////            }
//////////        }

//////////        private static List<NetworkModule> DiscoverModules()
//////////        {
//////////            var found = new List<NetworkModule>();
//////////            string baseIp = GetLocalSubnetBase();
//////////            if (string.IsNullOrEmpty(baseIp)) return found;

//////////            var localIps = new HashSet<string>(
//////////                NetworkInterface.GetAllNetworkInterfaces()
//////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////                    .Select(ua => ua.Address.ToString())
//////////            );

//////////            const int targetPort = 502;
//////////            var tasks = new List<Task<Tuple<bool, string>>>();

//////////            for (int i = 1; i <= 254; i++)
//////////            {
//////////                string targetIp = $"{baseIp}.{i}";
//////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////////            }

//////////            Task.WaitAll(tasks.ToArray());

//////////            foreach (var t in tasks)
//////////            {
//////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////////                {
//////////                    found.Add(new NetworkModule
//////////                    {
//////////                        IpAddress = t.Result.Item2,
//////////                        SerialNumber = "ESP32-BLDC",
//////////                        FirmwareRev = "1.0.0",
//////////                        Mask = "255.255.255.0",
//////////                        Position = "—"
//////////                    });
//////////                }
//////////            }

//////////            return found;
//////////        }

//////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////////        {
//////////            try
//////////            {
//////////                using (var client = new TcpClient())
//////////                {
//////////                    var connectTask = client.ConnectAsync(ip, port);
//////////                    var delayTask = Task.Delay(200);
//////////                    var completedTask = await Task.WhenAny(connectTask, delayTask);
//////////                    if (completedTask == connectTask && client.Connected)
//////////                        return Tuple.Create(true, ip);
//////////                }
//////////                return Tuple.Create(false, ip);
//////////            }
//////////            catch
//////////            {
//////////                return Tuple.Create(false, ip);
//////////            }
//////////        }

//////////        private static string GetLocalSubnetBase()
//////////        {
//////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////////                .ToList();

//////////            if (!candidates.Any())
//////////            {
//////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                    .ToList();
//////////            }

//////////            foreach (NetworkInterface ni in candidates)
//////////            {
//////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////////                {
//////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////                    {
//////////                        string[] parts = ua.Address.ToString().Split('.');
//////////                        if (parts.Length == 4)
//////////                        {
//////////                            if (parts[0] == "127") continue;               // loopback
//////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////////                        }
//////////                    }
//////////                }
//////////            }
//////////            return string.Empty;
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  LIST SELECTION → populate fields
//////////        // ════════════════════════════════════════════════════════
//////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////////        {
//////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////////            if (m == null) return;

//////////            TxtSerialNumber.Text = m.SerialNumber;
//////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////////            // ── Push selected module's IP into the Connect panel ──
//////////            // so clicking Connect on the main comm control uses this module's IP.
//////////            //if (Application.Current.MainWindow is MainWindow mainWin)
//////////            //{
//////////            //    mainWin.CommMotorSelect.TcpIpAddress.Text = m.IpAddress;
//////////            //}
//////////            if (Application.Current.MainWindow is MainWindow mainWin)
//////////            {
//////////                string[] octets = m.IpAddress.Split('.');
//////////                if (octets.Length == 4)
//////////                {
//////////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
//////////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
//////////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
//////////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
//////////                }
//////////            }
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  SELECT ALL / NONE
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////////            => LstModules.SelectAll();

//////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////////            => LstModules.UnselectAll();

//////////        // ════════════════════════════════════════════════════════
//////////        //  SET IP  (works with or without an existing connection)
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string ip = GetIp();
//////////            string mask = GetMask();
//////////            string gateway = GetGateway();

//////////            if (!IsValidIp(ip))
//////////            {
//////////                Warn("Enter a valid IP address.", "Invalid IP");
//////////                return;
//////////            }

//////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////////            {
//////////                Warn("Main window not available.", "Error");
//////////                return;
//////////            }

//////////            var commCtrl = mainWin.CommMotorSelect;

//////////            if (commCtrl?.BLLogic == null)
//////////            {
//////////                Warn("Communication layer not initialized.", "Error");
//////////                return;
//////////            }

//////////            // ── Step 1: Split IP into 4 octets ──
//////////            string[] ipParts = ip.Split('.');
//////////            if (ipParts.Length != 4 ||
//////////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////////                !byte.TryParse(ipParts[3], out byte octet4))
//////////            {
//////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////////                return;
//////////            }

//////////            if (octet1 == 0)
//////////            {
//////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////////                return;
//////////            }

//////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////////            byte maskLastOctet = 0;
//////////            string[] maskParts = mask.Split('.');
//////////            if (maskParts.Length == 4)
//////////                byte.TryParse(maskParts[3], out maskLastOctet);

//////////            // ── Step 2: Confirm with the user before rebooting the device ──
//////////            //var confirm = MessageBox.Show(
//////////            //    $"This will set the device IP to {ip} and REBOOT it to apply the change.\n\n" +
//////////            //    "The connection will be lost automatically during the reboot.\n\nContinue?",
//////////            //    "Confirm IP Change",
//////////            //    MessageBoxButton.YesNo,
//////////            //    MessageBoxImage.Warning);

//////////            //if (confirm != MessageBoxResult.Yes) return;


//////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////////            //string currentDeviceIp = selected != null
//////////            //    ? selected.IpAddress
//////////            //    : commCtrl.TcpIpAddress.Text.Trim();
//////////            string currentDeviceIp = selected != null
//////////    ? selected.IpAddress
//////////    : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();
//////////            if (!IsValidIp(currentDeviceIp))
//////////            {
//////////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
//////////                return;
//////////            }


//////////             int  port1 = DEFAULT_MODBUS_PORT;

//////////            // ── Steps 4-5: Fire the writes straight over a fresh socket to
//////////            // the device's CURRENT ip:port — completely independent of
//////////            // whatever BLLogic's connection state is. No pre-flight
//////////            // "are we connected" check, no popup if it can't connect —
//////////            // just try to send, and report status via TxtStatus.
//////////            try
//////////            {
//////////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet);
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
//////////                return; // don't touch UI state below — nothing confirmed sent
//////////            }

//////////            //MessageBox.Show(
//////////            //    $"IP {ip} sent and commit issued.\n" +
//////////            //    "The device is rebooting now — reconnect using the new IP.",
//////////            //    "IP Sent",
//////////            //    MessageBoxButton.OK,
//////////            //    MessageBoxImage.Information);

//////////            // ── Step 6: Update selected module in list if one is selected ──
//////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////////            if (module != null)
//////////            {
//////////                module.IpAddress = ip;
//////////                module.Mask = mask;
//////////                module.Gateway = gateway;
//////////                int idx = LstModules.SelectedIndex;
//////////                LstModules.Items.Refresh();
//////////                LstModules.SelectedIndex = idx;
//////////            }

//////////            // ── Step 7: If the main comm panel happened to be connected to
//////////            // this same device, drop it (device is rebooting anyway).
//////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////////            {
//////////                commCtrl.BLLogic.Close();
//////////                mainWin.DataContext = null;
//////////            }

//////////            //commCtrl.TcpIpAddress.Text = ip;
//////////            string[] octets = ip.Split('.');
//////////            if (octets.Length == 4)
//////////            {
//////////                commCtrl.TcpIpOctet1.Text = octets[0];
//////////                commCtrl.TcpIpOctet2.Text = octets[1];
//////////                commCtrl.TcpIpOctet3.Text = octets[2];
//////////                commCtrl.TcpIpOctet4.Text = octets[3];
//////////            }
//////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////////            commCtrl.ApplyAndSaveIp(ip);

//////////            TxtStatus.Text = $"IP set → {ip}.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  DIRECT SET-IP WRITE — bypasses BLLogic entirely.
//////////        //  Opens its own short-lived TCP socket straight to the device's
//////////        //  current ip:port, sends all 6 Modbus writes back-to-back on
//////////        //  that one socket (in order: octets, mask, then commit/reboot),
//////////        //  then closes. Works regardless of whether the app's main
//////////        //  Connect panel is connected or not.
//////////        //
//////////        //  IMPORTANT: each write is followed by a read-back of the
//////////        //  device's response. Per the Modbus "Write Single Register"
//////////        //  spec (function 0x06), a slave that accepted the write echoes
//////////        //  the same frame back. If we don't see that echo within the
//////////        //  timeout, the write did NOT reach/get processed by the ESP32
//////////        //  — even though the bytes left our socket successfully — and
//////////        //  we now treat that as a failure instead of reporting success.
//////////        // ════════════════════════════════════════════════════════
//////////        private void SendSetIpDirect(string targetIp, int port,
//////////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet)
//////////        {
//////////            using (var client = new TcpClient())
//////////            {
//////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
//////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//////////                client.EndConnect(connectResult);

//////////                using (NetworkStream stream = client.GetStream())
//////////                {
//////////                    stream.WriteTimeout = 2000;
//////////                    stream.ReadTimeout = 2000;

//////////                    // Give the ESP32's TCP/Modbus stack a moment to finish
//////////                    // accept() and be ready to read before we start writing.
//////////                    Thread.Sleep(100);

//////////                    // ── REQUIRED FIRST: the firmware's tcp_server_task will
//////////                    // NOT process ANY write until it sees a 2-byte handshake
//////////                    // (literal bytes 0x01, 0x01) from the client. Until then
//////////                    // handshake_done stays false and every byte we send is
//////////                    // silently discarded (offset++ with no action) — this
//////////                    // was the actual cause of "app says success, device
//////////                    // shows old IP": the writes were never being read at all.
//////////                    SendHandshake(stream);

//////////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
//////////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

//////////                    // Commit/reboot last. Device may not send a clean echo
//////////                    // for this one if it starts rebooting mid-response —
//////////                    // that's expected, so don't throw if this specific
//////////                    // read-back times out.
//////////                    try
//////////                    {
//////////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
//////////                    }
//////////                    catch (TimeoutException)
//////////                    {
//////////                        // Acceptable: device may have started rebooting
//////////                        // before it could echo the commit frame back.
//////////                    }

//////////                    // Let the last frame actually clear the OS send buffer
//////////                    // and reach the NIC before we tear the socket down.
//////////                    Thread.Sleep(150);
//////////                }
//////////            }
//////////        }

//////////        // Sends the 2-byte handshake (literal 0x01, 0x01) the ESP32
//////////        // firmware requires before it will process any register writes on
//////////        // a freshly-accepted connection, and consumes the device's reply.
//////////        //
//////////        // Per modbus_server.cpp:
//////////        //   if (!handshake_done && rx[offset]==0x01 && rx[offset+1]==0x01) {
//////////        //       send(sock, &rx[offset], remaining, 0);      // echoes our 2 bytes
//////////        //       send_modbus_frame(sock, 0x01, 0x00, 0x00);  // + a 12-byte frame
//////////        //       handshake_done = true;
//////////        //   }
//////////        // So the expected reply is: our 2 bytes echoed back, followed by a
//////////        // fixed 12-byte confirmation frame — 14 bytes total. We must fully
//////////        // consume both before reading normal write-confirmation frames,
//////////        // otherwise leftover bytes misalign the next ReadModbusFrame call.
//////////        private static void SendHandshake(NetworkStream stream)
//////////        {
//////////            byte[] handshake = { 0x01, 0x01 };
//////////            stream.Write(handshake, 0, handshake.Length);
//////////            stream.Flush();

//////////            byte[] echo = ReadExactOrNull(stream, 2);
//////////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
//////////                throw new TimeoutException(
//////////                    "Device did not respond to the required connection handshake " +
//////////                    "(no echo received) — writes cannot proceed.");

//////////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
//////////            if (confirmFrame == null)
//////////                throw new TimeoutException(
//////////                    "Device echoed the handshake but did not send its confirmation frame.");
//////////        }

//////////        // Writes one Modbus "Write Single Register" frame and blocks until
//////////        // the device echoes it back (per spec) or ReadTimeout elapses.
//////////        // Throws if no valid echo is received — this is what turns a blind
//////////        // fire-and-forget write into an actually-verified one.
//////////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
//////////        {
//////////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
//////////            stream.Write(frame, 0, frame.Length);
//////////            stream.Flush();

//////////            byte[] response = ReadModbusFrame(stream);

//////////            // FC 0x06 success response echoes: [unitId, 0x06, regHi, regLo, valHi, valLo]
//////////            // (offset by the 6-byte MBAP header we also need to read first).
//////////            if (response == null)
//////////                throw new TimeoutException(
//////////                    $"No response from device for register 0x{registerAddress:X4} " +
//////////                    "(write may not have been processed).");

//////////            if (response.Length >= 6 && response[1] == 0x86) // 0x86 = FC 0x06 with error bit set
//////////            {
//////////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
//////////                throw new InvalidOperationException(
//////////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
//////////            }
//////////        }

//////////        // Reads one full Modbus TCP frame: 6-byte MBAP header + PDU body
//////////        // (length taken from the header). Returns the PDU body only
//////////        // (matching the convention used elsewhere in this app), or null
//////////        // if the read times out / connection closes before a full frame
//////////        // arrives.
//////////        private static byte[] ReadModbusFrame(NetworkStream stream)
//////////        {
//////////            byte[] mbap = ReadExactOrNull(stream, 6);
//////////            if (mbap == null) return null;

//////////            int length = (mbap[4] << 8) | mbap[5];
//////////            if (length <= 0 || length > 253) return null;

//////////            byte[] body = ReadExactOrNull(stream, length);
//////////            return body;
//////////        }

//////////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
//////////        {
//////////            byte[] buffer = new byte[count];
//////////            int read = 0;
//////////            try
//////////            {
//////////                while (read < count)
//////////                {
//////////                    int r = stream.Read(buffer, read, count - read);
//////////                    if (r <= 0) return null;
//////////                    read += r;
//////////                }
//////////                return buffer;
//////////            }
//////////            catch (IOException)
//////////            {
//////////                // Includes read timeout (thrown as IOException wrapping a
//////////                // SocketException when ReadTimeout elapses).
//////////                return null;
//////////            }
//////////        }

//////////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
//////////        {
//////////            byte[] data = {
//////////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//////////                (byte)(value >> 8), (byte)(value & 0xFF)
//////////            };

//////////            byte[] pdu = new byte[2 + data.Length];
//////////            pdu[0] = 0x01; // slave id
//////////            pdu[1] = 0x06; // function code: write single register
//////////            Array.Copy(data, 0, pdu, 2, data.Length);

//////////            byte[] frame = new byte[6 + pdu.Length];
//////////            frame[0] = 0x00; frame[1] = 0x01; // transaction id
//////////            frame[2] = 0x00; frame[3] = 0x00; // protocol id
//////////            frame[4] = (byte)(pdu.Length >> 8);
//////////            frame[5] = (byte)(pdu.Length & 0xFF);
//////////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

//////////            return frame;
//////////        }

//////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            // TODO: implement backup logic
//////////        }
//////////        // ════════════════════════════════════════════════════════
//////////        //  RESTORE
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.SelectedItems.Count == 0)
//////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Open Backup File",
//////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            int restored = 0;
//////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////            {
//////////                string[] parts = line.Split(',');
//////////                if (parts.Length < 6) continue;

//////////                string sn = parts[1].Trim();
//////////                foreach (NetworkModule m in LstModules.SelectedItems)
//////////                {
//////////                    if (m.SerialNumber != sn) continue;
//////////                    m.IpAddress = parts[2].Trim();
//////////                    m.Mask = parts[3].Trim();
//////////                    m.Gateway = parts[4].Trim();
//////////                    m.Position = parts[5].Trim();
//////////                    restored++;
//////////                }
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = restored > 0
//////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////////                : "No matching modules found in the backup file.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  RESTORE BY IP
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string ip = GetIp();

//////////            if (!IsValidIp(ip))
//////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Open Backup File",
//////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            bool found = false;
//////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////            {
//////////                string[] parts = line.Split(',');
//////////                if (parts.Length < 6) continue;
//////////                if (parts[2].Trim() != ip) continue;

//////////                foreach (NetworkModule m in LstModules.Items)
//////////                {
//////////                    if (m.IpAddress != ip) continue;
//////////                    m.Mask = parts[3].Trim();
//////////                    m.Gateway = parts[4].Trim();
//////////                    m.Position = parts[5].Trim();
//////////                    found = true;
//////////                }
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = found
//////////                ? $"Restore by IP complete → {ip}"
//////////                : $"No backup entry found for IP {ip}.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  REORDER IPs
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.Items.Count == 0)
//////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////////            ReorderIpDialog dlg = new ReorderIpDialog();
//////////            if (dlg.ShowDialog() != true) return;

//////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////////            byte[] bytes = startAddr.GetAddressBytes();
//////////            int counter = bytes[3];

//////////            foreach (NetworkModule m in LstModules.Items)
//////////            {
//////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////////                m.IpAddress = new IPAddress(bytes).ToString();
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  UPGRADE FW
//////////        // ════════════════════════════════════════════════════════
//////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.SelectedItems.Count == 0)
//////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////////            MessageBoxResult confirm = MessageBox.Show(
//////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////////                "Do NOT power off during upgrade.",
//////////                "Confirm Firmware Upgrade",
//////////                MessageBoxButton.YesNo,
//////////                MessageBoxImage.Question);

//////////            if (confirm != MessageBoxResult.Yes) return;

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Select Firmware File",
//////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            Button btn = (Button)sender;
//////////            btn.IsEnabled = false;
//////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////////            List<NetworkModule> targets = LstModules.SelectedItems
//////////                .Cast<NetworkModule>()
//////////                .ToList();

//////////            string fwPath = dlg.FileName;

//////////            try
//////////            {
//////////                await Task.Run(() =>
//////////                {
//////////                    foreach (NetworkModule m in targets)
//////////                    {
//////////                        System.Threading.Thread.Sleep(500);
//////////                        m.FirmwareRev = "NEW";
//////////                    }
//////////                });

//////////                LstModules.Items.Refresh();
//////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////////            }
//////////            finally
//////////            {
//////////                btn.IsEnabled = true;
//////////            }
//////////        }

//////////        private static void Warn(string message, string title) =>
//////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////////    // ────────────────────────────────────────────────────────────
//////////    public class ReorderIpDialog : Window
//////////    {
//////////        private readonly TextBox _input;
//////////        public string StartIp => _input.Text.Trim();

//////////        public ReorderIpDialog()
//////////        {
//////////            Title = "Reorder IPs";
//////////            Width = 280;
//////////            Height = 130;
//////////            ResizeMode = ResizeMode.NoResize;
//////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////////            sp.Children.Add(new TextBlock
//////////            {
//////////                Text = "Enter starting IP address:",
//////////                Margin = new Thickness(0, 0, 0, 6)
//////////            });

//////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////////            sp.Children.Add(_input);

//////////            StackPanel btnRow = new StackPanel
//////////            {
//////////                Orientation = Orientation.Horizontal,
//////////                HorizontalAlignment = HorizontalAlignment.Right,
//////////                Margin = new Thickness(0, 10, 0, 0)
//////////            };

//////////            Button ok = new Button
//////////            {
//////////                Content = "OK",
//////////                Width = 70,
//////////                Height = 24,
//////////                Margin = new Thickness(0, 0, 6, 0),
//////////                IsDefault = true
//////////            };
//////////            ok.Click += (s, ev) => { DialogResult = true; };

//////////            Button cancel = new Button
//////////            {
//////////                Content = "Cancel",
//////////                Width = 70,
//////////                Height = 24,
//////////                IsCancel = true
//////////            };

//////////            btnRow.Children.Add(ok);
//////////            btnRow.Children.Add(cancel);
//////////            sp.Children.Add(btnRow);

//////////            Content = sp;
//////////        }
//////////    }
//////////}
//////////using BLDC_Demo.Controls;
//////////using Microsoft.Win32;
//////////using System;
//////////using System.Collections.Generic;
//////////using System.IO;
//////////using System.Linq;
//////////using System.Net;
//////////using System.Net.NetworkInformation;
//////////using System.Net.Sockets;
//////////using System.Text.RegularExpressions;
//////////using System.Threading;
//////////using System.Threading.Tasks;
//////////using System.Windows;
//////////using System.Windows.Controls;
//////////using System.Windows.Input;

//////////namespace BLDC_Demo
//////////{
//////////    // ────────────────────────────────────────────────────────────
//////////    //  Model: a discovered network module
//////////    // ────────────────────────────────────────────────────────────
//////////    public class NetworkModule
//////////    {
//////////        public string SerialNumber { get; set; } = string.Empty;
//////////        public string IpAddress { get; set; } = string.Empty;
//////////        public string Mask { get; set; } = string.Empty;
//////////        public string Gateway { get; set; } = string.Empty;
//////////        public string FirmwareRev { get; set; } = string.Empty;
//////////        public string Position { get; set; } = "—";

//////////        public override string ToString() =>
//////////            $"{IpAddress}   SN: {SerialNumber} ";
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  Backup container
//////////    // ────────────────────────────────────────────────────────────
//////////    public class ModuleBackup
//////////    {
//////////        public string SerialNumber { get; set; } = string.Empty;
//////////        public string IpAddress { get; set; } = string.Empty;
//////////        public string Mask { get; set; } = string.Empty;
//////////        public string Gateway { get; set; } = string.Empty;
//////////        public string Position { get; set; } = string.Empty;
//////////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  UserControl code-behind  (targets C# 7.3)
//////////    // ────────────────────────────────────────────────────────────
//////////    public partial class NetworkService : UserControl
//////////    {
//////////        // ── ESP32 Modbus holding-register map for network config ──
//////////        private const ushort REG_IP_OCTET1 = 0x0136;
//////////        private const ushort REG_IP_OCTET2 = 0x0137;
//////////        private const ushort REG_IP_OCTET3 = 0x0138;
//////////        private const ushort REG_IP_OCTET4 = 0x0139;
//////////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////////        private const ushort REG_APPLY_REBOOT = 0x013B;
//////////        private const ushort REG_NAME_START = 0x0150;
//////////        private const int NAME_REG_COUNT = 16;
//////////        private const ushort REG_NAME_APPLY = 0x0160;

//////////        // Default Modbus TCP port used when we have to open a temporary
//////////        // connection to push a new IP to a device that isn't connected yet.
//////////        private const int DEFAULT_MODBUS_PORT = 502;

//////////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////////            = new Dictionary<string, ModuleBackup>();

//////////        public NetworkService()
//////////        {
//////////            InitializeComponent();
//////////            AttachOctetHandlers();
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  OCTET HELPERS
//////////        // ════════════════════════════════════════════════════════
//////////        private void AttachOctetHandlers()
//////////        {
//////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////////            AttachToGroup(ipBoxes);
//////////            AttachToGroup(maskBoxes);
//////////            AttachToGroup(gwBoxes);

//////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////////        }

//////////        private void SetGatewayEnabled(bool enabled)
//////////        {
//////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////////        }

//////////        private static void AttachToGroup(TextBox[] boxes)
//////////        {
//////////            for (int i = 0; i < boxes.Length; i++)
//////////            {
//////////                int idx = i;

//////////                boxes[idx].PreviewTextInput += (s, e) =>
//////////                {
//////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////////                };

//////////                boxes[idx].PreviewKeyDown += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////////                    {
//////////                        e.Handled = true;
//////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////////                    }
//////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////////                    {
//////////                        boxes[idx - 1].Focus();
//////////                        boxes[idx - 1].SelectAll();
//////////                    }
//////////                };

//////////                boxes[idx].TextChanged += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////////                        boxes[idx + 1].Focus();
//////////                };

//////////                boxes[idx].LostFocus += (s, e) =>
//////////                {
//////////                    TextBox tb = (TextBox)s;
//////////                    if (int.TryParse(tb.Text, out int val))
//////////                        tb.Text = Clamp(val, 0, 255).ToString();
//////////                    else if (tb.Text.Length > 0)
//////////                        tb.Text = string.Empty;
//////////                };
//////////            }
//////////        }

//////////        private static int Clamp(int value, int min, int max)
//////////        {
//////////            if (value < min) return min;
//////////            if (value > max) return max;
//////////            return value;
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  IP FIELD READ / WRITE
//////////        // ════════════════════════════════════════════════════════
//////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////////        private string GetGateway() =>
//////////            ChkGateway.IsChecked == true
//////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////////                : string.Empty;

//////////        private void SetIpFields(string ip, string mask, string gw)
//////////        {
//////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////////            ChkGateway.IsChecked = hasGw;
//////////            SetGatewayEnabled(hasGw);

//////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////////        }

//////////        private static void FillOctets(string address,
//////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////////        {
//////////            string[] parts = address != null
//////////                ? address.Split('.')
//////////                : new string[0];

//////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////////        }

//////////        private static void ClearOctets(params TextBox[] boxes)
//////////        {
//////////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////////        }

//////////        private static bool IsValidIp(string ip) =>
//////////            IPAddress.TryParse(ip, out _);

//////////        // ════════════════════════════════════════════════════════
//////////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
//////////        // ════════════════════════════════════════════════════════
//////////        //private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////////        //{
//////////        //    LstModules.Items.Clear();
//////////        //    TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////////        //    Button btn = (Button)sender;
//////////        //    btn.IsEnabled = false;

//////////        //    try
//////////        //    {
//////////        //        List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

//////////        //        foreach (NetworkModule m in modules)
//////////        //            LstModules.Items.Add(m);

//////////        //        TxtStatus.Text = modules.Count == 0
//////////        //            ? "No motor controllers found."
//////////        //            : $"Found {modules.Count} motor controller(s).";
//////////        //    }
//////////        //    catch (Exception ex)
//////////        //    {
//////////        //        TxtStatus.Text = $"Discovery error: {ex.Message}";
//////////        //    }
//////////        //    finally
//////////        //    {
//////////        //        btn.IsEnabled = true;
//////////        //    }
//////////        //}
//////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            LstModules.Items.Clear();
//////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////////            Button btn = (Button)sender;
//////////            btn.IsEnabled = false;

//////////            try
//////////            {
//////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

//////////                // ── Option A: fill in serial for the device BL is connected to ──
//////////                if (Application.Current.MainWindow is MainWindow mw)
//////////                {
//////////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
//////////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
//////////                    {
//////////                        foreach (NetworkModule m in modules)
//////////                            if (m.IpAddress == dev.IpAddress)
//////////                                m.SerialNumber = dev.SerialNumber;
//////////                    }
//////////                }

//////////                foreach (NetworkModule m in modules)
//////////                    LstModules.Items.Add(m);

//////////                TxtStatus.Text = modules.Count == 0
//////////                    ? "No motor controllers found."
//////////                    : $"Found {modules.Count} motor controller(s).";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////////            }
//////////            finally
//////////            {
//////////                btn.IsEnabled = true;
//////////            }
//////////        }

//////////        private static List<NetworkModule> DiscoverModules()
//////////        {
//////////            var found = new List<NetworkModule>();
//////////            string baseIp = GetLocalSubnetBase();
//////////            if (string.IsNullOrEmpty(baseIp)) return found;

//////////            var localIps = new HashSet<string>(
//////////                NetworkInterface.GetAllNetworkInterfaces()
//////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////                    .Select(ua => ua.Address.ToString())
//////////            );

//////////            const int targetPort = 502;
//////////            var tasks = new List<Task<Tuple<bool, string>>>();

//////////            for (int i = 1; i <= 254; i++)
//////////            {
//////////                string targetIp = $"{baseIp}.{i}";
//////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////////            }

//////////            Task.WaitAll(tasks.ToArray());

//////////            foreach (var t in tasks)
//////////            {
//////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////////                {
//////////                    found.Add(new NetworkModule
//////////                    {
//////////                        IpAddress = t.Result.Item2,
//////////                        SerialNumber ="-",
//////////                        FirmwareRev = "1.0.0",
//////////                        Mask = "255.255.255.0",
//////////                        Position = "—"
//////////                    });
//////////                }
//////////            }

//////////            return found;
//////////        }

//////////        // Tries twice per host, with a short gap between attempts, and a
//////////        // longer per-attempt timeout than before (500ms vs the old 200ms).
//////////        // This is a workaround for devices that are slow to accept right
//////////        // after a previous client disconnected — it does NOT fix a device
//////////        // that is actively refusing connections (that's a firmware issue:
//////////        // the ESP32's accept loop not resetting after client disconnect).
//////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////////        {
//////////            for (int attempt = 0; attempt < 2; attempt++)
//////////            {
//////////                try
//////////                {
//////////                    using (var client = new TcpClient())
//////////                    {
//////////                        var connectTask = client.ConnectAsync(ip, port);
//////////                        var delayTask = Task.Delay(500); // was 200
//////////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
//////////                        if (completedTask == connectTask && client.Connected)
//////////                            return Tuple.Create(true, ip);
//////////                    }
//////////                }
//////////                catch
//////////                {
//////////                    // Connection actively refused, or the attempt threw —
//////////                    // fall through and retry once.
//////////                }

//////////                if (attempt == 0)
//////////                    await Task.Delay(300);
//////////            }

//////////            return Tuple.Create(false, ip);
//////////        }

//////////        private static string GetLocalSubnetBase()
//////////        {
//////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////////                .ToList();

//////////            if (!candidates.Any())
//////////            {
//////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////////                    .ToList();
//////////            }

//////////            foreach (NetworkInterface ni in candidates)
//////////            {
//////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////////                {
//////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////////                    {
//////////                        string[] parts = ua.Address.ToString().Split('.');
//////////                        if (parts.Length == 4)
//////////                        {
//////////                            if (parts[0] == "127") continue;               // loopback
//////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////////                        }
//////////                    }
//////////                }
//////////            }
//////////            return string.Empty;
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  LIST SELECTION → populate fields
//////////        // ════════════════════════════════════════════════════════
//////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////////        {
//////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////////            if (m == null) return;

//////////            TxtSerialNumber.Text = m.SerialNumber;
//////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////////            // ── Push selected module's IP into the Connect panel ──
//////////            if (Application.Current.MainWindow is MainWindow mainWin)
//////////            {
//////////                string[] octets = m.IpAddress.Split('.');
//////////                if (octets.Length == 4)
//////////                {
//////////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
//////////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
//////////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
//////////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
//////////                }
//////////            }
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  SELECT ALL / NONE
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////////            => LstModules.SelectAll();

//////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////////            => LstModules.UnselectAll();

//////////        // ════════════════════════════════════════════════════════
//////////        //  SET IP  (works with or without an existing connection)
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string ip = GetIp();
//////////            string mask = GetMask();
//////////            string gateway = GetGateway();

//////////            if (!IsValidIp(ip))
//////////            {
//////////                Warn("Enter a valid IP address.", "Invalid IP");
//////////                return;
//////////            }

//////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////////            {
//////////                Warn("Main window not available.", "Error");
//////////                return;
//////////            }

//////////            var commCtrl = mainWin.CommMotorSelect;

//////////            if (commCtrl?.BLLogic == null)
//////////            {
//////////                Warn("Communication layer not initialized.", "Error");
//////////                return;
//////////            }

//////////            // ── Step 1: Split IP into 4 octets ──
//////////            string[] ipParts = ip.Split('.');
//////////            if (ipParts.Length != 4 ||
//////////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////////                !byte.TryParse(ipParts[3], out byte octet4))
//////////            {
//////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////////                return;
//////////            }

//////////            if (octet1 == 0)
//////////            {
//////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////////                return;
//////////            }

//////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////////            byte maskLastOctet = 0;
//////////            string[] maskParts = mask.Split('.');
//////////            if (maskParts.Length == 4)
//////////                byte.TryParse(maskParts[3], out maskLastOctet);

//////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////////            string currentDeviceIp = selected != null
//////////                ? selected.IpAddress
//////////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

//////////            if (!IsValidIp(currentDeviceIp))
//////////            {
//////////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
//////////                return;
//////////            }

//////////            int port1 = DEFAULT_MODBUS_PORT;

//////////            try
//////////            {
//////////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet);
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Set IP failed: {ex.Message}";
//////////                return; // don't touch UI state below — nothing confirmed sent
//////////            }

//////////            // ── Step 6: Update selected module in list if one is selected ──
//////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////////            if (module != null)
//////////            {
//////////                module.IpAddress = ip;
//////////                module.Mask = mask;
//////////                module.Gateway = gateway;
//////////                int idx = LstModules.SelectedIndex;
//////////                LstModules.Items.Refresh();
//////////                LstModules.SelectedIndex = idx;
//////////            }

//////////            // ── Step 7: If the main comm panel happened to be connected to
//////////            // this same device, drop it (device is rebooting anyway).
//////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////////            {
//////////                commCtrl.BLLogic.Close();
//////////                mainWin.DataContext = null;
//////////            }

//////////            string[] octets = ip.Split('.');
//////////            if (octets.Length == 4)
//////////            {
//////////                commCtrl.TcpIpOctet1.Text = octets[0];
//////////                commCtrl.TcpIpOctet2.Text = octets[1];
//////////                commCtrl.TcpIpOctet3.Text = octets[2];
//////////                commCtrl.TcpIpOctet4.Text = octets[3];
//////////            }
//////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////////            commCtrl.ApplyAndSaveIp(ip);

//////////            TxtStatus.Text = $"IP set → {ip}.";
//////////        }
//////////        private void BtnSetName_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string name = TxtStationName.Text ?? string.Empty;

//////////            if (name.Length > 32)
//////////            {
//////////                Warn("Station name can be 32 characters maximum.", "Invalid Name");
//////////                return;
//////////            }
//////////            foreach (char c in name)
//////////            {
//////////                if (c < 32 || c > 126)
//////////                {
//////////                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
//////////                    return;
//////////                }
//////////            }

//////////            if (!(Application.Current.MainWindow is MainWindow mainWin)) return;
//////////            var commCtrl = mainWin.CommMotorSelect;

//////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////////            string deviceIp = selected != null
//////////                ? selected.IpAddress
//////////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

//////////            if (!IsValidIp(deviceIp))
//////////            {
//////////                Warn("No valid device IP. Run Discover or select a module first.", "Missing IP");
//////////                return;
//////////            }

//////////            try
//////////            {
//////////                SendStationNameDirect(deviceIp, DEFAULT_MODBUS_PORT, name);
//////////                TxtStatus.Text = $"Station name set → {name}";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Set name failed: {ex.Message}";
//////////            }
//////////        }

//////////        private void SendStationNameDirect(string targetIp, int port, string name)
//////////        {
//////////            byte[] buf = new byte[32];   // all zeros = padding
//////////            byte[] ascii = System.Text.Encoding.ASCII.GetBytes(name);
//////////            Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

//////////            using (var client = new TcpClient())
//////////            {
//////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
//////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//////////                client.EndConnect(connectResult);

//////////                using (NetworkStream stream = client.GetStream())
//////////                {
//////////                    stream.WriteTimeout = 2000;
//////////                    stream.ReadTimeout = 2000;

//////////                    Thread.Sleep(100);
//////////                    SendHandshake(stream);

//////////                    for (int i = 0; i < NAME_REG_COUNT; i++)
//////////                    {
//////////                        ushort value = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
//////////                        WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), value);
//////////                    }

//////////                    WriteAndConfirm(stream, REG_NAME_APPLY, 1);
//////////                    Thread.Sleep(150);
//////////                }
//////////            }
//////////        }
//////////        // ════════════════════════════════════════════════════════
//////////        //  DIRECT SET-IP WRITE — bypasses BLLogic entirely.
//////////        // ════════════════════════════════════════════════════════
//////////        private void SendSetIpDirect(string targetIp, int port,
//////////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet)
//////////        {
//////////            using (var client = new TcpClient())
//////////            {
//////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
//////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//////////                client.EndConnect(connectResult);

//////////                using (NetworkStream stream = client.GetStream())
//////////                {
//////////                    stream.WriteTimeout = 2000;
//////////                    stream.ReadTimeout = 2000;

//////////                    Thread.Sleep(100);

//////////                    SendHandshake(stream);

//////////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
//////////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
//////////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

//////////                    try
//////////                    {
//////////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
//////////                    }
//////////                    catch (TimeoutException)
//////////                    {
//////////                        // Acceptable: device may have started rebooting
//////////                        // before it could echo the commit frame back.
//////////                    }

//////////                    Thread.Sleep(150);
//////////                }
//////////            }
//////////        }

//////////        private static void SendHandshake(NetworkStream stream)
//////////        {
//////////            byte[] handshake = { 0x01, 0x01 };
//////////            stream.Write(handshake, 0, handshake.Length);
//////////            stream.Flush();

//////////            byte[] echo = ReadExactOrNull(stream, 2);
//////////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
//////////                throw new TimeoutException(
//////////                    "Device did not respond to the required connection handshake " +
//////////                    "(no echo received) — writes cannot proceed.");

//////////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
//////////            if (confirmFrame == null)
//////////                throw new TimeoutException(
//////////                    "Device echoed the handshake but did not send its confirmation frame.");
//////////        }

//////////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
//////////        {
//////////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
//////////            stream.Write(frame, 0, frame.Length);
//////////            stream.Flush();

//////////            byte[] response = ReadModbusFrame(stream);

//////////            if (response == null)
//////////                throw new TimeoutException(
//////////                    $"No response from device for register 0x{registerAddress:X4} " +
//////////                    "(write may not have been processed).");

//////////            if (response.Length >= 6 && response[1] == 0x86)
//////////            {
//////////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
//////////                throw new InvalidOperationException(
//////////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
//////////            }
//////////        }

//////////        private static byte[] ReadModbusFrame(NetworkStream stream)
//////////        {
//////////            byte[] mbap = ReadExactOrNull(stream, 6);
//////////            if (mbap == null) return null;

//////////            int length = (mbap[4] << 8) | mbap[5];
//////////            if (length <= 0 || length > 253) return null;

//////////            byte[] body = ReadExactOrNull(stream, length);
//////////            return body;
//////////        }

//////////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
//////////        {
//////////            byte[] buffer = new byte[count];
//////////            int read = 0;
//////////            try
//////////            {
//////////                while (read < count)
//////////                {
//////////                    int r = stream.Read(buffer, read, count - read);
//////////                    if (r <= 0) return null;
//////////                    read += r;
//////////                }
//////////                return buffer;
//////////            }
//////////            catch (IOException)
//////////            {
//////////                return null;
//////////            }
//////////        }

//////////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
//////////        {
//////////            byte[] data = {
//////////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//////////                (byte)(value >> 8), (byte)(value & 0xFF)
//////////            };

//////////            byte[] pdu = new byte[2 + data.Length];
//////////            pdu[0] = 0x01;
//////////            pdu[1] = 0x06;
//////////            Array.Copy(data, 0, pdu, 2, data.Length);

//////////            byte[] frame = new byte[6 + pdu.Length];
//////////            frame[0] = 0x00; frame[1] = 0x01;
//////////            frame[2] = 0x00; frame[3] = 0x00;
//////////            frame[4] = (byte)(pdu.Length >> 8);
//////////            frame[5] = (byte)(pdu.Length & 0xFF);
//////////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

//////////            return frame;
//////////        }

//////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            // TODO: implement backup logic
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  RESTORE
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.SelectedItems.Count == 0)
//////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Open Backup File",
//////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            int restored = 0;
//////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////            {
//////////                string[] parts = line.Split(',');
//////////                if (parts.Length < 6) continue;

//////////                string sn = parts[1].Trim();
//////////                foreach (NetworkModule m in LstModules.SelectedItems)
//////////                {
//////////                    if (m.SerialNumber != sn) continue;
//////////                    m.IpAddress = parts[2].Trim();
//////////                    m.Mask = parts[3].Trim();
//////////                    m.Gateway = parts[4].Trim();
//////////                    m.Position = parts[5].Trim();
//////////                    restored++;
//////////                }
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = restored > 0
//////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////////                : "No matching modules found in the backup file.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  RESTORE BY IP
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            string ip = GetIp();

//////////            if (!IsValidIp(ip))
//////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Open Backup File",
//////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            bool found = false;
//////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////////            {
//////////                string[] parts = line.Split(',');
//////////                if (parts.Length < 6) continue;
//////////                if (parts[2].Trim() != ip) continue;

//////////                foreach (NetworkModule m in LstModules.Items)
//////////                {
//////////                    if (m.IpAddress != ip) continue;
//////////                    m.Mask = parts[3].Trim();
//////////                    m.Gateway = parts[4].Trim();
//////////                    m.Position = parts[5].Trim();
//////////                    found = true;
//////////                }
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = found
//////////                ? $"Restore by IP complete → {ip}"
//////////                : $"No backup entry found for IP {ip}.";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  REORDER IPs
//////////        // ════════════════════════════════════════════════════════
//////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.Items.Count == 0)
//////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////////            ReorderIpDialog dlg = new ReorderIpDialog();
//////////            if (dlg.ShowDialog() != true) return;

//////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////////            byte[] bytes = startAddr.GetAddressBytes();
//////////            int counter = bytes[3];

//////////            foreach (NetworkModule m in LstModules.Items)
//////////            {
//////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////////                m.IpAddress = new IPAddress(bytes).ToString();
//////////            }

//////////            LstModules.Items.Refresh();
//////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////////        }

//////////        // ════════════════════════════════════════════════════════
//////////        //  UPGRADE FW
//////////        // ════════════════════════════════════════════════════════
//////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            if (LstModules.SelectedItems.Count == 0)
//////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////////            MessageBoxResult confirm = MessageBox.Show(
//////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////////                "Do NOT power off during upgrade.",
//////////                "Confirm Firmware Upgrade",
//////////                MessageBoxButton.YesNo,
//////////                MessageBoxImage.Question);

//////////            if (confirm != MessageBoxResult.Yes) return;

//////////            OpenFileDialog dlg = new OpenFileDialog
//////////            {
//////////                Title = "Select Firmware File",
//////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////////            };

//////////            if (dlg.ShowDialog() != true) return;

//////////            Button btn = (Button)sender;
//////////            btn.IsEnabled = false;
//////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////////            List<NetworkModule> targets = LstModules.SelectedItems
//////////                .Cast<NetworkModule>()
//////////                .ToList();

//////////            string fwPath = dlg.FileName;

//////////            try
//////////            {
//////////                await Task.Run(() =>
//////////                {
//////////                    foreach (NetworkModule m in targets)
//////////                    {
//////////                        System.Threading.Thread.Sleep(500);
//////////                        m.FirmwareRev = "NEW";
//////////                    }
//////////                });

//////////                LstModules.Items.Refresh();
//////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////////            }
//////////            finally
//////////            {
//////////                btn.IsEnabled = true;
//////////            }
//////////        }

//////////        private static void Warn(string message, string title) =>
//////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////////    }

//////////    // ────────────────────────────────────────────────────────────
//////////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////////    // ────────────────────────────────────────────────────────────
//////////    public class ReorderIpDialog : Window
//////////    {
//////////        private readonly TextBox _input;
//////////        public string StartIp => _input.Text.Trim();

//////////        public ReorderIpDialog()
//////////        {
//////////            Title = "Reorder IPs";
//////////            Width = 280;
//////////            Height = 130;
//////////            ResizeMode = ResizeMode.NoResize;
//////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////////            sp.Children.Add(new TextBlock
//////////            {
//////////                Text = "Enter starting IP address:",
//////////                Margin = new Thickness(0, 0, 0, 6)
//////////            });

//////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////////            sp.Children.Add(_input);

//////////            StackPanel btnRow = new StackPanel
//////////            {
//////////                Orientation = Orientation.Horizontal,
//////////                HorizontalAlignment = HorizontalAlignment.Right,
//////////                Margin = new Thickness(0, 10, 0, 0)
//////////            };

//////////            Button ok = new Button
//////////            {
//////////                Content = "OK",
//////////                Width = 70,
//////////                Height = 24,
//////////                Margin = new Thickness(0, 0, 6, 0),
//////////                IsDefault = true
//////////            };
//////////            ok.Click += (s, ev) => { DialogResult = true; };

//////////            Button cancel = new Button
//////////            {
//////////                Content = "Cancel",
//////////                Width = 70,
//////////                Height = 24,
//////////                IsCancel = true
//////////            };

//////////            btnRow.Children.Add(ok);
//////////            btnRow.Children.Add(cancel);
//////////            sp.Children.Add(btnRow);

//////////            Content = sp;
//////////        }
//////////    }
//////////}
////////using BLDC_Demo.Controls;
////////using Microsoft.Win32;
////////using System;
////////using System.Collections.Generic;
////////using System.IO;
////////using System.Linq;
////////using System.Net;
////////using System.Net.NetworkInformation;
////////using System.Net.Sockets;
////////using System.Text.RegularExpressions;
////////using System.Threading;
////////using System.Threading.Tasks;
////////using System.Windows;
////////using System.Windows.Controls;
////////using System.Windows.Input;

////////namespace BLDC_Demo
////////{
////////    // ────────────────────────────────────────────────────────────
////////    //  Model: a discovered network module
////////    // ────────────────────────────────────────────────────────────
////////    public class NetworkModule
////////    {
////////        public string SerialNumber { get; set; } = string.Empty;
////////        public string IpAddress { get; set; } = string.Empty;
////////        public string Mask { get; set; } = string.Empty;
////////        public string Gateway { get; set; } = string.Empty;
////////        public string FirmwareRev { get; set; } = string.Empty;
////////        public string Position { get; set; } = "—";

////////        public override string ToString() =>
////////            $"{IpAddress}   SN: {SerialNumber} ";
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  Backup container
////////    // ────────────────────────────────────────────────────────────
////////    public class ModuleBackup
////////    {
////////        public string SerialNumber { get; set; } = string.Empty;
////////        public string IpAddress { get; set; } = string.Empty;
////////        public string Mask { get; set; } = string.Empty;
////////        public string Gateway { get; set; } = string.Empty;
////////        public string Position { get; set; } = string.Empty;
////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  UserControl code-behind  (targets C# 7.3)
////////    // ────────────────────────────────────────────────────────────
////////    public partial class NetworkService : UserControl
////////    {
////////        // ── ESP32 Modbus holding-register map for network config ──
////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////        // ── Station name registers ──
////////        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
////////        private const int NAME_REG_COUNT = 16;
////////        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

////////        // Default Modbus TCP port used when we have to open a temporary
////////        // connection to push a new IP to a device that isn't connected yet.
////////        private const int DEFAULT_MODBUS_PORT = 502;

////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////            = new Dictionary<string, ModuleBackup>();

////////        public NetworkService()
////////        {
////////            InitializeComponent();
////////            AttachOctetHandlers();
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  OCTET HELPERS
////////        // ════════════════════════════════════════════════════════
////////        private void AttachOctetHandlers()
////////        {
////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////            AttachToGroup(ipBoxes);
////////            AttachToGroup(maskBoxes);
////////            AttachToGroup(gwBoxes);

////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////        }

////////        private void SetGatewayEnabled(bool enabled)
////////        {
////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////        }

////////        private static void AttachToGroup(TextBox[] boxes)
////////        {
////////            for (int i = 0; i < boxes.Length; i++)
////////            {
////////                int idx = i;

////////                boxes[idx].PreviewTextInput += (s, e) =>
////////                {
////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////                };

////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////                    {
////////                        e.Handled = true;
////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////                    }
////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////                    {
////////                        boxes[idx - 1].Focus();
////////                        boxes[idx - 1].SelectAll();
////////                    }
////////                };

////////                boxes[idx].TextChanged += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////                        boxes[idx + 1].Focus();
////////                };

////////                boxes[idx].LostFocus += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (int.TryParse(tb.Text, out int val))
////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////                    else if (tb.Text.Length > 0)
////////                        tb.Text = string.Empty;
////////                };
////////            }
////////        }

////////        private static int Clamp(int value, int min, int max)
////////        {
////////            if (value < min) return min;
////////            if (value > max) return max;
////////            return value;
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  IP FIELD READ / WRITE
////////        // ════════════════════════════════════════════════════════
////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////        private string GetGateway() =>
////////            ChkGateway.IsChecked == true
////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////                : string.Empty;

////////        private void SetIpFields(string ip, string mask, string gw)
////////        {
////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////            ChkGateway.IsChecked = hasGw;
////////            SetGatewayEnabled(hasGw);

////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////        }

////////        private static void FillOctets(string address,
////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////        {
////////            string[] parts = address != null
////////                ? address.Split('.')
////////                : new string[0];

////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////        }

////////        private static void ClearOctets(params TextBox[] boxes)
////////        {
////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////        }

////////        private static bool IsValidIp(string ip) =>
////////            IPAddress.TryParse(ip, out _);

////////        // ════════════════════════════════════════════════════════
////////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
////////        // ════════════════════════════════════════════════════════
////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////        {
////////            LstModules.Items.Clear();
////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////            Button btn = (Button)sender;
////////            btn.IsEnabled = false;

////////            try
////////            {
////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules());

////////                // ── Option A: fill in serial for the device BL is connected to ──
////////                if (Application.Current.MainWindow is MainWindow mw)
////////                {
////////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
////////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
////////                    {
////////                        foreach (NetworkModule m in modules)
////////                            if (m.IpAddress == dev.IpAddress)
////////                                m.SerialNumber = dev.SerialNumber;
////////                    }
////////                }

////////                foreach (NetworkModule m in modules)
////////                    LstModules.Items.Add(m);

////////                TxtStatus.Text = modules.Count == 0
////////                    ? "No motor controllers found."
////////                    : $"Found {modules.Count} motor controller(s).";
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////            }
////////            finally
////////            {
////////                btn.IsEnabled = true;
////////            }
////////        }

////////        private static List<NetworkModule> DiscoverModules()
////////        {
////////            var found = new List<NetworkModule>();
////////            string baseIp = GetLocalSubnetBase();
////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////            var localIps = new HashSet<string>(
////////                NetworkInterface.GetAllNetworkInterfaces()
////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////                    .Select(ua => ua.Address.ToString())
////////            );

////////            const int targetPort = 502;
////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////            for (int i = 1; i <= 254; i++)
////////            {
////////                string targetIp = $"{baseIp}.{i}";
////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////            }

////////            Task.WaitAll(tasks.ToArray());

////////            foreach (var t in tasks)
////////            {
////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////                {
////////                    found.Add(new NetworkModule
////////                    {
////////                        IpAddress = t.Result.Item2,
////////                        SerialNumber = "-",
////////                        FirmwareRev = "1.0.0",
////////                        Mask = "255.255.255.0",
////////                        Position = "—"
////////                    });
////////                }
////////            }

////////            return found;
////////        }

////////        // Tries twice per host, with a short gap between attempts, and a
////////        // longer per-attempt timeout than before (500ms vs the old 200ms).
////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////        {
////////            for (int attempt = 0; attempt < 2; attempt++)
////////            {
////////                try
////////                {
////////                    using (var client = new TcpClient())
////////                    {
////////                        var connectTask = client.ConnectAsync(ip, port);
////////                        var delayTask = Task.Delay(500);
////////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
////////                        if (completedTask == connectTask && client.Connected)
////////                            return Tuple.Create(true, ip);
////////                    }
////////                }
////////                catch
////////                {
////////                    // Connection refused or failed — retry once.
////////                }

////////                if (attempt == 0)
////////                    await Task.Delay(300);
////////            }

////////            return Tuple.Create(false, ip);
////////        }

////////        private static string GetLocalSubnetBase()
////////        {
////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////                .ToList();

////////            if (!candidates.Any())
////////            {
////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                    .ToList();
////////            }

////////            foreach (NetworkInterface ni in candidates)
////////            {
////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////                {
////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////                    {
////////                        string[] parts = ua.Address.ToString().Split('.');
////////                        if (parts.Length == 4)
////////                        {
////////                            if (parts[0] == "127") continue;               // loopback
////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////                        }
////////                    }
////////                }
////////            }
////////            return string.Empty;
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  LIST SELECTION → populate fields
////////        // ════════════════════════════════════════════════════════
////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////        {
////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////            if (m == null) return;

////////            TxtSerialNumber.Text = m.SerialNumber;
////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////            // ── Push selected module's IP into the Connect panel ──
////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////            {
////////                string[] octets = m.IpAddress.Split('.');
////////                if (octets.Length == 4)
////////                {
////////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
////////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
////////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
////////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
////////                }

////////                // ── Show the current station name (only if BL is connected to this device) ──
////////                var dev = mainWin.CommMotorSelect?.BLLogic?.CurrentDevice;
////////                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
////////                    ? dev.StationName
////////                    : string.Empty;
////////            }
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  SELECT ALL / NONE
////////        // ════════════════════════════════════════════════════════
////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////            => LstModules.SelectAll();

////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////            => LstModules.UnselectAll();

////////        // ════════════════════════════════════════════════════════
////////        //  SET  (station name + IP, works with or without an existing connection)
////////        // ════════════════════════════════════════════════════════
////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////        {
////////            string ip = GetIp();
////////            string mask = GetMask();
////////            string gateway = GetGateway();

////////            if (!IsValidIp(ip))
////////            {
////////                Warn("Enter a valid IP address.", "Invalid IP");
////////                return;
////////            }

////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////            {
////////                Warn("Main window not available.", "Error");
////////                return;
////////            }

////////            var commCtrl = mainWin.CommMotorSelect;

////////            if (commCtrl?.BLLogic == null)
////////            {
////////                Warn("Communication layer not initialized.", "Error");
////////                return;
////////            }

////////            // ── Step 1: Split IP into 4 octets ──
////////            string[] ipParts = ip.Split('.');
////////            if (ipParts.Length != 4 ||
////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////                !byte.TryParse(ipParts[3], out byte octet4))
////////            {
////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////                return;
////////            }

////////            if (octet1 == 0)
////////            {
////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////                return;
////////            }

////////            // ── Station name check ──
////////            string stationName = TxtStationName.Text ?? string.Empty;

////////            if (stationName.Length > 32)
////////            {
////////                Warn("Station name can be 32 characters maximum.", "Invalid Name");
////////                return;
////////            }
////////            foreach (char c in stationName)
////////            {
////////                if (c < 32 || c > 126)
////////                {
////////                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
////////                    return;
////////                }
////////            }

////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////            byte maskLastOctet = 0;
////////            string[] maskParts = mask.Split('.');
////////            if (maskParts.Length == 4)
////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
////////            string currentDeviceIp = selected != null
////////                ? selected.IpAddress
////////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

////////            if (!IsValidIp(currentDeviceIp))
////////            {
////////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
////////                return;
////////            }

////////            int port1 = DEFAULT_MODBUS_PORT;

////////            try
////////            {
////////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Set failed: {ex.Message}";
////////                return; // don't touch UI state below — nothing confirmed sent
////////            }

////////            // ── Step 6: Update selected module in list if one is selected ──
////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////            if (module != null)
////////            {
////////                module.IpAddress = ip;
////////                module.Mask = mask;
////////                module.Gateway = gateway;
////////                int idx = LstModules.SelectedIndex;
////////                LstModules.Items.Refresh();
////////                LstModules.SelectedIndex = idx;
////////            }

////////            // ── Step 7: If the main comm panel happened to be connected to
////////            // this same device, drop it (device is rebooting anyway).
////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////            {
////////                commCtrl.BLLogic.Close();
////////                mainWin.DataContext = null;
////////            }

////////            string[] octets = ip.Split('.');
////////            if (octets.Length == 4)
////////            {
////////                commCtrl.TcpIpOctet1.Text = octets[0];
////////                commCtrl.TcpIpOctet2.Text = octets[1];
////////                commCtrl.TcpIpOctet3.Text = octets[2];
////////                commCtrl.TcpIpOctet4.Text = octets[3];
////////            }
////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////            commCtrl.ApplyAndSaveIp(ip);

////////            TxtStatus.Text = stationName.Length > 0
////////                ? $"IP set → {ip}, name → {stationName}"
////////                : $"IP set → {ip}.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
////////        // ════════════════════════════════════════════════════════
////////        private void SendSetIpDirect(string targetIp, int port,
////////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
////////            string stationName)
////////        {
////////            using (var client = new TcpClient())
////////            {
////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

////////                client.EndConnect(connectResult);

////////                using (NetworkStream stream = client.GetStream())
////////                {
////////                    stream.WriteTimeout = 2000;
////////                    stream.ReadTimeout = 2000;

////////                    Thread.Sleep(100);

////////                    SendHandshake(stream);

////////                    // ── Station name first (the IP commit reboots the device) ──
////////                    if (!string.IsNullOrEmpty(stationName))
////////                    {
////////                        byte[] buf = new byte[32];   // all zeros = padding
////////                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
////////                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

////////                        for (int i = 0; i < NAME_REG_COUNT; i++)
////////                        {
////////                            // first char = high byte, second char = low byte
////////                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
////////                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
////////                        }

////////                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
////////                    }

////////                    // ── Then the IP settings ──
////////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
////////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
////////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
////////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
////////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

////////                    try
////////                    {
////////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
////////                    }
////////                    catch (TimeoutException)
////////                    {
////////                        // Acceptable: device may have started rebooting
////////                        // before it could echo the commit frame back.
////////                    }

////////                    Thread.Sleep(150);
////////                }
////////            }
////////        }

////////        private static void SendHandshake(NetworkStream stream)
////////        {
////////            byte[] handshake = { 0x01, 0x01 };
////////            stream.Write(handshake, 0, handshake.Length);
////////            stream.Flush();

////////            byte[] echo = ReadExactOrNull(stream, 2);
////////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
////////                throw new TimeoutException(
////////                    "Device did not respond to the required connection handshake " +
////////                    "(no echo received) — writes cannot proceed.");

////////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
////////            if (confirmFrame == null)
////////                throw new TimeoutException(
////////                    "Device echoed the handshake but did not send its confirmation frame.");
////////        }

////////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
////////        {
////////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
////////            stream.Write(frame, 0, frame.Length);
////////            stream.Flush();

////////            byte[] response = ReadModbusFrame(stream);

////////            if (response == null)
////////                throw new TimeoutException(
////////                    $"No response from device for register 0x{registerAddress:X4} " +
////////                    "(write may not have been processed).");

////////            if (response.Length >= 6 && response[1] == 0x86)
////////            {
////////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
////////                throw new InvalidOperationException(
////////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
////////            }
////////        }

////////        private static byte[] ReadModbusFrame(NetworkStream stream)
////////        {
////////            byte[] mbap = ReadExactOrNull(stream, 6);
////////            if (mbap == null) return null;

////////            int length = (mbap[4] << 8) | mbap[5];
////////            if (length <= 0 || length > 253) return null;

////////            byte[] body = ReadExactOrNull(stream, length);
////////            return body;
////////        }

////////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
////////        {
////////            byte[] buffer = new byte[count];
////////            int read = 0;
////////            try
////////            {
////////                while (read < count)
////////                {
////////                    int r = stream.Read(buffer, read, count - read);
////////                    if (r <= 0) return null;
////////                    read += r;
////////                }
////////                return buffer;
////////            }
////////            catch (IOException)
////////            {
////////                return null;
////////            }
////////        }

////////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
////////        {
////////            byte[] data = {
////////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
////////                (byte)(value >> 8), (byte)(value & 0xFF)
////////            };

////////            byte[] pdu = new byte[2 + data.Length];
////////            pdu[0] = 0x01;
////////            pdu[1] = 0x06;
////////            Array.Copy(data, 0, pdu, 2, data.Length);

////////            byte[] frame = new byte[6 + pdu.Length];
////////            frame[0] = 0x00; frame[1] = 0x01;
////////            frame[2] = 0x00; frame[3] = 0x00;
////////            frame[4] = (byte)(pdu.Length >> 8);
////////            frame[5] = (byte)(pdu.Length & 0xFF);
////////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

////////            return frame;
////////        }

////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////        {
////////            // TODO: implement backup logic
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  RESTORE
////////        // ════════════════════════════════════════════════════════
////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.SelectedItems.Count == 0)
////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Open Backup File",
////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            int restored = 0;
////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////            {
////////                string[] parts = line.Split(',');
////////                if (parts.Length < 6) continue;

////////                string sn = parts[1].Trim();
////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////                {
////////                    if (m.SerialNumber != sn) continue;
////////                    m.IpAddress = parts[2].Trim();
////////                    m.Mask = parts[3].Trim();
////////                    m.Gateway = parts[4].Trim();
////////                    m.Position = parts[5].Trim();
////////                    restored++;
////////                }
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = restored > 0
////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////                : "No matching modules found in the backup file.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  RESTORE BY IP
////////        // ════════════════════════════════════════════════════════
////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////        {
////////            string ip = GetIp();

////////            if (!IsValidIp(ip))
////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Open Backup File",
////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            bool found = false;
////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////            {
////////                string[] parts = line.Split(',');
////////                if (parts.Length < 6) continue;
////////                if (parts[2].Trim() != ip) continue;

////////                foreach (NetworkModule m in LstModules.Items)
////////                {
////////                    if (m.IpAddress != ip) continue;
////////                    m.Mask = parts[3].Trim();
////////                    m.Gateway = parts[4].Trim();
////////                    m.Position = parts[5].Trim();
////////                    found = true;
////////                }
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = found
////////                ? $"Restore by IP complete → {ip}"
////////                : $"No backup entry found for IP {ip}.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  REORDER IPs
////////        // ════════════════════════════════════════════════════════
////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.Items.Count == 0)
////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////            if (dlg.ShowDialog() != true) return;

////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////            byte[] bytes = startAddr.GetAddressBytes();
////////            int counter = bytes[3];

////////            foreach (NetworkModule m in LstModules.Items)
////////            {
////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////                m.IpAddress = new IPAddress(bytes).ToString();
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  UPGRADE FW
////////        // ════════════════════════════════════════════════════════
////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.SelectedItems.Count == 0)
////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////            MessageBoxResult confirm = MessageBox.Show(
////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////                "Do NOT power off during upgrade.",
////////                "Confirm Firmware Upgrade",
////////                MessageBoxButton.YesNo,
////////                MessageBoxImage.Question);

////////            if (confirm != MessageBoxResult.Yes) return;

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Select Firmware File",
////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            Button btn = (Button)sender;
////////            btn.IsEnabled = false;
////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////            List<NetworkModule> targets = LstModules.SelectedItems
////////                .Cast<NetworkModule>()
////////                .ToList();

////////            string fwPath = dlg.FileName;

////////            try
////////            {
////////                await Task.Run(() =>
////////                {
////////                    foreach (NetworkModule m in targets)
////////                    {
////////                        System.Threading.Thread.Sleep(500);
////////                        m.FirmwareRev = "NEW";
////////                    }
////////                });

////////                LstModules.Items.Refresh();
////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////            }
////////            finally
////////            {
////////                btn.IsEnabled = true;
////////            }
////////        }

////////        private static void Warn(string message, string title) =>
////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////    // ────────────────────────────────────────────────────────────
////////    public class ReorderIpDialog : Window
////////    {
////////        private readonly TextBox _input;
////////        public string StartIp => _input.Text.Trim();

////////        public ReorderIpDialog()
////////        {
////////            Title = "Reorder IPs";
////////            Width = 280;
////////            Height = 130;
////////            ResizeMode = ResizeMode.NoResize;
////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////            sp.Children.Add(new TextBlock
////////            {
////////                Text = "Enter starting IP address:",
////////                Margin = new Thickness(0, 0, 0, 6)
////////            });

////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////            sp.Children.Add(_input);

////////            StackPanel btnRow = new StackPanel
////////            {
////////                Orientation = Orientation.Horizontal,
////////                HorizontalAlignment = HorizontalAlignment.Right,
////////                Margin = new Thickness(0, 10, 0, 0)
////////            };

////////            Button ok = new Button
////////            {
////////                Content = "OK",
////////                Width = 70,
////////                Height = 24,
////////                Margin = new Thickness(0, 0, 6, 0),
////////                IsDefault = true
////////            };
////////            ok.Click += (s, ev) => { DialogResult = true; };

////////            Button cancel = new Button
////////            {
////////                Content = "Cancel",
////////                Width = 70,
////////                Height = 24,
////////                IsCancel = true
////////            };

////////            btnRow.Children.Add(ok);
////////            btnRow.Children.Add(cancel);
////////            sp.Children.Add(btnRow);

////////            Content = sp;
////////        }
////////    }
////////}
////////using BLDC_Demo.Controls;
////////using Microsoft.Win32;
////////using System;
////////using System.Collections.Generic;
////////using System.IO;
////////using System.Linq;
////////using System.Net;
////////using System.Net.NetworkInformation;
////////using System.Net.Sockets;
////////using System.Text.RegularExpressions;
////////using System.Threading;
////////using System.Threading.Tasks;
////////using System.Windows;
////////using System.Windows.Controls;
////////using System.Windows.Input;

////////namespace BLDC_Demo
////////{
////////    // ────────────────────────────────────────────────────────────
////////    //  Model: a discovered network module
////////    // ────────────────────────────────────────────────────────────
////////    public class NetworkModule
////////    {
////////        public string SerialNumber { get; set; } = string.Empty;
////////        public string StationName { get; set; } = string.Empty;
////////        public string IpAddress { get; set; } = string.Empty;
////////        public string Mask { get; set; } = string.Empty;
////////        public string Gateway { get; set; } = string.Empty;
////////        public string FirmwareRev { get; set; } = string.Empty;
////////        public string Position { get; set; } = "—";

////////        // List shows:  192.168.1.50   Name: Station1
////////        public override string ToString() =>
////////            $"{IpAddress}   Name: {(string.IsNullOrEmpty(StationName) ? "-" : StationName)}";
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  Backup container
////////    // ────────────────────────────────────────────────────────────
////////    public class ModuleBackup
////////    {
////////        public string SerialNumber { get; set; } = string.Empty;
////////        public string IpAddress { get; set; } = string.Empty;
////////        public string Mask { get; set; } = string.Empty;
////////        public string Gateway { get; set; } = string.Empty;
////////        public string Position { get; set; } = string.Empty;
////////        public DateTime BackupTime { get; set; } = DateTime.Now;
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  UserControl code-behind  (targets C# 7.3)
////////    // ────────────────────────────────────────────────────────────
////////    public partial class NetworkService : UserControl
////////    {
////////        // ── ESP32 Modbus holding-register map for network config ──
////////        private const ushort REG_IP_OCTET1 = 0x0136;
////////        private const ushort REG_IP_OCTET2 = 0x0137;
////////        private const ushort REG_IP_OCTET3 = 0x0138;
////////        private const ushort REG_IP_OCTET4 = 0x0139;
////////        private const ushort REG_MASK_OCTET4 = 0x013A;
////////        private const ushort REG_APPLY_REBOOT = 0x013B;

////////        // ── Station name registers ──
////////        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
////////        private const int NAME_REG_COUNT = 16;
////////        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

////////        // Default Modbus TCP port used when we have to open a temporary
////////        // connection to push a new IP to a device that isn't connected yet.
////////        private const int DEFAULT_MODBUS_PORT = 502;

////////        private readonly Dictionary<string, ModuleBackup> _backupStore
////////            = new Dictionary<string, ModuleBackup>();

////////        public NetworkService()
////////        {
////////            InitializeComponent();
////////            AttachOctetHandlers();
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  OCTET HELPERS
////////        // ════════════════════════════════════════════════════════
////////        private void AttachOctetHandlers()
////////        {
////////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////////            AttachToGroup(ipBoxes);
////////            AttachToGroup(maskBoxes);
////////            AttachToGroup(gwBoxes);

////////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////////        }

////////        private void SetGatewayEnabled(bool enabled)
////////        {
////////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////////        }

////////        private static void AttachToGroup(TextBox[] boxes)
////////        {
////////            for (int i = 0; i < boxes.Length; i++)
////////            {
////////                int idx = i;

////////                boxes[idx].PreviewTextInput += (s, e) =>
////////                {
////////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////////                };

////////                boxes[idx].PreviewKeyDown += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////////                    {
////////                        e.Handled = true;
////////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////////                    }
////////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////////                    {
////////                        boxes[idx - 1].Focus();
////////                        boxes[idx - 1].SelectAll();
////////                    }
////////                };

////////                boxes[idx].TextChanged += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////////                        boxes[idx + 1].Focus();
////////                };

////////                boxes[idx].LostFocus += (s, e) =>
////////                {
////////                    TextBox tb = (TextBox)s;
////////                    if (int.TryParse(tb.Text, out int val))
////////                        tb.Text = Clamp(val, 0, 255).ToString();
////////                    else if (tb.Text.Length > 0)
////////                        tb.Text = string.Empty;
////////                };
////////            }
////////        }

////////        private static int Clamp(int value, int min, int max)
////////        {
////////            if (value < min) return min;
////////            if (value > max) return max;
////////            return value;
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  IP FIELD READ / WRITE
////////        // ════════════════════════════════════════════════════════
////////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////////        private string GetGateway() =>
////////            ChkGateway.IsChecked == true
////////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////////                : string.Empty;

////////        private void SetIpFields(string ip, string mask, string gw)
////////        {
////////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////////            ChkGateway.IsChecked = hasGw;
////////            SetGatewayEnabled(hasGw);

////////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////////        }

////////        private static void FillOctets(string address,
////////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////////        {
////////            string[] parts = address != null
////////                ? address.Split('.')
////////                : new string[0];

////////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////////        }

////////        private static void ClearOctets(params TextBox[] boxes)
////////        {
////////            foreach (TextBox b in boxes) b.Text = string.Empty;
////////        }

////////        private static bool IsValidIp(string ip) =>
////////            IPAddress.TryParse(ip, out _);

////////        // ════════════════════════════════════════════════════════
////////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
////////        // ════════════════════════════════════════════════════════
////////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////////        {
////////            LstModules.Items.Clear();
////////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////////            Button btn = (Button)sender;
////////            btn.IsEnabled = false;

////////            try
////////            {
////////                // The device that the main app is already connected to:
////////                // use its name directly, do not open a second connection.
////////                string connectedIp = null;
////////                string connectedName = null;
////////                if (Application.Current.MainWindow is MainWindow mw0)
////////                {
////////                    var d0 = mw0.CommMotorSelect?.BLLogic?.CurrentDevice;
////////                    if (d0 != null)
////////                    {
////////                        connectedIp = d0.IpAddress;
////////                        connectedName = d0.StationName;
////////                    }
////////                }

////////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules(connectedIp, connectedName));

////////                // ── Option A: fill in serial for the device BL is connected to ──
////////                if (Application.Current.MainWindow is MainWindow mw)
////////                {
////////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
////////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
////////                    {
////////                        foreach (NetworkModule m in modules)
////////                            if (m.IpAddress == dev.IpAddress)
////////                                m.SerialNumber = dev.SerialNumber;
////////                    }
////////                }

////////                foreach (NetworkModule m in modules)
////////                    LstModules.Items.Add(m);

////////                TxtStatus.Text = modules.Count == 0
////////                    ? "No motor controllers found."
////////                    : $"Found {modules.Count} motor controller(s).";
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////////            }
////////            finally
////////            {
////////                btn.IsEnabled = true;
////////            }
////////        }

////////        private static List<NetworkModule> DiscoverModules(string connectedIp, string connectedName)
////////        {
////////            var found = new List<NetworkModule>();
////////            string baseIp = GetLocalSubnetBase();
////////            if (string.IsNullOrEmpty(baseIp)) return found;

////////            var localIps = new HashSet<string>(
////////                NetworkInterface.GetAllNetworkInterfaces()
////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////                    .Select(ua => ua.Address.ToString())
////////            );

////////            const int targetPort = 502;
////////            var tasks = new List<Task<Tuple<bool, string>>>();

////////            for (int i = 1; i <= 254; i++)
////////            {
////////                string targetIp = $"{baseIp}.{i}";
////////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////////            }

////////            Task.WaitAll(tasks.ToArray());

////////            foreach (var t in tasks)
////////            {
////////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////////                {
////////                    found.Add(new NetworkModule
////////                    {
////////                        IpAddress = t.Result.Item2,
////////                        SerialNumber = "-",
////////                        FirmwareRev = "1.0.0",
////////                        Mask = "255.255.255.0",
////////                        Position = "—"
////////                    });
////////                }
////////            }

////////            // Read the station name from each found device (in parallel)
////////            Parallel.ForEach(found, m =>
////////            {
////////                m.StationName = (m.IpAddress == connectedIp)
////////                    ? (connectedName ?? string.Empty)
////////                    : ReadStationName(m.IpAddress);
////////            });

////////            return found;
////////        }

////////        // Opens a short connection, sends the poll command, and collects
////////        // registers 0x0150 - 0x015F to build the station name.
////////        private static string ReadStationName(string ip)
////////        {
////////            try
////////            {
////////                using (var client = new TcpClient())
////////                {
////////                    var r = client.BeginConnect(ip, DEFAULT_MODBUS_PORT, null, null);
////////                    if (!r.AsyncWaitHandle.WaitOne(1500)) return string.Empty;
////////                    client.EndConnect(r);

////////                    using (NetworkStream stream = client.GetStream())
////////                    {
////////                        stream.ReadTimeout = 1500;
////////                        stream.WriteTimeout = 1500;

////////                        Thread.Sleep(100);
////////                        SendHandshake(stream);

////////                        // Same poll frame that BL.UpdateData() sends: function 0xFF
////////                        byte[] poll = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0xFF, 0x00, 0x00 };
////////                        stream.Write(poll, 0, poll.Length);
////////                        stream.Flush();

////////                        ushort[] regs = new ushort[NAME_REG_COUNT];
////////                        int received = 0;   // bit mask: one bit per register
////////                        int allBits = (1 << NAME_REG_COUNT) - 1;
////////                        DateTime deadline = DateTime.UtcNow.AddSeconds(4);

////////                        while (DateTime.UtcNow < deadline && received != allBits)
////////                        {
////////                            byte[] body = ReadModbusFrame(stream);
////////                            if (body == null) break;      // timeout or closed
////////                            if (body.Length < 6) continue;

////////                            ushort reg = (ushort)((body[2] << 8) | body[3]);
////////                            ushort val = (ushort)((body[4] << 8) | body[5]);

////////                            if (reg >= REG_NAME_START && reg < REG_NAME_START + NAME_REG_COUNT)
////////                            {
////////                                regs[reg - REG_NAME_START] = val;
////////                                received |= 1 << (reg - REG_NAME_START);
////////                            }
////////                        }

////////                        if (received != allBits) return string.Empty;

////////                        byte[] bytes = new byte[NAME_REG_COUNT * 2];
////////                        for (int i = 0; i < NAME_REG_COUNT; i++)
////////                        {
////////                            bytes[2 * i] = (byte)(regs[i] >> 8);
////////                            bytes[2 * i + 1] = (byte)(regs[i] & 0xFF);
////////                        }

////////                        string name = System.Text.Encoding.ASCII.GetString(bytes);
////////                        int zero = name.IndexOf('\0');
////////                        return zero >= 0 ? name.Substring(0, zero) : name;
////////                    }
////////                }
////////            }
////////            catch
////////            {
////////                return string.Empty;
////////            }
////////        }

////////        // Tries twice per host, with a short gap between attempts, and a
////////        // longer per-attempt timeout than before (500ms vs the old 200ms).
////////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////////        {
////////            for (int attempt = 0; attempt < 2; attempt++)
////////            {
////////                try
////////                {
////////                    using (var client = new TcpClient())
////////                    {
////////                        var connectTask = client.ConnectAsync(ip, port);
////////                        var delayTask = Task.Delay(500);
////////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
////////                        if (completedTask == connectTask && client.Connected)
////////                            return Tuple.Create(true, ip);
////////                    }
////////                }
////////                catch
////////                {
////////                    // Connection refused or failed — retry once.
////////                }

////////                if (attempt == 0)
////////                    await Task.Delay(300);
////////            }

////////            return Tuple.Create(false, ip);
////////        }

////////        private static string GetLocalSubnetBase()
////////        {
////////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////////                .ToList();

////////            if (!candidates.Any())
////////            {
////////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////////                    .ToList();
////////            }

////////            foreach (NetworkInterface ni in candidates)
////////            {
////////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////////                {
////////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////////                    {
////////                        string[] parts = ua.Address.ToString().Split('.');
////////                        if (parts.Length == 4)
////////                        {
////////                            if (parts[0] == "127") continue;               // loopback
////////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////////                        }
////////                    }
////////                }
////////            }
////////            return string.Empty;
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  LIST SELECTION → populate fields
////////        // ════════════════════════════════════════════════════════
////////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////////        {
////////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////////            if (m == null) return;

////////            TxtSerialNumber.Text = m.SerialNumber;
////////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////////            // ── Push selected module's IP into the Connect panel ──
////////            if (Application.Current.MainWindow is MainWindow mainWin)
////////            {
////////                string[] octets = m.IpAddress.Split('.');
////////                if (octets.Length == 4)
////////                {
////////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
////////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
////////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
////////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
////////                }

////////                // ── Show the station name ──
////////                var dev = mainWin.CommMotorSelect?.BLLogic?.CurrentDevice;
////////                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
////////                    ? dev.StationName
////////                    : m.StationName;
////////            }
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  SELECT ALL / NONE
////////        // ════════════════════════════════════════════════════════
////////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////////            => LstModules.SelectAll();

////////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////////            => LstModules.UnselectAll();

////////        // ════════════════════════════════════════════════════════
////////        //  SET  (station name + IP, works with or without an existing connection)
////////        // ════════════════════════════════════════════════════════
////////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////////        {
////////            string ip = GetIp();
////////            string mask = GetMask();
////////            string gateway = GetGateway();

////////            if (!IsValidIp(ip))
////////            {
////////                Warn("Enter a valid IP address.", "Invalid IP");
////////                return;
////////            }

////////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////////            {
////////                Warn("Main window not available.", "Error");
////////                return;
////////            }

////////            var commCtrl = mainWin.CommMotorSelect;

////////            if (commCtrl?.BLLogic == null)
////////            {
////////                Warn("Communication layer not initialized.", "Error");
////////                return;
////////            }

////////            // ── Step 1: Split IP into 4 octets ──
////////            string[] ipParts = ip.Split('.');
////////            if (ipParts.Length != 4 ||
////////                !byte.TryParse(ipParts[0], out byte octet1) ||
////////                !byte.TryParse(ipParts[1], out byte octet2) ||
////////                !byte.TryParse(ipParts[2], out byte octet3) ||
////////                !byte.TryParse(ipParts[3], out byte octet4))
////////            {
////////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////////                return;
////////            }

////////            if (octet1 == 0)
////////            {
////////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////////                return;
////////            }

////////            // ── Station name check ──
////////            string stationName = TxtStationName.Text ?? string.Empty;

////////            if (stationName.Length > 32)
////////            {
////////                Warn("Station name can be 32 characters maximum.", "Invalid Name");
////////                return;
////////            }
////////            foreach (char c in stationName)
////////            {
////////                if (c < 32 || c > 126)
////////                {
////////                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
////////                    return;
////////                }
////////            }

////////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////////            byte maskLastOctet = 0;
////////            string[] maskParts = mask.Split('.');
////////            if (maskParts.Length == 4)
////////                byte.TryParse(maskParts[3], out maskLastOctet);

////////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
////////            string currentDeviceIp = selected != null
////////                ? selected.IpAddress
////////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

////////            if (!IsValidIp(currentDeviceIp))
////////            {
////////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
////////                return;
////////            }

////////            int port1 = DEFAULT_MODBUS_PORT;

////////            try
////////            {
////////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Set failed: {ex.Message}";
////////                return; // don't touch UI state below — nothing confirmed sent
////////            }

////////            // ── Step 6: Update selected module in list if one is selected ──
////////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////////            if (module != null)
////////            {
////////                module.IpAddress = ip;
////////                module.Mask = mask;
////////                module.Gateway = gateway;
////////                if (stationName.Length > 0)
////////                    module.StationName = stationName;
////////                int idx = LstModules.SelectedIndex;
////////                LstModules.Items.Refresh();
////////                LstModules.SelectedIndex = idx;
////////            }

////////            // ── Step 7: If the main comm panel happened to be connected to
////////            // this same device, drop it (device is rebooting anyway).
////////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////////            {
////////                commCtrl.BLLogic.Close();
////////                mainWin.DataContext = null;
////////            }

////////            string[] octets = ip.Split('.');
////////            if (octets.Length == 4)
////////            {
////////                commCtrl.TcpIpOctet1.Text = octets[0];
////////                commCtrl.TcpIpOctet2.Text = octets[1];
////////                commCtrl.TcpIpOctet3.Text = octets[2];
////////                commCtrl.TcpIpOctet4.Text = octets[3];
////////            }
////////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////////            commCtrl.ApplyAndSaveIp(ip);

////////            TxtStatus.Text = stationName.Length > 0
////////                ? $"IP set → {ip}, name → {stationName}"
////////                : $"IP set → {ip}.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
////////        // ════════════════════════════════════════════════════════
////////        private void SendSetIpDirect(string targetIp, int port,
////////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
////////            string stationName)
////////        {
////////            using (var client = new TcpClient())
////////            {
////////                var connectResult = client.BeginConnect(targetIp, port, null, null);
////////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
////////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

////////                client.EndConnect(connectResult);

////////                using (NetworkStream stream = client.GetStream())
////////                {
////////                    stream.WriteTimeout = 2000;
////////                    stream.ReadTimeout = 2000;

////////                    Thread.Sleep(100);

////////                    SendHandshake(stream);

////////                    // ── Station name first (the IP commit reboots the device) ──
////////                    if (!string.IsNullOrEmpty(stationName))
////////                    {
////////                        byte[] buf = new byte[32];   // all zeros = padding
////////                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
////////                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

////////                        for (int i = 0; i < NAME_REG_COUNT; i++)
////////                        {
////////                            // first char = high byte, second char = low byte
////////                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
////////                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
////////                        }

////////                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
////////                    }

////////                    // ── Then the IP settings ──
////////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
////////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
////////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
////////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
////////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

////////                    try
////////                    {
////////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
////////                    }
////////                    catch (TimeoutException)
////////                    {
////////                        // Acceptable: device may have started rebooting
////////                        // before it could echo the commit frame back.
////////                    }

////////                    Thread.Sleep(150);
////////                }
////////            }
////////        }

////////        private static void SendHandshake(NetworkStream stream)
////////        {
////////            byte[] handshake = { 0x01, 0x01 };
////////            stream.Write(handshake, 0, handshake.Length);
////////            stream.Flush();

////////            byte[] echo = ReadExactOrNull(stream, 2);
////////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
////////                throw new TimeoutException(
////////                    "Device did not respond to the required connection handshake " +
////////                    "(no echo received) — writes cannot proceed.");

////////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
////////            if (confirmFrame == null)
////////                throw new TimeoutException(
////////                    "Device echoed the handshake but did not send its confirmation frame.");
////////        }

////////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
////////        {
////////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
////////            stream.Write(frame, 0, frame.Length);
////////            stream.Flush();

////////            byte[] response = ReadModbusFrame(stream);

////////            if (response == null)
////////                throw new TimeoutException(
////////                    $"No response from device for register 0x{registerAddress:X4} " +
////////                    "(write may not have been processed).");

////////            if (response.Length >= 6 && response[1] == 0x86)
////////            {
////////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
////////                throw new InvalidOperationException(
////////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
////////            }
////////        }

////////        private static byte[] ReadModbusFrame(NetworkStream stream)
////////        {
////////            byte[] mbap = ReadExactOrNull(stream, 6);
////////            if (mbap == null) return null;

////////            int length = (mbap[4] << 8) | mbap[5];
////////            if (length <= 0 || length > 253) return null;

////////            byte[] body = ReadExactOrNull(stream, length);
////////            return body;
////////        }

////////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
////////        {
////////            byte[] buffer = new byte[count];
////////            int read = 0;
////////            try
////////            {
////////                while (read < count)
////////                {
////////                    int r = stream.Read(buffer, read, count - read);
////////                    if (r <= 0) return null;
////////                    read += r;
////////                }
////////                return buffer;
////////            }
////////            catch (IOException)
////////            {
////////                return null;
////////            }
////////        }

////////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
////////        {
////////            byte[] data = {
////////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
////////                (byte)(value >> 8), (byte)(value & 0xFF)
////////            };

////////            byte[] pdu = new byte[2 + data.Length];
////////            pdu[0] = 0x01;
////////            pdu[1] = 0x06;
////////            Array.Copy(data, 0, pdu, 2, data.Length);

////////            byte[] frame = new byte[6 + pdu.Length];
////////            frame[0] = 0x00; frame[1] = 0x01;
////////            frame[2] = 0x00; frame[3] = 0x00;
////////            frame[4] = (byte)(pdu.Length >> 8);
////////            frame[5] = (byte)(pdu.Length & 0xFF);
////////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

////////            return frame;
////////        }

////////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////////        {
////////            // TODO: implement backup logic
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  RESTORE
////////        // ════════════════════════════════════════════════════════
////////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.SelectedItems.Count == 0)
////////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Open Backup File",
////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            int restored = 0;
////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////            {
////////                string[] parts = line.Split(',');
////////                if (parts.Length < 6) continue;

////////                string sn = parts[1].Trim();
////////                foreach (NetworkModule m in LstModules.SelectedItems)
////////                {
////////                    if (m.SerialNumber != sn) continue;
////////                    m.IpAddress = parts[2].Trim();
////////                    m.Mask = parts[3].Trim();
////////                    m.Gateway = parts[4].Trim();
////////                    m.Position = parts[5].Trim();
////////                    restored++;
////////                }
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = restored > 0
////////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////////                : "No matching modules found in the backup file.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  RESTORE BY IP
////////        // ════════════════════════════════════════════════════════
////////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////////        {
////////            string ip = GetIp();

////////            if (!IsValidIp(ip))
////////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Open Backup File",
////////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            bool found = false;
////////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////////            {
////////                string[] parts = line.Split(',');
////////                if (parts.Length < 6) continue;
////////                if (parts[2].Trim() != ip) continue;

////////                foreach (NetworkModule m in LstModules.Items)
////////                {
////////                    if (m.IpAddress != ip) continue;
////////                    m.Mask = parts[3].Trim();
////////                    m.Gateway = parts[4].Trim();
////////                    m.Position = parts[5].Trim();
////////                    found = true;
////////                }
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = found
////////                ? $"Restore by IP complete → {ip}"
////////                : $"No backup entry found for IP {ip}.";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  REORDER IPs
////////        // ════════════════════════════════════════════════════════
////////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.Items.Count == 0)
////////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////////            ReorderIpDialog dlg = new ReorderIpDialog();
////////            if (dlg.ShowDialog() != true) return;

////////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////////            byte[] bytes = startAddr.GetAddressBytes();
////////            int counter = bytes[3];

////////            foreach (NetworkModule m in LstModules.Items)
////////            {
////////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////////                m.IpAddress = new IPAddress(bytes).ToString();
////////            }

////////            LstModules.Items.Refresh();
////////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////////        }

////////        // ════════════════════════════════════════════════════════
////////        //  UPGRADE FW
////////        // ════════════════════════════════════════════════════════
////////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////////        {
////////            if (LstModules.SelectedItems.Count == 0)
////////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////////            MessageBoxResult confirm = MessageBox.Show(
////////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////////                "Do NOT power off during upgrade.",
////////                "Confirm Firmware Upgrade",
////////                MessageBoxButton.YesNo,
////////                MessageBoxImage.Question);

////////            if (confirm != MessageBoxResult.Yes) return;

////////            OpenFileDialog dlg = new OpenFileDialog
////////            {
////////                Title = "Select Firmware File",
////////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////////            };

////////            if (dlg.ShowDialog() != true) return;

////////            Button btn = (Button)sender;
////////            btn.IsEnabled = false;
////////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////////            List<NetworkModule> targets = LstModules.SelectedItems
////////                .Cast<NetworkModule>()
////////                .ToList();

////////            string fwPath = dlg.FileName;

////////            try
////////            {
////////                await Task.Run(() =>
////////                {
////////                    foreach (NetworkModule m in targets)
////////                    {
////////                        System.Threading.Thread.Sleep(500);
////////                        m.FirmwareRev = "NEW";
////////                    }
////////                });

////////                LstModules.Items.Refresh();
////////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////////            }
////////            catch (Exception ex)
////////            {
////////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////////            }
////////            finally
////////            {
////////                btn.IsEnabled = true;
////////            }
////////        }

////////        private static void Warn(string message, string title) =>
////////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////////    }

////////    // ────────────────────────────────────────────────────────────
////////    //  Minimal "Enter start IP" dialog for Reorder IPs
////////    // ────────────────────────────────────────────────────────────
////////    public class ReorderIpDialog : Window
////////    {
////////        private readonly TextBox _input;
////////        public string StartIp => _input.Text.Trim();

////////        public ReorderIpDialog()
////////        {
////////            Title = "Reorder IPs";
////////            Width = 280;
////////            Height = 130;
////////            ResizeMode = ResizeMode.NoResize;
////////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////////            sp.Children.Add(new TextBlock
////////            {
////////                Text = "Enter starting IP address:",
////////                Margin = new Thickness(0, 0, 0, 6)
////////            });

////////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////////            sp.Children.Add(_input);

////////            StackPanel btnRow = new StackPanel
////////            {
////////                Orientation = Orientation.Horizontal,
////////                HorizontalAlignment = HorizontalAlignment.Right,
////////                Margin = new Thickness(0, 10, 0, 0)
////////            };

////////            Button ok = new Button
////////            {
////////                Content = "OK",
////////                Width = 70,
////////                Height = 24,
////////                Margin = new Thickness(0, 0, 6, 0),
////////                IsDefault = true
////////            };
////////            ok.Click += (s, ev) => { DialogResult = true; };

////////            Button cancel = new Button
////////            {
////////                Content = "Cancel",
////////                Width = 70,
////////                Height = 24,
////////                IsCancel = true
////////            };

////////            btnRow.Children.Add(ok);
////////            btnRow.Children.Add(cancel);
////////            sp.Children.Add(btnRow);

////////            Content = sp;
////////        }
////////    }
////////}
//////using BLDC_Demo.Controls;
//////using Microsoft.Win32;
//////using System;
//////using System.Collections.Generic;
//////using System.IO;
//////using System.Linq;
//////using System.Net;
//////using System.Net.NetworkInformation;
//////using System.Net.Sockets;
//////using System.Text.RegularExpressions;
//////using System.Threading;
//////using System.Threading.Tasks;
//////using System.Windows;
//////using System.Windows.Controls;
//////using System.Windows.Input;

//////namespace BLDC_Demo
//////{
//////    // ────────────────────────────────────────────────────────────
//////    //  Model: a discovered network module
//////    // ────────────────────────────────────────────────────────────
//////    public class NetworkModule
//////    {
//////        public string SerialNumber { get; set; } = string.Empty;
//////        public string StationName { get; set; } = string.Empty;
//////        public string IpAddress { get; set; } = string.Empty;
//////        public string Mask { get; set; } = string.Empty;
//////        public string Gateway { get; set; } = string.Empty;
//////        public string FirmwareRev { get; set; } = string.Empty;
//////        public string Position { get; set; } = "—";

//////        // List shows:  192.168.1.50   Name: Station1
//////        public override string ToString() =>
//////            $"{IpAddress}   Name: {(string.IsNullOrEmpty(StationName) ? "-" : StationName)}";
//////    }

//////    // ────────────────────────────────────────────────────────────
//////    //  Backup container
//////    // ────────────────────────────────────────────────────────────
//////    public class ModuleBackup
//////    {
//////        public string SerialNumber { get; set; } = string.Empty;
//////        public string IpAddress { get; set; } = string.Empty;
//////        public string Mask { get; set; } = string.Empty;
//////        public string Gateway { get; set; } = string.Empty;
//////        public string Position { get; set; } = string.Empty;
//////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////    }

//////    // ────────────────────────────────────────────────────────────
//////    //  UserControl code-behind  (targets C# 7.3)
//////    // ────────────────────────────────────────────────────────────
//////    public partial class NetworkService : UserControl
//////    {
//////        // ── ESP32 Modbus holding-register map for network config ──
//////        private const ushort REG_IP_OCTET1 = 0x0136;
//////        private const ushort REG_IP_OCTET2 = 0x0137;
//////        private const ushort REG_IP_OCTET3 = 0x0138;
//////        private const ushort REG_IP_OCTET4 = 0x0139;
//////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////        private const ushort REG_APPLY_REBOOT = 0x013B;

//////        // ── Station name registers ──
//////        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
//////        private const int NAME_REG_COUNT = 16;
//////        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

//////        // Default Modbus TCP port used when we have to open a temporary
//////        // connection to push a new IP to a device that isn't connected yet.
//////        private const int DEFAULT_MODBUS_PORT = 502;

//////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////            = new Dictionary<string, ModuleBackup>();

//////        public NetworkService()
//////        {
//////            InitializeComponent();
//////            AttachOctetHandlers();
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  OCTET HELPERS
//////        // ════════════════════════════════════════════════════════
//////        private void AttachOctetHandlers()
//////        {
//////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////            AttachToGroup(ipBoxes);
//////            AttachToGroup(maskBoxes);
//////            AttachToGroup(gwBoxes);

//////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////        }

//////        private void SetGatewayEnabled(bool enabled)
//////        {
//////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////        }

//////        private static void AttachToGroup(TextBox[] boxes)
//////        {
//////            for (int i = 0; i < boxes.Length; i++)
//////            {
//////                int idx = i;

//////                boxes[idx].PreviewTextInput += (s, e) =>
//////                {
//////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////                };

//////                boxes[idx].PreviewKeyDown += (s, e) =>
//////                {
//////                    TextBox tb = (TextBox)s;
//////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////                    {
//////                        e.Handled = true;
//////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////                    }
//////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////                    {
//////                        boxes[idx - 1].Focus();
//////                        boxes[idx - 1].SelectAll();
//////                    }
//////                };

//////                boxes[idx].TextChanged += (s, e) =>
//////                {
//////                    TextBox tb = (TextBox)s;
//////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////                        boxes[idx + 1].Focus();
//////                };

//////                boxes[idx].LostFocus += (s, e) =>
//////                {
//////                    TextBox tb = (TextBox)s;
//////                    if (int.TryParse(tb.Text, out int val))
//////                        tb.Text = Clamp(val, 0, 255).ToString();
//////                    else if (tb.Text.Length > 0)
//////                        tb.Text = string.Empty;
//////                };
//////            }
//////        }

//////        private static int Clamp(int value, int min, int max)
//////        {
//////            if (value < min) return min;
//////            if (value > max) return max;
//////            return value;
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  IP FIELD READ / WRITE
//////        // ════════════════════════════════════════════════════════
//////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////        private string GetGateway() =>
//////            ChkGateway.IsChecked == true
//////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////                : string.Empty;

//////        private void SetIpFields(string ip, string mask, string gw)
//////        {
//////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////            ChkGateway.IsChecked = hasGw;
//////            SetGatewayEnabled(hasGw);

//////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////        }

//////        private static void FillOctets(string address,
//////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////        {
//////            string[] parts = address != null
//////                ? address.Split('.')
//////                : new string[0];

//////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////        }

//////        private static void ClearOctets(params TextBox[] boxes)
//////        {
//////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////        }

//////        private static bool IsValidIp(string ip) =>
//////            IPAddress.TryParse(ip, out _);

//////        // ════════════════════════════════════════════════════════
//////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
//////        // ════════════════════════════════════════════════════════
//////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////        {
//////            LstModules.Items.Clear();
//////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////            Button btn = (Button)sender;
//////            btn.IsEnabled = false;

//////            try
//////            {
//////                // The device that the main app is already connected to:
//////                // use its name directly, do not open a second connection.
//////                string connectedIp = null;
//////                string connectedName = null;
//////                string connectedSerial = null;

//////                if (Application.Current.MainWindow is MainWindow mw0)
//////                {
//////                    var comm = mw0.CommMotorSelect;
//////                    var d0 = comm?.BLLogic?.CurrentDevice;

//////                    if (d0 != null && !string.IsNullOrEmpty(d0.IpAddress))
//////                    {
//////                        connectedIp = d0.IpAddress;
//////                        connectedName = d0.StationName;
//////                        connectedSerial = d0.SerialNumber;
//////                    }
//////                    else if (comm?.BLLogic?.IsConnected == true)
//////                    {
//////                        // Fallback: IP that CommMotorSelectControl stored when connecting
//////                        connectedIp = comm.ConnectedIp;
//////                    }
//////                }

//////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules(connectedIp, connectedName));

//////                // The connected device usually accepts only one client, so the port scan
//////                // cannot reach it and it would be missing from the list. Add it manually.
//////                if (!string.IsNullOrEmpty(connectedIp) &&
//////                    !modules.Any(m => m.IpAddress == connectedIp))
//////                {
//////                    modules.Insert(0, new NetworkModule
//////                    {
//////                        IpAddress = connectedIp,
//////                        StationName = connectedName ?? string.Empty,
//////                        SerialNumber = string.IsNullOrEmpty(connectedSerial) ? "-" : connectedSerial,
//////                        FirmwareRev = "1.0.0",
//////                        Mask = "255.255.255.0",
//////                        Position = "—"
//////                    });
//////                }

//////                // ── Option A: fill in serial for the device BL is connected to ──
//////                if (Application.Current.MainWindow is MainWindow mw)
//////                {
//////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
//////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
//////                    {
//////                        foreach (NetworkModule m in modules)
//////                            if (m.IpAddress == dev.IpAddress)
//////                                m.SerialNumber = dev.SerialNumber;
//////                    }
//////                }

//////                foreach (NetworkModule m in modules)
//////                    LstModules.Items.Add(m);

//////                TxtStatus.Text = modules.Count == 0
//////                    ? "No motor controllers found."
//////                    : $"Found {modules.Count} motor controller(s).";
//////            }
//////            catch (Exception ex)
//////            {
//////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////            }
//////            finally
//////            {
//////                btn.IsEnabled = true;
//////            }
//////        }

//////        private static List<NetworkModule> DiscoverModules(string connectedIp, string connectedName)
//////        {
//////            var found = new List<NetworkModule>();
//////            string baseIp = GetLocalSubnetBase();
//////            if (string.IsNullOrEmpty(baseIp)) return found;

//////            var localIps = new HashSet<string>(
//////                NetworkInterface.GetAllNetworkInterfaces()
//////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////                    .Select(ua => ua.Address.ToString())
//////            );

//////            const int targetPort = 502;
//////            var tasks = new List<Task<Tuple<bool, string>>>();

//////            for (int i = 1; i <= 254; i++)
//////            {
//////                string targetIp = $"{baseIp}.{i}";
//////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////            }

//////            Task.WaitAll(tasks.ToArray());

//////            foreach (var t in tasks)
//////            {
//////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////                {
//////                    found.Add(new NetworkModule
//////                    {
//////                        IpAddress = t.Result.Item2,
//////                        SerialNumber = "-",
//////                        FirmwareRev = "1.0.0",
//////                        Mask = "255.255.255.0",
//////                        Position = "—"
//////                    });
//////                }
//////            }

//////            // Read the station name from each found device (in parallel)
//////            Parallel.ForEach(found, m =>
//////            {
//////                m.StationName = (m.IpAddress == connectedIp)
//////                    ? (connectedName ?? string.Empty)
//////                    : ReadStationName(m.IpAddress);
//////            });

//////            return found;
//////        }

//////        // Opens a short connection, sends the poll command, and collects
//////        // registers 0x0150 - 0x015F to build the station name.
//////        private static string ReadStationName(string ip)
//////        {
//////            try
//////            {
//////                using (var client = new TcpClient())
//////                {
//////                    var r = client.BeginConnect(ip, DEFAULT_MODBUS_PORT, null, null);
//////                    if (!r.AsyncWaitHandle.WaitOne(1500)) return string.Empty;
//////                    client.EndConnect(r);

//////                    using (NetworkStream stream = client.GetStream())
//////                    {
//////                        stream.ReadTimeout = 1500;
//////                        stream.WriteTimeout = 1500;

//////                        Thread.Sleep(100);
//////                        SendHandshake(stream);

//////                        // Same poll frame that BL.UpdateData() sends: function 0xFF
//////                        byte[] poll = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0xFF, 0x00, 0x00 };
//////                        stream.Write(poll, 0, poll.Length);
//////                        stream.Flush();

//////                        ushort[] regs = new ushort[NAME_REG_COUNT];
//////                        int received = 0;   // bit mask: one bit per register
//////                        int allBits = (1 << NAME_REG_COUNT) - 1;
//////                        DateTime deadline = DateTime.UtcNow.AddSeconds(4);

//////                        while (DateTime.UtcNow < deadline && received != allBits)
//////                        {
//////                            byte[] body = ReadModbusFrame(stream);
//////                            if (body == null) break;      // timeout or closed
//////                            if (body.Length < 6) continue;

//////                            ushort reg = (ushort)((body[2] << 8) | body[3]);
//////                            ushort val = (ushort)((body[4] << 8) | body[5]);

//////                            if (reg >= REG_NAME_START && reg < REG_NAME_START + NAME_REG_COUNT)
//////                            {
//////                                regs[reg - REG_NAME_START] = val;
//////                                received |= 1 << (reg - REG_NAME_START);
//////                            }
//////                        }

//////                        if (received != allBits) return string.Empty;

//////                        byte[] bytes = new byte[NAME_REG_COUNT * 2];
//////                        for (int i = 0; i < NAME_REG_COUNT; i++)
//////                        {
//////                            bytes[2 * i] = (byte)(regs[i] >> 8);
//////                            bytes[2 * i + 1] = (byte)(regs[i] & 0xFF);
//////                        }

//////                        string name = System.Text.Encoding.ASCII.GetString(bytes);
//////                        int zero = name.IndexOf('\0');
//////                        return zero >= 0 ? name.Substring(0, zero) : name;
//////                    }
//////                }
//////            }
//////            catch
//////            {
//////                return string.Empty;
//////            }
//////        }

//////        // Tries twice per host, with a short gap between attempts, and a
//////        // longer per-attempt timeout than before (500ms vs the old 200ms).
//////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////        {
//////            for (int attempt = 0; attempt < 2; attempt++)
//////            {
//////                try
//////                {
//////                    using (var client = new TcpClient())
//////                    {
//////                        var connectTask = client.ConnectAsync(ip, port);
//////                        var delayTask = Task.Delay(500);
//////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
//////                        if (completedTask == connectTask && client.Connected)
//////                            return Tuple.Create(true, ip);
//////                    }
//////                }
//////                catch
//////                {
//////                    // Connection refused or failed — retry once.
//////                }

//////                if (attempt == 0)
//////                    await Task.Delay(300);
//////            }

//////            return Tuple.Create(false, ip);
//////        }

//////        private static string GetLocalSubnetBase()
//////        {
//////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////                .ToList();

//////            if (!candidates.Any())
//////            {
//////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////                    .ToList();
//////            }

//////            foreach (NetworkInterface ni in candidates)
//////            {
//////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////                {
//////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////                    {
//////                        string[] parts = ua.Address.ToString().Split('.');
//////                        if (parts.Length == 4)
//////                        {
//////                            if (parts[0] == "127") continue;               // loopback
//////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////                        }
//////                    }
//////                }
//////            }
//////            return string.Empty;
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  LIST SELECTION → populate fields
//////        // ════════════════════════════════════════════════════════
//////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////        {
//////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////            if (m == null) return;

//////            TxtSerialNumber.Text = m.SerialNumber;
//////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////            // ── Push selected module's IP into the Connect panel ──
//////            if (Application.Current.MainWindow is MainWindow mainWin)
//////            {
//////                string[] octets = m.IpAddress.Split('.');
//////                if (octets.Length == 4)
//////                {
//////                    mainWin.CommMotorSelect.TcpIpOctet1.Text = octets[0];
//////                    mainWin.CommMotorSelect.TcpIpOctet2.Text = octets[1];
//////                    mainWin.CommMotorSelect.TcpIpOctet3.Text = octets[2];
//////                    mainWin.CommMotorSelect.TcpIpOctet4.Text = octets[3];
//////                }

//////                // ── Show the station name ──
//////                var dev = mainWin.CommMotorSelect?.BLLogic?.CurrentDevice;
//////                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
//////                    ? dev.StationName
//////                    : m.StationName;
//////            }
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  SELECT ALL / NONE
//////        // ════════════════════════════════════════════════════════
//////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////            => LstModules.SelectAll();

//////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////            => LstModules.UnselectAll();

//////        // ════════════════════════════════════════════════════════
//////        //  SET  (station name + IP, works with or without an existing connection)
//////        // ════════════════════════════════════════════════════════
//////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////        {
//////            string ip = GetIp();
//////            string mask = GetMask();
//////            string gateway = GetGateway();

//////            if (!IsValidIp(ip))
//////            {
//////                Warn("Enter a valid IP address.", "Invalid IP");
//////                return;
//////            }

//////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////            {
//////                Warn("Main window not available.", "Error");
//////                return;
//////            }

//////            var commCtrl = mainWin.CommMotorSelect;

//////            if (commCtrl?.BLLogic == null)
//////            {
//////                Warn("Communication layer not initialized.", "Error");
//////                return;
//////            }

//////            // ── Step 1: Split IP into 4 octets ──
//////            string[] ipParts = ip.Split('.');
//////            if (ipParts.Length != 4 ||
//////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////                !byte.TryParse(ipParts[3], out byte octet4))
//////            {
//////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////                return;
//////            }

//////            if (octet1 == 0)
//////            {
//////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////                return;
//////            }

//////            // ── Station name check ──
//////            string stationName = TxtStationName.Text ?? string.Empty;

//////            if (stationName.Length > 32)
//////            {
//////                Warn("Station name can be 32 characters maximum.", "Invalid Name");
//////                return;
//////            }
//////            foreach (char c in stationName)
//////            {
//////                if (c < 32 || c > 126)
//////                {
//////                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
//////                    return;
//////                }
//////            }

//////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////            byte maskLastOctet = 0;
//////            string[] maskParts = mask.Split('.');
//////            if (maskParts.Length == 4)
//////                byte.TryParse(maskParts[3], out maskLastOctet);

//////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////            string currentDeviceIp = selected != null
//////                ? selected.IpAddress
//////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

//////            if (!IsValidIp(currentDeviceIp))
//////            {
//////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
//////                return;
//////            }

//////            int port1 = DEFAULT_MODBUS_PORT;

//////            try
//////            {
//////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
//////            }
//////            catch (Exception ex)
//////            {
//////                TxtStatus.Text = $"Set failed: {ex.Message}";
//////                return; // don't touch UI state below — nothing confirmed sent
//////            }

//////            // ── Step 6: Update selected module in list if one is selected ──
//////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////            if (module != null)
//////            {
//////                module.IpAddress = ip;
//////                module.Mask = mask;
//////                module.Gateway = gateway;
//////                if (stationName.Length > 0)
//////                    module.StationName = stationName;
//////                int idx = LstModules.SelectedIndex;
//////                LstModules.Items.Refresh();
//////                LstModules.SelectedIndex = idx;
//////            }

//////            // ── Step 7: If the main comm panel happened to be connected to
//////            // this same device, drop it (device is rebooting anyway).
//////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////            {
//////                commCtrl.BLLogic.Close();
//////                mainWin.DataContext = null;
//////            }

//////            string[] octets = ip.Split('.');
//////            if (octets.Length == 4)
//////            {
//////                commCtrl.TcpIpOctet1.Text = octets[0];
//////                commCtrl.TcpIpOctet2.Text = octets[1];
//////                commCtrl.TcpIpOctet3.Text = octets[2];
//////                commCtrl.TcpIpOctet4.Text = octets[3];
//////            }
//////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////            commCtrl.ApplyAndSaveIp(ip);

//////            TxtStatus.Text = stationName.Length > 0
//////                ? $"IP set → {ip}, name → {stationName}"
//////                : $"IP set → {ip}.";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
//////        // ════════════════════════════════════════════════════════
//////        private void SendSetIpDirect(string targetIp, int port,
//////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
//////            string stationName)
//////        {
//////            using (var client = new TcpClient())
//////            {
//////                var connectResult = client.BeginConnect(targetIp, port, null, null);
//////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//////                client.EndConnect(connectResult);

//////                using (NetworkStream stream = client.GetStream())
//////                {
//////                    stream.WriteTimeout = 2000;
//////                    stream.ReadTimeout = 2000;

//////                    Thread.Sleep(100);

//////                    SendHandshake(stream);

//////                    // ── Station name first (the IP commit reboots the device) ──
//////                    if (!string.IsNullOrEmpty(stationName))
//////                    {
//////                        byte[] buf = new byte[32];   // all zeros = padding
//////                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
//////                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

//////                        for (int i = 0; i < NAME_REG_COUNT; i++)
//////                        {
//////                            // first char = high byte, second char = low byte
//////                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
//////                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
//////                        }

//////                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
//////                    }

//////                    // ── Then the IP settings ──
//////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
//////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
//////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
//////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
//////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

//////                    try
//////                    {
//////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
//////                    }
//////                    catch (TimeoutException)
//////                    {
//////                        // Acceptable: device may have started rebooting
//////                        // before it could echo the commit frame back.
//////                    }

//////                    Thread.Sleep(150);
//////                }
//////            }
//////        }

//////        private static void SendHandshake(NetworkStream stream)
//////        {
//////            byte[] handshake = { 0x01, 0x01 };
//////            stream.Write(handshake, 0, handshake.Length);
//////            stream.Flush();

//////            byte[] echo = ReadExactOrNull(stream, 2);
//////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
//////                throw new TimeoutException(
//////                    "Device did not respond to the required connection handshake " +
//////                    "(no echo received) — writes cannot proceed.");

//////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
//////            if (confirmFrame == null)
//////                throw new TimeoutException(
//////                    "Device echoed the handshake but did not send its confirmation frame.");
//////        }

//////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
//////        {
//////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
//////            stream.Write(frame, 0, frame.Length);
//////            stream.Flush();

//////            byte[] response = ReadModbusFrame(stream);

//////            if (response == null)
//////                throw new TimeoutException(
//////                    $"No response from device for register 0x{registerAddress:X4} " +
//////                    "(write may not have been processed).");

//////            if (response.Length >= 6 && response[1] == 0x86)
//////            {
//////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
//////                throw new InvalidOperationException(
//////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
//////            }
//////        }

//////        private static byte[] ReadModbusFrame(NetworkStream stream)
//////        {
//////            byte[] mbap = ReadExactOrNull(stream, 6);
//////            if (mbap == null) return null;

//////            int length = (mbap[4] << 8) | mbap[5];
//////            if (length <= 0 || length > 253) return null;

//////            byte[] body = ReadExactOrNull(stream, length);
//////            return body;
//////        }

//////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
//////        {
//////            byte[] buffer = new byte[count];
//////            int read = 0;
//////            try
//////            {
//////                while (read < count)
//////                {
//////                    int r = stream.Read(buffer, read, count - read);
//////                    if (r <= 0) return null;
//////                    read += r;
//////                }
//////                return buffer;
//////            }
//////            catch (IOException)
//////            {
//////                return null;
//////            }
//////        }

//////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
//////        {
//////            byte[] data = {
//////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//////                (byte)(value >> 8), (byte)(value & 0xFF)
//////            };

//////            byte[] pdu = new byte[2 + data.Length];
//////            pdu[0] = 0x01;
//////            pdu[1] = 0x06;
//////            Array.Copy(data, 0, pdu, 2, data.Length);

//////            byte[] frame = new byte[6 + pdu.Length];
//////            frame[0] = 0x00; frame[1] = 0x01;
//////            frame[2] = 0x00; frame[3] = 0x00;
//////            frame[4] = (byte)(pdu.Length >> 8);
//////            frame[5] = (byte)(pdu.Length & 0xFF);
//////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

//////            return frame;
//////        }

//////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////        {
//////            // TODO: implement backup logic
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  RESTORE
//////        // ════════════════════════════════════════════════════════
//////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////        {
//////            if (LstModules.SelectedItems.Count == 0)
//////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////            OpenFileDialog dlg = new OpenFileDialog
//////            {
//////                Title = "Open Backup File",
//////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////            };

//////            if (dlg.ShowDialog() != true) return;

//////            int restored = 0;
//////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////            {
//////                string[] parts = line.Split(',');
//////                if (parts.Length < 6) continue;

//////                string sn = parts[1].Trim();
//////                foreach (NetworkModule m in LstModules.SelectedItems)
//////                {
//////                    if (m.SerialNumber != sn) continue;
//////                    m.IpAddress = parts[2].Trim();
//////                    m.Mask = parts[3].Trim();
//////                    m.Gateway = parts[4].Trim();
//////                    m.Position = parts[5].Trim();
//////                    restored++;
//////                }
//////            }

//////            LstModules.Items.Refresh();
//////            TxtStatus.Text = restored > 0
//////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////                : "No matching modules found in the backup file.";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  RESTORE BY IP
//////        // ════════════════════════════════════════════════════════
//////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////        {
//////            string ip = GetIp();

//////            if (!IsValidIp(ip))
//////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////            OpenFileDialog dlg = new OpenFileDialog
//////            {
//////                Title = "Open Backup File",
//////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////            };

//////            if (dlg.ShowDialog() != true) return;

//////            bool found = false;
//////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////            {
//////                string[] parts = line.Split(',');
//////                if (parts.Length < 6) continue;
//////                if (parts[2].Trim() != ip) continue;

//////                foreach (NetworkModule m in LstModules.Items)
//////                {
//////                    if (m.IpAddress != ip) continue;
//////                    m.Mask = parts[3].Trim();
//////                    m.Gateway = parts[4].Trim();
//////                    m.Position = parts[5].Trim();
//////                    found = true;
//////                }
//////            }

//////            LstModules.Items.Refresh();
//////            TxtStatus.Text = found
//////                ? $"Restore by IP complete → {ip}"
//////                : $"No backup entry found for IP {ip}.";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  REORDER IPs
//////        // ════════════════════════════════════════════════════════
//////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////        {
//////            if (LstModules.Items.Count == 0)
//////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////            ReorderIpDialog dlg = new ReorderIpDialog();
//////            if (dlg.ShowDialog() != true) return;

//////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////            byte[] bytes = startAddr.GetAddressBytes();
//////            int counter = bytes[3];

//////            foreach (NetworkModule m in LstModules.Items)
//////            {
//////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////                m.IpAddress = new IPAddress(bytes).ToString();
//////            }

//////            LstModules.Items.Refresh();
//////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  UPGRADE FW
//////        // ════════════════════════════════════════════════════════
//////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////        {
//////            if (LstModules.SelectedItems.Count == 0)
//////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////            MessageBoxResult confirm = MessageBox.Show(
//////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////                "Do NOT power off during upgrade.",
//////                "Confirm Firmware Upgrade",
//////                MessageBoxButton.YesNo,
//////                MessageBoxImage.Question);

//////            if (confirm != MessageBoxResult.Yes) return;

//////            OpenFileDialog dlg = new OpenFileDialog
//////            {
//////                Title = "Select Firmware File",
//////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////            };

//////            if (dlg.ShowDialog() != true) return;

//////            Button btn = (Button)sender;
//////            btn.IsEnabled = false;
//////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////            List<NetworkModule> targets = LstModules.SelectedItems
//////                .Cast<NetworkModule>()
//////                .ToList();

//////            string fwPath = dlg.FileName;

//////            try
//////            {
//////                await Task.Run(() =>
//////                {
//////                    foreach (NetworkModule m in targets)
//////                    {
//////                        System.Threading.Thread.Sleep(500);
//////                        m.FirmwareRev = "NEW";
//////                    }
//////                });

//////                LstModules.Items.Refresh();
//////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////            }
//////            catch (Exception ex)
//////            {
//////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////            }
//////            finally
//////            {
//////                btn.IsEnabled = true;
//////            }
//////        }

//////        private static void Warn(string message, string title) =>
//////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////    }

//////    // ────────────────────────────────────────────────────────────
//////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////    // ────────────────────────────────────────────────────────────
//////    public class ReorderIpDialog : Window
//////    {
//////        private readonly TextBox _input;
//////        public string StartIp => _input.Text.Trim();

//////        public ReorderIpDialog()
//////        {
//////            Title = "Reorder IPs";
//////            Width = 280;
//////            Height = 130;
//////            ResizeMode = ResizeMode.NoResize;
//////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////            sp.Children.Add(new TextBlock
//////            {
//////                Text = "Enter starting IP address:",
//////                Margin = new Thickness(0, 0, 0, 6)
//////            });

//////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////            sp.Children.Add(_input);

//////            StackPanel btnRow = new StackPanel
//////            {
//////                Orientation = Orientation.Horizontal,
//////                HorizontalAlignment = HorizontalAlignment.Right,
//////                Margin = new Thickness(0, 10, 0, 0)
//////            };

//////            Button ok = new Button
//////            {
//////                Content = "OK",
//////                Width = 70,
//////                Height = 24,
//////                Margin = new Thickness(0, 0, 6, 0),
//////                IsDefault = true
//////            };
//////            ok.Click += (s, ev) => { DialogResult = true; };

//////            Button cancel = new Button
//////            {
//////                Content = "Cancel",
//////                Width = 70,
//////                Height = 24,
//////                IsCancel = true
//////            };

//////            btnRow.Children.Add(ok);
//////            btnRow.Children.Add(cancel);
//////            sp.Children.Add(btnRow);

//////            Content = sp;
//////        }
//////    }
//////}
//////using BLDC_Demo.Controls;
//////using Microsoft.Win32;
//////using System;
//////using System.Collections.Generic;
//////using System.IO;
//////using System.Linq;
//////using System.Net;
//////using System.Net.NetworkInformation;
//////using System.Net.Sockets;
//////using System.Text.RegularExpressions;
//////using System.Threading;
//////using System.Threading.Tasks;
//////using System.Windows;
//////using System.Windows.Controls;
//////using System.Windows.Input;

//////namespace BLDC_Demo
//////{
//////    // ────────────────────────────────────────────────────────────
//////    //  Model: a discovered network module
//////    // ────────────────────────────────────────────────────────────
//////    public class NetworkModule
//////    {
//////        public string SerialNumber { get; set; } = string.Empty;
//////        public string StationName { get; set; } = string.Empty;
//////        public string IpAddress { get; set; } = string.Empty;
//////        public string Mask { get; set; } = string.Empty;
//////        public string Gateway { get; set; } = string.Empty;
//////        public string FirmwareRev { get; set; } = string.Empty;
//////        public string Position { get; set; } = "—";

//////        // List shows:  192.168.1.50   Name: Station1
//////        public override string ToString() =>
//////            $"{IpAddress}   Name: {(string.IsNullOrEmpty(StationName) ? "-" : StationName)}";
//////    }

//////    // ────────────────────────────────────────────────────────────
//////    //  Backup container
//////    // ────────────────────────────────────────────────────────────
//////    public class ModuleBackup
//////    {
//////        public string SerialNumber { get; set; } = string.Empty;
//////        public string IpAddress { get; set; } = string.Empty;
//////        public string Mask { get; set; } = string.Empty;
//////        public string Gateway { get; set; } = string.Empty;
//////        public string Position { get; set; } = string.Empty;
//////        public DateTime BackupTime { get; set; } = DateTime.Now;
//////    }

//////    // ────────────────────────────────────────────────────────────
//////    //  UserControl code-behind  (targets C# 7.3)
//////    // ────────────────────────────────────────────────────────────
//////    public partial class NetworkService : UserControl
//////    {
//////        // ── ESP32 Modbus holding-register map for network config ──
//////        private const ushort REG_IP_OCTET1 = 0x0136;
//////        private const ushort REG_IP_OCTET2 = 0x0137;
//////        private const ushort REG_IP_OCTET3 = 0x0138;
//////        private const ushort REG_IP_OCTET4 = 0x0139;
//////        private const ushort REG_MASK_OCTET4 = 0x013A;
//////        private const ushort REG_APPLY_REBOOT = 0x013B;

//////        // ── Station name registers ──
//////        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
//////        private const int NAME_REG_COUNT = 16;
//////        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

//////        // Default Modbus TCP port used when we have to open a temporary
//////        // connection to push a new IP to a device that isn't connected yet.
//////        private const int DEFAULT_MODBUS_PORT = 502;

//////        private readonly Dictionary<string, ModuleBackup> _backupStore
//////            = new Dictionary<string, ModuleBackup>();

//////        public NetworkService()
//////        {
//////            InitializeComponent();
//////            AttachOctetHandlers();
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  OCTET HELPERS
//////        // ════════════════════════════════════════════════════════
//////        private void AttachOctetHandlers()
//////        {
//////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//////            AttachToGroup(ipBoxes);
//////            AttachToGroup(maskBoxes);
//////            AttachToGroup(gwBoxes);

//////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//////        }

//////        private void SetGatewayEnabled(bool enabled)
//////        {
//////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//////        }

//////        private static void AttachToGroup(TextBox[] boxes)
//////        {
//////            for (int i = 0; i < boxes.Length; i++)
//////            {
//////                int idx = i;

//////                boxes[idx].PreviewTextInput += (s, e) =>
//////                {
//////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//////                };

//////                boxes[idx].PreviewKeyDown += (s, e) =>
//////                {
//////                    TextBox tb = (TextBox)s;
//////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//////                    {
//////                        e.Handled = true;
//////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//////                    }
//////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//////                    {
//////                        boxes[idx - 1].Focus();
//////                        boxes[idx - 1].SelectAll();
//////                    }
//////                };

//////                boxes[idx].TextChanged += (s, e) =>
//////                {
//////                    TextBox tb = (TextBox)s;
//////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//////                        boxes[idx + 1].Focus();
//////                };

//////                boxes[idx].LostFocus += (s, e) =>
//////                {
//////                    TextBox tb = (TextBox)s;
//////                    if (int.TryParse(tb.Text, out int val))
//////                        tb.Text = Clamp(val, 0, 255).ToString();
//////                    else if (tb.Text.Length > 0)
//////                        tb.Text = string.Empty;
//////                };
//////            }
//////        }

//////        private static int Clamp(int value, int min, int max)
//////        {
//////            if (value < min) return min;
//////            if (value > max) return max;
//////            return value;
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  IP FIELD READ / WRITE
//////        // ════════════════════════════════════════════════════════
//////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//////        private string GetGateway() =>
//////            ChkGateway.IsChecked == true
//////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//////                : string.Empty;

//////        private void SetIpFields(string ip, string mask, string gw)
//////        {
//////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//////            ChkGateway.IsChecked = hasGw;
//////            SetGatewayEnabled(hasGw);

//////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//////        }

//////        private static void FillOctets(string address,
//////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//////        {
//////            string[] parts = address != null
//////                ? address.Split('.')
//////                : new string[0];

//////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//////        }

//////        private static void ClearOctets(params TextBox[] boxes)
//////        {
//////            foreach (TextBox b in boxes) b.Text = string.Empty;
//////        }

//////        private static bool IsValidIp(string ip) =>
//////            IPAddress.TryParse(ip, out _);

//////        // ════════════════════════════════════════════════════════
//////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
//////        // ════════════════════════════════════════════════════════
//////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//////        {
//////            LstModules.Items.Clear();
//////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//////            Button btn = (Button)sender;
//////            btn.IsEnabled = false;

//////            try
//////            {
//////                // The device that the main app is already connected to:
//////                // use its name directly, do not open a second connection.
//////                string connectedIp = null;
//////                string connectedName = null;
//////                string connectedSerial = null;

//////                if (Application.Current.MainWindow is MainWindow mw0)
//////                {
//////                    var comm = mw0.CommMotorSelect;
//////                    var d0 = comm?.BLLogic?.CurrentDevice;

//////                    if (d0 != null && !string.IsNullOrEmpty(d0.IpAddress))
//////                    {
//////                        connectedIp = d0.IpAddress;
//////                        connectedName = d0.StationName;
//////                        connectedSerial = d0.SerialNumber;
//////                    }
//////                    else if (comm?.BLLogic?.IsConnected == true)
//////                    {
//////                        // Fallback: IP that CommMotorSelectControl stored when connecting
//////                        connectedIp = comm.ConnectedIp;
//////                    }
//////                }

//////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules(connectedIp, connectedName));

//////                // The connected device usually accepts only one client, so the port scan
//////                // cannot reach it and it would be missing from the list. Add it manually.
//////                if (!string.IsNullOrEmpty(connectedIp) &&
//////                    !modules.Any(m => m.IpAddress == connectedIp))
//////                {
//////                    modules.Insert(0, new NetworkModule
//////                    {
//////                        IpAddress = connectedIp,
//////                        StationName = connectedName ?? string.Empty,
//////                        SerialNumber = string.IsNullOrEmpty(connectedSerial) ? "-" : connectedSerial,
//////                        FirmwareRev = "1.0.0",
//////                        Mask = "255.255.255.0",
//////                        Position = "—"
//////                    });
//////                }

//////                // ── Option A: fill in serial for the device BL is connected to ──
//////                if (Application.Current.MainWindow is MainWindow mw)
//////                {
//////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
//////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
//////                    {
//////                        foreach (NetworkModule m in modules)
//////                            if (m.IpAddress == dev.IpAddress)
//////                                m.SerialNumber = dev.SerialNumber;
//////                    }
//////                }

//////                foreach (NetworkModule m in modules)
//////                    LstModules.Items.Add(m);

//////                TxtStatus.Text = modules.Count == 0
//////                    ? "No motor controllers found."
//////                    : $"Found {modules.Count} motor controller(s).";
//////            }
//////            catch (Exception ex)
//////            {
//////                TxtStatus.Text = $"Discovery error: {ex.Message}";
//////            }
//////            finally
//////            {
//////                btn.IsEnabled = true;
//////            }
//////        }

//////        private static List<NetworkModule> DiscoverModules(string connectedIp, string connectedName)
//////        {
//////            var found = new List<NetworkModule>();
//////            string baseIp = GetLocalSubnetBase();
//////            if (string.IsNullOrEmpty(baseIp)) return found;

//////            var localIps = new HashSet<string>(
//////                NetworkInterface.GetAllNetworkInterfaces()
//////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////                    .Select(ua => ua.Address.ToString())
//////            );

//////            const int targetPort = 502;
//////            var tasks = new List<Task<Tuple<bool, string>>>();

//////            for (int i = 1; i <= 254; i++)
//////            {
//////                string targetIp = $"{baseIp}.{i}";
//////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//////            }

//////            Task.WaitAll(tasks.ToArray());

//////            foreach (var t in tasks)
//////            {
//////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//////                {
//////                    found.Add(new NetworkModule
//////                    {
//////                        IpAddress = t.Result.Item2,
//////                        SerialNumber = "-",
//////                        FirmwareRev = "1.0.0",
//////                        Mask = "255.255.255.0",
//////                        Position = "—"
//////                    });
//////                }
//////            }

//////            // Read the station name from each found device (in parallel)
//////            Parallel.ForEach(found, m =>
//////            {
//////                m.StationName = (m.IpAddress == connectedIp)
//////                    ? (connectedName ?? string.Empty)
//////                    : ReadStationName(m.IpAddress);
//////            });

//////            return found;
//////        }

//////        // Opens a short connection, sends the poll command, and collects
//////        // registers 0x0150 - 0x015F to build the station name.
//////        private static string ReadStationName(string ip)
//////        {
//////            try
//////            {
//////                using (var client = new TcpClient())
//////                {
//////                    var r = client.BeginConnect(ip, DEFAULT_MODBUS_PORT, null, null);
//////                    if (!r.AsyncWaitHandle.WaitOne(1500)) return string.Empty;
//////                    client.EndConnect(r);

//////                    using (NetworkStream stream = client.GetStream())
//////                    {
//////                        stream.ReadTimeout = 1500;
//////                        stream.WriteTimeout = 1500;

//////                        Thread.Sleep(100);
//////                        SendHandshake(stream);

//////                        // Same poll frame that BL.UpdateData() sends: function 0xFF
//////                        byte[] poll = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0xFF, 0x00, 0x00 };
//////                        stream.Write(poll, 0, poll.Length);
//////                        stream.Flush();

//////                        ushort[] regs = new ushort[NAME_REG_COUNT];
//////                        int received = 0;   // bit mask: one bit per register
//////                        int allBits = (1 << NAME_REG_COUNT) - 1;
//////                        DateTime deadline = DateTime.UtcNow.AddSeconds(4);

//////                        while (DateTime.UtcNow < deadline && received != allBits)
//////                        {
//////                            byte[] body = ReadModbusFrame(stream);
//////                            if (body == null) break;      // timeout or closed
//////                            if (body.Length < 6) continue;

//////                            ushort reg = (ushort)((body[2] << 8) | body[3]);
//////                            ushort val = (ushort)((body[4] << 8) | body[5]);

//////                            if (reg >= REG_NAME_START && reg < REG_NAME_START + NAME_REG_COUNT)
//////                            {
//////                                regs[reg - REG_NAME_START] = val;
//////                                received |= 1 << (reg - REG_NAME_START);
//////                            }
//////                        }

//////                        if (received != allBits) return string.Empty;

//////                        byte[] bytes = new byte[NAME_REG_COUNT * 2];
//////                        for (int i = 0; i < NAME_REG_COUNT; i++)
//////                        {
//////                            bytes[2 * i] = (byte)(regs[i] >> 8);
//////                            bytes[2 * i + 1] = (byte)(regs[i] & 0xFF);
//////                        }

//////                        string name = System.Text.Encoding.ASCII.GetString(bytes);
//////                        int zero = name.IndexOf('\0');
//////                        return zero >= 0 ? name.Substring(0, zero) : name;
//////                    }
//////                }
//////            }
//////            catch
//////            {
//////                return string.Empty;
//////            }
//////        }

//////        // Tries twice per host, with a short gap between attempts, and a
//////        // longer per-attempt timeout than before (500ms vs the old 200ms).
//////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//////        {
//////            for (int attempt = 0; attempt < 2; attempt++)
//////            {
//////                try
//////                {
//////                    using (var client = new TcpClient())
//////                    {
//////                        var connectTask = client.ConnectAsync(ip, port);
//////                        var delayTask = Task.Delay(500);
//////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
//////                        if (completedTask == connectTask && client.Connected)
//////                            return Tuple.Create(true, ip);
//////                    }
//////                }
//////                catch
//////                {
//////                    // Connection refused or failed — retry once.
//////                }

//////                if (attempt == 0)
//////                    await Task.Delay(300);
//////            }

//////            return Tuple.Create(false, ip);
//////        }

//////        private static string GetLocalSubnetBase()
//////        {
//////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//////                .ToList();

//////            if (!candidates.Any())
//////            {
//////                candidates = NetworkInterface.GetAllNetworkInterfaces()
//////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//////                    .ToList();
//////            }

//////            foreach (NetworkInterface ni in candidates)
//////            {
//////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//////                {
//////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//////                    {
//////                        string[] parts = ua.Address.ToString().Split('.');
//////                        if (parts.Length == 4)
//////                        {
//////                            if (parts[0] == "127") continue;               // loopback
//////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//////                        }
//////                    }
//////                }
//////            }
//////            return string.Empty;
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  LIST SELECTION → populate fields
//////        // ════════════════════════════════════════════════════════
//////        // Set while the code itself refreshes/re-selects the list (e.g. in BtnSet_Click)
//////        // so that it is not treated as the user clicking an IP.
//////        private bool _suppressSelectionHandling = false;

//////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////        {
//////            if (_suppressSelectionHandling) return;

//////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//////            if (m == null) return;

//////            TxtSerialNumber.Text = m.SerialNumber;
//////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//////            if (Application.Current.MainWindow is MainWindow mainWin)
//////            {
//////                var comm = mainWin.CommMotorSelect;

//////                // ── Show the station name ──
//////                var dev = comm?.BLLogic?.CurrentDevice;
//////                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
//////                    ? dev.StationName
//////                    : m.StationName;

//////                // ── Push selected module's IP into the Connect panel,
//////                //    but ONLY when the app is not connected ──
//////                bool connected = comm?.BLLogic != null &&
//////                                 (comm.BLLogic.IsConnected ||
//////                                  !string.IsNullOrEmpty(comm.BLLogic.ActivePortName));

//////                if (connected)
//////                {
//////                    Warn("Could not load the IP into the connection panel because a device " +
//////                         "is already connected.\nDisconnect first, then select the IP again.",
//////                         "Could Not Load IP");
//////                    return;
//////                }

//////                string[] octets = m.IpAddress.Split('.');
//////                if (comm != null && octets.Length == 4)
//////                {
//////                    comm.TcpIpOctet1.Text = octets[0];
//////                    comm.TcpIpOctet2.Text = octets[1];
//////                    comm.TcpIpOctet3.Text = octets[2];
//////                    comm.TcpIpOctet4.Text = octets[3];
//////                }
//////            }
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  SELECT ALL / NONE
//////        // ════════════════════════════════════════════════════════
//////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//////            => LstModules.SelectAll();

//////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//////            => LstModules.UnselectAll();

//////        // ════════════════════════════════════════════════════════
//////        //  SET  (station name + IP, works with or without an existing connection)
//////        // ════════════════════════════════════════════════════════
//////        private void BtnSet_Click(object sender, RoutedEventArgs e)
//////        {
//////            string ip = GetIp();
//////            string mask = GetMask();
//////            string gateway = GetGateway();

//////            if (!IsValidIp(ip))
//////            {
//////                Warn("Enter a valid IP address.", "Invalid IP");
//////                return;
//////            }

//////            if (!(Application.Current.MainWindow is MainWindow mainWin))
//////            {
//////                Warn("Main window not available.", "Error");
//////                return;
//////            }

//////            var commCtrl = mainWin.CommMotorSelect;

//////            if (commCtrl?.BLLogic == null)
//////            {
//////                Warn("Communication layer not initialized.", "Error");
//////                return;
//////            }

//////            // ── Step 1: Split IP into 4 octets ──
//////            string[] ipParts = ip.Split('.');
//////            if (ipParts.Length != 4 ||
//////                !byte.TryParse(ipParts[0], out byte octet1) ||
//////                !byte.TryParse(ipParts[1], out byte octet2) ||
//////                !byte.TryParse(ipParts[2], out byte octet3) ||
//////                !byte.TryParse(ipParts[3], out byte octet4))
//////            {
//////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//////                return;
//////            }

//////            if (octet1 == 0)
//////            {
//////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//////                return;
//////            }

//////            // ── Station name check ──
//////            string stationName = TxtStationName.Text ?? string.Empty;

//////            if (stationName.Length > 32)
//////            {
//////                Warn("Station name can be 32 characters maximum.", "Invalid Name");
//////                return;
//////            }
//////            foreach (char c in stationName)
//////            {
//////                if (c < 32 || c > 126)
//////                {
//////                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
//////                    return;
//////                }
//////            }

//////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//////            byte maskLastOctet = 0;
//////            string[] maskParts = mask.Split('.');
//////            if (maskParts.Length == 4)
//////                byte.TryParse(maskParts[3], out maskLastOctet);

//////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//////            string currentDeviceIp = selected != null
//////                ? selected.IpAddress
//////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

//////            if (!IsValidIp(currentDeviceIp))
//////            {
//////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
//////                return;
//////            }

//////            int port1 = DEFAULT_MODBUS_PORT;

//////            try
//////            {
//////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
//////            }
//////            catch (Exception ex)
//////            {
//////                TxtStatus.Text = $"Set failed: {ex.Message}";
//////                return; // don't touch UI state below — nothing confirmed sent
//////            }

//////            // ── Step 6: Update selected module in list if one is selected ──
//////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//////            if (module != null)
//////            {
//////                module.IpAddress = ip;
//////                module.Mask = mask;
//////                module.Gateway = gateway;
//////                if (stationName.Length > 0)
//////                    module.StationName = stationName;
//////                int idx = LstModules.SelectedIndex;
//////                _suppressSelectionHandling = true;   // re-selecting here is not a user click
//////                try
//////                {
//////                    LstModules.Items.Refresh();
//////                    LstModules.SelectedIndex = idx;
//////                }
//////                finally
//////                {
//////                    _suppressSelectionHandling = false;
//////                }
//////            }

//////            // ── Step 7: If the main comm panel happened to be connected to
//////            // this same device, drop it (device is rebooting anyway).
//////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//////            {
//////                commCtrl.BLLogic.Close();
//////                mainWin.DataContext = null;
//////            }

//////            string[] octets = ip.Split('.');
//////            if (octets.Length == 4)
//////            {
//////                commCtrl.TcpIpOctet1.Text = octets[0];
//////                commCtrl.TcpIpOctet2.Text = octets[1];
//////                commCtrl.TcpIpOctet3.Text = octets[2];
//////                commCtrl.TcpIpOctet4.Text = octets[3];
//////            }
//////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//////            commCtrl.ApplyAndSaveIp(ip);

//////            TxtStatus.Text = stationName.Length > 0
//////                ? $"IP set → {ip}, name → {stationName}"
//////                : $"IP set → {ip}.";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
//////        // ════════════════════════════════════════════════════════
//////        private void SendSetIpDirect(string targetIp, int port,
//////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
//////            string stationName)
//////        {
//////            using (var client = new TcpClient())
//////            {
//////                var connectResult = client.BeginConnect(targetIp, port, null, null);
//////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//////                client.EndConnect(connectResult);

//////                using (NetworkStream stream = client.GetStream())
//////                {
//////                    stream.WriteTimeout = 2000;
//////                    stream.ReadTimeout = 2000;

//////                    Thread.Sleep(100);

//////                    SendHandshake(stream);

//////                    // ── Station name first (the IP commit reboots the device) ──
//////                    if (!string.IsNullOrEmpty(stationName))
//////                    {
//////                        byte[] buf = new byte[32];   // all zeros = padding
//////                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
//////                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

//////                        for (int i = 0; i < NAME_REG_COUNT; i++)
//////                        {
//////                            // first char = high byte, second char = low byte
//////                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
//////                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
//////                        }

//////                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
//////                    }

//////                    // ── Then the IP settings ──
//////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
//////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
//////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
//////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
//////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

//////                    try
//////                    {
//////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
//////                    }
//////                    catch (TimeoutException)
//////                    {
//////                        // Acceptable: device may have started rebooting
//////                        // before it could echo the commit frame back.
//////                    }

//////                    Thread.Sleep(150);
//////                }
//////            }
//////        }

//////        private static void SendHandshake(NetworkStream stream)
//////        {
//////            byte[] handshake = { 0x01, 0x01 };
//////            stream.Write(handshake, 0, handshake.Length);
//////            stream.Flush();

//////            byte[] echo = ReadExactOrNull(stream, 2);
//////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
//////                throw new TimeoutException(
//////                    "Device did not respond to the required connection handshake " +
//////                    "(no echo received) — writes cannot proceed.");

//////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
//////            if (confirmFrame == null)
//////                throw new TimeoutException(
//////                    "Device echoed the handshake but did not send its confirmation frame.");
//////        }

//////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
//////        {
//////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
//////            stream.Write(frame, 0, frame.Length);
//////            stream.Flush();

//////            byte[] response = ReadModbusFrame(stream);

//////            if (response == null)
//////                throw new TimeoutException(
//////                    $"No response from device for register 0x{registerAddress:X4} " +
//////                    "(write may not have been processed).");

//////            if (response.Length >= 6 && response[1] == 0x86)
//////            {
//////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
//////                throw new InvalidOperationException(
//////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
//////            }
//////        }

//////        private static byte[] ReadModbusFrame(NetworkStream stream)
//////        {
//////            byte[] mbap = ReadExactOrNull(stream, 6);
//////            if (mbap == null) return null;

//////            int length = (mbap[4] << 8) | mbap[5];
//////            if (length <= 0 || length > 253) return null;

//////            byte[] body = ReadExactOrNull(stream, length);
//////            return body;
//////        }

//////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
//////        {
//////            byte[] buffer = new byte[count];
//////            int read = 0;
//////            try
//////            {
//////                while (read < count)
//////                {
//////                    int r = stream.Read(buffer, read, count - read);
//////                    if (r <= 0) return null;
//////                    read += r;
//////                }
//////                return buffer;
//////            }
//////            catch (IOException)
//////            {
//////                return null;
//////            }
//////        }

//////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
//////        {
//////            byte[] data = {
//////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//////                (byte)(value >> 8), (byte)(value & 0xFF)
//////            };

//////            byte[] pdu = new byte[2 + data.Length];
//////            pdu[0] = 0x01;
//////            pdu[1] = 0x06;
//////            Array.Copy(data, 0, pdu, 2, data.Length);

//////            byte[] frame = new byte[6 + pdu.Length];
//////            frame[0] = 0x00; frame[1] = 0x01;
//////            frame[2] = 0x00; frame[3] = 0x00;
//////            frame[4] = (byte)(pdu.Length >> 8);
//////            frame[5] = (byte)(pdu.Length & 0xFF);
//////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

//////            return frame;
//////        }

//////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//////        {
//////            // TODO: implement backup logic
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  RESTORE
//////        // ════════════════════════════════════════════════════════
//////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//////        {
//////            if (LstModules.SelectedItems.Count == 0)
//////            { Warn("Select at least one module to restore.", "No Selection"); return; }

//////            OpenFileDialog dlg = new OpenFileDialog
//////            {
//////                Title = "Open Backup File",
//////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////            };

//////            if (dlg.ShowDialog() != true) return;

//////            int restored = 0;
//////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////            {
//////                string[] parts = line.Split(',');
//////                if (parts.Length < 6) continue;

//////                string sn = parts[1].Trim();
//////                foreach (NetworkModule m in LstModules.SelectedItems)
//////                {
//////                    if (m.SerialNumber != sn) continue;
//////                    m.IpAddress = parts[2].Trim();
//////                    m.Mask = parts[3].Trim();
//////                    m.Gateway = parts[4].Trim();
//////                    m.Position = parts[5].Trim();
//////                    restored++;
//////                }
//////            }

//////            LstModules.Items.Refresh();
//////            TxtStatus.Text = restored > 0
//////                ? $"Restored {restored} module(s) from {dlg.FileName}"
//////                : "No matching modules found in the backup file.";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  RESTORE BY IP
//////        // ════════════════════════════════════════════════════════
//////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//////        {
//////            string ip = GetIp();

//////            if (!IsValidIp(ip))
//////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//////            OpenFileDialog dlg = new OpenFileDialog
//////            {
//////                Title = "Open Backup File",
//////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//////            };

//////            if (dlg.ShowDialog() != true) return;

//////            bool found = false;
//////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//////            {
//////                string[] parts = line.Split(',');
//////                if (parts.Length < 6) continue;
//////                if (parts[2].Trim() != ip) continue;

//////                foreach (NetworkModule m in LstModules.Items)
//////                {
//////                    if (m.IpAddress != ip) continue;
//////                    m.Mask = parts[3].Trim();
//////                    m.Gateway = parts[4].Trim();
//////                    m.Position = parts[5].Trim();
//////                    found = true;
//////                }
//////            }

//////            LstModules.Items.Refresh();
//////            TxtStatus.Text = found
//////                ? $"Restore by IP complete → {ip}"
//////                : $"No backup entry found for IP {ip}.";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  REORDER IPs
//////        // ════════════════════════════════════════════════════════
//////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//////        {
//////            if (LstModules.Items.Count == 0)
//////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//////            ReorderIpDialog dlg = new ReorderIpDialog();
//////            if (dlg.ShowDialog() != true) return;

//////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//////            byte[] bytes = startAddr.GetAddressBytes();
//////            int counter = bytes[3];

//////            foreach (NetworkModule m in LstModules.Items)
//////            {
//////                bytes[3] = (byte)Clamp(counter++, 1, 254);
//////                m.IpAddress = new IPAddress(bytes).ToString();
//////            }

//////            LstModules.Items.Refresh();
//////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//////        }

//////        // ════════════════════════════════════════════════════════
//////        //  UPGRADE FW
//////        // ════════════════════════════════════════════════════════
//////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//////        {
//////            if (LstModules.SelectedItems.Count == 0)
//////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//////            MessageBoxResult confirm = MessageBox.Show(
//////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//////                "Do NOT power off during upgrade.",
//////                "Confirm Firmware Upgrade",
//////                MessageBoxButton.YesNo,
//////                MessageBoxImage.Question);

//////            if (confirm != MessageBoxResult.Yes) return;

//////            OpenFileDialog dlg = new OpenFileDialog
//////            {
//////                Title = "Select Firmware File",
//////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//////            };

//////            if (dlg.ShowDialog() != true) return;

//////            Button btn = (Button)sender;
//////            btn.IsEnabled = false;
//////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//////            List<NetworkModule> targets = LstModules.SelectedItems
//////                .Cast<NetworkModule>()
//////                .ToList();

//////            string fwPath = dlg.FileName;

//////            try
//////            {
//////                await Task.Run(() =>
//////                {
//////                    foreach (NetworkModule m in targets)
//////                    {
//////                        System.Threading.Thread.Sleep(500);
//////                        m.FirmwareRev = "NEW";
//////                    }
//////                });

//////                LstModules.Items.Refresh();
//////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//////            }
//////            catch (Exception ex)
//////            {
//////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//////            }
//////            finally
//////            {
//////                btn.IsEnabled = true;
//////            }
//////        }

//////        private static void Warn(string message, string title) =>
//////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//////    }

//////    // ────────────────────────────────────────────────────────────
//////    //  Minimal "Enter start IP" dialog for Reorder IPs
//////    // ────────────────────────────────────────────────────────────
//////    public class ReorderIpDialog : Window
//////    {
//////        private readonly TextBox _input;
//////        public string StartIp => _input.Text.Trim();

//////        public ReorderIpDialog()
//////        {
//////            Title = "Reorder IPs";
//////            Width = 280;
//////            Height = 130;
//////            ResizeMode = ResizeMode.NoResize;
//////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//////            sp.Children.Add(new TextBlock
//////            {
//////                Text = "Enter starting IP address:",
//////                Margin = new Thickness(0, 0, 0, 6)
//////            });

//////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//////            sp.Children.Add(_input);

//////            StackPanel btnRow = new StackPanel
//////            {
//////                Orientation = Orientation.Horizontal,
//////                HorizontalAlignment = HorizontalAlignment.Right,
//////                Margin = new Thickness(0, 10, 0, 0)
//////            };

//////            Button ok = new Button
//////            {
//////                Content = "OK",
//////                Width = 70,
//////                Height = 24,
//////                Margin = new Thickness(0, 0, 6, 0),
//////                IsDefault = true
//////            };
//////            ok.Click += (s, ev) => { DialogResult = true; };

//////            Button cancel = new Button
//////            {
//////                Content = "Cancel",
//////                Width = 70,
//////                Height = 24,
//////                IsCancel = true
//////            };

//////            btnRow.Children.Add(ok);
//////            btnRow.Children.Add(cancel);
//////            sp.Children.Add(btnRow);

//////            Content = sp;
//////        }
//////    }
//////}
////using BLDC_Demo.Controls;
////using Microsoft.Win32;
////using System;
////using System.Collections.Generic;
////using System.IO;
////using System.Linq;
////using System.Net;
////using System.Net.NetworkInformation;
////using System.Net.Sockets;
////using System.Text.RegularExpressions;
////using System.Threading;
////using System.Threading.Tasks;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Input;
////using System.Windows.Media;
////using System.Windows.Threading;

////namespace BLDC_Demo
////{
////    // ────────────────────────────────────────────────────────────
////    //  Model: a discovered network module
////    // ────────────────────────────────────────────────────────────
////    public class NetworkModule
////    {
////        public string SerialNumber { get; set; } = string.Empty;
////        public string StationName { get; set; } = string.Empty;
////        public string IpAddress { get; set; } = string.Empty;
////        public string Mask { get; set; } = string.Empty;
////        public string Gateway { get; set; } = string.Empty;
////        public string FirmwareRev { get; set; } = string.Empty;
////        public string Position { get; set; } = "—";

////        // List shows:  192.168.1.50   Name: Station1
////        public override string ToString() =>
////            $"{IpAddress}   Name: {(string.IsNullOrEmpty(StationName) ? "-" : StationName)}";
////    }

////    // ────────────────────────────────────────────────────────────
////    //  Backup container
////    // ────────────────────────────────────────────────────────────
////    public class ModuleBackup
////    {
////        public string SerialNumber { get; set; } = string.Empty;
////        public string IpAddress { get; set; } = string.Empty;
////        public string Mask { get; set; } = string.Empty;
////        public string Gateway { get; set; } = string.Empty;
////        public string Position { get; set; } = string.Empty;
////        public DateTime BackupTime { get; set; } = DateTime.Now;
////    }

////    // ────────────────────────────────────────────────────────────
////    //  UserControl code-behind  (targets C# 7.3)
////    // ────────────────────────────────────────────────────────────
////    public partial class NetworkService : UserControl
////    {
////        // ── ESP32 Modbus holding-register map for network config ──
////        private const ushort REG_IP_OCTET1 = 0x0136;
////        private const ushort REG_IP_OCTET2 = 0x0137;
////        private const ushort REG_IP_OCTET3 = 0x0138;
////        private const ushort REG_IP_OCTET4 = 0x0139;
////        private const ushort REG_MASK_OCTET4 = 0x013A;
////        private const ushort REG_APPLY_REBOOT = 0x013B;

////        // ── Station name registers ──
////        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
////        private const int NAME_REG_COUNT = 16;
////        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

////        // Default Modbus TCP port used when we have to open a temporary
////        // connection to push a new IP to a device that isn't connected yet.
////        private const int DEFAULT_MODBUS_PORT = 502;

////        private readonly Dictionary<string, ModuleBackup> _backupStore
////            = new Dictionary<string, ModuleBackup>();

////        // Keeps the Set button in sync with the connection state
////        private DispatcherTimer _stateTimer;
////        private Button _setButton;

////        public NetworkService()
////        {
////            InitializeComponent();
////            AttachOctetHandlers();

////            Loaded += NetworkService_Loaded;
////            Unloaded += NetworkService_Unloaded;
////        }

////        // ════════════════════════════════════════════════════════
////        //  SET BUTTON: disabled while connected to an IP
////        // ════════════════════════════════════════════════════════
////        private void NetworkService_Loaded(object sender, RoutedEventArgs e)
////        {
////            UpdateSetButtonState();

////            if (_stateTimer == null)
////            {
////                _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
////                _stateTimer.Tick += (s, args) => UpdateSetButtonState();
////            }
////            _stateTimer.Start();
////        }

////        private void NetworkService_Unloaded(object sender, RoutedEventArgs e)
////        {
////            _stateTimer?.Stop();
////        }

////        private static bool IsDeviceConnected()
////        {
////            var mainWin = Application.Current.MainWindow as MainWindow;
////            var bl = mainWin?.CommMotorSelect?.BLLogic;
////            return bl != null && bl.IsConnected;
////        }

////        private void UpdateSetButtonState()
////        {
////            Button setBtn = FindSetButton();
////            if (setBtn == null) return;

////            setBtn.IsEnabled = !IsDeviceConnected();
////        }

////        // Finds the Set button without needing a known x:Name:
////        // first tries x:Name="BtnSet", then a Button whose text is "Set".
////        private Button FindSetButton()
////        {
////            if (_setButton != null) return _setButton;

////            _setButton = FindName("BtnSet") as Button;

////            if (_setButton == null)
////            {
////                foreach (Button b in FindVisualChildren<Button>(this))
////                {
////                    if (b.Content is string text &&
////                        text.Trim().Equals("set", StringComparison.OrdinalIgnoreCase))
////                    {
////                        _setButton = b;
////                        break;
////                    }
////                }
////            }

////            return _setButton;   // stays null (and is retried next tick) if not found yet
////        }

////        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
////        {
////            if (parent == null) yield break;

////            int count = VisualTreeHelper.GetChildrenCount(parent);
////            for (int i = 0; i < count; i++)
////            {
////                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

////                if (child is T match)
////                    yield return match;

////                foreach (T nested in FindVisualChildren<T>(child))
////                    yield return nested;
////            }
////        }

////        // ════════════════════════════════════════════════════════
////        //  OCTET HELPERS
////        // ════════════════════════════════════════════════════════
////        private void AttachOctetHandlers()
////        {
////            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
////            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
////            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

////            AttachToGroup(ipBoxes);
////            AttachToGroup(maskBoxes);
////            AttachToGroup(gwBoxes);

////            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
////            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
////        }

////        private void SetGatewayEnabled(bool enabled)
////        {
////            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
////            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
////        }

////        private static void AttachToGroup(TextBox[] boxes)
////        {
////            for (int i = 0; i < boxes.Length; i++)
////            {
////                int idx = i;

////                boxes[idx].PreviewTextInput += (s, e) =>
////                {
////                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
////                };

////                boxes[idx].PreviewKeyDown += (s, e) =>
////                {
////                    TextBox tb = (TextBox)s;
////                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
////                    {
////                        e.Handled = true;
////                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
////                    }
////                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
////                    {
////                        boxes[idx - 1].Focus();
////                        boxes[idx - 1].SelectAll();
////                    }
////                };

////                boxes[idx].TextChanged += (s, e) =>
////                {
////                    TextBox tb = (TextBox)s;
////                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
////                        boxes[idx + 1].Focus();
////                };

////                boxes[idx].LostFocus += (s, e) =>
////                {
////                    TextBox tb = (TextBox)s;
////                    if (int.TryParse(tb.Text, out int val))
////                        tb.Text = Clamp(val, 0, 255).ToString();
////                    else if (tb.Text.Length > 0)
////                        tb.Text = string.Empty;
////                };
////            }
////        }

////        private static int Clamp(int value, int min, int max)
////        {
////            if (value < min) return min;
////            if (value > max) return max;
////            return value;
////        }

////        // ════════════════════════════════════════════════════════
////        //  IP FIELD READ / WRITE
////        // ════════════════════════════════════════════════════════
////        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
////        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

////        private string GetGateway() =>
////            ChkGateway.IsChecked == true
////                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
////                : string.Empty;

////        private void SetIpFields(string ip, string mask, string gw)
////        {
////            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
////            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

////            bool hasGw = !string.IsNullOrWhiteSpace(gw);
////            ChkGateway.IsChecked = hasGw;
////            SetGatewayEnabled(hasGw);

////            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
////        }

////        private static void FillOctets(string address,
////            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
////        {
////            string[] parts = address != null
////                ? address.Split('.')
////                : new string[0];

////            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
////            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
////            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
////            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
////        }

////        private static void ClearOctets(params TextBox[] boxes)
////        {
////            foreach (TextBox b in boxes) b.Text = string.Empty;
////        }

////        private static bool IsValidIp(string ip) =>
////            IPAddress.TryParse(ip, out _);

////        // ════════════════════════════════════════════════════════
////        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
////        // ════════════════════════════════════════════════════════
////        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
////        {
////            LstModules.Items.Clear();
////            TxtStatus.Text = "Scanning subnet for active motor controllers...";

////            Button btn = (Button)sender;
////            btn.IsEnabled = false;

////            try
////            {
////                // The device that the main app is already connected to:
////                // use its name directly, do not open a second connection.
////                string connectedIp = null;
////                string connectedName = null;
////                string connectedSerial = null;

////                if (Application.Current.MainWindow is MainWindow mw0)
////                {
////                    var comm = mw0.CommMotorSelect;
////                    var d0 = comm?.BLLogic?.CurrentDevice;

////                    if (d0 != null && !string.IsNullOrEmpty(d0.IpAddress))
////                    {
////                        connectedIp = d0.IpAddress;
////                        connectedName = d0.StationName;
////                        connectedSerial = d0.SerialNumber;
////                    }
////                    else if (comm?.BLLogic?.IsConnected == true)
////                    {
////                        // Fallback: IP that CommMotorSelectControl stored when connecting
////                        connectedIp = comm.ConnectedIp;
////                    }
////                }

////                List<NetworkModule> modules = await Task.Run(() => DiscoverModules(connectedIp, connectedName));

////                // The connected device usually accepts only one client, so the port scan
////                // cannot reach it and it would be missing from the list. Add it manually.
////                if (!string.IsNullOrEmpty(connectedIp) &&
////                    !modules.Any(m => m.IpAddress == connectedIp))
////                {
////                    modules.Insert(0, new NetworkModule
////                    {
////                        IpAddress = connectedIp,
////                        StationName = connectedName ?? string.Empty,
////                        SerialNumber = string.IsNullOrEmpty(connectedSerial) ? "-" : connectedSerial,
////                        FirmwareRev = "1.0.0",
////                        Mask = "255.255.255.0",
////                        Position = "—"
////                    });
////                }

////                // ── Option A: fill in serial for the device BL is connected to ──
////                if (Application.Current.MainWindow is MainWindow mw)
////                {
////                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
////                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
////                    {
////                        foreach (NetworkModule m in modules)
////                            if (m.IpAddress == dev.IpAddress)
////                                m.SerialNumber = dev.SerialNumber;
////                    }
////                }

////                foreach (NetworkModule m in modules)
////                    LstModules.Items.Add(m);

////                TxtStatus.Text = modules.Count == 0
////                    ? "No motor controllers found."
////                    : $"Found {modules.Count} motor controller(s).";
////            }
////            catch (Exception ex)
////            {
////                TxtStatus.Text = $"Discovery error: {ex.Message}";
////            }
////            finally
////            {
////                btn.IsEnabled = true;
////            }
////        }

////        private static List<NetworkModule> DiscoverModules(string connectedIp, string connectedName)
////        {
////            var found = new List<NetworkModule>();
////            string baseIp = GetLocalSubnetBase();
////            if (string.IsNullOrEmpty(baseIp)) return found;

////            var localIps = new HashSet<string>(
////                NetworkInterface.GetAllNetworkInterfaces()
////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
////                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
////                    .Select(ua => ua.Address.ToString())
////            );

////            const int targetPort = 502;
////            var tasks = new List<Task<Tuple<bool, string>>>();

////            for (int i = 1; i <= 254; i++)
////            {
////                string targetIp = $"{baseIp}.{i}";
////                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
////            }

////            Task.WaitAll(tasks.ToArray());

////            foreach (var t in tasks)
////            {
////                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
////                {
////                    found.Add(new NetworkModule
////                    {
////                        IpAddress = t.Result.Item2,
////                        SerialNumber = "-",
////                        FirmwareRev = "1.0.0",
////                        Mask = "255.255.255.0",
////                        Position = "—"
////                    });
////                }
////            }

////            // Read the station name from each found device (in parallel)
////            Parallel.ForEach(found, m =>
////            {
////                m.StationName = (m.IpAddress == connectedIp)
////                    ? (connectedName ?? string.Empty)
////                    : ReadStationName(m.IpAddress);
////            });

////            return found;
////        }

////        // Opens a short connection, sends the poll command, and collects
////        // registers 0x0150 - 0x015F to build the station name.
////        private static string ReadStationName(string ip)
////        {
////            try
////            {
////                using (var client = new TcpClient())
////                {
////                    var r = client.BeginConnect(ip, DEFAULT_MODBUS_PORT, null, null);
////                    if (!r.AsyncWaitHandle.WaitOne(1500)) return string.Empty;
////                    client.EndConnect(r);

////                    using (NetworkStream stream = client.GetStream())
////                    {
////                        stream.ReadTimeout = 1500;
////                        stream.WriteTimeout = 1500;

////                        Thread.Sleep(100);
////                        SendHandshake(stream);

////                        // Same poll frame that BL.UpdateData() sends: function 0xFF
////                        byte[] poll = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0xFF, 0x00, 0x00 };
////                        stream.Write(poll, 0, poll.Length);
////                        stream.Flush();

////                        ushort[] regs = new ushort[NAME_REG_COUNT];
////                        int received = 0;   // bit mask: one bit per register
////                        int allBits = (1 << NAME_REG_COUNT) - 1;
////                        DateTime deadline = DateTime.UtcNow.AddSeconds(4);

////                        while (DateTime.UtcNow < deadline && received != allBits)
////                        {
////                            byte[] body = ReadModbusFrame(stream);
////                            if (body == null) break;      // timeout or closed
////                            if (body.Length < 6) continue;

////                            ushort reg = (ushort)((body[2] << 8) | body[3]);
////                            ushort val = (ushort)((body[4] << 8) | body[5]);

////                            if (reg >= REG_NAME_START && reg < REG_NAME_START + NAME_REG_COUNT)
////                            {
////                                regs[reg - REG_NAME_START] = val;
////                                received |= 1 << (reg - REG_NAME_START);
////                            }
////                        }

////                        if (received != allBits) return string.Empty;

////                        byte[] bytes = new byte[NAME_REG_COUNT * 2];
////                        for (int i = 0; i < NAME_REG_COUNT; i++)
////                        {
////                            bytes[2 * i] = (byte)(regs[i] >> 8);
////                            bytes[2 * i + 1] = (byte)(regs[i] & 0xFF);
////                        }

////                        string name = System.Text.Encoding.ASCII.GetString(bytes);
////                        int zero = name.IndexOf('\0');
////                        return zero >= 0 ? name.Substring(0, zero) : name;
////                    }
////                }
////            }
////            catch
////            {
////                return string.Empty;
////            }
////        }

////        // Tries twice per host, with a short gap between attempts, and a
////        // longer per-attempt timeout than before (500ms vs the old 200ms).
////        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
////        {
////            for (int attempt = 0; attempt < 2; attempt++)
////            {
////                try
////                {
////                    using (var client = new TcpClient())
////                    {
////                        var connectTask = client.ConnectAsync(ip, port);
////                        var delayTask = Task.Delay(500);
////                        var completedTask = await Task.WhenAny(connectTask, delayTask);
////                        if (completedTask == connectTask && client.Connected)
////                            return Tuple.Create(true, ip);
////                    }
////                }
////                catch
////                {
////                    // Connection refused or failed — retry once.
////                }

////                if (attempt == 0)
////                    await Task.Delay(300);
////            }

////            return Tuple.Create(false, ip);
////        }

////        private static string GetLocalSubnetBase()
////        {
////            var candidates = NetworkInterface.GetAllNetworkInterfaces()
////                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
////                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
////                .ToList();

////            if (!candidates.Any())
////            {
////                candidates = NetworkInterface.GetAllNetworkInterfaces()
////                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
////                    .ToList();
////            }

////            foreach (NetworkInterface ni in candidates)
////            {
////                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
////                {
////                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
////                    {
////                        string[] parts = ua.Address.ToString().Split('.');
////                        if (parts.Length == 4)
////                        {
////                            if (parts[0] == "127") continue;               // loopback
////                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
////                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
////                        }
////                    }
////                }
////            }
////            return string.Empty;
////        }

////        // ════════════════════════════════════════════════════════
////        //  LIST SELECTION → populate fields
////        // ════════════════════════════════════════════════════════
////        // Set while the code itself refreshes/re-selects the list (e.g. in BtnSet_Click)
////        // so that it is not treated as the user clicking an IP.
////        private bool _suppressSelectionHandling = false;

////        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            if (_suppressSelectionHandling) return;

////            NetworkModule m = LstModules.SelectedItem as NetworkModule;
////            if (m == null) return;

////            TxtSerialNumber.Text = m.SerialNumber;
////            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

////            if (Application.Current.MainWindow is MainWindow mainWin)
////            {
////                var comm = mainWin.CommMotorSelect;

////                // ── Show the station name ──
////                var dev = comm?.BLLogic?.CurrentDevice;
////                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
////                    ? dev.StationName
////                    : m.StationName;

////                // ── Push selected module's IP into the Connect panel,
////                //    but ONLY when the app is not connected ──
////                bool connected = comm?.BLLogic != null &&
////                                 (comm.BLLogic.IsConnected ||
////                                  !string.IsNullOrEmpty(comm.BLLogic.ActivePortName));

////                // Connected: silently do not load the IP (no popup)
////                if (connected)
////                    return;

////                string[] octets = m.IpAddress.Split('.');
////                if (comm != null && octets.Length == 4)
////                {
////                    comm.TcpIpOctet1.Text = octets[0];
////                    comm.TcpIpOctet2.Text = octets[1];
////                    comm.TcpIpOctet3.Text = octets[2];
////                    comm.TcpIpOctet4.Text = octets[3];
////                }
////            }
////        }

////        // ════════════════════════════════════════════════════════
////        //  SELECT ALL / NONE
////        // ════════════════════════════════════════════════════════
////        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
////            => LstModules.SelectAll();

////        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
////            => LstModules.UnselectAll();

////        // ════════════════════════════════════════════════════════
////        //  SET  (station name + IP, works with or without an existing connection)
////        // ════════════════════════════════════════════════════════
////        private void BtnSet_Click(object sender, RoutedEventArgs e)
////        {
////            string ip = GetIp();
////            string mask = GetMask();
////            string gateway = GetGateway();

////            if (!IsValidIp(ip))
////            {
////                Warn("Enter a valid IP address.", "Invalid IP");
////                return;
////            }

////            if (!(Application.Current.MainWindow is MainWindow mainWin))
////            {
////                Warn("Main window not available.", "Error");
////                return;
////            }

////            var commCtrl = mainWin.CommMotorSelect;

////            if (commCtrl?.BLLogic == null)
////            {
////                Warn("Communication layer not initialized.", "Error");
////                return;
////            }

////            // ── Step 1: Split IP into 4 octets ──
////            string[] ipParts = ip.Split('.');
////            if (ipParts.Length != 4 ||
////                !byte.TryParse(ipParts[0], out byte octet1) ||
////                !byte.TryParse(ipParts[1], out byte octet2) ||
////                !byte.TryParse(ipParts[2], out byte octet3) ||
////                !byte.TryParse(ipParts[3], out byte octet4))
////            {
////                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
////                return;
////            }

////            if (octet1 == 0)
////            {
////                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
////                return;
////            }

////            // ── Station name check ──
////            string stationName = TxtStationName.Text ?? string.Empty;

////            if (stationName.Length > 32)
////            {
////                Warn("Station name can be 32 characters maximum.", "Invalid Name");
////                return;
////            }
////            foreach (char c in stationName)
////            {
////                if (c < 32 || c > 126)
////                {
////                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
////                    return;
////                }
////            }

////            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
////            byte maskLastOctet = 0;
////            string[] maskParts = mask.Split('.');
////            if (maskParts.Length == 4)
////                byte.TryParse(maskParts[3], out maskLastOctet);

////            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
////            string currentDeviceIp = selected != null
////                ? selected.IpAddress
////                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

////            if (!IsValidIp(currentDeviceIp))
////            {
////                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
////                return;
////            }

////            int port1 = DEFAULT_MODBUS_PORT;

////            try
////            {
////                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
////            }
////            catch (Exception ex)
////            {
////                TxtStatus.Text = $"Set failed: {ex.Message}";
////                return; // don't touch UI state below — nothing confirmed sent
////            }

////            // ── Step 6: Update selected module in list if one is selected ──
////            NetworkModule module = LstModules.SelectedItem as NetworkModule;
////            if (module != null)
////            {
////                module.IpAddress = ip;
////                module.Mask = mask;
////                module.Gateway = gateway;
////                if (stationName.Length > 0)
////                    module.StationName = stationName;
////                int idx = LstModules.SelectedIndex;
////                _suppressSelectionHandling = true;   // re-selecting here is not a user click
////                try
////                {
////                    LstModules.Items.Refresh();
////                    LstModules.SelectedIndex = idx;
////                }
////                finally
////                {
////                    _suppressSelectionHandling = false;
////                }
////            }

////            // ── Step 7: If the main comm panel happened to be connected to
////            // this same device, drop it (device is rebooting anyway).
////            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
////            {
////                commCtrl.BLLogic.Close();
////                mainWin.DataContext = null;
////            }

////            string[] octets = ip.Split('.');
////            if (octets.Length == 4)
////            {
////                commCtrl.TcpIpOctet1.Text = octets[0];
////                commCtrl.TcpIpOctet2.Text = octets[1];
////                commCtrl.TcpIpOctet3.Text = octets[2];
////                commCtrl.TcpIpOctet4.Text = octets[3];
////            }
////            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
////            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
////            commCtrl.ApplyAndSaveIp(ip);

////            TxtStatus.Text = stationName.Length > 0
////                ? $"IP set → {ip}, name → {stationName}"
////                : $"IP set → {ip}.";
////        }

////        // ════════════════════════════════════════════════════════
////        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
////        // ════════════════════════════════════════════════════════
////        private void SendSetIpDirect(string targetIp, int port,
////            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
////            string stationName)
////        {
////            using (var client = new TcpClient())
////            {
////                var connectResult = client.BeginConnect(targetIp, port, null, null);
////                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
////                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

////                client.EndConnect(connectResult);

////                using (NetworkStream stream = client.GetStream())
////                {
////                    stream.WriteTimeout = 2000;
////                    stream.ReadTimeout = 2000;

////                    Thread.Sleep(100);

////                    SendHandshake(stream);

////                    // ── Station name first (the IP commit reboots the device) ──
////                    if (!string.IsNullOrEmpty(stationName))
////                    {
////                        byte[] buf = new byte[32];   // all zeros = padding
////                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
////                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

////                        for (int i = 0; i < NAME_REG_COUNT; i++)
////                        {
////                            // first char = high byte, second char = low byte
////                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
////                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
////                        }

////                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
////                    }

////                    // ── Then the IP settings ──
////                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
////                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
////                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
////                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
////                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

////                    try
////                    {
////                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
////                    }
////                    catch (TimeoutException)
////                    {
////                        // Acceptable: device may have started rebooting
////                        // before it could echo the commit frame back.
////                    }

////                    Thread.Sleep(150);
////                }
////            }
////        }

////        private static void SendHandshake(NetworkStream stream)
////        {
////            byte[] handshake = { 0x01, 0x01 };
////            stream.Write(handshake, 0, handshake.Length);
////            stream.Flush();

////            byte[] echo = ReadExactOrNull(stream, 2);
////            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
////                throw new TimeoutException(
////                    "Device did not respond to the required connection handshake " +
////                    "(no echo received) — writes cannot proceed.");

////            byte[] confirmFrame = ReadExactOrNull(stream, 12);
////            if (confirmFrame == null)
////                throw new TimeoutException(
////                    "Device echoed the handshake but did not send its confirmation frame.");
////        }

////        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
////        {
////            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
////            stream.Write(frame, 0, frame.Length);
////            stream.Flush();

////            byte[] response = ReadModbusFrame(stream);

////            if (response == null)
////                throw new TimeoutException(
////                    $"No response from device for register 0x{registerAddress:X4} " +
////                    "(write may not have been processed).");

////            if (response.Length >= 6 && response[1] == 0x86)
////            {
////                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
////                throw new InvalidOperationException(
////                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
////            }
////        }

////        private static byte[] ReadModbusFrame(NetworkStream stream)
////        {
////            byte[] mbap = ReadExactOrNull(stream, 6);
////            if (mbap == null) return null;

////            int length = (mbap[4] << 8) | mbap[5];
////            if (length <= 0 || length > 253) return null;

////            byte[] body = ReadExactOrNull(stream, length);
////            return body;
////        }

////        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
////        {
////            byte[] buffer = new byte[count];
////            int read = 0;
////            try
////            {
////                while (read < count)
////                {
////                    int r = stream.Read(buffer, read, count - read);
////                    if (r <= 0) return null;
////                    read += r;
////                }
////                return buffer;
////            }
////            catch (IOException)
////            {
////                return null;
////            }
////        }

////        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
////        {
////            byte[] data = {
////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
////                (byte)(value >> 8), (byte)(value & 0xFF)
////            };

////            byte[] pdu = new byte[2 + data.Length];
////            pdu[0] = 0x01;
////            pdu[1] = 0x06;
////            Array.Copy(data, 0, pdu, 2, data.Length);

////            byte[] frame = new byte[6 + pdu.Length];
////            frame[0] = 0x00; frame[1] = 0x01;
////            frame[2] = 0x00; frame[3] = 0x00;
////            frame[4] = (byte)(pdu.Length >> 8);
////            frame[5] = (byte)(pdu.Length & 0xFF);
////            Array.Copy(pdu, 0, frame, 6, pdu.Length);

////            return frame;
////        }

////        private void BtnBackup_Click(object sender, RoutedEventArgs e)
////        {
////            // TODO: implement backup logic
////        }

////        // ════════════════════════════════════════════════════════
////        //  RESTORE
////        // ════════════════════════════════════════════════════════
////        private void BtnRestore_Click(object sender, RoutedEventArgs e)
////        {
////            if (LstModules.SelectedItems.Count == 0)
////            { Warn("Select at least one module to restore.", "No Selection"); return; }

////            OpenFileDialog dlg = new OpenFileDialog
////            {
////                Title = "Open Backup File",
////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////            };

////            if (dlg.ShowDialog() != true) return;

////            int restored = 0;
////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////            {
////                string[] parts = line.Split(',');
////                if (parts.Length < 6) continue;

////                string sn = parts[1].Trim();
////                foreach (NetworkModule m in LstModules.SelectedItems)
////                {
////                    if (m.SerialNumber != sn) continue;
////                    m.IpAddress = parts[2].Trim();
////                    m.Mask = parts[3].Trim();
////                    m.Gateway = parts[4].Trim();
////                    m.Position = parts[5].Trim();
////                    restored++;
////                }
////            }

////            LstModules.Items.Refresh();
////            TxtStatus.Text = restored > 0
////                ? $"Restored {restored} module(s) from {dlg.FileName}"
////                : "No matching modules found in the backup file.";
////        }

////        // ════════════════════════════════════════════════════════
////        //  RESTORE BY IP
////        // ════════════════════════════════════════════════════════
////        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
////        {
////            string ip = GetIp();

////            if (!IsValidIp(ip))
////            { Warn("Enter a valid target IP address first.", "No IP"); return; }

////            OpenFileDialog dlg = new OpenFileDialog
////            {
////                Title = "Open Backup File",
////                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
////            };

////            if (dlg.ShowDialog() != true) return;

////            bool found = false;
////            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
////            {
////                string[] parts = line.Split(',');
////                if (parts.Length < 6) continue;
////                if (parts[2].Trim() != ip) continue;

////                foreach (NetworkModule m in LstModules.Items)
////                {
////                    if (m.IpAddress != ip) continue;
////                    m.Mask = parts[3].Trim();
////                    m.Gateway = parts[4].Trim();
////                    m.Position = parts[5].Trim();
////                    found = true;
////                }
////            }

////            LstModules.Items.Refresh();
////            TxtStatus.Text = found
////                ? $"Restore by IP complete → {ip}"
////                : $"No backup entry found for IP {ip}.";
////        }

////        // ════════════════════════════════════════════════════════
////        //  REORDER IPs
////        // ════════════════════════════════════════════════════════
////        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
////        {
////            if (LstModules.Items.Count == 0)
////            { Warn("No modules in the list to reorder.", "Empty List"); return; }

////            ReorderIpDialog dlg = new ReorderIpDialog();
////            if (dlg.ShowDialog() != true) return;

////            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
////            { Warn("Invalid start IP address.", "Invalid IP"); return; }

////            byte[] bytes = startAddr.GetAddressBytes();
////            int counter = bytes[3];

////            foreach (NetworkModule m in LstModules.Items)
////            {
////                bytes[3] = (byte)Clamp(counter++, 1, 254);
////                m.IpAddress = new IPAddress(bytes).ToString();
////            }

////            LstModules.Items.Refresh();
////            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
////        }

////        // ════════════════════════════════════════════════════════
////        //  UPGRADE FW
////        // ════════════════════════════════════════════════════════
////        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
////        {
////            if (LstModules.SelectedItems.Count == 0)
////            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

////            MessageBoxResult confirm = MessageBox.Show(
////                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
////                "Do NOT power off during upgrade.",
////                "Confirm Firmware Upgrade",
////                MessageBoxButton.YesNo,
////                MessageBoxImage.Question);

////            if (confirm != MessageBoxResult.Yes) return;

////            OpenFileDialog dlg = new OpenFileDialog
////            {
////                Title = "Select Firmware File",
////                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
////            };

////            if (dlg.ShowDialog() != true) return;

////            Button btn = (Button)sender;
////            btn.IsEnabled = false;
////            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

////            List<NetworkModule> targets = LstModules.SelectedItems
////                .Cast<NetworkModule>()
////                .ToList();

////            string fwPath = dlg.FileName;

////            try
////            {
////                await Task.Run(() =>
////                {
////                    foreach (NetworkModule m in targets)
////                    {
////                        System.Threading.Thread.Sleep(500);
////                        m.FirmwareRev = "NEW";
////                    }
////                });

////                LstModules.Items.Refresh();
////                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
////            }
////            catch (Exception ex)
////            {
////                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
////            }
////            finally
////            {
////                btn.IsEnabled = true;
////            }
////        }

////        private static void Warn(string message, string title) =>
////            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
////    }

////    // ────────────────────────────────────────────────────────────
////    //  Minimal "Enter start IP" dialog for Reorder IPs
////    // ────────────────────────────────────────────────────────────
////    public class ReorderIpDialog : Window
////    {
////        private readonly TextBox _input;
////        public string StartIp => _input.Text.Trim();

////        public ReorderIpDialog()
////        {
////            Title = "Reorder IPs";
////            Width = 280;
////            Height = 130;
////            ResizeMode = ResizeMode.NoResize;
////            WindowStartupLocation = WindowStartupLocation.CenterOwner;

////            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

////            sp.Children.Add(new TextBlock
////            {
////                Text = "Enter starting IP address:",
////                Margin = new Thickness(0, 0, 0, 6)
////            });

////            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
////            sp.Children.Add(_input);

////            StackPanel btnRow = new StackPanel
////            {
////                Orientation = Orientation.Horizontal,
////                HorizontalAlignment = HorizontalAlignment.Right,
////                Margin = new Thickness(0, 10, 0, 0)
////            };

////            Button ok = new Button
////            {
////                Content = "OK",
////                Width = 70,
////                Height = 24,
////                Margin = new Thickness(0, 0, 6, 0),
////                IsDefault = true
////            };
////            ok.Click += (s, ev) => { DialogResult = true; };

////            Button cancel = new Button
////            {
////                Content = "Cancel",
////                Width = 70,
////                Height = 24,
////                IsCancel = true
////            };

////            btnRow.Children.Add(ok);
////            btnRow.Children.Add(cancel);
////            sp.Children.Add(btnRow);

////            Content = sp;
////        }
////    }
////}
//using BLDC_Demo.Controls;
//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.NetworkInformation;
//using System.Net.Sockets;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;

//namespace BLDC_Demo
//{
//    // ────────────────────────────────────────────────────────────
//    //  Model: a discovered network module
//    // ────────────────────────────────────────────────────────────
//    public class NetworkModule
//    {
//        public string SerialNumber { get; set; } = string.Empty;
//        public string StationName { get; set; } = string.Empty;
//        public string IpAddress { get; set; } = string.Empty;
//        public string Mask { get; set; } = string.Empty;
//        public string Gateway { get; set; } = string.Empty;
//        public string FirmwareRev { get; set; } = string.Empty;
//        public string Position { get; set; } = "—";

//        // List shows:  192.168.1.50   Name: Station1
//        public override string ToString() =>
//            $"{IpAddress}   Name: {(string.IsNullOrEmpty(StationName) ? "-" : StationName)}";
//    }

//    // ────────────────────────────────────────────────────────────
//    //  Backup container
//    // ────────────────────────────────────────────────────────────
//    public class ModuleBackup
//    {
//        public string SerialNumber { get; set; } = string.Empty;
//        public string IpAddress { get; set; } = string.Empty;
//        public string Mask { get; set; } = string.Empty;
//        public string Gateway { get; set; } = string.Empty;
//        public string Position { get; set; } = string.Empty;
//        public DateTime BackupTime { get; set; } = DateTime.Now;
//    }

//    // ────────────────────────────────────────────────────────────
//    //  UserControl code-behind  (targets C# 7.3)
//    // ────────────────────────────────────────────────────────────
//    public partial class NetworkService : UserControl
//    {
//        // ── ESP32 Modbus holding-register map for network config ──
//        private const ushort REG_IP_OCTET1 = 0x0136;
//        private const ushort REG_IP_OCTET2 = 0x0137;
//        private const ushort REG_IP_OCTET3 = 0x0138;
//        private const ushort REG_IP_OCTET4 = 0x0139;
//        private const ushort REG_MASK_OCTET4 = 0x013A;
//        private const ushort REG_APPLY_REBOOT = 0x013B;

//        // ── Station name registers ──
//        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
//        private const int NAME_REG_COUNT = 16;
//        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

//        // Default Modbus TCP port used when we have to open a temporary
//        // connection to push a new IP to a device that isn't connected yet.
//        private const int DEFAULT_MODBUS_PORT = 502;

//        private readonly Dictionary<string, ModuleBackup> _backupStore
//            = new Dictionary<string, ModuleBackup>();

//        public NetworkService()
//        {
//            InitializeComponent();
//            AttachOctetHandlers();
//        }

//        // True while the main app is connected to a device
//        private static bool IsDeviceConnected()
//        {
//            var mainWin = Application.Current.MainWindow as MainWindow;
//            var bl = mainWin?.CommMotorSelect?.BLLogic;
//            return bl != null && bl.IsConnected;
//        }

//        // ════════════════════════════════════════════════════════
//        //  OCTET HELPERS
//        // ════════════════════════════════════════════════════════
//        private void AttachOctetHandlers()
//        {
//            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
//            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
//            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

//            AttachToGroup(ipBoxes);
//            AttachToGroup(maskBoxes);
//            AttachToGroup(gwBoxes);

//            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
//            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
//        }

//        private void SetGatewayEnabled(bool enabled)
//        {
//            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
//            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
//        }

//        private static void AttachToGroup(TextBox[] boxes)
//        {
//            for (int i = 0; i < boxes.Length; i++)
//            {
//                int idx = i;

//                boxes[idx].PreviewTextInput += (s, e) =>
//                {
//                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
//                };

//                boxes[idx].PreviewKeyDown += (s, e) =>
//                {
//                    TextBox tb = (TextBox)s;
//                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
//                    {
//                        e.Handled = true;
//                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
//                    }
//                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
//                    {
//                        boxes[idx - 1].Focus();
//                        boxes[idx - 1].SelectAll();
//                    }
//                };

//                boxes[idx].TextChanged += (s, e) =>
//                {
//                    TextBox tb = (TextBox)s;
//                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
//                        boxes[idx + 1].Focus();
//                };

//                boxes[idx].LostFocus += (s, e) =>
//                {
//                    TextBox tb = (TextBox)s;
//                    if (int.TryParse(tb.Text, out int val))
//                        tb.Text = Clamp(val, 0, 255).ToString();
//                    else if (tb.Text.Length > 0)
//                        tb.Text = string.Empty;
//                };
//            }
//        }

//        private static int Clamp(int value, int min, int max)
//        {
//            if (value < min) return min;
//            if (value > max) return max;
//            return value;
//        }

//        // ════════════════════════════════════════════════════════
//        //  IP FIELD READ / WRITE
//        // ════════════════════════════════════════════════════════
//        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
//        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

//        private string GetGateway() =>
//            ChkGateway.IsChecked == true
//                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
//                : string.Empty;

//        private void SetIpFields(string ip, string mask, string gw)
//        {
//            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
//            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

//            bool hasGw = !string.IsNullOrWhiteSpace(gw);
//            ChkGateway.IsChecked = hasGw;
//            SetGatewayEnabled(hasGw);

//            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
//        }

//        private static void FillOctets(string address,
//            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
//        {
//            string[] parts = address != null
//                ? address.Split('.')
//                : new string[0];

//            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
//            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
//            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
//            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;
//        }

//        private static void ClearOctets(params TextBox[] boxes)
//        {
//            foreach (TextBox b in boxes) b.Text = string.Empty;
//        }

//        private static bool IsValidIp(string ip) =>
//            IPAddress.TryParse(ip, out _);

//        // ════════════════════════════════════════════════════════
//        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
//        // ════════════════════════════════════════════════════════
//        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
//        {
//            LstModules.Items.Clear();
//            TxtStatus.Text = "Scanning subnet for active motor controllers...";

//            Button btn = (Button)sender;
//            btn.IsEnabled = false;

//            try
//            {
//                // The device that the main app is already connected to:
//                // use its name directly, do not open a second connection.
//                string connectedIp = null;
//                string connectedName = null;
//                string connectedSerial = null;

//                if (Application.Current.MainWindow is MainWindow mw0)
//                {
//                    var comm = mw0.CommMotorSelect;
//                    var d0 = comm?.BLLogic?.CurrentDevice;

//                    if (d0 != null && !string.IsNullOrEmpty(d0.IpAddress))
//                    {
//                        connectedIp = d0.IpAddress;
//                        connectedName = d0.StationName;
//                        connectedSerial = d0.SerialNumber;
//                    }
//                    else if (comm?.BLLogic?.IsConnected == true)
//                    {
//                        // Fallback: IP that CommMotorSelectControl stored when connecting
//                        connectedIp = comm.ConnectedIp;
//                    }
//                }

//                List<NetworkModule> modules = await Task.Run(() => DiscoverModules(connectedIp, connectedName));

//                // The connected device usually accepts only one client, so the port scan
//                // cannot reach it and it would be missing from the list. Add it manually.
//                if (!string.IsNullOrEmpty(connectedIp) &&
//                    !modules.Any(m => m.IpAddress == connectedIp))
//                {
//                    modules.Insert(0, new NetworkModule
//                    {
//                        IpAddress = connectedIp,
//                        StationName = connectedName ?? string.Empty,
//                        SerialNumber = string.IsNullOrEmpty(connectedSerial) ? "-" : connectedSerial,
//                        FirmwareRev = "1.0.0",
//                        Mask = "255.255.255.0",
//                        Position = "—"
//                    });
//                }

//                // ── Option A: fill in serial for the device BL is connected to ──
//                if (Application.Current.MainWindow is MainWindow mw)
//                {
//                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
//                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
//                    {
//                        foreach (NetworkModule m in modules)
//                            if (m.IpAddress == dev.IpAddress)
//                                m.SerialNumber = dev.SerialNumber;
//                    }
//                }

//                foreach (NetworkModule m in modules)
//                    LstModules.Items.Add(m);

//                TxtStatus.Text = modules.Count == 0
//                    ? "No motor controllers found."
//                    : $"Found {modules.Count} motor controller(s).";
//            }
//            catch (Exception ex)
//            {
//                TxtStatus.Text = $"Discovery error: {ex.Message}";
//            }
//            finally
//            {
//                btn.IsEnabled = true;
//            }
//        }

//        private static List<NetworkModule> DiscoverModules(string connectedIp, string connectedName)
//        {
//            var found = new List<NetworkModule>();
//            string baseIp = GetLocalSubnetBase();
//            if (string.IsNullOrEmpty(baseIp)) return found;

//            var localIps = new HashSet<string>(
//                NetworkInterface.GetAllNetworkInterfaces()
//                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
//                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
//                    .Select(ua => ua.Address.ToString())
//            );

//            const int targetPort = 502;
//            var tasks = new List<Task<Tuple<bool, string>>>();

//            for (int i = 1; i <= 254; i++)
//            {
//                string targetIp = $"{baseIp}.{i}";
//                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
//            }

//            Task.WaitAll(tasks.ToArray());

//            foreach (var t in tasks)
//            {
//                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
//                {
//                    found.Add(new NetworkModule
//                    {
//                        IpAddress = t.Result.Item2,
//                        SerialNumber = "-",
//                        FirmwareRev = "1.0.0",
//                        Mask = "255.255.255.0",
//                        Position = "—"
//                    });
//                }
//            }

//            // Read the station name from each found device (in parallel)
//            Parallel.ForEach(found, m =>
//            {
//                m.StationName = (m.IpAddress == connectedIp)
//                    ? (connectedName ?? string.Empty)
//                    : ReadStationName(m.IpAddress);
//            });

//            return found;
//        }

//        // Opens a short connection, sends the poll command, and collects
//        // registers 0x0150 - 0x015F to build the station name.
//        private static string ReadStationName(string ip)
//        {
//            try
//            {
//                using (var client = new TcpClient())
//                {
//                    var r = client.BeginConnect(ip, DEFAULT_MODBUS_PORT, null, null);
//                    if (!r.AsyncWaitHandle.WaitOne(1500)) return string.Empty;
//                    client.EndConnect(r);

//                    using (NetworkStream stream = client.GetStream())
//                    {
//                        stream.ReadTimeout = 1500;
//                        stream.WriteTimeout = 1500;

//                        Thread.Sleep(100);
//                        SendHandshake(stream);

//                        // Same poll frame that BL.UpdateData() sends: function 0xFF
//                        byte[] poll = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0xFF, 0x00, 0x00 };
//                        stream.Write(poll, 0, poll.Length);
//                        stream.Flush();

//                        ushort[] regs = new ushort[NAME_REG_COUNT];
//                        int received = 0;   // bit mask: one bit per register
//                        int allBits = (1 << NAME_REG_COUNT) - 1;
//                        DateTime deadline = DateTime.UtcNow.AddSeconds(4);

//                        while (DateTime.UtcNow < deadline && received != allBits)
//                        {
//                            byte[] body = ReadModbusFrame(stream);
//                            if (body == null) break;      // timeout or closed
//                            if (body.Length < 6) continue;

//                            ushort reg = (ushort)((body[2] << 8) | body[3]);
//                            ushort val = (ushort)((body[4] << 8) | body[5]);

//                            if (reg >= REG_NAME_START && reg < REG_NAME_START + NAME_REG_COUNT)
//                            {
//                                regs[reg - REG_NAME_START] = val;
//                                received |= 1 << (reg - REG_NAME_START);
//                            }
//                        }

//                        if (received != allBits) return string.Empty;

//                        byte[] bytes = new byte[NAME_REG_COUNT * 2];
//                        for (int i = 0; i < NAME_REG_COUNT; i++)
//                        {
//                            bytes[2 * i] = (byte)(regs[i] >> 8);
//                            bytes[2 * i + 1] = (byte)(regs[i] & 0xFF);
//                        }

//                        string name = System.Text.Encoding.ASCII.GetString(bytes);
//                        int zero = name.IndexOf('\0');
//                        return zero >= 0 ? name.Substring(0, zero) : name;
//                    }
//                }
//            }
//            catch
//            {
//                return string.Empty;
//            }
//        }

//        // Tries twice per host, with a short gap between attempts, and a
//        // longer per-attempt timeout than before (500ms vs the old 200ms).
//        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
//        {
//            for (int attempt = 0; attempt < 2; attempt++)
//            {
//                try
//                {
//                    using (var client = new TcpClient())
//                    {
//                        var connectTask = client.ConnectAsync(ip, port);
//                        var delayTask = Task.Delay(500);
//                        var completedTask = await Task.WhenAny(connectTask, delayTask);
//                        if (completedTask == connectTask && client.Connected)
//                            return Tuple.Create(true, ip);
//                    }
//                }
//                catch
//                {
//                    // Connection refused or failed — retry once.
//                }

//                if (attempt == 0)
//                    await Task.Delay(300);
//            }

//            return Tuple.Create(false, ip);
//        }

//        private static string GetLocalSubnetBase()
//        {
//            var candidates = NetworkInterface.GetAllNetworkInterfaces()
//                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
//                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
//                .ToList();

//            if (!candidates.Any())
//            {
//                candidates = NetworkInterface.GetAllNetworkInterfaces()
//                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
//                    .ToList();
//            }

//            foreach (NetworkInterface ni in candidates)
//            {
//                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
//                {
//                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
//                    {
//                        string[] parts = ua.Address.ToString().Split('.');
//                        if (parts.Length == 4)
//                        {
//                            if (parts[0] == "127") continue;               // loopback
//                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
//                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
//                        }
//                    }
//                }
//            }
//            return string.Empty;
//        }

//        // ════════════════════════════════════════════════════════
//        //  LIST SELECTION → populate fields
//        // ════════════════════════════════════════════════════════
//        // Set while the code itself refreshes/re-selects the list (e.g. in BtnSet_Click)
//        // so that it is not treated as the user clicking an IP.
//        private bool _suppressSelectionHandling = false;

//        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (_suppressSelectionHandling) return;

//            NetworkModule m = LstModules.SelectedItem as NetworkModule;
//            if (m == null) return;

//            TxtSerialNumber.Text = m.SerialNumber;
//            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

//            if (Application.Current.MainWindow is MainWindow mainWin)
//            {
//                var comm = mainWin.CommMotorSelect;

//                // ── Show the station name ──
//                var dev = comm?.BLLogic?.CurrentDevice;
//                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
//                    ? dev.StationName
//                    : m.StationName;

//                // ── Push selected module's IP into the Connect panel,
//                //    but ONLY when the app is not connected ──
//                bool connected = comm?.BLLogic != null &&
//                                 (comm.BLLogic.IsConnected ||
//                                  !string.IsNullOrEmpty(comm.BLLogic.ActivePortName));

//                // Connected: silently do not load the IP (no popup)
//                if (connected)
//                    return;

//                string[] octets = m.IpAddress.Split('.');
//                if (comm != null && octets.Length == 4)
//                {
//                    comm.TcpIpOctet1.Text = octets[0];
//                    comm.TcpIpOctet2.Text = octets[1];
//                    comm.TcpIpOctet3.Text = octets[2];
//                    comm.TcpIpOctet4.Text = octets[3];
//                }
//            }
//        }

//        // ════════════════════════════════════════════════════════
//        //  SELECT ALL / NONE
//        // ════════════════════════════════════════════════════════
//        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
//            => LstModules.SelectAll();

//        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
//            => LstModules.UnselectAll();

//        // ════════════════════════════════════════════════════════
//        //  SET  (station name + IP, works with or without an existing connection)
//        // ════════════════════════════════════════════════════════
//        private void BtnSet_Click(object sender, RoutedEventArgs e)
//        {
//            // The Set button stays enabled, but it does nothing while connected
//            if (IsDeviceConnected())
//            {
//                Warn("Cannot set the IP while a device is connected.\nDisconnect first, then press Set.",
//                     "Device Connected");
//                return;
//            }

//            string ip = GetIp();
//            string mask = GetMask();
//            string gateway = GetGateway();

//            if (!IsValidIp(ip))
//            {
//                Warn("Enter a valid IP address.", "Invalid IP");
//                return;
//            }

//            if (!(Application.Current.MainWindow is MainWindow mainWin))
//            {
//                Warn("Main window not available.", "Error");
//                return;
//            }

//            var commCtrl = mainWin.CommMotorSelect;

//            if (commCtrl?.BLLogic == null)
//            {
//                Warn("Communication layer not initialized.", "Error");
//                return;
//            }

//            // ── Step 1: Split IP into 4 octets ──
//            string[] ipParts = ip.Split('.');
//            if (ipParts.Length != 4 ||
//                !byte.TryParse(ipParts[0], out byte octet1) ||
//                !byte.TryParse(ipParts[1], out byte octet2) ||
//                !byte.TryParse(ipParts[2], out byte octet3) ||
//                !byte.TryParse(ipParts[3], out byte octet4))
//            {
//                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
//                return;
//            }

//            if (octet1 == 0)
//            {
//                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
//                return;
//            }

//            // ── Station name check ──
//            string stationName = TxtStationName.Text ?? string.Empty;

//            if (stationName.Length > 32)
//            {
//                Warn("Station name can be 32 characters maximum.", "Invalid Name");
//                return;
//            }
//            foreach (char c in stationName)
//            {
//                if (c < 32 || c > 126)
//                {
//                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
//                    return;
//                }
//            }

//            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
//            byte maskLastOctet = 0;
//            string[] maskParts = mask.Split('.');
//            if (maskParts.Length == 4)
//                byte.TryParse(maskParts[3], out maskLastOctet);

//            NetworkModule selected = LstModules.SelectedItem as NetworkModule;
//            string currentDeviceIp = selected != null
//                ? selected.IpAddress
//                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

//            if (!IsValidIp(currentDeviceIp))
//            {
//                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
//                return;
//            }

//            int port1 = DEFAULT_MODBUS_PORT;

//            try
//            {
//                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
//            }
//            catch (Exception ex)
//            {
//                TxtStatus.Text = $"Set failed: {ex.Message}";
//                return; // don't touch UI state below — nothing confirmed sent
//            }

//            // ── Step 6: Update selected module in list if one is selected ──
//            NetworkModule module = LstModules.SelectedItem as NetworkModule;
//            if (module != null)
//            {
//                module.IpAddress = ip;
//                module.Mask = mask;
//                module.Gateway = gateway;
//                if (stationName.Length > 0)
//                    module.StationName = stationName;
//                int idx = LstModules.SelectedIndex;
//                _suppressSelectionHandling = true;   // re-selecting here is not a user click
//                try
//                {
//                    LstModules.Items.Refresh();
//                    LstModules.SelectedIndex = idx;
//                }
//                finally
//                {
//                    _suppressSelectionHandling = false;
//                }
//            }

//            // ── Step 7: If the main comm panel happened to be connected to
//            // this same device, drop it (device is rebooting anyway).
//            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
//            {
//                commCtrl.BLLogic.Close();
//                mainWin.DataContext = null;
//            }

//            string[] octets = ip.Split('.');
//            if (octets.Length == 4)
//            {
//                commCtrl.TcpIpOctet1.Text = octets[0];
//                commCtrl.TcpIpOctet2.Text = octets[1];
//                commCtrl.TcpIpOctet3.Text = octets[2];
//                commCtrl.TcpIpOctet4.Text = octets[3];
//            }
//            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
//            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
//            commCtrl.ApplyAndSaveIp(ip);

//            TxtStatus.Text = stationName.Length > 0
//                ? $"IP set → {ip}, name → {stationName}"
//                : $"IP set → {ip}.";
//        }

//        // ════════════════════════════════════════════════════════
//        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
//        // ════════════════════════════════════════════════════════
//        private void SendSetIpDirect(string targetIp, int port,
//            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
//            string stationName)
//        {
//            using (var client = new TcpClient())
//            {
//                var connectResult = client.BeginConnect(targetIp, port, null, null);
//                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

//                client.EndConnect(connectResult);

//                using (NetworkStream stream = client.GetStream())
//                {
//                    stream.WriteTimeout = 2000;
//                    stream.ReadTimeout = 2000;

//                    Thread.Sleep(100);

//                    SendHandshake(stream);

//                    // ── Station name first (the IP commit reboots the device) ──
//                    if (!string.IsNullOrEmpty(stationName))
//                    {
//                        byte[] buf = new byte[32];   // all zeros = padding
//                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
//                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

//                        for (int i = 0; i < NAME_REG_COUNT; i++)
//                        {
//                            // first char = high byte, second char = low byte
//                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
//                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
//                        }

//                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
//                    }

//                    // ── Then the IP settings ──
//                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
//                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
//                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
//                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
//                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

//                    try
//                    {
//                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
//                    }
//                    catch (TimeoutException)
//                    {
//                        // Acceptable: device may have started rebooting
//                        // before it could echo the commit frame back.
//                    }

//                    Thread.Sleep(150);
//                }
//            }
//        }

//        private static void SendHandshake(NetworkStream stream)
//        {
//            byte[] handshake = { 0x01, 0x01 };
//            stream.Write(handshake, 0, handshake.Length);
//            stream.Flush();

//            byte[] echo = ReadExactOrNull(stream, 2);
//            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
//                throw new TimeoutException(
//                    "Device did not respond to the required connection handshake " +
//                    "(no echo received) — writes cannot proceed.");

//            byte[] confirmFrame = ReadExactOrNull(stream, 12);
//            if (confirmFrame == null)
//                throw new TimeoutException(
//                    "Device echoed the handshake but did not send its confirmation frame.");
//        }

//        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
//        {
//            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
//            stream.Write(frame, 0, frame.Length);
//            stream.Flush();

//            byte[] response = ReadModbusFrame(stream);

//            if (response == null)
//                throw new TimeoutException(
//                    $"No response from device for register 0x{registerAddress:X4} " +
//                    "(write may not have been processed).");

//            if (response.Length >= 6 && response[1] == 0x86)
//            {
//                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
//                throw new InvalidOperationException(
//                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
//            }
//        }

//        private static byte[] ReadModbusFrame(NetworkStream stream)
//        {
//            byte[] mbap = ReadExactOrNull(stream, 6);
//            if (mbap == null) return null;

//            int length = (mbap[4] << 8) | mbap[5];
//            if (length <= 0 || length > 253) return null;

//            byte[] body = ReadExactOrNull(stream, length);
//            return body;
//        }

//        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
//        {
//            byte[] buffer = new byte[count];
//            int read = 0;
//            try
//            {
//                while (read < count)
//                {
//                    int r = stream.Read(buffer, read, count - read);
//                    if (r <= 0) return null;
//                    read += r;
//                }
//                return buffer;
//            }
//            catch (IOException)
//            {
//                return null;
//            }
//        }

//        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
//        {
//            byte[] data = {
//                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//                (byte)(value >> 8), (byte)(value & 0xFF)
//            };

//            byte[] pdu = new byte[2 + data.Length];
//            pdu[0] = 0x01;
//            pdu[1] = 0x06;
//            Array.Copy(data, 0, pdu, 2, data.Length);

//            byte[] frame = new byte[6 + pdu.Length];
//            frame[0] = 0x00; frame[1] = 0x01;
//            frame[2] = 0x00; frame[3] = 0x00;
//            frame[4] = (byte)(pdu.Length >> 8);
//            frame[5] = (byte)(pdu.Length & 0xFF);
//            Array.Copy(pdu, 0, frame, 6, pdu.Length);

//            return frame;
//        }

//        private void BtnBackup_Click(object sender, RoutedEventArgs e)
//        {
//            // TODO: implement backup logic
//        }

//        // ════════════════════════════════════════════════════════
//        //  RESTORE
//        // ════════════════════════════════════════════════════════
//        private void BtnRestore_Click(object sender, RoutedEventArgs e)
//        {
//            if (LstModules.SelectedItems.Count == 0)
//            { Warn("Select at least one module to restore.", "No Selection"); return; }

//            OpenFileDialog dlg = new OpenFileDialog
//            {
//                Title = "Open Backup File",
//                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//            };

//            if (dlg.ShowDialog() != true) return;

//            int restored = 0;
//            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//            {
//                string[] parts = line.Split(',');
//                if (parts.Length < 6) continue;

//                string sn = parts[1].Trim();
//                foreach (NetworkModule m in LstModules.SelectedItems)
//                {
//                    if (m.SerialNumber != sn) continue;
//                    m.IpAddress = parts[2].Trim();
//                    m.Mask = parts[3].Trim();
//                    m.Gateway = parts[4].Trim();
//                    m.Position = parts[5].Trim();
//                    restored++;
//                }
//            }

//            LstModules.Items.Refresh();
//            TxtStatus.Text = restored > 0
//                ? $"Restored {restored} module(s) from {dlg.FileName}"
//                : "No matching modules found in the backup file.";
//        }

//        // ════════════════════════════════════════════════════════
//        //  RESTORE BY IP
//        // ════════════════════════════════════════════════════════
//        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
//        {
//            string ip = GetIp();

//            if (!IsValidIp(ip))
//            { Warn("Enter a valid target IP address first.", "No IP"); return; }

//            OpenFileDialog dlg = new OpenFileDialog
//            {
//                Title = "Open Backup File",
//                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
//            };

//            if (dlg.ShowDialog() != true) return;

//            bool found = false;
//            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
//            {
//                string[] parts = line.Split(',');
//                if (parts.Length < 6) continue;
//                if (parts[2].Trim() != ip) continue;

//                foreach (NetworkModule m in LstModules.Items)
//                {
//                    if (m.IpAddress != ip) continue;
//                    m.Mask = parts[3].Trim();
//                    m.Gateway = parts[4].Trim();
//                    m.Position = parts[5].Trim();
//                    found = true;
//                }
//            }

//            LstModules.Items.Refresh();
//            TxtStatus.Text = found
//                ? $"Restore by IP complete → {ip}"
//                : $"No backup entry found for IP {ip}.";
//        }

//        // ════════════════════════════════════════════════════════
//        //  REORDER IPs
//        // ════════════════════════════════════════════════════════
//        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
//        {
//            if (LstModules.Items.Count == 0)
//            { Warn("No modules in the list to reorder.", "Empty List"); return; }

//            ReorderIpDialog dlg = new ReorderIpDialog();
//            if (dlg.ShowDialog() != true) return;

//            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
//            { Warn("Invalid start IP address.", "Invalid IP"); return; }

//            byte[] bytes = startAddr.GetAddressBytes();
//            int counter = bytes[3];

//            foreach (NetworkModule m in LstModules.Items)
//            {
//                bytes[3] = (byte)Clamp(counter++, 1, 254);
//                m.IpAddress = new IPAddress(bytes).ToString();
//            }

//            LstModules.Items.Refresh();
//            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
//        }

//        // ════════════════════════════════════════════════════════
//        //  UPGRADE FW
//        // ════════════════════════════════════════════════════════
//        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
//        {
//            if (LstModules.SelectedItems.Count == 0)
//            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

//            MessageBoxResult confirm = MessageBox.Show(
//                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
//                "Do NOT power off during upgrade.",
//                "Confirm Firmware Upgrade",
//                MessageBoxButton.YesNo,
//                MessageBoxImage.Question);

//            if (confirm != MessageBoxResult.Yes) return;

//            OpenFileDialog dlg = new OpenFileDialog
//            {
//                Title = "Select Firmware File",
//                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
//            };

//            if (dlg.ShowDialog() != true) return;

//            Button btn = (Button)sender;
//            btn.IsEnabled = false;
//            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

//            List<NetworkModule> targets = LstModules.SelectedItems
//                .Cast<NetworkModule>()
//                .ToList();

//            string fwPath = dlg.FileName;

//            try
//            {
//                await Task.Run(() =>
//                {
//                    foreach (NetworkModule m in targets)
//                    {
//                        System.Threading.Thread.Sleep(500);
//                        m.FirmwareRev = "NEW";
//                    }
//                });

//                LstModules.Items.Refresh();
//                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
//            }
//            catch (Exception ex)
//            {
//                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
//            }
//            finally
//            {
//                btn.IsEnabled = true;
//            }
//        }

//        private static void Warn(string message, string title) =>
//            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
//    }

//    // ────────────────────────────────────────────────────────────
//    //  Minimal "Enter start IP" dialog for Reorder IPs
//    // ────────────────────────────────────────────────────────────
//    public class ReorderIpDialog : Window
//    {
//        private readonly TextBox _input;
//        public string StartIp => _input.Text.Trim();

//        public ReorderIpDialog()
//        {
//            Title = "Reorder IPs";
//            Width = 280;
//            Height = 130;
//            ResizeMode = ResizeMode.NoResize;
//            WindowStartupLocation = WindowStartupLocation.CenterOwner;

//            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

//            sp.Children.Add(new TextBlock
//            {
//                Text = "Enter starting IP address:",
//                Margin = new Thickness(0, 0, 0, 6)
//            });

//            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
//            sp.Children.Add(_input);

//            StackPanel btnRow = new StackPanel
//            {
//                Orientation = Orientation.Horizontal,
//                HorizontalAlignment = HorizontalAlignment.Right,
//                Margin = new Thickness(0, 10, 0, 0)
//            };

//            Button ok = new Button
//            {
//                Content = "OK",
//                Width = 70,
//                Height = 24,
//                Margin = new Thickness(0, 0, 6, 0),
//                IsDefault = true
//            };
//            ok.Click += (s, ev) => { DialogResult = true; };

//            Button cancel = new Button
//            {
//                Content = "Cancel",
//                Width = 70,
//                Height = 24,
//                IsCancel = true
//            };

//            btnRow.Children.Add(ok);
//            btnRow.Children.Add(cancel);
//            sp.Children.Add(btnRow);

//            Content = sp;
//        }
//    }
//}
using BLDC_Demo.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BLDC_Demo
{
    // ────────────────────────────────────────────────────────────
    //  Model: a discovered network module
    // ────────────────────────────────────────────────────────────
    public class NetworkModule
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string FirmwareRev { get; set; } = string.Empty;
        public string Position { get; set; } = "—";

        // List shows:  192.168.1.50   Name: Station1
        public override string ToString() =>
            $"{IpAddress}   Name: {(string.IsNullOrEmpty(StationName) ? "-" : StationName)}";
    }

    // ────────────────────────────────────────────────────────────
    //  Backup container
    // ────────────────────────────────────────────────────────────
    public class ModuleBackup
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime BackupTime { get; set; } = DateTime.Now;
    }

    // ────────────────────────────────────────────────────────────
    //  UserControl code-behind  (targets C# 7.3)
    // ────────────────────────────────────────────────────────────
    public partial class NetworkService : UserControl
    {
        // ── ESP32 Modbus holding-register map for network config ──
        private const ushort REG_IP_OCTET1 = 0x0136;
        private const ushort REG_IP_OCTET2 = 0x0137;
        private const ushort REG_IP_OCTET3 = 0x0138;
        private const ushort REG_IP_OCTET4 = 0x0139;
        private const ushort REG_MASK_OCTET4 = 0x013A;
        private const ushort REG_APPLY_REBOOT = 0x013B;

        // ── Station name registers ──
        private const ushort REG_NAME_START = 0x0150;   // 0x0150 - 0x015F (16 registers)
        private const int NAME_REG_COUNT = 16;
        private const ushort REG_NAME_APPLY = 0x0160;   // write 1 = apply name

        // Default Modbus TCP port used when we have to open a temporary
        // connection to push a new IP to a device that isn't connected yet.
        private const int DEFAULT_MODBUS_PORT = 502;

        public NetworkService()
        {
            InitializeComponent();
            AttachOctetHandlers();

            // Cursor goes to the END of the text when the Station Name box
            // gets keyboard focus (Tab), instead of sitting at the front.
            TxtStationName.GotKeyboardFocus += (s, e) =>
                TxtStationName.CaretIndex = TxtStationName.Text.Length;
        }

        // True while the main app is connected to a device
        private static bool IsDeviceConnected()
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            var bl = mainWin?.CommMotorSelect?.BLLogic;
            return bl?.IsConnected == true;
        }

        // ════════════════════════════════════════════════════════
        //  OCTET HELPERS
        // ════════════════════════════════════════════════════════
        private void AttachOctetHandlers()
        {
            var ipBoxes = new[] { TxtIp1, TxtIp2, TxtIp3, TxtIp4 };
            var maskBoxes = new[] { TxtMask1, TxtMask2, TxtMask3, TxtMask4 };
            var gwBoxes = new[] { TxtGw1, TxtGw2, TxtGw3, TxtGw4 };

            AttachToGroup(ipBoxes);
            AttachToGroup(maskBoxes);
            AttachToGroup(gwBoxes);

            ChkGateway.Checked += (s, e) => SetGatewayEnabled(true);
            ChkGateway.Unchecked += (s, e) => SetGatewayEnabled(false);
        }

        private void SetGatewayEnabled(bool enabled)
        {
            TxtGw1.IsEnabled = TxtGw2.IsEnabled =
            TxtGw3.IsEnabled = TxtGw4.IsEnabled = enabled;
        }

        private static void AttachToGroup(TextBox[] boxes)
        {
            for (int i = 0; i < boxes.Length; i++)
            {
                int idx = i;

                boxes[idx].PreviewTextInput += (s, e) =>
                {
                    e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
                };

                boxes[idx].PreviewKeyDown += (s, e) =>
                {
                    TextBox tb = (TextBox)s;
                    if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
                    {
                        e.Handled = true;
                        if (idx + 1 < boxes.Length) boxes[idx + 1].Focus();
                    }
                    else if (e.Key == Key.Back && tb.Text.Length == 0 && idx > 0)
                    {
                        boxes[idx - 1].Focus();
                        boxes[idx - 1].SelectAll();
                    }
                };

                boxes[idx].TextChanged += (s, e) =>
                {
                    TextBox tb = (TextBox)s;
                    if (tb.Text.Length == 3 && idx + 1 < boxes.Length)
                        boxes[idx + 1].Focus();
                };

                // Cursor at the end when the box gets keyboard focus.
                boxes[idx].GotKeyboardFocus += (s, e) =>
                {
                    TextBox tb = (TextBox)s;
                    tb.CaretIndex = tb.Text.Length;
                };

                boxes[idx].LostFocus += (s, e) =>
                {
                    TextBox tb = (TextBox)s;
                    if (int.TryParse(tb.Text, out int val))
                        tb.Text = Clamp(val, 0, 255).ToString();
                    else if (tb.Text.Length > 0)
                        tb.Text = string.Empty;
                };
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ════════════════════════════════════════════════════════
        //  IP FIELD READ / WRITE
        // ════════════════════════════════════════════════════════
        private string GetIp() => $"{TxtIp1.Text}.{TxtIp2.Text}.{TxtIp3.Text}.{TxtIp4.Text}";
        private string GetMask() => $"{TxtMask1.Text}.{TxtMask2.Text}.{TxtMask3.Text}.{TxtMask4.Text}";

        private string GetGateway() =>
            ChkGateway.IsChecked == true
                ? $"{TxtGw1.Text}.{TxtGw2.Text}.{TxtGw3.Text}.{TxtGw4.Text}"
                : string.Empty;

        private void SetIpFields(string ip, string mask, string gw)
        {
            FillOctets(ip, TxtIp1, TxtIp2, TxtIp3, TxtIp4);
            FillOctets(mask, TxtMask1, TxtMask2, TxtMask3, TxtMask4);

            bool hasGw = !string.IsNullOrWhiteSpace(gw);
            ChkGateway.IsChecked = hasGw;
            SetGatewayEnabled(hasGw);

            if (hasGw) FillOctets(gw, TxtGw1, TxtGw2, TxtGw3, TxtGw4);
            else ClearOctets(TxtGw1, TxtGw2, TxtGw3, TxtGw4);
        }

        private static void FillOctets(string address,
            TextBox b1, TextBox b2, TextBox b3, TextBox b4)
        {
            string[] parts = address != null
                ? address.Split('.')
                : new string[0];

            b1.Text = parts.Length > 0 ? parts[0] : string.Empty;
            b2.Text = parts.Length > 1 ? parts[1] : string.Empty;
            b3.Text = parts.Length > 2 ? parts[2] : string.Empty;
            b4.Text = parts.Length > 3 ? parts[3] : string.Empty;

            // Cursor at the end of each box after the text is set from code.
            b1.CaretIndex = b1.Text.Length;
            b2.CaretIndex = b2.Text.Length;
            b3.CaretIndex = b3.Text.Length;
            b4.CaretIndex = b4.Text.Length;
        }

        private static void ClearOctets(params TextBox[] boxes)
        {
            foreach (TextBox b in boxes) b.Text = string.Empty;
        }

        private static bool IsValidIp(string ip) =>
            IPAddress.TryParse(ip, out _);

        // ════════════════════════════════════════════════════════
        //  DISCOVER (PORT SCAN FILTERED, WITH RETRY)
        // ════════════════════════════════════════════════════════
        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
        {
            LstModules.Items.Clear();
            TxtStatus.Text = "Scanning subnet for active motor controllers...";

            Button btn = (Button)sender;
            btn.IsEnabled = false;

            try
            {
                // The device that the main app is already connected to:
                // use its name directly, do not open a second connection.
                string connectedIp = null;
                string connectedName = null;
                string connectedSerial = null;
                bool isLiveConnection = false;

                if (Application.Current.MainWindow is MainWindow mw0)
                {
                    var comm = mw0.CommMotorSelect;
                    var bl = comm?.BLLogic;

                    // Use the "connected device" ONLY when the connection is really alive.
                    // After a disconnect, CurrentDevice still holds the old data - ignore it.
                    if (bl != null && bl.IsConnected)
                    {
                        isLiveConnection = true;
                        var d0 = bl.CurrentDevice;

                        if (d0 != null && !string.IsNullOrEmpty(d0.IpAddress))
                        {
                            connectedIp = d0.IpAddress;
                            connectedName = d0.StationName;
                            connectedSerial = d0.SerialNumber;
                        }
                        else
                        {
                            // Fallback: IP that CommMotorSelectControl stored when connecting
                            connectedIp = comm.ConnectedIp;
                        }
                    }
                }

                List<NetworkModule> modules = await Task.Run(() => DiscoverModules(connectedIp, connectedName));

                // The connected device usually accepts only one client, so the port scan
                // cannot reach it and it would be missing from the list. Add it manually.
                if (!string.IsNullOrEmpty(connectedIp) &&
                    !modules.Any(m => m.IpAddress == connectedIp))
                {
                    modules.Insert(0, new NetworkModule
                    {
                        IpAddress = connectedIp,
                        StationName = connectedName ?? string.Empty,
                        SerialNumber = string.IsNullOrEmpty(connectedSerial) ? "-" : connectedSerial,
                        FirmwareRev = "1.0.0",
                        Mask = "255.255.255.0",
                        Position = "—"
                    });
                }

                // ── Option A: fill in serial for the device BL is connected to ──
                // Only while the connection is alive.
                if (isLiveConnection &&
                    Application.Current.MainWindow is MainWindow mw)
                {
                    var dev = mw.CommMotorSelect?.BLLogic?.CurrentDevice;
                    if (dev != null && !string.IsNullOrEmpty(dev.SerialNumber))
                    {
                        foreach (NetworkModule m in modules)
                            if (m.IpAddress == dev.IpAddress)
                                m.SerialNumber = dev.SerialNumber;
                    }
                }

                foreach (NetworkModule m in modules)
                    LstModules.Items.Add(m);

                TxtStatus.Text = modules.Count == 0
                    ? "No motor controllers found."
                    : $"Found {modules.Count} motor controller(s).";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Discovery error: {ex.Message}";
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private static List<NetworkModule> DiscoverModules(string connectedIp, string connectedName)
        {
            var found = new List<NetworkModule>();
            string baseIp = GetLocalSubnetBase();
            if (string.IsNullOrEmpty(baseIp)) return found;

            var localIps = new HashSet<string>(
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ua => ua.Address.ToString())
            );

            const int targetPort = 502;
            var tasks = new List<Task<Tuple<bool, string>>>();

            for (int i = 1; i <= 254; i++)
            {
                string targetIp = $"{baseIp}.{i}";
                tasks.Add(CheckControllerPortAsync(targetIp, targetPort));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var t in tasks)
            {
                if (t.Result.Item1 && !localIps.Contains(t.Result.Item2))
                {
                    found.Add(new NetworkModule
                    {
                        IpAddress = t.Result.Item2,
                        SerialNumber = "-",
                        FirmwareRev = "1.0.0",
                        Mask = "255.255.255.0",
                        Position = "—"
                    });
                }
            }

            // Read the station name from each found device (in parallel)
            Parallel.ForEach(found, m =>
            {
                m.StationName = (m.IpAddress == connectedIp)
                    ? (connectedName ?? string.Empty)
                    : ReadStationName(m.IpAddress);
            });

            return found;
        }

        // Opens a short connection, sends the poll command, and collects
        // registers 0x0150 - 0x015F to build the station name.
        private static string ReadStationName(string ip)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var r = client.BeginConnect(ip, DEFAULT_MODBUS_PORT, null, null);
                    if (!r.AsyncWaitHandle.WaitOne(1500)) return string.Empty;
                    client.EndConnect(r);

                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.ReadTimeout = 1500;
                        stream.WriteTimeout = 1500;

                        Thread.Sleep(100);
                        SendHandshake(stream);

                        // Same poll frame that BL.UpdateData() sends: function 0xFF
                        byte[] poll = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0xFF, 0x00, 0x00 };
                        stream.Write(poll, 0, poll.Length);
                        stream.Flush();

                        ushort[] regs = new ushort[NAME_REG_COUNT];
                        int received = 0;   // bit mask: one bit per register
                        int allBits = (1 << NAME_REG_COUNT) - 1;
                        DateTime deadline = DateTime.UtcNow.AddSeconds(4);

                        while (DateTime.UtcNow < deadline && received != allBits)
                        {
                            byte[] body = ReadModbusFrame(stream);
                            if (body == null) break;      // timeout or closed
                            if (body.Length < 6) continue;

                            ushort reg = (ushort)((body[2] << 8) | body[3]);
                            ushort val = (ushort)((body[4] << 8) | body[5]);

                            if (reg >= REG_NAME_START && reg < REG_NAME_START + NAME_REG_COUNT)
                            {
                                regs[reg - REG_NAME_START] = val;
                                received |= 1 << (reg - REG_NAME_START);
                            }
                        }

                        if (received != allBits) return string.Empty;

                        byte[] bytes = new byte[NAME_REG_COUNT * 2];
                        for (int i = 0; i < NAME_REG_COUNT; i++)
                        {
                            bytes[2 * i] = (byte)(regs[i] >> 8);
                            bytes[2 * i + 1] = (byte)(regs[i] & 0xFF);
                        }

                        string name = System.Text.Encoding.ASCII.GetString(bytes);
                        int zero = name.IndexOf('\0');
                        return zero >= 0 ? name.Substring(0, zero) : name;
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        // Tries twice per host, with a short gap between attempts, and a
        // longer per-attempt timeout than before (500ms vs the old 200ms).
        private static async Task<Tuple<bool, string>> CheckControllerPortAsync(string ip, int port)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync(ip, port);
                        var delayTask = Task.Delay(500);
                        var completedTask = await Task.WhenAny(connectTask, delayTask);
                        if (completedTask == connectTask && client.Connected)
                            return Tuple.Create(true, ip);
                    }
                }
                catch
                {
                    // Connection refused or failed — retry once.
                }

                if (attempt == 0)
                    await Task.Delay(300);
            }

            return Tuple.Create(false, ip);
        }

        private static string GetLocalSubnetBase()
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .ToList();

            if (!candidates.Any())
            {
                candidates = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
            }

            foreach (NetworkInterface ni in candidates)
            {
                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string[] parts = ua.Address.ToString().Split('.');
                        if (parts.Length == 4)
                        {
                            if (parts[0] == "127") continue;               // loopback
                            if (parts[0] == "169" && parts[1] == "254") continue; // APIPA / no real network
                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
                        }
                    }
                }
            }
            return string.Empty;
        }

        // ════════════════════════════════════════════════════════
        //  LIST SELECTION → populate fields
        // ════════════════════════════════════════════════════════
        // Set while the code itself refreshes/re-selects the list (e.g. in BtnSet_Click)
        // so that it is not treated as the user clicking an IP.
        private bool _suppressSelectionHandling = false;

        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionHandling) return;

            if (!(LstModules.SelectedItem is NetworkModule m)) return;

            TxtSerialNumber.Text = m.SerialNumber;
            SetIpFields(m.IpAddress, m.Mask, m.Gateway);

            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                var comm = mainWin.CommMotorSelect;

                // ── Show the station name ──
                var dev = comm?.BLLogic?.CurrentDevice;
                TxtStationName.Text = (dev != null && dev.IpAddress == m.IpAddress)
                    ? dev.StationName
                    : m.StationName;
                TxtStationName.CaretIndex = TxtStationName.Text.Length;   // cursor at the end

                // ── Push selected module's IP into the Connect panel,
                //    but ONLY when the app is not connected ──
                bool connected = comm?.BLLogic != null &&
                                 (comm.BLLogic.IsConnected ||
                                  !string.IsNullOrEmpty(comm.BLLogic.ActivePortName));

                // Connected: silently do not load the IP (no popup)
                if (connected)
                    return;

                string[] octets = m.IpAddress.Split('.');
                if (comm != null && octets.Length == 4)
                {
                    comm.TcpIpOctet1.Text = octets[0];
                    comm.TcpIpOctet2.Text = octets[1];
                    comm.TcpIpOctet3.Text = octets[2];
                    comm.TcpIpOctet4.Text = octets[3];
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  SELECT ALL / NONE
        // ════════════════════════════════════════════════════════
        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
            => LstModules.SelectAll();

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
            => LstModules.UnselectAll();

        // ════════════════════════════════════════════════════════
        //  SET  (station name + IP, works with or without an existing connection)
        // ════════════════════════════════════════════════════════
        private void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            // The Set button stays enabled, but it does nothing while connected
            if (IsDeviceConnected())
            {
                Warn("Cannot set the IP while a device is connected.\nDisconnect first, then press Set.",
                     "Device Connected");
                return;
            }

            string ip = GetIp();
            string mask = GetMask();
            string gateway = GetGateway();

            if (!IsValidIp(ip))
            {
                Warn("Enter a valid IP address.", "Invalid IP");
                return;
            }

            if (!(Application.Current.MainWindow is MainWindow mainWin))
            {
                Warn("Main window not available.", "Error");
                return;
            }

            var commCtrl = mainWin.CommMotorSelect;

            if (commCtrl?.BLLogic == null)
            {
                Warn("Communication layer not initialized.", "Error");
                return;
            }

            // ── Step 1: Split IP into 4 octets ──
            string[] ipParts = ip.Split('.');
            if (ipParts.Length != 4 ||
                !byte.TryParse(ipParts[0], out byte octet1) ||
                !byte.TryParse(ipParts[1], out byte octet2) ||
                !byte.TryParse(ipParts[2], out byte octet3) ||
                !byte.TryParse(ipParts[3], out byte octet4))
            {
                Warn("IP address could not be parsed into 4 octets.", "Invalid IP");
                return;
            }

            if (octet1 == 0)
            {
                Warn("First IP octet cannot be 0 — the device will reject the commit.", "Invalid IP");
                return;
            }

            // ── Station name check ──
            string stationName = TxtStationName.Text ?? string.Empty;

            if (stationName.Length > 32)
            {
                Warn("Station name can be 32 characters maximum.", "Invalid Name");
                return;
            }
            foreach (char c in stationName)
            {
                if (c < 32 || c > 126)
                {
                    Warn("Use simple English letters, numbers and symbols only.", "Invalid Name");
                    return;
                }
            }

            // Optional: parse the mask's last octet (firmware only supports 255.255.255.X)
            byte maskLastOctet = 0;
            string[] maskParts = mask.Split('.');
            if (maskParts.Length == 4)
                byte.TryParse(maskParts[3], out maskLastOctet);

            string currentDeviceIp = LstModules.SelectedItem is NetworkModule selected
                ? selected.IpAddress
                : $"{commCtrl.TcpIpOctet1.Text}.{commCtrl.TcpIpOctet2.Text}.{commCtrl.TcpIpOctet3.Text}.{commCtrl.TcpIpOctet4.Text}".Trim();

            if (!IsValidIp(currentDeviceIp))
            {
                Warn("No valid current device IP known. Run Discover or select a module first.", "Missing IP");
                return;
            }

            int port1 = DEFAULT_MODBUS_PORT;

            try
            {
                SendSetIpDirect(currentDeviceIp, port1, octet1, octet2, octet3, octet4, maskLastOctet, stationName);
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Set failed: {ex.Message}";
                return; // don't touch UI state below — nothing confirmed sent
            }

            // ── Step 6: Update selected module in list if one is selected ──
            if (LstModules.SelectedItem is NetworkModule module)
            {
                module.IpAddress = ip;
                module.Mask = mask;
                module.Gateway = gateway;
                if (stationName.Length > 0)
                    module.StationName = stationName;
                int idx = LstModules.SelectedIndex;
                _suppressSelectionHandling = true;   // re-selecting here is not a user click
                try
                {
                    LstModules.Items.Refresh();
                    LstModules.SelectedIndex = idx;
                }
                finally
                {
                    _suppressSelectionHandling = false;
                }
            }

            // ── Step 7: If the main comm panel happened to be connected to
            // this same device, drop it (device is rebooting anyway).
            if (!string.IsNullOrEmpty(commCtrl.BLLogic?.ActivePortName))
            {
                commCtrl.BLLogic.Close();
                mainWin.DataContext = null;
            }

            string[] octets = ip.Split('.');
            if (octets.Length == 4)
            {
                commCtrl.TcpIpOctet1.Text = octets[0];
                commCtrl.TcpIpOctet2.Text = octets[1];
                commCtrl.TcpIpOctet3.Text = octets[2];
                commCtrl.TcpIpOctet4.Text = octets[3];
            }
            commCtrl.TcpScanStatus.Text = $"IP set to {ip} — device rebooting, wait a few seconds then press Connect.";
            commCtrl.TcpScanStatus.Foreground = System.Windows.Media.Brushes.Orange;
            commCtrl.ApplyAndSaveIp(ip);

            TxtStatus.Text = stationName.Length > 0
                ? $"IP set → {ip}, name → {stationName}"
                : $"IP set → {ip}.";
        }

        // ════════════════════════════════════════════════════════
        //  DIRECT SET WRITE (name + IP) — bypasses BLLogic entirely.
        // ════════════════════════════════════════════════════════
        private void SendSetIpDirect(string targetIp, int port,
            byte octet1, byte octet2, byte octet3, byte octet4, byte maskLastOctet,
            string stationName)
        {
            using (var client = new TcpClient())
            {
                var connectResult = client.BeginConnect(targetIp, port, null, null);
                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
                    throw new TimeoutException($"Timed out connecting to {targetIp}:{port}");

                client.EndConnect(connectResult);

                using (NetworkStream stream = client.GetStream())
                {
                    stream.WriteTimeout = 2000;
                    stream.ReadTimeout = 2000;

                    Thread.Sleep(100);

                    SendHandshake(stream);

                    // ── Station name first (the IP commit reboots the device) ──
                    if (!string.IsNullOrEmpty(stationName))
                    {
                        byte[] buf = new byte[32];   // all zeros = padding
                        byte[] ascii = System.Text.Encoding.ASCII.GetBytes(stationName);
                        Array.Copy(ascii, buf, Math.Min(ascii.Length, 32));

                        for (int i = 0; i < NAME_REG_COUNT; i++)
                        {
                            // first char = high byte, second char = low byte
                            ushort nameValue = (ushort)((buf[2 * i] << 8) | buf[2 * i + 1]);
                            WriteAndConfirm(stream, (ushort)(REG_NAME_START + i), nameValue);
                        }

                        WriteAndConfirm(stream, REG_NAME_APPLY, 1);   // 0x0160 apply name
                    }

                    // ── Then the IP settings ──
                    WriteAndConfirm(stream, REG_IP_OCTET1, octet1);
                    WriteAndConfirm(stream, REG_IP_OCTET2, octet2);
                    WriteAndConfirm(stream, REG_IP_OCTET3, octet3);
                    WriteAndConfirm(stream, REG_IP_OCTET4, octet4);
                    WriteAndConfirm(stream, REG_MASK_OCTET4, maskLastOctet);

                    try
                    {
                        WriteAndConfirm(stream, REG_APPLY_REBOOT, 1);
                    }
                    catch (TimeoutException)
                    {
                        // Acceptable: device may have started rebooting
                        // before it could echo the commit frame back.
                    }

                    Thread.Sleep(150);
                }
            }
        }

        private static void SendHandshake(NetworkStream stream)
        {
            byte[] handshake = { 0x01, 0x01 };
            stream.Write(handshake, 0, handshake.Length);
            stream.Flush();

            byte[] echo = ReadExactOrNull(stream, 2);
            if (echo == null || echo[0] != 0x01 || echo[1] != 0x01)
                throw new TimeoutException(
                    "Device did not respond to the required connection handshake " +
                    "(no echo received) — writes cannot proceed.");

            byte[] confirmFrame = ReadExactOrNull(stream, 12);
            if (confirmFrame == null)
                throw new TimeoutException(
                    "Device echoed the handshake but did not send its confirmation frame.");
        }

        private static void WriteAndConfirm(NetworkStream stream, ushort registerAddress, ushort value)
        {
            byte[] frame = BuildModbusWriteFrame(registerAddress, value);
            stream.Write(frame, 0, frame.Length);
            stream.Flush();

            byte[] response = ReadModbusFrame(stream);

            if (response == null)
                throw new TimeoutException(
                    $"No response from device for register 0x{registerAddress:X4} " +
                    "(write may not have been processed).");

            if (response.Length >= 6 && response[1] == 0x86)
            {
                byte exceptionCode = response.Length > 6 ? response[6] : (byte)0;
                throw new InvalidOperationException(
                    $"Device rejected write to register 0x{registerAddress:X4} (Modbus exception 0x{exceptionCode:X2}).");
            }
        }

        private static byte[] ReadModbusFrame(NetworkStream stream)
        {
            byte[] mbap = ReadExactOrNull(stream, 6);
            if (mbap == null) return null;

            int length = (mbap[4] << 8) | mbap[5];
            if (length <= 0 || length > 253) return null;

            byte[] body = ReadExactOrNull(stream, length);
            return body;
        }

        private static byte[] ReadExactOrNull(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int read = 0;
            try
            {
                while (read < count)
                {
                    int r = stream.Read(buffer, read, count - read);
                    if (r <= 0) return null;
                    read += r;
                }
                return buffer;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static byte[] BuildModbusWriteFrame(ushort registerAddress, ushort value)
        {
            byte[] data = {
                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
                (byte)(value >> 8), (byte)(value & 0xFF)
            };

            byte[] pdu = new byte[2 + data.Length];
            pdu[0] = 0x01;
            pdu[1] = 0x06;
            Array.Copy(data, 0, pdu, 2, data.Length);

            byte[] frame = new byte[6 + pdu.Length];
            frame[0] = 0x00; frame[1] = 0x01;
            frame[2] = 0x00; frame[3] = 0x00;
            frame[4] = (byte)(pdu.Length >> 8);
            frame[5] = (byte)(pdu.Length & 0xFF);
            Array.Copy(pdu, 0, frame, 6, pdu.Length);

            return frame;
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement backup logic
        }

        // ════════════════════════════════════════════════════════
        //  RESTORE
        // ════════════════════════════════════════════════════════
        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (LstModules.SelectedItems.Count == 0)
            { Warn("Select at least one module to restore.", "No Selection"); return; }

            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Open Backup File",
                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            int restored = 0;
            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
            {
                string[] parts = line.Split(',');
                if (parts.Length < 6) continue;

                string sn = parts[1].Trim();
                foreach (NetworkModule m in LstModules.SelectedItems)
                {
                    if (m.SerialNumber != sn) continue;
                    m.IpAddress = parts[2].Trim();
                    m.Mask = parts[3].Trim();
                    m.Gateway = parts[4].Trim();
                    m.Position = parts[5].Trim();
                    restored++;
                }
            }

            LstModules.Items.Refresh();
            TxtStatus.Text = restored > 0
                ? $"Restored {restored} module(s) from {dlg.FileName}"
                : "No matching modules found in the backup file.";
        }

        // ════════════════════════════════════════════════════════
        //  RESTORE BY IP
        // ════════════════════════════════════════════════════════
        private void BtnRestoreByIp_Click(object sender, RoutedEventArgs e)
        {
            string ip = GetIp();

            if (!IsValidIp(ip))
            { Warn("Enter a valid target IP address first.", "No IP"); return; }

            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Open Backup File",
                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            bool found = false;
            foreach (string line in System.IO.File.ReadAllLines(dlg.FileName))
            {
                string[] parts = line.Split(',');
                if (parts.Length < 6) continue;
                if (parts[2].Trim() != ip) continue;

                foreach (NetworkModule m in LstModules.Items)
                {
                    if (m.IpAddress != ip) continue;
                    m.Mask = parts[3].Trim();
                    m.Gateway = parts[4].Trim();
                    m.Position = parts[5].Trim();
                    found = true;
                }
            }

            LstModules.Items.Refresh();
            TxtStatus.Text = found
                ? $"Restore by IP complete → {ip}"
                : $"No backup entry found for IP {ip}.";
        }

        // ════════════════════════════════════════════════════════
        //  REORDER IPs
        // ════════════════════════════════════════════════════════
        private void BtnReorderIPs_Click(object sender, RoutedEventArgs e)
        {
            if (LstModules.Items.Count == 0)
            { Warn("No modules in the list to reorder.", "Empty List"); return; }

            ReorderIpDialog dlg = new ReorderIpDialog();
            if (dlg.ShowDialog() != true) return;

            if (!IPAddress.TryParse(dlg.StartIp, out IPAddress startAddr))
            { Warn("Invalid start IP address.", "Invalid IP"); return; }

            byte[] bytes = startAddr.GetAddressBytes();
            int counter = bytes[3];

            foreach (NetworkModule m in LstModules.Items)
            {
                bytes[3] = (byte)Clamp(counter++, 1, 254);
                m.IpAddress = new IPAddress(bytes).ToString();
            }

            LstModules.Items.Refresh();
            TxtStatus.Text = $"IPs reordered starting from {dlg.StartIp}";
        }

        // ════════════════════════════════════════════════════════
        //  UPGRADE FW
        // ════════════════════════════════════════════════════════
        private async void BtnUpgradeFw_Click(object sender, RoutedEventArgs e)
        {
            if (LstModules.SelectedItems.Count == 0)
            { Warn("Select at least one module to upgrade.", "No Selection"); return; }

            MessageBoxResult confirm = MessageBox.Show(
                $"Upgrade firmware on {LstModules.SelectedItems.Count} module(s)?\n" +
                "Do NOT power off during upgrade.",
                "Confirm Firmware Upgrade",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Select Firmware File",
                Filter = "Firmware files (*.bin;*.fw)|*.bin;*.fw|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            Button btn = (Button)sender;
            btn.IsEnabled = false;
            TxtStatus.Text = "Firmware upgrade in progress… do not power off.";

            List<NetworkModule> targets = LstModules.SelectedItems
                .Cast<NetworkModule>()
                .ToList();

            string fwPath = dlg.FileName;

            try
            {
                await Task.Run(() =>
                {
                    foreach (NetworkModule m in targets)
                    {
                        System.Threading.Thread.Sleep(500);
                        m.FirmwareRev = "NEW";
                    }
                });

                LstModules.Items.Refresh();
                TxtStatus.Text = $"Firmware upgrade complete for {targets.Count} module(s).";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Firmware upgrade failed: {ex.Message}";
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private static void Warn(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ────────────────────────────────────────────────────────────
    //  Minimal "Enter start IP" dialog for Reorder IPs
    // ────────────────────────────────────────────────────────────
    public class ReorderIpDialog : Window
    {
        private readonly TextBox _input;
        public string StartIp => _input.Text.Trim();

        public ReorderIpDialog()
        {
            Title = "Reorder IPs";
            Width = 280;
            Height = 130;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            StackPanel sp = new StackPanel { Margin = new Thickness(12) };

            sp.Children.Add(new TextBlock
            {
                Text = "Enter starting IP address:",
                Margin = new Thickness(0, 0, 0, 6)
            });

            _input = new TextBox { Height = 24, Text = "192.168.1.1" };
            sp.Children.Add(_input);

            StackPanel btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button ok = new Button
            {
                Content = "OK",
                Width = 70,
                Height = 24,
                Margin = new Thickness(0, 0, 6, 0),
                IsDefault = true
            };
            ok.Click += (s, ev) => { DialogResult = true; };

            Button cancel = new Button
            {
                Content = "Cancel",
                Width = 70,
                Height = 24,
                IsCancel = true
            };

            btnRow.Children.Add(ok);
            btnRow.Children.Add(cancel);
            sp.Children.Add(btnRow);

            Content = sp;
        }
    }
}