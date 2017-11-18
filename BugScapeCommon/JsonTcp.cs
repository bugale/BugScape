using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
            var bytesRead = 0;

            /* Read length */
            var lengthBuffer = new byte[2];
            while (bytesRead < lengthBuffer.Length) {
                bytesRead += await this._stream.ReadAsync(lengthBuffer, bytesRead, lengthBuffer.Length - bytesRead);
            }

            /* Read data */
            var length = BitConverter.ToUInt16(lengthBuffer, 0);
            var dataBuffer = new byte[length];
            bytesRead = 0;
            while (bytesRead < length) {
                bytesRead += await this._stream.ReadAsync(dataBuffer, bytesRead, length - bytesRead);
            }

            /* Parse data */
            var dataStr = Encoding.UTF8.GetString(dataBuffer);
            return await JsonConvert.DeserializeObjectAsync<T>(dataStr, this._settings);
        }
        public T ReadObject<T>() {
            var bytesRead = 0;

            /* Read length */
            var lengthBuffer = new byte[2];
            while (bytesRead < lengthBuffer.Length) {
                bytesRead += this._stream.Read(lengthBuffer, bytesRead, lengthBuffer.Length - bytesRead);
            }

            /* Read data */
            var length = BitConverter.ToUInt16(lengthBuffer, 0);
            var dataBuffer = new byte[length];
            bytesRead = 0;
            while (bytesRead < length) {
                bytesRead += this._stream.Read(dataBuffer, bytesRead, length - bytesRead);
            }

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
            var dataLength = (short)Encoding.UTF8.GetByteCount(dataStr);
            var lengthBuffer = BitConverter.GetBytes(dataLength);
            
            // Create frame buffer
            var frameBuffer = new byte[lengthBuffer.Length + dataLength];

            // Insert data to buffer
            Encoding.UTF8.GetBytes(dataStr, 0, dataLength, frameBuffer, lengthBuffer.Length);

            // Insert length to buffer
            for (var i = 0; i < lengthBuffer.Length; i++) frameBuffer[i] = lengthBuffer[i];

            // Send frame
            await this._stream.WriteAsync(frameBuffer, 0, frameBuffer.Length);
        }
        public void WriteObject(object obj) {
            var dataStr = JsonConvert.SerializeObject(obj, Formatting.None, this._settings);
            var dataLength = (short)Encoding.UTF8.GetByteCount(dataStr);
            var lengthBuffer = BitConverter.GetBytes(dataLength);

            // Create frame buffer
            var frameBuffer = new byte[lengthBuffer.Length + dataLength];

            // Insert data to buffer
            Encoding.UTF8.GetBytes(dataStr, 0, dataLength, frameBuffer, lengthBuffer.Length);

            // Insert length to buffer
            for (var i = 0; i < lengthBuffer.Length; i++) frameBuffer[i] = lengthBuffer[i];

            // Send frame
            this._stream.Write(frameBuffer, 0, frameBuffer.Length);
        }
    }

    public class JsonClient {
        private readonly Stream _stream;
        private readonly JsonStreamReader _reader;
        private readonly JsonStreamWriter _writer;

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All
        };

        public JsonClient(Stream stream) {
            this._stream = stream;
            this._reader = new JsonStreamReader(stream, this._jsonSettings);
            this._writer = new JsonStreamWriter(stream, this._jsonSettings);
        }

        public async Task<T> RecvObjectAsync<T>() { return await this._reader.ReadObjectAsync<T>(); }
        public T RecvObject<T>() { return this._reader.ReadObject<T>(); }

        public async Task SendObjectAsync(object obj) { await this._writer.WriteObjectAsync(obj); }
        public void SendObject(object obj) { this._writer.WriteObject(obj); }

        public void Close() { this._stream.Close(); }
    }
    public class AsyncJsonTcpReactor<TDataBaseType> {
        public enum ReactorAction {
            Connected,
            ReceivedData,
            Disconnected
        }
        public delegate Task AsyncHandler(JsonClient client, ReactorAction action, TDataBaseType data);
        
        private readonly Dictionary<ReactorAction, AsyncHandler> _handlerDictionary = new Dictionary<ReactorAction, AsyncHandler>();
        private readonly Dictionary<JsonClient, Tuple<Task, Task>> _tasks = new Dictionary<JsonClient, Tuple<Task, Task>>();
        private readonly Dictionary<JsonClient, BufferBlock<TDataBaseType>> _writeQueues = new Dictionary<JsonClient, BufferBlock<TDataBaseType>>();

        public void SetHandler(ReactorAction action, AsyncHandler handler) {
            if (handler == null) {
                this._handlerDictionary.Remove(action);
            } else {
                this._handlerDictionary[action] = handler;
            }
        }   
        
        public async Task RunServerAsync(IPAddress address, int port) {
            var listener = new TcpListener(address, port);
            listener.Start();

            while (true) {
                var tcpClient = await listener.AcceptTcpClientAsync();
                tcpClient.NoDelay = true;
                var client = new JsonClient(tcpClient.GetStream());
                this._writeQueues[client] = new BufferBlock<TDataBaseType>();
                this._tasks[client] = new Tuple<Task, Task>(this.ClientReadTask(client),
                                                            this.ClientWriteTask(client, this._writeQueues[client]));
            }
        }

        public async Task ClientDisconnectAsync(JsonClient client) {
            await this.InvokeHandler(client, ReactorAction.Disconnected, default(TDataBaseType));
            this._writeQueues.Remove(client);
            this._tasks.Remove(client);
            client.Close();
        }
        public async Task SendDataAsync(JsonClient client, TDataBaseType data) {
            await this._writeQueues[client].SendAsync(data);
        }

        private async Task InvokeHandler(JsonClient client, ReactorAction action, TDataBaseType data) {
            if (this._handlerDictionary.ContainsKey(action)) {
                await this._handlerDictionary[action](client, action, data);
            }
        }

        private async Task ClientReadTask(JsonClient client) {
            if (client == null) {
                return;
            }

            try {
                while (true) {
                    var data = await client.RecvObjectAsync<TDataBaseType>();
                    await this.InvokeHandler(client, ReactorAction.ReceivedData, data);
                }
            } catch (IOException) {
                Console.WriteLine("Client disconnected");
            } catch (Exception e) {
                Console.WriteLine("Exception while handling tcp client read: {0}", e);
            } finally {
                await this.ClientDisconnectAsync(client);
            }
        }
        private async Task ClientWriteTask(JsonClient client, ISourceBlock<TDataBaseType> writeQueue) {
            if (client == null) {
                return;
            }

            try {
                while (true) {
                    var data = await writeQueue.ReceiveAsync();
                    await client.SendObjectAsync(data);
                }
            } catch (IOException) {
                Console.WriteLine("Client disconnected");
            } catch (Exception e) {
                Console.WriteLine("Exception while handling tcp client read: {0}", e);
            } finally {
                await this.ClientDisconnectAsync(client);
            }
        }
    }
}
