
//using System;
//using System.IO;
//using System.Windows;
//using System.Windows.Controls;

//namespace BLDC_Demo
//{
//    public partial class Motor_Type : UserControl
//    {
//        // Path to the CSV file storing "Name,Gear" per motor type.
//        // Sits next to the executable by default — change if you need a fixed/shared location.
//        private static readonly string MotorGearFilePath =
//            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotorTypes.csv");

//        public Motor_Type()
//        {
//            InitializeComponent();
//        }

//        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//        {
//            if (Application.Current.MainWindow is MainWindow mainWin)
//            {
//                RemoveMotorCombo.ItemsSource = mainWin.MainShell.LeftMdr.MotorTypes;
//            }
//        }

//        private void RemoveButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (RemoveMotorCombo.SelectedItem is string selectedName)
//            {
//                var mainWindow = Application.Current.MainWindow as MainWindow;
//                if (mainWindow == null) return;

//                var result = MessageBox.Show(
//                    $"Are you sure you want to remove motor type '{selectedName}'?",
//                    "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

//                if (result != MessageBoxResult.Yes)
//                    return;

//                mainWindow.MainShell.LeftMdr.MotorTypes.Remove(selectedName);
//                mainWindow.MainShell.RightMdr.MotorTypes.Remove(selectedName);

//                mainWindow.MainShell.LeftMdr.SaveMotorTypes();
//                mainWindow.MainShell.RightMdr.SaveMotorTypes();

//                MessageBox.Show($"Motor type '{selectedName}' removed and changes saved.",
//                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            else
//            {
//                MessageBox.Show("Please select a motor type to remove.", "Input Error",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//            }
//        }

//        private void AddButton_Click(object sender, RoutedEventArgs e)
//        {
//            string newMotorName = NameTextBox.Text?.Trim();
//            string gear1 = GearTextBox1.Text?.Trim();


//            if (string.IsNullOrWhiteSpace(newMotorName))
//            {
//                MessageBox.Show("Please enter a valid Motor Name.", "Input Error",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (string.IsNullOrWhiteSpace(gear1))
//            {
//                MessageBox.Show("Please enter a valid Gear1.", "Input Error",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }


//            if (Application.Current.MainWindow is MainWindow mainWin)
//            {
//                if (mainWin.MainShell.LeftMdr.MotorTypes.Contains(newMotorName))
//                {
//                    MessageBox.Show($"Motor type '{newMotorName}' already exists.", "Duplicate",
//                        MessageBoxButton.OK, MessageBoxImage.Warning);
//                    return;
//                }

//                mainWin.MainShell.LeftMdr.MotorTypes.Add(newMotorName);
//                mainWin.MainShell.RightMdr.MotorTypes.Add(newMotorName);

//                mainWin.MainShell.LeftMdr.SaveMotorTypes();
//                mainWin.MainShell.RightMdr.SaveMotorTypes();

//                try
//                {
//                    // File.AppendAllText(MotorGearFilePath, $"{newMotorName},{gear1}{Environment.NewLine}");
//                    File.AppendAllText(MotorGearFilePath, $"{newMotorName},{gear1}{Environment.NewLine}");
//                }
//                catch (IOException ex)
//                {
//                    MessageBox.Show($"Motor type added, but writing to file failed: {ex.Message}",
//                        "File Write Error", MessageBoxButton.OK, MessageBoxImage.Warning);
//                }

//                MessageBox.Show($"Motor type '{newMotorName}' added and saved permanently!",
//                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

//                NameTextBox.Clear();
//                GearTextBox1.Clear();

//            }
//        }
//    }
//}
//using System;
//using System.IO;
//using System.Linq;
//using System.Windows;
//using System.Windows.Controls;
//using BLDC_Demo.Models;

//namespace BLDC_Demo
//{
//    public partial class Motor_Type : UserControl
//    {
//        public Motor_Type()
//        {
//            InitializeComponent();
//        }

//        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//        {
//            if (Application.Current.MainWindow is MainWindow mainWin)
//            {
//                RemoveMotorCombo.ItemsSource = mainWin.MainShell.LeftMdr.MotorTypes;
//                RemoveMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
//            }
//        }

//        private void RemoveButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (RemoveMotorCombo.SelectedItem is MotorTypeItem selected)
//            {
//                var mainWindow = Application.Current.MainWindow as MainWindow;
//                if (mainWindow == null) return;

//                var result = MessageBox.Show(
//                    $"Are you sure you want to remove motor type '{selected.Name}'?",
//                    "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

//                if (result != MessageBoxResult.Yes)
//                    return;

//                // RemoveMotorCombo is bound to LeftMdr.MotorTypes directly, so this
//                // item reference can be removed straight from that collection.
//                mainWindow.MainShell.LeftMdr.MotorTypes.Remove(selected);

//                // RightMdr has its own separate collection/instances, so find the
//                // matching item there by name before removing.
//                var rightMatch = mainWindow.MainShell.RightMdr.MotorTypes
//                    .FirstOrDefault(m => m.Name == selected.Name);
//                if (rightMatch != null)
//                    mainWindow.MainShell.RightMdr.MotorTypes.Remove(rightMatch);

