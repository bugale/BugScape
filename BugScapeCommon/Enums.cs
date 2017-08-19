using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugScapeCommon {
    public enum EDirection {
        None,
        Left,
        Right,
        Up,
        Down,
    }

    public enum EBugScapeResult {
        Success,
        Error,
        ErrorUserAlreadyExists,
        ErrorInvalidCredentials
    }
}
