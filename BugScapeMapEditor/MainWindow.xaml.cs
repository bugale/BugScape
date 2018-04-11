using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BugScapeCommon;

namespace BugScapeMapEditor {
    public partial class MainWindow {
        private readonly List<EditingMap> _editedMaps = new List<EditingMap>();

        public MainWindow() { this.InitializeComponent(); }

        private void StartLoading() {
            this.IsEnabled = false;
            this.LoadingLabel.Visibility = Visibility.Visible;
        }
        private void FinishLoading() {
            this.IsEnabled = true;
            this.LoadingLabel.Visibility = Visibility.Hidden;
        }

        private async Task ReloadAllMaps() {
            this.StartLoading();
            try {
                /* Load all maps */
                await Task.Run(() => {
                                   using (var dbContext = new BugScapeDbContext()) {
                                       foreach (var m in dbContext.GetMapStructureDict().Values) {
                                           this._editedMaps.Add(new EditingMap(m));
                                       }
                                   }
                               });
            } finally {
                this.FinishLoading();
            }
        }

        private void DeleteMaps(object sender, RoutedEventArgs e) {
        }
        private void AddNewMap(object sender, RoutedEventArgs e) {
        }
        private async void SaveAllChanges(object sender, RoutedEventArgs e) {
            this.StartLoading();
            try {
                await Task.Run(() => {
                    using (var dbContext = new BugScapeDbContext()) {
                        foreach (var map in this._editedMaps) {

                        }
                    }
                });
            } finally {
                this.FinishLoading();
            }
        }
        private async void DiscardAllChanges(object sender, RoutedEventArgs e) { await this.ReloadAllMaps(); }
        private void SearchTextChanged(object sender, RoutedEventArgs e) {
            var strFilter = ((TextBox)sender).Text;

            var cv = CollectionViewSource.GetDefaultView(this.MapList.ItemsSource);
            if (string.IsNullOrEmpty(strFilter)) {
                cv.Filter = o => true;
            } else {
                cv.Filter =
                o => ((EditingMap)o).Map.Name.ToUpper().Contains(strFilter.ToUpper()) || ((EditingMap)o).Map.ID.ToString().Contains(strFilter);
            }
        }
        private void MapList_MapDoubleClick(object sender, MouseButtonEventArgs e) {
            var r = (DataGridRow)sender;
            var m = (EditingMap)r.DataContext;

            this.PropertiesFrame.Content = new MapProperties {DataContext = m};
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e) { this.MapList.ItemsSource = this._editedMaps; await this.ReloadAllMaps(); }
    }
}
