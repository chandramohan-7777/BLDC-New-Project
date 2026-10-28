
////using System;
////using System.Diagnostics;
////using System.Net.Sockets;
////using System.Threading;
////using System.Threading.Tasks;

////namespace BLDC_Demo.Controls
////{
////    internal class CommEthernet : IComm, IDisposable
////    {
////        private TcpClient _client;
////        private NetworkStream _stream;
////        private readonly string _ip;
////        private readonly int _port;
////        private bool _isListening = false;

////        public event Action<byte[]> TelemetryReceived;
////        public event Action ConnectionLost;

////        public CommEthernet(string ipAddress, int port = 502)
////        {
////            _ip = ipAddress;
////            _port = port;
////        }

////        public string Name => $"{_ip}:{_port}";
////        public bool IsOpen => _client != null && _client.Connected;

////        public bool Open()
////        {
////            try
////            {
////                Close();
////                _client = new TcpClient();

////                var result = _client.BeginConnect(_ip, _port, null, null);
////                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
////                {
////                    Log("Connection timed out.");
////                    return false;
////                }

////                _stream = _client.GetStream();
////                _stream.ReadTimeout = 2000;

////                while (_stream.DataAvailable) _stream.ReadByte();

////                Log($"Connected to {_ip}:{_port}. Waiting for ESP32 telemetry...");

////                StartBackgroundRead();
////                return true;
////            }
////            catch (Exception ex)
////            {
////                Log("Open Error: " + ex.Message);
////                Close();
////                return false;
////            }
////        }

////        // Set true temporarily to see the RAW bytes with no framing assumptions,
////        // so you can figure out what protocol the device is actually speaking.
////        public bool RawDiagnosticMode = false;

////        public void StartBackgroundRead()
////        {
////            _isListening = true;
////            Task.Run(() =>
////            {
////                while (_isListening && IsOpen)
////                {
////                    try
////                    {
////                        if (RawDiagnosticMode)
////                        {
////                            ReadRawDiagnostic();
////                            continue;
////                        }

////                        // ── Modbus TCP MBAP header: 6 bytes ──────────────
////                        // [0-1] Transaction ID   [2-3] Protocol ID (0x0000)
////                        // [4-5] Length = byte count of Unit ID + PDU that follows
////                        byte[] mbap = ReadExact(6);
////                        if (mbap == null) throw new Exception("Disconnect");

////                        int length = (mbap[4] << 8) | mbap[5];

////                        if (length <= 0 || length > 253)
////                        {
////                            Log($"[RESYNC] Bad MBAP length ({length}). Shifting by 1 byte.");
////                            mbap = ResyncMbap(mbap);
////                            if (mbap == null) throw new Exception("Disconnect");
////                            continue;
////                        }

////                        byte[] body = ReadExact(length);
////                        if (body == null) throw new Exception("Disconnect");

////                        byte[] frame = new byte[mbap.Length + body.Length];
////                        Array.Copy(mbap, 0, frame, 0, mbap.Length);
////                        Array.Copy(body, 0, frame, mbap.Length, body.Length);

////                        // ── PRINT RECEIVED DATA (full frame, for diagnostics) ──
////                        //PrintReceivedData(frame);

////                        // BL.ParseAndUpdateUI expects just the 6-byte BODY
////                        // [unitId, funcCode, regHi, regLo, valHi, valLo] —
////                        // NOT the 12-byte frame with the MBAP header attached.
////                        TelemetryReceived?.Invoke(body);
////                    }
////                    catch (Exception ex)
////                    {
////                        Log("Read loop error: " + ex.Message);
////                        Close();
////                        break;
////                    }
////                }
////            });
////        }

////        // ════════════════════════════════════════════════════════
////        //  RESYNC (MBAP variant) — shift the 6-byte header by 1 byte
////        // ════════════════════════════════════════════════════════
////        private byte[] ResyncMbap(byte[] current)
////        {
////            byte[] shifted = new byte[current.Length];
////            Array.Copy(current, 1, shifted, 0, current.Length - 1);
////            byte[] nextByte = ReadExact(1);
////            if (nextByte == null) return null;
////            shifted[shifted.Length - 1] = nextByte[0];
////            return shifted;
////        }

