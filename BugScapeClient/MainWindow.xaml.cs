using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;

namespace BugScapeClient {
    public interface ISwitchable {
        void SwitchTo();
        void SwitchFrom();
    }

    public static class MainWindowPager {
        public static MainWindow Window { get; set; }

        public static void SwitchPage(Page page) {
            (Window.Content as ISwitchable)?.SwitchFrom();
            Window.Content = page;
            (Window.Content as ISwitchable)?.SwitchTo();
        }
    }

    public partial class MainWindow {
        public MainWindow() {
            this.InitializeComponent();
            MainWindowPager.Window = this;
            MainWindowPager.SwitchPage(new GamePage());
        }
    }
}
