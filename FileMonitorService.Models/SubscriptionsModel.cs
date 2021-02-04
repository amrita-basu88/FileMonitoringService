namespace FileMonitorService.Models
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("Subscriptions")]
    public class SubscriptionsModel
    {
        [XmlElement("Subscription")]
        public List<SubscriptionModel> Subscriptions { get; set; }

        public List<Subscription> Convert()
        {
            var subscriptions = new List<Subscription>();
            foreach (var subscription in Subscriptions)
            {
                subscriptions.Add(subscription.Convert());
            }

            return subscriptions;
        }

        public static List<Subscription> Convert(IEnumerable<SubscriptionModel> subscriptionModels)
        {
            var subscriptions = new List<Subscription>();
            foreach (var model in subscriptionModels)
            {
                subscriptions.Add(model.Convert());
            }

            return subscriptions;
        }
    }
}
