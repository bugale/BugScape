using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugScapeCommon {
    public enum EDirection {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4,
    }

    public enum EBugScapeOperation {
        GetMapState = 0,
        Move = 1
    }

    public enum EBugScapeResult {
        Success = 0,
        Error = 1
    }
}
