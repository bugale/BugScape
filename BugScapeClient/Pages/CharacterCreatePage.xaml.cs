using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using BugScapeCommon;

namespace BugScapeClient.Pages {
    public partial class CharacterCreatePage : ISwitchable {
        public User User { get; set; }

        public CharacterCreatePage() {
            this.InitializeComponent();
        }

        public CharacterCreatePage(User user) : this() { this.User = user; }

        public void SwitchTo() { ClientConnection.MessageReceivedEvent += this.HandleServerResponse; }
        public void SwitchFrom() { ClientConnection.MessageReceivedEvent -= this.HandleServerResponse; }

        private async void CreateButton_Click(object sender, RoutedEventArgs e) {
            this.IsEnabled = false;

            if (!Regex.Match(this.DisplayNameTextBox.Text, @"^[0-9a-zA-Z_\!\@\#\$\%\^\&\*\-\=\+]{6,32}$").Success) {
                MessageBox.Show("Invalid character name (must be between 6-32 characters)");
                this.IsEnabled = true;
                return;
            }
            if (this.ColorPicker.SelectedColor == null) {
                MessageBox.Show("Please choose a color");
                this.IsEnabled = true;
                return;
            }

            await
            ClientConnection.Client.SendObjectAsync(new BugScapeRequestCharacterCreate {
                Character =
                    new Character {
                        DisplayName = this.DisplayNameTextBox.Text,
                        Color = new RgbColor(this.ColorPicker.SelectedColor.Value.R, this.ColorPicker.SelectedColor.Value.G, this.ColorPicker.SelectedColor.Value.B)
                    }
            });
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            MainWindowPager.SwitchPage(new CharacterSelectPage(this.User));
        }

        private async Task HandleServerResponse(BugScapeMessage message) {
            if (message is BugScapeResponseCharacterCreateSuccessful) {
                var response = (BugScapeResponseCharacterCreateSuccessful)message;
                MessageBox.Show("Character created successfully!");
                this.User = response.User;
                MainWindowPager.SwitchPage(new CharacterSelectPage(this.User));
            } else if (message is BugScapeResponseCharacterCreateAlreadyExist) {
                MessageBox.Show("This character already exists");
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
