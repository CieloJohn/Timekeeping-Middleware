using Microsoft.Data.SqlClient;
using offSiteTimekeeping.Helpers;
using offSiteTimekeeping.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static ConfigEncryptor;

namespace offSiteTimekeeping.Services
{
    public static class DatabaseServices
    {
        public static string ConnectionString => GetConnectionString();
        public static string errorMessage = null;
        public static void SendLogsToCentralDb(List<BiometricLog> logs, string deviceSerial)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    foreach (var log in logs)
                    {
                        var empIDKey = new SqlCommand(
                            "SELECT emp_id FROM [HRIS].[dbo].[timekeeping_JointID] WHERE biometric_id = @enroll", conn);
                        empIDKey.Parameters.AddWithValue("@enroll", log.EnrollNumber);
                        var pmsIDkey = empIDKey.ExecuteScalar();

                        if (pmsIDkey == null)
                            throw new Exception($"No user found with enroll number {log.EnrollNumber}");
                            //MessageBox.Show($"[pmsIDkey == null] No user found with enroll number {log.EnrollNumber}");

                        var getUserIdCmd = new SqlCommand(
                            "SELECT employee_id FROM [HRIS].[dbo].[PMS_personnel_information] WHERE hris_id = @hris_id", conn);
                        getUserIdCmd.Parameters.AddWithValue("@hris_id", pmsIDkey);
                        var userIdObj = getUserIdCmd.ExecuteScalar();

                        if (userIdObj == null)
                            throw new Exception($"No user found with hris_id {pmsIDkey}");
                            //MessageBox.Show($"[userIdObj == null] No user found with hris_id {pmsIDkey}");

                        var userId = userIdObj;

                        var cmd = new SqlCommand(@"
                            INSERT INTO [HRIS].[dbo].[timekeeping_LogRecord] 
                                ([personnel_id],[user_id],[modtype],[action],[logtime],
                                 [workcode],[device_sn],[sched],[flexi],[no_mandosched])
                            VALUES 
                                (@personnel_id, @user_id, @modtype, @action, @logtime,
                                 @workcode, @device_sn, @sched, @flexi, @no_mandosched)", conn);

                        cmd.Parameters.AddWithValue("@personnel_id", pmsIDkey);
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@modtype", log.ModType);
                        cmd.Parameters.AddWithValue("@action", log.InOutMode);
                        cmd.Parameters.AddWithValue("@logtime", log.Timestamp);
                        cmd.Parameters.AddWithValue("@workcode", log.WorkCode);
                        cmd.Parameters.AddWithValue("@device_sn", deviceSerial);
                        cmd.Parameters.AddWithValue("@sched", DBNull.Value);
                        cmd.Parameters.AddWithValue("@flexi", 0);
                        cmd.Parameters.AddWithValue("@no_mandosched", 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("[catch] Database error: " + ex.Message);
            }
        }

        public static HashSet<string> GetExistingLogKeysInCentralDb(List<BiometricLog> logs, string deviceSerial)
        {
            var existingKeys = new HashSet<string>();

            if (logs == null || logs.Count == 0)
                return existingKeys;

            const int batchSize = 100;
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            for (int i = 0; i < logs.Count; i += batchSize)
            {
                var batch = logs.Skip(i).Take(batchSize).ToList();

                var conditions = new List<string>();
                var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 60;

                for (int j = 0; j < batch.Count; j++)
                {
                    var paramTime = $"@ts{j}";
                    var paramDevice = $"@sn{j}";
                    conditions.Add($"(logtime = {paramTime} AND device_sn = {paramDevice})");

                    cmd.Parameters.AddWithValue(paramTime, batch[j].Timestamp);
                    cmd.Parameters.AddWithValue(paramDevice, deviceSerial);
                }

                cmd.CommandText = $@"
                    SELECT logtime, device_sn
                    FROM [HRIS].[dbo].[timekeeping_LogRecord]
                    WHERE {string.Join(" OR ", conditions)}
                ";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var timestamp = reader.GetDateTime(0);
                    var sn = reader.GetString(1);
                    var key = $"{timestamp:yyyy-MM-dd HH:mm:ss}|{sn}";
                    existingKeys.Add(key);
                }
            }

            return existingKeys;
        }
        public static DateTime GetServerTime()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                using var cmd = new SqlCommand("SELECT GETDATE()", conn);
                return (DateTime)cmd.ExecuteScalar(); // comment out line if debugging

                //uncomment for debug
                //var result = cmd.ExecuteScalar();
                //    if (result != null && result is DateTime serverTime)
                //    {
                //        MessageBox.Show("Successfully retrieved server time: " + serverTime.ToString("yyyy-MM-dd HH:mm:ss"),
                //                        "Server Time Synced", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //        return serverTime;
                //    }
                //    else
                //    {
                //        MessageBox.Show("Server returned no valid time. Using local time instead.",
                //                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //        return DateTime.Now;
                //    }
                }
            catch /*(Exception ex)*/
            {
                //MessageBox.Show("Unable to retrieve server time. Using local time instead.\n\n" + ex.Message,
                //                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return DateTime.Now;
            }
        }

        public static bool TestDatabaseConnection()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                return true;
            }

            // uncomment for debugging
            //catch (SqlException ex)
            //{
            //    switch (ex.Number)
            //    {
            //        case 53:
            //            MessageBox.Show("Database server is unreachable. Check if the server is up and the network is available.",
            //                            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //            break;

            //        case 18456:
            //            MessageBox.Show("Invalid username or password. Please check your database credentials.",
            //                            "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //            break;

            //        default:
            //            MessageBox.Show($"SQL Error {ex.Number}: {ex.Message}",
            //                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //            break;
            //    }
            //    return false;
            // }

            catch /*(Exception ex)*/
            {
                //MessageBox.Show($"Unexpected error: {ex.Message}", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

        }
    }
}
