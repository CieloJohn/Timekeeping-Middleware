using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.Logging;
using offSiteTimekeeping.Models;
using offSiteTimekeeping.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace   offSiteTimekeeping.Services
{
    public static class LocalDbService
    {
        private static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local_logs.db");

        static LocalDbService()
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var tableCmd = conn.CreateCommand();
            tableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS BiometricLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EnrollNumber TEXT NOT NULL,
                    InOutMode INTEGER,
                    ModType INTEGER,
                    Timestamp TEXT NOT NULL,
                    WorkCode INTEGER,
                    Synced INTEGER DEFAULT 0
                )";
            tableCmd.ExecuteNonQuery();

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "PRAGMA table_info(BiometricLogs)";
            using var reader = checkCmd.ExecuteReader();
            bool hasModType = false;
            bool hasDeviceSn = false;
            bool hasWorkCode = false;

            while (reader.Read())
            {
                string columnName = reader.GetString(1);
                if (columnName.Equals("ModType", StringComparison.OrdinalIgnoreCase))
                    hasModType = true;
                else if (columnName.Equals("DeviceSerialNumber", StringComparison.OrdinalIgnoreCase))
                    hasDeviceSn = true;
                else if (columnName.Equals("WorkCode", StringComparison.OrdinalIgnoreCase))
                    hasWorkCode = true;
            }

            if (!hasModType)
            {
                var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BiometricLogs ADD COLUMN ModType INTEGER DEFAULT 0";
                alterCmd.ExecuteNonQuery();
            }

            if (!hasDeviceSn)
            {
                var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BiometricLogs ADD COLUMN DeviceSerialNumber TEXT DEFAULT ''";
                alterCmd.ExecuteNonQuery();
            }

            if (!hasWorkCode)
            {
                var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BiometricLogs ADD COLUMN WorkCode INTEGER DEFAULT 0";
                alterCmd.ExecuteNonQuery();
            }

        }
        public static void SaveLogs(List<BiometricLog> logs)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            foreach (var log in logs)
            {
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT COUNT(*) FROM BiometricLogs 
                    WHERE EnrollNumber = @enroll AND InOutMode = @action AND ModType = @modtype 
                      AND Timestamp = @time AND WorkCode = @workcode AND DeviceSerialNumber = @serial";
                checkCmd.Parameters.AddWithValue("@enroll", log.EnrollNumber);
                checkCmd.Parameters.AddWithValue("@action", log.InOutMode);
                checkCmd.Parameters.AddWithValue("@modtype", log.ModType);
                checkCmd.Parameters.AddWithValue("@time", log.Timestamp.ToString("s"));
                checkCmd.Parameters.AddWithValue("@workcode", log.WorkCode);
                checkCmd.Parameters.AddWithValue("@serial", log.DeviceSerialNumber ?? "");

                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                    continue;
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
            INSERT INTO BiometricLogs (EnrollNumber, InOutMode, ModType, Timestamp, WorkCode, Synced, DeviceSerialNumber)
            VALUES (@enroll, @action, @modtype, @time, @workcode, 0, @serial)";
                insertCmd.Parameters.AddWithValue("@enroll", log.EnrollNumber);
                insertCmd.Parameters.AddWithValue("@action", log.InOutMode);
                insertCmd.Parameters.AddWithValue("@modtype", log.ModType);
                insertCmd.Parameters.AddWithValue("@time", log.Timestamp.ToString("s"));
                insertCmd.Parameters.AddWithValue("@workcode", log.WorkCode);
                insertCmd.Parameters.AddWithValue("@serial", log.DeviceSerialNumber ?? "");

                insertCmd.ExecuteNonQuery();
            }
        }
        public static void SaveLog(BiometricLog log)
        {
            SaveLogs(new List<BiometricLog> { log });
        }

        public static bool LogExists(BiometricLog log)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM BiometricLogs 
                WHERE EnrollNumber = @enroll AND InOutMode = @action AND ModType = @modtype 
                AND Timestamp = @time AND WorkCode = @workcode AND DeviceSerialNumber = @device";
            cmd.Parameters.AddWithValue("@enroll", log.EnrollNumber);
            cmd.Parameters.AddWithValue("@action", log.InOutMode);
            cmd.Parameters.AddWithValue("@modtype", log.ModType);
            cmd.Parameters.AddWithValue("@time", log.Timestamp.ToString("s"));
            cmd.Parameters.AddWithValue("@workcode", log.WorkCode);
            cmd.Parameters.AddWithValue("@device", log.DeviceSerialNumber ?? "");

            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }

        public static List<BiometricLog> GetUnsyncedLogs()
        {
            var result = new List<BiometricLog>();
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, EnrollNumber, InOutMode, ModType, Timestamp, WorkCode FROM BiometricLogs WHERE Synced = 0";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new BiometricLog
                {
                    Id = reader.GetInt32(0),
                    EnrollNumber = reader.GetString(1),
                    InOutMode = reader.GetInt32(2),
                    ModType = reader.GetInt32(3),
                    Timestamp = DateTime.Parse(reader.GetString(4)),
                    WorkCode = reader.GetInt32(5)
                });
            }

            return result;
        }

        public static void MarkLogsAsSynced(List<int> ids)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            foreach (var id in ids)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE BiometricLogs SET Synced = 1 WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<BiometricLog> GetAllLogs()
        {
            var result = new List<BiometricLog>();
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, EnrollNumber, InOutMode, ModType, Timestamp, WorkCode, Synced FROM BiometricLogs";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new BiometricLog
                {
                    Id = reader.GetInt32(0),
                    EnrollNumber = reader.GetString(1),
                    InOutMode = reader.GetInt32(2),
                    ModType = reader.GetInt32(3),
                    Timestamp = DateTime.Parse(reader.GetString(4)),
                    WorkCode = reader.GetInt32(5),
                    IsSynced = reader.GetInt32(6) == 1
                });
            }

            return result;
        }

        public static void DeleteLocalDatabase()
        {
            try
            {
                // Force dispose all connections
                SqliteConnection.ClearAllPools();

                if (File.Exists(DbPath))
                {
                    File.Delete(DbPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete local database: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
