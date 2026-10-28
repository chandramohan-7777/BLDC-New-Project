using BLDC_Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace BLDC_Demo
{
    /// <summary>
    /// Interaction logic for NetworkConfigControl.xaml
    /// </summary>
    public partial class NetworkConfigControl : UserControl
    {
        public NetworkConfigControl()
        {
            InitializeComponent();
        }
        public void UpdateMotorTelemetry(ModbusDevice device)
        {
            if (device == null) return;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FirmwareTextBox.Text = device.FirmwareVersion;
                HardwareTextBox.Text = device.HardwareRevision;
                DevicestsTextBox.Text = device.DeviceStatus;
                DeviceIDTextBox.Text = device.DeviceID;
               // Node1TextBox.Text = device.Node1;
               // Node2TextBox.Text = device.Node2;
                SerialNOTextBox.Text = device.SerialNumber;
            });
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
                Owner = System.Windows.Application.Current.MainWindow
            };

            popup.ShowDialog();
        }

        public void ClearUI()
        {
            Dispatcher.Invoke(() =>
            {
                this.DataContext = null;
                FirmwareTextBox.Text = string.Empty;
                HardwareTextBox.Text = string.Empty;
                DevicestsTextBox.Text = string.Empty;
                Node1TextBox.Text = string.Empty;
                Node2TextBox.Text = string.Empty;
                DeviceIDTextBox.Text = string.Empty;
                SerialNOTextBox.Text = string.Empty;
            });
        }

        private void DeviceIDTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}