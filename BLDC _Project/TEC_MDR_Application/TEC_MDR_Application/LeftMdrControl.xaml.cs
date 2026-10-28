
////using BLDC_Demo.Controls;
////using BLDC_Demo.Models;
////using System;
////using System.Collections.ObjectModel;
////using System.IO;
////using System.Linq;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Shapes;

////namespace BLDC_Demo
////{
////    public partial class LeftMdrControl : UserControl
////    {

////        private static readonly string MotorTypesFile =
////            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

////        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
////            = new ObservableCollection<MotorTypeItem>();

////        private static readonly string BrakeTypesFile =
////            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

////        public ObservableCollection<string> BrakeTypes { get; set; }
////            = new ObservableCollection<string>();

////        public LeftMdrControl()
////        {
////            InitializeComponent();

////            LoadMotorTypes();
////            LeftMotorCombo.ItemsSource = MotorTypes;
////            LeftMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
////            if (MotorTypes.Count > 0)
////                LeftMotorCombo.SelectedIndex = -1;
////            LeftMotorCombo.SelectionChanged += LeftMotorCombo_SelectionChanged;

////            LoadBrakeTypes();
////            LeftBrakeCombo.ItemsSource = BrakeTypes;
////            if (BrakeTypes.Count > 0)
////                LeftBrakeCombo.SelectedIndex = -1;
////            LeftBrakeCombo.SelectionChanged += LeftBrakeCombo_SelectionChanged;
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
////                    "BL 42 85,15,",
////                    "BL 42 85,4.9,",
////                    "BL 42 93,19,",
////                    "BL 42 93,27,",
////                    "BL 42 93,49,",
////                    "BL 42 93,82,",
////                    "BL 57 93,23,",
////                    "BL 57 93,96,",
////                    "BL 57 93,43,"
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

////        private void LoadBrakeTypes()
////        {
////            BrakeTypes.Clear();

////            if (!File.Exists(BrakeTypesFile))
////            {
////                File.WriteAllLines(BrakeTypesFile, new[]
////                {
////                    "Normal,,",
////                    "Free,,",
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
////        private void LeftMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            if (LeftMotorCombo.SelectedIndex < 0) return;

////            // Register 0x0001 = Left Hall Type (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0001, (ushort)LeftMotorCombo.SelectedIndex);
////        }

////        private void LeftBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            if (LeftBrakeCombo.SelectedIndex < 0) return;

////            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
////        }

////        // ══════════════════ MOTOR TYPE ══════════════════
////        // SET button still works too (sends the same value again) — harmless,
////        // kept for anyone used to clicking it, and to show a confirmation popup.
////        private void SetLeftMotorType_Click(object sender, RoutedEventArgs e)
////        {
////            if (LeftMotorCombo.SelectedIndex < 0)
////            {
////                MessageBox.Show("Please select a motor type first.", "No Selection",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            var selected = LeftMotorCombo.SelectedItem as MotorTypeItem;
////            System.Diagnostics.Debug.WriteLine("Leftmotor_Motortype comment sent");
////            // Register 0x0001 = Left Hall Type (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0001, (ushort)LeftMotorCombo.SelectedIndex);

////            //MessageBox.Show($"Left motor type set to: {selected?.Name}, Gear: {selected?.Gear}",
////            //                 "Motor Type Set", MessageBoxButton.OK, MessageBoxImage.Information);
////        }

////        // ══════════════════ BRAKE METHOD ══════════════════
////        // Sends whatever is currently selected in the combo when SET is clicked.
////        private void SetLeftBrake_Click(object sender, RoutedEventArgs e)
////        {
////            if (LeftBrakeCombo.SelectedIndex < 0)
////            {
////                MessageBox.Show("Please select a brake method first.", "No Selection",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            string selectedBrake = LeftBrakeCombo.SelectedItem as string;

////            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
////            System.Diagnostics.Debug.WriteLine("Leftmotor_Brake comment sent");
////            //MessageBox.Show($"Left brake method set to: {selectedBrake}",
////            //                 "Brake Set", MessageBoxButton.OK, MessageBoxImage.Information);
////        }

////        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
////        private void SetSpeed_Click(object sender, RoutedEventArgs e)
////        {
////            if (!int.TryParse(LeftSpeedTextBox.Text, out int speed))
////            {
////                MessageBox.Show("Enter a valid speed value.", "Invalid Input",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            // Register 0x0027 = Left Speed (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x0027, (ushort)speed);

////            System.Diagnostics.Debug.WriteLine("leftmotor_speed comment sent");
////        }

////        private void SetAccel_Click(object sender, RoutedEventArgs e)
////        {
////            if (!int.TryParse(LeftAccelTextBox.Text, out int accel))
////            {
////                MessageBox.Show("Enter a valid acceleration value.", "Invalid Input",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            // Register 0x002A = Left Acceleration (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x002A, (ushort)accel);

////            System.Diagnostics.Debug.WriteLine("leftmotor_Accel comment sent");
////        }

////        private void SetDecel_Click(object sender, RoutedEventArgs e)
////        {
////            if (!int.TryParse(LeftDecelTextBox.Text, out int decel))
////            {
////                MessageBox.Show("Enter a valid deceleration value.", "Invalid Input",
////                                 MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }

////            // Register 0x002B = Left Deceleration (see BL.ParseAndUpdateUI)
////            BLLogic?.SendWriteCommand(0x002B, (ushort)decel);
////            System.Diagnostics.Debug.WriteLine("leftmotor_decel comment sent");
////        }
////        // Called externally (e.g. from MotorControlPanel's global Start/Stop) to keep
////        // this control's button in sync without re-sending the write command.
////        public void UpdateRunState(bool running)
////        {
////            _isRunning = running;
////            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
////        }
////        // ══════════════════ RUN / STOP TOGGLE ══════════════════
////        private bool _isRunning = false;
////        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
////        {
////            _isRunning = !_isRunning;

