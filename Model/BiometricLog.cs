using System;

namespace offSiteTimekeeping.Models
{ 
    public class BiometricLog
        {
        public int Id { get; set; }
        public string EnrollNumber { get; set; }
        public int InOutMode { get; set; }
        public int ModType { get; set; }
        public DateTime Timestamp { get; set; }
        public int WorkCode { get; set; }
        public bool IsSynced { get; set; }
        public string DeviceSerialNumber { get; set; }
    }
}