
////////////using System;
////////////using System.IO;
////////////using System.Windows;
////////////using System.Windows.Controls;
////////////using System.Windows.Media;
////////////using System.Windows.Threading;
////////////using System.Threading.Tasks;
////////////using BLDC_Demo.Models;
////////////using BLDC_Demo.Controls;

////////////namespace BLDC_Demo
////////////{
////////////    public partial class CommMotorSelectControl : UserControl
////////////    {
////////////        public static CommMotorSelectControl Instance { get; private set; }

////////////        internal BL BLLogic { get; set; }
////////////        private DispatcherTimer _pollTimer;
////////////        private bool _isBusy = false;
////////////        private DateTime _tempStatusEndTime = DateTime.MinValue;
////////////        private string _tempStatusMessage = "";

////////////        internal readonly object LockObject = new object();

////////////        public CommMotorSelectControl()
////////////        {
////////////            InitializeComponent();
////////////            Instance = this;
////////////        }

////////////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
////////////        {
////////////            // ── Load Left/Right motor parameter row definitions from JSON ──
////////////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
////////////            MotorParamConfig.LoadFromFile(paramFilePath);

////////////            // ── Restore last saved IP on every app launch ──────────
////////////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
////////////            if (!string.IsNullOrWhiteSpace(savedIp))
////////////                TcpIpAddress.Text = savedIp;

////////////            // ── Save IP whenever user manually types a new one ─────
////////////            TcpIpAddress.TextChanged += (s, args) =>
////////////            {
////////////                string currentIp = TcpIpAddress.Text.Trim();
////////////                if (!string.IsNullOrWhiteSpace(currentIp))
////////////                {
////////////                    Properties.Settings.Default.LastTcpIpAddress = currentIp;
////////////                    Properties.Settings.Default.Save();
////////////                }
////////////            };

////////////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
////////////            _pollTimer.Tick += async (s, args) =>
////////////            {
////////////                await PollActiveNodeAsync();
////////////                UpdateStatusText();
////////////            };
////////////            _pollTimer.Start();
////////////        }

////////////        // ── Called from NetworkService.BtnSet_Click ────────────────
////////////        internal void ApplyAndSaveIp(string ip)
////////////        {
////////////            TcpIpAddress.Text = ip;

////////////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
////////////            TcpScanStatus.Foreground = Brushes.Orange;
////////////        }

////////////        private async Task PollActiveNodeAsync()
////////////        {
////////////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

////////////            try
////////////            {
////////////                _isBusy = true;
////////////                await Task.Run(() => BLLogic.UpdateData());
////////////            }
////////////            catch (Exception ex)
////////////            {
////////////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
////////////            }
////////////            finally
////////////            {
////////////                _isBusy = false;
////////////            }
////////////        }

////////////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
////////////        {
////////////            // ── DISCONNECT ──────────────────────────────────────────
////////////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
////////////            {
////////////                BLLogic.Close();

////////////                if (Application.Current.MainWindow is MainWindow mw)
////////////                {
////////////                    mw.DataContext = null;
////////////                }

////////////                UpdateStatusText();
////////////                return;
////////////            }

////////////            // ── CONNECT ──────────────────────────────────────────────
////////////            if (!int.TryParse(TcpPort.Text, out int port)) return;

////////////            string ip = TcpIpAddress.Text.Trim();

////////////            Properties.Settings.Default.LastTcpIpAddress = ip;
////////////            Properties.Settings.Default.Save();

////////////            var tcp = new CommEthernet(ip, port);

////////////            if (BLLogic.Start(tcp))
////////////            {
////////////                _tempStatusEndTime = DateTime.MinValue;
////////////                UpdateStatusText();
////////////            }
////////////            else
////////////            {
////////////                _tempStatusMessage = "CONNECTION FAILED";
////////////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
////////////            }
////////////        }

////////////        // internal (not private) so MainWindow's BLLogic.OnDisconnected handler
////////////        // can force a refresh — this keeps TcpConnectButton's Content/color in
////////////        // sync any time the connection drops on its own (not just on click).
////////////        internal void UpdateStatusText()
////////////        {
////////////            if (BLLogic == null) return;

////////////            if (DateTime.Now < _tempStatusEndTime)
////////////            {
////////////                TcpScanStatus.Foreground = Brushes.Red;
////////////                TcpScanStatus.Text = _tempStatusMessage;

////////////                TcpConnectButton.Content = "CONNECT";
////////////                TcpConnectButton.IsEnabled = true;
////////////                TcpConnectButton.Background = null;
////////////            }
////////////            else if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
////////////            {
////////////                TcpScanStatus.Foreground = Brushes.LimeGreen;
////////////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

////////////                TcpConnectButton.Content = "DISCONNECT";
////////////                TcpConnectButton.IsEnabled = true;
////////////                TcpConnectButton.Background =
////////////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
////////////            }
////////////            else
////////////            {
////////////                TcpScanStatus.Foreground = Brushes.Gray;
////////////                TcpScanStatus.Text = "NOT CONNECTED";
////////////                TcpConnectButton.Content = "CONNECT";
////////////                TcpConnectButton.IsEnabled = true;
////////////                TcpConnectButton.Background = null;
////////////            }
////////////        }
////////////    }
////////////}
//////////using System;
//////////using System.IO;
//////////using System.Windows;
//////////using System.Windows.Controls;
//////////using System.Windows.Media;
//////////using System.Windows.Threading;
//////////using System.Threading.Tasks;
//////////using BLDC_Demo.Models;
//////////using BLDC_Demo.Controls;

//////////namespace BLDC_Demo
//////////{
//////////    public partial class CommMotorSelectControl : UserControl
//////////    {
//////////        public static CommMotorSelectControl Instance { get; private set; }

//////////        internal BL BLLogic { get; set; }
//////////        private DispatcherTimer _pollTimer;
//////////        private bool _isBusy = false;
//////////        private DateTime _tempStatusEndTime = DateTime.MinValue;
//////////        private string _tempStatusMessage = "";

//////////        internal readonly object LockObject = new object();

//////////        public CommMotorSelectControl()
//////////        {
//////////            InitializeComponent();
//////////            Instance = this;
//////////        }

//////////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//////////        {
//////////            // ── Load Left/Right motor parameter row definitions from JSON ──
//////////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
//////////            MotorParamConfig.LoadFromFile(paramFilePath);

//////////            // ── Restore last saved IP on every app launch ──────────
//////////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
//////////            if (!string.IsNullOrWhiteSpace(savedIp))
//////////                TcpIpAddress.Text = savedIp;

//////////            // ── Save IP whenever user manually types a new one ─────
//////////            TcpIpAddress.TextChanged += (s, args) =>
//////////            {
//////////                string currentIp = TcpIpAddress.Text.Trim();
//////////                if (!string.IsNullOrWhiteSpace(currentIp))
//////////                {
//////////                    Properties.Settings.Default.LastTcpIpAddress = currentIp;
//////////                    Properties.Settings.Default.Save();
//////////                }
//////////            };

//////////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
//////////            _pollTimer.Tick += async (s, args) =>
//////////            {
//////////                await PollActiveNodeAsync();
//////////                UpdateStatusText();
//////////            };
//////////            _pollTimer.Start();
//////////        }

//////////        // ── Called from NetworkService.BtnSet_Click ────────────────
//////////        internal void ApplyAndSaveIp(string ip)
//////////        {
//////////            TcpIpAddress.Text = ip;

//////////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
//////////            TcpScanStatus.Foreground = Brushes.Orange;
//////////        }

//////////        private async Task PollActiveNodeAsync()
//////////        {
//////////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

//////////            try
//////////            {
//////////                _isBusy = true;
//////////                await Task.Run(() => BLLogic.UpdateData());
//////////            }
//////////            catch (Exception ex)
//////////            {
//////////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
//////////            }
//////////            finally
//////////            {
//////////                _isBusy = false;
//////////            }
//////////        }

//////////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            // ── DISCONNECT ──────────────────────────────────────────
//////////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//////////            {
//////////                BLLogic.Close();

//////////                if (Application.Current.MainWindow is MainWindow mw)
//////////                {
//////////                    mw.DataContext = null;
//////////                }

//////////                UpdateStatusText();
//////////                return;
//////////            }

//////////            // ── CONNECT ──────────────────────────────────────────────
//////////            if (!int.TryParse(TcpPort.Text, out int port)) return;

//////////            string ip = TcpIpAddress.Text.Trim();

//////////            Properties.Settings.Default.LastTcpIpAddress = ip;
//////////            Properties.Settings.Default.Save();

//////////            var tcp = new CommEthernet(ip, port);

//////////            if (BLLogic.Start(tcp))
//////////            {
//////////                _tempStatusEndTime = DateTime.MinValue;
//////////                UpdateStatusText();
//////////            }
//////////            else
//////////            {
//////////                _tempStatusMessage = "CONNECTION FAILED";
//////////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
//////////            }
//////////        }

//////////        // internal (not private) so MainWindow's BLLogic.OnDisconnected handler
//////////        // can force a refresh — this keeps TcpConnectButton's Content/color in
//////////        // sync any time the connection drops on its own (not just on click).
//////////        //
//////////        // Also called from NetworkService.AutoReconnectAsync once the app
//////////        // successfully reconnects to a device after an IP change + reboot.
//////////        internal void UpdateStatusText()
//////////        {
//////////            if (BLLogic == null) return;

