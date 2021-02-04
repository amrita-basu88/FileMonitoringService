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
    public class DatabaseSubscriptionStoreTests
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
            ISubscriptionStore store = new DatabaseSubscriptionStore();
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
                Assert.NotNull(files);
            }
        }

        /// <summary>
        /// Create And Delete Subscription
        /// </summary>
        /// <remarks>
        /// Create and Add subscription together to be sure that subscription is created prior to the deletion
        /// </remarks>
        [Fact]
        public void TestCreateAndDeleteSubscription_Success()
        {
            ISubscriptionStore store = CreateSubscriptionStore();
            store.Create(subscription);

            var createdSub = store.Get(subscription.Id);
            Assert.NotNull(createdSub);

            store.Delete(subscription.Id);
            createdSub = store.Get(subscription.Id);

            Assert.Null(createdSub);
        }

        [Fact]
        public void TestCreateAndDeleteCompleteSubscriptions()
        {
            List<string> paths = new List<string>();

            using (ISubscriptionStore store = CreateSubscriptionStore())
            {

                string camSerialDirectory = DateTime.Now.Ticks.ToString();
                string projectXmlFile = camSerialDirectory + "PROJECT.XML";

                for (int s = 0; s < 1; s++)
                {
                    Subscription newSub = new Subscription()
                    {
                        //Id = Guid.NewGuid(),
                        Path = "Sub_" + DateTime.Now.Ticks,
                        InvokeMethodData = new InvokeMethodData()
                        {
                            AssemblyName = ".",
                            ClassName = ".",
                            MethodName = ".",
                            MethodParameters = new List<InvokeMethodParameterData>()
                    {
                        new InvokeMethodParameterData
                        {
                            AssemblyName = typeof(String).Assembly.FullName,
                            ClassName = typeof(String).FullName,
                            XmlData = InvokeMethodParameterData.SerializeToXmlData( projectXmlFile )
                        },
                        new InvokeMethodParameterData
                        {
                            AssemblyName = typeof(String).Assembly.FullName,
                            ClassName = typeof(String).FullName,
                            XmlData = InvokeMethodParameterData.SerializeToXmlData( camSerialDirectory )
                        }
                    }
                        },
                        IntervalInSeconds = 1
                    };
                    paths.Add(newSub.Path);
                    store.Create(newSub);
                    var createdSub = store.Get(newSub.Id);
                    Assert.NotNull(createdSub);
                    Assert.NotNull(createdSub.InvokeMethodData);

                    for (int f = 0; f < 10; f++)
                    {
                        store.AddFile(createdSub, new NetworkFile() { Path = "File_" + DateTime.Now.Ticks + Guid.NewGuid(), ModificationDate = DateTime.Now });
                    }
                }
            }

            // Delete
            using (ISubscriptionStore store = CreateSubscriptionStore())
            {
                var subToDelete = store.GetAll().Where(s => paths.Contains(s.Path)).FirstOrDefault();
                while (subToDelete != null)
                {
                    store.Delete(subToDelete.Id);
                    subToDelete = store.GetAll().Where(s => paths.Contains(s.Path)).FirstOrDefault();
                }

                Assert.False(store.GetAll().Any(s => paths.Contains(s.Path)));
            }
        }

        [Fact]
        public void TestRemoveNetworkFile()
        {
            ISubscriptionStore store = CreateSubscriptionStore();

            var sub = store.GetAll().FirstOrDefault();
            if (sub != null && sub.fileIndex.Any())
            {
                long fileId = sub.fileIndex.First().Id;
                store.DeleteFile(sub, sub.fileIndex.First());

                var sub2 = store.Get(sub.Id);
                Assert.False(sub2.fileIndex.Any(f => f.Id == fileId));
            }
        }
    }
}