////            // TODO: confirm the correct register/value pair for Run/Stop.
////            BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
////            if (_isRunning)
////            {
////                System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
////            }
////            else
////            {
////                System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
////            }
////            //BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
////            //System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
////            //BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
////            //System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
////            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
////        }

////        public void UpdateMotorTelemetry(ModbusDevice device)
////        {
////            if (device == null) return;

////            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
////            {
////                if (!LeftSpeedTextBox.IsFocused || device.LeftSpeed == "0")
////                {
////                    if (LeftSpeedTextBox.Text != device.LeftSpeed)
////                        LeftSpeedTextBox.Text = device.LeftSpeed;
////                }

////                if (!LeftAccelTextBox.IsFocused || device.LeftAcceleration == "0")
////                {
////                    if (LeftAccelTextBox.Text != device.LeftAcceleration)
////                        LeftAccelTextBox.Text = device.LeftAcceleration;
////                }

////                if (!LeftDecelTextBox.IsFocused || device.LeftDeceleration == "0")
////                {
////                    if (LeftDecelTextBox.Text != device.LeftDeceleration)
////                        LeftDecelTextBox.Text = device.LeftDeceleration;
////                }

////                if (LeftCurrentTextBox.Text != device.LeftCurrent)
////                    LeftCurrentTextBox.Text = device.LeftCurrent;

////                // NOTE: Motor Type / Brake Method combos are intentionally NOT
////                // updated from device telemetry here. They only change when the
////                // user picks a new item from the dropdown, and that selection
////                // stays put (unaffected by incoming device data) until SET is
////                // clicked to send it.

////                FlagHallSensor.Tag = device.LeftHallSensorError;
////               // FlagOverCurrent.Tag = device.LeftOverCurrent;
////                FlagOverVoltage.Tag = device.LeftOverVoltage;
////                FlagUnderVoltage.Tag = device.LeftUnderVoltage;
////               // FlagMotorStalled.Tag = device.LeftMotorStalled;
////               // FlagOverTemp.Tag = device.LeftOverTemperature;
////            }));
////        }

////        // ══════════════════ MANUAL UI UPDATE ══════════════════
////        public void ClearUI()
////        {
////            LeftSpeedTextBox.Text = string.Empty;
////            LeftCurrentTextBox.Text = string.Empty;
////            LeftAccelTextBox.Text = string.Empty;
////            LeftDecelTextBox.Text = string.Empty;
////            LeftBrakeCombo.SelectedIndex = -1;
////            LeftMotorCombo.SelectedIndex = -1;
////        }
////    }
////}
//using BLDC_Demo.Controls;
//using BLDC_Demo.Models;
//using System;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Shapes;

//namespace BLDC_Demo
//{
//    public partial class LeftMdrControl : UserControl
//    {

//        private static readonly string MotorTypesFile =
//            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

//        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
//            = new ObservableCollection<MotorTypeItem>();

//        private static readonly string BrakeTypesFile =
//            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

//        public ObservableCollection<string> BrakeTypes { get; set; }
//            = new ObservableCollection<string>();

//        public LeftMdrControl()
//        {
//            InitializeComponent();

//            LoadMotorTypes();
//            LeftMotorCombo.ItemsSource = MotorTypes;
//            LeftMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
//            if (MotorTypes.Count > 0)
//                LeftMotorCombo.SelectedIndex = -1;
//            LeftMotorCombo.SelectionChanged += LeftMotorCombo_SelectionChanged;

//            LoadBrakeTypes();
//            LeftBrakeCombo.ItemsSource = BrakeTypes;
//            if (BrakeTypes.Count > 0)
//                LeftBrakeCombo.SelectedIndex = -1;
//            LeftBrakeCombo.SelectionChanged += LeftBrakeCombo_SelectionChanged;
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
//                    //"BL 42 85,15,",
//                    //"BL 42 85,4.9,",
//                    //"BL 42 93,19,",
//                    //"BL 42 93,27,",
//                    //"BL 42 93,49,",
//                    //"BL 42 93,82,",
//                    //"BL 57 93,23,",
//                    //"BL 57 93,96,",
//                    //"BL 57 93,43,"
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

//        private void LoadBrakeTypes()
//        {
//            BrakeTypes.Clear();

//            if (!File.Exists(BrakeTypesFile))
//            {
//                File.WriteAllLines(BrakeTypesFile, new[]
//                {
//                    "Normal,,",
//                   // "Free,,",
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

//        // Fires the write the instant the user picks a new item — no need to
//        // wait for the SET button. Guarded with SelectedIndex < 0 so it's a
//        // no-op during startup/clearing (SelectedIndex = -1).
//        //private void LeftMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        //{
//        //    if (LeftMotorCombo.SelectedIndex < 0) return;

//        //    // Register 0x0001 = Left Hall Type (see BL.ParseAndUpdateUI)
//        //    BLLogic?.SendWriteCommand(0x0001, (ushort)LeftMotorCombo.SelectedIndex);
//        //}
//        public static readonly DependencyProperty SetButtonsEnabledProperty =
//    DependencyProperty.Register(
//        nameof(SetButtonsEnabled),
//        typeof(bool),
//        typeof(LeftMdrControl),
//        new PropertyMetadata(false));

//        public bool SetButtonsEnabled
//        {
//            get => (bool)GetValue(SetButtonsEnabledProperty);
//            set => SetValue(SetButtonsEnabledProperty, value);
//        }
//        public void SetConnected(bool connected)
//        {
//            SetButtonsEnabled = connected;
//        }
//        private void LeftMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (LeftMotorCombo.SelectedItem == null) return;

//            string selected = LeftMotorCombo.SelectedItem.ToString(); // "BL 42 85 x 24"

//            if (selected.Length % 2 != 0) selected += "\0"; // pad if odd length

