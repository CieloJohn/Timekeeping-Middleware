using offSiteTimekeeping.Models;
using offSiteTimekeeping.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TimekeepingMiddleware;
using zkemkeeper;

namespace offSiteTimekeeping_NET8.Services
{
    internal class ZkServices
    {
        private readonly CZKEM _zk = new CZKEM();
        private readonly System.Timers.Timer _pollTimer = new System.Timers.Timer();
        private readonly string _ip;
        private readonly int _port;
        private readonly int _commKey;
        private DateTime _lastSuccessfulPull = DateTime.MinValue;

        // Events to safely notify the form
        public event Action<List<BiometricLog>> NewLogsFetched;
        public event Action<string> StatusChanged;
        public event Action<Exception> ErrorOccurred;

        public ZkServices(string ip, int port, int commKey)
        {
            _ip = ip;
            _port = port;
            _commKey = commKey;

            _pollTimer.Elapsed += PollTimer_Elapsed;
            _pollTimer.AutoReset = true;
        }

        public bool Connect()
        {
            if (_zk.Connect_Net(_ip, _port))
            {
                _zk.SetCommPassword(_commKey);
                _pollTimer.Interval = Program.IntervalTime;
                _pollTimer.Start();
                StatusChanged?.Invoke("Connected");
                return true;
            }
            return false;
        }

        private async void PollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _pollTimer.Stop();

            try
            {
                await Task.Run(() => FetchLogsInternal());
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                _pollTimer.Start();
            }
        }

        public void FetchLogsInternal()
        {
            if (!_zk.ReadGeneralLogData(1))
            {
                return;
            }

            var logs = new List<BiometricLog>();
            int dwVerifyMode, dwInOutMode, dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond, dwWorkCode = 0;
            string dwEnrollNumber;

            while (_zk.SSR_GetGeneralLogData(1, out dwEnrollNumber, out dwVerifyMode,
                out dwInOutMode, out dwYear, out dwMonth, out dwDay,
                out dwHour, out dwMinute, out dwSecond, ref dwWorkCode))
            {
                var ts = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond);

                if (ts > _lastSuccessfulPull)
                {
                    logs.Add(new BiometricLog
                    {
                        EnrollNumber = dwEnrollNumber,
                        ModType = dwVerifyMode,
                        InOutMode = dwInOutMode,
                        Timestamp = ts,
                        WorkCode = dwWorkCode,
                        DeviceSerialNumber = GetSerialNumber()
                    });
                }
            }

            if (logs.Count > 0)
            {
                _lastSuccessfulPull = DateTime.Now;
                LocalDbService.SaveLogs(logs);
                NewLogsFetched?.Invoke(logs);
            }

            // Optional: call SyncLogsToCentral() here too (also on background)
            // SyncLogsToCentral();
        }

        public string GetSerialNumber()
        {
            string sn = "";
            _zk.GetSerialNumber(1, out sn);
            return sn;
        }

        public void Disconnect()
        {
            _pollTimer.Stop();
            _zk.Disconnect();
        }
    }
}
