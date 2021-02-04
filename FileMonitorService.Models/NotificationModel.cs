using System;

namespace FileMonitorService.Models
{
    public class NotificationModel
    {
        public String Path { get; set; }
        public String Type { get; set; }
        public long SubscriptionId { get; set; }
    }
}
