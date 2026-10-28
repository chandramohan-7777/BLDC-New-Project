
////using System;
////using System.Collections.Concurrent;
////using System.Diagnostics;
////using System.Threading;
////using System.Windows;
////using BLDC_Demo.Models;

////namespace BLDC_Demo.Controls
////{
////    public class BL
////    {
////        public ModbusDevice CurrentDevice { get; private set; }

////        public string ActivePortName { get; private set; } = string.Empty;
////        private IComm _comm;

////        public event Action OnDisconnected;

////        public bool IsConnected => _comm != null && _comm.IsOpen;


////        private const int MaxReconnectAttempts = 5;
////        private static readonly TimeSpan[] ReconnectDelays =
////        {
////            TimeSpan.FromSeconds(1),
////            TimeSpan.FromSeconds(2),
////            TimeSpan.FromSeconds(3),
////            TimeSpan.FromSeconds(5),
////            TimeSpan.FromSeconds(5)
////        };

////        private volatile bool _userInitiatedClose;
////        private string _lastCommFactoryIp;
////        private int _lastCommFactoryPort;
////        private Func<string, int, IComm> _commFactory;

////        private readonly ConcurrentQueue<Action> _priorityQueue = new ConcurrentQueue<Action>();
////        private readonly ConcurrentQueue<Action> _normalQueue = new ConcurrentQueue<Action>();
////        private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);
////        private Thread _ioThread;
////        private volatile bool _running;

////        public bool Start(IComm c)
////        {
////            _userInitiatedClose = false;
////            return StartInternal(c);
////        }

////        // Overload that also remembers how to rebuild the same kind of
////        // connection later, so auto-reconnect can create a fresh IComm
////        // instead of reusing a dead/closed one.
////        public bool Start(string ip, int port, Func<string, int, IComm> commFactory)
////        {
////            _userInitiatedClose = false;
////            _commFactory = commFactory;
////            _lastCommFactoryIp = ip;
////            _lastCommFactoryPort = port;
////            return StartInternal(commFactory(ip, port));
////        }

////        private bool StartInternal(IComm c)
////        {

////            StopIoThread();

////            _comm = c;

////            if (_comm is CommEthernet tcp)
////            {
////                tcp.TelemetryReceived += (payload) =>
////                {
////                    if (payload == null || payload.Length < 6) return;

////                    Debug.WriteLine($"[RAW PAYLOAD] Length={payload.Length}  Bytes={BitConverter.ToString(payload)}");

////                    if (CurrentDevice != null)
////                    {
////                        ParseAndUpdateUI(payload, CurrentDevice);
////                    }
////                };

////                tcp.ConnectionLost += HandleConnectionLost;
////            }

////            if (_comm.Open())
////            {
////                ActivePortName = _comm.Name;

////                Application.Current.Dispatcher.Invoke(() =>
////                {
////                    CurrentDevice = new ModbusDevice
////                    {
////                        PortName = ActivePortName,
////                        IpAddress = _comm.Name.Split(':')[0],
////                        Status = "ONLINE",
////                        IsActive = true
////                    };

////                    if (Application.Current.MainWindow is MainWindow mainWindow)
////                    {
////                        mainWindow.DataContext = CurrentDevice;
////                        mainWindow.MainShell?.UpdateMdrUI(CurrentDevice);

////                        Debug.WriteLine($"[DATACONTEXT] Switched to device, IsActive={CurrentDevice.IsActive}");
////                    }
////                });

////                // Start the dedicated I/O thread that owns the socket.
////                // At this point StopIoThread() above guarantees there is
////                // no other IoLoop thread still alive.
////                _running = true;
////                _ioThread = new Thread(IoLoop)
////                {
////                    IsBackground = true,
////                    Name = "BL-IO-Thread"
////                };
////                _ioThread.Start();

////                return true;
////            }
////            return false;
////        }


////        private void StopIoThread()
////        {
////            if (_ioThread == null) return;

////            _running = false;
////            _queueSignal.Set();
////            _ioThread.Join(500);
////            _ioThread = null;

////            while (_priorityQueue.TryDequeue(out _)) { }
////            while (_normalQueue.TryDequeue(out _)) { }
////        }

////        // Called when CommEthernet's ConnectionLost fires — i.e. the read
////        // loop hit an exception/timeout on its own, NOT because the user
////        // clicked Disconnect. Attempts a few automatic reconnects before
////        // giving up and notifying the UI.
////        private void HandleConnectionLost()
////        {
////            if (_userInitiatedClose) return;   // user clicked Disconnect — don't reconnect
////            if (_commFactory == null)
////            {
////                // No factory was supplied (Start(IComm) was used directly),
////                // so we can't rebuild the connection — fall back to a normal close.
////                Close();
////                return;
////            }

////            ThreadPool.QueueUserWorkItem(_ => TryAutoReconnect());
////        }

////        private void TryAutoReconnect()
////        {
////            for (int attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
////            {
////                if (_userInitiatedClose) return;

////                var delay = ReconnectDelays[Math.Min(attempt - 1, ReconnectDelays.Length - 1)];
////                Debug.WriteLine($"[BL RECONNECT] Attempt {attempt}/{MaxReconnectAttempts} in {delay.TotalSeconds}s...");
////                Thread.Sleep(delay);

////                if (_userInitiatedClose) return;

////                try
////                {
////                    var freshComm = _commFactory(_lastCommFactoryIp, _lastCommFactoryPort);
////                    if (StartInternal(freshComm))
////                    {
////                        Debug.WriteLine("[BL RECONNECT] Success.");
////                        return;
////                    }
////                }
////                catch (Exception ex)
////                {
////                    Debug.WriteLine($"[BL RECONNECT] Attempt {attempt} failed: {ex.Message}");
////                }
////            }

////            Debug.WriteLine("[BL RECONNECT] Giving up after max attempts.");
////            Close();
////        }


////        private void IoLoop()
////        {
////            while (_running)
////            {
////                _queueSignal.WaitOne(200);

////                // Drain ALL pending writes first, every loop iteration.
////                while (_running && _priorityQueue.TryDequeue(out var writeAction))
////                {
////                    try { writeAction(); }
////                    catch (Exception ex) { Debug.WriteLine($"[BL WRITE ERROR] {ex.Message}"); }
////                }

////                if (!_running) break;

////                if (_normalQueue.TryDequeue(out var pollAction))
////                {
////                    try { pollAction(); }
////                    catch (Exception ex) { Debug.WriteLine($"[BL POLL ERROR] {ex.Message}"); }
////                }
////            }
////        }

////        // Called by the polling timer. Non-blocking — just enqueues.
////        public void UpdateData()
////        {
////            if (!IsConnected) return;

////            // Avoid piling up stale poll requests if the loop is busy.
////            if (!_normalQueue.IsEmpty) return;

////            _normalQueue.Enqueue(() => SendTcpFrame(0x01, 0xFF, new byte[] { 0x00, 0x00 }));
////            _queueSignal.Set();
////        }

////        public void SendWriteCommand(ushort registerAddress, ushort value)
////        {
////            if (!IsConnected) return;

////            byte[] data = {
////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
////                (byte)(value >> 8), (byte)(value & 0xFF)
////            };

////            _priorityQueue.Enqueue(() => SendTcpFrame(0x01, 0x06, data));
////            _queueSignal.Set();
////        }

////        public void ParseAndUpdateUI(byte[] payload, ModbusDevice device)
////        {
////            if (payload.Length < 6) return;

////            ushort regAddr = (ushort)((payload[2] << 8) | payload[3]);
////            ushort val = (ushort)((payload[4] << 8) | payload[5]);

