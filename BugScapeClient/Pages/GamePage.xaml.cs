using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BugScapeCommon;

namespace BugScapeClient.Pages {
    public partial class GamePage : ISwitchable {
        private Map Map { get; set; }
        private Character Character { get; set; }

        private static readonly Dictionary<Key, EDirection> KeyDictionary = new Dictionary<Key, EDirection> {
            {Key.Left, EDirection.Left},
            {Key.Right, EDirection.Right},
            {Key.Up, EDirection.Up},
            {Key.Down, EDirection.Down}
        };

        public GamePage(Map map, Character character) {
            this.InitializeComponent();
            this.Character = character;
            this.Map = map;
            this.Title = this.Character.DisplayName;
            this.RedrawMap();
        }

        public void SwitchTo() {
            ClientConnection.MessageReceivedEvent += this.HandleServerData;
            MainWindowPager.Window.KeyDown += OnKeyDown;
            this.RedrawMap();
        }
        public void SwitchFrom() {
            ClientConnection.MessageReceivedEvent -= this.HandleServerData;
            MainWindowPager.Window.KeyDown -= OnKeyDown;
        }

        private async Task HandleServerData(BugScapeMessage message) {
            if (message is BugScapeResponseMapChanged) {
                this.Map = ((BugScapeResponseMapChanged)message).Map;
                this.RedrawMap();
            } else if (message is BugScapeMessageUnexpectedError) {
                MessageBox.Show(((BugScapeMessageUnexpectedError)message).Message);
            }

            // To avoid warning
            await Task.Delay(0);
        }

        private static async void OnKeyDown(object sender, KeyEventArgs args) {
            if (!KeyDictionary.ContainsKey(args.Key)) {
                /* Do nothing */
                return;
            }
            await ClientConnection.Client.SendObjectAsync(new BugScapeRequestMove() {Direction = KeyDictionary[args.Key]});
        }
        
        private void RedrawMap() {
            this.MapCanvas.Children.Clear();

            /* Set map margins */
            this.MapCanvas.Margin = new Thickness(ClientSettings.GuiTileX, ClientSettings.GuiTileY, ClientSettings.GuiTileX, ClientSettings.GuiTileY);

            /* Set map size */
            this.MapCanvas.Width = this.Map.Width * ClientSettings.GuiTileX;
            this.MapCanvas.Height = this.Map.Height * ClientSettings.GuiTileY;

            /* Draw characters */
            foreach (var character in this.Map.Characters) {
                /* Draw figure */
                var characterFigure = new Ellipse {
                    Fill = new SolidColorBrush(Color.FromRgb(character.Color.R, character.Color.G, character.Color.B)),
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    Width = ClientSettings.GuiTileX,
                    Height = ClientSettings.GuiTileY
                };
                Canvas.SetLeft(characterFigure, character.Location.X*ClientSettings.GuiTileX);
                Canvas.SetTop(characterFigure, character.Location.Y*ClientSettings.GuiTileY);
                this.MapCanvas.Children.Add(characterFigure);

                /* Draw display name */
                var displayNameLabel = new Label {
                    Content = character.DisplayName,
                    FontSize = 20
                };
                displayNameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                Canvas.SetLeft(displayNameLabel,
                               character.Location.X*ClientSettings.GuiTileX - 0.5*displayNameLabel.DesiredSize.Width +
                               0.5*characterFigure.Width); /* Set it centered horizontally */
                Canvas.SetTop(displayNameLabel, (character.Location.Y + 1) * ClientSettings.GuiTileY); /* Set it below the figure */
                this.MapCanvas.Children.Add(displayNameLabel);
            }
        }
    }
}
