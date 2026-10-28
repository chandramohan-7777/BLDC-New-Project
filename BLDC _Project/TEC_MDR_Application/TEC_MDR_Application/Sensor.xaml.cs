//using BLDC_Demo.Controls;
//using BLDC_Demo.Models;
//using System.Windows;
//using System.Windows.Controls;

//namespace BLDC_Demo
//{
//    public partial class Sensor : UserControl
//    {
//        // Modbus register addresses for sensor configuration on the ESP32 side.
//        private const ushort LeftSensor1NpnRegister = 0x0061;  // sensor type: 1 = NPN, 0 = PNP
//        private const ushort LeftSensor1NcRegister = 0x0062;   // contact state: 1 = NC, 0 = NO
//        private const ushort RightSensor1NpnRegister = 0x0063; // sensor type: 1 = NPN, 0 = PNP
//        private const ushort RightSensor1NcRegister = 0x0064;  // contact state: 1 = NC, 0 = NO

//        // TODO: confirm these register addresses — placeholders continuing the sequence.
//        private const ushort LeftSensor2NpnRegister = 0x0065;
//        private const ushort LeftSensor2NcRegister = 0x0066;
//        private const ushort RightSensor2NpnRegister = 0x0067;
//        private const ushort RightSensor2NcRegister = 0x0068;

//        // Set by MainWindow after construction, same pattern as CommMotorSelect.BLLogic.
//        public BL BLLogic { get; set; }

//        public Sensor()
//        {
//            InitializeComponent();
//        }

//        // ══════════════════ LEFT SENSOR 1 ══════════════════
//        private void SaveLeftSensor1Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = LeftSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = LeftSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(LeftSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(LeftSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Left Sensor 1 NPN and NC command sent");
//        }

//        // ══════════════════ LEFT SENSOR 2 ══════════════════
//        private void SaveLeftSensor2Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = LeftSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = LeftSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(LeftSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(LeftSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Left Sensor 2 NPN and NC command sent");
//        }

//        // ══════════════════ RIGHT SENSOR 1 ══════════════════
//        private void SaveRightSensor1Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = RightSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = RightSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(RightSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(RightSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Right Sensor 1 NPN and NC command sent");
//        }

//        // ══════════════════ RIGHT SENSOR 2 ══════════════════
//        private void SaveRightSensor2Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = RightSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = RightSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(RightSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(RightSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Right Sensor 2 NPN and NC command sent");
//        }
//    }
//}
using BLDC_Demo.Controls;
using BLDC_Demo.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BLDC_Demo
{
    public partial class Sensor : UserControl
    {
        // Modbus register addresses for sensor configuration on the ESP32 side.
        private const ushort LeftSensor1NpnRegister = 0x0061;  // sensor type: 1 = NPN, 0 = PNP
        private const ushort LeftSensor1NcRegister = 0x0062;   // contact state: 1 = NC, 0 = NO
        private const ushort RightSensor1NpnRegister = 0x0063; // sensor type: 1 = NPN, 0 = PNP
        private const ushort RightSensor1NcRegister = 0x0064;  // contact state: 1 = NC, 0 = NO

        // TODO: confirm these register addresses — placeholders continuing the sequence.
        private const ushort LeftSensor2NpnRegister = 0x0065;
        private const ushort LeftSensor2NcRegister = 0x0066;
        private const ushort RightSensor2NpnRegister = 0x0067;
        private const ushort RightSensor2NcRegister = 0x0068;

        // Set by MainWindow after construction, same pattern as CommMotorSelect.BLLogic.
        public BL BLLogic { get; set; }

        // Keeps the Save buttons in sync with the connection state
        private DispatcherTimer _stateTimer;

        public Sensor()
        {
            InitializeComponent();

            Loaded += Sensor_Loaded;
            Unloaded += Sensor_Unloaded;
        }

        // ══════════════════ CONNECTION STATE ══════════════════
        private void Sensor_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateButtonState();

            if (_stateTimer == null)
            {
                _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                _stateTimer.Tick += (s, args) => UpdateButtonState();
            }
            _stateTimer.Start();
        }

        private void Sensor_Unloaded(object sender, RoutedEventArgs e)
        {
            _stateTimer?.Stop();
        }

        // Uses this control's BLLogic if MainWindow set it, otherwise MainWindow's own.
        private BL ActiveBL =>
            BLLogic ?? (Application.Current.MainWindow as MainWindow)?.BLLogic;

        private bool IsDeviceConnected()
        {
            var bl = ActiveBL;
            return bl != null && bl.IsConnected;
        }

        // Save buttons are enabled only while connected to an IP.
        // Every Button in this control (the four Save buttons) is found through the
        // visual tree, so no x:Name is needed.
        private void UpdateButtonState()
        {
            bool connected = IsDeviceConnected();

            foreach (Button b in FindVisualChildren<Button>(this))
                b.IsEnabled = connected;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T match)
                    yield return match;

                foreach (T nested in FindVisualChildren<T>(child))
                    yield return nested;
            }
        }

        // ══════════════════ LEFT SENSOR 1 ══════════════════
        private void SaveLeftSensor1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = LeftSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = LeftSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(LeftSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(LeftSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Left Sensor 1 NPN and NC command sent");
        }

        // ══════════════════ LEFT SENSOR 2 ══════════════════
        private void SaveLeftSensor2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = LeftSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = LeftSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(LeftSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(LeftSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Left Sensor 2 NPN and NC command sent");
        }

        // ══════════════════ RIGHT SENSOR 1 ══════════════════
        private void SaveRightSensor1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = RightSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = RightSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(RightSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(RightSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Right Sensor 1 NPN and NC command sent");
        }

        // ══════════════════ RIGHT SENSOR 2 ══════════════════
        private void SaveRightSensor2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = RightSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = RightSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(RightSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(RightSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Right Sensor 2 NPN and NC command sent");
        }
    }
}