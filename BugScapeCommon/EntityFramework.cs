using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace BugScapeCommon {
    public class BugScapeDbContext : DbContext {
        public BugScapeDbContext() : base("name=BugScapeDBConnStr") { }

        public DbSet<Map> Maps { get; set; }
        public DbSet<MapObstacle> MapObstacles { get; set; }
        public DbSet<Portal> Portals { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<User> Users { get; set; }

        public Dictionary<int, Map> GetMapStructureDict() {
            var dict = new Dictionary<int, Map>();

            /* Convert to list here to prevent database access inside loop from interfering with the maps request */
            foreach (var map in this.Maps.ToList()) {
                dict[map.ID] = (Map)map.CloneFromDatabase();
            }

            /* Set all portal dest connections */
            foreach (var portal in dict.Values.SelectMany(map => map.Portals)) {
                var destPortal = this.Portals.Single(x => x.ID == portal.ID).DestPortal;
                portal.DestPortal = dict[destPortal.Map.ID].Portals.Single(x => x.ID == destPortal.ID);
            }

            return dict;
        }
        public async Task<Dictionary<int, Map>> GetMapStructureDictAsync() {
            var dict = new Dictionary<int, Map>();

            /* Convert to list here to prevent database access inside loop from interfering with the maps request */
            foreach (var map in await this.Maps.ToListAsync()) {
                dict[map.ID] = (Map)map.CloneFromDatabase();
            }

            /* Set all portal dest connections */
            foreach (var portal in dict.Values.SelectMany(map => map.Portals)) {
                var destPortal = (await this.Portals.SingleAsync(x => x.ID == portal.ID)).DestPortal;
                portal.DestPortal = dict[destPortal.Map.ID].Portals.Single(x => x.ID == destPortal.ID);
            }

            return dict;
        }
    }
}
