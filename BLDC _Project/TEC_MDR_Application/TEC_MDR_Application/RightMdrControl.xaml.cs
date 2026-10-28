
////using System;
////using System.Collections.ObjectModel;
////using System.IO;
////using System.Linq;
////using System.Windows;
////using System.Windows.Controls;
////using BLDC_Demo.Controls;
////using BLDC_Demo.Models;

////namespace BLDC_Demo
////{
////    public partial class RightMdrControl : UserControl
////    {

////        private static readonly string MotorTypesFile =
////            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

////        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
////            = new ObservableCollection<MotorTypeItem>();

////        private static readonly string BrakeTypesFile =
////            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

////        public ObservableCollection<string> BrakeTypes { get; set; }
////            = new ObservableCollection<string>();

////        public RightMdrControl()
////        {
////            InitializeComponent();

////            LoadMotorTypes();
////            RightMotorCombo.ItemsSource = MotorTypes;
////            RightMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
////            if (MotorTypes.Count > 0)
////                RightMotorCombo.SelectedIndex = -1;
////            RightMotorCombo.SelectionChanged += RightMotorCombo_SelectionChanged;

////            LoadBrakeTypes();
////            RightBrakeCombo.ItemsSource = BrakeTypes;
////            if (BrakeTypes.Count > 0)
////                RightBrakeCombo.SelectedIndex = -1;
////            RightBrakeCombo.SelectionChanged += RightBrakeCombo_SelectionChanged;
////        }

////        private void LoadMotorTypes()
////        {
////            MotorTypes.Clear();

////            if (!File.Exists(MotorTypesFile))
////            {
////                File.WriteAllLines(MotorTypesFile, new[]
////                {
////                    "BL 42 85,24,",
////                    "BL 42 85,19,",
////                    //"BL 42 85,15,",
////                    //"BL 42 85,4.9,",
////                    //"BL 42 93,19,",
////                    //"BL 42 93,27,",
////                    //"BL 42 93,49,",
////                    //"BL 42 93,82,",
////                    //"BL 57 93,23,",
////                    //"BL 57 93,96,",
////                    //"BL 57 93,43,"
////                });
////            }

////            foreach (string line in File.ReadAllLines(MotorTypesFile))
////            {
////                if (string.IsNullOrWhiteSpace(line)) continue;

////                string[] parts = line.Split(',');
////                string name = parts[0].Trim();
////                string gear = parts.Length > 1 ? parts[1].Trim() : string.Empty;

////                if (!string.IsNullOrWhiteSpace(name))
////                    MotorTypes.Add(new MotorTypeItem(name, gear));
////            }
////        }

////        public void SaveMotorTypes()
////        {
////            var lines = MotorTypes.Select(m => $"{m.Name},{m.Gear},");
////            File.WriteAllLines(MotorTypesFile, lines);
////        }
////        public void SetConnected(bool connected)
////        {
////            // Only the SET buttons follow the connection state.
////            // Combos and text boxes stay usable.
////            SetButtonsEnabled = connected;
////        }
////        public static readonly DependencyProperty SetButtonsEnabledProperty =
////    DependencyProperty.Register(
////        nameof(SetButtonsEnabled),
////        typeof(bool),
////        typeof(RightMdrControl),
////        new PropertyMetadata(false)); // SET buttons disabled until connected

////        public bool SetButtonsEnabled
////        {
////            get => (bool)GetValue(SetButtonsEnabledProperty);
////            set => SetValue(SetButtonsEnabledProperty, value);
////        }
////        private void LoadBrakeTypes()
////        {
////            BrakeTypes.Clear();

////            if (!File.Exists(BrakeTypesFile))
////            {
////                File.WriteAllLines(BrakeTypesFile, new[]
////                {
////                    "Normal,,",
////                    "ServoBreak,,"
////                });
////            }

////            foreach (string line in File.ReadAllLines(BrakeTypesFile))
////            {
////                if (string.IsNullOrWhiteSpace(line)) continue;
////                string name = line.Split(',')[0].Trim();
////                if (!string.IsNullOrWhiteSpace(name))
////                    BrakeTypes.Add(name);
////            }
////        }

////        public void SaveBrakeTypes()
////        {
////            var lines = BrakeTypes.Select(name => $"{name},,");
////            File.WriteAllLines(BrakeTypesFile, lines);
////        }

////        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;

////        // Convenience accessor for the BL logic instance owned by MainWindow.
////        private BL BLLogic => OwnerWindow?.BLLogic;

////        // Fires the write the instant the user picks a new item — no need to
////        // wait for the SET button. Guarded with SelectedIndex < 0 so it's a
////        // no-op during startup/clearing (SelectedIndex = -1).
////        //private void RightMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        //{
////        //    if (RightMotorCombo.SelectedIndex < 0) return;

////        //    // Register 0x0002 = Right Hall Type (see BL.ParseAndUpdateUI)
////        //    BLLogic?.SendWriteCommand(0x0002, (ushort)RightMotorCombo.SelectedIndex);
////        //}
////        private void RightMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            if (RightMotorCombo.SelectedItem == null) return;

////            string selected = RightMotorCombo.SelectedItem.ToString(); // "BL 42 85 x 24"

////            if (selected.Length % 2 != 0) selected += "\0"; // pad if odd length

////            ushort[] regs = new ushort[selected.Length / 2];
////            for (int i = 0; i < regs.Length; i++)
////            {
////                byte hi = (byte)selected[i * 2];
////                byte lo = (byte)selected[i * 2 + 1];
////                regs[i] = (ushort)((hi << 8) | lo);
////            }

////            // 14 chars -> 7 registers
////            for (int i = 0; i < regs.Length; i++)
////            {
////                BLLogic?.SendWriteCommand((ushort)(0x0002 + i), regs[i]);
////            }// use the correct register address for Motor Type
////        }
////        private void RightBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            if (RightBrakeCombo.SelectedIndex < 0) return;

////            // Register 0x0046 = Right Brake Index (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex);
////        }

////        // ══════════════════ MOTOR TYPE ══════════════════
////        // SET button still works too (sends the same value again) — harmless,
////        // kept for anyone used to clicking it, and to show a confirmation popup.
////        private void SetRightMotorType_Click(object sender, RoutedEventArgs e)
////        {
////            if (RightMotorCombo.SelectedIndex < 0)
////            {
////                MessageBox.Show("Please select a motor type first.", "No Selection",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////           // var selected = RightMotorCombo.SelectedItem as MotorTypeItem;

////            // Register 0x0002 = Right Hall Type (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0002, (ushort)RightMotorCombo.SelectedIndex);
////            System.Diagnostics.Debug.WriteLine("Rightmotor_Motortype comment sent");
////            //MessageBox.Show($"Right motor type set to: {selected?.Name}, Gear: {selected?.Gear}",
////            //                 "Motor Type Set", MessageBoxButton.OK, MessageBoxImage.Information);
////        }
////        private void SetRightDirection_Click(object sender, RoutedEventArgs e)
////        {
////            if (RightDirectionCombo.SelectedIndex < 0)
////            {
////                MessageBox.Show("Please select a direction first.", "No Selection",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            System.Diagnostics.Debug.WriteLine("Leftmotor_Direction command sent");
////            // Register 0x000X = Left Direction (update to your actual register)
////            BLLogic?.SendWriteCommand(0x0070,(ushort)RightDirectionCombo.SelectedIndex);
////        }
////        // ══════════════════ BRAKE METHOD ══════════════════
////        private void SetRightBrake_Click(object sender, RoutedEventArgs e)
////        {
////            if (RightBrakeCombo.SelectedIndex < 0)
////            {
////                MessageBox.Show("Please select a brake method first.", "No Selection",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            string selectedBrake = RightBrakeCombo.SelectedItem as string;