//                mainWindow.MainShell.LeftMdr.SaveMotorTypes();
//                mainWindow.MainShell.RightMdr.SaveMotorTypes();

//                MessageBox.Show($"Motor type '{selected.Name}' removed and changes saved.",
//                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            else
//            {
//                MessageBox.Show("Please select a motor type to remove.", "Input Error",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//            }
//        }

//        private void AddButton_Click(object sender, RoutedEventArgs e)
//        {
//            string newMotorName = NameTextBox.Text?.Trim();
//            string gear1 = GearTextBox1.Text?.Trim();

//            if (string.IsNullOrWhiteSpace(newMotorName))
//            {
//                MessageBox.Show("Please enter a valid Motor Name.", "Input Error",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            if (string.IsNullOrWhiteSpace(gear1))
//            {
//                MessageBox.Show("Please enter a valid Gear1.", "Input Error",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            if (Application.Current.MainWindow is MainWindow mainWin)
//            {
//                if (mainWin.MainShell.LeftMdr.MotorTypes.Any(m => m.Name == newMotorName))
//                {
//                    MessageBox.Show($"Motor type '{newMotorName}' already exists.", "Duplicate",
//                        MessageBoxButton.OK, MessageBoxImage.Warning);
//                    return;
//                }

//                mainWin.MainShell.LeftMdr.MotorTypes.Add(new MotorTypeItem(newMotorName, gear1));
//                mainWin.MainShell.RightMdr.MotorTypes.Add(new MotorTypeItem(newMotorName, gear1));

//                mainWin.MainShell.LeftMdr.SaveMotorTypes();
//                mainWin.MainShell.RightMdr.SaveMotorTypes();

//                MessageBox.Show($"Motor type '{newMotorName}', Gear '{gear1}' added and saved permanently!",
//                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

//                NameTextBox.Clear();
//                GearTextBox1.Clear();
//            }
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BLDC_Demo.Models;

namespace BLDC_Demo
{
    public partial class Motor_Type : UserControl
    {
        // Keeps the Add / Remove buttons in sync with the connection state
        private DispatcherTimer _stateTimer;

        public Motor_Type()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                RemoveMotorCombo.ItemsSource = mainWin.MainShell.LeftMdr.MotorTypes;
                RemoveMotorCombo.DisplayMemberPath = "Name"; // show Name, keep MotorTypeItem as SelectedItem
            }

            // Add / Remove are enabled only while connected to an IP
            UpdateButtonState();

            if (_stateTimer == null)
            {
                _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                _stateTimer.Tick += (s, args) => UpdateButtonState();
            }
            _stateTimer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _stateTimer?.Stop();
        }

        private static bool IsDeviceConnected()
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            return mainWin?.BLLogic != null && mainWin.BLLogic.IsConnected;
        }

        // Enable / disable every Button in this control (the Add and Remove buttons),
        // found through the visual tree so no x:Name is needed.
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

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected())
            {
                UpdateButtonState();
                return;
            }

            if (RemoveMotorCombo.SelectedItem is MotorTypeItem selected)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null) return;

                var result = MessageBox.Show(
                    $"Are you sure you want to remove motor type '{selected.Name}'?",
                    "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // RemoveMotorCombo is bound to LeftMdr.MotorTypes directly, so this
                // item reference can be removed straight from that collection.
                mainWindow.MainShell.LeftMdr.MotorTypes.Remove(selected);

                // RightMdr has its own separate collection/instances, so find the
                // matching item there by name before removing.
                var rightMatch = mainWindow.MainShell.RightMdr.MotorTypes
                    .FirstOrDefault(m => m.Name == selected.Name);
                if (rightMatch != null)
                    mainWindow.MainShell.RightMdr.MotorTypes.Remove(rightMatch);

                mainWindow.MainShell.LeftMdr.SaveMotorTypes();
                mainWindow.MainShell.RightMdr.SaveMotorTypes();

                MessageBox.Show($"Motor type '{selected.Name}' removed and changes saved.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a motor type to remove.", "Input Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected())
            {
                UpdateButtonState();
                return;
            }

            string enteredName = NameTextBox.Text?.Trim();
            string gear1 = GearTextBox1.Text?.Trim();

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                MessageBox.Show("Please enter a valid Motor Name.", "Input Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(gear1))
            {
                MessageBox.Show("Please enter a valid Gear1.", "Input Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Final name = entered name + " x" + gear
            // e.g. name "bl 45" and gear "23"  ->  "bl 45 x23"
            string newMotorName = $"{enteredName} x {gear1}";

            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                if (mainWin.MainShell.LeftMdr.MotorTypes.Any(m => m.Name == newMotorName))
                {
                    MessageBox.Show($"Motor type '{newMotorName}' already exists.", "Duplicate",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                mainWin.MainShell.LeftMdr.MotorTypes.Add(new MotorTypeItem(newMotorName, gear1));
                mainWin.MainShell.RightMdr.MotorTypes.Add(new MotorTypeItem(newMotorName, gear1));

                mainWin.MainShell.LeftMdr.SaveMotorTypes();
                mainWin.MainShell.RightMdr.SaveMotorTypes();

                MessageBox.Show($"Motor type '{newMotorName}' added and saved permanently!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                NameTextBox.Clear();
                GearTextBox1.Clear();
            }
        }
    }
}