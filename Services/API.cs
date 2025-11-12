using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using offSiteTimekeeping.Models;

namespace offSiteTimekeeping.Services
{
    public static class API
    {
        public static async Task SendToApiAsync(List<BiometricLog> logs)
        {
            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(logs);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(/*Input API link->> */"", content);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to upload logs");
        }
    }
}