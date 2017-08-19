using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BugScapeCommon {
    [ComplexType]
    public class Point2D {
        public Point2D() {
            this.X = 0;
            this.Y = 0;
        }
        public Point2D(int x, int y) {
            this.X = x;
            this.Y = y;
        }
        public Point2D(Point2D point) : this(point.X, point.Y) { }

        public int X { get; set; }
        public int Y { get; set; }

        public bool IsInsideRectangle(Point2D point1, Point2D point2) {
            return this.X <= point2.X && this.X >= point1.X && this.Y <= point2.Y && this.Y >= point1.Y;
        }

        public bool IsInsideRectangle(Point2D point2) { return this.IsInsideRectangle(new Point2D(0, 0), point2); }
    }

    public class Map {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MapID { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public virtual ICollection<Character> Characters { get; set; }
    }

    public class Character {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CharacterID { get; set; }

        public Point2D Location { get; set; }

        [JsonIgnore]
        public virtual Map Map { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }

        public void Move(EDirection direction) {
            var destination = new Point2D(this.Location);
            switch (direction) {
            case EDirection.Down:
                destination.Y += 1;
                break;
            case EDirection.Left:
                destination.X -= 1;
                break;
            case EDirection.Right:
                destination.X += 1;
                break;
            case EDirection.Up:
                destination.Y -= 1;
                break;
            case EDirection.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            if (destination.IsInsideRectangle(new Point2D(this.Map.Width - 1, this.Map.Height - 1))) {
                this.Location = destination;
            }
        }
    }

    public class User {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Index(IsUnique = true)]
        [MaxLength(256)]
        public string Username { get; set; }

        [JsonIgnore]
        public byte[] PasswordHash { get; set; }

        [JsonIgnore]
        public byte[] PasswordSalt { get; set; }

        public virtual ICollection<Character> Characters { get; set; }
    }
}
