using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugScapeCommon {
    public class BugScapeRequest {
        public EBugScapeOperation Operation;
        public int CharacterID;
    }

    public class BugScapeGetMapStateRequest : BugScapeRequest {
        public BugScapeGetMapStateRequest(int characterID) {
            this.Operation = EBugScapeOperation.GetMapState;
            this.CharacterID = characterID;
        }
    }

    public class BugScapeMoveRequest : BugScapeRequest {
        public EDirection Direction;

        public BugScapeMoveRequest(int characterID, EDirection direction) {
            this.Operation = EBugScapeOperation.Move;
            this.CharacterID = characterID;
            this.Direction = direction;
        }
    }

    public class BugScapeResponse {
        public EBugScapeResult Result;

        public BugScapeResponse(EBugScapeResult result) { this.Result = result; }
    }

    public class BugScapeMapResponse : BugScapeResponse {
        public Map Map;

        public BugScapeMapResponse(Map map) : base(EBugScapeResult.Success) { this.Map = map; }
    }
}
