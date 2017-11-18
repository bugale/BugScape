namespace BugScapeCommon {
    public class BugScapeMessage { }
    
    public class BugScapeRequestLogin : BugScapeMessage {
        public string Username { get; set; }
        public string Password { get; set; }

        public BugScapeRequestLogin(string username, string password) {
            this.Username = username;
            this.Password = password;
        }
    }
    public class BugScapeRequestRegister : BugScapeMessage {
        public string Username { get; set; }
        public string Password { get; set; }

        public BugScapeRequestRegister(string username, string password) {
            this.Username = username;
            this.Password = password;
        }
    }
    public class BugScapeRequestMove : BugScapeMessage {
        public EDirection Direction { get; }

        public BugScapeRequestMove(EDirection direction) {
            this.Direction = direction;
        }
    }

    public class BugScapeResponseLoginInvalidCredentials : BugScapeMessage { }
    public class BugScapeResponseLoginAlreadyLoggedIn : BugScapeMessage { }
    public class BugScapeResponseLoginSuccessful : BugScapeMessage {
        public Character Character { get; set; }
        public Map Map { get; set; }

        public BugScapeResponseLoginSuccessful(Character character) {
            this.Character = character;
            this.Map = character.Map;
        }
    }
    public class BugScapeResponseRegisterAlreadyExist : BugScapeMessage{ }
    public class BugScapeResponseRegisterSuccessful : BugScapeMessage {}

    public class BugScapeUpdateMapChanged : BugScapeMessage {
        public Map Map { get; set; }

        public BugScapeUpdateMapChanged(Map map) {
            this.Map = map;
        }
    }
}