////        // ════════════════════════════════════════════════════════
////        //  RAW DIAGNOSTIC READ — no framing assumptions at all
////        // ════════════════════════════════════════════════════════
////        private void ReadRawDiagnostic()
////        {
////            byte[] buffer = new byte[256];
////            int r = _stream.Read(buffer, 0, buffer.Length);
////            if (r <= 0) throw new Exception("Disconnect");

////            byte[] chunk = new byte[r];
////            Array.Copy(buffer, chunk, r);

////            string hex = BitConverter.ToString(chunk);
////            string ascii = System.Text.Encoding.ASCII.GetString(chunk)
////                .Replace("\r", "\\r").Replace("\n", "\\n");

////           // Log($"[RAW {r} bytes] HEX: {hex}");
////            //Log($"[RAW {r} bytes] ASCII: {ascii}");
////        }

////        // ════════════════════════════════════════════════════════
////        //  PRINT RECEIVED DATA
////        // ════════════════════════════════════════════════════════
////        private void PrintReceivedData(byte[] frame)
////        {
////            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
////            string hex = BitConverter.ToString(frame);

////            string decoded = "";
////            if (frame.Length >= 12)
////            {
////                byte unitId = frame[6];
////                byte funcCode = frame[7];
////                int reg = (frame[8] << 8) | frame[9];
////                int val = (frame[10] << 8) | frame[11];
////                decoded = $"  [unit=0x{unitId:X2} func=0x{funcCode:X2} reg=0x{reg:X4} val={val}]";
////            }

////            string line = $"[{timestamp}] RX ({frame.Length} bytes): {hex}{decoded}";

////            // Visual Studio "Output" window (Debug > Windows > Output)
////           // Debug.WriteLine(line);

////            // Actual console, visible if run from a console host
////            // or after calling AllocConsole() in a WPF app.
////           // Console.WriteLine(line);
////        }

////        private void Log(string message)
////        {
////            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
////            Debug.WriteLine(line);
////            Console.WriteLine(line);
////        }

////        public byte[] SendRecv(byte[] req)
////        {
////            if (!IsOpen || req == null) return null;

////            try
////            {
////                _stream.Write(req, 0, req.Length);
////                _stream.Flush();
////            }
////            catch (Exception ex)
////            {
////                Log("Send Error: " + ex.Message);
////                Close();
////            }

////            return null;
////        }

////        private byte[] ReadExact(int count)
////        {
////            byte[] buffer = new byte[count];
////            int read = 0;
////            while (read < count)
////            {
////                int r = _stream.Read(buffer, read, count - read);
////                if (r <= 0) return null;
////                read += r;
////            }
////            return buffer;
////        }

////        public void Close()
////        {
////            bool wasOpen = _isListening;
////            _isListening = false;
////            try { _stream?.Close(); _client?.Close(); }
////            catch { }
////            finally
////            {
////                _stream = null;
////                _client = null;
////                if (wasOpen)
////                {
////                    Log("Connection closed.");
////                    ConnectionLost?.Invoke();
////                }
////            }
////        }

////        public void Dispose() => Close();
////    }
////}
//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Net.Sockets;
//using System.Threading;
//using System.Threading.Tasks;

//namespace BLDC_Demo.Controls
//{
//    internal class CommEthernet : IComm, IDisposable
//    {
//        private TcpClient _client;
//        private NetworkStream _stream;
//        private readonly string _ip;
//        private readonly int _port;
//        private bool _isListening = false;

//        public event Action<byte[]> TelemetryReceived;
//        public event Action ConnectionLost;

//        public CommEthernet(string ipAddress, int port = 502)
//        {
//            _ip = ipAddress;
//            _port = port;
//        }

//        public string Name => $"{_ip}:{_port}";
//        public bool IsOpen => _client != null && _client.Connected;

//        public bool Open()
//        {
//            try
//            {
//                Close();
//                _client = new TcpClient();

//                var result = _client.BeginConnect(_ip, _port, null, null);
//                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
//                {
//                    Log("Connection timed out.");
//                    return false;
//                }

//                _client.EndConnect(result);

//                _stream = _client.GetStream();
//                _stream.ReadTimeout = 2000;

//                while (_stream.DataAvailable) _stream.ReadByte();

