using Microsoft.Win32;
using System;

namespace offSiteTimekeeping_NET8.Helpers
{
    internal class RegistryInterface
    {
        private string appName = "PVAOTimekeepingMiddleware";
        public void Register(string exePath)
        {
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

        public void Remove(string exePath)
        {
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (reg == null) return;

                string[] valueNames = reg.GetValueNames();

                foreach (string name in valueNames)
                {
                    object val = reg.GetValue(name);

                    if (val is string path &&
                        path.Equals(exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        reg.DeleteValue(name, false);
                    }
                }
            }
        }

        public bool Exists(string exePath)
        {
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                if (reg == null) return false;

                object val = reg.GetValue(appName);

                return val is string path &&
                       path.Equals(exePath, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
