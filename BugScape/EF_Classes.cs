using System;
using System.Data.Entity;
using BugScapeCommon;
using Newtonsoft.Json.Serialization;

namespace BugScape {
    public class EntityFrameworkSerializationBinder : DefaultSerializationBinder {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName) {
            var realType = serializedType.Namespace == "System.Data.Entity.DynamicProxies" ? serializedType.BaseType : serializedType;
            base.BindToName(realType, out assemblyName, out typeName);
        }
    }

    public class BugScapeDbContext : DbContext {
        public BugScapeDbContext() : base("name=BugScapeDBConnStr") { }

        public DbSet<Map> Maps { get; set; }
        public DbSet<Character> Characters { get; set; }
    }
}