////            Application.Current.Dispatcher.Invoke(() =>
////            {
////                switch (regAddr)
////                {
////                    case 0x0020:
////                        device.DeviceID = val.ToString();
////                        break;
////                    case 0x0021:
////                        device.FirmwareVersion = val.ToString();
////                        break;
////                    case 0x0022:
////                        device.HardwareRevision = val.ToString();
////                        break;
////                    case 0x0023:
////                        device.DeviceStatus = val.ToString();
////                        break;
////                    case 0x0024:
////                        device.Node1 = val.ToString();
////                        break;
////                    case 0x0025:
////                        device.Node2 = val.ToString();
////                        break;
////                    case 0x0026:
////                        device.SerialNumber = val.ToString();
////                        break;
////                    case 0x0027: // Left Speed
////                        device.LeftSpeed = val.ToString();
////                        break;
////                    case 0x002A: // Left Acceleration
////                        device.LeftAcceleration = val.ToString();
////                        break;
////                    case 0x002B: // Left Deceleration
////                        device.LeftDeceleration = val.ToString();
////                        break;
////                    case 0x002F: // Left Direction
////                        device.LeftDirection = (val == 1) ? "CCW" : "CW";
////                        break;
////                    case 0x0036: // Left Current
////                        device.LeftCurrent = (val / 10.0).ToString("F1");
////                        break;
////                    case 0x002E: // Left Brake Index
////                        device.LeftBrakeIndex = val;
////                        break;
////                    case 0x0001: // Left Hall Type
////                        device.LeftMotorHallTypeIndex = val;
////                        break;
////                    case 0x003A: // LEFT STATUS WORD (Bitmask)
////                        device.LeftOverVoltage = (val & (1 << 6)) != 0;
////                        device.LeftUnderVoltage = (val & (1 << 7)) != 0;
////                        device.LeftOverTemperature = (val & (1 << 8)) != 0;
////                        device.LeftOverCurrent = (val & (1 << 9)) != 0;
////                        device.LeftMotorStalled = (val & (1 << 13)) != 0;
////                        device.LeftHallSensorError = (val & (1 << 14)) != 0;
////                        break;

////                    // ================== RIGHT MOTOR ==================
////                    case 0x003F: // Right Speed
////                        device.RightSpeed = val.ToString();
////                        break;
////                    case 0x0042: // Right Acceleration
////                        device.RightAcceleration = val.ToString();
////                        break;
////                    case 0x0043: // Right Deceleration
////                        device.RightDeceleration = val.ToString();
////                        break;
////                    case 0x0047: // Right Direction
////                        device.RightDirection = (val == 1) ? "CCW" : "CW";
////                        break;
////                    case 0x004E: // Right Current
////                        device.RightCurrent = (val / 10.0).ToString("F1");
////                        break;
////                    case 0x0046: // Right Brake Index
////                        device.RightBrakeIndex = val;
////                        break;
////                    case 0x0002: // Right Hall Type
////                        device.RightMotorHallTypeIndex = val;
////                        break;
////                    case 0x0052: // RIGHT STATUS WORD (Bitmask)
////                        device.RightOverVoltage = (val & (1 << 6)) != 0;
////                        device.RightUnderVoltage = (val & (1 << 7)) != 0;
////                        device.RightOverTemperature = (val & (1 << 8)) != 0;
////                        device.RightOverCurrent = (val & (1 << 9)) != 0;
////                        device.RightMotorStalled = (val & (1 << 13)) != 0;
////                        device.RightHallSensorError = (val & (1 << 14)) != 0;
////                        break;

////                    default:
////                       // Debug.WriteLine($"[UNKNOWN REGISTER] 0x{regAddr:X4} = {val}");
////                        break;
////                }
////            });
////        }

////        // Only ever called from the I/O thread now — no locking needed,
////        // since StopIoThread()/StartInternal() guarantee exactly one
////        // thread touches _comm at a time.
////        private void SendTcpFrame(byte slaveId, byte functionCode, byte[] data)
////        {
////            byte[] pdu = new byte[2 + data.Length];
////            pdu[0] = slaveId;
////            pdu[1] = functionCode;
////            Array.Copy(data, 0, pdu, 2, data.Length);

////            byte[] tcpFrame = new byte[6 + pdu.Length];
////            tcpFrame[0] = 0x00; tcpFrame[1] = 0x01;
////            tcpFrame[2] = 0x00; tcpFrame[3] = 0x00;
////            tcpFrame[4] = (byte)(pdu.Length >> 8);
////            tcpFrame[5] = (byte)(pdu.Length & 0xFF);
////            Array.Copy(pdu, 0, tcpFrame, 6, pdu.Length);

////            _comm.SendRecv(tcpFrame);
////        }

////        public void Close()
////        {
////            _userInitiatedClose = true;

////            if (_comm != null)
////            {
////                StopIoThread();

////                if (_comm is CommEthernet tcp)
////                    tcp.ConnectionLost -= HandleConnectionLost;

////                _comm.Close();
////                _comm = null;
////                ActivePortName = string.Empty;

////                if (CurrentDevice != null)
////                {
////                    Application.Current.Dispatcher.Invoke(() =>
////                    {
////                        CurrentDevice.IsActive = false;
////                    });
////                }
////                CurrentDevice = null;

////                OnDisconnected?.Invoke();
////            }
////        }
////    }
////}
////using System;
////using System.Collections.Concurrent;
////using System.Diagnostics;
////using System.Threading;
////using System.Windows;
////using BLDC_Demo.Models;

////namespace BLDC_Demo.Controls
////{
////    public class BL
////    {
////        public ModbusDevice CurrentDevice { get; private set; }

////        public string ActivePortName { get; private set; } = string.Empty;
////        private IComm _comm;

////        public event Action OnDisconnected;

////        public bool IsConnected => _comm != null && _comm.IsOpen;


////        private const int MaxReconnectAttempts = 5;
////        private static readonly TimeSpan[] ReconnectDelays =
////        {
////            TimeSpan.FromSeconds(1),
////            TimeSpan.FromSeconds(2),
////            TimeSpan.FromSeconds(3),
////            TimeSpan.FromSeconds(5),
////            TimeSpan.FromSeconds(5)
////        };

////        private volatile bool _userInitiatedClose;
////        private string _lastCommFactoryIp;
////        private int _lastCommFactoryPort;
////        private Func<string, int, IComm> _commFactory;

////        private readonly ConcurrentQueue<Action> _priorityQueue = new ConcurrentQueue<Action>();
////        private readonly ConcurrentQueue<Action> _normalQueue = new ConcurrentQueue<Action>();
////        private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);
////        private Thread _ioThread;
////        private volatile bool _running;

////        public bool Start(IComm c)
////        {
////            _userInitiatedClose = false;
////            return StartInternal(c);
////        }

////        // Overload that also remembers how to rebuild the same kind of
////        // connection later, so auto-reconnect can create a fresh IComm
////        // instead of reusing a dead/closed one.
////        public bool Start(string ip, int port, Func<string, int, IComm> commFactory)
////        {
////            _userInitiatedClose = false;
////            _commFactory = commFactory;
////            _lastCommFactoryIp = ip;
////            _lastCommFactoryPort = port;
////            return StartInternal(commFactory(ip, port));
////        }

////        private bool StartInternal(IComm c)
////        {

////            StopIoThread(drainPendingWrites: false); // nothing meaningful queued yet for a fresh Start

////            _comm = c;

////            if (_comm is CommEthernet tcp)
////            {
////                tcp.TelemetryReceived += (payload) =>
////                {
////                    if (payload == null || payload.Length < 6) return;

