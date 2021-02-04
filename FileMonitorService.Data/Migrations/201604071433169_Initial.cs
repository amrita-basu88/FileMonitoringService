namespace FileMonitorService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.InvokeMethodDatas",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        AssemblyName = c.String(),
                        ClassName = c.String(),
                        MethodName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.InvokeMethodParameterDatas",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        AssemblyName = c.String(),
                        ClassName = c.String(),
                        XmlData = c.String(),
                        InvokeMethodDataId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.InvokeMethodDatas", t => t.InvokeMethodDataId, cascadeDelete: true)
                .Index(t => t.InvokeMethodDataId);
            
            CreateTable(
                "dbo.NetworkFiles",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Path = c.String(),
                        ModificationDateText = c.String(),
                        SubscriptionId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Subscriptions", t => t.SubscriptionId, cascadeDelete: true)
                .Index(t => t.SubscriptionId);
            
            CreateTable(
                "dbo.Subscriptions",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Path = c.String(),
                        IsRecursive = c.Boolean(nullable: false),
                        IsWatchingDirectories = c.Boolean(nullable: false),
                        IsWatchingFiles = c.Boolean(nullable: false),
                        IntervalInSeconds = c.Int(nullable: false),
                        NextCheckDate = c.DateTime(),
                        LastRunDate = c.DateTime(),
                        InvokeMethodData_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.InvokeMethodDatas", t => t.InvokeMethodData_Id, cascadeDelete: true)
                .Index(t => t.InvokeMethodData_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Subscriptions", "InvokeMethodData_Id", "dbo.InvokeMethodDatas");
            DropForeignKey("dbo.NetworkFiles", "SubscriptionId", "dbo.Subscriptions");
            DropForeignKey("dbo.InvokeMethodParameterDatas", "InvokeMethodDataId", "dbo.InvokeMethodDatas");
            DropIndex("dbo.Subscriptions", new[] { "InvokeMethodData_Id" });
            DropIndex("dbo.NetworkFiles", new[] { "SubscriptionId" });
            DropIndex("dbo.InvokeMethodParameterDatas", new[] { "InvokeMethodDataId" });
            DropTable("dbo.Subscriptions");
            DropTable("dbo.NetworkFiles");
            DropTable("dbo.InvokeMethodParameterDatas");
            DropTable("dbo.InvokeMethodDatas");
        }
    }
}
