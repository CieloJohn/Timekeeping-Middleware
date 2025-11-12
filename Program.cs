//using offSiteTimekeeping.Helpers;
using offSiteTimekeeping.Helpers;
using offSiteTimekeeping_NET8.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace offSiteTimekeeping_NET_8
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

        [STAThread]
        static void Main()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
            File.AppendAllText(logPath, $"Started at {DateTime.Now}\n");
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
                            //MessageBox.Show("Joe Mama");
                        }

                        mainForm = new Form1(config?.StartHidden ?? false);
                    }
                }
                catch (Exception ex)
                {
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
                    if (Application.OpenForms.Count > 0)
                    {
                        var activeMainForm = Application.OpenForms[0] as Form1;
                        activeMainForm?.CleanupNotifyIcon();
                    }
                };

                // Handle UI thread exceptions
                Application.ThreadException += (s, e) =>
                {
                    if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is Form1 form)
                    {
                        form.SafeInvoke(() =>
                        {
                            form.popUpMessage("Unhandled Error", e.Exception.Message, 1);
                        });
                    }
                };

                // Handle background thread exceptions (non-UI)
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is Form1 form)
                    {
                        var ex = e.ExceptionObject as Exception;
                        if (ex != null)
                        {
                            form.SafeInvoke(() =>
                            {
                                form.popUpMessage("Critical Error", ex.Message, 1);
                            });
                        }
                    }
                };


                Application.Run(mainForm);
                if (mainForm.Tag?.ToString() == "Restart")
                {
                    goto RestartApp;
                }
            }
        }

    }
}
