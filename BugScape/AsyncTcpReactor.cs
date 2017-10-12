using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScape {
    public class AsyncJsonTcpReactor<TRequest, TResponse> {
        public delegate Task<TResponse> RequestHandler(TRequest request);

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            Binder = new EntityFrameworkSerializationBinder()
        };

        private readonly IPAddress _address;
        private readonly int _port;
        private readonly Dictionary<Type, RequestHandler> _handlerDictionary = new Dictionary<Type, RequestHandler>();

        public AsyncJsonTcpReactor(IPAddress address, int port) {
            this._port = port;
            this._address = address;
        }

        public void SetHandler(Type type, RequestHandler handler) { this._handlerDictionary[type] = handler; }

        public async Task Run() {
            var listener = new TcpListener(this._address, this._port);
            var tasks = new List<Task>();
            listener.Start();

            while (true) {
                var tcpClient = await listener.AcceptTcpClientAsync();
                tasks.Add(this.ProcessConnection(tcpClient));
                /* TODO: Remove it from the list when done */
            }
        }

        private async Task ProcessConnection(TcpClient tcpClient) {
            var stream = tcpClient.GetStream();
            var reader = new JsonStreamReader(stream, this._jsonSettings);
            var writer = new JsonStreamWriter(stream, this._jsonSettings);

            try {
                while (true) {
                    var request = await reader.ReadObjectAsync<TRequest>();
                    if (!this._handlerDictionary.ContainsKey(request.GetType())) {
                        Console.WriteLine("Invalid operation type {0}", request.GetType());
                    }
                    var response = await this._handlerDictionary[request.GetType()](request);
                    await writer.WriteObjectAsync(response);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            } finally {
                tcpClient.Close();
            }
        }
    }
}