//////////            if (DateTime.Now < _tempStatusEndTime)
//////////            {
//////////                TcpScanStatus.Foreground = Brushes.Red;
//////////                TcpScanStatus.Text = _tempStatusMessage;

//////////                TcpConnectButton.Content = "CONNECT";
//////////                TcpConnectButton.IsEnabled = true;
//////////                TcpConnectButton.Background = null;
//////////            }
//////////            else if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//////////            {
//////////                TcpScanStatus.Foreground = Brushes.LimeGreen;
//////////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

//////////                TcpConnectButton.Content = "DISCONNECT";
//////////                TcpConnectButton.IsEnabled = true;
//////////                TcpConnectButton.Background =
//////////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
//////////            }
//////////            else
//////////            {
//////////                TcpScanStatus.Foreground = Brushes.Gray;
//////////                TcpScanStatus.Text = "NOT CONNECTED";
//////////                TcpConnectButton.Content = "CONNECT";
//////////                TcpConnectButton.IsEnabled = true;
//////////                TcpConnectButton.Background = null;
//////////            }
//////////        }
//////////    }
//////////}
////////using System;
////////using System.IO;
////////using System.Windows;
////////using System.Windows.Controls;
////////using System.Windows.Media;
////////using System.Windows.Threading;
////////using System.Threading.Tasks;
////////using BLDC_Demo.Models;
////////using BLDC_Demo.Controls;

////////namespace BLDC_Demo
////////{
////////    public partial class CommMotorSelectControl : UserControl
////////    {
////////        public static CommMotorSelectControl Instance { get; private set; }

////////        internal BL BLLogic { get; set; }
////////        private DispatcherTimer _pollTimer;
////////        private bool _isBusy = false;
////////        private DateTime _tempStatusEndTime = DateTime.MinValue;
////////        private string _tempStatusMessage = "";

////////        internal readonly object LockObject = new object();

////////        public CommMotorSelectControl()
////////        {
////////            InitializeComponent();
////////            Instance = this;
////////        }

////////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
////////        {
////////            // ── Load Left/Right motor parameter row definitions from JSON ──
////////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
////////            MotorParamConfig.LoadFromFile(paramFilePath);

////////            // ── Restore last saved IP on every app launch ──────────
////////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
////////            if (!string.IsNullOrWhiteSpace(savedIp))
////////            {
////////                string[] octets = savedIp.Split('.');
////////                if (octets.Length == 4)
////////                {
////////                    TcpIpOctet1.Text = octets[0];
////////                    TcpIpOctet2.Text = octets[1];
////////                    TcpIpOctet3.Text = octets[2];
////////                    TcpIpOctet4.Text = octets[3];
////////                }
////////            }

////////            // ── Save IP whenever user manually types a new one ─────
////////            TextChangedEventHandler saveIpHandler = (s, args) =>
////////            {
////////                string currentIp = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();
////////                if (!string.IsNullOrWhiteSpace(currentIp))
////////                {
////////                    Properties.Settings.Default.LastTcpIpAddress = currentIp;
////////                    Properties.Settings.Default.Save();
////////                }
////////            };

////////            TcpIpOctet1.TextChanged += saveIpHandler;
////////            TcpIpOctet2.TextChanged += saveIpHandler;
////////            TcpIpOctet3.TextChanged += saveIpHandler;
////////            TcpIpOctet4.TextChanged += saveIpHandler;

////////            // Keep the button/status in sync any time the connection state
////////            // changes on its own (auto-reconnect succeeding/failing), not
////////            // just when the user clicks Connect/Disconnect.
////////            BLLogic.OnDisconnected += () =>
////////            {
////////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
////////            };

////////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
////////            _pollTimer.Tick += async (s, args) =>
////////            {
////////                await PollActiveNodeAsync();
////////                UpdateStatusText();
////////            };
////////            _pollTimer.Start();
////////        }

////////        // ── Called from NetworkService.BtnSet_Click ────────────────
////////        //internal void ApplyAndSaveIp(string ip)
////////        //{
////////        //    TcpIpAddress.Text = ip;

////////        //    TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
////////        //    TcpScanStatus.Foreground = Brushes.Orange;
////////        //}
////////        internal void ApplyAndSaveIp(string ip)
////////        {
////////            string[] octets = ip.Split('.');
////////            if (octets.Length == 4)
////////            {
////////                TcpIpOctet1.Text = octets[0];
////////                TcpIpOctet2.Text = octets[1];
////////                TcpIpOctet3.Text = octets[2];
////////                TcpIpOctet4.Text = octets[3];
////////            }

////////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
////////            TcpScanStatus.Foreground = Brushes.Orange;
////////        }
////////        private async Task PollActiveNodeAsync()
////////        {
////////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

////////            try
////////            {
////////                _isBusy = true;
////////                await Task.Run(() => BLLogic.UpdateData());
////////            }
////////            catch (Exception ex)
////////            {
////////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
////////            }
////////            finally
////////            {
////////                _isBusy = false;
////////            }
////////        }

////////        private void BlinkToggleButton_Click(object sender, RoutedEventArgs e)
////////        {
////////            string[] octets = { TcpIpOctet1_BW.Text, TcpIpOctet2_BW.Text, TcpIpOctet3_BW.Text, TcpIpOctet4_BW.Text };

////////            ushort baseRegister = 0x72; // IP octets: 0x72, 0x73, 0x74, 0x75

////////            for (int i = 0; i < 4; i++)
////////            {
////////                if (byte.TryParse(octets[i], out byte value))
////////                {
////////                    BLLogic?.SendWriteCommand((ushort)(baseRegister + i), value);
////////                }
////////            }
////////        }
////////        private bool _isRunning = false;
////////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
////////        {
////////            _isRunning = !_isRunning;

////////            // TODO: confirm the correct register for RIGHT motor run/stop.
////////            // This is a placeholder — the left control uses 0x0030, but that
////////            // was itself unverified. Replace with the real run/stop register
////////            // for the right motor once you have it from the device spec.
////////            BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));
////////            if (_isRunning)
////////            {
////////                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
////////            }
////////            else
////////            {
////////                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
////////            }
////////            //BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));
////////            //BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));

////////            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
////////        }
////////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
////////        {
////////            // ── DISCONNECT ──────────────────────────────────────────
////////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
////////            {
////////                // User-initiated: BLLogic.Close() sets _userInitiatedClose,
////////                // which is what stops auto-reconnect from kicking back in
////////                // after this deliberate disconnect.
////////                BLLogic.Close();

////////                if (Application.Current.MainWindow is MainWindow mw)
////////                {
////////                    mw.DataContext = null;
////////                }

////////                UpdateStatusText();
////////                return;
////////            }

////////            // ── CONNECT ──────────────────────────────────────────────
////////            int port = 502;
////////            //if (!int.TryParse(TcpPort, out int port)) return;

////////            //string ip = TcpIpAddress.Text.Trim();

////////string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

////////            Properties.Settings.Default.LastTcpIpAddress = ip;
////////            Properties.Settings.Default.Save();

////////            // IMPORTANT: use the (ip, port, factory) overload, not Start(IComm).
////////            // Only this overload sets _commFactory in BL, which is what arms
////////            // BL's existing auto-reconnect logic (TryAutoReconnect). The
////////            // plain Start(IComm) overload leaves _commFactory null, so
////////            // HandleConnectionLost just calls Close() with no retry — that
////////            // was why a connection that dropped (timeout, Wi-Fi blip, etc.)
////////            // stayed disconnected forever instead of automatically retrying.
////////            if (BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)))
////////            {
////////                _tempStatusEndTime = DateTime.MinValue;
////////                UpdateStatusText();
////////            }
////////            else
////////            {
////////                _tempStatusMessage = "CONNECTION FAILED";
////////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
////////            }
////////        }

////////        // internal (not private) so MainWindow's BLLogic.OnDisconnected handler
////////        // can force a refresh — this keeps TcpConnectButton's Content/color in
////////        // sync any time the connection drops on its own (not just on click).
////////        //
////////        // Also called from NetworkService.AutoReconnectAsync once the app
////////        // successfully reconnects to a device after an IP change + reboot.
////////        internal void UpdateStatusText()
////////        {
////////            if (BLLogic == null) return;

////////            if (DateTime.Now < _tempStatusEndTime)
////////            {
////////                TcpScanStatus.Foreground = Brushes.Red;
////////                TcpScanStatus.Text = _tempStatusMessage;

////////                TcpConnectButton.Content = "CONNECT";
////////                TcpConnectButton.IsEnabled = true;
////////                TcpConnectButton.Background = null;
////////            }
////////            else if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
////////            {
////////                TcpScanStatus.Foreground = Brushes.LimeGreen;
////////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

////////                TcpConnectButton.Content = "DISCONNECT";
////////                TcpConnectButton.IsEnabled = true;
////////                TcpConnectButton.Background =
////////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
////////            }
////////            else
////////            {
////////                TcpScanStatus.Foreground = Brushes.Gray;
////////                TcpScanStatus.Text = "NOT CONNECTED";
////////                TcpConnectButton.Content = "CONNECT";
////////                TcpConnectButton.IsEnabled = true;
////////                TcpConnectButton.Background = null;
////////            }
////////        }
////////    }
////////}
//////using System;
//////using System.IO;
//////using System.Windows;
//////using System.Windows.Controls;
//////using System.Windows.Media;
//////using System.Windows.Threading;
//////using System.Threading.Tasks;
//////using BLDC_Demo.Models;
//////using BLDC_Demo.Controls;

