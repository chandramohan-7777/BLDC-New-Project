//////////using System;
//////////using System.Collections.Generic;
//////////using System.Linq;
//////////using System.Text;
//////////using System.Threading.Tasks;
//////////using System.Windows;
//////////using System.Windows.Controls;
//////////using System.Windows.Data;
//////////using System.Windows.Documents;
//////////using System.Windows.Input;
//////////using System.Windows.Media;
//////////using System.Windows.Media.Imaging;
//////////using System.Windows.Navigation;
//////////using System.Windows.Shapes;
//////////using BLDC_Demo.Models;
//////////namespace BLDC_Demo
//////////{
//////////    /// <summary>
//////////    /// Interaction logic for ZPASettingControl.xaml
//////////    /// </summary>
//////////    public partial class ZPASettingControl : UserControl
//////////    {
//////////        public ZPASettingControl()
//////////        {
//////////            InitializeComponent();
//////////        }
//////////        public void UpdateMotorTelemetry(ModbusDevice device)
//////////        {
//////////            if (device == null) return;

//////////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//////////            {
//////////                if (!UpStreamIPTextBox.IsFocused || device.node1 == "0")
//////////                {
//////////                    if (UpStreamIPTextBox.Text != device.node1)
//////////                        UpStreamIPTextBox.Text = device.node1;
//////////                }

//////////                if (!DownStreamIPTextBox.IsFocused || device.node1 == "0")
//////////                {
//////////                    if (DownStreamIPTextBox.Text != device.node1)
//////////                        DownStreamIPTextBox.Text = device.node1;
//////////                }

//////////            }));
//////////        }
//////////        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
//////////        {
//////////            var advancedControl = new AdvancedSettingsControl();

//////////            Window popup = new Window
//////////            {
//////////                Title = "Advanced Configuration",
//////////                Content = advancedControl,
//////////                SizeToContent = SizeToContent.WidthAndHeight,
//////////                WindowStartupLocation = WindowStartupLocation.CenterOwner,
//////////                Owner = System.Windows.Application.Current.MainWindow
//////////            };

//////////            popup.ShowDialog();
//////////        }
//////////    }
//////////}
////////using System;
////////using System.Windows;
////////using System.Windows.Controls;
////////using System.Windows.Input;
////////using BLDC_Demo.Models;          // <-- fixes 'ModbusDevice' not found

////////namespace BLDC_Demo
////////{
////////    public partial class ZPASettingControl : UserControl
////////    {
////////        public ZPASettingControl()
////////        {
////////            InitializeComponent();
////////        }

////////        public void UpdateMotorTelemetry(ModbusDevice device)
////////        {
////////            if (device == null) return;

////////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
////////            {
////////                // Upstream = Node1
////////                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
////////                {
////////                    if (UpStreamIPTextBox.Text != device.UpStreamIP)
////////                        UpStreamIPTextBox.Text = device.UpStreamIP;
////////                }

////////                // Downstream = Node2
////////                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
////////                {
////////                    if (DownStreamIPTextBox.Text != device.DownStreamIP)
////////                        DownStreamIPTextBox.Text = device.DownStreamIP;
////////                }
////////                //if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
////////                //{
////////                //    if (DownStreamIPTextBox.Text != device.DownStreamIP)
////////                //        DownStreamIPTextBox.Text = device.DownStreamIP;
////////                //}
////////            }));
////////        }

////////        // Closes the dropdown when the mouse leaves the combo
////////        private void Combo_MouseLeave(object sender, MouseEventArgs e)
////////        {
////////            if (sender is ComboBox cb && cb.IsDropDownOpen)
////////                cb.IsDropDownOpen = false;
////////        }

////////        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
////////        {
////////            var advancedControl = new AdvancedSettingsControl();

////////            Window popup = new Window
////////            {
////////                Title = "Advanced Configuration",
////////                Content = advancedControl,
////////                SizeToContent = SizeToContent.WidthAndHeight,
////////                WindowStartupLocation = WindowStartupLocation.CenterOwner,
////////                Owner = Application.Current.MainWindow
////////            };

////////            popup.ShowDialog();
////////        }
////////    }
////////}
//////using System;
//////using System.Windows;
//////using System.Windows.Controls;
//////using System.Windows.Input;
//////using BLDC_Demo.Controls;   // BL
//////using BLDC_Demo.Models;     // ModbusDevice

//////namespace BLDC_Demo
//////{
//////    public partial class ZPASettingControl : UserControl
//////    {
//////        // TODO: fill these in from the device spec. null = not configured yet.
//////        private  ushort? REG_ZPA_MODE = null;   // e.g. 0x0060
//////        private  ushort? REG_MDR_DIRECTION = null;   // e.g. 0x0061

//////        // Registers for the text boxes. UpStream/DownStream use the same addresses the
//////        // app already READS them from (BL.ParseAndUpdateUI 0x0024 / 0x0025) - confirm the
//////        // ESP32 accepts writes there. MDR off delay register is unknown: fill it in and it works.
//////        private const ushort REG_UPSTREAM_IP = 0x0024;
//////        private const ushort REG_DOWNSTREAM_IP = 0x0025;
//////        private ushort? REG_MDR_OFF_DELAY = null;   // TODO: fill in from register map

//////        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;
//////        private BL BLLogic => OwnerWindow?.BLLogic;

//////        public ZPASettingControl()
//////        {
//////            InitializeComponent();
//////        }

//////        // Only react to the user's own choice, not to code setting SelectedIndex
//////        private static bool IsUserChange(ComboBox cb) =>
//////            cb.IsDropDownOpen || cb.IsKeyboardFocusWithin;

//////        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////        {
//////            var cb = (ComboBox)sender;
//////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

//////            // 0 = OFF, 1 = Singulation (Bus), 2 = GAP Train
//////            BLLogic?.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex);
//////        }

//////        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//////        {
//////            var cb = (ComboBox)sender;
//////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
//////            BLLogic?.SendWriteCommand(0x0041, (ushort)cb.SelectedIndex);
//////            // 0 = CW, 1 = CCW (same encoding as ParseAndUpdateUI: val == 1 -> CCW)

//////        }



//////        // ── Text boxes: send on Enter, or on focus-loss if the text changed ──
//////        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
//////        {
//////            var tb = (TextBox)sender;
//////            tb.Tag = tb.Text;                       // remember value at focus time
//////        }

//////        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
//////        {
//////            var tb = (TextBox)sender;
//////            if ((tb.Tag as string) != tb.Text) SendTextBox(tb);
//////        }

//////        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
//////        {
//////            if (e.Key != Key.Enter) return;
//////            var tb = (TextBox)sender;
//////            SendTextBox(tb);
//////            tb.Tag = tb.Text;
//////            Keyboard.ClearFocus();
//////        }

