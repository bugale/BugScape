using System;
using System.Collections.Generic;
using System.Linq;
using BugScapeCommon;

namespace BugScape {
    internal class Program {
        private static void Main() {
            Console.WriteLine(@"Hello BugScape!");

            /* Add basic map for testing */
            using (var dbContext = new BugScapeDbContext()) {
                if (!dbContext.Maps.Any()) {
                    var m1 = new Map {
                        Size = new Point2D(500, 500),
                        IsNewCharacterMap = true,
                        MapObstacles =
                            new List<MapObstacle> {
                                new MapWall {
                                    Location = new Point2D(100, 100),
                                    Size = new Point2D(50, 50)
                                }
                            }
                    };
                    var m2 = new Map {
                        Size = new Point2D(800, 800),
                        MapObstacles =
                            new List<MapObstacle> {
                                new MapWall {
                                    Location = new Point2D(200, 200),
                                    Size = new Point2D(50, 50)
                                },
                                new MapWall {
                                    Location = new Point2D(300, 300),
                                    Size = new Point2D(100, 180)
                                }
                            }
                    };
                    var m3 = new Map {
                        Size = new Point2D(250, 250),
                        MapObstacles =
                            new List<MapObstacle> {
                                new MapWall {
                                    Location = new Point2D(0, 0),
                                    Size = new Point2D(250, 50)
                                },
                                new MapWall {
                                    Location = new Point2D(0, 0),
                                    Size = new Point2D(50, 250)
                                },
                                new MapWall {
                                    Location = new Point2D(200, 0),
                                    Size = new Point2D(50, 250)
                                },
                                new MapWall {
                                    Location = new Point2D(0, 200),
                                    Size = new Point2D(250, 50)
                                }
                            }
                    };
                    var p1 = new Portal {
                        IsDefaultSpawnable = true,
                        Location = new Point2D(10, 10),
                        Size = new Point2D(75, 75),
                        Map = m1
                    };
                    var p2 = new Portal {
                        IsDefaultSpawnable = true,
                        Location = new Point2D(10, 10),
                        Size = new Point2D(75, 75),
                        Map = m2
                    };
                    var p3 = new Portal {
                        IsDefaultSpawnable = true,
                        Location = new Point2D(400, 10),
                        Size = new Point2D(75, 75),
                        Map = m2
                    };
                    var p4 = new Portal {
                        IsDefaultSpawnable = true,
                        Location = new Point2D(100, 100),
                        Size = new Point2D(75, 75),
                        Map = m3
                    };
                    dbContext.Maps.Add(m1);
                    dbContext.Maps.Add(m2);
                    dbContext.Portals.Add(p1);
                    dbContext.Portals.Add(p2);
                    dbContext.Portals.Add(p3);
                    dbContext.Portals.Add(p4);
                    dbContext.SaveChanges();
                    p1.DestPortal = p2;
                    p2.DestPortal = p1;
                    p3.DestPortal = p4;
                    p4.DestPortal = p3;
                    dbContext.SaveChanges();
                }
            }

            new BugScapeServer().Run().Wait();
        }
    }
}