//////namespace BLDC_Demo
//////{
//////    public partial class CommMotorSelectControl : UserControl
//////    {
//////        public static CommMotorSelectControl Instance { get; private set; }

//////        internal BL BLLogic { get; set; }
//////        private DispatcherTimer _pollTimer;
//////        private bool _isBusy = false;
//////        private DateTime _tempStatusEndTime = DateTime.MinValue;
//////        private string _tempStatusMessage = "";

//////        internal readonly object LockObject = new object();

//////        public CommMotorSelectControl()
//////        {
//////            InitializeComponent();
//////            Instance = this;
//////        }

//////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//////        {
//////            // ── Load Left/Right motor parameter row definitions from JSON ──
//////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
//////            MotorParamConfig.LoadFromFile(paramFilePath);

//////            // ── Restore last saved IP on every app launch ──────────
//////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
//////            if (!string.IsNullOrWhiteSpace(savedIp))
//////            {
//////                string[] octets = savedIp.Split('.');
//////                if (octets.Length == 4)
//////                {
//////                    TcpIpOctet1.Text = octets[0];
//////                    TcpIpOctet2.Text = octets[1];
//////                    TcpIpOctet3.Text = octets[2];
//////                    TcpIpOctet4.Text = octets[3];
//////                }
//////            }

//////            // ── Save IP whenever user manually types a new one ─────
//////            TextChangedEventHandler saveIpHandler = (s, args) =>
//////            {
//////                string currentIp = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();
//////                if (!string.IsNullOrWhiteSpace(currentIp))
//////                {
//////                    Properties.Settings.Default.LastTcpIpAddress = currentIp;
//////                    Properties.Settings.Default.Save();
//////                }
//////            };

//////            TcpIpOctet1.TextChanged += saveIpHandler;
//////            TcpIpOctet2.TextChanged += saveIpHandler;
//////            TcpIpOctet3.TextChanged += saveIpHandler;
//////            TcpIpOctet4.TextChanged += saveIpHandler;

//////            // Keep the button/status in sync any time the connection state
//////            // changes on its own (auto-reconnect succeeding/failing), not
//////            // just when the user clicks Connect/Disconnect.
//////            BLLogic.OnDisconnected += () =>
//////            {
//////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
//////            };

//////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
//////            _pollTimer.Tick += async (s, args) =>
//////            {
//////                await PollActiveNodeAsync();
//////                UpdateStatusText();
//////            };
//////            _pollTimer.Start();
//////        }

//////        // ── Called from NetworkService.BtnSet_Click ────────────────
//////        //internal void ApplyAndSaveIp(string ip)
//////        //{
//////        //    TcpIpAddress.Text = ip;

//////        //    TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
//////        //    TcpScanStatus.Foreground = Brushes.Orange;
//////        //}
//////        internal void ApplyAndSaveIp(string ip)
//////        {
//////            string[] octets = ip.Split('.');
//////            if (octets.Length == 4)
//////            {
//////                TcpIpOctet1.Text = octets[0];
//////                TcpIpOctet2.Text = octets[1];
//////                TcpIpOctet3.Text = octets[2];
//////                TcpIpOctet4.Text = octets[3];
//////            }

//////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
//////            TcpScanStatus.Foreground = Brushes.Orange;
//////        }
//////        private async Task PollActiveNodeAsync()
//////        {
//////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

//////            try
//////            {
//////                _isBusy = true;
//////                await Task.Run(() => BLLogic.UpdateData());
//////            }
//////            catch (Exception ex)
//////            {
//////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
//////            }
//////            finally
//////            {
//////                _isBusy = false;
//////            }
//////        }


//////        private bool _isRunning = false;
//////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)

//////        {
//////            string ip = $"{TcpIpOctet1_BW.Text}.{TcpIpOctet2_BW.Text}.{TcpIpOctet3_BW.Text}.{TcpIpOctet4_BW.Text}".Trim();

//////            // ── Reject bad input before touching settings/BLLogic ───
//////            if (!IsValidIp(ip, out string ipError))
//////            {
//////                MessageBox.Show(
//////                    $"Please enter a valid IP address.\n{ipError}",
//////                    "Invalid IP",
//////                    MessageBoxButton.OK,
//////                    MessageBoxImage.Warning);
//////                return;
//////            }
//////            _isRunning = !_isRunning;


//////            BLLogic?.SendWriteCommand(0x0098, (ushort)(_isRunning ? 1 : 0));
//////            if (_isRunning)
//////            {
//////                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
//////            }
//////            else
//////            {
//////                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
//////            }
//////            //BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));
//////            //BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));

//////            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
//////        }

//////        // ── Validate a dotted IP string built from 4 octet textboxes ──
//////        // Checks: exactly 4 parts, each parses as a byte (0–255), and
//////        // none of the parts are blank.
//////        private bool IsValidIp(string ip, out string errorReason)
//////        {
//////            errorReason = "";

//////            if (string.IsNullOrWhiteSpace(ip))
//////            {
//////                errorReason = "IP address cannot be empty.";
//////                return false;
//////            }

//////            string[] octets = ip.Split('.');
//////            if (octets.Length != 4)
//////            {
//////                errorReason = "IP address must have 4 parts (e.g. 192.168.1.10).";
//////                return false;
//////            }

//////            foreach (string part in octets)
//////            {
//////                if (string.IsNullOrWhiteSpace(part) || !byte.TryParse(part, out _))
//////                {
//////                    errorReason = $"'{part}' is not a valid number (0–255).";
//////                    return false;
//////                }
//////            }

//////            return true;
//////        }

//////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
//////        {
//////            // ── DISCONNECT ──────────────────────────────────────────
//////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//////            {
//////                // User-initiated: BLLogic.Close() sets _userInitiatedClose,
//////                // which is what stops auto-reconnect from kicking back in
//////                // after this deliberate disconnect.
//////                BLLogic.Close();

//////                if (Application.Current.MainWindow is MainWindow mw)
//////                {
//////                    mw.DataContext = null;
//////                }

//////                UpdateStatusText();
//////                return;
//////            }

//////            // ── CONNECT ──────────────────────────────────────────────
//////            int port = 502;
//////            //if (!int.TryParse(TcpPort, out int port)) return;

//////            //string ip = TcpIpAddress.Text.Trim();

//////            string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

//////            // ── Reject bad input before touching settings/BLLogic ───
//////            if (!IsValidIp(ip, out string ipError))
//////            {
//////                MessageBox.Show(
//////                    $"Please enter a valid IP address.\n{ipError}",
//////                    "Invalid IP",
//////                    MessageBoxButton.OK,
//////                    MessageBoxImage.Warning);
//////                return;
//////            }

//////            Properties.Settings.Default.LastTcpIpAddress = ip;
//////            Properties.Settings.Default.Save();

//////            // IMPORTANT: use the (ip, port, factory) overload, not Start(IComm).
//////            // Only this overload sets _commFactory in BL, which is what arms
//////            // BL's existing auto-reconnect logic (TryAutoReconnect). The
//////            // plain Start(IComm) overload leaves _commFactory null, so
//////            // HandleConnectionLost just calls Close() with no retry — that
//////            // was why a connection that dropped (timeout, Wi-Fi blip, etc.)
//////            // stayed disconnected forever instead of automatically retrying.
//////            if (BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)))
//////            {
//////                _tempStatusEndTime = DateTime.MinValue;
//////                TripleStateButton.IsEnabled = true;
//////                UpdateStatusText();
//////            }
//////            else
//////            {
//////                TripleStateButton.IsEnabled = false;
//////                _tempStatusMessage = "CONNECTION FAILED";
//////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
//////            }
//////        }

//////        // internal (not private) so MainWindow's BLLogic.OnDisconnected handler
//////        // can force a refresh — this keeps TcpConnectButton's Content/color in
//////        // sync any time the connection drops on its own (not just on click).
//////        //
//////        // Also called from NetworkService.AutoReconnectAsync once the app
//////        // successfully reconnects to a device after an IP change + reboot.
//////        internal void UpdateStatusText()
//////        {
//////            if (BLLogic == null) return;

//////            if (DateTime.Now < _tempStatusEndTime)
//////            {
//////                TcpScanStatus.Foreground = Brushes.Red;
//////                TcpScanStatus.Text = _tempStatusMessage;

//////                TcpConnectButton.Content = "CONNECT";
//////                TcpConnectButton.IsEnabled = true;
//////                TcpConnectButton.Background = null;
//////            }
//////            else if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//////            {
//////                TcpScanStatus.Foreground = Brushes.LimeGreen;
//////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

//////                TcpConnectButton.Content = "DISCONNECT";
//////                TcpConnectButton.IsEnabled = true;
//////                TcpConnectButton.Background =
//////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
//////            }
//////            else
//////            {
//////                TcpScanStatus.Foreground = Brushes.Gray;
//////                TcpScanStatus.Text = "NOT CONNECTED";
//////                TcpConnectButton.Content = "CONNECT";
//////                TcpConnectButton.IsEnabled = true;
//////                TcpConnectButton.Background = null;
//////            }
//////        }
//////    }
//////}
//////using System;
//////using System.IO;
//////using System.Windows;
//////using System.Windows.Controls;
//////using System.Windows.Media;
//////using System.Windows.Threading;
//////using System.Threading.Tasks;
//////using BLDC_Demo.Models;
//////using BLDC_Demo.Controls;