////                    //Debug.WriteLine($"[RAW PAYLOAD] Length={payload.Length}  Bytes={BitConverter.ToString(payload)}");

////                    if (CurrentDevice != null)
////                    {
////                        ParseAndUpdateUI(payload, CurrentDevice);
////                    }
////                };

////                tcp.ConnectionLost += HandleConnectionLost;
////            }

////            if (_comm.Open())
////            {
////                ActivePortName = _comm.Name;

////                Application.Current.Dispatcher.Invoke(() =>
////                {
////                    CurrentDevice = new ModbusDevice
////                    {
////                        PortName = ActivePortName,
////                        IpAddress = _comm.Name.Split(':')[0],
////                        Status = "ONLINE",
////                        IsActive = true
////                    };

////                    if (Application.Current.MainWindow is MainWindow mainWindow)
////                    {
////                        mainWindow.DataContext = CurrentDevice;
////                        mainWindow.MainShell?.UpdateMdrUI(CurrentDevice);

////                        Debug.WriteLine($"[DATACONTEXT] Switched to device, IsActive={CurrentDevice.IsActive}");
////                    }
////                });

////                // Start the dedicated I/O thread that owns the socket.
////                // At this point StopIoThread() above guarantees there is
////                // no other IoLoop thread still alive.
////                _running = true;
////                _ioThread = new Thread(IoLoop)
////                {
////                    IsBackground = true,
////                    Name = "BL-IO-Thread"
////                };
////                _ioThread.Start();

////                return true;
////            }
////            return false;
////        }

////        // drainPendingWrites: when true, gives the IoLoop one last chance to
////        // flush anything sitting in _priorityQueue (e.g. a SET-IP / reboot
////        // command you just enqueued) before the thread is actually torn down.
////        // Previously this always discarded the queue immediately, which could
////        // silently drop the final "commit/reboot" write if Close() was called
////        // right after SendWriteCommand() — a race that made Set-IP look like
////        // it worked in the UI even when the device never received the write.
////        private void StopIoThread(bool drainPendingWrites)
////        {
////            if (_ioThread == null) return;

////            if (drainPendingWrites)
////            {
////                // Give the IO thread a short window to drain whatever's queued
////                // (writes are fire-and-forget over TCP, so this is a best-effort
////                // flush, not a guaranteed ack from the device).
////                var deadline = DateTime.UtcNow.AddMilliseconds(400);
////                while (!_priorityQueue.IsEmpty && DateTime.UtcNow < deadline)
////                {
////                    _queueSignal.Set();
////                    Thread.Sleep(20);
////                }
////            }

////            _running = false;
////            _queueSignal.Set();
////            _ioThread.Join(500);
////            _ioThread = null;

////            while (_priorityQueue.TryDequeue(out _)) { }
////            while (_normalQueue.TryDequeue(out _)) { }
////        }

////        // Called when CommEthernet's ConnectionLost fires — i.e. the read
////        // loop hit an exception/timeout on its own, NOT because the user
////        // clicked Disconnect. Attempts a few automatic reconnects before
////        // giving up and notifying the UI.
////        private void HandleConnectionLost()
////        {
////            if (_userInitiatedClose) return;   // user clicked Disconnect — don't reconnect
////            if (_commFactory == null)
////            {
////                // No factory was supplied (Start(IComm) was used directly),
////                // so we can't rebuild the connection — fall back to a normal close.
////                Close();
////                return;
////            }

////            ThreadPool.QueueUserWorkItem(_ => TryAutoReconnect());
////        }

////        private void TryAutoReconnect()
////        {
////            for (int attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
////            {
////                if (_userInitiatedClose) return;

////                var delay = ReconnectDelays[Math.Min(attempt - 1, ReconnectDelays.Length - 1)];
////                Debug.WriteLine($"[BL RECONNECT] Attempt {attempt}/{MaxReconnectAttempts} in {delay.TotalSeconds}s...");
////                Thread.Sleep(delay);

////                if (_userInitiatedClose) return;

////                try
////                {
////                    var freshComm = _commFactory(_lastCommFactoryIp, _lastCommFactoryPort);
////                    if (StartInternal(freshComm))
////                    {
////                        Debug.WriteLine("[BL RECONNECT] Success.");
////                        return;
////                    }
////                }
////                catch (Exception ex)
////                {
////                    Debug.WriteLine($"[BL RECONNECT] Attempt {attempt} failed: {ex.Message}");
////                }
////            }

////            Debug.WriteLine("[BL RECONNECT] Giving up after max attempts.");
////            Close();
////        }


////        private void IoLoop()
////        {
////            while (_running)
////            {
////                _queueSignal.WaitOne(200);

////                // Drain ALL pending writes first, every loop iteration.
////                while (_priorityQueue.TryDequeue(out var writeAction))
////                {
////                    try { writeAction(); }
////                    catch (Exception ex) { Debug.WriteLine($"[BL WRITE ERROR] {ex.Message}"); }
////                }

////                if (!_running) break;

////                if (_normalQueue.TryDequeue(out var pollAction))
////                {
////                    try { pollAction(); }
////                    catch (Exception ex) { Debug.WriteLine($"[BL POLL ERROR] {ex.Message}"); }
////                }
////            }
////        }

////        // Called by the polling timer. Non-blocking — just enqueues.
////        public void UpdateData()
////        {
////            if (!IsConnected) return;

////            // Avoid piling up stale poll requests if the loop is busy.
////            if (!_normalQueue.IsEmpty) return;

////            _normalQueue.Enqueue(() => SendTcpFrame(0x01, 0xFF, new byte[] { 0x00, 0x00 }));
////            _queueSignal.Set();
////        }

////        // Returns true if the write was actually queued for sending (i.e. we
////        // have a live connection), false if it was dropped because nothing
////        // is connected. Callers MUST check this — previously this method
////        // returned void and silently no-op'd when disconnected, which let
////        // the UI report "IP set" even though nothing was ever sent.
////        public bool SendWriteCommand(ushort registerAddress, ushort value)
////        {
////            if (!IsConnected) return false;

////            byte[] data = {
////                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
////                (byte)(value >> 8), (byte)(value & 0xFF)
////            };

////            _priorityQueue.Enqueue(() => SendTcpFrame(0x01, 0x06, data));
////            _queueSignal.Set();
////            return true;
////        }

////        public void ParseAndUpdateUI(byte[] payload, ModbusDevice device)
////        {
////            if (payload.Length < 6) return;

////            ushort regAddr = (ushort)((payload[2] << 8) | payload[3]);
////            ushort val = (ushort)((payload[4] << 8) | payload[5]);

////            Application.Current.Dispatcher.Invoke(() =>
////            {
////                switch (regAddr)
////                {
////                    case 0x0020:
////                        device.DeviceID = val.ToString();
////                        break;
////                    case 0x0021:
////                        device.FirmwareVersion = val.ToString();
////                        (Application.Current.MainWindow as MainWindow)?.UpdateFirmwareVersion(device.FirmwareVersion);

////                        device.FirmwareVersion = val.ToString();
////                        break;