//            ushort[] regs = new ushort[selected.Length / 2];
//            for (int i = 0; i < regs.Length; i++)
//            {
//                byte hi = (byte)selected[i * 2];
//                byte lo = (byte)selected[i * 2 + 1];
//                regs[i] = (ushort)((hi << 8) | lo);
//            }

//            // 14 chars -> 7 registers
//            for (int i = 0; i < regs.Length; i++)
//            {
//                BLLogic?.SendWriteCommand((ushort)(0x0001 + i), regs[i]);
//            }// use the correct register address for Motor Type
//        }
//        private void LeftBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (LeftBrakeCombo.SelectedIndex < 0) return;

//            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
//        }

//        // ══════════════════ MOTOR TYPE ══════════════════
//        // SET button still works too (sends the same value again) — harmless,
//        // kept for anyone used to clicking it, and to show a confirmation popup.
//        private void SetLeftMotorType_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftMotorCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a motor type first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            System.Diagnostics.Debug.WriteLine("Leftmotor_Motortype comment sent");
//            // Register 0x0001 = Left Hall Type (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x0001, (ushort)LeftMotorCombo.SelectedIndex);

//            //MessageBox.Show($"Left motor type set to: {selected?.Name}, Gear: {selected?.Gear}",
//            //                 "Motor Type Set", MessageBoxButton.OK, MessageBoxImage.Information);
//        }
//        private void SetLeftDirection_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftDirectionCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a direction first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (LeftDirectionCombo.SelectedIndex == 0)
//            {
//                // Index 0 is "CLOCKWISE"
//                // Generates: 01 06 00 47 00 01 F8 1F
//              BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0047, 1);
//                BLLogic.SendWriteCommand(0x0069, 1);
//                BLLogic.SendWriteCommand(0x0069, 1);
//                BLLogic.SendWriteCommand(0x0069, 1);
//            }
//            else if (LeftDirectionCombo.SelectedIndex == 1)
//            {
//                // Index 1 is "ANTI-CW"
//                // Generates: 01 06 00 47 00 00 39
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0047, 0);
//                BLLogic.SendWriteCommand(0x0069, 0);
//                BLLogic.SendWriteCommand(0x0069, 0);
//                BLLogic.SendWriteCommand(0x0069, 0);
//            }
//            System.Diagnostics.Debug.WriteLine("Leftmotor_Direction command sent");
//            // Register 0x000X = Left Direction (update to your actual register)
//            BLLogic?.SendWriteCommand(0x0069, (ushort)LeftDirectionCombo.SelectedIndex);
//        }
//        // ══════════════════ BRAKE METHOD ══════════════════
//        // Sends whatever is currently selected in the combo when SET is clicked.
//        private void SetLeftBrake_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftBrakeCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a brake method first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
//            System.Diagnostics.Debug.WriteLine("Leftmotor_Brake comment sent");
//            //MessageBox.Show($"Left brake method set to: {selectedBrake}",
//            //                 "Brake Set", MessageBoxButton.OK, MessageBoxImage.Information);
//        }

//        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
//        private void SetSpeed_Click(object sender, RoutedEventArgs e)
//        {
//            if (!int.TryParse(LeftSpeedTextBox.Text, out int speed))
//            {
//                MessageBox.Show("Enter a valid speed value.", "Invalid Input",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x0027 = Left Speed (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x0027, (ushort)speed);

//            System.Diagnostics.Debug.WriteLine("leftmotor_speed comment sent");
//        }

//        private void SetAccel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!int.TryParse(LeftAccelTextBox.Text, out int accel))
//            {
//                MessageBox.Show("Enter a valid acceleration value.", "Invalid Input",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x002A = Left Acceleration (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002A, (ushort)accel);

//            System.Diagnostics.Debug.WriteLine("leftmotor_Accel comment sent");
//        }

//        private void SetDecel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!int.TryParse(LeftDecelTextBox.Text, out int decel))
//            {
//                MessageBox.Show("Enter a valid deceleration value.", "Invalid Input",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x002B = Left Deceleration (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002B, (ushort)decel);
//            System.Diagnostics.Debug.WriteLine("leftmotor_decel comment sent");
//        }
//        // Called externally (e.g. from MotorControlPanel's global Start/Stop) to keep
//        // this control's button in sync without re-sending the write command.
//        public void UpdateRunState(bool running)
//        {
//            _isRunning = running;
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }
//        // ══════════════════ RUN / STOP TOGGLE ══════════════════
//        private bool _isRunning = false;
//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
//        {
//            _isRunning = !_isRunning;

//            // TODO: confirm the correct register/value pair for Run/Stop.
//            BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
//            if (_isRunning)
//            {
//                System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
//            }
//            //BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
//            //System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
//            //BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
//            //System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                if (!LeftSpeedTextBox.IsFocused || device.LeftSpeed == "0")
//                {
//                    if (LeftSpeedTextBox.Text != device.LeftSpeed)
//                        LeftSpeedTextBox.Text = device.LeftSpeed;
//                }

//                if (!LeftAccelTextBox.IsFocused || device.LeftAcceleration == "0")
//                {
//                    if (LeftAccelTextBox.Text != device.LeftAcceleration)
//                        LeftAccelTextBox.Text = device.LeftAcceleration;
//                }

//                if (!LeftDecelTextBox.IsFocused || device.LeftDeceleration == "0")
//                {
//                    if (LeftDecelTextBox.Text != device.LeftDeceleration)
//                        LeftDecelTextBox.Text = device.LeftDeceleration;
//                }

//                if (LeftCurrentTextBox.Text != device.LeftCurrent)
//                    LeftCurrentTextBox.Text = device.LeftCurrent;

//                // NOTE: Motor Type / Brake Method combos are intentionally NOT
//                // updated from device telemetry here. They only change when the
//                // user picks a new item from the dropdown, and that selection
//                // stays put (unaffected by incoming device data) until SET is
//                // clicked to send it.

