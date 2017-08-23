using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScapeClient {
    public static class BugScapeCommunicate {
        private static readonly HttpClient Client = new HttpClient();

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
        };

        public static async Task<BugScapeResponse> SendBugScapeRequestAsync(BugScapeRequest request) {
            var post =
            new StringContent(await JsonConvert.SerializeObjectAsync(request, Formatting.Indented, JsonSettings),
                                Encoding.UTF8, "application/json");
            var responseHttp = await Client.PostAsync(ServerSettings.ServerAddress, post);
            var responseJson = await responseHttp.Content.ReadAsStringAsync();
            return await JsonConvert.DeserializeObjectAsync<BugScapeResponse>(responseJson, JsonSettings);
        }
    }
}