////                    case 0x0022:
////                        device.HardwareRevision = val.ToString();
////                        break;
////                    case 0x0023:
////                        device.DeviceStatus = val.ToString();
////                        break;
////                    case 0x0024:
////                        device.UpStreamIP = val.ToString();
////                        break;
////                    case 0x0025:
////                        device.DownStreamIP = val.ToString();
////                        break;
////                    case 0x0026:
////                        device.SerialNumber = val.ToString();
////                        break;
////                    case 0x0027: // Left Speed
////                        device.LeftSpeed = val.ToString();
////                        break;
////                    case 0x002A: // Left Acceleration
////                        device.LeftAcceleration = val.ToString();
////                        break;
////                    case 0x002B: // Left Deceleration
////                        device.LeftDeceleration = val.ToString();
////                        break;
////                    case 0x002F: // Left Direction
////                        device.LeftDirection = (val == 1) ? "CCW" : "CW";
////                        break;
////                    case 0x0036: // Left Current
////                        device.LeftCurrent = (val / 10.0).ToString("F1");
////                        break;
////                    case 0x002E: // Left Brake Index
////                        device.LeftBrakeIndex = val;
////                        break;
////                    case 0x0001: // Left Hall Type
////                        device.LeftMotorHallTypeIndex = val;
////                        break;
////                    case 0x003A: // LEFT STATUS WORD (Bitmask)
////                        device.LeftOverVoltage = (val & (1 << 6)) != 0;
////                        device.LeftLowVoltage = (val & (1 << 7)) != 0;

////                        device.LeftOverCurrent = (val & (1 << 8)) != 0;
////                        device.LeftSensor1 = (val & (1 << 9)) != 0;
////                        device.LeftSensor2 = (val & (1 << 10)) != 0;
////                        device.LeftHallSensorError = (val & (1 << 11)) != 0;
////                        break;

////                    // ================== RIGHT MOTOR ==================
////                    case 0x003F: // Right Speed
////                        device.RightSpeed = val.ToString();
////                        break;
////                    case 0x0042: // Right Acceleration
////                        device.RightAcceleration = val.ToString();
////                        break;
////                    case 0x0043: // Right Deceleration
////                        device.RightDeceleration = val.ToString();
////                        break;
////                    case 0x0047: // Right Direction
////                        device.RightDirection = (val == 1) ? "CCW" : "CW";
////                        break;
////                    case 0x004E: // Right Current
////                        device.RightCurrent = (val / 10.0).ToString("F1");
////                        break;
////                    case 0x0046: // Right Brake Index
////                        device.RightBrakeIndex = val;
////                        break;
////                    case 0x0002: // Right Hall Type
////                        device.RightMotorHallTypeIndex = val;
////                        break;
////                    //case 0x0052: // RIGHT STATUS WORD (Bitmask)
////                    //    device.RightOverVoltage = (val & (1 << 6)) != 0;
////                    //    device.RightLowVoltage = (val & (1 << 7)) != 0;
////                    //    device.RightOverTemperature = (val & (1 << 8)) != 0;
////                    //    device.RightOverCurrent = (val & (1 << 9)) != 0;
////                    //    device.RightMotorStalled = (val & (1 << 13)) != 0;
////                    //    device.RightHallSensorError = (val & (1 << 14)) != 0;
////                    //    break;
////                    case 0x0052: // RIGHT STATUS WORD (Bitmask)
////                        device.RightOverVoltage = (val & (1 << 6)) != 0;
////                        device.RightLowVoltage = (val & (1 << 7)) != 0;

////                        device.RightOverCurrent = (val & (1 << 8)) != 0;
////                        device.RightSensor1 = (val & (1 << 9)) != 0;
////                        device.RightSensor2 = (val & (1 << 10)) != 0;
////                        device.RightHallSensorError = (val & (1 << 11)) != 0;
////                        break;

////                    default:
////                       // Debug.WriteLine($"[UNKNOWN REGISTER] 0x{regAddr:X4} = {val}");
////                        break;
////                }
////            });
////        }

////        // Only ever called from the I/O thread now — no locking needed,
////        // since StopIoThread()/StartInternal() guarantee exactly one
////        // thread touches _comm at a time.
////        private void SendTcpFrame(byte slaveId, byte functionCode, byte[] data)
////        {
////            byte[] pdu = new byte[2 + data.Length];
////            pdu[0] = slaveId;
////            pdu[1] = functionCode;
////            Array.Copy(data, 0, pdu, 2, data.Length);

////            byte[] tcpFrame = new byte[6 + pdu.Length];
////            tcpFrame[0] = 0x00; tcpFrame[1] = 0x01;
////            tcpFrame[2] = 0x00; tcpFrame[3] = 0x00;
////            tcpFrame[4] = (byte)(pdu.Length >> 8);
////            tcpFrame[5] = (byte)(pdu.Length & 0xFF);
////            Array.Copy(pdu, 0, tcpFrame, 6, pdu.Length);

////            _comm.SendRecv(tcpFrame);
////        }

////        // Existing signature preserved for any other callers — closes
////        // WITHOUT draining pending writes (original behavior).
////        public void Close() => Close(drainPendingWrites: false);

////        // Use this overload after a burst of SendWriteCommand() calls (e.g.
////        // Set-IP + commit/reboot) so the queued writes get a chance to
////        // actually leave the socket before it's torn down.
////        public void Close(bool drainPendingWrites)
////        {
////            _userInitiatedClose = true;

////            if (_comm != null)
////            {
////                StopIoThread(drainPendingWrites);

////                if (_comm is CommEthernet tcp)
////                    tcp.ConnectionLost -= HandleConnectionLost;

////                _comm.Close();
////                _comm = null;
////                ActivePortName = string.Empty;

////                if (CurrentDevice != null)
////                {
////                    Application.Current.Dispatcher.Invoke(() =>
////                    {
////                        CurrentDevice.IsActive = false;
////                    });
////                }
////                CurrentDevice = null;

////                OnDisconnected?.Invoke();
////            }
////        }
////    }
////}
//using System;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.Threading;
//using System.Windows;
//using BLDC_Demo.Models;

//namespace BLDC_Demo.Controls
//{
//    public class BL
//    {
//        public ModbusDevice CurrentDevice { get; private set; }

//        public string ActivePortName { get; private set; } = string.Empty;
//        private IComm _comm;
//        private const ushort NameBase = 0x0150;
//        private const int NameRegCount = 16;
//        private readonly ushort[] _nameBuf = new ushort[NameRegCount];
//        public event Action OnDisconnected;

//        // Fires the INSTANT a drop is detected (before any reconnect
//        // attempts run), so the UI can flip to "NOT CONNECTED" right away
//        // instead of waiting out the whole ~16s auto-reconnect sequence.
//        // Also re-fired on a successful auto-reconnect so the UI flips
//        // back to "CONNECTED" without waiting for the next poll tick.
//        public event Action OnConnectionLost;

//        public bool IsConnected => _comm != null && _comm.IsOpen;


//        private const int MaxReconnectAttempts = 5;
//        private static readonly TimeSpan[] ReconnectDelays =
//        {
//            TimeSpan.FromSeconds(1),
//            TimeSpan.FromSeconds(2),
//            TimeSpan.FromSeconds(3),
//            TimeSpan.FromSeconds(5),
//            TimeSpan.FromSeconds(5)
//        };

//        private volatile bool _userInitiatedClose;
//        private string _lastCommFactoryIp;
//        private int _lastCommFactoryPort;
//        private Func<string, int, IComm> _commFactory;

//        private readonly ConcurrentQueue<Action> _priorityQueue = new ConcurrentQueue<Action>();
//        private readonly ConcurrentQueue<Action> _normalQueue = new ConcurrentQueue<Action>();
//        private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);
//        private Thread _ioThread;
//        private volatile bool _running;

//        public bool Start(IComm c)
//        {
//            _userInitiatedClose = false;
//            return StartInternal(c);
//        }

//        // Overload that also remembers how to rebuild the same kind of
//        // connection later, so auto-reconnect can create a fresh IComm
//        // instead of reusing a dead/closed one.
//        public bool Start(string ip, int port, Func<string, int, IComm> commFactory)
//        {
//            _userInitiatedClose = false;
//            _commFactory = commFactory;
//            _lastCommFactoryIp = ip;
//            _lastCommFactoryPort = port;
//            return StartInternal(commFactory(ip, port));
//        }

