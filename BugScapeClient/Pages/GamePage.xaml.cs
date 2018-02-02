using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using BugScapeCommon;

namespace BugScapeClient.Pages {
    public partial class GamePage : ISwitchable {
        private static readonly ImageSource PortalImageSource =
        Imaging.CreateBitmapSourceFromHBitmap(BugScapeCommon.Properties.Resources.Portal.GetHbitmap(), IntPtr.Zero,
                                              Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        private static readonly ImageSource WallImageSource =
        Imaging.CreateBitmapSourceFromHBitmap(BugScapeCommon.Properties.Resources.Wall.GetHbitmap(), IntPtr.Zero,
                                              Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        private Map Map { get; set; }
        private Character Character { get; set; }
        private bool _shouldRedraw;
        private readonly DispatcherTimer _redrawTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(10)};
        private readonly DispatcherTimer _moveTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(50)};
        private EDirection _moveDirection = EDirection.None;

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
            this._moveTimer.Tick += this.MoveIteration;
            this._redrawTimer.Tick += this.RedrawMap;
            this._shouldRedraw = true;
        }

        public void SwitchTo() {
            ClientConnection.MessageReceivedEvent += this.HandleServerData;
            MainWindowPager.Window.KeyDown += this.OnKeyDown;
            MainWindowPager.Window.KeyUp += this.OnKeyUp;
            this._redrawTimer.Start();
        }
        public void SwitchFrom() {
            ClientConnection.MessageReceivedEvent -= this.HandleServerData;
            MainWindowPager.Window.KeyDown -= this.OnKeyDown;
            MainWindowPager.Window.KeyUp -= this.OnKeyUp;
            this._redrawTimer.Stop();
            this._moveTimer.Stop();
        }

        private async Task HandleServerData(BugScapeMessage message) {
            if (message is BugScapeResponseMapChanged) {
                this.Map = ((BugScapeResponseMapChanged)message).Map;
                this._shouldRedraw = true;
            } else if (message is BugScapeResponseCharacterChanged) {
                this.Character = ((BugScapeResponseCharacterChanged)message).Character;
                this.Map = ((BugScapeResponseCharacterChanged)message).Map;
                this._shouldRedraw = true;
            } else if (message is BugScapeMessageUnexpectedError) {
                MessageBox.Show(((BugScapeMessageUnexpectedError)message).Message);
            }

            // To avoid warning
            await Task.Delay(0);
        }

        private async void OnKeyDown(object sender, KeyEventArgs args) {
            // Ignore repetitions
            if (args.IsRepeat) {
                return;
            }

            // Handle regular movements
            if (KeyDictionary.ContainsKey(args.Key)) {
                this._moveDirection = KeyDictionary[args.Key];
                await
                ClientConnection.Client.SendObjectAsync(new BugScapeRequestMove {
                    Direction = KeyDictionary[args.Key],
                    MoveMax = false
                });
                this._moveTimer.Start();
            } else {
                this._moveDirection = EDirection.None;
                this._moveTimer.Stop();
            }

            // Handle other keys
            if (args.Key == Key.Space) {
                // Use portal
                await ClientConnection.Client.SendObjectAsync(new BugscapeRequestUsePortal());
            }
        }
        public void OnKeyUp(object sender, KeyEventArgs args) {
            // Stop regular movement if there was one
            this._moveDirection = EDirection.None;
            this._moveTimer.Stop();
        }

        private async void MoveIteration(object sender, EventArgs args) {
            await
            ClientConnection.Client.SendObjectAsync(new BugScapeRequestMove {
                Direction = this._moveDirection,
                MoveMax = true
            });
        }

        private void AddToCanvas(FrameworkElement element, Point2D location, Point2D size) {
            if (element == null) return;
            if (size != null) {
                element.Width = size.X;
                element.Height = size.Y;
            }
            if (location != null) {
                Canvas.SetLeft(element, location.X);
                Canvas.SetTop(element, location.Y);
            }
            this.MapCanvas.Children.Add(element);
        }
        private void DrawMapWall(MapWall wall) {
            var brush = new ImageBrush {
                ImageSource = WallImageSource,
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(new Point(0, 0), new Point(WallImageSource.Width, WallImageSource.Height))
            };
            var element = new Rectangle {Fill = brush};
            this.AddToCanvas(element, wall.Location, wall.Size);
        }
        private void DrawPortal(Portal portal) {
            var element = new Image {
                Source = PortalImageSource,
                Stretch = Stretch.Fill
            };
            this.AddToCanvas(element, portal.Location, portal.Size);
        }
        private void DrawCharacter(Character character) {
            /* Draw figure */
            var characterFigure = new Ellipse {
                Fill =
                        new SolidColorBrush(Color.FromRgb(character.Color.R, character.Color.G, character.Color.B)),
                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0))
            };
            this.AddToCanvas(characterFigure, character.Location, character.Size);

            /* Draw display name */
            var displayNameLabel = new Label {Content = character.DisplayName, FontSize = 20};
            displayNameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            /* Set it centered horizontally, below the figure */
            var location = character.Location +
                           new Point2D(0.5*(characterFigure.Width - displayNameLabel.DesiredSize.Width),
                                       characterFigure.Height);

            /* Create backshadow */
            var backShadow = new Rectangle {
                Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                Opacity = 0.5,
                RadiusX = 10,
                RadiusY = 10
            };

            this.AddToCanvas(backShadow, location, new Point2D(displayNameLabel.DesiredSize.Width, displayNameLabel.DesiredSize.Height));
            this.AddToCanvas(displayNameLabel, location, null);
        }
        private void RedrawMap(object sender, EventArgs args) {
            if (!this._shouldRedraw) return;

            var map = this.Map;

            this.MapCanvas.Children.Clear();

            /* Set map margins */
            this.MapCanvas.Margin = new Thickness(0, 0, 0, 0);

            /* Set map size */
            this.MapCanvas.Width = map.Size.X;
            this.MapCanvas.Height = map.Size.Y;

            /* Draw walls */
            foreach (var wall in map.MapObstacles.OfType<MapWall>()) {
                this.DrawMapWall(wall);
            }

            /* Draw portals */
            foreach (var portal in map.Portals) {
                this.DrawPortal(portal);
            }

            /* Draw characters */
            foreach (var character in map.Characters) {
                this.DrawCharacter(character);
            }
        }
    }
}