////            // Register 0x0046 = Right Brake Index (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex);
////            System.Diagnostics.Debug.WriteLine("Rightmotor_Brake comment sent");
////            MessageBox.Show($"Right brake method set to: {selectedBrake}",
////                             "Brake Set", MessageBoxButton.OK, MessageBoxImage.Information);
////        }

////        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
////        private void SetRightSpeed_Click(object sender, RoutedEventArgs e)
////        {
////            if (!int.TryParse(RightSpeedTextBox.Text, out int speed))
////            {
////                MessageBox.Show("Enter a valid speed value.", "Invalid Input",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            // Register 0x003F = Right Speed (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x003F, (ushort)speed);
////            System.Diagnostics.Debug.WriteLine("Rightmotor_speed comment sent");
////        }

////        private void SetRightAccel_Click(object sender, RoutedEventArgs e)
////        {
////            if (!int.TryParse(RightAccelTextBox.Text, out int accel))
////            {
////                MessageBox.Show("Enter a valid acceleration value.", "Invalid Input",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            // Register 0x0042 = Right Acceleration (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0042, (ushort)accel);
////            System.Diagnostics.Debug.WriteLine("Rightmotor_accel comment sent");
////        }

////        // NOTE: your Right XAML wires the Decel button to "SetDecel_Click"
////        // (not "SetRightDecel_Click") — kept as-is so it matches the XAML.
////        private void SetDecel_Click(object sender, RoutedEventArgs e)
////        {

////            if (!int.TryParse(RightDecelTextBox.Text, out int decel))
////            {
////                MessageBox.Show("Enter a valid deceleration value.", "Invalid Input",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            // Register 0x0043 = Right Deceleration (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0043, (ushort)decel);
////            System.Diagnostics.Debug.WriteLine("Rightmotor_Decel comment sent");
////        }

////        // ══════════════════ RUN / STOP TOGGLE ══════════════════
////        private bool _isRunning = false;
////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)


////        {
////            _isRunning = !_isRunning;

////            // TODO: confirm the correct register for RIGHT motor run/stop.
////            // This is a placeholder — the left control uses 0x0030, but that
////            // was itself unverified. Replace with the real run/stop register
////            // for the right motor once you have it from the device spec.
////            BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));
////            if (_isRunning)
////            {
////                System.Diagnostics.Debug.WriteLine("Rightmotor_run comment sent");
////            }
////            else
////            {
////                System.Diagnostics.Debug.WriteLine("Rightmotor_stop comment sent");
////            }
////            //BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));
////            //BLLogic?.SendWriteCommand(0x0048, (ushort)(_isRunning ? 1 : 0));

////            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
////        }
////        // Called externally (e.g. from MotorControlPanel's global Start/Stop) to keep
////        // this control's button in sync without re-sending the write command.
////        public void UpdateRunState(bool running)
////        {
////            _isRunning = running;
////            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
////        }
////        public void UpdateMotorTelemetry(ModbusDevice device)
////        {
////            if (device == null) return;

////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
////            {
////                if (!RightSpeedTextBox.IsFocused || device.RightSpeed == "0")
////                {
////                    if (RightSpeedTextBox.Text != device.RightSpeed)
////                        RightSpeedTextBox.Text = device.RightSpeed;
////                }

////                if (!RightAccelTextBox.IsFocused || device.RightAcceleration == "0")
////                {
////                    if (RightAccelTextBox.Text != device.RightAcceleration)
////                        RightAccelTextBox.Text = device.RightAcceleration;
////                }

////                if (!RightDecelTextBox.IsFocused || device.RightDeceleration == "0")
////                {
////                    if (RightDecelTextBox.Text != device.RightDeceleration)
////                        RightDecelTextBox.Text = device.RightDeceleration;
////                }

////                if (RightCurrentTextBox.Text != device.RightCurrent)
////                    RightCurrentTextBox.Text = device.RightCurrent;

////                // NOTE: Motor Type / Brake Method combos are intentionally NOT
////                // updated from device telemetry here. They only change when the
////                // user picks a new item from the dropdown, and that selection
////                // stays put (unaffected by incoming device data) until SET is
////                // clicked to send it.

////                FlagHallSensor.Tag = device.RightHallSensorError;
////                FlagOverCurrent.Tag = device.RightOverCurrent;
////                FlagOverVoltage.Tag = device.RightOverVoltage;
////                FlagLowVoltage.Tag = device.RightLowVoltage;
////                Flagrightsensor1.Tag = device.RightSensor1;
////                Flagrightsensor2.Tag = device.RightSensor2;
////                // FlagMotorStalled.Tag = device.RightMotorStalled;
////                // FlagOverTemp.Tag = device.RightOverTemperature;
////            }));
////        }

////        // ══════════════════ MANUAL UI UPDATE ══════════════════
////        public void ClearUI()
////        {
////            RightSpeedTextBox.Text = string.Empty;
////            RightCurrentTextBox.Text = string.Empty;
////            RightAccelTextBox.Text = string.Empty;
////            RightDecelTextBox.Text = string.Empty;
////            RightBrakeCombo.SelectedIndex = -1;
////            RightMotorCombo.SelectedIndex = -1;
////            RightDirectionCombo.SelectedIndex = -1;
////            TripleStateButton.Content = "▶  RUN  ";
////            SetButtonsEnabled = false;
////        }

////    }
////}
//using BLDC_Demo.Controls;
//using BLDC_Demo.Models;
//using System;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Media;

//namespace BLDC_Demo
//{
//    public partial class RightMdrControl : UserControl
//    {
//        private static readonly string MotorTypesFile =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

//        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
//            = new ObservableCollection<MotorTypeItem>();

//        private static readonly string BrakeTypesFile =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

//        public ObservableCollection<string> BrakeTypes { get; set; }
//            = new ObservableCollection<string>();

//        // Gap between consecutive writes in a burst, so the ESP32 never
//        // receives several 12-byte frames in a single TCP chunk.
//        private const int WriteGapMs = 30;

//        public RightMdrControl()
//        {
//            InitializeComponent();

//            LoadMotorTypes();
//            RightMotorCombo.ItemsSource = MotorTypes;
//            RightMotorCombo.DisplayMemberPath = "Name";
//            if (MotorTypes.Count > 0)
//                RightMotorCombo.SelectedIndex = -1;
//            RightMotorCombo.SelectionChanged += RightMotorCombo_SelectionChanged;

//            LoadBrakeTypes();
//            RightBrakeCombo.ItemsSource = BrakeTypes;
//            if (BrakeTypes.Count > 0)
//                RightBrakeCombo.SelectedIndex = -1;
//            RightBrakeCombo.SelectionChanged += RightBrakeCombo_SelectionChanged;
//        }

//        private void LoadMotorTypes()
//        {
//            MotorTypes.Clear();

//            if (!File.Exists(MotorTypesFile))
//            {
//                File.WriteAllLines(MotorTypesFile, new[]
//                {
//                    "BL 42 85,24,",
//                    "BL 42 85,19,",
//                });
//            }

//            foreach (string line in File.ReadAllLines(MotorTypesFile))
//            {
//                if (string.IsNullOrWhiteSpace(line)) continue;

//                string[] parts = line.Split(',');
//                string name = parts[0].Trim();
//                string gear = parts.Length > 1 ? parts[1].Trim() : string.Empty;

//                if (!string.IsNullOrWhiteSpace(name))
//                    MotorTypes.Add(new MotorTypeItem(name, gear));
//            }
//        }

//        public void SaveMotorTypes()
//        {
//            var lines = MotorTypes.Select(m => $"{m.Name},{m.Gear},");
//            File.WriteAllLines(MotorTypesFile, lines);
//        }

//        public void SetConnected(bool connected)
//        {
//            // Only the SET buttons follow the connection state.
//            SetButtonsEnabled = connected;
//        }