//        private bool StartInternal(IComm c)
//        {

//            StopIoThread(drainPendingWrites: false); // nothing meaningful queued yet for a fresh Start

//            _comm = c;

//            if (_comm is CommEthernet tcp)
//            {
//                tcp.TelemetryReceived += (payload) =>
//                {
//                    if (payload == null || payload.Length < 6) return;

//                    //Debug.WriteLine($"[RAW PAYLOAD] Length={payload.Length}  Bytes={BitConverter.ToString(payload)}");

//                    if (CurrentDevice != null)
//                    {
//                        ParseAndUpdateUI(payload, CurrentDevice);
//                    }
//                };

//                tcp.ConnectionLost += HandleConnectionLost;
//            }

//            if (_comm.Open())
//            {
//                ActivePortName = _comm.Name;

//                Application.Current.Dispatcher.Invoke(() =>
//                {
//                    CurrentDevice = new ModbusDevice
//                    {
//                        PortName = ActivePortName,
//                        IpAddress = _comm.Name.Split(':')[0],
//                        Status = "ONLINE",
//                        IsActive = true
//                    };

//                    if (Application.Current.MainWindow is MainWindow mainWindow)
//                    {
//                        mainWindow.DataContext = CurrentDevice;
//                        mainWindow.MainShell?.UpdateMdrUI(CurrentDevice);

//                      //  Debug.WriteLine($"[DATACONTEXT] Switched to device, IsActive={CurrentDevice.IsActive}");
//                    }
//                });

//                // Start the dedicated I/O thread that owns the socket.
//                // At this point StopIoThread() above guarantees there is
//                // no other IoLoop thread still alive.
//                _running = true;
//                _ioThread = new Thread(IoLoop)
//                {
//                    IsBackground = true,
//                    Name = "BL-IO-Thread"
//                };
//                _ioThread.Start();

//                return true;
//            }
//            return false;
//        }

//        // drainPendingWrites: when true, gives the IoLoop one last chance to
//        // flush anything sitting in _priorityQueue (e.g. a SET-IP / reboot
//        // command you just enqueued) before the thread is actually torn down.
//        // Previously this always discarded the queue immediately, which could
//        // silently drop the final "commit/reboot" write if Close() was called
//        // right after SendWriteCommand() — a race that made Set-IP look like
//        // it worked in the UI even when the device never received the write.
//        private void StopIoThread(bool drainPendingWrites)
//        {
//            if (_ioThread == null) return;

//            if (drainPendingWrites)
//            {
//                // Give the IO thread a short window to drain whatever's queued
//                // (writes are fire-and-forget over TCP, so this is a best-effort
//                // flush, not a guaranteed ack from the device).
//                var deadline = DateTime.UtcNow.AddMilliseconds(400);
//                while (!_priorityQueue.IsEmpty && DateTime.UtcNow < deadline)
//                {
//                    _queueSignal.Set();
//                    Thread.Sleep(20);
//                }
//            }

//            _running = false;
//            _queueSignal.Set();
//            _ioThread.Join(500);
//            _ioThread = null;

//            while (_priorityQueue.TryDequeue(out _)) { }
//            while (_normalQueue.TryDequeue(out _)) { }
//        }

//        // Called when CommEthernet's ConnectionLost fires — i.e. the read
//        // loop hit an exception/timeout on its own, NOT because the user
//        // clicked Disconnect. Attempts a few automatic reconnects before
//        // giving up and notifying the UI.
//        private void HandleConnectionLost()
//        {
//            if (_userInitiatedClose) return;   // user clicked Disconnect — don't reconnect

//            // Tell the UI right away — IsConnected is already false at this
//            // point (CommEthernet.Close() nulls out its socket before firing
//            // ConnectionLost), so subscribers can safely re-check IsConnected
//            // here without waiting for TryAutoReconnect to finish.
//            OnConnectionLost?.Invoke();

//            if (_commFactory == null)
//            {
//                // No factory was supplied (Start(IComm) was used directly),
//                // so we can't rebuild the connection — fall back to a normal close.
//                Close();
//                return;
//            }

//            ThreadPool.QueueUserWorkItem(_ => TryAutoReconnect());
//        }

//        private void TryAutoReconnect()
//        {
//            for (int attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
//            {
//                if (_userInitiatedClose) return;

//                var delay = ReconnectDelays[Math.Min(attempt - 1, ReconnectDelays.Length - 1)];
//             //   Debug.WriteLine($"[BL RECONNECT] Attempt {attempt}/{MaxReconnectAttempts} in {delay.TotalSeconds}s...");
//                Thread.Sleep(delay);

//                if (_userInitiatedClose) return;

//                try
//                {
//                    var freshComm = _commFactory(_lastCommFactoryIp, _lastCommFactoryPort);
//                    if (StartInternal(freshComm))
//                    {
//                        Debug.WriteLine("[BL RECONNECT] Success.");
//                        OnConnectionLost?.Invoke(); // reuse the same event to force a UI refresh back to "connected"
//                        return;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[BL RECONNECT] Attempt {attempt} failed: {ex.Message}");
//                }
//            }

//            Debug.WriteLine("[BL RECONNECT] Giving up after max attempts.");
//            Close();
//        }


//        private void IoLoop()
//        {
//            while (_running)
//            {
//                _queueSignal.WaitOne(200);

//                // Drain ALL pending writes first, every loop iteration.
//                while (_priorityQueue.TryDequeue(out var writeAction))
//                {
//                    try { writeAction(); }
//                    catch (Exception ex) { Debug.WriteLine($"[BL WRITE ERROR] {ex.Message}"); }
//                }

//                if (!_running) break;

//                if (_normalQueue.TryDequeue(out var pollAction))
//                {
//                    try { pollAction(); }
//                    catch (Exception ex) { Debug.WriteLine($"[BL POLL ERROR] {ex.Message}"); }
//                }
//            }
//        }

//        // Called by the polling timer. Non-blocking — just enqueues.
//        public void UpdateData()
//        {
//            if (!IsConnected) return;

//            // Avoid piling up stale poll requests if the loop is busy.
//            if (!_normalQueue.IsEmpty) return;

//            _normalQueue.Enqueue(() => SendTcpFrame(0x01, 0xFF, new byte[] { 0x00, 0x00 }));
//            _queueSignal.Set();
//        }

//        // Returns true if the write was actually queued for sending (i.e. we
//        // have a live connection), false if it was dropped because nothing
//        // is connected. Callers MUST check this — previously this method
//        // returned void and silently no-op'd when disconnected, which let
//        // the UI report "IP set" even though nothing was ever sent.
//        public bool SendWriteCommand(ushort registerAddress, ushort value)
//        {
//            if (!IsConnected) return false;

//            byte[] data = {
//                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
//                (byte)(value >> 8), (byte)(value & 0xFF)
//            };

//            _priorityQueue.Enqueue(() => SendTcpFrame(0x01, 0x06, data));
//            _queueSignal.Set();
//            return true;
//        }

//        public void ParseAndUpdateUI(byte[] payload, ModbusDevice device)
//        {
//            if (payload.Length < 6) return;

//            ushort regAddr = (ushort)((payload[2] << 8) | payload[3]);
//            ushort val = (ushort)((payload[4] << 8) | payload[5]);

//            Application.Current.Dispatcher.Invoke(() =>
//            {
//                if (regAddr >= NameBase && regAddr < NameBase + NameRegCount)
//                {
//                    _nameBuf[regAddr - NameBase] = val;

