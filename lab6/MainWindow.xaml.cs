using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class MainWindow : Window
    {
        Color currentColor = SystemColors.WindowFrameBrush.Color;
        Point currentPoint;
        Polyline currentPolyline;
        Server server = new Server();
        Client client = new Client();

        public MainWindow()
        {
            InitializeComponent();
            App.InitializeApp(this);
            canvas.IsEnabled = false;
            //Task.Factory.StartNew(() => CanvasUpdater());
        }
        
        // Server and Client management methods
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (client.Connect(ipaddr.Text, int.Parse(port.Text), canvas) != 0) return;

            canvas.IsEnabled = true;
            connectButton.IsEnabled = false;
            disconnectButton.IsEnabled = true;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            client.Disconnect();

            canvas.IsEnabled = false;
            connectButton.IsEnabled = true;
            disconnectButton.IsEnabled = false;
        }

        private void HostButton_Click(object sender, RoutedEventArgs e)
        {
            if (server.Start(canvas, ipaddr.Text, int.Parse(port.Text)) != 0) return;
            
            hostButton.IsEnabled = false;
            stopHostingButton.IsEnabled = true;
        }

        private void StopHostingButton_Click(object sender, RoutedEventArgs e)
        {
            server.Stop();
            
            hostButton.IsEnabled = true;
            stopHostingButton.IsEnabled = false;
        }

        // Canvas methods
        private void Cp_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (cp.SelectedColor.HasValue) { currentColor = cp.SelectedColor.Value; };
        }

        public void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                currentPoint = e.GetPosition(this);
                currentPolyline = new Polyline
                {
                    Stroke = new SolidColorBrush(currentColor),
                    StrokeThickness = lineWidthSlider.Value,
                    FillRule = FillRule.EvenOdd
                };
                canvas.Children.Add(currentPolyline);

                client.StartSending(currentPoint);
            }
        }

        public void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                currentPoint = e.GetPosition(this);
                currentPolyline.Points.Add(currentPoint);

                client.ContinueSending(currentPoint);
            }
        }

        public void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            client.StopSending();
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e) // to potrzebne? canvas i tak przytnie punkty do swojego obszaru...
        {
            //App.StopSending();
        }

        //private void CanvasUpdater()
        //{
        //    while (true)
        //    {
        //        List<Point> newPoints = client.IncomingPointsHandler();
        //        if (newPoints == null) continue;
        //        currentPolyline = new Polyline
        //        {
        //            Stroke = new SolidColorBrush(currentColor),
        //            StrokeThickness = lineWidthSlider.Value,
        //            FillRule = FillRule.EvenOdd
        //        };
        //        foreach (Point point in newPoints)
        //        {
        //            currentPolyline.Dispatcher.Invoke(() => currentPolyline.Points.Add(point));
        //        }
        //        canvas.Dispatcher.Invoke(() => canvas.Children.Add(currentPolyline) );
        //    }
        //}
    }
}