//        public static readonly DependencyProperty SetButtonsEnabledProperty =
//            DependencyProperty.Register(
//                nameof(SetButtonsEnabled),
//                typeof(bool),
//                typeof(RightMdrControl),
//                new PropertyMetadata(false));

//        public bool SetButtonsEnabled
//        {
//            get => (bool)GetValue(SetButtonsEnabledProperty);
//            set => SetValue(SetButtonsEnabledProperty, value);
//        }

//        private void LoadBrakeTypes()
//        {
//            BrakeTypes.Clear();

//            if (!File.Exists(BrakeTypesFile))
//            {
//                File.WriteAllLines(BrakeTypesFile, new[]
//                {
//                    "Normal,,",
//                    "ServoBreak,,"
//                });
//            }

//            foreach (string line in File.ReadAllLines(BrakeTypesFile))
//            {
//                if (string.IsNullOrWhiteSpace(line)) continue;
//                string name = line.Split(',')[0].Trim();
//                if (!string.IsNullOrWhiteSpace(name))
//                    BrakeTypes.Add(name);
//            }
//        }

//        public void SaveBrakeTypes()
//        {
//            var lines = BrakeTypes.Select(name => $"{name},,");
//            File.WriteAllLines(BrakeTypesFile, lines);
//        }

//        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;

//        // Convenience accessor for the BL logic instance owned by MainWindow.
//        private BL BLLogic => OwnerWindow?.BLLogic;

//        // ══════════════════ MOTOR TYPE ══════════════════
//        // Sends the motor-type string as ASCII, 2 chars per register,
//        // starting at 0x0002. Writes are spaced by WriteGapMs.
//        private bool _sendingMotorType = false;

//        private async Task SendMotorTypeAsync()
//        {
//            var item = RightMotorCombo.SelectedItem as MotorTypeItem;
//            if (item == null || BLLogic == null || _sendingMotorType) return;

//            _sendingMotorType = true;
//            try
//            {
//                string selected = $"{item.Name} x {item.Gear}"; // e.g. "BL 42 85 x 24"
//                if (selected.Length % 2 != 0) selected += "\0"; // pad if odd length

//                int regCount = selected.Length / 2;
//                for (int i = 0; i < regCount; i++)
//                {
//                    byte hi = (byte)selected[i * 2];
//                    byte lo = (byte)selected[i * 2 + 1];
//                    ushort reg = (ushort)((hi << 8) | lo);

//                    if (!BLLogic.SendWriteCommand((ushort)(0x0002 + i), reg))
//                        return; // not connected, stop the burst

//                    await Task.Delay(WriteGapMs);
//                }
//            }
//            finally
//            {
//                _sendingMotorType = false;
//            }
//        }

//        // Fires the write the instant the user picks a new item.
//        private async void RightMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (RightMotorCombo.SelectedItem == null) return;
//            await SendMotorTypeAsync();
//        }

//        // SET button: sends the same motor-type string (previously it wrote the
//        // combo INDEX to 0x0002, which overwrote the first character register).
//        private async void SetRightMotorType_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightMotorCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a motor type first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            await SendMotorTypeAsync();
//            System.Diagnostics.Debug.WriteLine("Rightmotor_Motortype command sent");
//        }

//        // ══════════════════ BRAKE METHOD ══════════════════
//        private void RightBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (RightBrakeCombo.SelectedIndex < 0) return;

//            // Register 0x0046 = Right Brake Index
//            BLLogic?.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex);
//        }

//        private void SetRightBrake_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightBrakeCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a brake method first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            string selectedBrake = RightBrakeCombo.SelectedItem as string;

//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Brake command sent");
//            MessageBox.Show($"Right brake method set to: {selectedBrake}",
//                             "Brake Set", MessageBoxButton.OK, MessageBoxImage.Information);
//        }

//        // ══════════════════ DIRECTION ══════════════════
//        private void SetRightDirection_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightDirectionCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a direction first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (RightDirectionCombo.SelectedIndex == 0)
//            {
//                // Index 0 is "CLOCKWISE"
//                // Generates: 01 06 00 47 00 01 F8 1F
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//            }
//            else if (RightDirectionCombo.SelectedIndex == 1)
//            {
//                // Index 1 is "ANTI-CW"
//                // Generates: 01 06 00 47 00 00 39 DE
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//            }
//            // CHECK: this was 0x0070. The telemetry register for Right Direction
//            // is 0x0047 (see BL.ParseAndUpdateUI). Change back to 0x0070 only if
//            // the firmware spec says the write register differs from the read one.
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0047, (ushort)RightDirectionCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Direction command sent");
//        }

//        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
//        private void SetRightSpeed_Click(object sender, RoutedEventArgs e)
//        {
//            if (!int.TryParse(RightSpeedTextBox.Text, out int speed))
//            {
//                MessageBox.Show("Enter a valid speed value.", "Invalid Input",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x003F = Right Speed
//            BLLogic?.SendWriteCommand(0x003F, (ushort)speed);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_speed command sent");
//        }

//        private void SetRightAccel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!int.TryParse(RightAccelTextBox.Text, out int accel))
//            {
//                MessageBox.Show("Enter a valid acceleration value.", "Invalid Input",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x0042 = Right Acceleration
//            BLLogic?.SendWriteCommand(0x0042, (ushort)accel);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_accel command sent");
//        }

//        // NOTE: the Right XAML wires the Decel button to "SetDecel_Click"
//        // (not "SetRightDecel_Click") — kept as-is so it matches the XAML.
//        private void SetDecel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!int.TryParse(RightDecelTextBox.Text, out int decel))
//            {
//                MessageBox.Show("Enter a valid deceleration value.", "Invalid Input",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x0043 = Right Deceleration
//            BLLogic?.SendWriteCommand(0x0043, (ushort)decel);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_Decel command sent");
//        }

//        // ══════════════════ RUN / STOP TOGGLE ══════════════════
//        private bool _isRunning = false;

//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
//        {
//            bool newState = !_isRunning;

//            // Only change UI state if the write was actually queued.
//            // Register 0x0048 = Right run/stop (verify against the device spec).
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0048, (ushort)(newState ? 1 : 0)))
//                return;

//            _isRunning = newState;
//            System.Diagnostics.Debug.WriteLine(_isRunning
//                ? "Rightmotor_run command sent"
//                : "Rightmotor_stop command sent");

//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }
//        private static readonly Brush ErrorBrush = Brushes.Red;
//        private static readonly Brush OkBrush = Brushes.LimeGreen;

//        private static void SetFlag(TextBlock flag, object value)
//        {
//            flag.Tag = value;
//            flag.Foreground = Convert.ToInt32(value) != 0 ? ErrorBrush : OkBrush;
//        }
//        // Called externally (e.g. MotorControlPanel's global Start/Stop) to keep
//        // this control's button in sync without re-sending the write command.
//        public void UpdateRunState(bool running)
//        {
//            _isRunning = running;
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                if (!RightSpeedTextBox.IsFocused || device.RightSpeed == "0")
//                {
//                    if (RightSpeedTextBox.Text != device.RightSpeed)
//                        RightSpeedTextBox.Text = device.RightSpeed;
//                }

//                if (!RightAccelTextBox.IsFocused || device.RightAcceleration == "0")
//                {
//                    if (RightAccelTextBox.Text != device.RightAcceleration)
//                        RightAccelTextBox.Text = device.RightAcceleration;
//                }

//                if (!RightDecelTextBox.IsFocused || device.RightDeceleration == "0")
//                {
//                    if (RightDecelTextBox.Text != device.RightDeceleration)
//                        RightDecelTextBox.Text = device.RightDeceleration;
//                }

//                if (RightCurrentTextBox.Text != device.RightCurrent)
//                    RightCurrentTextBox.Text = device.RightCurrent;