//                    if (regAddr == NameBase + NameRegCount - 1)   // last register arrived
//                    {
//                        byte[] bytes = new byte[NameRegCount * 2];
//                        for (int i = 0; i < NameRegCount; i++)
//                        {
//                            bytes[2 * i] = (byte)(_nameBuf[i] >> 8);
//                            bytes[2 * i + 1] = (byte)(_nameBuf[i] & 0xFF);
//                        }

//                        string name = System.Text.Encoding.ASCII.GetString(bytes);
//                        int zero = name.IndexOf('\0');
//                        if (zero >= 0) name = name.Substring(0, zero);

//                        device.StationName = name;
//                    }
//                    return;
//                }
//                switch (regAddr)
//                {
//                    case 0x0020:
//                        device.DeviceID = val.ToString();
//                        break;
//                    case 0x0021:
//                        device.FirmwareVersion = val.ToString();
//                        (Application.Current.MainWindow as MainWindow)?.UpdateFirmwareVersion(device.FirmwareVersion);
//                        break;
//                    case 0x0022:
//                        device.HardwareRevision = val.ToString();
//                        break;
//                    case 0x0023:
//                        device.DeviceStatus = val.ToString();
//                        break;
//                    case 0x0024:
//                        device.UpStreamIP = val.ToString();
//                        break;
//                    case 0x0025:
//                        device.DownStreamIP = val.ToString();
//                        break;
//                    case 0x0026:
//                        device.SerialNumber = val.ToString();
//                        break;
//                    case 0x0027: // Left Speed
//                        device.LeftSpeed = val.ToString();
//                        break;
//                    case 0x002A: // Left Acceleration
//                        device.LeftAcceleration = val.ToString();
//                        break;
//                    case 0x002B: // Left Deceleration
//                        device.LeftDeceleration = val.ToString();
//                        break;
//                    case 0x002F: // Left Direction
//                        device.LeftDirection = (val == 1) ? "CCW" : "CW";
//                        break;
//                    case 0x0036: // Left Current
//                        device.LeftCurrent = val.ToString();
//                        break;
//                    case 0x002E: // Left Brake Index
//                        device.LeftBrakeIndex = val;
//                        break;
//                    case 0x0001: // Left Hall Type
//                        device.LeftMotorHallTypeIndex = val;
//                        break;
//                    case 0x003A: // LEFT STATUS WORD (Bitmask)
//                        device.LeftOverVoltage = (val & (1 << 6)) != 0 ? 1 : 0;
//                        device.LeftLowVoltage = (val & (1 << 7)) != 0 ? 1 : 0;
//                        device.LeftOverCurrent = (val & (1 << 8)) != 0 ? 1 : 0;
//                        device.LeftSensor1 = (val & (1 << 9)) != 0 ? 1 : 0;
//                        device.LeftSensor2 = (val & (1 << 10)) != 0 ? 1 : 0;
//                        device.LeftHallSensorError = (val & (1 << 11)) != 0 ? 1 : 0;
//                        //device.LeftOverVoltage = (val & (1 << 6)) != 0;
//                        //device.LeftLowVoltage = (val & (1 << 7)) != 0;

//                        //device.LeftOverCurrent = (val & (1 << 8)) != 0;
//                        //device.LeftSensor1 = (val & 0x01) != 0;
//                        //device.LeftSensor1 = (val & (1 << 9)) != 0;
//                        //device.LeftSensor2 = (val & (1 << 10)) != 0;
//                        //device.LeftHallSensorError = (val & (1 << 11)) != 0;
//                        break;

//                    // ================== RIGHT MOTOR ==================
//                    case 0x003F: // Right Speed
//                        device.RightSpeed = val.ToString();
//                        break;
//                    case 0x0042: // Right Acceleration
//                        device.RightAcceleration = val.ToString();
//                        break;
//                    case 0x0043: // Right Deceleration
//                        device.RightDeceleration = val.ToString();
//                        break;
//                    case 0x0047: // Right Direction
//                        device.RightDirection = (val == 1) ? "CCW" : "CW";
//                        break;
//                    case 0x004E: // Right Current
//                                 //device.RightCurrent = (val / 10.0).ToString("F1");
//                        device.RightCurrent = val.ToString();
//                        break;
//                    case 0x0046: // Right Brake Index
//                        device.RightBrakeIndex = val;
//                        break;
//                    case 0x0002: // Right Hall Type
//                        device.RightMotorHallTypeIndex = val;
//                        break;
//                    case 0x0052: // RIGHT STATUS WORD (Bitmask)
//                        //device.RightOverVoltage = (val & (1 << 6)) != 0;
//                        //device.RightLowVoltage = (val & (1 << 7)) != 0;

//                        //device.RightOverCurrent = (val & (1 << 8)) != 0;
//                        //device.RightSensor1 = (val & (1 << 9)) != 0;
//                        //device.RightSensor2 = (val & (1 << 10)) != 0;
//                        //device.RightHallSensorError = (val & (1 << 11)) != 0;
//                        device.RightOverVoltage = (val & (1 << 6)) != 0 ? 1 : 0;
//                        device.RightLowVoltage = (val & (1 << 7)) != 0 ? 1 : 0;
//                        device.RightOverCurrent = (val & (1 << 8)) != 0 ? 1 : 0;
//                        device.RightSensor1 = (val & (1 << 9)) != 0 ? 1 : 0;
//                        device.RightSensor2 = (val & (1 << 10)) != 0 ? 1 : 0;
//                        device.RightHallSensorError = (val & (1 << 11)) != 0 ? 1 : 0;
//                        break;

//                    default:
//                        // Debug.WriteLine($"[UNKNOWN REGISTER] 0x{regAddr:X4} = {val}");
//                        break;
//                }
//            });
//        }

//        // Only ever called from the I/O thread now — no locking needed,
//        // since StopIoThread()/StartInternal() guarantee exactly one
//        // thread touches _comm at a time.
//        private void SendTcpFrame(byte slaveId, byte functionCode, byte[] data)
//        {
//            byte[] pdu = new byte[2 + data.Length];
//            pdu[0] = slaveId;
//            pdu[1] = functionCode;
//            Array.Copy(data, 0, pdu, 2, data.Length);

//            byte[] tcpFrame = new byte[6 + pdu.Length];
//            tcpFrame[0] = 0x00; tcpFrame[1] = 0x01;
//            tcpFrame[2] = 0x00; tcpFrame[3] = 0x00;
//            tcpFrame[4] = (byte)(pdu.Length >> 8);
//            tcpFrame[5] = (byte)(pdu.Length & 0xFF);
//            Array.Copy(pdu, 0, tcpFrame, 6, pdu.Length);

//            _comm.SendRecv(tcpFrame);
//        }

//        // Existing signature preserved for any other callers — closes
//        // WITHOUT draining pending writes (original behavior).
//        public void Close() => Close(drainPendingWrites: false);

//        // Use this overload after a burst of SendWriteCommand() calls (e.g.
//        // Set-IP + commit/reboot) so the queued writes get a chance to
//        // actually leave the socket before it's torn down.
//        public void Close(bool drainPendingWrites)
//        {
//            _userInitiatedClose = true;

//            if (_comm != null)
//            {
//                StopIoThread(drainPendingWrites);

//                if (_comm is CommEthernet tcp)
//                    tcp.ConnectionLost -= HandleConnectionLost;

//                _comm.Close();
//                _comm = null;
//                ActivePortName = string.Empty;

//                if (CurrentDevice != null)
//                {
//                    Application.Current.Dispatcher.Invoke(() =>
//                    {
//                        CurrentDevice.IsActive = false;
//                    });
//                }
//                CurrentDevice = null;

