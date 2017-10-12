using System.Windows;
using BugScapeCommon;

namespace BugScapeClient {
    public partial class RegisterPage {
        public RegisterPage() {
            this.InitializeComponent();
        }

        public RegisterPage(string username) : this() { this.UsernameTextBox.Text = username; }


        public void SwitchTo(string state) { if (state != null) this.UsernameTextBox.Text = state; }
        public void SwitchFrom() { }

        private void RegisterButton_Click(object sender, RoutedEventArgs e) {
            if (this.PasswordRetypeTextBox.Password != this.PasswordTextBox.Password) {
                MessageBox.Show("Passwords do not match!");
                return;
            }

            var request = new BugScapeRequestRegister(this.UsernameTextBox.Text, this.PasswordTextBox.Password);
            var response = BugScapeCommunicate.SendBugScapeRequest(request);

            switch (response.Result) {
            case EBugScapeResult.ErrorUserAlreadyExists:
                MessageBox.Show("Username already exists!");
                break;
            case EBugScapeResult.Success:
                MessageBox.Show("Registered successfully. Please login now.");
                MainWindowPager.SwitchPage(new LoginPage(this.UsernameTextBox.Text));
                break;
            default:
                MessageBox.Show("Error during registration: " + response.ResultExplain);
                break;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e) {
            MainWindowPager.SwitchPage(new LoginPage(this.UsernameTextBox.Text));
        }
    }
}
