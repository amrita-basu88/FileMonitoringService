namespace FileMonitorService.Models
{
    public interface INotifier
    {
        void Notify( NotificationData notificationData );
    }
}
