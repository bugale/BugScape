using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BugScapeCommon;

namespace BugScapeClient.Pages {
    public partial class CharacterSelectPage : ISwitchable {
        private User _user;
        public User User {
            get { return this._user; }
            set {
                this._user = value;
                this.DataContext = value;
            }
        }

        public CharacterSelectPage() {
            this.InitializeComponent();
        }

        public CharacterSelectPage(User user) : this() { this.User = user; }

        private async void SelectCharacterButton_Click(object sender, RoutedEventArgs e) {
            var character = (Character)(((Button)sender).DataContext);
            this.IsEnabled = false;
            await ClientConnection.Client.SendObjectAsync(new BugScapeRequestCharacterEnter {Character = character});
        }
        private async void RemoveCharacterButton_Click(object sender, RoutedEventArgs e) {
            var character = (Character)(((Button)sender).DataContext);
            this.IsEnabled = false;
            await ClientConnection.Client.SendObjectAsync(new BugScapeRequestCharacterRemove {Character = character});
        }
        private void NewCharacterButton_Click(object sender, RoutedEventArgs e) {
            MainWindowPager.SwitchPage(new CharacterCreatePage(this.User));
        }

        private async Task HandleServerResponse(BugScapeMessage message) {
            if (message is BugScapeRequestCharacterRemoveSuccessful) {
                var response = (BugScapeRequestCharacterRemoveSuccessful)message;
                MessageBox.Show("Character removed successfully");
                this.User = response.User;
                this.IsEnabled = true;
            } else if (message is BugScapeResponseCharacterEnterSuccessful) {
                var response = (BugScapeResponseCharacterEnterSuccessful)message;
                MainWindowPager.SwitchPage(new GamePage(response.Map, response.Character));
            } else if (message is BugScapeMessageUnexpectedError) {
                MessageBox.Show(((BugScapeMessageUnexpectedError)message).Message);
                this.IsEnabled = true;
            }

            await Task.Delay(0); /* To avoid warning */
        }

        public void SwitchTo() { ClientConnection.MessageReceivedEvent += this.HandleServerResponse; }
        public void SwitchFrom() { ClientConnection.MessageReceivedEvent -= this.HandleServerResponse; }
    }
}
