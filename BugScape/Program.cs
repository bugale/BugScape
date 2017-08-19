using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScape {
    internal class Program {
        private static void Main(string[] args) {
            Console.WriteLine("Hello BugScape!");

            var server = new BugScapeServer();
            server.Run().Wait();
        }
    }
}