//                OnDisconnected?.Invoke();
//            }
//        }
//    }
//}
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using BLDC_Demo.Models;

namespace BLDC_Demo.Controls
{
    public class BL
    {
        public ModbusDevice CurrentDevice { get; private set; }

        public string ActivePortName { get; private set; } = string.Empty;
        private IComm _comm;
        private const ushort NameBase = 0x0150;
        private const int NameRegCount = 16;
        private readonly ushort[] _nameBuf = new ushort[NameRegCount];
        public event Action OnDisconnected;

        // Fires the INSTANT a drop is detected (before any reconnect
        // attempts run), so the UI can flip to "NOT CONNECTED" right away
        // instead of waiting out the whole ~16s auto-reconnect sequence.
        // Also re-fired on a successful auto-reconnect so the UI flips
        // back to "CONNECTED" without waiting for the next poll tick.
        public event Action OnConnectionLost;

        public bool IsConnected => _comm != null && _comm.IsOpen;

        private const int MaxReconnectAttempts = 1;
        private static readonly TimeSpan[] ReconnectDelays =
        {
         
            TimeSpan.FromSeconds(1)
        };

        private volatile bool _userInitiatedClose;
        private string _lastCommFactoryIp;
        private int _lastCommFactoryPort;
        private Func<string, int, IComm> _commFactory;

        // ── NEW: generation counter ─────────────────────────────────
        // Bumped on every user Start() and every Close(). A background
        // auto-reconnect thread remembers the generation it was born in;
        // if the value has changed, someone else (the user pressing
        // CONNECT / DISCONNECT) has taken over and the old thread must die.
        // This fixes the stale reconnect thread fighting the new connection.
        private int _generation;

        // NEW: only one thread may open/replace the socket at a time.
        private readonly object _startLock = new object();

        private readonly ConcurrentQueue<Action> _priorityQueue = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Action> _normalQueue = new ConcurrentQueue<Action>();
        private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);
        private Thread _ioThread;
        private volatile bool _running;

        public bool Start(IComm c)
        {
            Interlocked.Increment(ref _generation);   // cancel any pending auto-reconnect
            _userInitiatedClose = false;
            return StartInternal(c, -1);
        }

        // Overload that also remembers how to rebuild the same kind of
        // connection later, so auto-reconnect can create a fresh IComm
        // instead of reusing a dead/closed one.
        public bool Start(string ip, int port, Func<string, int, IComm> commFactory)
        {
            Interlocked.Increment(ref _generation);   // cancel any pending auto-reconnect
            _userInitiatedClose = false;
            _commFactory = commFactory;
            _lastCommFactoryIp = ip;
            _lastCommFactoryPort = port;
            return StartInternal(commFactory(ip, port), -1);
        }

        // gen = -1  -> user-initiated start (always proceeds)
        // gen >= 0  -> auto-reconnect start (aborts if the generation changed)
        private bool StartInternal(IComm c, int gen)
        {
            lock (_startLock)
            {
                if (gen >= 0 && gen != _generation)
                {
                    // A newer user action took over while we were waiting.
                    try { c?.Close(); } catch { }
                    return false;
                }

                StopIoThread(drainPendingWrites: false);

                // Make sure an older (dead) comm can't keep firing events
                // or hold the device's single client slot.
                if (_comm != null && !ReferenceEquals(_comm, c))
                {
                    try
                    {
                        if (_comm is CommEthernet oldTcp)
                            oldTcp.ConnectionLost -= HandleConnectionLost;
                        _comm.Close();
                    }
                    catch { }
                }

                _comm = c;

                if (_comm is CommEthernet tcp)
                {
                    tcp.TelemetryReceived += (payload) =>
                    {
                        if (payload == null || payload.Length < 6) return;

                        if (CurrentDevice != null)
                        {
                            ParseAndUpdateUI(payload, CurrentDevice);
                        }
                    };

                    tcp.ConnectionLost += HandleConnectionLost;
                }

                if (_comm.Open())
                {
                    ActivePortName = _comm.Name;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentDevice = new ModbusDevice
                        {
                            PortName = ActivePortName,
                            IpAddress = _comm.Name.Split(':')[0],
                            Status = "ONLINE",
                            IsActive = true
                        };

                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.DataContext = CurrentDevice;
                            mainWindow.MainShell?.UpdateMdrUI(CurrentDevice);
                        }
                    });

                    // Start the dedicated I/O thread that owns the socket.
                    _running = true;
                    _ioThread = new Thread(IoLoop)
                    {
                        IsBackground = true,
                        Name = "BL-IO-Thread"
                    };
                    _ioThread.Start();