//////namespace BLDC_Demo
//////{
//////    public partial class CommMotorSelectControl : UserControl
//////    {
//////        public static CommMotorSelectControl Instance { get; private set; }

//////        internal BL BLLogic { get; set; }
//////        private DispatcherTimer _pollTimer;
//////        private bool _isBusy = false;
//////        private DateTime _tempStatusEndTime = DateTime.MinValue;
//////        private string _tempStatusMessage = "";

//////        internal readonly object LockObject = new object();

//////        public CommMotorSelectControl()
//////        {
//////            InitializeComponent();
//////            Instance = this;
//////        }

//////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//////        {
//////            // ── Load Left/Right motor parameter row definitions from JSON ──
//////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
//////            MotorParamConfig.LoadFromFile(paramFilePath);

//////            // ── Restore last saved IP on every app launch ──────────
//////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
//////            if (!string.IsNullOrWhiteSpace(savedIp))
//////            {
//////                string[] octets = savedIp.Split('.');
//////                if (octets.Length == 4)
//////                {
//////                    TcpIpOctet1.Text = octets[0];
//////                    TcpIpOctet2.Text = octets[1];
//////                    TcpIpOctet3.Text = octets[2];
//////                    TcpIpOctet4.Text = octets[3];
//////                }
//////            }

//////            // ── Save IP whenever user manually types a new one ─────
//////            TextChangedEventHandler saveIpHandler = (s, args) =>
//////            {
//////                string currentIp = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();
//////                if (!string.IsNullOrWhiteSpace(currentIp))
//////                {
//////                    Properties.Settings.Default.LastTcpIpAddress = currentIp;
//////                    Properties.Settings.Default.Save();
//////                }
//////            };

//////            TcpIpOctet1.TextChanged += saveIpHandler;
//////            TcpIpOctet2.TextChanged += saveIpHandler;
//////            TcpIpOctet3.TextChanged += saveIpHandler;
//////            TcpIpOctet4.TextChanged += saveIpHandler;

//////            // Keep the button/status in sync any time the connection state
//////            // changes on its own (auto-reconnect succeeding/failing), not
//////            // just when the user clicks Connect/Disconnect.
//////            BLLogic.OnDisconnected += () =>
//////            {
//////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
//////            };

//////            // NEW: fires the instant a drop is detected (before reconnect
//////            // attempts run) AND again on a successful auto-reconnect, so the
//////            // UI never keeps showing "CONNECTED" during the reconnect window.
//////            BLLogic.OnConnectionLost += () =>
//////            {
//////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
//////            };

//////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
//////            _pollTimer.Tick += async (s, args) =>
//////            {
//////                await PollActiveNodeAsync();
//////                UpdateStatusText();
//////            };
//////            _pollTimer.Start();
//////        }

//////        // ── Called from NetworkService.BtnSet_Click ────────────────
//////        internal void ApplyAndSaveIp(string ip)
//////        {
//////            string[] octets = ip.Split('.');
//////            if (octets.Length == 4)
//////            {
//////                TcpIpOctet1.Text = octets[0];
//////                TcpIpOctet2.Text = octets[1];
//////                TcpIpOctet3.Text = octets[2];
//////                TcpIpOctet4.Text = octets[3];
//////            }

//////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
//////            TcpScanStatus.Foreground = Brushes.Orange;
//////        }

//////        private async Task PollActiveNodeAsync()
//////        {
//////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

//////            try
//////            {
//////                _isBusy = true;
//////                await Task.Run(() => BLLogic.UpdateData());
//////            }
//////            catch (Exception ex)
//////            {
//////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
//////            }
//////            finally
//////            {
//////                _isBusy = false;
//////            }
//////        }


//////        private bool _isRunning = false;
//////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)

//////        {
//////            string ip = $"{TcpIpOctet1_BW.Text}.{TcpIpOctet2_BW.Text}.{TcpIpOctet3_BW.Text}.{TcpIpOctet4_BW.Text}".Trim();

//////            // ── Reject bad input before touching settings/BLLogic ───
//////            if (!IsValidIp(ip, out string ipError))
//////            {
//////                MessageBox.Show(
//////                    $"Please enter a valid IP address.\n{ipError}",
//////                    "Invalid IP",
//////                    MessageBoxButton.OK,
//////                    MessageBoxImage.Warning);
//////                return;
//////            }
//////            _isRunning = !_isRunning;


//////            BLLogic?.SendWriteCommand(0x0098, (ushort)(_isRunning ? 1 : 0));
//////            if (_isRunning)
//////            {
//////                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
//////            }
//////            else
//////            {
//////                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
//////            }

//////            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
//////        }

//////        // ── Validate a dotted IP string built from 4 octet textboxes ──
//////        private bool IsValidIp(string ip, out string errorReason)
//////        {
//////            errorReason = "";

//////            if (string.IsNullOrWhiteSpace(ip))
//////            {
//////                errorReason = "IP address cannot be empty.";
//////                return false;
//////            }

//////            string[] octets = ip.Split('.');
//////            if (octets.Length != 4)
//////            {
//////                errorReason = "IP address must have 4 parts (e.g. 192.168.1.10).";
//////                return false;
//////            }

//////            foreach (string part in octets)
//////            {
//////                if (string.IsNullOrWhiteSpace(part) || !byte.TryParse(part, out _))
//////                {
//////                    errorReason = $"'{part}' is not a valid number (0–255).";
//////                    return false;
//////                }
//////            }

//////            return true;
//////        }

//////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
//////        {
//////            // ── DISCONNECT ──────────────────────────────────────────
//////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//////            {
//////                // User-initiated: BLLogic.Close() sets _userInitiatedClose,
//////                // which is what stops auto-reconnect from kicking back in
//////                // after this deliberate disconnect.
//////                BLLogic.Close();

//////                if (Application.Current.MainWindow is MainWindow mw)
//////                {
//////                    mw.DataContext = null;
//////                }

//////                UpdateStatusText();
//////                return;
//////            }

//////            // ── CONNECT ──────────────────────────────────────────────
//////            int port = 502;

//////            string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

//////            // ── Reject bad input before touching settings/BLLogic ───
//////            if (!IsValidIp(ip, out string ipError))
//////            {
//////                MessageBox.Show(
//////                    $"Please enter a valid IP address.\n{ipError}",
//////                    "Invalid IP",
//////                    MessageBoxButton.OK,
//////                    MessageBoxImage.Warning);
//////                return;
//////            }

//////            Properties.Settings.Default.LastTcpIpAddress = ip;
//////            Properties.Settings.Default.Save();

//////            // IMPORTANT: use the (ip, port, factory) overload, not Start(IComm).
//////            // Only this overload sets _commFactory in BL, which is what arms
//////            // BL's existing auto-reconnect logic (TryAutoReconnect).
//////            if (BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)))
//////            {
//////                _tempStatusEndTime = DateTime.MinValue;
//////                TripleStateButton.IsEnabled = true;
//////                UpdateStatusText();
//////            }
//////            else
//////            {
//////                TripleStateButton.IsEnabled = false;
//////                _tempStatusMessage = "CONNECTION FAILED";
//////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
//////            }
//////        }

//////        // internal (not private) so MainWindow's BLLogic.OnDisconnected /
//////        // OnConnectionLost handlers can force a refresh — this keeps
//////        // TcpConnectButton's Content/color in sync any time the connection
//////        // drops or recovers on its own, not just when the user clicks it.
//////        internal void UpdateStatusText()
//////        {
//////            if (BLLogic == null) return;

//////            if (DateTime.Now < _tempStatusEndTime)
//////            {
//////                TcpScanStatus.Foreground = Brushes.Red;
//////                TcpScanStatus.Text = _tempStatusMessage;

//////                TcpConnectButton.Content = "CONNECT";
//////                TcpConnectButton.IsEnabled = true;
//////                TcpConnectButton.Background = null;
//////                TripleStateButton.IsEnabled = false;
//////            }
//////            // CHANGED: was "!string.IsNullOrEmpty(BLLogic.ActivePortName)".
//////            // ActivePortName is only cleared inside BL.Close(), which runs
//////            // AFTER auto-reconnect gives up — so during a drop it stayed
//////            // "CONNECTED" even though the socket was already dead.
//////            // IsConnected reflects the live socket state immediately.
//////            else if (BLLogic.IsConnected)
//////            {
//////                TcpScanStatus.Foreground = Brushes.LimeGreen;
//////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

//////                TcpConnectButton.Content = "DISCONNECT";
//////                TcpConnectButton.IsEnabled = true;
//////                TcpConnectButton.Background =
//////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
//////            }
//////            else
//////            {
//////                TcpScanStatus.Foreground = Brushes.Gray;
//////              //  TcpScanStatus.Text = "NOT CONNECTED";
//////                TcpConnectButton.Content = "CONNECT";
//////                TcpConnectButton.IsEnabled = true;
//////                TcpConnectButton.Background = null;
//////                TripleStateButton.IsEnabled = false;
//////            }
//////        }
//////    }
//////}
////using System;
////using System.IO;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Media;
////using System.Windows.Threading;
////using System.Threading.Tasks;
////using BLDC_Demo.Models;
////using BLDC_Demo.Controls;

