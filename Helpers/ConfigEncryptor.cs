using TimekeepingMiddleware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

public static class ConfigEncryptor
{
    private const string SecretKey = "MySuperSecurePassphrase123";
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("TimekeepingSalt123");
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TimekeepingMiddleware", "connection.json");

    public class ConfigData
    {
        public string ConnectionString { get; set; }
        public string BiometricsIp { get; set; }
        public int BiometricsPort { get; set; }
        public int CommKey { get; set; }
        public string DataTransferMode { get; set; }
        public int IntervalSeconds { get; set; }
        public string ScheduledTimeString { get; set; }
        public bool StartHidden { get; set; } = false;

        [System.Text.Json.Serialization.JsonIgnore]
        public TimeSpan ScheduledTime
        {
            get => TimeSpan.TryParse(ScheduledTimeString, out var ts) ? ts : TimeSpan.Zero;
            set => ScheduledTimeString = value.ToString();
        }
    }

    public static void SaveConfig(
    string ip, int port, int commKey, string dataTransferMode = null,
    int intervalSeconds = 0, TimeSpan? scheduledTime = null, bool startHidden = false)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            Dictionary<string, string> data;
            if (File.Exists(ConfigPath))
            {
                var jsonExisting = File.ReadAllText(ConfigPath);
                data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonExisting)
                       ?? new Dictionary<string, string>();
            }
            else
            {
                data = new Dictionary<string, string>();
            }
            data["BiometricsIP"] = Encrypt(ip ?? "");
            data["BiometricsPort"] = Encrypt(port.ToString());
            data["BiometricsComKey"] = Encrypt(commKey.ToString());
            data["DataTransferMode"] = Encrypt(dataTransferMode ?? "");
            data["IntervalSeconds"] = Encrypt(intervalSeconds.ToString());
            data["ScheduledTime"] = Encrypt((scheduledTime ?? TimeSpan.Zero).ToString());
            data["StartHidden"] = Encrypt(startHidden.ToString());

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            //MessageBox.Show("Error saving config:\n" + ex.Message,
            //    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new Exception("Error saving config:\n" + ex.Message + "Save Error");

        }
    }


    public static ConfigData? LoadConfig()
    {
        if (!File.Exists(ConfigPath))
            return null;

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            return new ConfigData
            {
                ConnectionString = Decrypt(data["ConnectionString"]),
                BiometricsIp = Decrypt(data["BiometricsIP"]),
                BiometricsPort = int.TryParse(Decrypt(data["BiometricsPort"]), out int port) ? port : 0,
                CommKey = int.TryParse(Decrypt(data["BiometricsComKey"]), out int comKey) ? comKey : 0,
                DataTransferMode = data.ContainsKey("DataTransferMode") ? Decrypt(data["DataTransferMode"]) : null,
                IntervalSeconds = data.ContainsKey("IntervalSeconds") && int.TryParse(Decrypt(data["IntervalSeconds"]), out int sec) ? sec : 0,
                ScheduledTimeString = data.ContainsKey("ScheduledTime") ? Decrypt(data["ScheduledTime"]) : null,
                StartHidden = data.ContainsKey("StartHidden") && bool.TryParse(Decrypt(data["StartHidden"]), out bool hidden) && hidden
            };
        }
        catch (Exception ex)
        {
            //MessageBox.Show("Error loading biometrics config:\n" + ex.Message,
            //    "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    public static void ClearConfig()
    {
        if (File.Exists(ConfigPath))
            File.Delete(ConfigPath);
    }

    public static string GetConnectionString()
    {
        if (!File.Exists(ConfigPath))
            throw new FileNotFoundException("Connection config not found.");

        var json = File.ReadAllText(ConfigPath);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        string encryptedBase64 = data["ConnectionString"];
        var cipherBytes = Convert.FromBase64String(encryptedBase64);

        using var aes = Aes.Create();
        using var key = new Rfc2898DeriveBytes(SecretKey, Salt, 10000);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16); 

        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    public static ConfigData AccessBiometricsCredentials()
    {
        if (!File.Exists(ConfigPath))
            throw new FileNotFoundException("Config file not found");

        var json = File.ReadAllText(ConfigPath);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        var config = new ConfigData
        {
            BiometricsIp = Decrypt(data["BiometricsIP"]),
            BiometricsPort = int.TryParse(Decrypt(data["BiometricsPort"]), out int port) ? port : 0,
            CommKey = int.TryParse(Decrypt(data["BiometricsComKey"]), out int comKey) ? comKey : 0,
            DataTransferMode = data.ContainsKey("DataTransferMode") ? Decrypt(data["DataTransferMode"]) : null
        };
        return config;
    }
    public static bool IsBiometricsCredentialsNull()
    {
        try
        {
            var config = AccessBiometricsCredentials();
            return !string.IsNullOrWhiteSpace(config.BiometricsIp) ||
                    config.BiometricsPort <= 0 ||
                    config.CommKey <= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string Decrypt(string base64)
    {
        var cipherBytes = Convert.FromBase64String(base64);
        using var aes = Aes.Create();
        using var key = new Rfc2898DeriveBytes(SecretKey, Salt, 10000);
        aes.Key = key.GetBytes(32); // AES-256 key
        aes.IV = key.GetBytes(16);

        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    //public static void SaveConnectionString(string plainText)
    //{
    //    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

    //    var encrypted = Encrypt(plainText);

    //    var data = new Dictionary<string, string>
    //    {
    //        ["ConnectionString"] = encrypted
    //    };

    //    File.WriteAllText(ConfigPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    //}

    private static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        using var key = new Rfc2898DeriveBytes(SecretKey, Salt, 10000);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }
}