//                // Motor Type / Brake combos are intentionally NOT updated from
//                // telemetry; they only change when the user picks an item.

//                FlagHallSensor.Tag = device.RightHallSensorError;
//                FlagOverCurrent.Tag = device.RightOverCurrent;
//                FlagOverVoltage.Tag = device.RightOverVoltage;
//                FlagLowVoltage.Tag = device.RightLowVoltage;
//                Flagrightsensor1.Tag = device.RightSensor1;
//                Flagrightsensor2.Tag = device.RightSensor2;
//            }));
//        }

//        // ══════════════════ MANUAL UI UPDATE ══════════════════
//        public void ClearUI()
//        {
//            FlagHallSensor.Tag = -1;
//            FlagOverCurrent.Tag = -1;
//            FlagOverVoltage.Tag = -1;
//            FlagLowVoltage.Tag = -1;
//            Flagrightsensor1.Tag = -1;
//            Flagrightsensor2.Tag = -1;
//            RightSpeedTextBox.Text = string.Empty;
//            RightCurrentTextBox.Text = string.Empty;
//            RightAccelTextBox.Text = string.Empty;
//            RightDecelTextBox.Text = string.Empty;
//            RightBrakeCombo.SelectedIndex = -1;
//            RightMotorCombo.SelectedIndex = -1;
//            RightDirectionCombo.SelectedIndex = -1;
//            TripleStateButton.Content = "▶  RUN  ";
//            SetButtonsEnabled = false;
//        }
//    }
//}
//using BLDC_Demo.Controls;
//using BLDC_Demo.Models;
//using System;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//namespace BLDC_Demo
//{
//    public partial class RightMdrControl : UserControl
//    {
//        private static readonly string MotorTypesFile =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

//        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
//            = new ObservableCollection<MotorTypeItem>();

//        private static readonly string BrakeTypesFile =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

//        public ObservableCollection<string> BrakeTypes { get; set; }
//            = new ObservableCollection<string>();

//        // ── Allowed input ranges ──
//        private const int SpeedMin = 0;
//        private const int SpeedMax = 100;
//        private const int AccelMin = 10;
//        private const int AccelMax = 10000;
//        private const int DecelMin = 10;
//        private const int DecelMax = 10000;

//        public RightMdrControl()
//        {
//            InitializeComponent();

//            LoadMotorTypes();
//            RightMotorCombo.ItemsSource = MotorTypes;
//            RightMotorCombo.DisplayMemberPath = "Name";
//            if (MotorTypes.Count > 0)
//                RightMotorCombo.SelectedIndex = -1;
//            RightMotorCombo.SelectionChanged += RightMotorCombo_SelectionChanged;

//            LoadBrakeTypes();
//            RightBrakeCombo.ItemsSource = BrakeTypes;
//            if (BrakeTypes.Count > 0)
//                RightBrakeCombo.SelectedIndex = -1;
//            RightBrakeCombo.SelectionChanged += RightBrakeCombo_SelectionChanged;
//        }

//        // ══════════════════ INPUT VALIDATION ══════════════════
//        private static readonly Regex DigitsOnly = new Regex("^[0-9]+$");

//        // Block any typed character that is not a digit.
//        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
//        {
//            e.Handled = !DigitsOnly.IsMatch(e.Text);
//        }

//        // The space key does not raise PreviewTextInput, so block it here.
//        private void NumericOnly_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.Key == Key.Space)
//                e.Handled = true;
//        }

//        // Block pasting anything that is not purely digits.
//        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
//        {
//            if (e.DataObject.GetDataPresent(typeof(string)))
//            {
//                string text = (string)e.DataObject.GetData(typeof(string));
//                if (!DigitsOnly.IsMatch(text))
//                    e.CancelCommand();
//            }
//            else
//            {
//                e.CancelCommand();
//            }
//        }

//        // Range check used by the SET buttons. Shows a message and returns false when invalid.
//        private bool TryGetInRange(TextBox box, int min, int max, string name, out int value)
//        {
//            if (!int.TryParse(box.Text, out value) || value < min || value > max)
//            {
//                MessageBox.Show($"{name} must be a number between {min} and {max}.",
//                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
//                box.Focus();
//                box.SelectAll();
//                return false;
//            }
//            return true;
//        }

//        private void LoadMotorTypes()
//        {
//            MotorTypes.Clear();

//            if (!File.Exists(MotorTypesFile))
//            {
//                File.WriteAllLines(MotorTypesFile, new[]
//                {
//                    "BL 42 85 x 24,",
//                    "BL 42 85 x 19,",
//                    "BL 42 85 x 15,",
//                    //"BL 42 85 x 4.9,",
//                    //"BL 42 93 x 19,",
//                    //"BL 42 93 x 27,",
//                    //"BL 42 93 x 49,",
//                    //"BL 42 93 x 82,",
//                    //"BL 57 93 x 23,",
//                    //"BL 57 93 x 96,",
//                    //"BL 57 93 x 43,"
//                });
//            }

//            foreach (string line in File.ReadAllLines(MotorTypesFile))
//            {
//                if (string.IsNullOrWhiteSpace(line)) continue;

//                string[] parts = line.Split(',');
//                string name = parts[0].Trim();
//                string gear = parts.Length > 1 ? parts[1].Trim() : string.Empty;

//                if (!string.IsNullOrWhiteSpace(name))
//                    MotorTypes.Add(new MotorTypeItem(name, gear));
//            }
//        }

//        public void SaveMotorTypes()
//        {
//            var lines = MotorTypes.Select(m => $"{m.Name},{m.Gear},");
//            File.WriteAllLines(MotorTypesFile, lines);
//        }

//        public void SetConnected(bool connected)
//        {
//            // Only the SET buttons follow the connection state.
//            SetButtonsEnabled = connected;
//        }

//        public static readonly DependencyProperty SetButtonsEnabledProperty =
//            DependencyProperty.Register(
//                nameof(SetButtonsEnabled),
//                typeof(bool),
//                typeof(RightMdrControl),
//                new PropertyMetadata(false));

//        public bool SetButtonsEnabled
//        {
//            get => (bool)GetValue(SetButtonsEnabledProperty);
//            set => SetValue(SetButtonsEnabledProperty, value);
//        }

//        private void LoadBrakeTypes()
//        {
//            BrakeTypes.Clear();

//            if (!File.Exists(BrakeTypesFile))
//            {
//                File.WriteAllLines(BrakeTypesFile, new[]
//                {
//                    "Normal,,",
//                    "ServoBreak,,"
//                });
//            }

//            foreach (string line in File.ReadAllLines(BrakeTypesFile))
//            {
//                if (string.IsNullOrWhiteSpace(line)) continue;
//                string name = line.Split(',')[0].Trim();
//                if (!string.IsNullOrWhiteSpace(name))
//                    BrakeTypes.Add(name);
//            }
//        }

//        public void SaveBrakeTypes()
//        {
//            var lines = BrakeTypes.Select(name => $"{name},,");
//            File.WriteAllLines(BrakeTypesFile, lines);
//        }

//        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;

//        // Convenience accessor for the BL logic instance owned by MainWindow.
//        private BL BLLogic => OwnerWindow?.BLLogic;

//        // ══════════════════ MOTOR TYPE ══════════════════
//        // Sends the number after the 'x' as a plain integer to register 0x0002.
//        // "BL 42 85 x 24" -> 24   (01 06 00 02 00 18 CRC CRC)
//        private void RightMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (!(RightMotorCombo.SelectedItem is MotorTypeItem item)) return;

//            int xPos = item.Name.LastIndexOf('x');
//            if (xPos < 0) return;
//            string ratioText = item.Name.Substring(xPos + 1).Trim();

//            if (!ushort.TryParse(ratioText, out ushort ratio)) return;

