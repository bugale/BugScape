using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using BugScapeCommon;

namespace BugScapeClient.Pages {
    public partial class RegisterPage : ISwitchable {
        public RegisterPage() {
            this.InitializeComponent();
        }

        public RegisterPage(string username) : this() { this.UsernameTextBox.Text = username; }

        public void SwitchTo() { ClientConnection.MessageReceivedEvent += this.HandleServerResponse; }
        public void SwitchFrom() { ClientConnection.MessageReceivedEvent -= this.HandleServerResponse; }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e) {
            this.IsEnabled = false;

            if (!Regex.Match(this.UsernameTextBox.Text, @"^[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]{6,32}$").Success) {
                MessageBox.Show("Invalid username (must be between 6-32 characters)");
                this.IsEnabled = true;
                return;
            }
            if (!Regex.Match(this.PasswordTextBox.Password, @"^[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]{6,32}$").Success) {
                MessageBox.Show("Invalid password (must be between 6-32 characters)");
                this.IsEnabled = true;
                return;
            }

            if (this.PasswordRetypeTextBox.Password != this.PasswordTextBox.Password) {
                MessageBox.Show("Passwords do not match!");
                this.IsEnabled = true;
                return;
            }

            await
            ClientConnection.Client.SendObjectAsync(new BugScapeRequestRegister() {
                User = new User() {Username = this.UsernameTextBox.Text},
                Password = this.PasswordTextBox.Password
            });
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e) {
            MainWindowPager.SwitchPage(new LoginPage(this.UsernameTextBox.Text));
        }

        private async Task HandleServerResponse(BugScapeMessage message) {
            if (message is BugScapeResponseRegisterSuccessful) {
                MessageBox.Show("The registration was successful");
                MainWindowPager.SwitchPage(new LoginPage(this.UsernameTextBox.Text));
            } else if (message is BugScapeResponseRegisterAlreadyExist) {
                MessageBox.Show("This user already exists");
                this.IsEnabled = true;
            } else if (message is BugScapeMessageUnexpectedError) {
                MessageBox.Show(((BugScapeMessageUnexpectedError)message).Message);
                this.IsEnabled = true;
            }

            // To avoid warning
            await Task.Delay(0);
        }
    }
}
