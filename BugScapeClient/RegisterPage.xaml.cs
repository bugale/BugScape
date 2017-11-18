using System.Threading.Tasks;
using System.Windows;
using BugScapeCommon;

namespace BugScapeClient {
    public partial class RegisterPage : ISwitchable {
        public RegisterPage() {
            this.InitializeComponent();
        }

        public RegisterPage(string username) : this() { this.UsernameTextBox.Text = username; }

        public void SwitchTo() { ClientConnection.MessageReceivedEvent += this.HandleServerResponse; }
        public void SwitchFrom() { ClientConnection.MessageReceivedEvent -= this.HandleServerResponse; }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e) {
            this.IsEnabled = false;
            if (this.PasswordRetypeTextBox.Password != this.PasswordTextBox.Password) {
                MessageBox.Show("Passwords do not match!");
                this.IsEnabled = true;
                return;
            }

            await ClientConnection.Client.SendObjectAsync(new BugScapeRequestRegister(this.UsernameTextBox.Text, this.PasswordTextBox.Password));
        }

        private async Task HandleServerResponse(BugScapeMessage message) {
            if (message is BugScapeResponseRegisterSuccessful) {
                MainWindowPager.SwitchPage(new LoginPage(this.UsernameTextBox.Text));
            } else if (message is BugScapeResponseRegisterAlreadyExist) {
                MessageBox.Show("This user already exists");
                this.IsEnabled = true;
            }

            // To avoid warning
            await Task.Delay(0);
        }
    }
}
