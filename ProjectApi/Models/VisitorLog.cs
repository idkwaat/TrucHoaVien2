using System;

namespace ProjectApi.Models
{
    public class VisitorLog
    {
        public int Id { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime VisitTime { get; set; } = DateTime.UtcNow;
    }
}
