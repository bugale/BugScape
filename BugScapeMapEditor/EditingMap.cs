using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BugScapeCommon;

namespace BugScapeMapEditor {
    public class EditingMap : INotifyPropertyChanged {
        private bool _changed;
        private bool _removed;
        private bool _new;

        public EditingMap() {
            this.Map = new Map();
            this.Changed = false;
            this.Removed = false;
            this.New = true;
        }
        public EditingMap(Map map) {
            this.Map = map;
            this.Changed = false;
            this.Removed = false;
            this.New = false;
        }

        public Map Map { get; }

        public bool Changed {
            get { return this._changed; }
            set { this._changed = value; this.NotifyStateChange(); }
        }
        public bool Removed {
            get { return this._removed; }
            set { this._removed = value; this.NotifyStateChange(); }
        }
        public bool New {
            get { return this._new; }
            set { this._new = value; this.NotifyStateChange(); }
        }

        public string EditingID => this.New ? "?" : this.Map.ID.ToString();

        public SolidColorBrush StateColor {
            get {
                if (this.Removed) return new SolidColorBrush(Colors.Red);
                if (this.New) return new SolidColorBrush(Colors.Green);

                return this.Changed ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.White);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyStateChange() {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusColor"));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(EditingID));
        }
    }
}
