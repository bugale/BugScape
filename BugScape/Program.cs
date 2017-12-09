﻿using System;
using System.Linq;
using BugScapeCommon;

namespace BugScape {
    internal class Program {
        private static void Main() {
            Console.WriteLine("Hello BugScape!");

            /* Add basic map for testing */
            using (var dbContext = new BugScapeDbContext()) {
                if (!dbContext.Maps.Any()) {
                    dbContext.Maps.Add(new Map {Width = 500, Height = 500, IsNewCharacterMap = true});
                }
                dbContext.SaveChanges();
            }

            new BugScapeServer().Run().Wait();
        }
    }
}