//            BLLogic?.SendWriteCommand(0x0002, ratio);
//        }

//        private void SetRightMotorType_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightMotorCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a motor type first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Motortype command sent");
//            // The value is already sent by SelectionChanged. Nothing is written here,
//            // so the ratio in 0x0002 is not overwritten by the combo index.
//        }

//        // ══════════════════ BRAKE METHOD ══════════════════
//        private void RightBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (RightBrakeCombo.SelectedIndex < 0) return;

//            // Register 0x0046 = Right Brake Index
//            BLLogic?.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex);
//        }

//        private void SetRightBrake_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightBrakeCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a brake method first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Brake command sent");
//        }

//        // ══════════════════ DIRECTION ══════════════════
//        private void SetRightDirection_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightDirectionCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a direction first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (RightDirectionCombo.SelectedIndex == 0)
//            {
//                // Index 0 is "CLOCKWISE"
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//            }
//            else if (RightDirectionCombo.SelectedIndex == 1)
//            {
//                // Index 1 is "ANTI-CW"
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//            }
//            // CHECK: this was 0x0070. The telemetry register for Right Direction
//            // is 0x0047 (see BL.ParseAndUpdateUI). Change back to 0x0070 only if
//            // the firmware spec says the write register differs from the read one.
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0047, (ushort)RightDirectionCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Direction command sent");
//        }

//        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
//        private void SetRightSpeed_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(RightSpeedTextBox, SpeedMin, SpeedMax, "Speed (%)", out int speed))
//                return;

//            // Register 0x003F = Right Speed
//            BLLogic?.SendWriteCommand(0x003F, (ushort)speed);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_speed command sent");
//        }

//        private void SetRightAccel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(RightAccelTextBox, AccelMin, AccelMax, "Acceleration (ms)", out int accel))
//                return;

//            // Register 0x0042 = Right Acceleration
//            BLLogic?.SendWriteCommand(0x0042, (ushort)accel);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_accel command sent");
//        }

//        // NOTE: the Right XAML wires the Decel button to "SetDecel_Click"
//        // (not "SetRightDecel_Click") — kept as-is so it matches the XAML.
//        private void SetDecel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(RightDecelTextBox, DecelMin, DecelMax, "Deceleration (ms)", out int decel))
//                return;

//            // Register 0x0043 = Right Deceleration
//            BLLogic?.SendWriteCommand(0x0043, (ushort)decel);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_Decel command sent");
//        }

//        // ══════════════════ RUN / STOP TOGGLE ══════════════════
//        private bool _isRunning = false;

//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
//        {
//            bool newState = !_isRunning;

//            // Only change UI state if the write was actually queued.
//            // Register 0x0048 = Right run/stop (verify against the device spec).
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0048, (ushort)(newState ? 1 : 0)))
//                return;

//            _isRunning = newState;
//            System.Diagnostics.Debug.WriteLine(_isRunning
//                ? "Rightmotor_run command sent"
//                : "Rightmotor_stop command sent");

//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        private static readonly Brush ErrorBrush = Brushes.Red;
//        private static readonly Brush OkBrush = Brushes.LimeGreen;

//        private static void SetFlag(TextBlock flag, object value)
//        {
//            flag.Tag = value;
//            flag.Foreground = Convert.ToInt32(value) != 0 ? ErrorBrush : OkBrush;
//        }

//        // Called externally (e.g. MotorControlPanel's global Start/Stop) to keep
//        // this control's button in sync without re-sending the write command.
//        public void UpdateRunState(bool running)
//        {
//            _isRunning = running;
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                if (!RightSpeedTextBox.IsFocused || device.RightSpeed == "0")
//                {
//                    if (RightSpeedTextBox.Text != device.RightSpeed)
//                        RightSpeedTextBox.Text = device.RightSpeed;
//                }

//                if (!RightAccelTextBox.IsFocused || device.RightAcceleration == "0")
//                {
//                    if (RightAccelTextBox.Text != device.RightAcceleration)
//                        RightAccelTextBox.Text = device.RightAcceleration;
//                }

//                if (!RightDecelTextBox.IsFocused || device.RightDeceleration == "0")
//                {
//                    if (RightDecelTextBox.Text != device.RightDeceleration)
//                        RightDecelTextBox.Text = device.RightDeceleration;
//                }

//                if (RightCurrentTextBox.Text != device.RightCurrent)
//                    RightCurrentTextBox.Text = device.RightCurrent;

//                // Motor Type / Brake combos are intentionally NOT updated from
//                // telemetry; they only change when the user picks an item.

//                FlagHallSensor.Tag = device.RightHallSensorError;
//                FlagOverCurrent.Tag = device.RightOverCurrent;
//                FlagOverVoltage.Tag = device.RightOverVoltage;
//                FlagLowVoltage.Tag = device.RightLowVoltage;
//                Flagrightsensor1.Tag = device.RightSensor1;
//                Flagrightsensor2.Tag = device.RightSensor2;
//            }));
//        }

//        // ══════════════════ MANUAL UI UPDATE ══════════════════
//        public void ClearUI()
//        {
//            FlagHallSensor.Tag = -1;
//            FlagOverCurrent.Tag = -1;
//            FlagOverVoltage.Tag = -1;
//            FlagLowVoltage.Tag = -1;
//            Flagrightsensor1.Tag = -1;
//            Flagrightsensor2.Tag = -1;
//            RightSpeedTextBox.Text = string.Empty;
//            RightCurrentTextBox.Text = string.Empty;
//            RightAccelTextBox.Text = string.Empty;
//            RightDecelTextBox.Text = string.Empty;
//            RightBrakeCombo.SelectedIndex = -1;
//            RightMotorCombo.SelectedIndex = -1;
//            RightDirectionCombo.SelectedIndex = -1;
//            TripleStateButton.Content = "▶  RUN  ";
//            SetButtonsEnabled = false;
//        }
//    }
//}
//using BLDC_Demo.Controls;
//using BLDC_Demo.Models;
//using System;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;

//namespace BLDC_Demo
//{
//    public partial class RightMdrControl : UserControl
//    {
//        private static readonly string MotorTypesFile =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

//        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
//            = new ObservableCollection<MotorTypeItem>();

//        private static readonly string BrakeTypesFile =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

//        public ObservableCollection<string> BrakeTypes { get; set; }
//            = new ObservableCollection<string>();

//        // ── Allowed input ranges ──
//        private const int SpeedMin = 0;
//        private const int SpeedMax = 100;
//        private const int AccelMin = 10;
//        private const int AccelMax = 10000;
//        private const int DecelMin = 10;
//        private const int DecelMax = 10000;

//        public RightMdrControl()
//        {
//            InitializeComponent();

//            LoadMotorTypes();
//            RightMotorCombo.ItemsSource = MotorTypes;
//            RightMotorCombo.DisplayMemberPath = "Name";
//            if (MotorTypes.Count > 0)
//                RightMotorCombo.SelectedIndex = -1;
//            RightMotorCombo.SelectionChanged += RightMotorCombo_SelectionChanged;

//            LoadBrakeTypes();
//            RightBrakeCombo.ItemsSource = BrakeTypes;
//            if (BrakeTypes.Count > 0)
//                RightBrakeCombo.SelectedIndex = -1;
//            RightBrakeCombo.SelectionChanged += RightBrakeCombo_SelectionChanged;
//        }

//        // ══════════════════ INPUT VALIDATION ══════════════════
//        private static readonly Regex DigitsOnly = new Regex("^[0-9]+$");

//        // Block any typed character that is not a digit.
//        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
//        {
//            e.Handled = !DigitsOnly.IsMatch(e.Text);
//        }

//        // The space key does not raise PreviewTextInput, so block it here.
//        private void NumericOnly_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.Key == Key.Space)
//                e.Handled = true;
//        }