//                Log($"Connected to {_ip}:{_port}. Performing handshake...");

//                // ── REQUIRED HANDSHAKE ──────────────────────────────
//                // Per the firmware (modbus_server.cpp), tcp_server_task will
//                // NOT process ANY write, ANY sequence-ID/activation frame,
//                // or send ANY telemetry until it sees this exact 2-byte
//                // handshake from the client. Without it, handshake_done
//                // stays false and every subsequent byte we send is
//                // silently discarded.
//                byte[] handshake = { 0x01, 0x01 };
//                _stream.Write(handshake, 0, handshake.Length);
//                _stream.Flush();

//                byte[] handshakeReply = new byte[2];
//                int hRead = 0;
//                while (hRead < 2)
//                {
//                    int r = _stream.Read(handshakeReply, hRead, 2 - hRead);
//                    if (r <= 0)
//                    {
//                        Log("Handshake failed: connection closed before reply.");
//                        Close();
//                        return false;
//                    }
//                    hRead += r;
//                }

//                if (handshakeReply[0] != 0x01 || handshakeReply[1] != 0x01)
//                {
//                    Log($"Handshake failed: unexpected reply {handshakeReply[0]:X2} {handshakeReply[1]:X2}.");
//                    Close();
//                    return false;
//                }

//                Log("Handshake OK. Sending activation frame...");

//                // ── ACTIVATION FRAME ─────────────────────────────────
//                // Tells the firmware activeSeqId = 1, which is what turns
//                // telemetry ON (send_telemetry only runs when handshake_done
//                // AND activeSeqId > 0). Format matches the firmware's
//                // "Sequence-ID packet" check: remaining>=8 && rx[offset+7]==0xFF.
//                byte[] activationFrame = {
//                    0x00, 0x01, // Transaction ID
//                    0x00, 0x00, // Protocol ID
//                    0x00, 0x02, // Length (Unit ID + Function code)
//                    0x01,       // Unit ID (Slave 1)
//                    0xFF        // Function code: Activate Telemetry
//                };
//                _stream.Write(activationFrame, 0, activationFrame.Length);
//                _stream.Flush();

//                // The device may reply to the handshake with an extra
//                // 12-byte confirmation frame before telemetry starts —
//                // drain it here so it doesn't get misread as the first
//                // telemetry frame by StartBackgroundRead's MBAP parser.
//                DrainAnyPendingHandshakeConfirmation();

//                Log($"Connected to {_ip}:{_port}. Waiting for ESP32 telemetry...");

//                StartBackgroundRead();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Log("Open Error: " + ex.Message);
//                Close();
//                return false;
//            }
//        }

//        // Best-effort drain of any extra bytes the firmware sends right
//        // after handshake/activation but before real telemetry, so the
//        // background read loop starts cleanly aligned on a real MBAP frame.
//        // Non-fatal if nothing extra arrives — just times out quickly.
//        private void DrainAnyPendingHandshakeConfirmation()
//        {
//            try
//            {
//                int originalTimeout = _stream.ReadTimeout;
//                _stream.ReadTimeout = 150; // short — don't block real telemetry

//                while (_stream.DataAvailable)
//                {
//                    byte[] buf = new byte[64];
//                    int r = _stream.Read(buf, 0, buf.Length);
//                    if (r <= 0) break;
//                }

//                _stream.ReadTimeout = originalTimeout;
//            }
//            catch (IOException)
//            {
//                // Read timeout while draining — fine, nothing more pending.
//            }
//        }

//        // Set true temporarily to see the RAW bytes with no framing assumptions,
//        // so you can figure out what protocol the device is actually speaking.
//        public bool RawDiagnosticMode = false;

//        public void StartBackgroundRead()
//        {
//            _isListening = true;
//            Task.Run(() =>
//            {
//                while (_isListening && IsOpen)
//                {
//                    try
//                    {
//                        if (RawDiagnosticMode)
//                        {
//                            ReadRawDiagnostic();
//                            continue;
//                        }

//                        // ── Modbus TCP MBAP header: 6 bytes ──────────────
//                        // [0-1] Transaction ID   [2-3] Protocol ID (0x0000)
//                        // [4-5] Length = byte count of Unit ID + PDU that follows
//                        byte[] mbap = ReadExact(6);
//                        if (mbap == null) throw new Exception("Disconnect");

