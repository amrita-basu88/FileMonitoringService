using System;

namespace FileMonitorService.Models
{
    [Serializable]
    public class NotificationData
    {
        public InvokeMethodData InvokeMethodData { get; set; }
        public String FilePath { get; set; }
        public NotificationType NotificationType { get; set; }
        public long SubscriptionId { get; set; }
    }
}
