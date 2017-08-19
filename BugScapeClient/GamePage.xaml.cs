using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
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

        private static async Task<string> SendBugScapeRequestAsync(BugScapeRequest request) {
            var post = new StringContent(await JsonConvert.SerializeObjectAsync(request), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(ServerSettings.ServerAddress, post);
            return await response.Content.ReadAsStringAsync();
        }

        private static async void OnKeyDown(object sender, KeyEventArgs args) {
            if (!KeyDictionary.ContainsKey(args.Key)) {
                /* Do nothing */
                return;
            }

            var response = await SendBugScapeRequestAsync(new BugScapeMoveRequest(2, KeyDictionary[args.Key]));
            dynamic resJson = await JsonConvert.DeserializeObjectAsync(response);
            if (resJson.Result != EBugScapeResult.Success) {
                Debug.WriteLine("Failed moving");
            }
        }

        private async void OnRefreshElapsed(object sender, EventArgs e) {
            await this.Dispatcher.InvokeAsync(this.RefreshGameAsync);
        }

        private async Task<int> RefreshGameAsync() {
            try {
                var response = await SendBugScapeRequestAsync(new BugScapeGetMapStateRequest(2));
                dynamic resJson = await JsonConvert.DeserializeObjectAsync(response);

                this.MainCanvas.Children.Clear();

                this.MainCanvas.Width = 50 * (int)resJson.Map.Width;
                this.MainCanvas.Height = 50 * (int)resJson.Map.Height;
                foreach (var mapCharacter in resJson.Map.Characters) {
                    var border = new Border {BorderBrush = Brushes.Transparent, Height = 50, Width = 50};
                    var textBlock = new TextBlock {
                        Text = mapCharacter.CharacterID,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                        FontSize = 40,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = textBlock;
                    Canvas.SetTop(border, 50 * (int)mapCharacter.Location.Y);
                    Canvas.SetLeft(border, 50 * (int)mapCharacter.Location.X);
                    this.MainCanvas.Children.Add(border);
                }

                return resJson.Result;
            } catch (Exception e) {
                Debug.WriteLine("Exception in RefreshGameAsync: {0}", new object[] { e.ToString() });
                return 0;
            }
        }
    }
}