//                FlagHallSensor.Tag = device.LeftHallSensorError;
//                FlagOverCurrent.Tag = device.LeftOverCurrent;
//                FlagOverVoltage.Tag = device.LeftOverVoltage;
//                FlagLowVoltage.Tag = device.LeftLowVoltage;
//                Flagleftsensor1.Tag = device.LeftSensor1;
//                Flagleftsensor2.Tag = device.LeftSensor2;
//                // FlagOverTemp.Tag = device.LeftOverTemperature;
//            }));
//        }

//        // ══════════════════ MANUAL UI UPDATE ══════════════════
//        public void ClearUI()
//        {
//            FlagHallSensor.Tag = -1;
//            FlagOverCurrent.Tag = -1;
//            FlagOverVoltage.Tag = -1;
//            FlagLowVoltage.Tag = -1;
//            Flagleftsensor1.Tag = -1;
//            Flagleftsensor2.Tag = -1;
//            LeftSpeedTextBox.Text = string.Empty;
//            LeftCurrentTextBox.Text = string.Empty;
//            LeftAccelTextBox.Text = string.Empty;
//            LeftDecelTextBox.Text = string.Empty;
//            LeftBrakeCombo.SelectedIndex = -1;
//            LeftMotorCombo.SelectedIndex = -1;
//            LeftDirectionCombo.SelectedIndex = -1;
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
//using System.Windows.Shapes;

//namespace BLDC_Demo
//{
//    public partial class LeftMdrControl : UserControl
//    {

//        private static readonly string MotorTypesFile =
//            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

//        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
//            = new ObservableCollection<MotorTypeItem>();

//        private static readonly string BrakeTypesFile =
//            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

//        public ObservableCollection<string> BrakeTypes { get; set; }
//            = new ObservableCollection<string>();

//        // ── Allowed input ranges ──
//        private const int SpeedMin = 0;
//        private const int SpeedMax = 100;
//        private const int AccelMin = 10;
//        private const int AccelMax = 10000;
//        private const int DecelMin = 10;
//        private const int DecelMax = 10000;

//        public LeftMdrControl()
//        {
//            InitializeComponent();

//            LoadMotorTypes();
//            LeftMotorCombo.ItemsSource = MotorTypes;
//            LeftMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
//            if (MotorTypes.Count > 0)
//                LeftMotorCombo.SelectedIndex = -1;
//            LeftMotorCombo.SelectionChanged += LeftMotorCombo_SelectionChanged;

//            LoadBrakeTypes();
//            LeftBrakeCombo.ItemsSource = BrakeTypes;
//            if (BrakeTypes.Count > 0)
//                LeftBrakeCombo.SelectedIndex = -1;
//            LeftBrakeCombo.SelectionChanged += LeftBrakeCombo_SelectionChanged;
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

//        private void LoadBrakeTypes()
//        {
//            BrakeTypes.Clear();

//            if (!File.Exists(BrakeTypesFile))
//            {
//                File.WriteAllLines(BrakeTypesFile, new[]
//                {
//                    "Normal,,",
//                   // "Free,,",
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

//        public static readonly DependencyProperty SetButtonsEnabledProperty =
//    DependencyProperty.Register(
//        nameof(SetButtonsEnabled),
//        typeof(bool),
//        typeof(LeftMdrControl),
//        new PropertyMetadata(false));

//        public bool SetButtonsEnabled
//        {
//            get => (bool)GetValue(SetButtonsEnabledProperty);
//            set => SetValue(SetButtonsEnabledProperty, value);
//        }
//        public void SetConnected(bool connected)
//        {
//            SetButtonsEnabled = connected;
//        }
//        private void LeftMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (!(LeftMotorCombo.SelectedItem is MotorTypeItem item)) return;

//            // "BL 42 85 x 24"  ->  "24"
//            int xPos = item.Name.LastIndexOf('x');
//            if (xPos < 0) return;
//            string ratioText = item.Name.Substring(xPos + 1).Trim();

//            if (!ushort.TryParse(ratioText, out ushort ratio)) return;   // 24 -> 24

//            BLLogic?.SendWriteCommand(0x0001, ratio);
//        }
//        private void LeftBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (LeftBrakeCombo.SelectedIndex < 0) return;

//            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
//        }

//        private void SetLeftMotorType_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftMotorCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a motor type first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            System.Diagnostics.Debug.WriteLine("Leftmotor_Motortype comment sent");
//            // Register 0x0001 = Left Hall Type (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x0001, (ushort)LeftMotorCombo.SelectedIndex);

//            //MessageBox.Show($"Left motor type set to: {selected?.Name}, Gear: {selected?.Gear}",
//            //                 "Motor Type Set", MessageBoxButton.OK, MessageBoxImage.Information);
//        }
//        private void SetLeftDirection_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftDirectionCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a direction first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (LeftDirectionCombo.SelectedIndex == 0)
//            {
//                // Index 0 is "CLOCKWISE"
//                // Generates: 01 06 00 47 00 01 F8 1F
//                BLLogic.SendWriteCommand(0x002F, 1);
//                BLLogic.SendWriteCommand(0x002F, 1);
//                BLLogic.SendWriteCommand(0x002F, 1);

//            }
//            else if (LeftDirectionCombo.SelectedIndex == 1)
//            {
//                // Index 1 is "ANTI-CW"
//                // Generates: 01 06 00 47 00 00 39
//                BLLogic.SendWriteCommand(0x002F, 0);
//                BLLogic.SendWriteCommand(0x002F, 0);
//                BLLogic.SendWriteCommand(0x002F, 0);