//////        private void SendTextBox(TextBox tb)
//////        {
//////            ushort? reg = tb == UpStreamIPTextBox ? REG_UPSTREAM_IP
//////                        : tb == DownStreamIPTextBox ? REG_DOWNSTREAM_IP

//////                        : (ushort?)null;

//////            if (reg == null || BLLogic == null) return;
//////            if (!ushort.TryParse(tb.Text.Trim(), out ushort value)) return;

//////            BLLogic.SendWriteCommand(reg.Value, value);
//////        }

//////        public void UpdateMotorTelemetry(ModbusDevice device)
//////        {
//////            if (device == null) return;

//////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//////            {
//////                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
//////                {
//////                    if (UpStreamIPTextBox.Text != device.UpStreamIP)
//////                        UpStreamIPTextBox.Text = device.UpStreamIP;
//////                }

//////                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
//////                {
//////                    if (DownStreamIPTextBox.Text != device.DownStreamIP)
//////                        DownStreamIPTextBox.Text = device.DownStreamIP;
//////                }
//////            }));
//////        }

//////        private void Combo_MouseLeave(object sender, MouseEventArgs e)
//////        {
//////            if (sender is ComboBox cb && cb.IsDropDownOpen)
//////                cb.IsDropDownOpen = false;
//////        }
//////        public void ClearUI()
//////        {
//////            UpStreamIPTextBox.Text = string.Empty;
//////            DownStreamIPTextBox.Text = string.Empty;


//////            ModeComboBox.SelectedIndex = -1;
//////            MDRDirectionComboBox.SelectedIndex = -1;

//////        }
//////        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
//////        {
//////            var advancedControl = new AdvancedSettingsControl();

//////            Window popup = new Window
//////            {
//////                Title = "Advanced Configuration",
//////                Content = advancedControl,
//////                SizeToContent = SizeToContent.WidthAndHeight,
//////                WindowStartupLocation = WindowStartupLocation.CenterOwner,
//////                Owner = Application.Current.MainWindow
//////            };

//////            popup.ShowDialog();
//////        }
//////    }
//////}
////using System;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Input;
////using BLDC_Demo.Controls;   // BL
////using BLDC_Demo.Models;     // ModbusDevice

////namespace BLDC_Demo
////{
////    public partial class ZPASettingControl : UserControl
////    {
////        // TODO: fill these in from the device spec. null = not configured yet.
////        private ushort? REG_ZPA_MODE = null;        // e.g. 0x0060
////        private ushort? REG_MDR_DIRECTION = null;   // e.g. 0x0061

////        // Registers for the text boxes. UpStream/DownStream use the same addresses the
////        // app already READS them from (BL.ParseAndUpdateUI 0x0024 / 0x0025) - confirm the
////        // ESP32 accepts writes there. MDR off delay register is unknown: fill it in and it works.
////        private const ushort REG_UPSTREAM_IP = 0x0024;
////        private const ushort REG_DOWNSTREAM_IP = 0x0025;
////        private ushort? REG_MDR_OFF_DELAY = null;   // TODO: fill in from register map

////        // TODO: fill these in from the register map (used by the stub handlers below)
////        private ushort? REG_MDR_OFF_FLAG = null;
////        private ushort? REG_ZPA_IP_DIRECTION = null;

////        // GAP Train is index 1 in ModeComboBox (OFF = 0, GAP Train = 1)
////        private const int MODE_GAP_TRAIN = 1;

////        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;
////        private BL BLLogic => OwnerWindow?.BLLogic;

////        public ZPASettingControl()
////        {
////            InitializeComponent();
////            ApplyModeState();   // start with Master IP / Last IP / MDR flag disabled
////        }

////        // Enable Master IP, Last IP and MDR Off Flag only when Mode = GAP Train
////        private void ApplyModeState()
////        {
////            // Controls can be null while InitializeComponent is still running
////            if (ModeComboBox == null || UpStreamIPTextBox == null ||
////                DownStreamIPTextBox == null || MDRflagComboBox == null) return;

////            bool gapTrain = ModeComboBox.SelectedIndex == MODE_GAP_TRAIN;

////            UpStreamIPTextBox.IsEnabled = gapTrain;     // Master IP
////            DownStreamIPTextBox.IsEnabled = gapTrain;   // Last IP
////            MDRflagComboBox.IsEnabled = gapTrain;       // MDR Off Flag

////            // If ZPA Direction should also depend on GAP Train, uncomment:
////            // ipdirection.IsEnabled = gapTrain;
////        }

////        // Only react to the user's own choice, not to code setting SelectedIndex
////        private static bool IsUserChange(ComboBox cb) =>
////            cb.IsDropDownOpen || cb.IsKeyboardFocusWithin;

////        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;

////            ApplyModeState();   // always refresh enable/disable, even for code-driven changes

////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

////            // 0 = OFF, 1 = GAP Train
////            BLLogic?.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex);
////        }

////        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;
////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

////            // 0 = CW, 1 = CCW (same encoding as ParseAndUpdateUI: val == 1 -> CCW)
////            BLLogic?.SendWriteCommand(0x0041, (ushort)cb.SelectedIndex);
////        }

////        // Referenced in XAML (MDR OFF FLAG combo). Stub - set REG_MDR_OFF_FLAG to enable writing.
////        private void MDRflagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;
////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
////            if (REG_MDR_OFF_FLAG == null) return;

////            // 0 = Enable, 1 = Disable (matches item order in XAML)
////            BLLogic?.SendWriteCommand(REG_MDR_OFF_FLAG.Value, (ushort)cb.SelectedIndex);
////        }

////        // Referenced in XAML (ZPA DIRECTION combo). Stub - set REG_ZPA_IP_DIRECTION to enable writing.
////        private void IpDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;
////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
////            if (REG_ZPA_IP_DIRECTION == null) return;

////            // 0 = Up Stream, 1 = Down stream (matches item order in XAML)
////            BLLogic?.SendWriteCommand(REG_ZPA_IP_DIRECTION.Value, (ushort)cb.SelectedIndex);
////        }

////        // ── Text boxes: send on Enter, or on focus-loss if the text changed ──
////        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
////        {
////            var tb = (TextBox)sender;
////            tb.Tag = tb.Text;                       // remember value at focus time
////        }

////        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
////        {
////            var tb = (TextBox)sender;
////            if ((tb.Tag as string) != tb.Text) SendTextBox(tb);
////        }

////        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
////        {
////            if (e.Key != Key.Enter) return;
////            var tb = (TextBox)sender;
////            SendTextBox(tb);
////            tb.Tag = tb.Text;
////            Keyboard.ClearFocus();
////        }

////        private void SendTextBox(TextBox tb)
////        {
////            ushort? reg = tb == UpStreamIPTextBox ? REG_UPSTREAM_IP
////                        : tb == DownStreamIPTextBox ? REG_DOWNSTREAM_IP
////                        : (ushort?)null;

