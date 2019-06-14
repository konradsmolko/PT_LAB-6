using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace lab6
{
    /// <summary>
    /// Logika interakcji dla klasy App.xaml
    /// </summary>
    public partial class App : Application
    {
        static UdpClient udpClient = new UdpClient();

        public static void Connect(string hostname, int port)
        {
            string text = "connect";
            byte[] buf = Encoding.ASCII.GetBytes(text);
            udpClient.Connect(hostname, port);
            udpClient.Send(buf, buf.Length);
        }

        public static void Disconnect()
        {
            string text = "disconnect";
            byte[] buf = Encoding.ASCII.GetBytes(text);
            udpClient.Send(buf, buf.Length);
            udpClient.Close();
        }

        public static void StartSending(Point start)
        {
            
        }

        public static void ContinueSending(Point point)
        {

        }

        public static void StopSending()
        {

        }

        public void Server(string ipaddr, int port)
        {
            Task.Factory.StartNew(
                () =>
                {
                    IPAddress address = IPAddress.Parse(ipaddr);
                    IPEndPoint endPoint = new IPEndPoint(address, port);
                    udpClient.Receive(ref endPoint);
                });
        }
    }
}
