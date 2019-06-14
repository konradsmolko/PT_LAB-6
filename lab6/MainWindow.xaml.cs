using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lab6
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


        }

        public void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point start;
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                start = e.GetPosition(this);
                App.StartSending(start);
            }

            
        }

        public void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            App.StopSending();
        }

        public void Canvas_MouseMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var point = e.GetPosition(this);
                App.ContinueSending(point);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            App.Connect(ipaddr.Text, int.Parse(port.Text));

            connectButton.IsEnabled = false;
            disconnectButton.IsEnabled = true;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            App.Disconnect();

            connectButton.IsEnabled = true;
            disconnectButton.IsEnabled = false;
        }

        private void HostButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StopHostingButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
