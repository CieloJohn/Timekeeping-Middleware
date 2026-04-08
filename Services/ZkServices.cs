using System;
using System.Collections.Generic;
using System.Threading;
using zkemkeeper;
using offSiteTimekeeping.Models;
using offSiteTimekeeping.Services;

namespace offSiteTimekeeping_NET8.Services
{
    internal class ZkServices : IDisposable
    {
        private CZKEM _zk;
        private Thread _staThread;
        private readonly AutoResetEvent _workEvent = new AutoResetEvent(false);
        private readonly object _lock = new object();

        private readonly string _ip;
        private readonly int _port;
        private readonly int _commKey;

        private DateTime _lastPullTime = DateTime.MinValue;
        private string _serialNumber = string.Empty;
        private bool _isConnected = false;

        private volatile bool _shouldRun = true;

        // Events
        public event Action<List<BiometricLog>> LogsReadyForUI;
        public event Action<string> StatusChanged;
        public event Action<string> SerialNumberChanged;
        public event Action<Exception> ErrorOccurred;
        public event Action<bool> ConnectionStatusChanged;

        public ZkServices(string ip, int port, int commKey)
        {
            _ip = ip;
            _port = port;
            _commKey = commKey;
        }

        public void Start()
        {
            _staThread = new Thread(StaThreadProc)
            {
                IsBackground = true,
                Name = "ZKemKeeper_STA_Thread"
            };
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();
        }

        private void StaThreadProc()
        {
            _zk = new CZKEM();

            while (_shouldRun)
            {
                try
                {
                    _workEvent.WaitOne(10000);

                    if (!_shouldRun) break;

                    lock (_lock)
                    {
                        if (!_isConnected)
                        {
                            bool connected = TryConnect();
                            ConnectionStatusChanged?.Invoke(connected);
                            if (connected)
                            {
                                _serialNumber = GetSerialNumber();
                                SerialNumberChanged?.Invoke(_serialNumber);
                                StatusChanged?.Invoke("Connected");
                            }
                            else
                            {
                                StatusChanged?.Invoke("Cannot Reach Device");
                            }
                        }
                        else
                        {
                            var logs = FetchNewLogs();
                            if (logs.Count > 0)
                            {
                                LogsReadyForUI?.Invoke(logs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }

            try { _zk?.Disconnect(); } catch { }
        }

        private bool TryConnect()
        {
            try
            {
                if (_zk.SetCommPassword(_commKey) && _zk.Connect_Net(_ip, _port))
                {
                    _zk.EnableDevice(1, true);
                    _isConnected = true;
                    return true;
                }
            }
            catch { }
            _isConnected = false;
            return false;
        }

        private List<BiometricLog> FetchNewLogs()
        {
            var newLogs = new List<BiometricLog>();

            try
            {
                if (!_zk.ReadGeneralLogData(1))
                    return newLogs;

                int dwVerifyMode, dwInOutMode, dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond, dwWorkCode = 0;
                string dwEnrollNumber;

                while (_zk.SSR_GetGeneralLogData(1, out dwEnrollNumber, out dwVerifyMode,
                    out dwInOutMode, out dwYear, out dwMonth, out dwDay,
                    out dwHour, out dwMinute, out dwSecond, ref dwWorkCode))
                {
                    var timestamp = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond);

                    if (timestamp > _lastPullTime)
                    {
                        newLogs.Add(new BiometricLog
                        {
                            EnrollNumber = dwEnrollNumber,
                            ModType = dwVerifyMode,
                            InOutMode = dwInOutMode,
                            Timestamp = timestamp,
                            WorkCode = dwWorkCode,
                            DeviceSerialNumber = _serialNumber
                        });
                    }
                }

                if (newLogs.Count > 0)
                {
                    _lastPullTime = DateTime.Now;
                    LocalDbService.SaveLogs(newLogs);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }

            return newLogs;
        }

        private string GetSerialNumber()
        {
            try
            {
                string sn = "";
                _zk.GetSerialNumber(1, out sn);
                return sn;
            }
            catch
            {
                return "Unknown";
            }
        }

        public void TriggerPolling()
        {
            _workEvent.Set();
        }

        public void Disconnect()
        {
            _shouldRun = false;
            _workEvent.Set();
            _staThread?.Join(2000);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}