                    return true;
                }
                return false;
            }
        }

        // drainPendingWrites: when true, gives the IoLoop one last chance to
        // flush anything sitting in _priorityQueue (e.g. a SET-IP / reboot
        // command you just enqueued) before the thread is actually torn down.
        private void StopIoThread(bool drainPendingWrites)
        {
            if (_ioThread == null) return;

            if (drainPendingWrites)
            {
                var deadline = DateTime.UtcNow.AddMilliseconds(400);
                while (!_priorityQueue.IsEmpty && DateTime.UtcNow < deadline)
                {
                    _queueSignal.Set();
                    Thread.Sleep(20);
                }
            }

            _running = false;
            _queueSignal.Set();
            _ioThread.Join(500);
            _ioThread = null;

            while (_priorityQueue.TryDequeue(out _)) { }
            while (_normalQueue.TryDequeue(out _)) { }
        }

        // Called when CommEthernet's ConnectionLost fires — i.e. the read
        // loop hit an exception/timeout on its own, NOT because the user
        // clicked Disconnect.
        private void HandleConnectionLost()
        {
            if (_userInitiatedClose) return;   // user clicked Disconnect — don't reconnect

            // Tell the UI right away.
            OnConnectionLost?.Invoke();

            if (_commFactory == null)
            {
                // No factory supplied — can't rebuild the connection.
                Close();
                return;
            }

            int gen = _generation;   // remember which "session" this reconnect belongs to
            ThreadPool.QueueUserWorkItem(_ => TryAutoReconnect(gen));
        }

        private void TryAutoReconnect(int gen)
        {
            for (int attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
            {
                if (_userInitiatedClose || gen != _generation) return;

                var delay = ReconnectDelays[Math.Min(attempt - 1, ReconnectDelays.Length - 1)];
                Thread.Sleep(delay);

                // The user may have pressed CONNECT/DISCONNECT while we slept.
                if (_userInitiatedClose || gen != _generation) return;

                try
                {
                    var freshComm = _commFactory(_lastCommFactoryIp, _lastCommFactoryPort);

                    if (gen != _generation)
                    {
                        try { freshComm.Close(); } catch { }
                        return;
                    }

                    if (StartInternal(freshComm, gen))
                    {
                        Debug.WriteLine("[BL RECONNECT] Success.");
                        OnConnectionLost?.Invoke(); // force UI refresh back to "connected"
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BL RECONNECT] Attempt {attempt} failed: {ex.Message}");
                }
            }

            Debug.WriteLine("[BL RECONNECT] Giving up after max attempts.");

            // Only give up if nobody else has taken over in the meantime.
            if (gen == _generation)
                Close();
        }

        private void IoLoop()
        {
            while (_running)
            {
                _queueSignal.WaitOne(200);

                // Drain ALL pending writes first, every loop iteration.
                while (_priorityQueue.TryDequeue(out var writeAction))
                {
                    try { writeAction(); }
                    catch (Exception ex) { Debug.WriteLine($"[BL WRITE ERROR] {ex.Message}"); }
                }

                if (!_running) break;

                if (_normalQueue.TryDequeue(out var pollAction))
                {
                    try { pollAction(); }
                    catch (Exception ex) { Debug.WriteLine($"[BL POLL ERROR] {ex.Message}"); }
                }
            }
        }

        // Called by the polling timer. Non-blocking — just enqueues.
        public void UpdateData()
        {
            if (!IsConnected) return;

            // Avoid piling up stale poll requests if the loop is busy.
            if (!_normalQueue.IsEmpty) return;

            _normalQueue.Enqueue(() => SendTcpFrame(0x01, 0xFF, new byte[] { 0x00, 0x00 }));
            _queueSignal.Set();
        }

        // Returns true if the write was actually queued for sending (i.e. we
        // have a live connection), false if it was dropped because nothing
        // is connected.
        public bool SendWriteCommand(ushort registerAddress, ushort value)
        {
            if (!IsConnected) return false;

            byte[] data = {
                (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
                (byte)(value >> 8), (byte)(value & 0xFF)
            };

            _priorityQueue.Enqueue(() => SendTcpFrame(0x01, 0x06, data));
            _queueSignal.Set();
            return true;
        }

        public void ParseAndUpdateUI(byte[] payload, ModbusDevice device)
        {
            if (payload.Length < 6) return;

            ushort regAddr = (ushort)((payload[2] << 8) | payload[3]);
            ushort val = (ushort)((payload[4] << 8) | payload[5]);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (regAddr >= NameBase && regAddr < NameBase + NameRegCount)
                {
                    _nameBuf[regAddr - NameBase] = val;

                    if (regAddr == NameBase + NameRegCount - 1)   // last register arrived
                    {
                        byte[] bytes = new byte[NameRegCount * 2];
                        for (int i = 0; i < NameRegCount; i++)
                        {
                            bytes[2 * i] = (byte)(_nameBuf[i] >> 8);
                            bytes[2 * i + 1] = (byte)(_nameBuf[i] & 0xFF);
                        }

                        string name = System.Text.Encoding.ASCII.GetString(bytes);
                        int zero = name.IndexOf('\0');
                        if (zero >= 0) name = name.Substring(0, zero);

                        device.StationName = name;
                    }
                    return;
                }
                switch (regAddr)
                {
                    case 0x0020:
                        device.DeviceID = val.ToString();
                        break;
                    case 0x0021:
                        device.FirmwareVersion = val.ToString();
                        (Application.Current.MainWindow as MainWindow)?.UpdateFirmwareVersion(device.FirmwareVersion);
                        break;
                    case 0x0022:
                        device.HardwareRevision = val.ToString();
                        break;
                    case 0x0023:
                        device.DeviceStatus = val.ToString();
                        break;
                    case 0x0024:
                        device.UpStreamIP = val.ToString();
                        break;
                    case 0x0025:
                        device.DownStreamIP = val.ToString();
                        break;
                    case 0x0026:
                        device.SerialNumber = val.ToString();
                        break;
                    case 0x0027: // Left Speed
                        device.LeftSpeed = val.ToString();
                        break;
                    case 0x002A: // Left Acceleration
                        device.LeftAcceleration = val.ToString();
                        break;
                    case 0x002B: // Left Deceleration
                        device.LeftDeceleration = val.ToString();
                        break;
                    case 0x002F: // Left Direction
                        device.LeftDirection = (val == 1) ? "CCW" : "CW";
                        break;
                    case 0x0036: // Left Current
                        device.LeftCurrent = val.ToString();
                        break;
                    case 0x002E: // Left Brake Index
                        device.LeftBrakeIndex = val;
                        break;
                    case 0x0001: // Left Hall Type
                        device.LeftMotorHallTypeIndex = val;
                        break;
                    case 0x003A: // LEFT STATUS WORD (Bitmask)
                        device.LeftOverVoltage = (val & (1 << 6)) != 0 ? 1 : 0;
                        device.LeftLowVoltage = (val & (1 << 7)) != 0 ? 1 : 0;
                        device.LeftOverCurrent = (val & (1 << 8)) != 0 ? 1 : 0;
                        device.LeftSensor1 = (val & (1 << 9)) != 0 ? 1 : 0;
                        device.LeftSensor2 = (val & (1 << 10)) != 0 ? 1 : 0;
                        device.LeftHallSensorError = (val & (1 << 11)) != 0 ? 1 : 0;
                        break;

                    // ================== RIGHT MOTOR ==================
                    case 0x003F: // Right Speed
                        device.RightSpeed = val.ToString();
                        break;
                    case 0x0042: // Right Acceleration
                        device.RightAcceleration = val.ToString();
                        break;
                    case 0x0043: // Right Deceleration
                        device.RightDeceleration = val.ToString();
                        break;
                    case 0x0047: // Right Direction
                        device.RightDirection = (val == 1) ? "CCW" : "CW";
                        break;
                    case 0x004E: // Right Current
                        device.RightCurrent = val.ToString();
                        break;
                    case 0x0046: // Right Brake Index
                        device.RightBrakeIndex = val;
                        break;
                    case 0x0002: // Right Hall Type
                        device.RightMotorHallTypeIndex = val;
                        break;
                    case 0x0052: // RIGHT STATUS WORD (Bitmask)
                        device.RightOverVoltage = (val & (1 << 6)) != 0 ? 1 : 0;
                        device.RightLowVoltage = (val & (1 << 7)) != 0 ? 1 : 0;
                        device.RightOverCurrent = (val & (1 << 8)) != 0 ? 1 : 0;
                        device.RightSensor1 = (val & (1 << 9)) != 0 ? 1 : 0;
                        device.RightSensor2 = (val & (1 << 10)) != 0 ? 1 : 0;
                        device.RightHallSensorError = (val & (1 << 11)) != 0 ? 1 : 0;
                        break;

                    default:
                        break;
                }
            });
        }

        // Only ever called from the I/O thread — no locking needed.
        private void SendTcpFrame(byte slaveId, byte functionCode, byte[] data)
        {
            byte[] pdu = new byte[2 + data.Length];
            pdu[0] = slaveId;
            pdu[1] = functionCode;
            Array.Copy(data, 0, pdu, 2, data.Length);

            byte[] tcpFrame = new byte[6 + pdu.Length];
            tcpFrame[0] = 0x00; tcpFrame[1] = 0x01;
            tcpFrame[2] = 0x00; tcpFrame[3] = 0x00;
            tcpFrame[4] = (byte)(pdu.Length >> 8);
            tcpFrame[5] = (byte)(pdu.Length & 0xFF);
            Array.Copy(pdu, 0, tcpFrame, 6, pdu.Length);

            _comm.SendRecv(tcpFrame);
        }

        // Existing signature preserved — closes WITHOUT draining pending writes.
        public void Close() => Close(drainPendingWrites: false);

        // Use this overload after a burst of SendWriteCommand() calls so the
        // queued writes get a chance to leave the socket before teardown.
        public void Close(bool drainPendingWrites)
        {
            Interlocked.Increment(ref _generation);   // cancel any pending auto-reconnect
            _userInitiatedClose = true;

            lock (_startLock)
            {
                if (_comm != null)
                {
                    StopIoThread(drainPendingWrites);

                    if (_comm is CommEthernet tcp)
                        tcp.ConnectionLost -= HandleConnectionLost;

                    _comm.Close();
                    _comm = null;
                    ActivePortName = string.Empty;

                    if (CurrentDevice != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentDevice.IsActive = false;
                        });
                    }
                    CurrentDevice = null;

                    OnDisconnected?.Invoke();
                }
            }
        }
    }
}