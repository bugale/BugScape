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
        private Map _map;
        private readonly Character _character;

        private static readonly Dictionary<Key, EDirection> KeyDictionary = new Dictionary<Key, EDirection> {
            {Key.Left, EDirection.Left},
            {Key.Right, EDirection.Right},
            {Key.Up, EDirection.Up},
            {Key.Down, EDirection.Down}
        };

        public GamePage(Map map, Character character) {
            this.InitializeComponent();
            this._map = map;
            this._character = character;
        }

        public void SwitchTo() {
            ClientConnection.MessageReceivedEvent += this.HandleServerData;
            MainWindowPager.Window.KeyDown += OnKeyDown;
            this.DrawMap();
        }
        public void SwitchFrom() {
            ClientConnection.MessageReceivedEvent -= this.HandleServerData;
            MainWindowPager.Window.KeyDown -= OnKeyDown;
        }

        private async Task HandleServerData(BugScapeMessage message) {
            if ((message as BugScapeUpdateMapChanged)?.Map.MapID == this._map.MapID) {
                this._map = ((BugScapeUpdateMapChanged)message).Map;
                this.DrawMap();
            }

            // To avoid warning
            await Task.Delay(0);
        }

        private static async void OnKeyDown(object sender, KeyEventArgs args) {
            if (!KeyDictionary.ContainsKey(args.Key)) {
                /* Do nothing */
                return;
            }
            await ClientConnection.Client.SendObjectAsync(new BugScapeRequestMove(KeyDictionary[args.Key]));
        }
        
        private void DrawMap() {
            this.MainCanvas.Children.Clear();

            this.MainCanvas.Width = 50 * this._map.Width;
            this.MainCanvas.Height = 50 * this._map.Height;
            foreach (var mapCharacter in this._map.Characters) {
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