////            if (reg == null || BLLogic == null) return;
////            if (!ushort.TryParse(tb.Text.Trim(), out ushort value)) return;

////            BLLogic.SendWriteCommand(reg.Value, value);
////        }

////        public void UpdateMotorTelemetry(ModbusDevice device)
////        {
////            if (device == null) return;

////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
////            {
////                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
////                {
////                    if (UpStreamIPTextBox.Text != device.UpStreamIP)
////                        UpStreamIPTextBox.Text = device.UpStreamIP;
////                }

////                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
////                {
////                    if (DownStreamIPTextBox.Text != device.DownStreamIP)
////                        DownStreamIPTextBox.Text = device.DownStreamIP;
////                }
////            }));
////        }

////        private void Combo_MouseLeave(object sender, MouseEventArgs e)
////        {
////            if (sender is ComboBox cb && cb.IsDropDownOpen)
////                cb.IsDropDownOpen = false;
////        }

////        public void ClearUI()
////        {
////            UpStreamIPTextBox.Text = string.Empty;
////            DownStreamIPTextBox.Text = string.Empty;

////            ModeComboBox.SelectedIndex = -1;
////            MDRDirectionComboBox.SelectedIndex = -1;

////            ApplyModeState();
////        }

////        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
////        {
////            var advancedControl = new AdvancedSettingsControl();

////            Window popup = new Window
////            {
////                Title = "Advanced Configuration",
////                Content = advancedControl,
////                SizeToContent = SizeToContent.WidthAndHeight,
////                WindowStartupLocation = WindowStartupLocation.CenterOwner,
////                Owner = Application.Current.MainWindow
////            };

////            popup.ShowDialog();
////        }
////    }
////}
////using System;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Input;
////using BLDC_Demo.Controls;   // BL
////using BLDC_Demo.Models;     // ModbusDevice

////namespace BLDC_Demo
////{
////    public partial class ZPASettingControl : UserControl
////    {
////        // TODO: fill these in from the device spec. null = not configured yet.
////        private ushort? REG_ZPA_MODE = null;        // e.g. 0x0060
////        private ushort? REG_MDR_DIRECTION = null;   // e.g. 0x0061

////        // Registers for the text boxes. UpStream/DownStream use the same addresses the
////        // app already READS them from (BL.ParseAndUpdateUI 0x0024 / 0x0025) - confirm the
////        // ESP32 accepts writes there. MDR off delay register is unknown: fill it in and it works.
////        private const ushort REG_UPSTREAM_IP = 0x0024;
////        private const ushort REG_DOWNSTREAM_IP = 0x0025;
////        private ushort? REG_MDR_OFF_DELAY = null;   // TODO: fill in from register map

////        // TODO: fill these in from the register map (used by the stub handlers below)
////        private ushort? REG_MDR_OFF_FLAG = null;
////        private ushort? REG_ZPA_IP_DIRECTION = null;

////        // GAP Train is index 1 in ModeComboBox (OFF = 0, GAP Train = 1)
////        private const int MODE_GAP_TRAIN = 1;

////        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;
////        private BL BLLogic => OwnerWindow?.BLLogic;

////        public ZPASettingControl()
////        {
////            InitializeComponent();
////            ApplyModeState();   // start with Master IP / Last IP / MDR flag disabled
////        }

////        // ── IP display helpers ──────────────────────────────────────────────
////        // Prefix = first 3 octets of the IP we are connected to (e.g. "192.168.1")
////        private static string GetIpPrefix()
////        {
////            string ip = CommMotorSelectControl.Instance?.ConnectedIp;
////            if (string.IsNullOrWhiteSpace(ip))
////                ip = Properties.Settings.Default.LastTcpIpAddress;

////            string[] o = ip?.Split('.');
////            return (o != null && o.Length == 4) ? $"{o[0]}.{o[1]}.{o[2]}" : null;
////        }

////        // "30" -> "192.168.1.30" (returns the input unchanged if no prefix is known)
////        private static string ToFullIp(string lastOctet)
////        {
////            string prefix = GetIpPrefix();
////            if (prefix == null || string.IsNullOrWhiteSpace(lastOctet)) return lastOctet;
////            return $"{prefix}.{lastOctet.Trim()}";
////        }

////        // "192.168.1.30" -> "30", "30" -> "30"
////        private static string LastOctetOf(string text)
////        {
////            text = text?.Trim() ?? string.Empty;
////            int i = text.LastIndexOf('.');
////            return i >= 0 ? text.Substring(i + 1) : text;
////        }

////        // Enable Master IP, Last IP and MDR Off Flag only when Mode = GAP Train
////        private void ApplyModeState()
////        {
////            // Controls can be null while InitializeComponent is still running
////            if (ModeComboBox == null || UpStreamIPTextBox == null ||
////                DownStreamIPTextBox == null || MDRflagComboBox == null) return;

////            bool gapTrain = ModeComboBox.SelectedIndex == MODE_GAP_TRAIN;

////            UpStreamIPTextBox.IsEnabled = true;     // Master IP
////            DownStreamIPTextBox.IsEnabled = true;   // Last IP
////            MDRflagComboBox.IsEnabled = true;       // MDR Off Flag

////            // If ZPA Direction should also depend on GAP Train, uncomment:
////            // ipdirection.IsEnabled = gapTrain;
////        }

////        // Only react to the user's own choice, not to code setting SelectedIndex
////        private static bool IsUserChange(ComboBox cb) =>
////            cb.IsDropDownOpen || cb.IsKeyboardFocusWithin;

////        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;

////            ApplyModeState();   // always refresh enable/disable, even for code-driven changes

////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

////            // 0 = OFF, 1 = GAP Train
////            BLLogic?.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex);
////        }

////        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;
////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

////            // 0 = CW, 1 = CCW (same encoding as ParseAndUpdateUI: val == 1 -> CCW)
////            BLLogic?.SendWriteCommand(0x0041, (ushort)cb.SelectedIndex);
////        }

////        // Referenced in XAML (MDR OFF FLAG combo). Stub - set REG_MDR_OFF_FLAG to enable writing.
////        private void MDRflagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;
////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
////            if (REG_MDR_OFF_FLAG == null) return;

////            // 0 = Enable, 1 = Disable (matches item order in XAML)
////            BLLogic?.SendWriteCommand(REG_MDR_OFF_FLAG.Value, (ushort)cb.SelectedIndex);
////        }

////        // Referenced in XAML (ZPA DIRECTION combo). Stub - set REG_ZPA_IP_DIRECTION to enable writing.
////        private void IpDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            var cb = (ComboBox)sender;
////            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
////            if (REG_ZPA_IP_DIRECTION == null) return;

////            // 0 = Up Stream, 1 = Down stream (matches item order in XAML)
////            BLLogic?.SendWriteCommand(REG_ZPA_IP_DIRECTION.Value, (ushort)cb.SelectedIndex);
////        }

