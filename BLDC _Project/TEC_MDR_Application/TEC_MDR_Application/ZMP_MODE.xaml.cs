using System;
using System.Windows;
using System.Windows.Controls;

namespace BLDC_Demo
{
    public partial class ZMP_MODE : UserControl
    {
        public ZMP_MODE()
        {
            InitializeComponent();
        }

        private void SetModeButton_Click(object sender, RoutedEventArgs e)
        {
            // Your Modbus TCP write command implementation goes here
            // e.g., reading GapTimerTextBox.Text and sending it via BLLogic
        }
    }
}