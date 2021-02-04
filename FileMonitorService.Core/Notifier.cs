using FileMonitorService.Models;

namespace FileMonitorService.Core
{
    public class Notifier : INotifier
    {
        public void Notify(NotificationData notificationData)
        {
            var model = new NotificationModel
            {
                Path = notificationData.FilePath,
                Type = notificationData.NotificationType.ToString().ToLower(),
                SubscriptionId = notificationData.SubscriptionId
            };

            InvokeMethodHelper.InvokeMethodByReflection(model, notificationData.InvokeMethodData);
        }
    }
}
