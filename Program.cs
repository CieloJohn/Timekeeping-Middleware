using offSiteTimekeeping.Helpers;
using offSiteTimekeeping_NET8.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimekeepingMiddleware
{
    internal static class Program
    {
        public static string BiometricsIp;
        public static int BiometricsPort;
        public static int BiometricsCommKey;
        public static string DataTransferMode;
        public static int IntervalTime { get; set; } = 1000;
        public static TimeSpan ScheduledTime { get; set; }
        public static bool LaunchedFromLoginForm = false;

        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "middleware_errors.log");

        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) =>
            {
                LogError("UI ThreadException", e.Exception);
                ShowPopupSafe("Unhandled Error", e.Exception.Message, 1);
            };


            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    LogError("AppDomain UnhandledException", ex);
                    ShowPopupSafe("Critical Error", ex.Message, 1);
                }
                else
                {
                    LogText("AppDomain UnhandledException", e.ExceptionObject?.ToString());
                    ShowPopupSafe("Critical Error", "An unknown critical error occurred.", 1);
                }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogError("TaskScheduler UnobservedTaskException", e.Exception);
                ShowPopupSafe("Background Task Error", e.Exception.Message, 2);
                e.SetObserved();
            };

            try
            {
                string startupLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
                File.AppendAllText(startupLog, $"Started at {DateTime.Now}\n");
            }
            catch { }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

        RestartApp:
            var config = ConfigEncryptor.LoadConfig();
            if (config != null)
            {
                BiometricsIp = config.BiometricsIp;
                BiometricsPort = config.BiometricsPort;
                BiometricsCommKey = config.CommKey;
                DataTransferMode = config.DataTransferMode;
                IntervalTime = config.IntervalSeconds;
                ScheduledTime = config.ScheduledTime;
            }

            Form1 mainForm = null;

            if (config == null)
            {
                try
                {
                    var registry = new RegistryInterface();
                    string exePath = Application.ExecutablePath;

                    if (registry.Exists(exePath))
                    {
                        registry.Remove(exePath);
                    }

                    var loginForm = new LoginForm();
                    var result = loginForm.ShowDialog();

                    while (loginForm.Visible)
                    {
                        Application.DoEvents();
                        Thread.Sleep(100);
                    }

                    if (result == DialogResult.OK && loginForm.LoginSuccessful)
                    {
                        LaunchedFromLoginForm = true;

                        var configToSave = new ConfigEncryptor.ConfigData
                        {
                            BiometricsIp = BiometricsIp,
                            BiometricsPort = BiometricsPort,
                            CommKey = BiometricsCommKey,
                            DataTransferMode = DataTransferMode,
                            IntervalSeconds = IntervalTime,
                            ScheduledTime = ScheduledTime,
                            StartHidden = true
                        };

                        ConfigEncryptor.SaveConfig(
                            configToSave.BiometricsIp,
                            configToSave.BiometricsPort,
                            configToSave.CommKey,
                            configToSave.DataTransferMode,
                            configToSave.IntervalSeconds,
                            configToSave.ScheduledTime,
                            configToSave.StartHidden = true
                        );

                        config = ConfigEncryptor.LoadConfig();

                        if (config != null)
                        {
                            BiometricsIp = config.BiometricsIp;
                            BiometricsPort = config.BiometricsPort;
                            BiometricsCommKey = config.CommKey;
                            DataTransferMode = config.DataTransferMode;
                            IntervalTime = config.IntervalSeconds;
                            ScheduledTime = config.ScheduledTime;
                        }

                        mainForm = new Form1(config?.StartHidden ?? false);
                    }
                }
                catch (Exception ex)
                {
                    LogError("Login/Config failed", ex);
                    ShowPopupSafe("Startup Error", ex.Message, 1);
                    return;
                }
            }
            else
            {
                mainForm = new Form1(config?.StartHidden ?? false);
            }

            if (mainForm != null)
            {
                Application.ApplicationExit += (s, e) =>
                {
                    try
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            var activeMainForm = Application.OpenForms[0] as Form1;
                            activeMainForm?.CleanupNotifyIcon();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError("CleanupNotifyIcon failed", ex);
                    }
                };

                Application.Run(mainForm);

                if (mainForm.Tag?.ToString() == "Restart")
                {
                    goto RestartApp;
                }
            }
        }

        private static void ShowPopupSafe(string title, string message, int mode)
        {
            try
            {
                if (Application.OpenForms.Count == 0) return;

                if (Application.OpenForms[0] is Form1 form)
                {
                    form.SafeInvoke(() =>
                    {
                        form.popUpMessage(title, message, mode);
                    });
                }
            }
            catch
            {

            }
        }

        private static void LogError(string source, Exception ex)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{ex}\n\n");
            }
            catch { }
        }

        private static void LogText(string source, string message)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{message}\n\n");
            }
            catch { }
        }
    }
}


