using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BugScapeCommon;

namespace BugScapeClient {
    public partial class GamePage : ISwitchable {
        private readonly Timer _refreshTimer = new Timer(5000);
        private readonly Character _character;

        private static readonly Dictionary<Key, EDirection> KeyDictionary = new Dictionary<Key, EDirection> {
            {Key.Left, EDirection.Left},
            {Key.Right, EDirection.Right},
            {Key.Up, EDirection.Up},
            {Key.Down, EDirection.Down}
        };
    /* TODO: Try with Release */

        public GamePage(Character character) {
            this.InitializeComponent();
            this._character = character;
        }

        public void SwitchTo() {
            MainWindowPager.Window.KeyDown += this.OnKeyDown;
            this._refreshTimer.Elapsed += this.OnRefreshElapsed;
            this._refreshTimer.Start();
        }
        public void SwitchFrom() {
            MainWindowPager.Window.KeyDown -= this.OnKeyDown;
            this._refreshTimer.Elapsed -= this.OnRefreshElapsed;
            this._refreshTimer.Stop();
        }

        private void OnKeyDown(object sender, KeyEventArgs args) {
            if (!KeyDictionary.ContainsKey(args.Key)) {
                /* Do nothing */
                return;
            }

            var response =
            BugScapeCommunicate.SendBugScapeRequest(new BugScapeRequestMove(this._character.CharacterID,
                                                                            KeyDictionary[args.Key]));
            if (response.Result != EBugScapeResult.Success) {
                Debug.WriteLine("Failed moving");
            }
        }

        private void OnRefreshElapsed(object sender, EventArgs e) {
            try {
                this.RefreshGameAsync().Wait();
            } catch (Exception ex) {
                Debug.WriteLine("Exception in OnRefreshElapsed: {0}", new object[] {ex.ToString()});
            }
        }

        private async Task<EBugScapeResult> RefreshGameAsync() {
            try {
                var response =
                (BugScapeCommunicate.SendBugScapeRequest(new BugScapeRequestMapState(this._character.CharacterID))) as
                BugScapeResponseMapState;

                if (response == null) {
                    Debug.WriteLine("Invalid response");
                    return EBugScapeResult.Error;
                }

                await this.Dispatcher.InvokeAsync(() => this.DrawMap(response.Map));

                return response.Result;
            } catch (Exception ex) {
                Debug.WriteLine("Exception in RefreshGameAsync: {0}", new object[] { ex.ToString() });
                return EBugScapeResult.Error;
            }
        }

        private void DrawMap(Map m) {
            this.MainCanvas.Children.Clear();

            this.MainCanvas.Width = 50 * m.Width;
            this.MainCanvas.Height = 50 * m.Height;
            foreach (var mapCharacter in m.Characters) {
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
        }
    }
}