////namespace BLDC_Demo
////{
////    public partial class CommMotorSelectControl : UserControl
////    {
////        public static CommMotorSelectControl Instance { get; private set; }

////        internal BL BLLogic { get; set; }

////        // IP of the device we are currently connected to (e.g. "192.168.1.10").
////        // Read by ZPASettingControl to build the full Master IP / Last IP display.
////        internal string ConnectedIp { get; private set; }

////        private DispatcherTimer _pollTimer;
////        private bool _isBusy = false;
////        private DateTime _tempStatusEndTime = DateTime.MinValue;
////        private string _tempStatusMessage = "";

////        internal readonly object LockObject = new object();

////        public CommMotorSelectControl()
////        {
////            InitializeComponent();
////            Instance = this;
////        }

////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
////        {
////            // ── Load Left/Right motor parameter row definitions from JSON ──
////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
////            MotorParamConfig.LoadFromFile(paramFilePath);

////            // ── Restore last saved IP on every app launch ──────────
////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
////            if (!string.IsNullOrWhiteSpace(savedIp))
////            {
////                string[] octets = savedIp.Split('.');
////                if (octets.Length == 4)
////                {
////                    TcpIpOctet1.Text = octets[0];
////                    TcpIpOctet2.Text = octets[1];
////                    TcpIpOctet3.Text = octets[2];
////                    TcpIpOctet4.Text = octets[3];
////                }
////            }

////            // ── Save IP whenever user manually types a new one ─────
////            TextChangedEventHandler saveIpHandler = (s, args) =>
////            {
////                string currentIp = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();
////                if (!string.IsNullOrWhiteSpace(currentIp))
////                {
////                    Properties.Settings.Default.LastTcpIpAddress = currentIp;
////                    Properties.Settings.Default.Save();
////                }
////            };

////            TcpIpOctet1.TextChanged += saveIpHandler;
////            TcpIpOctet2.TextChanged += saveIpHandler;
////            TcpIpOctet3.TextChanged += saveIpHandler;
////            TcpIpOctet4.TextChanged += saveIpHandler;

////            // Keep the button/status in sync any time the connection state
////            // changes on its own (auto-reconnect succeeding/failing), not
////            // just when the user clicks Connect/Disconnect.
////            BLLogic.OnDisconnected += () =>
////            {
////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
////            };

////            // NEW: fires the instant a drop is detected (before reconnect
////            // attempts run) AND again on a successful auto-reconnect, so the
////            // UI never keeps showing "CONNECTED" during the reconnect window.
////            BLLogic.OnConnectionLost += () =>
////            {
////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
////            };

////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
////            _pollTimer.Tick += async (s, args) =>
////            {
////                await PollActiveNodeAsync();
////                UpdateStatusText();
////            };
////            _pollTimer.Start();
////        }

////        // ── Called from NetworkService.BtnSet_Click ────────────────
////        internal void ApplyAndSaveIp(string ip)
////        {
////            string[] octets = ip.Split('.');
////            if (octets.Length == 4)
////            {
////                TcpIpOctet1.Text = octets[0];
////                TcpIpOctet2.Text = octets[1];
////                TcpIpOctet3.Text = octets[2];
////                TcpIpOctet4.Text = octets[3];
////            }

////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
////            TcpScanStatus.Foreground = Brushes.Orange;
////        }

////        private async Task PollActiveNodeAsync()
////        {
////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

////            try
////            {
////                _isBusy = true;
////                await Task.Run(() => BLLogic.UpdateData());
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
////            }
////            finally
////            {
////                _isBusy = false;
////            }
////        }


////        private bool _isRunning = false;
////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)

////        {
////            string ip = $"{TcpIpOctet1_BW.Text}.{TcpIpOctet2_BW.Text}.{TcpIpOctet3_BW.Text}.{TcpIpOctet4_BW.Text}".Trim();

////            // ── Reject bad input before touching settings/BLLogic ───
////            if (!IsValidIp(ip, out string ipError))
////            {
////                MessageBox.Show(
////                    $"Please enter a valid IP address.\n{ipError}",
////                    "Invalid IP",
////                    MessageBoxButton.OK,
////                    MessageBoxImage.Warning);
////                return;
////            }
////            _isRunning = !_isRunning;


////            BLLogic?.SendWriteCommand(0x0098, (ushort)(_isRunning ? 1 : 0));
////            if (_isRunning)
////            {
////                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
////            }
////            else
////            {
////                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
////            }

////            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
////        }

////        // ── Validate a dotted IP string built from 4 octet textboxes ──
////        private bool IsValidIp(string ip, out string errorReason)
////        {
////            errorReason = "";

////            if (string.IsNullOrWhiteSpace(ip))
////            {
////                errorReason = "IP address cannot be empty.";
////                return false;
////            }

////            string[] octets = ip.Split('.');
////            if (octets.Length != 4)
////            {
////                errorReason = "IP address must have 4 parts (e.g. 192.168.1.10).";
////                return false;
////            }

////            foreach (string part in octets)
////            {
////                if (string.IsNullOrWhiteSpace(part) || !byte.TryParse(part, out _))
////                {
////                    errorReason = $"'{part}' is not a valid number (0–255).";
////                    return false;
////                }
////            }

////            return true;
////        }

////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
////        {
////            // ── DISCONNECT ──────────────────────────────────────────
////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
////            {
////                // User-initiated: BLLogic.Close() sets _userInitiatedClose,
////                // which is what stops auto-reconnect from kicking back in
////                // after this deliberate disconnect.
////                BLLogic.Close();
////                ConnectedIp = null;

////                if (Application.Current.MainWindow is MainWindow mw)
////                {
////                    mw.DataContext = null;
////                }

////                UpdateStatusText();
////                return;
////            }

////            // ── CONNECT ──────────────────────────────────────────────
////            int port = 502;

////            string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

////            // ── Reject bad input before touching settings/BLLogic ───
////            if (!IsValidIp(ip, out string ipError))
////            {
////                MessageBox.Show(
////                    $"Please enter a valid IP address.\n{ipError}",
////                    "Invalid IP",
////                    MessageBoxButton.OK,
////                    MessageBoxImage.Warning);
////                return;
////            }

////            Properties.Settings.Default.LastTcpIpAddress = ip;
////            Properties.Settings.Default.Save();

////            // IMPORTANT: use the (ip, port, factory) overload, not Start(IComm).
////            // Only this overload sets _commFactory in BL, which is what arms
////            // BL's existing auto-reconnect logic (TryAutoReconnect).
////            if (BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)))
////            {
////                ConnectedIp = ip;   // remember the connected IP for ZPA Master/Last IP display
////                _tempStatusEndTime = DateTime.MinValue;
////                TripleStateButton.IsEnabled = true;
////                UpdateStatusText();
////            }
////            else
////            {
////                ConnectedIp = null;
////                TripleStateButton.IsEnabled = false;
////                _tempStatusMessage = "CONNECTION FAILED";
////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
////            }
////        }

////        // internal (not private) so MainWindow's BLLogic.OnDisconnected /
////        // OnConnectionLost handlers can force a refresh — this keeps
////        // TcpConnectButton's Content/color in sync any time the connection
////        // drops or recovers on its own, not just when the user clicks it.
////        internal void UpdateStatusText()
////        {
////            if (BLLogic == null) return;

////            if (DateTime.Now < _tempStatusEndTime)
////            {
////                TcpScanStatus.Foreground = Brushes.Red;
////                //TcpScanStatus.Text = _tempStatusMessage;

////                TcpConnectButton.Content = "CONNECT";
////                TcpConnectButton.IsEnabled = true;
////                TcpConnectButton.Background = null;
////                TripleStateButton.IsEnabled = false;
////            }
////            // CHANGED: was "!string.IsNullOrEmpty(BLLogic.ActivePortName)".
////            // ActivePortName is only cleared inside BL.Close(), which runs
////            // AFTER auto-reconnect gives up — so during a drop it stayed
////            // "CONNECTED" even though the socket was already dead.
////            // IsConnected reflects the live socket state immediately.
////            else if (BLLogic.IsConnected)
////            {
////                TcpScanStatus.Foreground = Brushes.LimeGreen;
////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

////                TcpConnectButton.Content = "DISCONNECT";
////                TcpConnectButton.IsEnabled = true;
////                TcpConnectButton.Background =
////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
////            }
////            else
////            {
////                TcpScanStatus.Foreground = Brushes.Gray;
////                //  TcpScanStatus.Text = "NOT CONNECTED";
////                TcpConnectButton.Content = "CONNECT";
////                TcpConnectButton.IsEnabled = true;
////                TcpConnectButton.Background = null;
////                TripleStateButton.IsEnabled = false;
////            }
////        }
////    }
////}
////using System;
////using System.IO;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Media;
////using System.Windows.Threading;
////using System.Threading.Tasks;
////using BLDC_Demo.Models;
////using BLDC_Demo.Controls;

////namespace BLDC_Demo
////{
////    public partial class CommMotorSelectControl : UserControl
////    {
////        public static CommMotorSelectControl Instance { get; private set; }

////        internal BL BLLogic { get; set; }

////        // IP of the device we are currently connected to (e.g. "192.168.1.10").
////        // Read by ZPASettingControl to build the full Master IP / Last IP display.
////        // Only set after a SUCCESSFUL connection; null otherwise.
////        internal string ConnectedIp { get; private set; }