////        // ── Text boxes: send on Enter, or on focus-loss if the text changed ──
////        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
////        {
////            var tb = (TextBox)sender;
////            tb.Tag = tb.Text;                       // remember value at focus time
////        }

////        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
////        {
////            var tb = (TextBox)sender;
////            if ((tb.Tag as string) != tb.Text) SendTextBox(tb);
////        }

////        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
////        {
////            if (e.Key != Key.Enter) return;
////            var tb = (TextBox)sender;
////            SendTextBox(tb);
////            tb.Tag = tb.Text;
////            Keyboard.ClearFocus();
////        }

////        private void SendTextBox(TextBox tb)
////        {
////            ushort? reg = tb == UpStreamIPTextBox ? REG_UPSTREAM_IP
////                        : tb == DownStreamIPTextBox ? REG_DOWNSTREAM_IP
////                        : (ushort?)null;

////            if (reg == null || BLLogic == null) return;
////            // User may type "30" or the full "192.168.1.30" - only the last octet goes to the device
////            if (!byte.TryParse(LastOctetOf(tb.Text), out byte value)) return;

////            BLLogic.SendWriteCommand(reg.Value, value);

////            // Show it back in full form
////            tb.Text = ToFullIp(value.ToString());
////        }

////        public void UpdateMotorTelemetry(ModbusDevice device)
////        {
////            if (device == null) return;

////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
////            {
////                string masterFull = ToFullIp(device.UpStreamIP);
////                string lastFull = ToFullIp(device.DownStreamIP);

////                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
////                {
////                    if (UpStreamIPTextBox.Text != masterFull)
////                        UpStreamIPTextBox.Text = masterFull;
////                }

////                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
////                {
////                    if (DownStreamIPTextBox.Text != lastFull)
////                        DownStreamIPTextBox.Text = lastFull;
////                }
////            }));
////        }

////        private void Combo_MouseLeave(object sender, MouseEventArgs e)
////        {
////            if (sender is ComboBox cb && cb.IsDropDownOpen)
////                cb.IsDropDownOpen = false;
////        }

////        public void ClearUI()
////        {
////            UpStreamIPTextBox.Text = string.Empty;
////            DownStreamIPTextBox.Text = string.Empty;

////            ModeComboBox.SelectedIndex = -1;
////            MDRDirectionComboBox.SelectedIndex = -1;

////            ApplyModeState();
////        }

////        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
////        {
////            var advancedControl = new AdvancedSettingsControl();

////            Window popup = new Window
////            {
////                Title = "Advanced Configuration",
////                Content = advancedControl,
////                SizeToContent = SizeToContent.WidthAndHeight,
////                WindowStartupLocation = WindowStartupLocation.CenterOwner,
////                Owner = Application.Current.MainWindow
////            };

////            popup.ShowDialog();
////        }
////    }
////}
//using System;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using BLDC_Demo.Controls;   // BL
//using BLDC_Demo.Models;     // ModbusDevice

//namespace BLDC_Demo
//{
//    public partial class ZPASettingControl : UserControl
//    {
//        // TODO: fill these in from the device spec. null = not configured yet.
//        private ushort? REG_ZPA_MODE = null;        // e.g. 0x0060
//        private ushort? REG_MDR_DIRECTION = null;   // e.g. 0x0061

//        // Registers for the text boxes. UpStream/DownStream use the same addresses the
//        // app already READS them from (BL.ParseAndUpdateUI 0x0024 / 0x0025) - confirm the
//        // ESP32 accepts writes there. MDR off delay register is unknown: fill it in and it works.
//        private const ushort REG_UPSTREAM_IP = 0x0024;
//        private const ushort REG_DOWNSTREAM_IP = 0x0025;
//        private ushort? REG_MDR_OFF_DELAY = null;   // TODO: fill in from register map

//        // TODO: fill these in from the register map (used by the stub handlers below)
//        private ushort? REG_MDR_OFF_FLAG = null;
//        private ushort? REG_ZPA_IP_DIRECTION = null;

//        // GAP Train is index 1 in ModeComboBox (OFF = 0, GAP Train = 1)
//        private const int MODE_GAP_TRAIN = 1;

//        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;
//        private BL BLLogic => OwnerWindow?.BLLogic;

//        public ZPASettingControl()
//        {
//            InitializeComponent();
//            ApplyModeState();   // start with Master IP / Last IP / MDR flag disabled
//        }

//        // ── IP display helpers ──────────────────────────────────────────────
//        // Prefix = first 3 octets of the IP we are SUCCESSFULLY connected to (e.g. "192.168.1").
//        // No fallback to any saved/previous value: if there is no live connection,
//        // there is no prefix.
//        private static string GetIpPrefix()
//        {
//            string ip = CommMotorSelectControl.Instance?.ConnectedIp;

//            string[] o = ip?.Split('.');
//            return (o != null && o.Length == 4) ? $"{o[0]}.{o[1]}.{o[2]}" : null;
//        }

//        // "30" -> "192.168.1.30" (returns the input unchanged if no prefix is known)
//        private static string ToFullIp(string lastOctet)
//        {
//            string prefix = GetIpPrefix();
//            if (prefix == null || string.IsNullOrWhiteSpace(lastOctet)) return lastOctet;
//            return $"{prefix}.{lastOctet.Trim()}";
//        }

//        // "192.168.1.30" -> "30", "30" -> "30"
//        private static string LastOctetOf(string text)
//        {
//            text = text?.Trim() ?? string.Empty;
//            int i = text.LastIndexOf('.');
//            return i >= 0 ? text.Substring(i + 1) : text;
//        }

//        // Enable Master IP, Last IP and MDR Off Flag only when Mode = GAP Train
//        private void ApplyModeState()
//        {
//            // Controls can be null while InitializeComponent is still running
//            if (ModeComboBox == null || UpStreamIPTextBox == null ||
//                DownStreamIPTextBox == null || MDRflagComboBox == null) return;

//            bool gapTrain = ModeComboBox.SelectedIndex == MODE_GAP_TRAIN;

//            UpStreamIPTextBox.IsEnabled = true;     // Master IP
//            DownStreamIPTextBox.IsEnabled = true;   // Last IP
//            MDRflagComboBox.IsEnabled = true;       // MDR Off Flag

//            // If ZPA Direction should also depend on GAP Train, uncomment:
//            // ipdirection.IsEnabled = gapTrain;
//        }

//        // Only react to the user's own choice, not to code setting SelectedIndex
//        private static bool IsUserChange(ComboBox cb) =>
//            cb.IsDropDownOpen || cb.IsKeyboardFocusWithin;

//        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;

//            ApplyModeState();   // always refresh enable/disable, even for code-driven changes

//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

//            // 0 = OFF, 1 = GAP Train
//            BLLogic?.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex);
//        }

//        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;
//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

