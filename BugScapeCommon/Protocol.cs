namespace BugScapeCommon {
    public enum EDirection {
        None,
        Left,
        Right,
        Up,
        Down,
    }

    public class BugScapeMessage { }
    public class BugScapeMessageUnexpectedError : BugScapeMessage {
        public string Message { get; set; }
    }

    public class BugScapeRequestLogin : BugScapeMessage {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class BugScapeResponseLoginInvalidCredentials : BugScapeMessage { }
    public class BugScapeResponseLoginAlreadyLoggedIn : BugScapeMessage { }
    public class BugScapeResponseLoginSuccessful : BugScapeMessage {
        public User User { get; set; }
    }

    public class BugScapeRequestRegister : BugScapeMessage {
        public User User { get; set; }
        public string Password { get; set; }
    }
    public class BugScapeResponseRegisterAlreadyExist : BugScapeMessage{ }
    public class BugScapeResponseRegisterSuccessful : BugScapeMessage { }

    public class BugScapeRequestCharacterRemove : BugScapeMessage {
        public Character Character { get; set; }
    }
    public class BugScapeRequestCharacterRemoveSuccessful : BugScapeMessage {
        public User User { get; set; }
    }

    public class BugScapeRequestCharacterCreate : BugScapeMessage {
        public Character Character { get; set; }
    }
    public class BugScapeResponseCharacterCreateAlreadyExist : BugScapeMessage { }
    public class BugScapeResponseCharacterCreateSuccessful : BugScapeMessage {
        public User User { get; set; }
    }

    public class BugScapeRequestCharacterEnter : BugScapeMessage {
        public Character Character { get; set; }
    }
    public class BugScapeResponseCharacterEnterSuccessful : BugScapeMessage {
        public Map Map { get; set; }
        public Character Character { get; set; }
    }

    public class BugScapeRequestMove : BugScapeMessage {
        public EDirection Direction { get; set; }
        public bool MoveMax { get; set; }
    }

    public class BugScapeResponseMapChanged : BugScapeMessage {
        public Map Map { get; set; }
    }
}
