using System;
using System.Collections.Generic;
using System.Linq;
using BugScapeCommon;

namespace BugScape {
    internal class Program {
        private static void Main() {
            Console.WriteLine("Hello BugScape!");

            /* Add basic map for testing */
            using (var dbContext = new BugScapeDbContext()) {
                if (!dbContext.Maps.Any()) {
                    var m = new Map {
                        Size = new Point2D(500, 500),
                        IsNewCharacterMap = true,
                        MapObjects =
                            new List<MapObject> {
                                new MapWall {
                                    Color = new RgbColor(0, 0, 0),
                                    Location = new Point2D(100, 100),
                                    Size = new Point2D(50, 50),
                                    IsBlocking = true
                                }
                            }
                    };
                    dbContext.Maps.Add(m);
                }
                dbContext.SaveChanges();
            }

            new BugScapeServer().Run().Wait();
        }
    }
}