//using offSiteTimekeeping.Helpers;
//using System;
//using System.IO;
//using System.Threading;
//using System.Windows.Forms;

//namespace offSiteTimekeeping_NET_8
//{
//    internal static class Program
//    {
//        public static string BiometricsIp;
//        public static int BiometricsPort;
//        public static int BiometricsCommKey;
//        public static string DataTransferMode;
//        public static int IntervalTime { get; set; } = 1000;
//        public static TimeSpan ScheduledTime { get; set; }
//        public static bool LaunchedFromLoginForm = false;

//        [STAThread]
//        static void Main()
//        {
//            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
//            File.AppendAllText(logPath, $"Started at {DateTime.Now}\n");
//            Application.SetHighDpiMode(HighDpiMode.SystemAware);
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            ApplicationConfiguration.Initialize();

//        RestartApp:
//            var config = ConfigEncryptor.LoadConfig();
//            if (config != null)
//            {
//                BiometricsIp = config.BiometricsIp;
//                BiometricsPort = config.BiometricsPort;
//                BiometricsCommKey = config.CommKey;
//                DataTransferMode = config.DataTransferMode;
//                IntervalTime = config.IntervalSeconds;
//                ScheduledTime = config.ScheduledTime;
//            }

//            Form1 mainForm = null;

//            if (config == null)
//            {
//                try
//                {
//                    var loginForm = new LoginForm();
//                    var result = loginForm.ShowDialog();

//                    while (loginForm.Visible)
//                    {
//                        Application.DoEvents();
//                        Thread.Sleep(100);
//                    }

//                    if (result == DialogResult.OK && loginForm.LoginSuccessful)
//                    {
//                        LaunchedFromLoginForm = true;

//                        var configToSave = new ConfigEncryptor.ConfigData
//                        {
//                            BiometricsIp = BiometricsIp,
//                            BiometricsPort = BiometricsPort,
//                            CommKey = BiometricsCommKey,
//                            DataTransferMode = DataTransferMode,
//                            IntervalSeconds = IntervalTime,
//                            ScheduledTime = ScheduledTime,
//                            StartHidden = true
//                        };

//                        ConfigEncryptor.SaveConfig(
//                           configToSave.BiometricsIp,
//                           configToSave.BiometricsPort,
//                           configToSave.CommKey,
//                           configToSave.DataTransferMode,
//                           configToSave.IntervalSeconds,
//                           configToSave.ScheduledTime,
//                           configToSave.StartHidden = true
//                       );

//                        config = ConfigEncryptor.LoadConfig();

//                        if (config != null)
//                        {
//                            BiometricsIp = config.BiometricsIp;
//                            BiometricsPort = config.BiometricsPort;
//                            BiometricsCommKey = config.CommKey;
//                            DataTransferMode = config.DataTransferMode;
//                            IntervalTime = config.IntervalSeconds;
//                            ScheduledTime = config.ScheduledTime;
//                            //MessageBox.Show("Joe Mama");
//                        }

//                        mainForm = new Form1(config?.StartHidden ?? false);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    return;
//                }
//            }
//            else
//            {
//                mainForm = new Form1(config?.StartHidden ?? false);
//            }

//            if (mainForm != null)
//            {
//                Application.ApplicationExit += (s, e) =>
//                {
//                    if (Application.OpenForms.Count > 0)
//                    {
//                        var activeMainForm = Application.OpenForms[0] as Form1;
//                        activeMainForm?.CleanupNotifyIcon();
//                    }
//                };

//                // Handle UI thread exceptions
//                Application.ThreadException += (s, e) =>
//                {
//                    if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is Form1 form)
//                    {
//                        form.SafeInvoke(() =>
//                        {
//                            form.popUpMessage("Unhandled Error", e.Exception.Message, 1);
//                        });
//                    }
//                };

//                // Handle background thread exceptions (non-UI)
//                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
//                {
//                    if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is Form1 form)
//                    {
//                        var ex = e.ExceptionObject as Exception;
//                        if (ex != null)
//                        {
//                            form.SafeInvoke(() =>
//                            {
//                                form.popUpMessage("Critical Error", ex.Message, 1);
//                            });
//                        }
//                    }
//                };


//                Application.Run(mainForm);
//                if (mainForm.Tag?.ToString() == "Restart")
//                {
//                    goto RestartApp;
//                }
//            }
//        }

//    }
//}
