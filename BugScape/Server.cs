using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScape {
    public class BugScapeServer {
        private readonly CancellationToken _cancel = new CancellationToken();

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            Binder = new EntityFrameworkSerializationBinder()
        };

        public async Task Run() {
            var listener = new HttpListener();
            listener.Prefixes.Add(ServerSettings.ServerAddress);
            listener.Start();
            
            while (!this._cancel.IsCancellationRequested) {
                await HandleRequestAsync(listener.GetContext());
            }
        }

        private static async Task HandleRequestAsync(object state) {
            var dbContext = new BugScapeDbContext();
            var context = (HttpListenerContext)state;

            var streamReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var request = await JsonConvert.DeserializeObjectAsync<BugScapeRequest>(await streamReader.ReadToEndAsync(), JsonSettings);

            BugScapeResponse response;
            switch (request.Operation) {
            case EBugScapeOperation.GetMapState:
                response = await HandleMapStateRequestAsync(request, dbContext);
                break;
            case EBugScapeOperation.Move:
                response = await HandleMoveRequestAsync(request as BugScapeMoveRequest, dbContext);
                break;
            default:
                Console.WriteLine("Invalid operation {0}", request.Operation);
                return;
            }

            if (response == null) {
                response = new BugScapeResponse(EBugScapeResult.Error);
            }

            var saveTask = dbContext.SaveChangesAsync();

            context.Response.ContentEncoding = context.Request.ContentEncoding;
            var resStream = new StreamWriter(context.Response.OutputStream, context.Response.ContentEncoding);
            await resStream.WriteAsync(await JsonConvert.SerializeObjectAsync(response, Formatting.Indented, JsonSettings));
            await resStream.FlushAsync();
            context.Response.Close();

            await saveTask;
        }

        private static async Task<BugScapeMapResponse> HandleMapStateRequestAsync(BugScapeRequest request, BugScapeDbContext dbContext) {
            if (request == null) return null;
            var character = await dbContext.Characters.FindAsync(request.CharacterID);
            return new BugScapeMapResponse(character?.Map);
        }

        private static async Task<BugScapeResponse> HandleMoveRequestAsync(BugScapeMoveRequest request, BugScapeDbContext dbContext) {
            if (request == null) return null;
            var character = await dbContext.Characters.FindAsync(request.CharacterID);
            character?.Move(request.Direction);
            return new BugScapeResponse(EBugScapeResult.Success);
        }
    }
}
