﻿using System;
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
        public static string ServerAddress = "http://127.0.0.1:8081/";
    }
}
