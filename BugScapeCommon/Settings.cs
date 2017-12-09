namespace BugScapeCommon {
    public static class ClientSettings {
        public static int CharacterSizeX => 50;
        public static int CharacterSizeY => 50;
    }

    public static class ServerSettings {
        public static int ServerPort => 8081;
        public static string ServerAddress => "bugalit.com";
        public static int PasswordHashSaltLength => 128;
        public static int PasswordHashLength => 128;
        public static int PasswordHashIterations => 1024;
    }
}
