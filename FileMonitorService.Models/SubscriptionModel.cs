namespace FileMonitorService.Models
{
    using System;

    public class SubscriptionModel
    {
        public long Id { get; set; }
        public string Path { get; set; }
        public bool IsRecursive { get; set; }
        public bool IsWatchingDirectories { get; set; }
        public bool IsWatchingFiles { get; set; }
        public InvokeMethodData InvokeMethodData { get; set; }
        public int IntervalInSeconds { get; set; }
        public DateTime? NextCheckDate { get; set; }
        public DateTime? LastRunDate { get; set; }

        public FileIndexModel FileIndex { get; set; }

        public static SubscriptionModel CreateFrom(Subscription subscription)
        {
            return new SubscriptionModel
            {
                Id = subscription.Id,
                Path = subscription.Path,
                IsRecursive = subscription.IsRecursive,
                IsWatchingDirectories = subscription.IsWatchingDirectories,
                IsWatchingFiles = subscription.IsWatchingFiles,
                InvokeMethodData = new InvokeMethodData(subscription.InvokeMethodData),
                IntervalInSeconds = subscription.IntervalInSeconds,
                NextCheckDate = subscription.NextCheckDate,
                LastRunDate = subscription.LastRunDate,
                FileIndex = FileIndexModel.CreateFrom(subscription.GetFileIndex())
            };
        }

        public Subscription Convert()
        {
            var subscription = new Subscription
            {
                Id = Id,
                Path = Path,
                IsRecursive = IsRecursive,
                IsWatchingDirectories = IsWatchingDirectories,
                IsWatchingFiles = IsWatchingFiles,
                InvokeMethodData = new InvokeMethodData(InvokeMethodData),
                IntervalInSeconds = IntervalInSeconds,
                NextCheckDate = NextCheckDate,
                LastRunDate = LastRunDate
            };

            foreach (var file in FileIndex.NetworkFiles)
            {
                subscription.AddToFileIndex(new NetworkFile
                {
                    Path = file.Path,
                    ModificationDate = file.ModificationDate
                });
            }

            return subscription;
        }
    }
}
