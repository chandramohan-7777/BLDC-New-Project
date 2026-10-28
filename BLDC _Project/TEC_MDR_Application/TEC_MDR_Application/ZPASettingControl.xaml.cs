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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            if (ModeComboBox == null || UpStreamIPTextBox == null ||
                DownStreamIPTextBox == null || MDRflagComboBox == null) return;

            bool gapTrain = IsGapTrainMode;

            UpStreamIPTextBox.IsEnabled = gapTrain;     // Master IP
            DownStreamIPTextBox.IsEnabled = gapTrain;   // Last IP
            MDRflagComboBox.IsEnabled = gapTrain;       // MDR Off Flag

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
            var cb = (ComboBox)sender;

            ApplyModeState();   // always refresh enable/disable + data, even for code-driven changes

            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

            // 0 = OFF, 1 = GAP Train
            BLLogic?.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex);
        }
        //private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var cb = (ComboBox)sender;

        //    ApplyModeState();   // refresh enable/disable + data

        //    if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

        //    // 0 = OFF, 1 = GAP Train
        //    if (BLLogic == null || !BLLogic.SendWriteCommand(0x0040, (ushort)cb.SelectedIndex))
        //        return;

        //    // GAP Train only: the command is already queued, now close the connection.
        //    if (cb.SelectedIndex == MODE_GAP_TRAIN)
        //        CommMotorSelectControl.Instance?.DropConnectionForModeChange();
        //}
        private void MDRDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)sender;
            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;

            // 0 = CW, 1 = CCW (same encoding as ParseAndUpdateUI: val == 1 -> CCW)
            BLLogic?.SendWriteCommand(0x0041, (ushort)cb.SelectedIndex);
        }

        // Referenced in XAML (MDR OFF FLAG combo). Stub - set REG_MDR_OFF_FLAG to enable writing.
        private void MDRflagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)sender;
            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
            if (REG_MDR_OFF_FLAG == null) return;

            // 0 = Enable, 1 = Disable (matches item order in XAML)
            BLLogic?.SendWriteCommand(REG_MDR_OFF_FLAG.Value, (ushort)cb.SelectedIndex);
        }

        // Referenced in XAML (ZPA DIRECTION combo). Stub - set REG_ZPA_IP_DIRECTION to enable writing.
        private void IpDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)sender;
            if (!cb.IsLoaded || cb.SelectedIndex < 0 || !IsUserChange(cb)) return;
            if (REG_ZPA_IP_DIRECTION == null) return;

            // 0 = Up Stream, 1 = Down stream (matches item order in XAML)
            BLLogic?.SendWriteCommand(REG_ZPA_IP_DIRECTION.Value, (ushort)cb.SelectedIndex);
        }

        // ── Text boxes: send on Enter, or on focus-loss if the text changed ──
        private void ZpaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            tb.Tag = tb.Text;                       // remember value at focus time
        }

        private void ZpaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            if ((tb.Tag as string) != tb.Text) SendTextBox(tb);
        }

        private void ZpaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var tb = (TextBox)sender;
            SendTextBox(tb);
            tb.Tag = tb.Text;
            Keyboard.ClearFocus();
        }

        private void SendTextBox(TextBox tb)
        {
            if (!IsGapTrainMode) return;   // only meaningful in GAP Train mode

            ushort? reg = tb == UpStreamIPTextBox ? REG_UPSTREAM_IP
                        : tb == DownStreamIPTextBox ? REG_DOWNSTREAM_IP
                        : (ushort?)null;

            if (reg == null || BLLogic == null) return;
            // User may type "30" or the full "192.168.1.30" - only the last octet goes to the device
            if (!byte.TryParse(LastOctetOf(tb.Text), out byte value)) return;

            BLLogic.SendWriteCommand(reg.Value, value);

            // Show it back in full form
            tb.Text = ToFullIp(value.ToString());
        }

        public void UpdateMotorTelemetry(ModbusDevice device)
        {
            if (device == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
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

        private void Combo_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is ComboBox cb && cb.IsDropDownOpen)
                cb.IsDropDownOpen = false;
        }

        public void ClearUI()
        {
            ModeComboBox.SelectedIndex = -1;
            MDRDirectionComboBox.SelectedIndex = -1;

            // Mode is now -1, so this disables and empties Master IP, Last IP and MDR Off Flag
            ApplyModeState();
        }
    }
}