//            }
//            System.Diagnostics.Debug.WriteLine("Leftmotor_Direction command sent");
//            // Register 0x000X = Left Direction (update to your actual register)
//            BLLogic?.SendWriteCommand(0x0069, (ushort)LeftDirectionCombo.SelectedIndex);
//        }
//        // ══════════════════ BRAKE METHOD ══════════════════
//        // Sends whatever is currently selected in the combo when SET is clicked.
//        private void SetLeftBrake_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftBrakeCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a brake method first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
//            System.Diagnostics.Debug.WriteLine("Leftmotor_Brake comment sent");
//            //MessageBox.Show($"Left brake method set to: {selectedBrake}",
//            //                 "Brake Set", MessageBoxButton.OK, MessageBoxImage.Information);
//        }

//        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
//        private void SetSpeed_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(LeftSpeedTextBox, SpeedMin, SpeedMax, "Speed (%)", out int speed))
//                return;

//            // Register 0x0027 = Left Speed (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x0027, (ushort)speed);

//            System.Diagnostics.Debug.WriteLine("leftmotor_speed comment sent");
//        }

//        private void SetAccel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(LeftAccelTextBox, AccelMin, AccelMax, "Acceleration (ms)", out int accel))
//                return;

//            // Register 0x002A = Left Acceleration (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002A, (ushort)accel);

//            System.Diagnostics.Debug.WriteLine("leftmotor_Accel comment sent");
//        }

//        private void SetDecel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(LeftDecelTextBox, DecelMin, DecelMax, "Deceleration (ms)", out int decel))
//                return;

//            // Register 0x002B = Left Deceleration (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002B, (ushort)decel);
//            System.Diagnostics.Debug.WriteLine("leftmotor_decel comment sent");
//        }
//        // Called externally (e.g. from MotorControlPanel's global Start/Stop) to keep
//        // this control's button in sync without re-sending the write command.
//        public void UpdateRunState(bool running)
//        {
//            _isRunning = running;
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }
//        // ══════════════════ RUN / STOP TOGGLE ══════════════════
//        private bool _isRunning = false;
//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
//        {
//            _isRunning = !_isRunning;

//            // TODO: confirm the correct register/value pair for Run/Stop.
//            BLLogic?.SendWriteCommand(0x0030, (ushort)(_isRunning ? 1 : 0));
//            if (_isRunning)
//            {
//                System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("leftmotor_run comment sent");
//            }

//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                if (!LeftSpeedTextBox.IsFocused || device.LeftSpeed == "0")
//                {
//                    if (LeftSpeedTextBox.Text != device.LeftSpeed)
//                        LeftSpeedTextBox.Text = device.LeftSpeed;
//                }

//                if (!LeftAccelTextBox.IsFocused || device.LeftAcceleration == "0")
//                {
//                    if (LeftAccelTextBox.Text != device.LeftAcceleration)
//                        LeftAccelTextBox.Text = device.LeftAcceleration;
//                }

//                if (!LeftDecelTextBox.IsFocused || device.LeftDeceleration == "0")
//                {
//                    if (LeftDecelTextBox.Text != device.LeftDeceleration)
//                        LeftDecelTextBox.Text = device.LeftDeceleration;
//                }

//                if (LeftCurrentTextBox.Text != device.LeftCurrent)
//                    LeftCurrentTextBox.Text = device.LeftCurrent;

//                FlagHallSensor.Tag = device.LeftHallSensorError;
//                FlagOverCurrent.Tag = device.LeftOverCurrent;
//                FlagOverVoltage.Tag = device.LeftOverVoltage;
//                FlagLowVoltage.Tag = device.LeftLowVoltage;
//                Flagleftsensor1.Tag = device.LeftSensor1;
//                Flagleftsensor2.Tag = device.LeftSensor2;

//            }));
//        }

//        // ══════════════════ MANUAL UI UPDATE ══════════════════
//        public void ClearUI()
//        {
//            FlagHallSensor.Tag = -1;
//            FlagOverCurrent.Tag = -1;
//            FlagOverVoltage.Tag = -1;
//            FlagLowVoltage.Tag = -1;
//            Flagleftsensor1.Tag = -1;
//            Flagleftsensor2.Tag = -1;
//            LeftSpeedTextBox.Text = string.Empty;
//            LeftCurrentTextBox.Text = string.Empty;
//            LeftAccelTextBox.Text = string.Empty;
//            LeftDecelTextBox.Text = string.Empty;
//            LeftBrakeCombo.SelectedIndex = -1;
//            LeftMotorCombo.SelectedIndex = -1;
//            LeftDirectionCombo.SelectedIndex = -1;
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
//    public partial class LeftMdrControl : UserControl
//    {

//        private static readonly string MotorTypesFile =
//            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

//        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
//            = new ObservableCollection<MotorTypeItem>();

//        private static readonly string BrakeTypesFile =
//            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

//        public ObservableCollection<string> BrakeTypes { get; set; }
//            = new ObservableCollection<string>();

//        // ── Allowed input ranges ──
//        private const int SpeedMin = 0;
//        private const int SpeedMax = 100;
//        private const int AccelMin = 10;
//        private const int AccelMax = 10000;
//        private const int DecelMin = 10;
//        private const int DecelMax = 10000;

//        public LeftMdrControl()
//        {
//            InitializeComponent();

//            LoadMotorTypes();
//            LeftMotorCombo.ItemsSource = MotorTypes;
//            LeftMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
//            if (MotorTypes.Count > 0)
//                LeftMotorCombo.SelectedIndex = -1;
//            LeftMotorCombo.SelectionChanged += LeftMotorCombo_SelectionChanged;

//            LoadBrakeTypes();
//            LeftBrakeCombo.ItemsSource = BrakeTypes;
//            if (BrakeTypes.Count > 0)
//                LeftBrakeCombo.SelectedIndex = -1;
//            LeftBrakeCombo.SelectionChanged += LeftBrakeCombo_SelectionChanged;
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

//        private void LoadBrakeTypes()
//        {
//            BrakeTypes.Clear();

//            if (!File.Exists(BrakeTypesFile))
//            {
//                File.WriteAllLines(BrakeTypesFile, new[]
//                {
//                    "Normal,,",
//                    // "Free,,",
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

