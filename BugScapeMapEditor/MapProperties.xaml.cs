using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BugScapeMapEditor {
    public partial class MapProperties {
        public MapProperties() {
            this.InitializeComponent();
        }

        private void PropertyChanged(object sender, DataTransferEventArgs e) {
            ((EditingMap)((FrameworkElement)sender).DataContext).Changed = true;
        }
    }
}
