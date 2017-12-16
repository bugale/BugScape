namespace BugScapeCommon {
    public static class ServerSettings {
        public static int ServerPort => 8081;
        public static string ServerAddress => "bugalit.com";
        public static int PasswordHashSaltLength => 128;
        public static int PasswordHashLength => 128;
        public static int PasswordHashIterations => 1024;
    }
}
