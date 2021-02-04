using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FileMonitorService.Core;
using FileMonitorService.Models;

namespace FileMonitorService.Core.UnitTests
{
    public class XmlSubscriptionStoreTests
    {
        private Subscription subscription = new Subscription()
        {
            Path = "..",
            InvokeMethodData = new InvokeMethodData()
            {
                AssemblyName = ".",
                ClassName = ".",
                MethodName = "."
            },
            IntervalInSeconds = 1
        };

        [Fact]
        public void TestGetAll()
        {
            ISubscriptionStore store = CreateSubscriptionStore();

            var result = store.GetAll();
            Assert.NotNull(result);
        }

        private static ISubscriptionStore CreateSubscriptionStore()
        {
            ISubscriptionStore store = new XmlSubscriptionStore(@"SubscriptionStore.xml");
            return store;
        }

        [Fact]
        public void TestGetFiles()
        {
            ISubscriptionStore store = CreateSubscriptionStore();

            var result = store.GetAll();
            if (result.Any())
            {
                var sub = result.FirstOrDefault();
                var files = sub.GetFileIndex();
                Assert.Equal(true, files.Any());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Create and Add subscription together to be sure that subscription is created prior to the deletion
        /// </remarks>
        [Fact]        
        public void CreateAndDeleteSubscription()
        {
            ISubscriptionStore store = CreateSubscriptionStore();
            store.Create(subscription);

            var createdSub = store.Get(subscription.Id);
            Assert.NotNull(createdSub);

            store.Delete(subscription.Id);
            createdSub = store.Get(subscription.Id);

            Assert.Null(createdSub);
        }

    }
}
