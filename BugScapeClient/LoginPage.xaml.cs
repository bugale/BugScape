using System.Threading.Tasks;
using System.Windows;
using BugScapeCommon;

namespace BugScapeClient {
    public partial class LoginPage : ISwitchable {
        public LoginPage() {
            this.InitializeComponent();
        }

        public LoginPage(string username) : this() { this.UsernameTextBox.Text = username; }

        private async void LoginButton_Click(object sender, RoutedEventArgs e) {
            this.IsEnabled = false;
            await ClientConnection.Client.SendObjectAsync(new BugScapeRequestLogin(this.UsernameTextBox.Text, this.PasswordTextBox.Password));
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e) {
            MainWindowPager.SwitchPage(new RegisterPage(this.UsernameTextBox.Text));
        }

        private async Task HandleServerResponse(BugScapeMessage message) {
            if (message is BugScapeResponseLoginSuccessful) {
                MainWindowPager.SwitchPage(new GamePage(((BugScapeResponseLoginSuccessful)message).Map, ((BugScapeResponseLoginSuccessful)message).Character));
            } else if (message is BugScapeResponseLoginInvalidCredentials) {
                MessageBox.Show("Invalid credentials");
                this.IsEnabled = true;
            } else if (message is BugScapeResponseLoginAlreadyLoggedIn) {
                MessageBox.Show("User is already logged in");
                this.IsEnabled = true;
            }

            // To avoid warning
            await Task.Delay(0);
        }

        public void SwitchTo() { ClientConnection.MessageReceivedEvent += this.HandleServerResponse; }
        public void SwitchFrom() { ClientConnection.MessageReceivedEvent -= this.HandleServerResponse; }
    }
}
