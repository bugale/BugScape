using System.Windows.Controls;

namespace BugScapeClient {
    public interface ISwitchable<in T> {
        void SwitchTo(T state);
        void SwitchFrom();
    }

    public static class MainWindowPager {
        public static MainWindow Window { get; set; }

        public static void SwitchPage<T>(Page page, T state) {
            (Window.Content as ISwitchable<T>)?.SwitchFrom();
            Window.Content = page;
            (Window.Content as ISwitchable<T>)?.SwitchTo(state);
        }
    }

    public partial class MainWindow {
        public MainWindow() {
            this.InitializeComponent();
            MainWindowPager.Window = this;
            MainWindowPager.SwitchPage<object>(new LoginPage(), null);
        }
    }
}