////        private DispatcherTimer _pollTimer;
////        private bool _isBusy = false;
////        private DateTime _tempStatusEndTime = DateTime.MinValue;
////        private string _tempStatusMessage = "";

////        internal readonly object LockObject = new object();

////        public CommMotorSelectControl()
////        {
////            InitializeComponent();
////            Instance = this;
////        }

////        private void UserControl_Loaded(object sender, RoutedEventArgs e)
////        {
////            // ── Load Left/Right motor parameter row definitions from JSON ──
////            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
////            MotorParamConfig.LoadFromFile(paramFilePath);

////            // ── Restore the last SUCCESSFULLY connected IP on app launch ──
////            // (an IP is only saved after a successful connection - see TcpConnectButton_Click)
////            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
////            if (!string.IsNullOrWhiteSpace(savedIp))
////            {
////                string[] octets = savedIp.Split('.');
////                if (octets.Length == 4)
////                {
////                    TcpIpOctet1.Text = octets[0];
////                    TcpIpOctet2.Text = octets[1];
////                    TcpIpOctet3.Text = octets[2];
////                    TcpIpOctet4.Text = octets[3];
////                }
////            }

////            // Keep the button/status in sync any time the connection state
////            // changes on its own (auto-reconnect succeeding/failing), not
////            // just when the user clicks Connect/Disconnect.
////            BLLogic.OnDisconnected += () =>
////            {
////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
////            };

////            // NEW: fires the instant a drop is detected (before reconnect
////            // attempts run) AND again on a successful auto-reconnect, so the
////            // UI never keeps showing "CONNECTED" during the reconnect window.
////            BLLogic.OnConnectionLost += () =>
////            {
////                Application.Current.Dispatcher.Invoke(UpdateStatusText);
////            };

////            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
////            _pollTimer.Tick += async (s, args) =>
////            {
////                await PollActiveNodeAsync();
////                UpdateStatusText();
////            };
////            _pollTimer.Start();
////        }

////        // ── Called from NetworkService.BtnSet_Click ────────────────
////        internal void ApplyAndSaveIp(string ip)
////        {
////            string[] octets = ip.Split('.');
////            if (octets.Length == 4)
////            {
////                TcpIpOctet1.Text = octets[0];
////                TcpIpOctet2.Text = octets[1];
////                TcpIpOctet3.Text = octets[2];
////                TcpIpOctet4.Text = octets[3];
////            }

////            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
////            TcpScanStatus.Foreground = Brushes.Orange;
////        }

////        private async Task PollActiveNodeAsync()
////        {
////            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

////            try
////            {
////                _isBusy = true;
////                await Task.Run(() => BLLogic.UpdateData());
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
////            }
////            finally
////            {
////                _isBusy = false;
////            }
////        }


////        private bool _isRunning = false;
////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)

////        {
////            string ip = $"{TcpIpOctet1_BW.Text}.{TcpIpOctet2_BW.Text}.{TcpIpOctet3_BW.Text}.{TcpIpOctet4_BW.Text}".Trim();

////            // ── Reject bad input before touching settings/BLLogic ───
////            if (!IsValidIp(ip, out string ipError))
////            {
////                MessageBox.Show(
////                    $"Please enter a valid IP address.\n{ipError}",
////                    "Invalid IP",
////                    MessageBoxButton.OK,
////                    MessageBoxImage.Warning);
////                return;
////            }
////            _isRunning = !_isRunning;


////            BLLogic?.SendWriteCommand(0x0098, (ushort)(_isRunning ? 1 : 0));
////            if (_isRunning)
////            {
////                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
////            }
////            else
////            {
////                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
////            }

////            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
////        }

////        // ── Validate a dotted IP string built from 4 octet textboxes ──
////        private bool IsValidIp(string ip, out string errorReason)
////        {
////            errorReason = "";

////            if (string.IsNullOrWhiteSpace(ip))
////            {
////                errorReason = "IP address cannot be empty.";
////                return false;
////            }

////            string[] octets = ip.Split('.');
////            if (octets.Length != 4)
////            {
////                errorReason = "IP address must have 4 parts (e.g. 192.168.1.10).";
////                return false;
////            }

////            foreach (string part in octets)
////            {
////                if (string.IsNullOrWhiteSpace(part) || !byte.TryParse(part, out _))
////                {
////                    errorReason = $"'{part}' is not a valid number (0–255).";
////                    return false;
////                }
////            }

////            return true;
////        }

////        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
////        {
////            // ── DISCONNECT ──────────────────────────────────────────
////            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
////            {
////                // User-initiated: BLLogic.Close() sets _userInitiatedClose,
////                // which is what stops auto-reconnect from kicking back in
////                // after this deliberate disconnect.
////                BLLogic.Close();
////                ConnectedIp = null;

////                if (Application.Current.MainWindow is MainWindow mw)
////                {
////                    mw.DataContext = null;
////                }

////                UpdateStatusText();
////                return;
////            }

////            // ── CONNECT ──────────────────────────────────────────────
////            int port = 502;

////            string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

////            // ── Reject bad input before touching BLLogic ───
////            if (!IsValidIp(ip, out string ipError))
////            {
////                MessageBox.Show(
////                    $"Please enter a valid IP address.\n{ipError}",
////                    "Invalid IP",
////                    MessageBoxButton.OK,
////                    MessageBoxImage.Warning);
////                return;
////            }

////            // NOTE: the IP is NOT saved here any more. It is saved only after
////            // the connection succeeds, so a failed attempt never overwrites
////            // the last good value.

////            // IMPORTANT: use the (ip, port, factory) overload, not Start(IComm).
////            // Only this overload sets _commFactory in BL, which is what arms
////            // BL's existing auto-reconnect logic (TryAutoReconnect).
////            if (BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)))
////            {
////                // ── Connection succeeded: store it now ──
////                ConnectedIp = ip;   // remember the connected IP for ZPA Master/Last IP display
////                Properties.Settings.Default.LastTcpIpAddress = ip;
////                Properties.Settings.Default.Save();

////                _tempStatusEndTime = DateTime.MinValue;
////                TripleStateButton.IsEnabled = true;
////                UpdateStatusText();
////            }
////            else
////            {
////                // ── Connection failed: store nothing, keep nothing ──
////                ConnectedIp = null;
////                TripleStateButton.IsEnabled = false;
////                _tempStatusMessage = "CONNECTION FAILED";
////                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
////            }
////        }

////        // internal (not private) so MainWindow's BLLogic.OnDisconnected /
////        // OnConnectionLost handlers can force a refresh — this keeps
////        // TcpConnectButton's Content/color in sync any time the connection
////        // drops or recovers on its own, not just when the user clicks it.
////        internal void UpdateStatusText()
////        {
////            if (BLLogic == null) return;

////            if (DateTime.Now < _tempStatusEndTime)
////            {
////                TcpScanStatus.Foreground = Brushes.Red;
////                TcpScanStatus.Text = _tempStatusMessage;

////                TcpConnectButton.Content = "CONNECT";
////                TcpConnectButton.IsEnabled = true;
////                TcpConnectButton.Background = null;
////                TripleStateButton.IsEnabled = false;
////            }
////            // CHANGED: was "!string.IsNullOrEmpty(BLLogic.ActivePortName)".
////            // ActivePortName is only cleared inside BL.Close(), which runs
////            // AFTER auto-reconnect gives up — so during a drop it stayed
////            // "CONNECTED" even though the socket was already dead.
////            // IsConnected reflects the live socket state immediately.
////            else if (BLLogic.IsConnected)
////            {
////                TcpScanStatus.Foreground = Brushes.LimeGreen;
////                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

////                TcpConnectButton.Content = "DISCONNECT";
////                TcpConnectButton.IsEnabled = true;
////                TcpConnectButton.Background =
////                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
////            }
////            else
////            {
////                TcpScanStatus.Foreground = Brushes.Gray;
////                TcpScanStatus.Text = "";
////                TcpConnectButton.Content = "CONNECT";
////                TcpConnectButton.IsEnabled = true;
////                TcpConnectButton.Background = null;
////                TripleStateButton.IsEnabled = false;
////            }
////        }
////    }
////}
//using System;
//using System.IO;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Media;
//using System.Windows.Threading;
//using System.Threading.Tasks;
//using BLDC_Demo.Models;
//using BLDC_Demo.Controls;

//namespace BLDC_Demo
//{
//    public partial class CommMotorSelectControl : UserControl
//    {
//        public static CommMotorSelectControl Instance { get; private set; }

//        internal BL BLLogic { get; set; }

//        // IP of the device we are currently connected to (e.g. "192.168.1.10").
//        // Read by ZPASettingControl to build the full Master IP / Last IP display.
//        // Only set after a SUCCESSFUL connection; null otherwise.
//        internal string ConnectedIp { get; private set; }

//        private DispatcherTimer _pollTimer;
//        private bool _isBusy = false;
//        private DateTime _tempStatusEndTime = DateTime.MinValue;
//        private string _tempStatusMessage = "";

//        internal readonly object LockObject = new object();

//        public CommMotorSelectControl()
//        {
//            InitializeComponent();
//            Instance = this;
//        }

//        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//        {
//            // ── Load Left/Right motor parameter row definitions from JSON ──
//            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
//            MotorParamConfig.LoadFromFile(paramFilePath);