//        // Block pasting anything that is not purely digits.
//        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
//        {
//            if (e.DataObject.GetDataPresent(typeof(string)))
//            {
//                string text = (string)e.DataObject.GetData(typeof(string));
//                if (!DigitsOnly.IsMatch(text))
//                    e.CancelCommand();
//            }
//            else
//            {
//                e.CancelCommand();
//            }
//        }

//        // Range check used by the SET buttons. Shows a message and returns false when invalid.
//        private bool TryGetInRange(TextBox box, int min, int max, string name, out int value)
//        {
//            if (!int.TryParse(box.Text, out value) || value < min || value > max)
//            {
//                MessageBox.Show($"{name} must be a number between {min} and {max}.",
//                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
//                box.Focus();
//                box.SelectAll();
//                return false;
//            }
//            return true;
//        }

//        private void LoadMotorTypes()
//        {
//            MotorTypes.Clear();

//            if (!File.Exists(MotorTypesFile))
//            {
//                File.WriteAllLines(MotorTypesFile, new[]
//                {
//                    "BL 42 85 x 24,",
//                    "BL 42 85 x 19,",
//                    "BL 42 85 x 15,",
//                    //"BL 42 85 x 4.9,",
//                    //"BL 42 93 x 19,",
//                    //"BL 42 93 x 27,",
//                    //"BL 42 93 x 49,",
//                    //"BL 42 93 x 82,",
//                    //"BL 57 93 x 23,",
//                    //"BL 57 93 x 96,",
//                    //"BL 57 93 x 43,"
//                });
//            }

//            foreach (string line in File.ReadAllLines(MotorTypesFile))
//            {
//                if (string.IsNullOrWhiteSpace(line)) continue;

//                string[] parts = line.Split(',');
//                string name = parts[0].Trim();
//                string gear = parts.Length > 1 ? parts[1].Trim() : string.Empty;

//                if (!string.IsNullOrWhiteSpace(name))
//                    MotorTypes.Add(new MotorTypeItem(name, gear));
//            }
//        }

//        public void SaveMotorTypes()
//        {
//            var lines = MotorTypes.Select(m => $"{m.Name},{m.Gear},");
//            File.WriteAllLines(MotorTypesFile, lines);
//        }

//        public void SetConnected(bool connected)
//        {
//            // Only the SET buttons follow the connection state.
//            SetButtonsEnabled = connected;
//        }

//        public static readonly DependencyProperty SetButtonsEnabledProperty =
//            DependencyProperty.Register(
//                nameof(SetButtonsEnabled),
//                typeof(bool),
//                typeof(RightMdrControl),
//                new PropertyMetadata(false));

//        public bool SetButtonsEnabled
//        {
//            get => (bool)GetValue(SetButtonsEnabledProperty);
//            set => SetValue(SetButtonsEnabledProperty, value);
//        }

//        private void LoadBrakeTypes()
//        {
//            BrakeTypes.Clear();

//            if (!File.Exists(BrakeTypesFile))
//            {
//                File.WriteAllLines(BrakeTypesFile, new[]
//                {
//                    "Normal,,",
//                    "ServoBreak,,"
//                });
//            }

//            foreach (string line in File.ReadAllLines(BrakeTypesFile))
//            {
//                if (string.IsNullOrWhiteSpace(line)) continue;
//                string name = line.Split(',')[0].Trim();
//                if (!string.IsNullOrWhiteSpace(name))
//                    BrakeTypes.Add(name);
//            }
//        }

//        public void SaveBrakeTypes()
//        {
//            var lines = BrakeTypes.Select(name => $"{name},,");
//            File.WriteAllLines(BrakeTypesFile, lines);
//        }

//        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;

//        // Convenience accessor for the BL logic instance owned by MainWindow.
//        private BL BLLogic => OwnerWindow?.BLLogic;

//        // ══════════════════ MOTOR TYPE ══════════════════
//        // Sends the number after the 'x' as a plain integer to register 0x0002.
//        // "BL 42 85 x 24" -> 24   (01 06 00 02 00 18 CRC CRC)
//        private void RightMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (!(RightMotorCombo.SelectedItem is MotorTypeItem item)) return;

//            int xPos = item.Name.LastIndexOf('x');
//            if (xPos < 0) return;
//            string ratioText = item.Name.Substring(xPos + 1).Trim();

//            if (!ushort.TryParse(ratioText, out ushort ratio)) return;

//            BLLogic?.SendWriteCommand(0x0002, ratio);
//        }

//        private void SetRightMotorType_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightMotorCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a motor type first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Motortype command sent");
//            // The value is already sent by SelectionChanged. Nothing is written here,
//            // so the ratio in 0x0002 is not overwritten by the combo index.
//        }

//        // ══════════════════ BRAKE METHOD ══════════════════
//        private void RightBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (RightBrakeCombo.SelectedIndex < 0) return;

//            // Register 0x0046 = Right Brake Index
//            BLLogic?.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex);
//        }

//        private void SetRightBrake_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightBrakeCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a brake method first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Brake command sent");
//        }

//        // ══════════════════ DIRECTION ══════════════════
//        private void SetRightDirection_Click(object sender, RoutedEventArgs e)
//        {
//            if (RightDirectionCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a direction first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (RightDirectionCombo.SelectedIndex == 0)
//            {
//                // Index 0 is "CLOCKWISE"
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//            }
//            else if (RightDirectionCombo.SelectedIndex == 1)
//            {
//                // Index 1 is "ANTI-CW"
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//            }
//            // CHECK: this was 0x0070. The telemetry register for Right Direction
//            // is 0x0047 (see BL.ParseAndUpdateUI). Change back to 0x0070 only if
//            // the firmware spec says the write register differs from the read one.
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0047, (ushort)RightDirectionCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Rightmotor_Direction command sent");
//        }

//        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
//        private void SetRightSpeed_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(RightSpeedTextBox, SpeedMin, SpeedMax, "Speed (%)", out int speed))
//                return;

//            // Register 0x003F = Right Speed
//            BLLogic?.SendWriteCommand(0x003F, (ushort)speed);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_speed command sent");
//        }

//        private void SetRightAccel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(RightAccelTextBox, AccelMin, AccelMax, "Acceleration (ms)", out int accel))
//                return;

//            // Register 0x0042 = Right Acceleration
//            BLLogic?.SendWriteCommand(0x0042, (ushort)accel);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_accel command sent");
//        }

//        // NOTE: the Right XAML wires the Decel button to "SetDecel_Click"
//        // (not "SetRightDecel_Click") — kept as-is so it matches the XAML.
//        private void SetDecel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(RightDecelTextBox, DecelMin, DecelMax, "Deceleration (ms)", out int decel))
//                return;

//            // Register 0x0043 = Right Deceleration
//            BLLogic?.SendWriteCommand(0x0043, (ushort)decel);
//            System.Diagnostics.Debug.WriteLine("Rightmotor_Decel command sent");
//        }

//        // ══════════════════ RUN / STOP TOGGLE ══════════════════
//        private bool _isRunning = false;

//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
//        {
//            bool newState = !_isRunning;

//            // Only change UI state if the write was actually queued.
//            // Register 0x0048 = Right run/stop (verify against the device spec).
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0048, (ushort)(newState ? 1 : 0)))
//                return;

//            _isRunning = newState;
//            System.Diagnostics.Debug.WriteLine(_isRunning
//                ? "Rightmotor_run command sent"
//                : "Rightmotor_stop command sent");

//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        // Called externally (e.g. MotorControlPanel's global Start/Stop) to keep
//        // this control's button in sync without re-sending the write command.
//        public void UpdateRunState(bool running)
//        {
//            _isRunning = running;
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                // FIXED: do not overwrite a box the user is typing in
//                // (the old "|| value == "0"" check erased typing while the motor was stopped).
//                if (!RightSpeedTextBox.IsFocused)
//                {
//                    if (RightSpeedTextBox.Text != device.RightSpeed)
//                        RightSpeedTextBox.Text = device.RightSpeed;
//                }

