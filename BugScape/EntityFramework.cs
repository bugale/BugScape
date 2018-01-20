using System.Data.Entity;
using BugScapeCommon;

namespace BugScape {
    public class BugScapeDbContext : DbContext {
        public BugScapeDbContext() : base("name=BugScapeDBConnStr") { }

        public DbSet<Map> Maps { get; set; }
        public DbSet<MapObstacle> MapObstacles { get; set; }
        public DbSet<Portal> Portals { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
