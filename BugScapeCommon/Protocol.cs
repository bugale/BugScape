using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BugScapeCommon {
    public class JsonStreamReader {
        private readonly Stream _stream;
        private readonly JsonSerializerSettings _settings;

        public JsonStreamReader(Stream stream, JsonSerializerSettings settings) {
            this._stream = stream;
            this._settings = settings;
        }

        public async Task<T> ReadObjectAsync<T>() {
            /* Read length */
            var lengthBuffer = new byte[2];
            await this._stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);

            /* Read data */
            var length = BitConverter.ToUInt16(lengthBuffer, 0);
            var dataBuffer = new byte[length];
            await this._stream.ReadAsync(dataBuffer, 0, dataBuffer.Length);

            /* Parse data */
            var dataStr = Encoding.UTF8.GetString(dataBuffer);
            return await JsonConvert.DeserializeObjectAsync<T>(dataStr, this._settings);
        }
        public T ReadObject<T>() {
            /* Read length */
            var lengthBuffer = new byte[2];
            this._stream.Read(lengthBuffer, 0, lengthBuffer.Length);

            /* Read data */
            var length = BitConverter.ToUInt16(lengthBuffer, 0);
            var dataBuffer = new byte[length];
            this._stream.Read(dataBuffer, 0, dataBuffer.Length);

            /* Parse data */
            var dataStr = Encoding.UTF8.GetString(dataBuffer);
            return JsonConvert.DeserializeObject<T>(dataStr, this._settings);
        }
    }
    public class JsonStreamWriter {
        private readonly Stream _stream;
        private readonly JsonSerializerSettings _settings;

        public JsonStreamWriter(Stream stream, JsonSerializerSettings settings) {
            this._stream = stream;
            this._settings = settings;
        }

        public async Task WriteObjectAsync(object obj) {
            var dataStr = await JsonConvert.SerializeObjectAsync(obj, Formatting.None, this._settings);
            var dataBuffer = Encoding.UTF8.GetBytes(dataStr);
            var length = (short)dataBuffer.Length;
            var lengthBuffer = BitConverter.GetBytes(length);

            await this._stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
            await this._stream.WriteAsync(dataBuffer, 0, dataBuffer.Length);
        }
        public void WriteObject(object obj) {
            var dataStr = JsonConvert.SerializeObject(obj, Formatting.None, this._settings);
            var dataBuffer = Encoding.UTF8.GetBytes(dataStr);
            var length = (short)dataBuffer.Length;
            var lengthBuffer = BitConverter.GetBytes(length);

            this._stream.Write(lengthBuffer, 0, lengthBuffer.Length);
            this._stream.Write(dataBuffer, 0, dataBuffer.Length);
        }
    }

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
