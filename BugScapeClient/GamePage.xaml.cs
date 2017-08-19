using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScapeClient {
    public partial class GamePage : ISwitchable {
        private static readonly HttpClient Client = new HttpClient();
        private readonly Timer _refreshTimer = new Timer(50);

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
        };

        private static readonly Dictionary<Key, EDirection> KeyDictionary = new Dictionary<Key, EDirection> {
            {Key.Left, EDirection.Left},
            {Key.Right, EDirection.Right},
            {Key.Up, EDirection.Up},
            {Key.Down, EDirection.Down}
        };

        public GamePage() {
            this.InitializeComponent();
            this._refreshTimer.Start();
        }

        public void SwitchTo() {
            MainWindowPager.Window.KeyDown += OnKeyDown;
            this._refreshTimer.Elapsed += this.OnRefreshElapsed;
        }

        public void SwitchFrom() {
            MainWindowPager.Window.KeyDown -= OnKeyDown;
            this._refreshTimer.Elapsed -= this.OnRefreshElapsed;
        }

        private static async Task<BugScapeResponse> SendBugScapeRequestAsync(BugScapeRequest request) {
            var post =
            new StringContent(await JsonConvert.SerializeObjectAsync(request, Formatting.Indented, JsonSettings),
                              Encoding.UTF8, "application/json");
            var responseHttp = await Client.PostAsync(ServerSettings.ServerAddress, post);
            var responseJson = await responseHttp.Content.ReadAsStringAsync();
            return await JsonConvert.DeserializeObjectAsync<BugScapeResponse>(responseJson, JsonSettings);
        }

        private static async void OnKeyDown(object sender, KeyEventArgs args) {
            if (!KeyDictionary.ContainsKey(args.Key)) {
                /* Do nothing */
                return;
            }

            var response = await SendBugScapeRequestAsync(new BugScapeMoveRequest(2, KeyDictionary[args.Key]));
            if (response.Result != EBugScapeResult.Success) {
                Debug.WriteLine("Failed moving");
            }
        }

        private async void OnRefreshElapsed(object sender, EventArgs e) {
            try {
                await this.Dispatcher.InvokeAsync(this.RefreshGameAsync);
            } catch (Exception ex) {
                Debug.WriteLine("Exception in OnRefreshElapsed: {0}", new object[] { ex.ToString() });
            }
        }

        private async Task<EBugScapeResult> RefreshGameAsync() {
            try {
                var response = (await SendBugScapeRequestAsync(new BugScapeGetMapStateRequest(2))) as BugScapeMapResponse;

                if (response == null) {
                    Debug.WriteLine("Invalid response");
                    return EBugScapeResult.Error;
                }

                this.MainCanvas.Children.Clear();

                this.MainCanvas.Width = 50 * response.Map.Width;
                this.MainCanvas.Height = 50 * response.Map.Height;
                foreach (var mapCharacter in response.Map.Characters) {
                    var border = new Border {BorderBrush = Brushes.Transparent, Height = 50, Width = 50};
                    var textBlock = new TextBlock {
                        Text = mapCharacter.CharacterID.ToString(),
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                        FontSize = 40,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = textBlock;
                    Canvas.SetTop(border, 50 * mapCharacter.Location.Y);
                    Canvas.SetLeft(border, 50 * mapCharacter.Location.X);
                    this.MainCanvas.Children.Add(border);
                }

                return response.Result;
            } catch (Exception ex) {
                Debug.WriteLine("Exception in RefreshGameAsync: {0}", new object[] { ex.ToString() });
                return EBugScapeResult.Error;
            }
        }
    }
}
