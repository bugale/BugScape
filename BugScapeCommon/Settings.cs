using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BugScapeCommon {
    public static class ClientSettings {
        public static int GuiTileX = 50;
        public static int GuiTileY = 50;
        public static Rect GuiTileRect = new Rect(new Point(0, 0), new Point(GuiTileX, GuiTileY));
    }

    public static class ServerSettings {
        public static int ServerPort = 8081;
        public static string ServerAddress = "bugalit.com";
        public static int PasswordHashSaltLength = 128;
        public static int PasswordHashLength = 128;
        public static int PasswordHashIterations = 1024;
    }
}