//                        int length = (mbap[4] << 8) | mbap[5];

//                        if (length <= 0 || length > 253)
//                        {
//                          //  Log($"[RESYNC] Bad MBAP length ({length}). Shifting by 1 byte.");
//                            mbap = ResyncMbap(mbap);
//                            if (mbap == null) throw new Exception("Disconnect");
//                            continue;
//                        }

//                        byte[] body = ReadExact(length);
//                        if (body == null) throw new Exception("Disconnect");

//                        byte[] frame = new byte[mbap.Length + body.Length];
//                        Array.Copy(mbap, 0, frame, 0, mbap.Length);
//                        Array.Copy(body, 0, frame, mbap.Length, body.Length);

//                        // ── PRINT RECEIVED DATA (full frame, for diagnostics) ──
//                        //PrintReceivedData(frame);

//                        // BL.ParseAndUpdateUI expects just the 6-byte BODY
//                        // [unitId, funcCode, regHi, regLo, valHi, valLo] —
//                        // NOT the 12-byte frame with the MBAP header attached.
//                        TelemetryReceived?.Invoke(body);
//                    }
//                    catch (Exception ex)
//                    {
//                        Log("Read loop error: " + ex.Message);
//                        Close();
//                        break;
//                    }
//                }
//            });
//        }

//        // ════════════════════════════════════════════════════════
//        //  RESYNC (MBAP variant) — shift the 6-byte header by 1 byte
//        // ════════════════════════════════════════════════════════
//        private byte[] ResyncMbap(byte[] current)
//        {
//            byte[] shifted = new byte[current.Length];
//            Array.Copy(current, 1, shifted, 0, current.Length - 1);
//            byte[] nextByte = ReadExact(1);
//            if (nextByte == null) return null;
//            shifted[shifted.Length - 1] = nextByte[0];
//            return shifted;
//        }

//        // ════════════════════════════════════════════════════════
//        //  RAW DIAGNOSTIC READ — no framing assumptions at all
//        // ════════════════════════════════════════════════════════
//        private void ReadRawDiagnostic()
//        {
//            byte[] buffer = new byte[256];
//            int r = _stream.Read(buffer, 0, buffer.Length);
//            if (r <= 0) throw new Exception("Disconnect");

//            byte[] chunk = new byte[r];
//            Array.Copy(buffer, chunk, r);

//            string hex = BitConverter.ToString(chunk);
//            string ascii = System.Text.Encoding.ASCII.GetString(chunk)
//                .Replace("\r", "\\r").Replace("\n", "\\n");

//            // Log($"[RAW {r} bytes] HEX: {hex}");
//            //Log($"[RAW {r} bytes] ASCII: {ascii}");
//        }

//        // ════════════════════════════════════════════════════════
//        //  PRINT RECEIVED DATA
//        // ════════════════════════════════════════════════════════
//        private void PrintReceivedData(byte[] frame)
//        {
//            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
//            string hex = BitConverter.ToString(frame);

//            string decoded = "";
//            if (frame.Length >= 12)
//            {
//                byte unitId = frame[6];
//                byte funcCode = frame[7];
//                int reg = (frame[8] << 8) | frame[9];
//                int val = (frame[10] << 8) | frame[11];
//                decoded = $"  [unit=0x{unitId:X2} func=0x{funcCode:X2} reg=0x{reg:X4} val={val}]";
//            }

//            string line = $"[{timestamp}] RX ({frame.Length} bytes): {hex}{decoded}";
//            Debug.WriteLine(line);
//        }

//        private void Log(string message)
//        {
//            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
//            Debug.WriteLine(line);
//            Console.WriteLine(line);
//        }

//        public byte[] SendRecv(byte[] req)
//        {
//            if (!IsOpen || req == null) return null;

//            try
//            {
//                _stream.Write(req, 0, req.Length);
//                _stream.Flush();
//            }
//            catch (Exception ex)
//            {
//                Log("Send Error: " + ex.Message);
//                Close();
//            }

//            return null;
//        }

