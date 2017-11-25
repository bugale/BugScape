using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BugScapeCommon;

namespace BugScapeClient {

    public interface ISwitchable {
        void SwitchTo();
        void SwitchFrom();
    }

    public static class MainWindowPager {
        public static MainWindow Window { get; set; }

        public static void SwitchPage(Page page) {
            (Window.Content as ISwitchable)?.SwitchFrom();
            Window.Visibility = page == null ? Visibility.Hidden : Visibility.Visible;
            Window.Content = page;
            (Window.Content as ISwitchable)?.SwitchTo();
        }
    }

    public static class ClientConnection {
        public delegate Task AsyncRecvHandler(BugScapeMessage message);

        public static JsonClient Client { get; set; }
        public static event AsyncRecvHandler MessageReceivedEvent;

        public static async void StartReceivingTask() {
            try {
                Connect();
                while (true) {
                    try {
                        var data = await Client.RecvObjectAsync<BugScapeMessage>();
                        if (MessageReceivedEvent != null) await MessageReceivedEvent.Invoke(data);
                    } catch (IOException) {
                        /* Disconnected */
                        MessageBox.Show("Disconnected from server");
                        Connect();
                    } catch (Exception e) {
                        Debug.WriteLine("Exception while handling tcp client read: {0}", e);
                        Connect();
                    }
                }
            } catch (SocketException) {
                MainWindowPager.SwitchPage(null);
                MessageBox.Show("Can't connect to the server");
                Application.Current.Shutdown();
            }
        }

        private static void Connect() {
            Client?.Close();
            var tcpClient = new TcpClient(ServerSettings.ServerAddress, ServerSettings.ServerPort);
            tcpClient.NoDelay = true;
            Client = new JsonClient(tcpClient.GetStream());
            MainWindowPager.SwitchPage(new Pages.LoginPage());
        }
    }

    public partial class MainWindow {

        public MainWindow() {
            this.InitializeComponent();
            MainWindowPager.Window = this;
            ClientConnection.StartReceivingTask();
            MainWindowPager.SwitchPage(new Pages.LoginPage());
        }
    }
}
