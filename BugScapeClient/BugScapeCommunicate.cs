using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScapeClient {
    public static class BugScapeCommunicate {
        private static readonly TcpClient Client = new TcpClient();
        private static JsonStreamReader _clientReader;
        private static JsonStreamWriter _clientWriter;
        private static readonly object Mutex = new object();
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
        };

        public static BugScapeResponse SendBugScapeRequest(BugScapeRequest request) {
            lock (Mutex) {
                if (!Client.Connected) {
                    Client.Connect(ServerSettings.ServerAddress, ServerSettings.ServerPort);
                    _clientReader = new JsonStreamReader(Client.GetStream(), JsonSettings);
                    _clientWriter = new JsonStreamWriter(Client.GetStream(), JsonSettings);
                }

                _clientWriter.WriteObject(request);
                return _clientReader.ReadObject<BugScapeResponse>();
            }
        }
    }
}