//                if (!RightAccelTextBox.IsFocused)
//                {
//                    if (RightAccelTextBox.Text != device.RightAcceleration)
//                        RightAccelTextBox.Text = device.RightAcceleration;
//                }

//                if (!RightDecelTextBox.IsFocused)
//                {
//                    if (RightDecelTextBox.Text != device.RightDeceleration)
//                        RightDecelTextBox.Text = device.RightDeceleration;
//                }

//                if (RightCurrentTextBox.Text != device.RightCurrent)
//                    RightCurrentTextBox.Text = device.RightCurrent;

//                // Motor Type / Brake combos are intentionally NOT updated from
//                // telemetry; they only change when the user picks an item.

//                FlagHallSensor.Tag = device.RightHallSensorError;
//                FlagOverCurrent.Tag = device.RightOverCurrent;
//                FlagOverVoltage.Tag = device.RightOverVoltage;
//                FlagLowVoltage.Tag = device.RightLowVoltage;
//                Flagrightsensor1.Tag = device.RightSensor1;
//                Flagrightsensor2.Tag = device.RightSensor2;
//            }));
//        }

//        // ══════════════════ MANUAL UI UPDATE ══════════════════
//        public void ClearUI()
//        {
//            FlagHallSensor.Tag = -1;
//            FlagOverCurrent.Tag = -1;
//            FlagOverVoltage.Tag = -1;
//            FlagLowVoltage.Tag = -1;
//            Flagrightsensor1.Tag = -1;
//            Flagrightsensor2.Tag = -1;
//            RightSpeedTextBox.Text = string.Empty;
//            RightCurrentTextBox.Text = string.Empty;
//            RightAccelTextBox.Text = string.Empty;
//            RightDecelTextBox.Text = string.Empty;
//            RightBrakeCombo.SelectedIndex = -1;
//            RightMotorCombo.SelectedIndex = -1;
//            RightDirectionCombo.SelectedIndex = -1;
//            TripleStateButton.Content = "▶  RUN  ";
//            _isRunning = false;   // FIXED: keep the toggle state in sync with the button text
//            SetButtonsEnabled = false;
//        }
//    }
//}
using BLDC_Demo.Controls;
using BLDC_Demo.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BLDC_Demo
{
    public partial class RightMdrControl : UserControl
    {
        private static readonly string MotorTypesFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
            = new ObservableCollection<MotorTypeItem>();

        private static readonly string BrakeTypesFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

        public ObservableCollection<string> BrakeTypes { get; set; }
            = new ObservableCollection<string>();

        // ── Allowed input ranges ──
        private const int SpeedMin = 0;
        private const int SpeedMax = 100;
        private const int AccelMin = 10;
        private const int AccelMax = 10000;
        private const int DecelMin = 10;
        private const int DecelMax = 10000;

        public RightMdrControl()
        {
            InitializeComponent();

            LoadMotorTypes();
            RightMotorCombo.ItemsSource = MotorTypes;
            RightMotorCombo.DisplayMemberPath = "Name";
            if (MotorTypes.Count > 0)
                RightMotorCombo.SelectedIndex = -1;
            RightMotorCombo.SelectionChanged += RightMotorCombo_SelectionChanged;

            LoadBrakeTypes();
            RightBrakeCombo.ItemsSource = BrakeTypes;
            if (BrakeTypes.Count > 0)
                RightBrakeCombo.SelectedIndex = -1;
            RightBrakeCombo.SelectionChanged += RightBrakeCombo_SelectionChanged;

            // ── Range limits for the number boxes ──
            AttachRange(RightSpeedTextBox, SpeedMin, SpeedMax);
            AttachRange(RightAccelTextBox, AccelMin, AccelMax);
            AttachRange(RightDecelTextBox, DecelMin, DecelMax);
        }

        // ══════════════════ INPUT VALIDATION ══════════════════
        private static readonly Regex DigitsOnly = new Regex("^[0-9]+$");

        // Block any typed character that is not a digit.
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsOnly.IsMatch(e.Text);
        }

        // The space key does not raise PreviewTextInput, so block it here.
        private void NumericOnly_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        // Block pasting anything that is not purely digits.
        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!DigitsOnly.IsMatch(text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Limits a box to a number range.
        // - Only digits are allowed (uses the NumericOnly handlers above).
        // - While typing: a number bigger than max is blocked.
        // - When the user leaves the box: a number smaller than min is raised to min.
        private void AttachRange(TextBox box, int min, int max)
        {
            box.MaxLength = max.ToString().Length;

            box.PreviewTextInput += NumericOnly_PreviewTextInput;
            box.PreviewKeyDown += NumericOnly_PreviewKeyDown;
            DataObject.AddPastingHandler(box, NumericOnly_Pasting);

            // Block a value that would be bigger than max
            box.PreviewTextInput += (s, e) =>
            {
                string newText = box.Text
                    .Remove(box.SelectionStart, box.SelectionLength)
                    .Insert(box.SelectionStart, e.Text);

                if (int.TryParse(newText, out int v) && v > max)
                    e.Handled = true;
            };

            // Fix the value when the user leaves the box
            box.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(box.Text)) return;

                if (int.TryParse(box.Text, out int v))
                {
                    if (v < min) box.Text = min.ToString();
                    else if (v > max) box.Text = max.ToString();
                }
            };
        }

        // Range check used by the SET buttons. Shows a message and returns false when invalid.
        private bool TryGetInRange(TextBox box, int min, int max, string name, out int value)
        {
            if (!int.TryParse(box.Text, out value) || value < min || value > max)
            {
                MessageBox.Show($"{name} must be a number between {min} and {max}.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                box.Focus();
                box.SelectAll();
                return false;
            }
            return true;
        }

        private void LoadMotorTypes()
        {
            MotorTypes.Clear();

            if (!File.Exists(MotorTypesFile))
            {
                File.WriteAllLines(MotorTypesFile, new[]
                {
                    "BL 42 85 x 24,",
                    "BL 42 85 x 19,",
                    "BL 42 85 x 15,",
                    //"BL 42 85 x 4.9,",
                    //"BL 42 93 x 19,",
                    //"BL 42 93 x 27,",
                    //"BL 42 93 x 49,",
                    //"BL 42 93 x 82,",
                    //"BL 57 93 x 23,",
                    //"BL 57 93 x 96,",
                    //"BL 57 93 x 43,"
                });
            }

            foreach (string line in File.ReadAllLines(MotorTypesFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(',');
                string name = parts[0].Trim();
                string gear = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                if (!string.IsNullOrWhiteSpace(name))
                    MotorTypes.Add(new MotorTypeItem(name, gear));
            }
        }

        public void SaveMotorTypes()
        {
            var lines = MotorTypes.Select(m => $"{m.Name},{m.Gear},");
            File.WriteAllLines(MotorTypesFile, lines);
        }

        public void SetConnected(bool connected)
        {
            // Only the SET buttons follow the connection state.
            SetButtonsEnabled = connected;
        }

        public static readonly DependencyProperty SetButtonsEnabledProperty =
            DependencyProperty.Register(
                nameof(SetButtonsEnabled),
                typeof(bool),
                typeof(RightMdrControl),
                new PropertyMetadata(false));

        public bool SetButtonsEnabled
        {
            get => (bool)GetValue(SetButtonsEnabledProperty);
            set => SetValue(SetButtonsEnabledProperty, value);
        }

        private void LoadBrakeTypes()
        {
            BrakeTypes.Clear();

            if (!File.Exists(BrakeTypesFile))
            {
                File.WriteAllLines(BrakeTypesFile, new[]
                {
                    "Normal,,",
                    "ServoBreak,,"
                });
            }

            foreach (string line in File.ReadAllLines(BrakeTypesFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string name = line.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    BrakeTypes.Add(name);
            }
        }

        public void SaveBrakeTypes()
        {
            var lines = BrakeTypes.Select(name => $"{name},,");
            File.WriteAllLines(BrakeTypesFile, lines);
        }

        private MainWindow OwnerWindow => Window.GetWindow(this) as MainWindow;

        // Convenience accessor for the BL logic instance owned by MainWindow.
        private BL BLLogic => OwnerWindow?.BLLogic;

        // ══════════════════ MOTOR TYPE ══════════════════
        // Sends the number after the 'x' as a plain integer to register 0x0002.
        // "BL 42 85 x 24" -> 24   (01 06 00 02 00 18 CRC CRC)
        private void RightMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(RightMotorCombo.SelectedItem is MotorTypeItem item)) return;

            int xPos = item.Name.LastIndexOf('x');
            if (xPos < 0) return;
            string ratioText = item.Name.Substring(xPos + 1).Trim();

            if (!ushort.TryParse(ratioText, out ushort ratio)) return;

            BLLogic?.SendWriteCommand(0x0002, ratio);
        }

        private void SetRightMotorType_Click(object sender, RoutedEventArgs e)
        {
            if (RightMotorCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a motor type first.", "No Selection",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine("Rightmotor_Motortype command sent");
            // The value is already sent by SelectionChanged. Nothing is written here,
            // so the ratio in 0x0002 is not overwritten by the combo index.
        }

        // ══════════════════ BRAKE METHOD ══════════════════
        private void RightBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RightBrakeCombo.SelectedIndex < 0) return;

            // Register 0x0046 = Right Brake Index
            BLLogic?.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex);
        }

        private void SetRightBrake_Click(object sender, RoutedEventArgs e)
        {
            if (RightBrakeCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a brake method first.", "No Selection",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0046, (ushort)RightBrakeCombo.SelectedIndex))
                return;

            System.Diagnostics.Debug.WriteLine("Rightmotor_Brake command sent");
        }

        // ══════════════════ DIRECTION ══════════════════
        private void SetRightDirection_Click(object sender, RoutedEventArgs e)
        {
            if (RightDirectionCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a direction first.", "No Selection",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (RightDirectionCombo.SelectedIndex == 0)
            {
                // Index 0 is "CLOCKWISE"
                BLLogic.SendWriteCommand(0x0070, 1);
                BLLogic.SendWriteCommand(0x0070, 1);
                BLLogic.SendWriteCommand(0x0070, 1);
            }
            else if (RightDirectionCombo.SelectedIndex == 1)
            {
                // Index 1 is "ANTI-CW"
                BLLogic.SendWriteCommand(0x0070, 0);
                BLLogic.SendWriteCommand(0x0070, 0);
                BLLogic.SendWriteCommand(0x0070, 0);
            }
            // CHECK: this was 0x0070. The telemetry register for Right Direction
            // is 0x0047 (see BL.ParseAndUpdateUI). Change back to 0x0070 only if
            // the firmware spec says the write register differs from the read one.
            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0070, (ushort)RightDirectionCombo.SelectedIndex))
                return;

            System.Diagnostics.Debug.WriteLine("Rightmotor_Direction command sent");
        }

        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
        private void SetRightSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInRange(RightSpeedTextBox, SpeedMin, SpeedMax, "Speed (%)", out int speed))
                return;

            // Register 0x003F = Right Speed
            BLLogic?.SendWriteCommand(0x003F, (ushort)speed);
            System.Diagnostics.Debug.WriteLine("Rightmotor_speed command sent");
        }

        private void SetRightAccel_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInRange(RightAccelTextBox, AccelMin, AccelMax, "Acceleration (ms)", out int accel))
                return;

            // Register 0x0042 = Right Acceleration
            BLLogic?.SendWriteCommand(0x0042, (ushort)accel);
            System.Diagnostics.Debug.WriteLine("Rightmotor_accel command sent");
        }

        // NOTE: the Right XAML wires the Decel button to "SetDecel_Click"
        // (not "SetRightDecel_Click") — kept as-is so it matches the XAML.
        private void SetDecel_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInRange(RightDecelTextBox, DecelMin, DecelMax, "Deceleration (ms)", out int decel))
                return;

            // Register 0x0043 = Right Deceleration
            BLLogic?.SendWriteCommand(0x0043, (ushort)decel);
            System.Diagnostics.Debug.WriteLine("Rightmotor_Decel command sent");
        }

        // ══════════════════ RUN / STOP TOGGLE ══════════════════
        private bool _isRunning = false;

        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
        {
            bool newState = !_isRunning;

            // Only change UI state if the write was actually queued.
            // Register 0x0048 = Right run/stop (verify against the device spec).
            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0048, (ushort)(newState ? 1 : 0)))
                return;

            _isRunning = newState;
            System.Diagnostics.Debug.WriteLine(_isRunning
                ? "Rightmotor_run command sent"
                : "Rightmotor_stop command sent");

            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
        }

        // Called externally (e.g. MotorControlPanel's global Start/Stop) to keep
        // this control's button in sync without re-sending the write command.
        public void UpdateRunState(bool running)
        {
            _isRunning = running;
            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
        }

        public void UpdateMotorTelemetry(ModbusDevice device)
        {
            if (device == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // FIXED: do not overwrite a box the user is typing in
                // (the old "|| value == "0"" check erased typing while the motor was stopped).
                if (!RightSpeedTextBox.IsFocused)
                {
                    if (RightSpeedTextBox.Text != device.RightSpeed)
                        RightSpeedTextBox.Text = device.RightSpeed;
                }

                if (!RightAccelTextBox.IsFocused)
                {
                    if (RightAccelTextBox.Text != device.RightAcceleration)
                        RightAccelTextBox.Text = device.RightAcceleration;
                }

                if (!RightDecelTextBox.IsFocused)
                {
                    if (RightDecelTextBox.Text != device.RightDeceleration)
                        RightDecelTextBox.Text = device.RightDeceleration;
                }

                if (RightCurrentTextBox.Text != device.RightCurrent)
                    RightCurrentTextBox.Text = device.RightCurrent;

                // Motor Type / Brake combos are intentionally NOT updated from
                // telemetry; they only change when the user picks an item.

                FlagHallSensor.Tag = device.RightHallSensorError;
                FlagOverCurrent.Tag = device.RightOverCurrent;
                FlagOverVoltage.Tag = device.RightOverVoltage;
                FlagLowVoltage.Tag = device.RightLowVoltage;
                Flagrightsensor1.Tag = device.RightSensor1;
                Flagrightsensor2.Tag = device.RightSensor2;
            }));
        }

        // ══════════════════ MANUAL UI UPDATE ══════════════════
        public void ClearUI()
        {
            FlagHallSensor.Tag = -1;
            FlagOverCurrent.Tag = -1;
            FlagOverVoltage.Tag = -1;
            FlagLowVoltage.Tag = -1;
            Flagrightsensor1.Tag = -1;
            Flagrightsensor2.Tag = -1;
            RightSpeedTextBox.Text = string.Empty;
            RightCurrentTextBox.Text = string.Empty;
            RightAccelTextBox.Text = string.Empty;
            RightDecelTextBox.Text = string.Empty;
            RightBrakeCombo.SelectedIndex = -1;
            RightMotorCombo.SelectedIndex = -1;
            RightDirectionCombo.SelectedIndex = -1;
            TripleStateButton.Content = "▶  RUN  ";
            _isRunning = false;   // FIXED: keep the toggle state in sync with the button text
            SetButtonsEnabled = false;
        }
    }
}