//        public static readonly DependencyProperty SetButtonsEnabledProperty =
//            DependencyProperty.Register(
//                nameof(SetButtonsEnabled),
//                typeof(bool),
//                typeof(LeftMdrControl),
//                new PropertyMetadata(false));

//        public bool SetButtonsEnabled
//        {
//            get => (bool)GetValue(SetButtonsEnabledProperty);
//            set => SetValue(SetButtonsEnabledProperty, value);
//        }

//        public void SetConnected(bool connected)
//        {
//            SetButtonsEnabled = connected;
//        }

//        // ══════════════════ MOTOR TYPE ══════════════════
//        // Sends the number after the 'x' as a plain integer to register 0x0001.
//        // "BL 42 85 x 24" -> 24
//        private void LeftMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (!(LeftMotorCombo.SelectedItem is MotorTypeItem item)) return;

//            int xPos = item.Name.LastIndexOf('x');
//            if (xPos < 0) return;
//            string ratioText = item.Name.Substring(xPos + 1).Trim();

//            if (!ushort.TryParse(ratioText, out ushort ratio)) return;   // 24 -> 24

//            BLLogic?.SendWriteCommand(0x0001, ratio);
//        }

//        private void SetLeftMotorType_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftMotorCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a motor type first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            System.Diagnostics.Debug.WriteLine("Leftmotor_Motortype command sent");
//            // FIXED: the ratio is already sent by SelectionChanged. Nothing is written
//            // here, so the ratio in 0x0001 is not overwritten by the combo index.
//        }

//        // ══════════════════ BRAKE METHOD ══════════════════
//        private void LeftBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (LeftBrakeCombo.SelectedIndex < 0) return;

//            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
//        }

//        // Sends whatever is currently selected in the combo when SET is clicked.
//        private void SetLeftBrake_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftBrakeCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a brake method first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex))
//                return;

//            System.Diagnostics.Debug.WriteLine("Leftmotor_Brake command sent");
//        }

//        private void SetLeftDirection_Click(object sender, RoutedEventArgs e)
//        {
//            if (LeftDirectionCombo.SelectedIndex < 0)
//            {
//                MessageBox.Show("Please select a direction first.", "No Selection",
//                                 MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (LeftDirectionCombo.SelectedIndex == 0)
//            {
//                // Index 0 is "CLOCKWISE"
//                // Generates: 01 06 00 47 00 01 F8 1F
//                BLLogic.SendWriteCommand(0x002F, 1);
//                BLLogic.SendWriteCommand(0x002F, 1);
//                BLLogic.SendWriteCommand(0x002F, 1);

//            }
//            else if (LeftDirectionCombo.SelectedIndex == 1)
//            {
//                // Index 1 is "ANTI-CW"
//                // Generates: 01 06 00 47 00 00 39
//                BLLogic.SendWriteCommand(0x002F, 0);
//                BLLogic.SendWriteCommand(0x002F, 0);
//                BLLogic.SendWriteCommand(0x002F, 0);

//            }
//            System.Diagnostics.Debug.WriteLine("Leftmotor_Direction command sent");
//            // Register 0x000X = Left Direction (update to your actual register)
//            BLLogic?.SendWriteCommand(0x0069, (ushort)LeftDirectionCombo.SelectedIndex);
//        }

//        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
//        private void SetSpeed_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(LeftSpeedTextBox, SpeedMin, SpeedMax, "Speed (%)", out int speed))
//                return;

//            // Register 0x0027 = Left Speed (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x0027, (ushort)speed);

//            System.Diagnostics.Debug.WriteLine("leftmotor_speed command sent");
//        }

//        private void SetAccel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(LeftAccelTextBox, AccelMin, AccelMax, "Acceleration (ms)", out int accel))
//                return;

//            // Register 0x002A = Left Acceleration (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002A, (ushort)accel);

//            System.Diagnostics.Debug.WriteLine("leftmotor_Accel command sent");
//        }

//        private void SetDecel_Click(object sender, RoutedEventArgs e)
//        {
//            if (!TryGetInRange(LeftDecelTextBox, DecelMin, DecelMax, "Deceleration (ms)", out int decel))
//                return;

//            // Register 0x002B = Left Deceleration (see BL.ParseAndUpdateUI)
//            BLLogic?.SendWriteCommand(0x002B, (ushort)decel);
//            System.Diagnostics.Debug.WriteLine("leftmotor_decel command sent");
//        }

//        // ══════════════════ RUN / STOP TOGGLE ══════════════════
//        private bool _isRunning = false;

//        // Called externally (e.g. from MotorControlPanel's global Start/Stop) to keep
//        // this control's button in sync without re-sending the write command.
//        public void UpdateRunState(bool running)
//        {
//            _isRunning = running;
//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
//        {
//            bool newState = !_isRunning;

//            // FIXED: only change UI state if the write was actually queued
//            // (same pattern as the Right control).
//            // TODO: confirm the correct register/value pair for Run/Stop.
//            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0030, (ushort)(newState ? 1 : 0)))
//                return;

//            _isRunning = newState;
//            System.Diagnostics.Debug.WriteLine(_isRunning
//                ? "leftmotor_run command sent"
//                : "leftmotor_stop command sent");

//            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
//        }

//        public void UpdateMotorTelemetry(ModbusDevice device)
//        {
//            if (device == null) return;

//            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
//            {
//                // FIXED: do not overwrite a box the user is typing in
//                // (the old "|| value == "0"" check erased typing while the motor was stopped).
//                if (!LeftSpeedTextBox.IsFocused)
//                {
//                    if (LeftSpeedTextBox.Text != device.LeftSpeed)
//                        LeftSpeedTextBox.Text = device.LeftSpeed;
//                }

//                if (!LeftAccelTextBox.IsFocused)
//                {
//                    if (LeftAccelTextBox.Text != device.LeftAcceleration)
//                        LeftAccelTextBox.Text = device.LeftAcceleration;
//                }

