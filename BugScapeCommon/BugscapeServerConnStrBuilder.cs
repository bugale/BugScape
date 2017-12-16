namespace BugScapeCommon {
    public class BugscapeServerConnStrBuilder {
        public BugscapeServerConnStrBuilder() {
            this.Server = "";
            this.Port = 0;
        }
        public BugscapeServerConnStrBuilder(string connStr) {
            this.Server = connStr.Split(':')[0];
            this.Port = int.Parse(connStr.Split(':')[1]);
        }

        public string Server { get; set; }
        public int Port { get; set; }

        public string ConnStr => this.Server + ":" + this.Port.ToString();
    }
}
