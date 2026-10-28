using System.Windows.Controls;

namespace BLDC_Demo
{
    public partial class AdvancedSettingsControl : UserControl
    {
        // ── Lazy-loaded tab controls ─────────────────────────────────
        //  private LookAheadTimingControl _lookAhead;
        //  private Connections _connections;
        private NetworkService _networkService;
        private Motor_Type _motor_type;
        //private ZMP_MODE _zmp_mode;
        private Sensor _sensor;
        //private Firmwareupload _firmwareupload;
        public AdvancedSettingsControl()
        {
            InitializeComponent();
            LoadTab(0);
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tc)
                LoadTab(tc.SelectedIndex);
        }

        private void LoadTab(int index)
        {
            switch (index)
            {
                // ── Tab 0: Look Ahead & Timing ───────────────────────
                //case 0:
                //    if (LookAheadContent == null) return;
                //    if (_lookAhead == null)
                //        _lookAhead = new LookAheadTimingControl();
                //    LookAheadContent.Content = _lookAhead;
                //    break;

                // ── Tab 6: Connections ───────────────────────────────
                //case 6:
                //    if (ConnectionsContent == null) return;
                //    if (_connections == null)
                //        _connections = new Connections();
                //    ConnectionsContent.Content = _connections;
                //    break;


                case 0:
                    if (NetworkServicesContent == null) return;
                    if (_networkService == null)
                        _networkService = new NetworkService();

                    NetworkServicesContent.Content = _networkService;
                    break;

                case 1:
                    if (Motor_TypeContent == null) return;
                    if (_motor_type == null)
                        _motor_type = new Motor_Type();
                    Motor_TypeContent.Content = _motor_type;
                    break;
                //case 2:
                //    if (ZMP_MODEContent == null) return;
                //    if (_zmp_mode == null)
                //        _zmp_mode = new ZMP_MODE();
                //    ZMP_MODEContent.Content = _zmp_mode;
                //    break;
                case 2:
                    if (SensorContent == null) return;
                    if (_sensor == null)
                        _sensor = new Sensor
                        {
                            BLLogic = (System.Windows.Application.Current.MainWindow as MainWindow)?.BLLogic
                        };
                    SensorContent.Content = _sensor;
                    break;
            }
        }
    }
}