//                if (!LeftDecelTextBox.IsFocused)
//                {
//                    if (LeftDecelTextBox.Text != device.LeftDeceleration)
//                        LeftDecelTextBox.Text = device.LeftDeceleration;
//                }

//                if (LeftCurrentTextBox.Text != device.LeftCurrent)
//                    LeftCurrentTextBox.Text = device.LeftCurrent;

//                FlagHallSensor.Tag = device.LeftHallSensorError;
//                FlagOverCurrent.Tag = device.LeftOverCurrent;
//                FlagOverVoltage.Tag = device.LeftOverVoltage;
//                FlagLowVoltage.Tag = device.LeftLowVoltage;
//                Flagleftsensor1.Tag = device.LeftSensor1;
//                Flagleftsensor2.Tag = device.LeftSensor2;
//            }));
//        }

//        // ══════════════════ MANUAL UI UPDATE ══════════════════
//        public void ClearUI()
//        {
//            FlagHallSensor.Tag = -1;
//            FlagOverCurrent.Tag = -1;
//            FlagOverVoltage.Tag = -1;
//            FlagLowVoltage.Tag = -1;
//            Flagleftsensor1.Tag = -1;
//            Flagleftsensor2.Tag = -1;
//            LeftSpeedTextBox.Text = string.Empty;
//            LeftCurrentTextBox.Text = string.Empty;
//            LeftAccelTextBox.Text = string.Empty;
//            LeftDecelTextBox.Text = string.Empty;
//            LeftBrakeCombo.SelectedIndex = -1;
//            LeftMotorCombo.SelectedIndex = -1;
//            LeftDirectionCombo.SelectedIndex = -1;
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
    public partial class LeftMdrControl : UserControl
    {

        private static readonly string MotorTypesFile =
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.txt");

        public ObservableCollection<MotorTypeItem> MotorTypes { get; set; }
            = new ObservableCollection<MotorTypeItem>();

        private static readonly string BrakeTypesFile =
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrakeTypes.txt");

        public ObservableCollection<string> BrakeTypes { get; set; }
            = new ObservableCollection<string>();

        // ── Allowed input ranges ──
        private const int SpeedMin = 0;
        private const int SpeedMax = 100;
        private const int AccelMin = 10;
        private const int AccelMax = 10000;
        private const int DecelMin = 10;
        private const int DecelMax = 10000;

        public LeftMdrControl()
        {
            InitializeComponent();

            LoadMotorTypes();
            LeftMotorCombo.ItemsSource = MotorTypes;
            LeftMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
            if (MotorTypes.Count > 0)
                LeftMotorCombo.SelectedIndex = -1;
            LeftMotorCombo.SelectionChanged += LeftMotorCombo_SelectionChanged;

            LoadBrakeTypes();
            LeftBrakeCombo.ItemsSource = BrakeTypes;
            if (BrakeTypes.Count > 0)
                LeftBrakeCombo.SelectedIndex = -1;
            LeftBrakeCombo.SelectionChanged += LeftBrakeCombo_SelectionChanged;

            // ── Range limits for the number boxes ──
            AttachRange(LeftSpeedTextBox, SpeedMin, SpeedMax);
            AttachRange(LeftAccelTextBox, AccelMin, AccelMax);
            AttachRange(LeftDecelTextBox, DecelMin, DecelMax);
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

        private void LoadBrakeTypes()
        {
            BrakeTypes.Clear();

            if (!File.Exists(BrakeTypesFile))
            {
                File.WriteAllLines(BrakeTypesFile, new[]
                {
                    "Normal,,",
                    // "Free,,",
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

        public static readonly DependencyProperty SetButtonsEnabledProperty =
            DependencyProperty.Register(
                nameof(SetButtonsEnabled),
                typeof(bool),
                typeof(LeftMdrControl),
                new PropertyMetadata(false));

        public bool SetButtonsEnabled
        {
            get => (bool)GetValue(SetButtonsEnabledProperty);
            set => SetValue(SetButtonsEnabledProperty, value);
        }

        public void SetConnected(bool connected)
        {
            SetButtonsEnabled = connected;
        }

        // ══════════════════ MOTOR TYPE ══════════════════
        // Sends the number after the 'x' as a plain integer to register 0x0001.
        // "BL 42 85 x 24" -> 24
        private void LeftMotorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(LeftMotorCombo.SelectedItem is MotorTypeItem item)) return;

            int xPos = item.Name.LastIndexOf('x');
            if (xPos < 0) return;
            string ratioText = item.Name.Substring(xPos + 1).Trim();

            if (!ushort.TryParse(ratioText, out ushort ratio)) return;   // 24 -> 24

            BLLogic?.SendWriteCommand(0x0001, ratio);
        }

        private void SetLeftMotorType_Click(object sender, RoutedEventArgs e)
        {
            if (LeftMotorCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a motor type first.", "No Selection",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine("Leftmotor_Motortype command sent");
            // FIXED: the ratio is already sent by SelectionChanged. Nothing is written
            // here, so the ratio in 0x0001 is not overwritten by the combo index.
        }

        // ══════════════════ BRAKE METHOD ══════════════════
        private void LeftBrakeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LeftBrakeCombo.SelectedIndex < 0) return;

            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
            BLLogic?.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex);
        }

        // Sends whatever is currently selected in the combo when SET is clicked.
        private void SetLeftBrake_Click(object sender, RoutedEventArgs e)
        {
            if (LeftBrakeCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a brake method first.", "No Selection",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Register 0x002E = Left Brake Index (see BL.ParseAndUpdateUI)
            if (BLLogic == null || !BLLogic.SendWriteCommand(0x002E, (ushort)LeftBrakeCombo.SelectedIndex))
                return;

            System.Diagnostics.Debug.WriteLine("Leftmotor_Brake command sent");
        }

        private void SetLeftDirection_Click(object sender, RoutedEventArgs e)
        {
            if (LeftDirectionCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a direction first.", "No Selection",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (LeftDirectionCombo.SelectedIndex == 0)
            {
                // Index 0 is "CLOCKWISE"
                // Generates: 01 06 00 47 00 01 F8 1F
                BLLogic.SendWriteCommand(0x0069, 1);
                BLLogic.SendWriteCommand(0x0069, 1);
                BLLogic.SendWriteCommand(0x0069, 1);

            }
            else if (LeftDirectionCombo.SelectedIndex == 1)
            {
                // Index 1 is "ANTI-CW"
                // Generates: 01 06 00 47 00 00 39
                BLLogic.SendWriteCommand(0x0069, 0);
                BLLogic.SendWriteCommand(0x0069, 0);
                BLLogic.SendWriteCommand(0x0069, 0);

            }
            System.Diagnostics.Debug.WriteLine("Leftmotor_Direction command sent");
            // Register 0x000X = Left Direction (update to your actual register)
            BLLogic?.SendWriteCommand(0x0069, (ushort)LeftDirectionCombo.SelectedIndex);
        }

        // ══════════════════ SPEED / ACCEL / DECEL ══════════════════
        private void SetSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInRange(LeftSpeedTextBox, SpeedMin, SpeedMax, "Speed (%)", out int speed))
                return;

            // Register 0x0027 = Left Speed (see BL.ParseAndUpdateUI)
            BLLogic?.SendWriteCommand(0x0027, (ushort)speed);

            System.Diagnostics.Debug.WriteLine("leftmotor_speed command sent");
        }

        private void SetAccel_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInRange(LeftAccelTextBox, AccelMin, AccelMax, "Acceleration (ms)", out int accel))
                return;

            // Register 0x002A = Left Acceleration (see BL.ParseAndUpdateUI)
            BLLogic?.SendWriteCommand(0x002A, (ushort)accel);

            System.Diagnostics.Debug.WriteLine("leftmotor_Accel command sent");
        }

        private void SetDecel_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInRange(LeftDecelTextBox, DecelMin, DecelMax, "Deceleration (ms)", out int decel))
                return;

            // Register 0x002B = Left Deceleration (see BL.ParseAndUpdateUI)
            BLLogic?.SendWriteCommand(0x002B, (ushort)decel);
            System.Diagnostics.Debug.WriteLine("leftmotor_decel command sent");
        }

        // ══════════════════ RUN / STOP TOGGLE ══════════════════
        private bool _isRunning = false;

        // Called externally (e.g. from MotorControlPanel's global Start/Stop) to keep
        // this control's button in sync without re-sending the write command.
        public void UpdateRunState(bool running)
        {
            _isRunning = running;
            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
        }

        private void TripleStateButton_Click(object sender, RoutedEventArgs e)
        {
            bool newState = !_isRunning;

            // FIXED: only change UI state if the write was actually queued
            // (same pattern as the Right control).
            // TODO: confirm the correct register/value pair for Run/Stop.
            if (BLLogic == null || !BLLogic.SendWriteCommand(0x0030, (ushort)(newState ? 1 : 0)))
                return;

            _isRunning = newState;
            System.Diagnostics.Debug.WriteLine(_isRunning
                ? "leftmotor_run command sent"
                : "leftmotor_stop command sent");

            TripleStateButton.Content = _isRunning ? "■  STOP " : "▶  RUN  ";
        }

        public void UpdateMotorTelemetry(ModbusDevice device)
        {
            if (device == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // FIXED: do not overwrite a box the user is typing in
                // (the old "|| value == "0"" check erased typing while the motor was stopped).
                if (!LeftSpeedTextBox.IsFocused)
                {
                    if (LeftSpeedTextBox.Text != device.LeftSpeed)
                        LeftSpeedTextBox.Text = device.LeftSpeed;
                }

                if (!LeftAccelTextBox.IsFocused)
                {
                    if (LeftAccelTextBox.Text != device.LeftAcceleration)
                        LeftAccelTextBox.Text = device.LeftAcceleration;
                }

                if (!LeftDecelTextBox.IsFocused)
                {
                    if (LeftDecelTextBox.Text != device.LeftDeceleration)
                        LeftDecelTextBox.Text = device.LeftDeceleration;
                }

                if (LeftCurrentTextBox.Text != device.LeftCurrent)
                    LeftCurrentTextBox.Text = device.LeftCurrent;

                FlagHallSensor.Tag = device.LeftHallSensorError;
                FlagOverCurrent.Tag = device.LeftOverCurrent;
                FlagOverVoltage.Tag = device.LeftOverVoltage;
                FlagLowVoltage.Tag = device.LeftLowVoltage;
                Flagleftsensor1.Tag = device.LeftSensor1;
                Flagleftsensor2.Tag = device.LeftSensor2;
            }));
        }

        // ══════════════════ MANUAL UI UPDATE ══════════════════
        public void ClearUI()
        {
            FlagHallSensor.Tag = -1;
            FlagOverCurrent.Tag = -1;
            FlagOverVoltage.Tag = -1;
            FlagLowVoltage.Tag = -1;
            Flagleftsensor1.Tag = -1;
            Flagleftsensor2.Tag = -1;
            LeftSpeedTextBox.Text = string.Empty;
            LeftCurrentTextBox.Text = string.Empty;
            LeftAccelTextBox.Text = string.Empty;
            LeftDecelTextBox.Text = string.Empty;
            LeftBrakeCombo.SelectedIndex = -1;
            LeftMotorCombo.SelectedIndex = -1;
            LeftDirectionCombo.SelectedIndex = -1;
            TripleStateButton.Content = "▶  RUN  ";
            _isRunning = false;   // FIXED: keep the toggle state in sync with the button text
            SetButtonsEnabled = false;
        }
    }
}