//        private byte[] ReadExact(int count)
//        {
//            byte[] buffer = new byte[count];
//            int read = 0;
//            while (read < count)
//            {
//                int r = _stream.Read(buffer, read, count - read);
//                if (r <= 0) return null;
//                read += r;
//            }
//            return buffer;
//        }

//        public void Close()
//        {
//            bool wasOpen = _isListening;
//            _isListening = false;
//            try { _stream?.Close(); _client?.Close(); }
//            catch { }
//            finally
//            {
//                _stream = null;
//                _client = null;
//                if (wasOpen)
//                {
//                    Log("Connection closed.");
//                    ConnectionLost?.Invoke();
//                }
//            }
//        }

//        public void Dispose() => Close();
//    }
//}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BLDC_Demo.Controls
{
    internal class CommEthernet : IComm, IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly string _ip;
        private readonly int _port;
        private bool _isListening = false;

        // Last bytes read by the read loop, dumped when a resync happens
        // so you can see exactly what preceded the misalignment.
        private readonly Queue<byte> _history = new Queue<byte>();
        private const int HistorySize = 48;

        public event Action<byte[]> TelemetryReceived;
        public event Action ConnectionLost;

        public CommEthernet(string ipAddress, int port = 502)
        {
            _ip = ipAddress;
            _port = port;
        }

        public string Name => $"{_ip}:{_port}";
        public bool IsOpen => _client != null && _client.Connected;

        public bool Open()
        {
            try
            {
                Close();
                _client = new TcpClient();
                _client.NoDelay = true; // send each frame immediately, no Nagle merging

                var result = _client.BeginConnect(_ip, _port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
                {
                    Log("Connection timed out.");
                    return false;
                }

                _client.EndConnect(result);

                _stream = _client.GetStream();
                _stream.ReadTimeout = 2000;

                while (_stream.DataAvailable) _stream.ReadByte();

                Log($"Connected to {_ip}:{_port}. Performing handshake...");

                // ── REQUIRED HANDSHAKE ──────────────────────────────
                byte[] handshake = { 0x01, 0x01 };
                _stream.Write(handshake, 0, handshake.Length);
                _stream.Flush();

                byte[] handshakeReply = new byte[2];
                int hRead = 0;
                while (hRead < 2)
                {
                    int r = _stream.Read(handshakeReply, hRead, 2 - hRead);
                    if (r <= 0)
                    {
                        Log("Handshake failed: connection closed before reply.");
                        Close();
                        return false;
                    }
                    hRead += r;
                }

                if (handshakeReply[0] != 0x01 || handshakeReply[1] != 0x01)
                {
                    Log($"Handshake failed: unexpected reply {handshakeReply[0]:X2} {handshakeReply[1]:X2}.");
                    Close();
                    return false;
                }

                Log("Handshake OK. Sending activation frame...");

                // ── ACTIVATION FRAME ─────────────────────────────────
                byte[] activationFrame = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x02, // Length (Unit ID + Function code)
                    0x01,       // Unit ID (Slave 1)
                    0xFF        // Function code: Activate Telemetry
                };
                _stream.Write(activationFrame, 0, activationFrame.Length);
                _stream.Flush();

                DrainAnyPendingHandshakeConfirmation();

                Log($"Connected to {_ip}:{_port}. Waiting for ESP32 telemetry...");

                StartBackgroundRead();
                return true;
            }
            catch (Exception ex)
            {
                Log("Open Error: " + ex.Message);
                Close();
                return false;
            }
        }

        private void DrainAnyPendingHandshakeConfirmation()
        {
            try
            {
                int originalTimeout = _stream.ReadTimeout;
                _stream.ReadTimeout = 150;

                while (_stream.DataAvailable)
                {
                    byte[] buf = new byte[64];
                    int r = _stream.Read(buf, 0, buf.Length);
                    if (r <= 0) break;
                    Log($"[DRAIN {r} bytes] {BitConverter.ToString(buf, 0, r)}");
                }

                _stream.ReadTimeout = originalTimeout;
            }
            catch (IOException)
            {
                // Read timeout while draining — fine.
            }
        }

        // Leave false for normal use. True = dump raw chunks, no parsing.
        public bool RawDiagnosticMode = false;

        public void StartBackgroundRead()
        {
            _isListening = true;
            _history.Clear();

            Task.Run(() =>
            {
                while (_isListening && IsOpen)
                {
                    try
                    {
                        if (RawDiagnosticMode)
                        {
                            ReadRawDiagnostic();
                            continue;
                        }

                        // ── Modbus TCP MBAP header: 6 bytes ──────────────
                        byte[] mbap = ReadExact(6);
                        if (mbap == null) throw new Exception("Disconnect");

                        // Slide forward 1 byte at a time until the header is valid,
                        // collecting the skipped bytes so they can be logged.
                        if (!IsValidMbap(mbap))
                        {
                            Log($"[RESYNC] Bad MBAP: {BitConverter.ToString(mbap)}");
                            Log($"[RESYNC] Previous bytes: {HistoryHex()}");

                            var skipped = new List<byte>();
                            while (!IsValidMbap(mbap))
                            {
                                skipped.Add(mbap[0]);
                                mbap = ResyncMbap(mbap);
                                if (mbap == null) throw new Exception("Disconnect");
                            }

                            Log($"[RESYNC] Skipped {skipped.Count} byte(s): {BitConverter.ToString(skipped.ToArray())}");
                            Log($"[RESYNC] Realigned on: {BitConverter.ToString(mbap)}");
                        }

                        int length = (mbap[4] << 8) | mbap[5];

                        byte[] body = ReadExact(length);
                        if (body == null) throw new Exception("Disconnect");

                        // BL.ParseAndUpdateUI expects just the BODY
                        // [unitId, funcCode, regHi, regLo, valHi, valLo]
                        TelemetryReceived?.Invoke(body);
                    }
                    catch (Exception ex)
                    {
                        Log("Read loop error: " + ex.Message);
                        Close();
                        break;
                    }
                }
            });
        }

        // MBAP is valid only if Protocol ID == 0 and Length is in range.
        private static bool IsValidMbap(byte[] m)
        {
            int len = (m[4] << 8) | m[5];
            return m[2] == 0 && m[3] == 0 && len >= 2 && len <= 253;
        }

        // Shift the 6-byte header by 1 byte and pull in one new byte.
        private byte[] ResyncMbap(byte[] current)
        {
            byte[] shifted = new byte[current.Length];
            Array.Copy(current, 1, shifted, 0, current.Length - 1);
            byte[] nextByte = ReadExact(1);
            if (nextByte == null) return null;
            shifted[shifted.Length - 1] = nextByte[0];
            return shifted;
        }

        private string HistoryHex()
        {
            return _history.Count == 0 ? "(none)" : BitConverter.ToString(_history.ToArray());
        }

        private void ReadRawDiagnostic()
        {
            byte[] buffer = new byte[256];
            int r = _stream.Read(buffer, 0, buffer.Length);
            if (r <= 0) throw new Exception("Disconnect");

            Log($"[RAW {r} bytes] HEX: {BitConverter.ToString(buffer, 0, r)}");
        }

        private void Log(string message)
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public byte[] SendRecv(byte[] req)
        {
            if (!IsOpen || req == null) return null;

            try
            {
                // Log writes (FC 0x06) but not the constant 0xFF poll frames.
                if (req.Length >= 8 && req[7] != 0xFF)
                    Log($"[TX {req.Length} bytes] {BitConverter.ToString(req)}");

                _stream.Write(req, 0, req.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Log("Send Error: " + ex.Message);
                Close();
            }

            return null;
        }

        // Reads exactly `count` bytes and records them in the history buffer.
        // Only called from the read-loop thread.
        private byte[] ReadExact(int count)
        {
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int r = _stream.Read(buffer, read, count - read);
                if (r <= 0) return null;
                read += r;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                _history.Enqueue(buffer[i]);
                if (_history.Count > HistorySize) _history.Dequeue();
            }
            return buffer;
        }

        public void Close()
        {
            bool wasOpen = _isListening;
            _isListening = false;
            try { _stream?.Close(); _client?.Close(); }
            catch { }
            finally
            {
                _stream = null;
                _client = null;
                if (wasOpen)
                {
                    Log("Connection closed.");
                    ConnectionLost?.Invoke();
                }
            }
        }

        public void Dispose() => Close();
    }
}