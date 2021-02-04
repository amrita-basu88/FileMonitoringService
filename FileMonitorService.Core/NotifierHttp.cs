using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using FileMonitorService.Core.Models;

namespace FileMonitorService.Core
{
    public class NotifierHttp
    {
        public static void SendHttp(NotificationModel notificationModel, Guid subscriptionId, string notificationURL)
        {
            using (var client = new HttpClient())
            {
                var serializedModel = JsonConvert.SerializeObject(notificationModel);
                var content = new StringContent(serializedModel, Encoding.UTF8);
                content.Headers.ContentType.MediaType = "application/json";
                content.Headers.ContentType.CharSet = "utf-8";
                var response = client.PostAsync(notificationURL, content).Result;

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new InvalidResponseStatusCodeException((int)response.StatusCode);
                }
            }
        }
    }
}
