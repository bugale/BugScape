﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BugScapeCommon;

namespace BugScapeClient.Pages {
    public partial class GamePage : ISwitchable {
        private Map Map { get; set; }
        private Character Character { get; }
        private bool _shouldRedraw;
        private readonly DispatcherTimer _redrawTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(10)};
        private readonly DispatcherTimer _moveTimer = new DispatcherTimer  {Interval = TimeSpan.FromMilliseconds(50)};
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
            } else if (message is BugScapeMessageUnexpectedError) {
                MessageBox.Show(((BugScapeMessageUnexpectedError)message).Message);
            }

            // To avoid warning
            await Task.Delay(0);
        }

        private async void OnKeyDown(object sender, KeyEventArgs args) {
            if (args.IsRepeat) {
                return;
            }
            if (!KeyDictionary.ContainsKey(args.Key)) {
                this._moveDirection = EDirection.None;
                this._moveTimer.Stop();
            } else {
                this._moveDirection = KeyDictionary[args.Key];
                await
                ClientConnection.Client.SendObjectAsync(new BugScapeRequestMove {
                    Direction = KeyDictionary[args.Key],
                    MoveMax = false
                });
                this._moveTimer.Start();
            }
        }
        public void OnKeyUp(object sender, KeyEventArgs args) {
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
        private void RedrawMap(object sender, EventArgs args) {
            if (!this._shouldRedraw) return;

            var map = this.Map;

            this.MapCanvas.Children.Clear();

            /* Set map margins */
            this.MapCanvas.Margin = new Thickness(0, 0, 0, 0);

            /* Set map size */
            this.MapCanvas.Width = map.Width;
            this.MapCanvas.Height = map.Height;

            /* Draw characters */
            foreach (var character in map.Characters) {
                /* Draw figure */
                var characterFigure = new Ellipse {
                    Fill =
                        new SolidColorBrush(Color.FromRgb(character.Color.R, character.Color.G, character.Color.B)),
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    Width = ClientSettings.CharacterSizeX,
                    Height = ClientSettings.CharacterSizeY
                };
                Canvas.SetLeft(characterFigure, character.Location.X);
                Canvas.SetTop(characterFigure, character.Location.Y);
                this.MapCanvas.Children.Add(characterFigure);

                /* Draw display name */
                var displayNameLabel = new Label {Content = character.DisplayName, FontSize = 20};
                displayNameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                Canvas.SetLeft(displayNameLabel,
                               character.Location.X - 0.5*displayNameLabel.DesiredSize.Width +
                               0.5*characterFigure.Width); /* Set it centered horizontally */
                Canvas.SetTop(displayNameLabel, character.Location.Y + characterFigure.Height);
                /* Set it below the figure */
                this.MapCanvas.Children.Add(displayNameLabel);
            }
        }
    }
}