//            // 0 = CW, 1 = CCW (same encoding as ParseAndUpdateUI: val == 1 -> CCW)
//            BLLogic?.SendWriteCommand(0x0041, (ushort)cb.SelectedIndex);
//        }

//        // Referenced in XAML (MDR OFF FLAG combo). Stub - set REG_MDR_OFF_FLAG to enable writing.
//        private void MDRflagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;
//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
//            if (REG_MDR_OFF_FLAG == null) return;

//            // 0 = Enable, 1 = Disable (matches item order in XAML)
//            BLLogic?.SendWriteCommand(REG_MDR_OFF_FLAG.Value, (ushort)cb.SelectedIndex);
//        }

//        // Referenced in XAML (ZPA DIRECTION combo). Stub - set REG_ZPA_IP_DIRECTION to enable writing.
//        private void IpDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;
//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
//            if (REG_ZPA_IP_DIRECTION == null) return;

//            // 0 = Up Stream, 1 = Down stream (matches item order in XAML)
//            BLLogic?.SendWriteCommand(REG_ZPA_IP_DIRECTION.Value, (ushort)cb.SelectedIndex);
//        }

//        // ── Text boxes: send on Enter, or on focus-loss if the text changed ──
//        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
//        {
//            var tb = (TextBox)sender;
//            tb.Tag = tb.Text;                       // remember value at focus time
//        }

//        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            var tb = (TextBox)sender;
//            if ((tb.Tag as string) != tb.Text) SendTextBox(tb);
//        }

//        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.Key != Key.Enter) return;
//            var tb = (TextBox)sender;
//            SendTextBox(tb);
//            tb.Tag = tb.Text;
//            Keyboard.ClearFocus();
//        }

//        private void SendTextBox(TextBox tb)
//        {
//            ushort? reg = tb == UpStreamIPTextBox ? REG_UPSTREAM_IP
//                        : tb == DownStreamIPTextBox ? REG_DOWNSTREAM_IP
//                        : (ushort?)null;

//            if (reg == null || BLLogic == null) return;
//            // User may type "30" or the full "192.168.1.30" - only the last octet goes to the device
//            if (!byte.TryParse(LastOctetOf(tb.Text), out byte value)) return;

//            BLLogic.SendWriteCommand(reg.Value, value);

//            // Show it back in full form
//            tb.Text = ToFullIp(value.ToString());
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                string masterFull = ToFullIp(device.UpStreamIP);
//                string lastFull = ToFullIp(device.DownStreamIP);

//                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
//                {
//                    if (UpStreamIPTextBox.Text != masterFull)
//                        UpStreamIPTextBox.Text = masterFull;
//                }

//                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
//                {
//                    if (DownStreamIPTextBox.Text != lastFull)
//                        DownStreamIPTextBox.Text = lastFull;
//                }
//            }));
//        }

//        private void Combo_MouseLeave(object sender, MouseEventArgs e)
//        {
//            if (sender is ComboBox cb && cb.IsDropDownOpen)
//                cb.IsDropDownOpen = false;
//        }

//        public void ClearUI()
//        {
//            UpStreamIPTextBox.Text = string.Empty;
//            DownStreamIPTextBox.Text = string.Empty;

//            ModeComboBox.SelectedIndex = -1;
//            MDRDirectionComboBox.SelectedIndex = -1;

//            ApplyModeState();
//        }

//        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
//        {
//            var advancedControl = new AdvancedSettingsControl();

//            Window popup = new Window
//            {
//                Title = "Advanced Configuration",
//                Content = advancedControl,
//                SizeToContent = SizeToContent.WidthAndHeight,
//                WindowStartupLocation = WindowStartupLocation.CenterOwner,
//                Owner = Application.Current.MainWindow
//            };

//            popup.ShowDialog();
//        }
//    }
//}
//using System;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using BLDC_Demo.Controls;   // BL
//using BLDC_Demo.Models;     // ModbusDevice

//namespace BLDC_Demo
//{
//    public partial class ZPASettingControl : UserControl
//    {
//        // TODO: fill these in from the device spec. null = not configured yet.
//        private ushort? REG_ZPA_MODE = null;        // e.g. 0x0060
//        private ushort? REG_MDR_DIRECTION = null;   // e.g. 0x0061

//        // Registers for the text boxes. UpStream/DownStream use the same addresses the
//        // app already READS them from (BL.ParseAndUpdateUI 0x0024 / 0x0025) - confirm the
//        // ESP32 accepts writes there. MDR off delay register is unknown: fill it in and it works.
//        private const ushort REG_UPSTREAM_IP = 0x0024;
//        private const ushort REG_DOWNSTREAM_IP = 0x0025;
//        private ushort? REG_MDR_OFF_DELAY = null;   // TODO: fill in from register map

//        // TODO: fill these in from the register map (used by the stub handlers below)
//        private ushort? REG_MDR_OFF_FLAG = null;
//        private ushort? REG_ZPA_IP_DIRECTION = null;

//        // GAP Train is index 1 in ModeComboBox (OFF = 0, GAP Train = 1)
//        private const int MODE_GAP_TRAIN = 1;

//        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;
//        private BL BLLogic => OwnerWindow?.BLLogic;

//        public ZPASettingControl()
//        {
//            InitializeComponent();
//            ApplyModeState();   // start with Master IP / Last IP / MDR flag disabled
//        }

//        // ── IP display helpers ──────────────────────────────────────────────
//        // Prefix = first 3 octets of the IP we are SUCCESSFULLY connected to (e.g. "192.168.1").
//        // No fallback to any saved/previous value: if there is no live connection,
//        // there is no prefix.
//        private static string GetIpPrefix()
//        {
//            string ip = CommMotorSelectControl.Instance?.ConnectedIp;

//            string[] o = ip?.Split('.');
//            return (o != null && o.Length == 4) ? $"{o[0]}.{o[1]}.{o[2]}" : null;
//        }

//        // "30" -> "192.168.1.30" (returns the input unchanged if no prefix is known)
//        private static string ToFullIp(string lastOctet)
//        {
//            string prefix = GetIpPrefix();
//            if (prefix == null || string.IsNullOrWhiteSpace(lastOctet)) return lastOctet;
//            return $"{prefix}.{lastOctet.Trim()}";
//        }

//        // "192.168.1.30" -> "30", "30" -> "30"
//        private static string LastOctetOf(string text)
//        {
//            text = text?.Trim() ?? string.Empty;
//            int i = text.LastIndexOf('.');
//            return i >= 0 ? text.Substring(i + 1) : text;
//        }

//        // Enable Master IP, Last IP and MDR Off Flag only when Mode = GAP Train
//        private void ApplyModeState()
//        {
//            // Controls can be null while InitializeComponent is still running
//            if (ModeComboBox == null || UpStreamIPTextBox == null ||
//                DownStreamIPTextBox == null || MDRflagComboBox == null) return;

