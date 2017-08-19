using System.Windows;

namespace BugScapeClient {
    public partial class LoginPage : ISwitchable<object> {
        public LoginPage() {
            this.InitializeComponent();
        }

        public void SwitchTo(object state) { }
        public void SwitchFrom() { }

        private void LoginButton_Click(object sender, RoutedEventArgs e) {
            int characterId;
            if (!int.TryParse(this.CharacterIdTextBox.Text, out characterId)) {
                MessageBox.Show("Invalid CharacterID!");
            } else {
                MainWindowPager.SwitchPage(new GamePage(), characterId);
            }
        }
    }
}
