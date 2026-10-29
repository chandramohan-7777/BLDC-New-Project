
using System.Collections.ObjectModel;
using System.ComponentModel;
using BLDC_Demo.Models;
namespace BLDC_Demo.Models
{
    public class SubNode : INotifyPropertyChanged
    {
        private string _ipAddress;
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(nameof(IpAddress)); }
        }

        // FIX: Added backing fields and OnPropertyChanged for Discovery data
        private string _macAddress;
        public string MacAddress
        {
            get => _macAddress;
            set { _macAddress = value; OnPropertyChanged(nameof(MacAddress)); }
        }
        private string _stationName = "";
        public string StationName
        {
            get => _stationName;
            set
            {
                if (_stationName == value) return;
                _stationName = value;
                OnPropertyChanged(nameof(StationName));
            }
        }
        private string _seqId;
        public string SeqId
        {
            get => _seqId;
            set { _seqId = value; OnPropertyChanged(nameof(SeqId)); }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private bool _isActive = false;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }
        private int _leftBrakeIndex;
        public int LeftBrakeIndex
        {
            get => _leftBrakeIndex;
            set { _leftBrakeIndex = value; OnPropertyChanged(nameof(LeftBrakeIndex)); }
        }

        private int _rightMotorHallTypeIndex;
        public int RightMotorHallTypeIndex
        {
            get => _rightMotorHallTypeIndex;
            set { _rightMotorHallTypeIndex = value; OnPropertyChanged(nameof(RightMotorHallTypeIndex)); }
        }
        private int _leftMotorHallTypeIndex;
        public int LeftMotorHallTypeIndex
        {
            get => _leftMotorHallTypeIndex;
            set { _leftMotorHallTypeIndex = value; OnPropertyChanged(nameof(LeftMotorHallTypeIndex)); }
        }

        private int _rightBrakeIndex;
        public int RightBrakeIndex
        {
            get => _rightBrakeIndex;
            set { _rightBrakeIndex = value; OnPropertyChanged(nameof(RightBrakeIndex)); }
        }
        // Inside BLDC_Demo.Models.SubNode class

        private string _firmwareVersion = "0.0.0";
        public string FirmwareVersion
        {
            get => _firmwareVersion;
            set { _firmwareVersion = value; OnPropertyChanged(nameof(FirmwareVersion)); }
        }

        private string _hardwareRevision = "0.0.0";
        public string HardwareRevision
        {
            get => _hardwareRevision;
            set { _hardwareRevision = value; OnPropertyChanged(nameof(HardwareRevision)); }
        }

        private string _serialNumber = "N/A";
        public string SerialNumber
        {
            get => _serialNumber;
            set { _serialNumber = value; OnPropertyChanged(nameof(SerialNumber)); }
        }

        private string _deviceStatus = "Initial";
        public string DeviceStatus
        {
            get => _deviceStatus;
            set { _deviceStatus = value; OnPropertyChanged(nameof(DeviceStatus)); }

        }
        private string _deviceID = "Initial";
        public string DeviceID
        {
            get => _deviceID;
            set { _deviceID = value; OnPropertyChanged(nameof(DeviceID)); }
        }

        private string _node1 = "0";
        public string Node1
        {
            get => _node1;
            set { _node1 = value; OnPropertyChanged(nameof(Node1)); }
        }

        private string _node2 = "0";
        public string Node2
        {
            get => _node2;
            set { _node2 = value; OnPropertyChanged(nameof(Node2)); }
        }
        // ==========================================
        // --- LEFT MOTOR PROPERTIES ---
        // ==========================================
        private string _leftSpeed = "";
        public string LeftSpeed { get => _leftSpeed; set { _leftSpeed = value; OnPropertyChanged(nameof(LeftSpeed)); } }

        private string _leftAcceleration = "";
        public string LeftAcceleration { get => _leftAcceleration; set { _leftAcceleration = value; OnPropertyChanged(nameof(LeftAcceleration)); } }

        private string _leftDirection = "CW";
        public string LeftDirection { get => _leftDirection; set { _leftDirection = value; OnPropertyChanged(nameof(LeftDirection)); } }

        private string _leftDeceleration = "";
        public string LeftDeceleration { get => _leftDeceleration; set { _leftDeceleration = value; OnPropertyChanged(nameof(LeftDeceleration)); } }

        private string _leftCurrent = "";
        public string LeftCurrent { get => _leftCurrent; set { _leftCurrent = value; OnPropertyChanged(nameof(LeftCurrent)); } }

        // --- LEFT MOTOR ERROR FLAGS ---
        private bool _leftHallSensorError;
        public bool LeftHallSensorError { get => _leftHallSensorError; set { _leftHallSensorError = value; OnPropertyChanged(nameof(LeftHallSensorError)); } }

        private bool _leftOverCurrent;
        public bool LeftOverCurrent { get => _leftOverCurrent; set { _leftOverCurrent = value; OnPropertyChanged(nameof(LeftOverCurrent)); } }

        private bool _leftOverVoltage;
        public bool LeftOverVoltage { get => _leftOverVoltage; set { _leftOverVoltage = value; OnPropertyChanged(nameof(LeftOverVoltage)); } }

        private bool _leftUnderVoltage;
        public bool LeftUnderVoltage { get => _leftUnderVoltage; set { _leftUnderVoltage = value; OnPropertyChanged(nameof(LeftUnderVoltage)); } }

        private bool _leftMotorStalled;
        public bool LeftMotorStalled { get => _leftMotorStalled; set { _leftMotorStalled = value; OnPropertyChanged(nameof(LeftMotorStalled)); } }

        private bool _leftOverTemperature;
        public bool LeftOverTemperature { get => _leftOverTemperature; set { _leftOverTemperature = value; OnPropertyChanged(nameof(LeftOverTemperature)); } }
        // --- BRAKE STATUS PROPERTIES ---



        public void ClearMemory()
        {
            // Wipe all identification
            this.DeviceID = string.Empty;
           // this.FirmwareVersion = string.Empty;
            this.HardwareRevision = string.Empty;
            this.DeviceStatus = string.Empty;
            this.SerialNumber = string.Empty;
            this.Node1 = string.Empty;
            this.Node2 = string.Empty;
            // Reset Motor Data
            this.LeftSpeed = string.Empty;
            this.RightSpeed = string.Empty;
            this.LeftCurrent = string.Empty;
            this.RightCurrent = string.Empty;
            this.LeftDeceleration = string.Empty;
            this.RightDeceleration = string.Empty;
            this.LeftAcceleration = string.Empty;
            this.RightAcceleration = string.Empty;
            this.LeftMotorHallTypeIndex = -1;
            this.RightMotorHallTypeIndex = -1;
            this.LeftBrakeIndex = -1;
            this.RightBrakeIndex = -1;
            // Reset Flags
            this.IsActive = false;
            this.LeftOverCurrent = false;
            this.RightOverCurrent = false;

            // This tells WPF that the data has changed to "Empty"
            OnPropertyChanged(string.Empty);
        }
        /// <summary>
        /// Parses the raw 16-bit status word for the Left Motor (Register 0x003A / 4:0058)
        /// </summary>
        public void ParseLeftStatus(ushort rawStatus)
        {
            // Bitmapping based on documentation
            LeftOverVoltage = (rawStatus & (1 << 6)) != 0;  // bit 06
            LeftUnderVoltage = (rawStatus & (1 << 7)) != 0;  // bit 07
            LeftOverTemperature = (rawStatus & (1 << 8)) != 0;  // bit 08
            LeftOverCurrent = (rawStatus & (1 << 9)) != 0;  // bit 09
            LeftMotorStalled = (rawStatus & (1 << 13)) != 0; // bit 13
            LeftHallSensorError = (rawStatus & (1 << 14)) != 0; // bit 14
        }

        // ==========================================
        // --- RIGHT MOTOR PROPERTIES ---
        // ==========================================
        private string _rightSpeed = string.Empty;
        public string RightSpeed { get => _rightSpeed; set { _rightSpeed = value; OnPropertyChanged(nameof(RightSpeed)); } }

        private string _rightAcceleration = string.Empty;
        public string RightAcceleration { get => _rightAcceleration; set { _rightAcceleration = value; OnPropertyChanged(nameof(RightAcceleration)); } }

        private string _rightDirection = string.Empty;
        public string RightDirection { get => _rightDirection; set { _rightDirection = value; OnPropertyChanged(nameof(RightDirection)); } }

        private string _rightDeceleration = string.Empty;
        public string RightDeceleration { get => _rightDeceleration; set { _rightDeceleration = value; OnPropertyChanged(nameof(RightDeceleration)); } }

        private string _rightCurrent = "0";
        public string RightCurrent { get => _rightCurrent; set { _rightCurrent = value; OnPropertyChanged(nameof(RightCurrent)); } }


        // --- RIGHT MOTOR ERROR FLAGS ---
        private bool _rightHallSensorError;
        public bool RightHallSensorError { get => _rightHallSensorError; set { _rightHallSensorError = value; OnPropertyChanged(nameof(RightHallSensorError)); } }

        private bool _rightOverCurrent;
        public bool RightOverCurrent { get => _rightOverCurrent; set { _rightOverCurrent = value; OnPropertyChanged(nameof(RightOverCurrent)); } }

        private bool _rightOverVoltage;
        public bool RightOverVoltage { get => _rightOverVoltage; set { _rightOverVoltage = value; OnPropertyChanged(nameof(RightOverVoltage)); } }

        private bool _rightUnderVoltage;
        public bool RightUnderVoltage { get => _rightUnderVoltage; set { _rightUnderVoltage = value; OnPropertyChanged(nameof(RightUnderVoltage)); } }

        private bool _rightMotorStalled;
        public bool RightMotorStalled { get => _rightMotorStalled; set { _rightMotorStalled = value; OnPropertyChanged(nameof(RightMotorStalled)); } }

        private bool _rightOverTemperature;
        public bool RightOverTemperature { get => _rightOverTemperature; set { _rightOverTemperature = value; OnPropertyChanged(nameof(RightOverTemperature)); } }

        /// <summary>
        /// Parses the raw 16-bit status word for the Right Motor (Register 0x0052 / 4:0082)
        /// </summary>
        public void ParseRightStatus(ushort rawStatus)
        {
            // Bitmapping based on documentation
            RightOverVoltage = (rawStatus & (1 << 6)) != 0;  // bit 06
            RightUnderVoltage = (rawStatus & (1 << 7)) != 0;  // bit 07
            RightOverTemperature = (rawStatus & (1 << 8)) != 0;  // bit 08
            RightOverCurrent = (rawStatus & (1 << 9)) != 0;  // bit 09
            RightMotorStalled = (rawStatus & (1 << 13)) != 0; // bit 13
            RightHallSensorError = (rawStatus & (1 << 14)) != 0; // bit 14
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ModbusDevice : INotifyPropertyChanged
    {
        public string PortName { get; set; }

        private bool _isActive = false;
        public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(nameof(IsActive)); } }

        private bool _hasDeviceInfo = false;
        public bool HasDeviceInfo { get => _hasDeviceInfo; set { _hasDeviceInfo = value; OnPropertyChanged(nameof(HasDeviceInfo)); } }

        // ── Moved here from SubNode ──
        // No more "node" concept: the device itself now holds the
        // identification + telemetry data directly.

        private string _ipAddress;
        public string IpAddress { get => _ipAddress; set { _ipAddress = value; OnPropertyChanged(nameof(IpAddress)); } }

        private string _macAddress;
        public string MacAddress { get => _macAddress; set { _macAddress = value; OnPropertyChanged(nameof(MacAddress)); } }
        private string _stationName = "";
        public string StationName
        {
            get => _stationName;
            set
            {
                if (_stationName == value) return;
                _stationName = value;
                OnPropertyChanged(nameof(StationName));
            }
        }


        private string _seqId;
        public string SeqId { get => _seqId; set { _seqId = value; OnPropertyChanged(nameof(SeqId)); } }

        private string _status;
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        private int _leftBrakeIndex;
        public int LeftBrakeIndex { get => _leftBrakeIndex; set { _leftBrakeIndex = value; OnPropertyChanged(nameof(LeftBrakeIndex)); } }

        private int _rightMotorHallTypeIndex;
        public int RightMotorHallTypeIndex { get => _rightMotorHallTypeIndex; set { _rightMotorHallTypeIndex = value; OnPropertyChanged(nameof(RightMotorHallTypeIndex)); } }

        private int _leftMotorHallTypeIndex;
        public int LeftMotorHallTypeIndex { get => _leftMotorHallTypeIndex; set { _leftMotorHallTypeIndex = value; OnPropertyChanged(nameof(LeftMotorHallTypeIndex)); } }

        private int _rightBrakeIndex;
        public int RightBrakeIndex { get => _rightBrakeIndex; set { _rightBrakeIndex = value; OnPropertyChanged(nameof(RightBrakeIndex)); } }

        private string _firmwareVersion = "0.0.0";
        public string FirmwareVersion { get => _firmwareVersion; set { _firmwareVersion = value; OnPropertyChanged(nameof(FirmwareVersion)); } }

        private string _hardwareRevision = "0.0.0";
        public string HardwareRevision { get => _hardwareRevision; set { _hardwareRevision = value; OnPropertyChanged(nameof(HardwareRevision)); } }

        private string _serialNumber = "N/A";
        public string SerialNumber { get => _serialNumber; set { _serialNumber = value; OnPropertyChanged(nameof(SerialNumber)); } }

        private string _deviceStatus = "Initial";
        public string DeviceStatus { get => _deviceStatus; set { _deviceStatus = value; OnPropertyChanged(nameof(DeviceStatus)); } }

        private string _deviceID = "Initial";
        public string DeviceID { get => _deviceID; set { _deviceID = value; OnPropertyChanged(nameof(DeviceID)); } }

        private string _upstreamIP = "";
        public string UpStreamIP { get => _upstreamIP; set { _upstreamIP = value; OnPropertyChanged(nameof(_upstreamIP)); } }

        private string _downstreamIP = "";
        public string DownStreamIP { get => _downstreamIP; set { _downstreamIP = value; OnPropertyChanged(nameof(_downstreamIP)); } }

        // --- LEFT MOTOR PROPERTIES ---
        private string _leftSpeed = "";
        public string LeftSpeed { get => _leftSpeed; set { _leftSpeed = value; OnPropertyChanged(nameof(LeftSpeed)); } }

        private string _leftAcceleration = "";
        public string LeftAcceleration { get => _leftAcceleration; set { _leftAcceleration = value; OnPropertyChanged(nameof(LeftAcceleration)); } }

        private string _leftDirection = "CW";
        public string LeftDirection { get => _leftDirection; set { _leftDirection = value; OnPropertyChanged(nameof(LeftDirection)); } }

        private string _leftDeceleration = "";
        public string LeftDeceleration { get => _leftDeceleration; set { _leftDeceleration = value; OnPropertyChanged(nameof(LeftDeceleration)); } }

        private string _leftCurrent = "";
        public string LeftCurrent { get => _leftCurrent; set { _leftCurrent = value; OnPropertyChanged(nameof(LeftCurrent)); } }

        // --- LEFT MOTOR ERROR FLAGS ---
        private int _leftHallSensorError=-1;
        public int LeftHallSensorError { get => _leftHallSensorError; set { _leftHallSensorError = value; OnPropertyChanged(nameof(LeftHallSensorError)); } }

        private int _leftOverCurrent=-1;
        public int LeftOverCurrent { get => _leftOverCurrent; set { _leftOverCurrent = value; OnPropertyChanged(nameof(LeftOverCurrent)); } }

        private int _leftOverVoltage=-1;
        public int LeftOverVoltage { get => _leftOverVoltage; set { _leftOverVoltage = value; OnPropertyChanged(nameof(LeftOverVoltage)); } }

        private int _leftLowVoltage= -1;
        public int LeftLowVoltage { get => _leftLowVoltage; set { _leftLowVoltage = value; OnPropertyChanged(nameof(LeftLowVoltage)); } }

        private int _leftsensor1=-1;
        public int LeftSensor1 { get => _leftsensor1; set { _leftsensor1 = value; OnPropertyChanged(nameof(LeftSensor1)); } }
        private int _leftsensor2=-1;
        public int LeftSensor2 { get => _leftsensor2; set { _leftsensor2 = value; OnPropertyChanged(nameof(LeftSensor2)); } }

        private bool _leftOverTemperature ;
        public bool LeftOverTemperature { get => _leftOverTemperature; set { _leftOverTemperature = value; OnPropertyChanged(nameof(LeftOverTemperature)); } }

        // --- RIGHT MOTOR PROPERTIES ---
        private string _rightSpeed = string.Empty;
        public string RightSpeed { get => _rightSpeed; set { _rightSpeed = value; OnPropertyChanged(nameof(RightSpeed)); } }

        private string _rightAcceleration = string.Empty;
        public string RightAcceleration { get => _rightAcceleration; set { _rightAcceleration = value; OnPropertyChanged(nameof(RightAcceleration)); } }

        private string _rightDirection = string.Empty;
        public string RightDirection { get => _rightDirection; set { _rightDirection = value; OnPropertyChanged(nameof(RightDirection)); } }

        private string _rightDeceleration = string.Empty;
        public string RightDeceleration { get => _rightDeceleration; set { _rightDeceleration = value; OnPropertyChanged(nameof(RightDeceleration)); } }

        private string _rightCurrent = "";
        public string RightCurrent { get => _rightCurrent; set { _rightCurrent = value; OnPropertyChanged(nameof(RightCurrent)); } }

        // --- RIGHT MOTOR ERROR FLAGS ---
        private int _rightHallSensorError=-1;
        public int RightHallSensorError { get => _rightHallSensorError; set { _rightHallSensorError = value; OnPropertyChanged(nameof(RightHallSensorError)); } }

        private int _rightOverCurrent=-1;
        public int RightOverCurrent { get => _rightOverCurrent; set { _rightOverCurrent = value; OnPropertyChanged(nameof(RightOverCurrent)); } }

        private int _rightOverVoltage=-1;
        public int RightOverVoltage { get => _rightOverVoltage; set { _rightOverVoltage = value; OnPropertyChanged(nameof(RightOverVoltage)); } }

        private int _rightLowVoltage =-1;
        public int RightLowVoltage { get => _rightLowVoltage; set { _rightLowVoltage = value; OnPropertyChanged(nameof(RightLowVoltage)); } }

        private int _rightMotorStalled=-1;
        public int RightMotorStalled { get => _rightMotorStalled; set { _rightMotorStalled = value; OnPropertyChanged(nameof(RightMotorStalled)); } }
        private int _rightsensor1=-1;
        public int RightSensor1 { get => _rightsensor1; set { _rightsensor1 = value; OnPropertyChanged(nameof(RightSensor1)); } }
        private int _rightsensor2=-1;
        public int RightSensor2 { get => _rightsensor2; set { _rightsensor2 = value; OnPropertyChanged(nameof(RightSensor2)); } }

        private int _rightOverTemperature=-1;
        public int RightOverTemperature { get => _rightOverTemperature; set { _rightOverTemperature = value; OnPropertyChanged(nameof(RightOverTemperature)); } }

        // ZPA status register (0x0040): 0 = inactive, 1 = active.
        // Updated live by BL.ParseAndUpdateUI so the UI can poll it.
        private ushort _zpaMode;
        public ushort ZpaMode { get => _zpaMode; set { _zpaMode = value; OnPropertyChanged(nameof(ZpaMode)); } }

                // Kept for compatibility with any other file that still references
        // ConnectedNodes / SubNode. Not used by BL.cs anymore.
        public ObservableCollection<SubNode> ConnectedNodes { get; } = new ObservableCollection<SubNode>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Reset() { IsActive = false; }

        public void ClearMemory()
        {
            this.DeviceID = string.Empty;
           // this.FirmwareVersion = string.Empty;
            this.HardwareRevision = string.Empty;
            this.DeviceStatus = string.Empty;
            this.SerialNumber = string.Empty;
            this.UpStreamIP = string.Empty;
            this.DownStreamIP = string.Empty;
            this.LeftSpeed = string.Empty;
            this.RightSpeed = string.Empty;
            this.LeftCurrent = string.Empty;
            this.RightCurrent = string.Empty;
            this.LeftDeceleration = string.Empty;
            this.RightDeceleration = string.Empty;
            this.LeftAcceleration = string.Empty;
            this.RightAcceleration = string.Empty;
            this.LeftMotorHallTypeIndex = -1;
            this.RightMotorHallTypeIndex = -1;
            this.LeftBrakeIndex = -1;
            this.RightBrakeIndex = -1;
            this.IsActive = false;
            this.LeftOverCurrent = -1;
            this.RightOverCurrent = -1;
            this.ZpaMode = 0;
            OnPropertyChanged(string.Empty);
        }
    }
}