//            // ── Restore the last SUCCESSFULLY connected IP on app launch ──
//            // (an IP is only saved after a successful connection - see TcpConnectButton_Click)
//            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
//            if (!string.IsNullOrWhiteSpace(savedIp))
//            {
//                string[] octets = savedIp.Split('.');
//                if (octets.Length == 4)
//                {
//                    TcpIpOctet1.Text = octets[0];
//                    TcpIpOctet2.Text = octets[1];
//                    TcpIpOctet3.Text = octets[2];
//                    TcpIpOctet4.Text = octets[3];
//                }
//            }

//            // Keep the button/status in sync any time the connection state
//            // changes on its own (auto-reconnect succeeding/failing), not
//            // just when the user clicks Connect/Disconnect.
//            BLLogic.OnDisconnected += () =>
//            {
//                Application.Current.Dispatcher.Invoke(UpdateStatusText);
//            };

//            // NEW: fires the instant a drop is detected (before reconnect
//            // attempts run) AND again on a successful auto-reconnect, so the
//            // UI never keeps showing "CONNECTED" during the reconnect window.
//            BLLogic.OnConnectionLost += () =>
//            {
//                Application.Current.Dispatcher.Invoke(UpdateStatusText);
//            };

//            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
//            _pollTimer.Tick += async (s, args) =>
//            {
//                await PollActiveNodeAsync();
//                UpdateStatusText();
//            };
//            _pollTimer.Start();
//        }

//        // ── Called from NetworkService.BtnSet_Click ────────────────
//        internal void ApplyAndSaveIp(string ip)
//        {
//            string[] octets = ip.Split('.');
//            if (octets.Length == 4)
//            {
//                TcpIpOctet1.Text = octets[0];
//                TcpIpOctet2.Text = octets[1];
//                TcpIpOctet3.Text = octets[2];
//                TcpIpOctet4.Text = octets[3];
//            }

//            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
//            TcpScanStatus.Foreground = Brushes.Orange;
//        }

//        private async Task PollActiveNodeAsync()
//        {
//            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

//            try
//            {
//                _isBusy = true;
//                await Task.Run(() => BLLogic.UpdateData());
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
//            }
//            finally
//            {
//                _isBusy = false;
//            }
//        }


//        private bool _isRunning = false;
//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)

//        {
//            string ip = $"{TcpIpOctet1_BW.Text}.{TcpIpOctet2_BW.Text}.{TcpIpOctet3_BW.Text}.{TcpIpOctet4_BW.Text}".Trim();

//            // ── Reject bad input before touching settings/BLLogic ───
//            if (!IsValidIp(ip, out string ipError))
//            {
//                MessageBox.Show(
//                    $"Please enter a valid IP address.\n{ipError}",
//                    "Invalid IP",
//                    MessageBoxButton.OK,
//                    MessageBoxImage.Warning);
//                return;
//            }
//            _isRunning = !_isRunning;


//            BLLogic?.SendWriteCommand(0x0098, (ushort)(_isRunning ? 1 : 0));
//            if (_isRunning)
//            {
//                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
//            }

//            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
//        }

//        // ── Validate a dotted IP string built from 4 octet textboxes ──
//        private bool IsValidIp(string ip, out string errorReason)
//        {
//            errorReason = "";

//            if (string.IsNullOrWhiteSpace(ip))
//            {
//                errorReason = "IP address cannot be empty.";
//                return false;
//            }

//            string[] octets = ip.Split('.');
//            if (octets.Length != 4)
//            {
//                errorReason = "IP address must have 4 parts (e.g. 192.168.1.10).";
//                return false;
//            }

//            foreach (string part in octets)
//            {
//                if (string.IsNullOrWhiteSpace(part) || !byte.TryParse(part, out _))
//                {
//                    errorReason = $"'{part}' is not a valid number (0–255).";
//                    return false;
//                }
//            }

//            return true;
//        }

//        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
//        {
//            // ── DISCONNECT ──────────────────────────────────────────
//            // CHANGED: was "!string.IsNullOrEmpty(BLLogic.ActivePortName)".
//            // ActivePortName is only cleared inside BL.Close(), so after the
//            // device dropped the link (e.g. right after selecting GAP Train) it
//            // was still set - and pressing CONNECT was treated as a DISCONNECT
//            // of the dead old session instead of a new connect.
//            // Only a live socket counts as "connected" here.
//            if (BLLogic.IsConnected)
//            {
//                // User-initiated: BLLogic.Close() sets _userInitiatedClose,
//                // which is what stops auto-reconnect from kicking back in
//                // after this deliberate disconnect.
//                BLLogic.Close();
//                ConnectedIp = null;

//                if (Application.Current.MainWindow is MainWindow mw)
//                {
//                    mw.DataContext = null;
//                }

//                UpdateStatusText();
//                return;
//            }

//            // ── STALE SESSION ────────────────────────────────────────
//            // The link is down but BL still holds the old session (ActivePortName
//            // not cleared yet / auto-reconnect still pending). Clear it first so
//            // the new connection starts from a clean state.
//            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//            {
//                BLLogic.Close();
//                ConnectedIp = null;
//            }

//            // ── CONNECT ──────────────────────────────────────────────
//            int port = 502;

//            string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

//            // ── Reject bad input before touching BLLogic ───
//            if (!IsValidIp(ip, out string ipError))
//            {
//                MessageBox.Show(
//                    $"Please enter a valid IP address.\n{ipError}",
//                    "Invalid IP",
//                    MessageBoxButton.OK,
//                    MessageBoxImage.Warning);
//                return;
//            }

//            // NOTE: the IP is NOT saved here any more. It is saved only after
//            // the connection succeeds, so a failed attempt never overwrites
//            // the last good value.

//            // IMPORTANT: use the (ip, port, factory) overload, not Start(IComm).
//            // Only this overload sets _commFactory in BL, which is what arms
//            // BL's existing auto-reconnect logic (TryAutoReconnect).
//            if (BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)))
//            {
//                // ── Connection succeeded: store it now ──
//                ConnectedIp = ip;   // remember the connected IP for ZPA Master/Last IP display
//                Properties.Settings.Default.LastTcpIpAddress = ip;
//                Properties.Settings.Default.Save();

//                _tempStatusEndTime = DateTime.MinValue;
//                TripleStateButton.IsEnabled = true;
//                UpdateStatusText();
//            }
//            else
//            {
//                // ── Connection failed: store nothing, keep nothing ──
//                ConnectedIp = null;
//                TripleStateButton.IsEnabled = false;
//                _tempStatusMessage = "CONNECTION FAILED";
//                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
//            }
//        }

//        // internal (not private) so MainWindow's BLLogic.OnDisconnected /
//        // OnConnectionLost handlers can force a refresh — this keeps
//        // TcpConnectButton's Content/color in sync any time the connection
//        // drops or recovers on its own, not just when the user clicks it.
//        internal void UpdateStatusText()
//        {
//            if (BLLogic == null) return;

//            if (DateTime.Now < _tempStatusEndTime)
//            {
//                TcpScanStatus.Foreground = Brushes.Red;
//                TcpScanStatus.Text = _tempStatusMessage;

//                TcpConnectButton.Content = "CONNECT";
//                TcpConnectButton.IsEnabled = true;
//                TcpConnectButton.Background = null;
//                TripleStateButton.IsEnabled = false;
//            }
//            // CHANGED: was "!string.IsNullOrEmpty(BLLogic.ActivePortName)".
//            // ActivePortName is only cleared inside BL.Close(), which runs
//            // AFTER auto-reconnect gives up — so during a drop it stayed
//            // "CONNECTED" even though the socket was already dead.
//            // IsConnected reflects the live socket state immediately.
//            else if (BLLogic.IsConnected)
//            {
//                TcpScanStatus.Foreground = Brushes.LimeGreen;
//                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

//                TcpConnectButton.Content = "DISCONNECT";
//                TcpConnectButton.IsEnabled = true;
//                TcpConnectButton.Background =
//                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
//            }
//            else
//            {
//                TcpScanStatus.Foreground = Brushes.Gray;
//                TcpScanStatus.Text = "";
//                TcpConnectButton.Content = "CONNECT";
//                TcpConnectButton.IsEnabled = true;
//                TcpConnectButton.Background = null;
//                TripleStateButton.IsEnabled = false;
//            }
//        }
//    }
//}
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using BLDC_Demo.Models;
using BLDC_Demo.Controls;

namespace BLDC_Demo
{
    public partial class CommMotorSelectControl : UserControl
    {
        public static CommMotorSelectControl Instance { get; private set; }

        internal BL BLLogic { get; set; }

        // IP of the device we are currently connected to (e.g. "192.168.1.10").
        // Read by ZPASettingControl to build the full Master IP / Last IP display.
        // Only set after a SUCCESSFUL connection; null otherwise.
        internal string ConnectedIp { get; private set; }

        private DispatcherTimer _pollTimer;
        private bool _isBusy = false;
        private DateTime _tempStatusEndTime = DateTime.MinValue;
        private string _tempStatusMessage = "";

        // NEW: true while a connect attempt is running on a background thread.
        // Stops the 200 ms poll timer from overwriting the "CONNECTING..." text
        // and prevents a second click from starting a second connect.
        private bool _isConnecting = false;

