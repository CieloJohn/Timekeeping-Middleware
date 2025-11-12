using Microsoft.Win32;
using offSiteTimekeeping.Helpers;
using offSiteTimekeeping.Models;
using offSiteTimekeeping.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace offSiteTimekeeping_NET_8
{
    public partial class Form1 : Form
    {

        public zkemkeeper.CZKEM zk = new zkemkeeper.CZKEM();
        private List<BiometricLog> biometricsLogsToDb = new();
        private string _biometricSerialNumber = string.Empty;
        public bool Connected;
        private HashSet<string> syncedKeys = new();
        private const int SB_VERT = 1;
        private DateTime lastTriggeredDate = DateTime.MinValue;
        private System.Windows.Forms.Timer clockTimer;
        private TimeSpan serverTimeOffset = TimeSpan.Zero;
        private bool _balloonTipShown = false;
        private bool _reconnectionTipShown = false;

        //Form Handling
        public Form1(bool startHidden = false)
        {
            try
            {
                _startHidden = startHidden;
                _startMinimizedToTray = startHidden;
                AddAppToStartup();
                InitializeComponent();
                InitializeLoadingPanel();

                if (_startHidden)
                {
                    this.Load += (s, e) =>
                    {
                        this.WindowState = FormWindowState.Minimized;
                        this.ShowInTaskbar = false;
                        this.Hide();
                        notifyIcon1.Visible = true;
                        ShowBalloonOnce("Timekeeping Middleware", "Running in background");
                    };
                }
                this.Shown += Form1_Shown;
                this.Resize += Form1_Resize;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.InnerException?.Message ?? ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void Form1_Shown(object sender, EventArgs e)
        {
            try
            {
                pollingTimer.Interval = 5000;
                pollingTimer.Tick -= Timer1_Tick;
                pollingTimer.Tick += Timer1_Tick;
                pollingTimer.Start();

                var serverTime = DatabaseServices.GetServerTime();
                serverTimeOffset = serverTime - DateTime.Now;

                clockTimer = new System.Windows.Forms.Timer();
                clockTimer.Interval = 1000;
                clockTimer.Tick += (s, ev) =>
                {
                    labelClock.Text = (DateTime.Now + serverTimeOffset).ToString("yyyy-MM-dd HH:mm:ss");
                };
                clockTimer.Start();

                ConnectAndFetchLogs();
                ConfigureSyncTimer();
                syncedKeys = DatabaseServices.GetExistingLogKeysInCentralDb(biometricsLogsToDb, _biometricSerialNumber);
                ShowConnectionInfos();
                ShowLogs();
                LoadStartHiddenSetting();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during startup: " + ex.Message);
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                ShowBalloonOnce("Timekeeping Middleware", "Running in background");
            }
        }
        public void CleanupNotifyIcon()
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
        }
        private bool IsConnected(string ip, int comkey, int port)
        {
            return zk.SetCommPassword(comkey) && zk.Connect_Net(ip, port);
        }

        private void AddAppToStartup()
        {
            try
            {
                string appName = "PVAOTimekeepingMiddleware";
                string exePath = Application.ExecutablePath;

                using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    string[] valueNames = reg.GetValueNames();
                    foreach (string name in valueNames)
                    {
                        object val = reg.GetValue(name);
                        if (val is string path && path.Equals(exePath, StringComparison.OrdinalIgnoreCase)
                            && name != appName)
                        {
                            reg.DeleteValue(name);
                        }
                    }

                    reg.SetValue(appName, exePath);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Failed to set startup: " + ex.Message);
                popUpMessage("ERROR", $"Failed to set startup: {ex.Message}", 1);
            }
        }
        private void RemoveFromStartup()
        {
            string appName = "PVAOTimekeepingMiddleware";
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (reg.GetValue(appName) != null)
                {
                    reg.DeleteValue(appName);
                }
            }
        }

        /// Button Controls
        //private async void button2_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        label1.Text = "Disconnecting...";
        //        // Cancels Retries tasks
        //        _retryCts?.Cancel();
        //        _retryCts = null;

        //        pollingTimer.Stop();
        //        syncTimer.Stop();
        //        checkTimeTimer.Stop();

        //        await Task.Run(() =>
        //        {
        //            if (Connected)
        //            {
        //                zk.Disconnect();
        //                Connected = false;
        //            }
        //        });

        //        listView1.Items.Clear();
        //        listView1.Columns.Clear();

        //        ConfigEncryptor.ClearConfig();
        //        LocalDbService.DeleteLocalDatabase();

        //        Program.BiometricsIp = null;
        //        Program.BiometricsCommKey = 0;
        //        Program.DataTransferMode = null;
        //        Program.IntervalTime = 0;
        //        Program.ScheduledTime = TimeSpan.Zero;

        //        this.Tag = "Restart";
        //        this.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error during disconnect: " + ex.Message);
        //    }
        //}
        private void minimize_Click(object sender, EventArgs e)
        {
            this.Hide();
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(1000, "Timekeeping Middleware", "Running in background", ToolTipIcon.Info);
        }

        private void exit_Click(object sender, EventArgs e)
        {
            this.Hide();
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(1000, "Timekeeping Middleware", "Running in background", ToolTipIcon.Info);
        }
        private void refresh_Click(object sender, EventArgs e)
        {
            ConnectAndFetchLogs();

            syncedKeys = DatabaseServices.GetExistingLogKeysInCentralDb(biometricsLogsToDb, _biometricSerialNumber);
            listView1.Items.Clear();
            listView1.Columns.Clear();
            ShowConnectionInfos();
            ShowLogs();
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate(); // Make sure it gets focus

            if (!_hasShownOnce)
            {
                _hasShownOnce = true;
            }
        }
        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            listView1.Items.Clear();
            listView1.Columns.Clear();

            ConfigEncryptor.ClearConfig();
            LocalDbService.DeleteLocalDatabase();

            Program.BiometricsIp = null;
            Program.BiometricsCommKey = 0;
            Program.DataTransferMode = null;
            Program.IntervalTime = 0;
            Program.ScheduledTime = TimeSpan.Zero;

            RemoveFromStartup();
            Application.Exit();
        }
        //Display Toggles
        private void ShowConnectionInfos()
        {
            if (string.IsNullOrWhiteSpace(Program.DataTransferMode))
            {
                label19.Text = "No data transfer mode selected.";
                return;
            }

            switch (Program.DataTransferMode)
            {
                case "Fixed Interval":
                    var minutes = Program.IntervalTime / 60000;
                    var seconds = (Program.IntervalTime / 1000) % 60;
                    label19.Text = $"Fixed Interval. Every {minutes} minute(s) and {seconds} second(s)";
                    label19.Font = new Font("Segoe UI", 8, FontStyle.Italic);
                    break;

                case "Specific Time":
                    label19.Text = $"Specific Time. Today at {Program.ScheduledTime}";
                    label19.Font = new Font("Segoe UI", 8, FontStyle.Italic);
                    break;

                default:
                    label19.Text = "Unknown data transfer mode.";
                    break;
            }
        }
        private void ShowLogs()
        {
            listView1.Items.Clear();
            listView1.Columns.Clear();

            listView1.Columns.Add("User");
            listView1.Columns.Add("ModType");
            listView1.Columns.Add("Action");
            listView1.Columns.Add("Timestamp");
            listView1.Columns.Add("Sync Status");

            listView1.OwnerDraw = true;
            listView1.ColumnWidthChanging += listView1_ColumnWidthChanging;
            listView1.DrawColumnHeader += listView1_DrawColumnHeader;
            listView1.DrawSubItem += listView1_DrawSubItem;

            listView1.BeginUpdate();
            foreach (var log in biometricsLogsToDb)
            {
                string key = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}|{_biometricSerialNumber}";
                bool isSynced = syncedKeys.Contains(key);

                var item = new ListViewItem(log.EnrollNumber);
                item.SubItems.Add(log.ModType.ToString());
                item.SubItems.Add(log.InOutMode.ToString());
                item.SubItems.Add(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(isSynced ? "Synced" : "Pending");

                // Optional styling
                item.ForeColor = isSynced ? Color.Gray : Color.Orange;

                listView1.Items.Add(item);
            }
            listView1.EndUpdate();

            if (listView1.Columns.Count == 0)
                return;

            int totalWidth = listView1.ClientSize.Width;
            int[] weights = { 14, 14, 14, 32, 26 };

            for (int i = 0; i < listView1.Columns.Count; i++)
            {
                int columnWidth = totalWidth * weights[i] / 100;
                listView1.Columns[i].Width = columnWidth;
            }
        }
        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(34, 59, 38)), e.Bounds);
                e.Graphics.DrawString(e.Header.Text, listView1.Font, brush, e.Bounds, sf);
            }
        }
        private void listView1_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listView1.Columns[e.ColumnIndex].Width;
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                if (e.ColumnIndex == 1)
                    sf.Alignment = StringAlignment.Center;
                else if (e.ColumnIndex == 2)
                    sf.Alignment = StringAlignment.Far;


                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView1.Font, e.Bounds, Color.FromArgb(150, 150, 150),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        private const int WM_VSCROLL = 0x0115;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;
        private const int SB_THUMBPOSITION = 4;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private void vScrollBarCustom_Scroll(object sender, ScrollEventArgs e)
        {
            SendMessage(listView1.Handle, WM_VSCROLL, (IntPtr)(e.Type == ScrollEventType.SmallIncrement ? SB_LINEDOWN :
                                                               e.Type == ScrollEventType.SmallDecrement ? SB_LINEUP :
                                                               SB_THUMBPOSITION | (e.NewValue << 16)), IntPtr.Zero);
        }
        private void InitializeLoadingPanel()
        {
            loadingPanel = new Panel
            {
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width + 160) / 2 - 200, (this.ClientSize.Height - 50) / 2),
                BackColor = Color.FromArgb(255, 39, 54, 38),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            loadingLabel = new Label
            {
                Text = "Connecting...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            loadingPanel.Controls.Add(loadingLabel);
            this.Controls.Add(loadingPanel);
        }

        private Panel loadingPanel;
        private Label loadingLabel;

        //Sync Logics
        private bool ConnectAndFetchLogs()
        {
            try
            {
                var fetchSuccess = FetchLogs();
                SyncDeviceTimeWithServer(1);
                if (!fetchSuccess)
                {
                    ReconnectionBalloonNotice("Timekeeping Middleware", "Network Unreachable: Middleware Trying to Reconnect");
                    StartAutoRetry();
                }
                return fetchSuccess;
            }

            catch
            {
                return false;
            }
        }

        private bool FetchLogs()
        {
            try
            {
                if (!DatabaseServices.TestDatabaseConnection())
                {
                    //MessageBox.Show("Database Unreachable");
                    return false;
                }
                else
                {
                    label1.Text = "Connected";
                }

                Connected = IsConnected(Program.BiometricsIp, Program.BiometricsCommKey, Program.BiometricsPort);
                if (!Connected)
                {
                    return false;
                }

                this.SafeInvoke(() =>
                {
                    label15.Text = "Connected";
                });

                const int dwMachineNumber = 1;
                int dwVerifyMode, dwInOutMode, dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond, dwWorkCode = 0;
                string dwEnrollNumber;
                string serialNumber = string.Empty;
                zk.GetSerialNumber(dwMachineNumber, out serialNumber);
                _biometricSerialNumber = serialNumber;
                label14.Text = serialNumber;
                biometricsLogsToDb.Clear();

                if (!zk.ReadGeneralLogData(dwMachineNumber))
                {
                    popUpMessage("NOTICE", "No Log Data available or connection lost.", 2);
                    //return false;
                }

                while (zk.SSR_GetGeneralLogData(
                    dwMachineNumber,
                    out dwEnrollNumber,
                    out dwVerifyMode,
                    out dwInOutMode,
                    out dwYear,
                    out dwMonth,
                    out dwDay,
                    out dwHour,
                    out dwMinute,
                    out dwSecond,
                    ref dwWorkCode))
                {
                    var timestamp = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond);
                    var log = new BiometricLog
                    {
                        EnrollNumber = dwEnrollNumber,
                        ModType = dwVerifyMode,
                        InOutMode = dwInOutMode,
                        Timestamp = timestamp,
                        WorkCode = dwWorkCode,
                        DeviceSerialNumber = _biometricSerialNumber
                    };

                    biometricsLogsToDb.Add(log);
                }

                if (biometricsLogsToDb.Count > 0)
                {
                    LocalDbService.SaveLogs(biometricsLogsToDb);
                }

                return true;
            }
            catch (Exception ex)
            {
                popUpMessage("NOTICE", $"{ex.Message}", 1);
                return false;
            }

        }

        private void SyncLogsToCentral()
        {
            var unsyncedLogs = LocalDbService.GetUnsyncedLogs();
            if (unsyncedLogs.Count == 0)
                return;

            var existingKeys = DatabaseServices.GetExistingLogKeysInCentralDb(unsyncedLogs, _biometricSerialNumber);
            var filtered = unsyncedLogs.Where(log =>
            {
                var key = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}|{_biometricSerialNumber}";
                return !existingKeys.Contains(key);
            }).ToList();

            if (filtered.Count > 0)
            {
                DatabaseServices.SendLogsToCentralDb(filtered, _biometricSerialNumber);
                LocalDbService.MarkLogsAsSynced(filtered.ConvertAll(l => l.Id));
            }
        }

        public void SyncDeviceTimeWithServer(int machineNumber)
        {

            try
            {
                DateTime serverTime = DatabaseServices.GetServerTime();
                SystemTimeHelper.SetSystemTime(serverTime);

                zk.SetDeviceTime2(machineNumber,
                    serverTime.Year, serverTime.Month, serverTime.Day,
                    serverTime.Hour, serverTime.Minute, serverTime.Second);
            }
            catch (Exception ex)
            {
                zk.SetDeviceTime(1);
                //MessageBox.Show("[Syncing local time] : Server time sync failed : " + ex.Message);
                popUpMessage("ERROR", $"[Syncing local time] : Server time sync failed : {ex.Message}", 1);
            }
        }

        public static class SystemTimeHelper
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetSystemTime(ref SYSTEMTIME st);

            [StructLayout(LayoutKind.Sequential)]
            private struct SYSTEMTIME
            {
                public short Year;
                public short Month;
                public short DayOfWeek;
                public short Day;
                public short Hour;
                public short Minute;
                public short Second;
                public short Milliseconds;
            }

            public static void SetSystemTime(DateTime dt)
            {
                SYSTEMTIME st = new SYSTEMTIME
                {
                    Year = (short)dt.Year,
                    Month = (short)dt.Month,
                    Day = (short)dt.Day,
                    Hour = (short)dt.Hour,
                    Minute = (short)dt.Minute,
                    Second = (short)dt.Second,
                    Milliseconds = (short)dt.Millisecond
                };

                DateTime utc = dt.ToUniversalTime();
                st.Year = (short)utc.Year;
                st.Month = (short)utc.Month;
                st.Day = (short)utc.Day;
                st.Hour = (short)utc.Hour;
                st.Minute = (short)utc.Minute;
                st.Second = (short)utc.Second;
                st.Milliseconds = (short)utc.Millisecond;

                SetSystemTime(ref st);
            }
        }


        //Timers
        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (!Connected) return;
            try
            {
                int dwMachineNumber = 1;
                string dwEnrollNumber;
                int dwVerifyMode;
                int dwInOutMode;
                int dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond;
                int dwWorkCode = 0;

                List<BiometricLog> newLogs = new();

                if (zk.ReadGeneralLogData(dwMachineNumber))
                {
                    while (zk.SSR_GetGeneralLogData(
                        dwMachineNumber,
                        out dwEnrollNumber,
                        out dwVerifyMode,
                        out dwInOutMode,
                        out dwYear,
                        out dwMonth,
                        out dwDay,
                        out dwHour,
                        out dwMinute,
                        out dwSecond,
                        ref dwWorkCode))
                    {
                        var timestamp = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond);
                        var log = new BiometricLog
                        {
                            EnrollNumber = dwEnrollNumber,
                            ModType = dwVerifyMode,
                            InOutMode = dwInOutMode,
                            Timestamp = timestamp,
                            WorkCode = dwWorkCode,
                            DeviceSerialNumber = _biometricSerialNumber
                        };

                        if (!LocalDbService.LogExists(log))
                        {
                            string key = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}|{_biometricSerialNumber}";
                            bool isSynced = syncedKeys.Contains(key);

                            LocalDbService.SaveLog(log);
                            if (isSynced)
                            {
                                LocalDbService.MarkLogsAsSynced(new List<int> { log.Id });
                            }

                            Invoke(() =>
                            {
                                var item = new ListViewItem(log.EnrollNumber);
                                item.SubItems.Add(log.ModType.ToString());
                                item.SubItems.Add(log.InOutMode.ToString());
                                item.SubItems.Add(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                                item.SubItems.Add(isSynced ? "Synced" : "Pending");
                                item.ForeColor = isSynced ? Color.Gray : Color.Orange;
                                listView1.Items.Add(item);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("TransferTimer Error: " + ex.Message);
                popUpMessage("ERROR", $"TransferTimer Error: {ex.Message}", 1);
            }
        }

        private async void SyncTimer_Tick(object sender, EventArgs e)
        {
            await Task.Run(() => SyncLogsToCentral());
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            labelClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private CancellationTokenSource _retryCts;
        private Task _backgroundRetryTask;

        private async Task RetryConnectUntilSuccessAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (DatabaseServices.TestDatabaseConnection() && IsBiometricDeviceConnected())
                    {
                        this.SafeInvoke(() =>
                        {
                            label1.Text = "Connected";
                            label1.Font = new Font("Segoe UI", 12F);
                            ConnectAndFetchLogs();
                            ConfigureSyncTimer();
                            ShowConnectionInfos();
                            syncedKeys = DatabaseServices.GetExistingLogKeysInCentralDb(biometricsLogsToDb, _biometricSerialNumber);
                            ShowLogs();
                        });
                        break;
                    }

                    if (!DatabaseServices.TestDatabaseConnection())
                    {
                        this.SafeInvoke(() =>
                        {
                            label1.Text = "Waiting for network";
                            label1.Font = new Font("Segoe UI", 10F);
                        });
                    }
                    if (!IsBiometricDeviceConnected())
                    {
                        this.SafeInvoke(() =>
                        {
                            label15.Text = "Cannot Reach";
                            //label1.Font = new Font("Segoe UI", 10F);
                        });
                    }

                    await Task.Delay(5000, token);
                }
            }
            catch (Exception ex)
            {
                popUpMessage("NOTICE", $"{ex.Message}", 2);
            }
        }
        private void StartAutoRetry()
        {
            _retryCts = new CancellationTokenSource();
            _backgroundRetryTask = RetryConnectUntilSuccessAsync(_retryCts.Token);
        }

        public bool IsBiometricDeviceConnected()
        {
            return IsConnected(Program.BiometricsIp, Program.BiometricsCommKey, Program.BiometricsPort);
        }
        private void CheckTimeTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            TimeSpan selectedTime = Program.ScheduledTime == TimeSpan.Zero
                ? new TimeSpan(16, 45, 0)
                : Program.ScheduledTime;

            if (Math.Abs((now - selectedTime).TotalMinutes) < 1 &&
                lastTriggeredDate.Date != DateTime.Today)
            {
                lastTriggeredDate = DateTime.Today;
                SyncLogsToCentral();
            }
        }
        private void ConfigureSyncTimer()
        {
            syncTimer.Stop();
            checkTimeTimer.Stop();

            var selected = Program.DataTransferMode;
            if (string.IsNullOrEmpty(selected))
                return;

            if (selected == "Fixed Interval" && Program.IntervalTime > 0)
            {
                syncTimer.Interval = Program.IntervalTime;
                syncTimer.Tick -= SyncTimer_Tick;
                syncTimer.Tick += SyncTimer_Tick;
                syncTimer.Start();
            }
            else if (selected == "Specific Time")
            {
                checkTimeTimer.Interval = 1000;
                checkTimeTimer.Tick -= CheckTimeTimer_Tick;
                checkTimeTimer.Tick += CheckTimeTimer_Tick;
                checkTimeTimer.Start();
            }
        }


        //WinForms Style Configs
        private void middlewareVerticalText(object sender, PaintEventArgs e)
        {
            string text = "| MIDDLEWARE |";
            Font font = new Font("JamGrotesque-Bold", 24, FontStyle.Bold);
            Brush brush = new SolidBrush(Color.FromArgb(255, 42, 53, 42));
            e.Graphics.TranslateTransform(40, MIDDLEWARE.Height - 350);
            e.Graphics.RotateTransform(90);
            e.Graphics.DrawString(text, font, brush, 0, 0);
            e.Graphics.ResetTransform();
        }


        //WinForms Behaviour Configs
        private bool _startHidden = false;
        private bool _startMinimizedToTray = false;
        private bool _hasShownOnce = false;
        private void LoadStartHiddenSetting()
        {
            var config = ConfigEncryptor.LoadConfig();
            hideToggle.Checked = config?.StartHidden ?? false;
            hideToggle.CheckedChanged += (s, ev) =>
            {
                var current = ConfigEncryptor.LoadConfig();
                if (current != null)
                {
                    ConfigEncryptor.SaveConfig(
                        ip: current.BiometricsIp,
                        port: current.BiometricsPort,
                        commKey: current.CommKey,
                        dataTransferMode: current.DataTransferMode,
                        intervalSeconds: current.IntervalSeconds,
                        scheduledTime: current.ScheduledTime,
                        startHidden: hideToggle.Checked
                    );
                }
            };
            if (_startHidden && !_hasShownOnce)
            {
                BeginInvoke(new Action(() =>
                {
                    this.Hide();
                    notifyIcon1.Visible = true;
                    ShowBalloonOnce("Timekeeping Middleware", "Running in background");
                }));
            }
        }

        private void HideToggle_CheckedChanged(object sender, EventArgs e)
        {
            var current = ConfigEncryptor.LoadConfig();
            if (current != null)
            {
                ConfigEncryptor.SaveConfig(
                    ip: current.BiometricsIp,
                    port: current.BiometricsPort,
                    commKey: current.CommKey,
                    dataTransferMode: current.DataTransferMode,
                    intervalSeconds: current.IntervalSeconds,
                    scheduledTime: current.ScheduledTime,
                    startHidden: hideToggle.Checked
                );
            }
        }
        private void ShowBalloonOnce(string title, string message)
        {
            if (_balloonTipShown) return;

            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(1000, title, message, ToolTipIcon.Info);
            _balloonTipShown = true;
        }

        private void ReconnectionBalloonNotice(string title, string message)
        {
            if (_reconnectionTipShown) return;

            DatabaseUnreachable.Icon = SystemIcons.Information;
            DatabaseUnreachable.Visible = true;
            DatabaseUnreachable.ShowBalloonTip(1000, title, message, ToolTipIcon.Warning);
            _reconnectionTipShown = true;
        }



        protected void EnableMinimizeToTray(NotifyIcon trayIcon)
        {
            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                    trayIcon.Visible = true;
                    ShowBalloonOnce("Timekeeping Middleware", "Running in background");
                }
            };

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                    trayIcon.Visible = true;
                    ShowBalloonOnce("Timekeeping Middleware", "Running in background");
                }
            };
        }

        //dragging support
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                IsConnected(Program.BiometricsIp, Program.BiometricsCommKey, Program.BiometricsPort);
                SyncDeviceTimeWithServer(1);
                //zk.SetDeviceTime(1);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                popUpMessage("NOTICE", $"{ex.Message}", 2);
            }
        }

        public void popUpMessage(string title, string message, int mode)
        {
            switch (mode)
            {
                //RED
                case 1:
                    popUpPanel.BackColor = Color.FromArgb(130, 50, 50);
                    popUpTitle.ForeColor = Color.FromArgb(190, 135, 135);
                    panel13.BackColor = Color.FromArgb(182, 120, 120);
                    popUpDesc.ForeColor = Color.FromArgb(210, 150, 150);
                    closePopUp.ForeColor = Color.FromArgb(190, 135, 135);
                    break;
                //YELLOW
                case 2:
                    popUpPanel.BackColor = Color.FromArgb(190, 210, 50);
                    popUpTitle.ForeColor = Color.FromArgb(50, 50, 10);
                    panel13.BackColor = Color.FromArgb(50, 50, 10);
                    popUpDesc.ForeColor = Color.FromArgb(70, 70, 20);
                    closePopUp.ForeColor = Color.FromArgb(50, 50, 10);
                    break;
                //BLUE
                case 3:
                    popUpPanel.BackColor = Color.FromArgb(32, 167, 199);
                    popUpTitle.ForeColor = Color.FromArgb(10, 90, 120);
                    panel13.BackColor = Color.FromArgb(10, 90, 130);
                    popUpDesc.ForeColor = Color.FromArgb(10, 70, 90);
                    closePopUp.ForeColor = Color.FromArgb(10, 90, 120);
                    break;

                    //case 3:
                    //    showCredentialsError.BackColor = Color.FromArgb(40, 60, 40);
                    //    label12.ForeColor = Color.FromArgb(90, 120, 90);
                    //    panel2.BackColor = Color.FromArgb(57, 82, 57);
                    //    credentialsNotice.ForeColor = Color.FromArgb(160, 190, 160);
                    //    closeCredentialsNotice.ForeColor = Color.FromArgb(90, 120, 90);
                    //    break;
            }

            popUpPanel.BringToFront();
            popUpPanel.Visible = true;
            popUpTitle.Text = title;
            popUpDesc.Text = message;

            popUpVisibilityTimer = new System.Windows.Forms.Timer();
            popUpVisibilityTimer.Interval = 5000;
            popUpVisibilityTimer.Tick += (s, e) =>
            {
                popUpPanel.Visible = false;
                popUpVisibilityTimer.Stop();
                popUpVisibilityTimer.Dispose();
            };
        }

        private void closePopUp_Click(object sender, EventArgs e)
        {
            popUpPanel.Visible = false;
            popUpVisibilityTimer.Stop();
        }
    }
}