//            bool gapTrain = ModeComboBox.SelectedIndex == MODE_GAP_TRAIN;

//            UpStreamIPTextBox.IsEnabled = true;     // Master IP
//            DownStreamIPTextBox.IsEnabled = true;   // Last IP
//            MDRflagComboBox.IsEnabled = true;       // MDR Off Flag

//            // If ZPA Direction should also depend on GAP Train, uncomment:
//            // ipdirection.IsEnabled = gapTrain;
//        }

//        // Only react to the user's own choice, not to code setting SelectedIndex
//        private static bool IsUserChange(ComboBox cb) =>
//            cb.IsDropDownOpen || cb.IsKeyboardFocusWithin;

//        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;

//            ApplyModeState();   // always refresh enable/disable, even for code-driven changes

//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

//            // 0 = OFF, 1 = GAP Train
//            BLLogic?.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex);
//        }

//        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;
//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

//            // 0 = CW, 1 = CCW (same encoding as ParseAndUpdateUI: val == 1 -> CCW)
//            BLLogic?.SendWriteCommand(0x0041, (ushort)cb.SelectedIndex);
//        }

//        // Referenced in XAML (MDR OFF FLAG combo). Stub - set REG_MDR_OFF_FLAG to enable writing.
//        private void MDRflagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;
//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
//            if (REG_MDR_OFF_FLAG == null) return;

//            // 0 = Enable, 1 = Disable (matches item order in XAML)
//            BLLogic?.SendWriteCommand(REG_MDR_OFF_FLAG.Value, (ushort)cb.SelectedIndex);
//        }

//        // Referenced in XAML (ZPA DIRECTION combo). Stub - set REG_ZPA_IP_DIRECTION to enable writing.
//        private void IpDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var cb = (ComboBox)sender;
//            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
//            if (REG_ZPA_IP_DIRECTION == null) return;

//            // 0 = Up Stream, 1 = Down stream (matches item order in XAML)
//            BLLogic?.SendWriteCommand(REG_ZPA_IP_DIRECTION.Value, (ushort)cb.SelectedIndex);
//        }

//        // ── Text boxes: send on Enter, or on focus-loss if the text changed ──
//        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
//        {
//            var tb = (TextBox)sender;
//            tb.Tag = tb.Text;                       // remember value at focus time
//        }

//        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            var tb = (TextBox)sender;
//            if ((tb.Tag as string) != tb.Text) SendTextBox(tb);
//        }

//        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.Key != Key.Enter) return;
//            var tb = (TextBox)sender;
//            SendTextBox(tb);
//            tb.Tag = tb.Text;
//            Keyboard.ClearFocus();
//        }

//        private void SendTextBox(TextBox tb)
//        {
//            ushort? reg = tb == UpStreamIPTextBox ? REG_UPSTREAM_IP
//                        : tb == DownStreamIPTextBox ? REG_DOWNSTREAM_IP
//                        : (ushort?)null;

//            if (reg == null || BLLogic == null) return;
//            // User may type "30" or the full "192.168.1.30" - only the last octet goes to the device
//            if (!byte.TryParse(LastOctetOf(tb.Text), out byte value)) return;

//            BLLogic.SendWriteCommand(reg.Value, value);

//            // Show it back in full form
//            tb.Text = ToFullIp(value.ToString());
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                string masterFull = ToFullIp(device.UpStreamIP);
//                string lastFull = ToFullIp(device.DownStreamIP);

//                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
//                {
//                    if (UpStreamIPTextBox.Text != masterFull)
//                        UpStreamIPTextBox.Text = masterFull;
//                }

//                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
//                {
//                    if (DownStreamIPTextBox.Text != lastFull)
//                        DownStreamIPTextBox.Text = lastFull;
//                }
//            }));
//        }

//        private void Combo_MouseLeave(object sender, MouseEventArgs e)
//        {
//            if (sender is ComboBox cb && cb.IsDropDownOpen)
//                cb.IsDropDownOpen = false;
//        }

//        public void ClearUI()
//        {
//            UpStreamIPTextBox.Text = string.Empty;
//            DownStreamIPTextBox.Text = string.Empty;

//            ModeComboBox.SelectedIndex = -1;
//            MDRDirectionComboBox.SelectedIndex = -1;

//            ApplyModeState();
//        }

//        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
//        {
//            var advancedControl = new AdvancedSettingsControl();

//            Window popup = new Window
//            {
//                Title = "Advanced Configuration",
//                Content = advancedControl,
//                SizeToContent = SizeToContent.WidthAndHeight,
//                WindowStartupLocation = WindowStartupLocation.CenterOwner,
//                Owner = Application.Current.MainWindow
//            };

//            popup.ShowDialog();
//        }
//    }
//}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BLDC_Demo.Controls;   // BL
using BLDC_Demo.Models;     // ModbusDevice

namespace BLDC_Demo
{
    public partial class ZPASettingControl : UserControl
    {
        // TODO: fill these in from the device spec. null = not configured yet.
        private ushort? REG_ZPA_MODE = null;        // e.g. 0x0060
        private ushort? REG_MDR_DIRECTION = null;   // e.g. 0x0061

        // Registers for the text boxes. UpStream/DownStream use the same addresses the
        // app already READS them from (BL.ParseAndUpdateUI 0x0024 / 0x0025) - confirm the
        // ESP32 accepts writes there. MDR off delay register is unknown: fill it in and it works.
        private const ushort REG_UPSTREAM_IP = 0x0024;
        private const ushort REG_DOWNSTREAM_IP = 0x0025;
        private ushort? REG_MDR_OFF_DELAY = null;   // TODO: fill in from register map

        // TODO: fill these in from the register map (used by the stub handlers below)
        private ushort? REG_MDR_OFF_FLAG = null;
        private ushort? REG_ZPA_IP_DIRECTION = null;

        // GAP Train is index 1 in ModeComboBox (OFF = 0, GAP Train = 1)
        private const int MODE_GAP_TRAIN = 1;

        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;
        private BL BLLogic => OwnerWindow?.BLLogic;

        public ZPASettingControl()
        {
            InitializeComponent();
            ApplyModeState();   // start with Master IP / Last IP / MDR flag disabled and empty
        }

        // True only while Mode = GAP Train
        private bool IsGapTrainMode =>
            ModeComboBox != null && ModeComboBox.SelectedIndex == MODE_GAP_TRAIN;

        // True only while Edit Mode toggle is switched ON
        public bool IsEditMode => EditModeToggle?.IsChecked == true;

