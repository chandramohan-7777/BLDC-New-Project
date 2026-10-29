
//using BLDC_Demo.Controls;
//using System;
//using System.ComponentModel;
//using System.Windows;
//using System.Windows.Media;
//using System.Windows.Threading;
//using BLDC_Demo.Models;
//namespace BLDC_Demo
//{
//    public partial class MainWindow : Window, INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;
//        private void OnPropertyChanged(string name) =>
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

//        public BL BLLogic { get; } = new BL();
//        private readonly DispatcherTimer _pollingTimer;

//        public MainWindow()
//        {
//            InitializeComponent();

//            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
//            Title = $"TEC-MDR Version ({version.Major}.{version.Minor}.{version.Build})";
//            // Bind against a real ModbusDevice from the start, not MainWindow itself —
//            // this is what LeftOverVoltage/RightMotorStalled/etc. actually live on.
//            DataContext = new ModbusDevice();

//            CommMotorSelect.BLLogic = BLLogic;

//            BLLogic.OnDisconnected += () =>
//            {
//                Application.Current.Dispatcher.Invoke(() =>
//                {
//                    // Let CommMotorSelectControl derive its own status text/color/
//                    // button content from BLLogic.ActivePortName — this is what
//                    // actually resets TcpConnectButton back to "CONNECT".
//                    CommMotorSelect?.UpdateStatusText();

//                    // Reset to a fresh idle device instead of leaving stale data bound.
//                    DataContext = new ModbusDevice();
//                    ClearAllUI();
//                });
//            };

//            _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
//            _pollingTimer.Tick += PollingTimer_Tick;
//            Loaded += MainWindow_Loaded;
//        }

//        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
//        {
//            _pollingTimer.Start();
//        }

//        //private void PollingTimer_Tick(object sender, EventArgs e)
//        //{
//        //    if (!string.IsNullOrEmpty(BLLogic.ActivePortName))
//        //    {
//        //        // Connected — poll for data, but never clear the UI while connected,
//        //        // even if CurrentDevice hasn't been populated by the first update yet.
//        //        BLLogic.UpdateData();

//        //        if (BLLogic.CurrentDevice != null)
//        //            MainShell.UpdateMdrUI(BLLogic.CurrentDevice);
//        //    }
//        //    else
//        //    {
//        //        // Not connected — safe to clear.
//        //        ClearAllUI();
//        //    }
//        //}
//        public void UpdateFirmwareVersion(string version)
//        {
//            FirmwareVersionText.Text = $" Firmware Version - {version}";
//        }
//        private void PollingTimer_Tick(object sender, EventArgs e)
//        {
//            bool isConnected = !string.IsNullOrEmpty(BLLogic.ActivePortName);

//            MainShell.LeftMdr?.SetConnected(isConnected);
//            MainShell.RightMdr?.SetConnected(isConnected);


//            if (isConnected)
//            {
//                BLLogic.UpdateData();
//                if (BLLogic.CurrentDevice != null)
//                    MainShell.UpdateMdrUI(BLLogic.CurrentDevice);
//            }
//            else
//            {
//                ClearAllUI();
//            }
//        }
//        public void ClearAllUI()
//        {
//            MainShell.LeftMdr?.ClearUI();
//            MainShell.RightMdr?.ClearUI();
//            MainShell.ZPASettingCtrl?.ClearUI();
//        }
//    }
//}
using BLDC_Demo.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BLDC_Demo.Models;
namespace BLDC_Demo
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
  

        public BL BLLogic { get; } = new BL();
        private readonly DispatcherTimer _pollingTimer;

        public MainWindow()
        {
            InitializeComponent();

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"TEC-MDR Version ({version.Major}.{version.Minor}.{version.Build})";
            // Bind against a real ModbusDevice from the start, not MainWindow itself —
            // this is what LeftOverVoltage/RightMotorStalled/etc. actually live on.
            DataContext = new ModbusDevice();

            CommMotorSelect.BLLogic = BLLogic;

            // Fires after auto-reconnect gives up / after a user-initiated Close().
            BLLogic.OnDisconnected += () =>
            {
                Application.Current.Dispatcher.Invoke(ResetToIdle);
            };

            // Fires the instant a drop is detected (before reconnect attempts run)
            // AND again after a successful auto-reconnect. Clear the screen right
            // away if the socket is dead, so the last values never stay visible.
            BLLogic.OnConnectionLost += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CommMotorSelect?.UpdateStatusText();
                    if (!BLLogic.IsConnected)
                        ResetToIdle();
                });
            };

            _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _pollingTimer.Tick += PollingTimer_Tick;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _pollingTimer.Start();
        }

        public void UpdateFirmwareVersion(string version)
        {
            FirmwareVersionText.Text = $" Firmware Version - {version}";
        }

        // Back to a fresh idle state: button/status text, bound device, all panels.
        private void ResetToIdle()
        {
            // Let CommMotorSelectControl derive its own status text/color/
            // button content — this is what resets TcpConnectButton to "CONNECT".
            CommMotorSelect?.UpdateStatusText();

            // Reset to a fresh idle device instead of leaving stale data bound.
            DataContext = new ModbusDevice();
            ClearAllUI();
        }

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            // CHANGED: was "!string.IsNullOrEmpty(BLLogic.ActivePortName)".
            // ActivePortName is only cleared inside BL.Close(), which runs AFTER
            // auto-reconnect gives up, so after a drop it still looked "connected"
            // here and the last values stayed on screen — while the Connect button
            // (which uses IsConnected) already showed disconnected.
            // IsConnected reflects the live socket state immediately.
            bool isConnected = BLLogic.IsConnected;

            MainShell.LeftMdr?.SetConnected(isConnected);
            MainShell.RightMdr?.SetConnected(isConnected);

            if (isConnected)
            {
                BLLogic.UpdateData();
                if (BLLogic.CurrentDevice != null)
                    MainShell.UpdateMdrUI(BLLogic.CurrentDevice);
            }
            else
            {
                //ClearAllUI();
            }
        }

        public void ClearAllUI()
        {
            MainShell.LeftMdr?.ClearUI();
            MainShell.RightMdr?.ClearUI();
            MainShell.ZPASettingCtrl?.ClearUI();
        }
    }
}