using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScape {

    public class BugScapeServer {
        private readonly CancellationToken _cancel = new CancellationToken();

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
            dynamic reqJson = await JsonConvert.DeserializeObjectAsync(await streamReader.ReadToEndAsync());
            BugScapeResponse response;

            switch ((EBugScapeOperation)reqJson.Operation) {
            case EBugScapeOperation.GetMapState:
                Console.WriteLine("Getting map state");
                response = await HandleMapStateRequestAsync(reqJson, dbContext);
                break;
            case EBugScapeOperation.Move:
                Console.WriteLine("Moving character");
                response = await HandleMoveRequestAsync(reqJson, dbContext);
                break;
            default:
                Console.WriteLine("Invalid operation {0}", (EBugScapeOperation)reqJson.Operatrion);
                return;
            }

            if (response == null) {
                response = new BugScapeResponse(EBugScapeResult.Error);
            }

            context.Response.ContentEncoding = Encoding.UTF8;
            var resStream = new StreamWriter(context.Response.OutputStream, context.Response.ContentEncoding);
            await resStream.WriteAsync(await JsonConvert.SerializeObjectAsync(response));
            await resStream.FlushAsync();
            context.Response.Close();
            await dbContext.SaveChangesAsync();
        }
        private static async Task<BugScapeResponse> HandleMapStateRequestAsync(dynamic request, BugScapeDbContext dbContext) {
            var character = await dbContext.Characters.FindAsync((int)request.CharacterID);
            if (character != null) return new BugScapeMapResponse(character.Map);

            Console.WriteLine("No characeter found");
            return null;
        }
        private static async Task<BugScapeResponse> HandleMoveRequestAsync(dynamic request, BugScapeDbContext dbContext) {
            var character = await dbContext.Characters.FindAsync((int)request.CharacterID);
            if (character == null) {
                Console.WriteLine("No characeter found");
                return null;
            }

            character.Move((EDirection)request.Direction);
            return new BugScapeResponse(EBugScapeResult.Success);
        }
    }
}