        internal readonly object LockObject = new object();
        // Satisfies the LockableSetButton style binding. The button's IsEnabled is
        // still set directly in code, which takes priority over this binding.
        public bool SetButtonsEnabled => true;
        public CommMotorSelectControl()
        {
            InitializeComponent();
            Instance = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // ── Load Left/Right motor parameter row definitions from JSON ──
            string paramFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motorparameters.json");
            MotorParamConfig.LoadFromFile(paramFilePath);

            // ── Restore the last SUCCESSFULLY connected IP on app launch ──
            string savedIp = Properties.Settings.Default.LastTcpIpAddress;
            if (!string.IsNullOrWhiteSpace(savedIp))
            {
                string[] octets = savedIp.Split('.');
                if (octets.Length == 4)
                {
                    TcpIpOctet1.Text = octets[0];
                    TcpIpOctet2.Text = octets[1];
                    TcpIpOctet3.Text = octets[2];
                    TcpIpOctet4.Text = octets[3];
                }
            }

            // Keep the button/status in sync any time the connection state
            // changes on its own (auto-reconnect succeeding/failing).
            BLLogic.OnDisconnected += () =>
            {
                Application.Current.Dispatcher.Invoke(UpdateStatusText);
            };

            // Fires the instant a drop is detected AND again on a successful
            // auto-reconnect, so the UI never keeps showing "CONNECTED".
            BLLogic.OnConnectionLost += () =>
            {
                Application.Current.Dispatcher.Invoke(UpdateStatusText);
            };

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _pollTimer.Tick += async (s, args) =>
            {
                await PollActiveNodeAsync();
                UpdateStatusText();
            };
            _pollTimer.Start();
        }

        // ── Called from NetworkService.BtnSet_Click ────────────────
        internal void ApplyAndSaveIp(string ip)
        {
            string[] octets = ip.Split('.');
            if (octets.Length == 4)
            {
                TcpIpOctet1.Text = octets[0];
                TcpIpOctet2.Text = octets[1];
                TcpIpOctet3.Text = octets[2];
                TcpIpOctet4.Text = octets[3];
            }

            TcpScanStatus.Text = $"IP set to {ip} — press Connect.";
            TcpScanStatus.Foreground = Brushes.Orange;
        }

        // ── NEW ─────────────────────────────────────────────────────
        // Call this from your Mode ComboBox SelectionChanged handler
        // (e.g. right after sending the GAP Train mode write).
        // It flushes the queued write, then drops the link immediately
        // and on purpose, so:
        //   * the UI shows DISCONNECTED at once (no waiting for a timeout)
        //   * auto-reconnect is cancelled (no pointless retries to the old
        //     device state that would later fight your new CONNECT click)
        internal void DropConnectionForModeChange()
        {
            if (BLLogic == null) return;

            BLLogic.Close(drainPendingWrites: true);
            ConnectedIp = null;

            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.DataContext = null;
            }

            _tempStatusEndTime = DateTime.MinValue;
            UpdateStatusText();
        }

        private async Task PollActiveNodeAsync()
        {
            if (BLLogic == null || _isBusy || !BLLogic.IsConnected) return;

            try
            {
                _isBusy = true;
                await Task.Run(() => BLLogic.UpdateData());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Poll Error: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private bool _isRunning = false;
        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = $"{TcpIpOctet1_BW.Text}.{TcpIpOctet2_BW.Text}.{TcpIpOctet3_BW.Text}.{TcpIpOctet4_BW.Text}".Trim();

            // ── Reject bad input before touching settings/BLLogic ───
            if (!IsValidIp(ip, out string ipError))
            {
                MessageBox.Show(
                    $"Please enter a valid IP address.\n{ipError}",
                    "Invalid IP",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            _isRunning = !_isRunning;

            BLLogic?.SendWriteCommand(0x0098, (ushort)(_isRunning ? 1 : 0));
            if (_isRunning)
            {
                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
            }

            TripleStateButton.Content = _isRunning ? "OFF " : "ON  ";
        }

        // ── Validate a dotted IP string built from 4 octet textboxes ──
        private bool IsValidIp(string ip, out string errorReason)
        {
            errorReason = "";

            if (string.IsNullOrWhiteSpace(ip))
            {
                errorReason = "IP address cannot be empty.";
                return false;
            }

            string[] octets = ip.Split('.');
            if (octets.Length != 4)
            {
                errorReason = "IP address must have 4 parts (e.g. 192.168.1.10).";
                return false;
            }

            foreach (string part in octets)
            {
                if (string.IsNullOrWhiteSpace(part) || !byte.TryParse(part, out _))
                {
                    errorReason = $"'{part}' is not a valid number (0–255).";
                    return false;
                }
            }

            return true;
        }

        // CHANGED: now async so the UI does not freeze while connecting.
        private async void TcpConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Ignore extra clicks while a connect attempt is already running.
            if (_isConnecting) return;

            // ── DISCONNECT ──────────────────────────────────────────
            // Only a live socket counts as "connected" here.
            if (BLLogic.IsConnected)
            {
                // User-initiated: Close() bumps the generation counter,
                // which cancels any pending auto-reconnect.
                BLLogic.Close();
                ConnectedIp = null;

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.DataContext = null;
                }

                UpdateStatusText();
                return;
            }

            // ── STALE SESSION ────────────────────────────────────────
            // Link is down but BL still holds the old session (or an
            // auto-reconnect is still pending). Clear it so the new
            // connection starts clean and the old reconnect thread dies.
            if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
            {
                BLLogic.Close();
                ConnectedIp = null;
            }

            // ── CONNECT ──────────────────────────────────────────────
            int port = 502;

            string ip = $"{TcpIpOctet1.Text}.{TcpIpOctet2.Text}.{TcpIpOctet3.Text}.{TcpIpOctet4.Text}".Trim();

            // ── Reject bad input before touching BLLogic ───
            if (!IsValidIp(ip, out string ipError))
            {
                MessageBox.Show(
                    $"Please enter a valid IP address.\n{ipError}",
                    "Invalid IP",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // ── Show progress and run the blocking connect off the UI thread ──
            _isConnecting = true;
            _tempStatusEndTime = DateTime.MinValue;
            TcpConnectButton.IsEnabled = false;
            TripleStateButton.IsEnabled = false;
            TcpScanStatus.Foreground = Brushes.Orange;
            TcpScanStatus.Text = "CONNECTING...";

            bool ok;
            try
            {
                // IMPORTANT: use the (ip, port, factory) overload so BL's
                // auto-reconnect logic is armed. StartInternal marshals UI
                // work with Dispatcher.Invoke, so a background thread is fine.
                ok = await Task.Run(() =>
                    BLLogic.Start(ip, port, (targetIp, targetPort) => new CommEthernet(targetIp, targetPort)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connect error: {ex.Message}");
                ok = false;
            }
            finally
            {
                _isConnecting = false;
            }

            if (ok)
            {
                // ── Connection succeeded: store it now ──
                ConnectedIp = ip;
                Properties.Settings.Default.LastTcpIpAddress = ip;
                Properties.Settings.Default.Save();

                _tempStatusEndTime = DateTime.MinValue;
                TripleStateButton.IsEnabled = true;
                UpdateStatusText();
            }
            else
            {
                // ── Connection failed: store nothing, keep nothing ──
                ConnectedIp = null;
                TripleStateButton.IsEnabled = false;
                _tempStatusMessage = "CONNECTION FAILED";
                _tempStatusEndTime = DateTime.Now.AddSeconds(3);
                UpdateStatusText();
            }
        }

        // internal (not private) so MainWindow's BLLogic.OnDisconnected /
        // OnConnectionLost handlers can force a refresh.
        internal void UpdateStatusText()
        {
            if (BLLogic == null) return;

            // NEW: while connecting, keep showing progress; the poll timer
            // must not overwrite it with the idle state.
            if (_isConnecting)
            {
                TcpScanStatus.Foreground = Brushes.Orange;
                TcpScanStatus.Text = "CONNECTING...";
                TcpConnectButton.IsEnabled = false;
                TripleStateButton.IsEnabled = false;
                return;
            }

            if (DateTime.Now < _tempStatusEndTime)
            {
                TcpScanStatus.Foreground = Brushes.Red;
                TcpScanStatus.Text = _tempStatusMessage;

                TcpConnectButton.Content = "CONNECT";
                TcpConnectButton.IsEnabled = true;
                TcpConnectButton.Background = null;
                TripleStateButton.IsEnabled = false;
            }
            // IsConnected reflects the live socket state immediately.
            else if (BLLogic.IsConnected)
            {
                TcpScanStatus.Foreground = Brushes.LimeGreen;
                TcpScanStatus.Text = $"CONNECTED: {BLLogic.ActivePortName}";

                TcpConnectButton.Content = "DISCONNECT";
                TcpConnectButton.IsEnabled = true;
                TcpConnectButton.Background =
                    new SolidColorBrush(Color.FromRgb(255, 230, 230));
            }
            else
            {
                TcpScanStatus.Foreground = Brushes.Gray;
                TcpScanStatus.Text = "";
                TcpConnectButton.Content = "CONNECT";
                TcpConnectButton.IsEnabled = true;
                TcpConnectButton.Background = null;
                TripleStateButton.IsEnabled = false;
            }
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            var advancedControl = new AdvancedSettingsControl();

            Window popup = new Window
            {
                Title = "Advanced Configuration",
                Content = advancedControl,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            popup.ShowDialog();
        }
    }
}