        private void EditModeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditMode)
            {
                Keyboard.ClearFocus();
                if (ModeComboBox != null) ModeComboBox.IsDropDownOpen = false;
                if (MDRDirectionComboBox != null) MDRDirectionComboBox.IsDropDownOpen = false;
                if (ipdirection != null) ipdirection.IsDropDownOpen = false;
                if (MDRflagComboBox != null) MDRflagComboBox.IsDropDownOpen = false;
            }
            ApplyModeState();
        }

        // ── IP display helpers ──────────────────────────────────────────────
        // Prefix = first 3 octets of the IP we are SUCCESSFULLY connected to (e.g. "192.168.1").
        // No fallback to any saved/previous value: if there is no live connection,
        // there is no prefix.
        private static string GetIpPrefix()
        {
            string ip = CommMotorSelectControl.Instance?.ConnectedIp;

            string[] o = ip?.Split('.');
            return (o != null && o.Length == 4) ? $"{o[0]}.{o[1]}.{o[2]}" : null;
        }

        // "30" -> "192.168.1.30" (returns the input unchanged if no prefix is known)
        private static string ToFullIp(string lastOctet)
        {
            string prefix = GetIpPrefix();
            if (prefix == null || string.IsNullOrWhiteSpace(lastOctet)) return lastOctet;
            return $"{prefix}.{lastOctet.Trim()}";
        }

        // "192.168.1.30" -> "30", "30" -> "30"
        private static string LastOctetOf(string text)
        {
            text = text?.Trim() ?? string.Empty;
            int i = text.LastIndexOf('.');
            return i >= 0 ? text.Substring(i + 1) : text;
        }

        // Mode = GAP Train  -> MDR Off Flag, Master IP and Last IP are enabled and show their data.
        // Any other mode    -> they are disabled and empty (no data shown).
        private void ApplyModeState()
        {
            // Controls can be null while InitializeComponent is still running
            if (ModeComboBox == null || MDRDirectionComboBox == null ||
                UpStreamIPTextBox == null || DownStreamIPTextBox == null ||
                MDRflagComboBox == null) return;

            bool isEdit = IsEditMode;
            bool gapTrain = IsGapTrainMode;

            // When Edit Mode is OFF: ALL dropdowns, input fields, and the Set button are disabled.
            // Option cannot be chosen or changed when not in edit mode.
            ModeComboBox.IsEnabled = isEdit;
            if (MDRDirectionComboBox != null)
                MDRDirectionComboBox.IsEnabled = isEdit;
            if (ipdirection != null)
                ipdirection.IsEnabled = isEdit;
            if (SetConfigurationButton != null)
                SetConfigurationButton.IsEnabled = isEdit;

            // When Edit Mode is ON, Master IP, Last IP, and MDR Off Flag depend on GAP Train mode.
            // When Edit Mode is OFF, they are disabled.
            UpStreamIPTextBox.IsEnabled = isEdit && gapTrain;     // Master IP
            DownStreamIPTextBox.IsEnabled = isEdit && gapTrain;   // Last IP
            MDRflagComboBox.IsEnabled = isEdit && gapTrain;       // MDR Off Flag

            if (!gapTrain)
            {
                // Not GAP Train: do not show any data
                UpStreamIPTextBox.Text = string.Empty;
                DownStreamIPTextBox.Text = string.Empty;
                MDRflagComboBox.SelectedIndex = -1;
            }

            // If ZPA Direction should also depend on GAP Train, uncomment:
            // ipdirection.IsEnabled = gapTrain;
        }

        // Only react to the user's own choice, not to code setting SelectedIndex
        private static bool IsUserChange(ComboBox cb) =>
            cb.IsDropDownOpen || cb.IsKeyboardFocusWithin;

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update UI enable/disable states (Master IP, Last IP, MDR Off Flag)
            ApplyModeState();
            // Note: Modbus write is deferred to Set Configuration button click
        }

        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Note: Modbus write is deferred to Set Configuration button click
        }

        private void MDRflagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Note: Modbus write is deferred to Set Configuration button click
        }

        private void IpDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Note: Modbus write is deferred to Set Configuration button click
        }

        // ── Text boxes: format display on Enter or LostFocus without sending write commands ──
        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            tb.Tag = tb.Text;
        }

        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            FormatIpTextBox(tb);
        }

        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var tb = (TextBox)sender;
            FormatIpTextBox(tb);
            tb.Tag = tb.Text;
            Keyboard.ClearFocus();
        }

        private void FormatIpTextBox(TextBox tb)
        {
            if (tb == null || string.IsNullOrWhiteSpace(tb.Text)) return;
            if (byte.TryParse(LastOctetOf(tb.Text), out byte val))
            {
                tb.Text = ToFullIp(val.ToString());
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  SET CONFIGURATION BUTTON CLICK
        //  Sends ZPA configuration to the controller.
        //  - If Mode is OFF: sends 0x0040 = 0 directly without requiring
        //    MDR direction or ZPA direction.
        //  - If Mode is GAP Train: validates required direction fields,
        //    sends parameters, and displays "Configuration sent".
        // ══════════════════════════════════════════════════════════════
        private async void SetConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditMode)
            {
                MessageBox.Show("Please enable EDIT MODE before applying configuration.",
                                "Edit Mode Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BLLogic == null || !BLLogic.IsConnected)
            {
                MessageBox.Show("Controller is not connected. Please connect first.",
                                "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModeComboBox == null || ModeComboBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a MODE.", "Required Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                ModeComboBox?.Focus();
                return;
            }

            // ── Turning ZPA OFF (Mode = 0) ──
            // Only click OFF then Set Configuration — no direction selection required
            if (ModeComboBox.SelectedIndex == 0)
            {
                BLLogic.SendWriteCommand(0x0040, 0);
                System.Diagnostics.Debug.WriteLine("[ZPA SET] Mode sent: 0 (OFF)");
                SetZpaActiveIndicator(false);
                _previousZpaMode = 0;
                MessageBox.Show("Configuration sent", "Configuration Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ── Turning ZPA ON (GAP Train Mode = 1) ──
            var missingFields = new List<string>();
            if (MDRDirectionComboBox == null || MDRDirectionComboBox.SelectedIndex < 0)
                missingFields.Add("MDR DIRECTION");
            if (ipdirection == null || ipdirection.SelectedIndex < 0)
                missingFields.Add("ZPA DIRECTION");

            if (missingFields.Count > 0)
            {
                string msg = missingFields.Count == 1
                    ? $"Please select a {missingFields[0]}."
                    : $"The following fields are required:\n• " + string.Join("\n• ", missingFields);

                MessageBox.Show(msg, "Required Field", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (MDRDirectionComboBox?.SelectedIndex < 0) MDRDirectionComboBox.Focus();
                else ipdirection?.Focus();

                return;
            }

            // 1. MODE (Register 0x0040: 1 = GAP Train)
            ushort modeVal = (ushort)ModeComboBox.SelectedIndex;
            BLLogic.SendWriteCommand(0x0040, modeVal);
            System.Diagnostics.Debug.WriteLine($"[ZPA SET] Mode sent: {modeVal} (GAP Train)");
            await Task.Delay(10);

            // 2. MDR DIRECTION (Register 0x0041: 0 = CW, 1 = CCW)
            if (MDRDirectionComboBox != null && MDRDirectionComboBox.SelectedIndex >= 0)
            {
                ushort dirVal = (ushort)MDRDirectionComboBox.SelectedIndex;
                BLLogic.SendWriteCommand(0x0041, dirVal);
                System.Diagnostics.Debug.WriteLine($"[ZPA SET] MDR Direction sent: {dirVal} ({(dirVal == 1 ? "CCW" : "CW")})");
                await Task.Delay(10);
            }

            // 3. ZPA DIRECTION (REG_ZPA_IP_DIRECTION: 0 = Up Stream, 1 = Down stream)
            if (ipdirection != null && ipdirection.SelectedIndex >= 0 && REG_ZPA_IP_DIRECTION.HasValue)
            {
                ushort zpaDirVal = (ushort)ipdirection.SelectedIndex;
                BLLogic.SendWriteCommand(REG_ZPA_IP_DIRECTION.Value, zpaDirVal);
                System.Diagnostics.Debug.WriteLine($"[ZPA SET] ZPA Direction sent: {zpaDirVal}");
                await Task.Delay(10);
            }

            // 4. MDR OFF FLAG (REG_MDR_OFF_FLAG: 0 = Enable, 1 = Disable)
            if (MDRflagComboBox != null && MDRflagComboBox.SelectedIndex >= 0 && REG_MDR_OFF_FLAG.HasValue)
            {
                ushort flagVal = (ushort)MDRflagComboBox.SelectedIndex;
                BLLogic.SendWriteCommand(REG_MDR_OFF_FLAG.Value, flagVal);
                System.Diagnostics.Debug.WriteLine($"[ZPA SET] MDR Off Flag sent: {flagVal}");
                await Task.Delay(10);
            }

            // 5. MASTER IP (Register 0x0024: Upstream IP last octet) - applicable in GAP Train mode
            if (UpStreamIPTextBox != null && !string.IsNullOrWhiteSpace(UpStreamIPTextBox.Text))
            {
                if (byte.TryParse(LastOctetOf(UpStreamIPTextBox.Text), out byte upValue))
                {
                    BLLogic.SendWriteCommand(REG_UPSTREAM_IP, upValue);
                    UpStreamIPTextBox.Text = ToFullIp(upValue.ToString());
                    System.Diagnostics.Debug.WriteLine($"[ZPA SET] Master IP sent: {upValue}");
                    await Task.Delay(10);
                }
                else
                {
                    MessageBox.Show("Please enter a valid Master IP (0–255).", "Invalid Master IP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 6. LAST IP (Register 0x0025: Downstream IP last octet) - applicable in GAP Train mode
            if (DownStreamIPTextBox != null && !string.IsNullOrWhiteSpace(DownStreamIPTextBox.Text))
            {
                if (byte.TryParse(LastOctetOf(DownStreamIPTextBox.Text), out byte downValue))
                {
                    BLLogic.SendWriteCommand(REG_DOWNSTREAM_IP, downValue);
                    DownStreamIPTextBox.Text = ToFullIp(downValue.ToString());
                    System.Diagnostics.Debug.WriteLine($"[ZPA SET] Last IP sent: {downValue}");
                    await Task.Delay(10);
                }
                else
                {
                    MessageBox.Show("Please enter a valid Last IP (0–255).", "Invalid Last IP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Popup as requested: "Configuration sent"
            MessageBox.Show("Configuration sent", "Configuration Sent", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private int _previousZpaMode = -1;

        public void UpdateMotorTelemetry(ModbusDevice device)
        {
            if (device == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Read mode status directly from telemetry
                if (device.ZpaMode == 1)
                {
                    // Green light blinks near edit option in ZPA only
                    SetZpaActiveIndicator(true);

                    // Show popup when transition to active occurs (only once per activation)
                    if (_previousZpaMode != 1)
                    {
                        _previousZpaMode = 1;

                        if (ModeComboBox != null && !IsEditMode && ModeComboBox.SelectedIndex != MODE_GAP_TRAIN)
                        {
                            ModeComboBox.SelectedIndex = MODE_GAP_TRAIN;
                        }

                        MessageBox.Show("ZPA is active", "ZPA Status", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    SetZpaActiveIndicator(false);
                    _previousZpaMode = 0;
                }

                // Master / Last IP data is shown only in GAP Train mode
                if (!IsGapTrainMode) return;

                string masterFull = ToFullIp(device.UpStreamIP);
                string lastFull = ToFullIp(device.DownStreamIP);

                if (!UpStreamIPTextBox.IsFocused || device.UpStreamIP == "0")
                {
                    if (UpStreamIPTextBox.Text != masterFull)
                        UpStreamIPTextBox.Text = masterFull;
                }

                if (!DownStreamIPTextBox.IsFocused || device.DownStreamIP == "0")
                {
                    if (DownStreamIPTextBox.Text != lastFull)
                        DownStreamIPTextBox.Text = lastFull;
                }
            }));
        }

        // Shows or hides the blinking green ZPA-active dot near the EDIT toggle.
        // The dot is only relevant in ZPA (GAP Train) mode; it goes away when
        // ZPA is off OR when the card reports ZpaMode = 0 (auto-config still running).
        private Storyboard _zpaBlink;
        private bool _zpaIndicatorOn = false;

        private void SetZpaActiveIndicator(bool active)
        {
            if (ZpaActiveDot == null) return;
            if (active == _zpaIndicatorOn) return;   // no change needed

            _zpaIndicatorOn = active;

            if (active)
            {
                ZpaActiveDot.Visibility = Visibility.Visible;

                // Retrieve and cache the storyboard from XAML resources on first use
                if (_zpaBlink == null)
                    _zpaBlink = (Storyboard)Resources["ZpaBlinkStoryboard"];

                _zpaBlink?.Begin(ZpaActiveDot, true);
            }
            else
            {
                _zpaBlink?.Stop(ZpaActiveDot);
                ZpaActiveDot.Visibility = Visibility.Collapsed;
                ZpaActiveDot.Opacity = 1.0;
            }
        }

        private void Combo_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is ComboBox cb && cb.IsDropDownOpen)
                cb.IsDropDownOpen = false;
        }

        public void ClearUI()
        {
            if (EditModeToggle != null)
                EditModeToggle.IsChecked = false;

            ModeComboBox.SelectedIndex = -1;
            MDRDirectionComboBox.SelectedIndex = -1;
            if (ipdirection != null)
                ipdirection.SelectedIndex = -1;

            // Mode is now -1, so this disables and empties Master IP, Last IP and MDR Off Flag
            ApplyModeState();

            // Hide the ZPA-active indicator when the UI is cleared (e.g. on disconnect)
            SetZpaActiveIndicator(false);
            _previousZpaMode = -1;
        }
    }
}