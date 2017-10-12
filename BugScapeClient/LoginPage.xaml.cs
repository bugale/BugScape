using System.Linq;
using System.Windows;
using BugScapeCommon;

namespace BugScapeClient {
    public partial class LoginPage {
        public LoginPage() {
            this.InitializeComponent();
        }

        public LoginPage(string username) : this() { this.UsernameTextBox.Text = username; }

        private async void LoginButton_Click(object sender, RoutedEventArgs e) {
            var request = new BugScapeRequestLogin(this.UsernameTextBox.Text, this.PasswordTextBox.Password);
            var response = BugScapeCommunicate.SendBugScapeRequest(request);

            switch (response.Result) {
            case EBugScapeResult.ErrorInvalidCredentials:
                MessageBox.Show("Invalid credentials!");
                break;
            case EBugScapeResult.Success:
                var responseUser = response as BugScapeResponseUser;
                if (responseUser == null) MessageBox.Show("Unknown error occourred.");
                else MainWindowPager.SwitchPage(new GamePage(responseUser.User.Characters.First()));
                break;
            default:
                MessageBox.Show("Error during registration: " + response.ResultExplain);
                break;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e) {
            MainWindowPager.SwitchPage(new RegisterPage(this.UsernameTextBox.Text));
        }
    }
}
