namespace BugScapeCommon {
    public class BugScapeRequest { }

    public class BugScapeRequestRegister : BugScapeRequest {
        public string Username { get; set; }
        public string Password { get; set; }

        public BugScapeRequestRegister(string username, string password) {
            this.Username = username;
            this.Password = password;
        }
    }

    public class BugScapeRequestLogin : BugScapeRequest {
        public string Username { get; set; }
        public string Password { get; set; }

        public BugScapeRequestLogin(string username, string password) {
            this.Username = username;
            this.Password = password;
        }
    }

    public class BugScapeRequestGame : BugScapeRequest {
        public int CharacterID { get; set; }
    }

    public class BugScapeRequestMapState : BugScapeRequestGame {
        public BugScapeRequestMapState(int characterID) {
            this.CharacterID = characterID;
        }
    }

    public class BugScapeRequestMove : BugScapeRequestGame {
        public EDirection Direction { get; }

        public BugScapeRequestMove(int characterID, EDirection direction) {
            this.CharacterID = characterID;
            this.Direction = direction;
        }
    }


    public class BugScapeResponse {
        public EBugScapeResult Result { get; set; }

        public string ResultExplain { get; set; }

        public BugScapeResponse(EBugScapeResult result) { this.Result = result; }
    }

    public class BugScapeResponseMapState : BugScapeResponse {
        public Map Map { get; set; }

        public BugScapeResponseMapState(Map map) : base(EBugScapeResult.Success) { this.Map = map; }
    }

    public class BugScapeResponseUser : BugScapeResponse {
        public User User { get; set; }

        public BugScapeResponseUser(User user) : base(EBugScapeResult.Success) { this.User = user; }
    }
}
