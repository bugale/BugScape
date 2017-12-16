using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
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

        public static Point2D operator +(Point2D a, Point2D b) {
            return new Point2D(a.X + b.X, a.Y + b.Y);
        }
        public static Point2D operator -(Point2D a, Point2D b) {
            return new Point2D(a.X - b.X, a.Y - b.Y);
        }

        public Point2D CloneFromDatabase() { return new Point2D(this); }
    }

    [ComplexType]
    public class Rect2D {
        public Rect2D() {
            this.X1 = 0;
            this.X2 = 0;
            this.Y1 = 0;
            this.Y2 = 0;
        }
        public Rect2D(Point2D a, Point2D d) {
            this.X1 = a.X;
            this.X2 = d.X;
            this.Y1 = a.Y;
            this.Y2 = d.Y;
        }
        public Rect2D(double x1, double x2, double y1, double y2) {
            this.X1 = x1;
            this.X2 = x2;
            this.Y1 = y1;
            this.Y2 = y2;
        }
        public Rect2D(Rect2D point) : this(point.X1, point.X2, point.Y1, point.Y2) { }

        public double X1 { get; set; }
        public double X2 { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }

        public double XMin => Math.Min(this.X1, this.X2);
        public double XMax => Math.Max(this.X1, this.X2);
        public double YMin => Math.Min(this.Y1, this.Y2);
        public double YMax => Math.Max(this.Y1, this.Y2);

        public Point2D A => new Point2D(this.XMin, this.YMin);
        public Point2D B => new Point2D(this.XMax, this.YMin);
        public Point2D C => new Point2D(this.XMin, this.YMax);
        public Point2D D => new Point2D(this.XMax, this.YMax);

        public bool IsCollidingWith(Rect2D r) {
            return this.YMin <= r.YMax && this.YMax >= r.YMin && this.XMin <= r.XMax && this.XMax >= r.XMin;
        }

        public static Rect2D operator +(Rect2D a, Point2D b) {
            return new Rect2D(a.X1 + b.X, a.X2 + b.X, a.Y1 + b.Y, a.Y2 + b.Y);
        }
        public static Rect2D operator -(Rect2D a, Point2D b) {
            return new Rect2D(a.X1 - b.X, a.X2 - b.X, a.Y1 - b.Y, a.Y2 - b.Y);
        }

        public Rect2D CloneFromDatabase() { return new Rect2D(this); }
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

        public RgbColor CloneFromDatabase() { return new RgbColor(this); }
    }

    [ComplexType]
    public class HashedPassword {
        private const int Iterations = 1024;
        private const int HashLength = 128;
        private const int SaltLength = 128;

        public HashedPassword() {
            this.Hash = new byte[] {};
            this.Salt = new byte[] {};
        }
        public HashedPassword(byte[] hash, byte[] salt) {
            this.Hash = (byte[])hash.Clone();
            this.Salt = (byte[])salt.Clone();
        }
        public HashedPassword(string password) {
            this.Salt = new byte[SaltLength];
            new RNGCryptoServiceProvider().GetBytes(this.Salt);
            this.Hash = HashPasswordCalculate(password, this.Salt);
        }
        public HashedPassword(HashedPassword hashedPassword) : this(hashedPassword.Hash, hashedPassword.Salt) { }

        [JsonIgnore]
        [MaxLength(1024)]
        public byte[] Hash { get; set; }

        [JsonIgnore]
        [MaxLength(1024)]
        public byte[] Salt { get; set; }

        public HashedPassword CloneFromDatabase() { return new HashedPassword(this); }

        private static byte[] HashPasswordCalculate(string password, byte[] passwordSalt) {
            var hash = new Rfc2898DeriveBytes(password, passwordSalt) {
                IterationCount = Iterations
            };
            return hash.GetBytes(HashLength);
        }
        
        public bool Compare(string password) {
            var hashedTry = HashPasswordCalculate(password, this.Salt);

            // Check every byte to eliminate comparing time attacks
            var diff = this.Hash.Length ^ hashedTry.Length;
            for (var i = 0; i < this.Hash.Length && i < hashedTry.Length; i++) diff |= this.Hash[i] ^ hashedTry[i];
            return diff == 0;
        }
    }

    public abstract class DatabaseObject {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        protected virtual DatabaseObject CopyFromDatabase(DatabaseObject o) {
            this.ID = o.ID;
            return this;
        }

        public abstract DatabaseObject CloneFromDatabase();
    }

    public abstract class MapObject : DatabaseObject {
        public Point2D Size { get; set; }
        public Point2D Location { get; set; }
        
        public virtual bool IsBlocking { get; set; }

        [JsonIgnore]
        public virtual Map Map { get; set; }

        [NotMapped]
        [JsonIgnore]
        public Rect2D Rect => new Rect2D(this.Location, this.Location + this.Size);

        protected override DatabaseObject CopyFromDatabase(DatabaseObject o) {
            this.Location = ((MapObject)o).Location.CloneFromDatabase();
            this.Size = ((MapObject)o).Size.CloneFromDatabase();
            this.IsBlocking = ((MapObject)o).IsBlocking;
            return base.CopyFromDatabase(o);
        }
    }

    public class MapWall : MapObject {
        public RgbColor Color { get; set; }

        protected override DatabaseObject CopyFromDatabase(DatabaseObject o) {
            this.Color = ((MapWall)o).Color.CloneFromDatabase();
            return base.CopyFromDatabase(o);
        }

        public override DatabaseObject CloneFromDatabase() { return new MapWall().CopyFromDatabase(this); }
    }

    public class Map : DatabaseObject {
        public Point2D Size { get; set; }

        public bool IsNewCharacterMap { get; set; }

        public virtual ICollection<Character> Characters { get; set; }

        public virtual ICollection<MapObject> MapObjects { get; set; }

        [NotMapped] [JsonIgnore] public Rect2D Rect => new Rect2D(new Point2D(), this.Size);
        [NotMapped] [JsonIgnore] public Rect2D RightEdge => new Rect2D(this.Rect.B, this.Rect.D);
        [NotMapped] [JsonIgnore] public Rect2D LeftEdge => new Rect2D(this.Rect.A, this.Rect.C);
        [NotMapped] [JsonIgnore] public Rect2D UpEdge => new Rect2D(this.Rect.A, this.Rect.B);
        [NotMapped] [JsonIgnore] public Rect2D DownEdge => new Rect2D(this.Rect.C, this.Rect.D);
        [NotMapped] [JsonIgnore] public List<Rect2D> AllEdges
            => new List<Rect2D> {this.UpEdge, this.DownEdge, this.RightEdge, this.LeftEdge};

        protected override DatabaseObject CopyFromDatabase(DatabaseObject o) {
            this.Size = ((Map)o).Size.CloneFromDatabase();
            this.IsNewCharacterMap = ((Map)o).IsNewCharacterMap;
            this.MapObjects = new List<MapObject>(((Map)o).MapObjects.Select(x => (MapObject)x.CloneFromDatabase()));
            foreach (var x in this.MapObjects) x.Map = this;
            return base.CopyFromDatabase(o);
        }

        public override DatabaseObject CloneFromDatabase() { return new Map().CopyFromDatabase(this); }
    }

    public class Character : DatabaseObject {
        [Index(IsUnique = true), MinLength(6), MaxLength(32), RegularExpression(@"[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]*")]
        public string DisplayName { get; set; }
        
        public RgbColor Color { get; set; }

        public Point2D Size { get; set; }
        public Point2D Location { get; set; }

        public double Speed { get; set; }

        [JsonIgnore]
        public virtual Map Map { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }

        [NotMapped]
        [JsonIgnore]
        public Rect2D Rect => new Rect2D(this.Location, this.Location + this.Size);

        [NotMapped]
        public DateTime LastMoveTime { get; set; }

        protected override DatabaseObject CopyFromDatabase(DatabaseObject o) {
            this.DisplayName = ((Character)o).DisplayName;
            this.Location = ((Character)o).Location.CloneFromDatabase();
            this.Color = ((Character)o).Color.CloneFromDatabase();
            this.Speed = ((Character)o).Speed;
            this.Size = ((Character)o).Size.CloneFromDatabase();
            this.LastMoveTime = DateTime.Now;
            return base.CopyFromDatabase(o);
        }

        public override DatabaseObject CloneFromDatabase() { return new Character().CopyFromDatabase(this); }
        
        public void Move(EDirection direction, bool moveMax) {
            var amount = 0.0;
            var max = this.Speed*(DateTime.Now - this.LastMoveTime).TotalSeconds;

            if (direction == EDirection.None) return;

            if (moveMax) {
                /* Validate that there was no too much time since the last move */
                if (DateTime.Now - this.LastMoveTime <= TimeSpan.FromMilliseconds(500)) {
                    amount = max;
                }
            } else {
                amount = Math.Min(1, max);
            }
            this.LastMoveTime = DateTime.Now;

            /* The collision detection here is using a rectangle of the movement.
             * The rectangle represents the whole area that the character will be in at some point during the movement.
             * In this way, if there is a collision between this rect and an obstacle, the move cannot be completed.
             * To get the character to the point right before the collision, we define an action that describes how
             * to calculate a smaller movement rectangle based on the obstacle.
             * This calculation is different for each movement direction.
             * Then we run on all of the obstacles and this way select the largest movement rectangle that has no collisions.
             * After we done, we have a movement rectangle, from which we need to infer the location of the character
             * at the end. This is also different for each movoment direction. */

            // The size of moveRect as a vecftor
            var moveRectSize = new Point2D();

            // A method that describes how to update the moveRect in case of a collision
            Action<Rect2D, Rect2D> moveRectUpdate = (m, x) => { };

            // A method that describes how to update the location of the character according to the final moveRect
            Action<Point2D, Rect2D> locationUpdate = (l, m) => { };

            switch (direction) {
            case EDirection.Right:
                moveRectSize = this.Size + new Point2D(amount, 0);
                moveRectUpdate = (m, x) => m.X2 = x.X1 - 1;
                locationUpdate = (l, m) => l.X = m.X2 - this.Size.X;
                break;
            case EDirection.Left:
                moveRectSize = new Point2D(-amount, this.Size.Y);
                moveRectUpdate = (m, x) => m.X2 = x.X2 + 1;
                locationUpdate = (l, m) => l.X = m.X2;
                break;
            case EDirection.Down:
                moveRectSize = this.Size + new Point2D(0, amount);
                moveRectUpdate = (m, x) => m.Y2 = x.Y1 - 1;
                locationUpdate = (l, m) => l.Y = m.Y2 - this.Size.Y;
                break;
            case EDirection.Up:
                moveRectSize = new Point2D(this.Size.X, -amount);
                moveRectUpdate = (m, x) => m.Y2 = x.Y2 + 1;
                locationUpdate = (l, m) => l.Y = m.Y2;
                break;
            }

            // Get all rects that might collide with us
            var collidableRects = this.Map.AllEdges;
            collidableRects.AddRange(this.Map.MapObjects.Where(x => x.IsBlocking).Select(x => x.Rect));

            // Check for collisions and update moveRect
            var moveRect = new Rect2D(this.Location, this.Location + moveRectSize);
            foreach (var x in collidableRects.Where(x => moveRect.IsCollidingWith(x))) {
                moveRectUpdate(moveRect, x);
            }

            // Set the furthest location for the character
            locationUpdate(this.Location, moveRect);
        }
    }

    public class User : DatabaseObject {
        [Index(IsUnique = true), MinLength(6), MaxLength(32), RegularExpression(@"[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]*")]
        public string Username { get; set; }

        public HashedPassword Password { get; set; }

        public virtual ICollection<Character> Characters { get; set; }
        
        protected override DatabaseObject CopyFromDatabase(DatabaseObject o) {
            this.Username = ((User)o).Username;
            this.Characters = new List<Character>(((User)o).Characters.Select(x => (Character)x.CloneFromDatabase()));
            foreach (var x in this.Characters) x.User = this;
            return base.CopyFromDatabase(o);
        }

        public override DatabaseObject CloneFromDatabase() { return new User().CopyFromDatabase(this); }
    }
}
