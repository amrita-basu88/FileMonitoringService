using FileMonitorService.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMonitorService.Data
{
    public class FileMonitorDbContext : DbContext
    {
        public FileMonitorDbContext()
            : base("DbContext")
        {
        }

        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<InvokeMethodData> InvokeMethodDatas { get; set; }
        public DbSet<NetworkFile> NetworkFiles { get; set; }
       
    }
}
