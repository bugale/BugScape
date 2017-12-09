using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace BugScapeCommon {
    [ComplexType]
    public class Point2D {
        public Point2D() {
            this.X = 0;
            this.Y = 0;
        }
        public Point2D(double x, double y) {
            this.X = x;
            this.Y = y;
        }
        public Point2D(Point2D point) : this(point.X, point.Y) { }

        public double X { get; set; }
        public double Y { get; set; }

        public bool IsInsideRectangle(Point2D point1, Point2D point2) {
            return this.X <= point2.X && this.X >= point1.X && this.Y <= point2.Y && this.Y >= point1.Y;
        }

        public bool IsInsideRectangle(Point2D point2) { return this.IsInsideRectangle(new Point2D(0, 0), point2); }

        public Point2D CloneToServer() { return new Point2D(this); }
    }

    [ComplexType]
    public class RgbColor {
        public RgbColor() {
            this.R = 0;
            this.G = 0;
            this.B = 0;
        }
        public RgbColor(byte r, byte g, byte b) {
            this.R = r;
            this.G = g;
            this.B = b;
        }
        public RgbColor(RgbColor color) : this(color.R, color.G, color.B) { }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public RgbColor CloneToServer() { return new RgbColor(this); }
    }

    public class Map {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MapID { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public bool IsNewCharacterMap { get; set; }

        public virtual ICollection<Character> Characters { get; set; }
        
        public Map CloneToServer() {
            return new Map {
                MapID = this.MapID,
                Width = this.Width,
                Height = this.Height,
                IsNewCharacterMap = this.IsNewCharacterMap
            };
        }
    }

    public class Character {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CharacterID { get; set; }

        [Index(IsUnique = true), MinLength(6), MaxLength(32), RegularExpression(@"[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]*")]
        public string DisplayName { get; set; }
        
        public RgbColor Color { get; set; }

        public Point2D Location { get; set; }

        public double Speed { get; set; }

        [NotMapped]
        public DateTime LastMoveTime { get; set; }

        [JsonIgnore]
        public virtual Map Map { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }

        public Character CloneToServer() {
            return new Character {
                CharacterID = this.CharacterID,
                DisplayName = this.DisplayName,
                Location = this.Location.CloneToServer(),
                Color = this.Color.CloneToServer(),
                Speed = this.Speed,
                LastMoveTime = DateTime.Now
            };
        }

        public void Move(EDirection direction, bool moveMax) {
            var destination = new Point2D(this.Location);
            var amount = 0.0;
            var max = this.Speed*(DateTime.Now - this.LastMoveTime).TotalSeconds;

            if (moveMax) {
                /* Validate that there was no too much time since the last move */
                if (DateTime.Now - this.LastMoveTime <= TimeSpan.FromMilliseconds(500)) {
                    amount = max;
                }
            } else {
                amount = Math.Min(1, max);
            }

            switch (direction) {
            case EDirection.Down:
                destination.Y += amount;
                break;
            case EDirection.Left:
                destination.X -= amount;
                break;
            case EDirection.Right:
                destination.X += amount;
                break;
            case EDirection.Up:
                destination.Y -= amount;
                break;
            case EDirection.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            this.LastMoveTime = DateTime.Now;

            if (destination.IsInsideRectangle(new Point2D(this.Map.Width, this.Map.Height))) {
                this.Location = destination;
            }
        }
    }

    public class User {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Index(IsUnique = true), MinLength(6), MaxLength(32), RegularExpression(@"[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]*")]
        public string Username { get; set; }

        [JsonIgnore]
        [MaxLength(1024)]
        public byte[] PasswordHash { get; set; }

        [JsonIgnore]
        [MaxLength(1024)]
        public byte[] PasswordSalt { get; set; }

        public User CloneToServer() {
            return new User {
                UserID = this.UserID,
                Username = this.Username,
                Characters = new List<Character>(this.Characters.Select(character => character.CloneToServer()))
            };
        }

        public virtual ICollection<Character> Characters { get; set; }
    }
}
